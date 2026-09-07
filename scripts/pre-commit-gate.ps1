<#
.SYNOPSIS
    The pre-commit gate: repository hygiene, style/formatting/ordering, a Release build, and the unit
    test suite on net8.0 and net10.0.

.DESCRIPTION
    CONTRIBUTING.md requires that all tests pass and the build succeeds with no warnings. Because
    TreatWarningsAsErrors=true, the build is also the style, trim-analyzer and public-API gate: IL2xxx /
    IL3050 diagnostics fail it, and so does an undeclared or vanished public member (RS0016 / RS0017).

    AI agents have hooks that nag about the public API files as you edit, but a hook only sees edits made
    through a tool - and Codex only runs its hooks once they are trusted. This script repeats that check
    over the whole working tree. Run it before every commit, whichever agent you are.

    WHAT THIS SCRIPT WRITES. By default: build output and package caches, and nothing else. It does not
    edit your source files and it does not touch the git index - the tidiness step runs as a CHECK, on a
    disposable copy of the tree. Pass -Fix to have it tidy the working tree first.

    TWO GATES ARE DELIBERATELY NOT RUN HERE, because both take minutes and neither applies to every
    change. Run them when their trigger applies:

      Integration tests   Trigger: SQL generation, an adapter, the CRUD or temporary-table paths, type
                          mapping - anything a substituted DbDataReader cannot exercise honestly. Needs
                          Docker. Scope the run; the default scope is SQLite + SQL Server, and a change
                          to the shared adapter seam obliges all five.
                          See .agents/skills/integration-db/SKILL.md.

      Native AOT gate     Trigger: any change to reflection, the [DynamicallyAccessedMembers]
                          annotations, the materializers or the temporary-table readers. Needs a C++
                          toolchain. It is the ONLY check in the repository that can see silent trimming
                          damage, because nothing is trimmed on the JIT.
                          Run: pwsh -File scripts/verify-package-aot.ps1 -Pack

.PARAMETER Fix
    Apply style, formatting and member ordering to the working tree before the checks, instead of only
    reporting what would change. This rewrites your files; review the diff and include it in your commit.

.PARAMETER SkipTidy
    Skip the style, formatting and ordering step entirely. The build still fails on any of them.

.PARAMETER SkipBuild
    Skip the Release build (implies -SkipTests).

.PARAMETER SkipTests
    Skip the unit test run.

.PARAMETER Configuration
    Build configuration. Release by default, because that is what CI and CONTRIBUTING.md use.

.EXAMPLE
    pwsh -File scripts/pre-commit-gate.ps1

.EXAMPLE
    pwsh -File scripts/pre-commit-gate.ps1 -Fix
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [Switch] $Fix,
    [Switch] $SkipBuild,
    [Switch] $SkipTests,
    [Switch] $SkipTidy,
    [String] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up. Everything below is anchored to it, so the
# script behaves the same whatever the current directory is.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$solutionFileName = 'DbConnectionPlus.slnx'
$unitTestProject = 'tests/DbConnectionPlus.UnitTests/DbConnectionPlus.UnitTests.csproj'
$publicApiGuard = Join-Path $repositoryRoot 'scripts/public-api-guard.ps1'
$tidy = Join-Path $repositoryRoot 'scripts/tidy-code.ps1'

$failures = New-Object System.Collections.Generic.List[String]

function Write-Section
{
    param([Parameter(Mandatory)] [String] $Title)

    Write-Output ''
    Write-Output "=== $Title ==="
}

function Invoke-FromRepositoryRoot
{
    <#
        Runs a native command with the repository root as the working directory. The location is restored
        in a finally block, so an interrupted run does not leave the caller's shell somewhere else.

        It deliberately returns NOTHING and the caller reads $LASTEXITCODE afterwards. Returning the exit
        code would put it on the pipeline together with everything the command printed, so the caller
        would receive an array of build output with a number on the end - and the build log would vanish
        into a variable instead of reaching the screen, which is exactly where it is needed when the build
        is what failed.

        The working directory is not cosmetic here. `dotnet test` resolves global.json from the CURRENT
        directory upward, and this repository's global.json is what selects the Microsoft.Testing.Platform
        runner. Run it from anywhere else and the setting is silently lost, along with every option that
        depends on it.
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

foreach ($required in @($publicApiGuard, $tidy))
{
    if (-not (Test-Path -LiteralPath $required -PathType Leaf))
    {
        Write-Output "pre-commit-gate: FAILED - $required does not exist."

        exit 1
    }
}

# --- 1. Public API ------------------------------------------------------------------------------------
# Not a failure - a reminder. An accidental public-surface change is already a build error (RS0016 /
# RS0017 from Microsoft.CodeAnalysis.PublicApiAnalyzers, below); what this catches is a deliberate one
# that arrived without the companion edits CONTRIBUTING.md requires. It reads; it never writes.

Write-Section 'Hygiene: public API'

$guardOutput = & pwsh -NoProfile -NonInteractive -File $publicApiGuard
$guardExitCode = $LASTEXITCODE

if ($guardExitCode -ne 0)
{
    $failures.Add('public-api-guard')
    Write-Output "FAIL - public-api-guard.ps1 exited with code $guardExitCode."
}
elseif ($guardOutput)
{
    $guardOutput | ForEach-Object { Write-Output $_ }
}
else
{
    Write-Output 'Unchanged.'
}

# --- 2. Style, formatting and member ordering ---------------------------------------------------------
# All three are build errors, so leaving them to step 3 only means a slower way of finding out - and the
# check prints the exact diff that would fix things, where the build only names the file.
#
# By default this REPORTS. -Fix applies. The editor hooks format on every edit, but they deliberately skip
# the two slow tools - the code-style fixers and the member reordering - and this is where those run.

Write-Section 'Style, formatting and ordering'

if ($SkipTidy)
{
    Write-Output 'Skipped (-SkipTidy).'
}
elseif ($Fix)
{
    # Snapshot the dirty .cs files BEFORE tidying. `git diff` afterwards lists your own edits too, so
    # reporting its count would say "tidied 40 files" when the tools touched one of them. What the run
    # actually changed is the difference between the two lists.
    $dirtyBefore = @(& git -C $repositoryRoot diff --name-only -- '*.cs' 2>$null | Where-Object { $_ })

    & pwsh -NoProfile -NonInteractive -File $tidy -Scope all
    $tidyExitCode = $LASTEXITCODE

    if ($tidyExitCode -ne 0)
    {
        $failures.Add('tidy')
        Write-Output 'FAIL - tidy-code could not finish. Run `dotnet tool restore` if the tools are missing.'
    }
    else
    {
        $dirtyAfter = @(& git -C $repositoryRoot diff --name-only -- '*.cs' 2>$null | Where-Object { $_ })
        $tidied = @($dirtyAfter | Where-Object { $_ -notin $dirtyBefore })

        Write-Output ''
        if ($tidied)
        {
            Write-Output "Tidied $($tidied.Count) file(s) that you had not already changed:"
            $tidied | ForEach-Object { Write-Output "  $_" }
            Write-Output 'Review the diff and include it in your commit.'
        }
        elseif ($dirtyBefore)
        {
            # Everything the tools touched was already in your diff, so there is nothing new to point at -
            # but the tools may still have rewritten those files, and that is worth one line.
            Write-Output "Tidy ran clean. Your $($dirtyBefore.Count) changed .cs file(s) may have been rewritten - review the diff."
        }
        else
        {
            Write-Output 'Already tidy.'
        }
    }
}
else
{
    & pwsh -NoProfile -NonInteractive -File $tidy -Scope all -Check
    $tidyExitCode = $LASTEXITCODE

    if ($tidyExitCode -ne 0)
    {
        $failures.Add('tidy')
        Write-Output ''
        Write-Output 'FAIL - the tree is not tidy, or tidy-code could not finish. The diff above is what'
        Write-Output 'would fix it. Apply it with: pwsh -File scripts/pre-commit-gate.ps1 -Fix'
    }
}

# --- 3. Build ----------------------------------------------------------------------------------------

if ($SkipBuild)
{
    Write-Section 'Build'
    Write-Output 'Skipped (-SkipBuild).'
}
else
{
    Write-Section "Build ($Configuration)"

    Invoke-FromRepositoryRoot { & dotnet build $solutionFileName -c $Configuration }
    $buildExitCode = $LASTEXITCODE

    if ($buildExitCode -ne 0)
    {
        $failures.Add('build')
        Write-Output 'FAIL - build did not succeed. TreatWarningsAsErrors=true, so a style slip or an'
        Write-Output 'IL2xxx/IL3050 trim diagnostic fails here too. Never suppress an IL warning to get green.'
    }
}

# --- 4. Unit tests -----------------------------------------------------------------------------------
# Both target frameworks, because the two builds of the shipping libraries are not the same code: the
# net8.0 build carries an IL3050 suppression the net10.0 build does not. The project multi-targets, so a
# single `dotnet test` covers both - the summary names each one.

if ($SkipBuild -or $SkipTests -or $failures.Contains('build'))
{
    Write-Section 'Unit tests'
    Write-Output 'Skipped.'
}
else
{
    Write-Section 'Unit tests (net8.0 and net10.0)'

    Invoke-FromRepositoryRoot {
        & dotnet test --project $unitTestProject -c $Configuration --no-build
    }
    $testExitCode = $LASTEXITCODE

    if ($testExitCode -ne 0) { $failures.Add('unit tests') }
}

# --- Summary -----------------------------------------------------------------------------------------

Write-Section 'Pre-commit gate summary'

if ($failures.Count -gt 0)
{
    Write-Output "FAILED: $($failures -join ', ')"
    Write-Output 'Do not commit over this - fix it and re-run.'

    exit 1
}

Write-Output 'PASSED. Not covered here, and not needed for every change:'
Write-Output '  - integration tests, when the change can only be proven against a real database'
Write-Output '    (.agents/skills/integration-db/SKILL.md)'
Write-Output '  - the Native AOT gate, after a change to a reflection path'
Write-Output '    (pwsh -File scripts/verify-package-aot.ps1 -Pack)'

exit 0
