<#
.SYNOPSIS
    The pre-release gate: everything CI checks that can honestly be checked on this machine, in the order
    CI checks it.

.DESCRIPTION
    Run this before pushing a release - or any branch you want CI to go green on. It is the pre-commit
    gate plus the jobs that gate a publish: the full integration matrix, the documentation build with
    warnings as errors, the pack with package validation, the Native AOT gate against the packed packages,
    and the all-adapters package consumer.

    It stops at the first failure. Later steps consume what earlier ones produce - the AOT gate and the
    consumer run against the packages the pack step wrote - so continuing past a failure would only
    produce a second, misleading one.

    WHAT IT DOES NOT COVER, and why. None of this is a judgement about importance; each one either needs
    infrastructure this machine does not have or is meaningless outside CI:

      CodeQL, dependency review    Need GitHub's analysis and advisory services.
      Codecov upload               Needs the OIDC token CI holds. The coverage numbers are produced by the
                                   test run here; it is only the upload that cannot happen.
      GitHub Pages deployment      Needs the Pages environment. The site itself IS built here.
      NuGet publication            Needs Trusted Publishing and a tag. Nothing here publishes anything.
      The Linux legs               CI runs the AOT gate and the consumers on Linux and Windows. This runs
                                   them on whichever platform you are on.
      The .NET-8-SDK-only leg      CI installs the 8.0 SDK alone to prove the documented net8.0 floor.
                                   This machine resolves the SDK the root global.json pins, so the
                                   consumer step here proves the packages work - not that they work with
                                   nothing but an 8.0 SDK installed.

    A dirty working tree is reported, not rejected: CI tests the commit you push, so anything uncommitted
    is untested by definition, but you may well still be iterating.

.PARAMETER Version
    The version you are about to release, for example 4.1.0. When given, two extra checks run - the same
    two CI runs immediately before it publishes: the <Version> in the repository-root Directory.Build.props
    must match, and CHANGELOG.md must hold exactly one dated, non-empty section for it. Omit it and both
    are skipped, because a branch push publishes nothing.

.PARAMETER SkipIntegrationTests
    Skip the integration suite. It needs a running Docker daemon and takes about ten minutes.

.PARAMETER SkipNativeAot
    Skip the Native AOT gate. It needs a C++ toolchain - MSVC on Windows, clang and zlib1g-dev on Linux.

.PARAMETER Configuration
    Build configuration. Release by default, because that is what CI uses and what gets published.

.EXAMPLE
    pwsh -File scripts/pre-release-gate.ps1

.EXAMPLE
    pwsh -File scripts/pre-release-gate.ps1 -Version 4.1.0
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [String] $Version,
    [Switch] $SkipIntegrationTests,
    [Switch] $SkipNativeAot,
    [String] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up, whatever the current directory is.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$solutionFileName = 'DbConnectionPlus.slnx'
$unitTestProject = 'tests/DbConnectionPlus.UnitTests/DbConnectionPlus.UnitTests.csproj'
$consumerDirectory = Join-Path $repositoryRoot 'tests/package-consumption'
$packageOutput = Join-Path $repositoryRoot 'artifacts/packages'
$docfxConfiguration = 'build/docfx/docfx.json'

$failure = $null
$stepSucceeded = $true

function Write-Section
{
    param([Parameter(Mandatory)] [String] $Title)

    Write-Output ''
    Write-Output "=== $Title ==="
}

function Invoke-FromRepositoryRoot
{
    <#
        Runs a command with the repository root as the working directory, restoring the caller's location
        in a finally block. It returns NOTHING and the caller reads $LASTEXITCODE - returning the exit code
        would mix it into the pipeline with everything the command printed, and that output is exactly what
        you need when this step is the one that failed.

        The working directory is not cosmetic: `dotnet` resolves global.json from the CURRENT directory
        upward, and this repository's global.json is what pins the SDK and selects the
        Microsoft.Testing.Platform test runner.
    #>
    param([Parameter(Mandatory)] [ScriptBlock] $Command)

    Push-Location -LiteralPath $repositoryRoot
    try
    {
        & $Command
    }
    finally
    {
        Pop-Location
    }
}

function Get-DeclaredVersion
{
    # The single source of truth for the version of every package, and what `dotnet pack` produces.
    $sharedProperties = Join-Path $repositoryRoot 'Directory.Build.props'

    return ([Xml] (Get-Content -Raw $sharedProperties)).Project.PropertyGroup.Version |
        Where-Object { $_ } |
        Select-Object -First 1
}

function Invoke-Step
{
    <#
        Runs one step unless an earlier one failed. $script:failure holds the name of the first failure and
        every later step becomes a no-op, so the output ends with the failure that matters rather than with
        whatever fell over as a consequence of it.

        A step reports its result through $script:stepSucceeded, and NOT by returning a value. In PowerShell
        a script block's output IS its return value, so `if (-not (& $Command))` would capture every line the
        build, the test run and the AOT gate print - the caller would see an array of build output rather
        than a result, nothing would reach the screen until the step ended, and a failing step that printed
        anything at all would evaluate as true and be recorded as a pass. Calling it bare lets its output
        stream straight to the console, which is where "See output" points.
    #>
    param(
        [Parameter(Mandatory)] [String] $Name,
        [Parameter(Mandatory)] [ScriptBlock] $Command
    )

    if ($script:failure)
    {
        return
    }

    Write-Section $Name

    # A step that returns without saying otherwise passed.
    $script:stepSucceeded = $true

    & $Command

    if (-not $script:stepSucceeded)
    {
        $script:failure = $Name
    }
}

# --- Working tree -------------------------------------------------------------------------------------

Write-Section 'Working tree'

$uncommitted = @(& git -C $repositoryRoot status --porcelain | Where-Object { $_ })

if ($uncommitted)
{
    Write-Output "$($uncommitted.Count) uncommitted change(s). CI tests the commit you push, so these are not covered:"
    $uncommitted | Select-Object -First 10 | ForEach-Object { Write-Output "  $_" }

    if ($uncommitted.Count -gt 10)
    {
        Write-Output "  ... and $($uncommitted.Count - 10) more"
    }
}
else
{
    Write-Output 'Clean.'
}

# --- Release identity ---------------------------------------------------------------------------------
# CI verifies both of these immediately before it publishes. A tag that does not match the packed version
# publishes stale packages or, thanks to --skip-duplicate, nothing at all; a missing changelog section
# produces a release whose notes are empty. Neither runs on a branch push, so both are opt-in here.

Invoke-Step 'Release identity' {
    if (-not $Version)
    {
        Write-Output 'Skipped - pass -Version to check the declared version and the changelog section.'

        return
    }

    $declared = Get-DeclaredVersion

    if ($declared -ne $Version)
    {
        Write-Output "FAILED - Directory.Build.props declares $declared, not $Version."

        $script:stepSucceeded = $false

        return
    }

    Write-Output "Directory.Build.props declares $declared."

    Invoke-FromRepositoryRoot {
        & pwsh -NoProfile -NonInteractive -File 'scripts/extract-release-notes.ps1' -Version $Version
    }

    $script:stepSucceeded = ($LASTEXITCODE -eq 0)
}

# --- Ignored revisions --------------------------------------------------------------------------------
# The lint job's first check, and the cheapest. A revision in .git-blame-ignore-revs that no longer
# resolves is skipped silently by git, and blame goes back to pointing at the tool that reformatted the
# line - which is exactly what squashing or rebasing a branch that added one does.

Invoke-Step 'Ignored revisions' {
    $revisions = @(Get-Content -LiteralPath (Join-Path $repositoryRoot '.git-blame-ignore-revs') |
        Where-Object { $_ -match '^[0-9a-f]{40}$' })

    if (-not $revisions)
    {
        Write-Output 'FAILED - .git-blame-ignore-revs lists no revisions.'

        $script:stepSucceeded = $false

        return
    }

    $unresolved = @($revisions | Where-Object {
            & git -C $repositoryRoot cat-file -e "$_^{commit}" 2>$null

            $LASTEXITCODE -ne 0
        })

    if ($unresolved)
    {
        Write-Output 'FAILED - these revisions are not commits in this repository:'
        $unresolved | ForEach-Object { Write-Output "  $_" }
        Write-Output 'A rebase or a squash merge rewrote them. Replace each with the SHA it became.'

        $script:stepSucceeded = $false

        return
    }

    Write-Output "$($revisions.Count) revision(s), all resolve."

    return
}

# --- Style, formatting and ordering -------------------------------------------------------------------
# The lint job. All of it is also a build error, so this is only the faster way to find out - but it prints
# the diff that would fix things, where the build names the file and stops. It checks a disposable copy of
# the tree and never writes to yours.

Invoke-Step 'Style, formatting and ordering' {
    Invoke-FromRepositoryRoot {
        & pwsh -NoProfile -NonInteractive -File 'scripts/tidy-code.ps1' -Scope all -Check
    }

    if ($LASTEXITCODE -ne 0)
    {
        Write-Output ''
        Write-Output 'FAILED - the tree is not tidy. Apply it with: pwsh -File scripts/pre-commit-gate.ps1 -Fix'

        $script:stepSucceeded = $false

        return
    }

    return
}

# --- Build --------------------------------------------------------------------------------------------
# TreatWarningsAsErrors is on for every project, so this is also the style, trim-analyzer and public-API
# gate: an IL2xxx/IL3050 diagnostic fails here, and so does an undeclared or vanished public member.

Invoke-Step "Build ($Configuration)" {
    Invoke-FromRepositoryRoot { & dotnet build $solutionFileName -c $Configuration }

    $script:stepSucceeded = ($LASTEXITCODE -eq 0)
}

# --- Tests --------------------------------------------------------------------------------------------
# CI runs the whole solution in one command: the unit suite on net8.0 and net10.0, plus the full
# integration matrix. --no-build and the configuration are both required - without them `dotnet test`
# silently rebuilds in Debug and tests that instead of the Release build above.

Invoke-Step 'Tests' {
    if ($SkipIntegrationTests)
    {
        Write-Output 'Integration suite skipped (-SkipIntegrationTests). Unit suite only.'

        Invoke-FromRepositoryRoot {
            & dotnet test --project $unitTestProject -c $Configuration --no-build
        }

        $script:stepSucceeded = ($LASTEXITCODE -eq 0)

        return
    }

    & docker version --format '{{.Server.Version}}' *> $null

    if ($LASTEXITCODE -ne 0)
    {
        Write-Output 'FAILED - the integration suite needs a running Docker daemon. Start Docker, or pass'
        Write-Output '-SkipIntegrationTests to run the unit suite alone.'

        $script:stepSucceeded = $false

        return
    }

    Invoke-FromRepositoryRoot {
        & dotnet test --solution $solutionFileName -c $Configuration --no-build
    }

    $script:stepSucceeded = ($LASTEXITCODE -eq 0)
}

# --- Documentation ------------------------------------------------------------------------------------
# --warningsAsErrors, because a docfx warning is a broken cross-reference or a file the configuration does
# not reach, and both of those reach the published site as a hole.

Invoke-Step 'Documentation' {
    Invoke-FromRepositoryRoot { & dotnet tool run docfx metadata $docfxConfiguration }

    if ($LASTEXITCODE -ne 0)
    {
        Write-Output 'FAILED - docfx metadata. Run `dotnet tool restore` if docfx is missing.'

        $script:stepSucceeded = $false

        return
    }

    Invoke-FromRepositoryRoot { & dotnet tool run docfx build $docfxConfiguration --warningsAsErrors }

    $script:stepSucceeded = ($LASTEXITCODE -eq 0)
}

# --- Pack ---------------------------------------------------------------------------------------------
# Package validation runs here, against the last published version: removing or changing a public
# signature fails the pack rather than reaching nuget.org. Both steps below consume what this writes.

Invoke-Step 'Pack' {
    Invoke-FromRepositoryRoot {
        & dotnet pack $solutionFileName -c $Configuration --no-build -o $packageOutput
    }

    if ($LASTEXITCODE -ne 0)
    {
        $script:stepSucceeded = $false

        return
    }

    # `dotnet pack --no-build` says nothing at all when it succeeds, which in a gate reads like a step that
    # did not run. Name what it produced instead.
    Get-ChildItem -LiteralPath $packageOutput -Filter '*.nupkg' |
        Sort-Object Name |
        ForEach-Object { Write-Output "  $($_.Name)" }
}

# --- Native AOT gate ----------------------------------------------------------------------------------
# The only check in the repository that can see silent trimming damage: nothing is trimmed on the JIT, so
# the entire unit and integration suite passes with a broken annotation chain. Both target frameworks,
# because net8.0 is not the same code as net10.0 - it carries an IL3050 suppression net10.0 does not.

Invoke-Step 'Native AOT gate' {
    if ($SkipNativeAot)
    {
        Write-Output 'Skipped (-SkipNativeAot).'

        return
    }

    foreach ($framework in @('net8.0', 'net10.0'))
    {
        Invoke-FromRepositoryRoot {
            & pwsh -NoProfile -NonInteractive -File 'scripts/verify-package-aot.ps1' `
                -Framework $framework -Configuration $Configuration
        }

        if ($LASTEXITCODE -ne 0)
        {
            Write-Output "FAILED - the Native AOT gate failed on $framework."

            $script:stepSucceeded = $false

            return
        }
    }

    return
}

# --- Package consumer ---------------------------------------------------------------------------------
# Installs every package the way a stranger would, and asserts that each adapter's driver dependency flowed
# transitively and that one shared DbConnectionPlus assembly satisfies all of them - none of which a
# project-referenced test can see. NUGET_PACKAGES and RestoreConfigFile are overridden so the run cannot
# quietly resolve a DbConnectionPlus assembly that did not come out of the packages just packed.

Invoke-Step 'Package consumer' {
    $packageVersion = Get-DeclaredVersion
    $originalNuGetPackages = $env:NUGET_PACKAGES
    $consumerNuGetConfig = Join-Path $consumerDirectory 'nuget.config'

    Push-Location -LiteralPath $consumerDirectory
    try
    {
        $env:NUGET_PACKAGES = Join-Path $consumerDirectory '.packages'

        & dotnet run --project 'AllAdaptersConsumer/AllAdaptersConsumer.csproj' -c $Configuration `
            -p:RestoreConfigFile=$consumerNuGetConfig -p:DbConnectionPlusVersion=$packageVersion

        $script:stepSucceeded = ($LASTEXITCODE -eq 0)
    }
    finally
    {
        $env:NUGET_PACKAGES = $originalNuGetPackages
        Pop-Location
    }
}

# --- Summary ------------------------------------------------------------------------------------------

Write-Section 'Pre-release gate summary'

if ($failure)
{
    Write-Output "FAILED: Check $failure failed. See output."

    exit 1
}

Write-Output 'PASSED: All checks passed.'

exit 0
