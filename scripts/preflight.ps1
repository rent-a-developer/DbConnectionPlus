<#
.SYNOPSIS
    The pre-commit gate: repo-hygiene checks, style/formatting/ordering, a Release build, and the unit
    test suite.

.DESCRIPTION
    CONTRIBUTING.md requires that all tests pass and the build succeeds with no warnings. Because
    TreatWarningsAsErrors=true, the build is also the style, trim-analyzer and public-API gate: IL2xxx /
    IL3050 diagnostics fail it, and so does an undeclared or vanished public member (RS0016 / RS0017).

    AI agents have hooks that nag about the public API files as you edit, but a hook only sees edits made
    through a tool - and Codex only runs its hooks once they are trusted. This script repeats that check
    over the whole working tree. Run it before every commit, whichever agent you are.

    Two gates are deliberately NOT run here, because both take minutes:

    - Integration tests, which need four Docker containers. See .agents/skills/integration-db/SKILL.md.
    - The Native AOT gate, which packs the six projects and publishes a package consumer natively. Run
      scripts/verify-package-aot.ps1 -Pack after any change to a reflection path; nothing else in the
      repository can see silent trimming damage.

.PARAMETER SkipTidy
    Skip applying style, formatting and member ordering. The build still fails on any of them.

.PARAMETER SkipBuild
    Skip the Release build (implies -SkipTests).

.PARAMETER SkipTests
    Skip the unit test run.

.PARAMETER Configuration
    Build configuration. Release by default, because that is what CI and CONTRIBUTING.md use.

.EXAMPLE
    pwsh -File scripts/preflight.ps1
#>
[CmdletBinding()]
param(
    [Switch] $SkipBuild,
    [Switch] $SkipTests,
    [Switch] $SkipTidy,
    [String] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot 'DbConnectionPlus.slnx'
$unitTests = Join-Path $repositoryRoot 'tests/DbConnectionPlus.UnitTests/DbConnectionPlus.UnitTests.csproj'
$publicApiGuard = Join-Path $repositoryRoot 'scripts/public-api-guard.ps1'
$tidy = Join-Path $repositoryRoot 'scripts/tidy-cs.ps1'

$failures = New-Object System.Collections.Generic.List[String]

function Write-Section {
    param([String] $Title)

    Write-Output ''
    Write-Output "=== $Title ==="
}

# --- 1. Public API ------------------------------------------------------------------------------------
# Not a failure - a reminder. An accidental public-surface change is already a build error (RS0016 /
# RS0017 from Microsoft.CodeAnalysis.PublicApiAnalyzers, below); what this catches is a deliberate one
# that arrived without the companion edits CONTRIBUTING.md requires.

Write-Section 'Hygiene: public API'

$guardOutput = & $publicApiGuard

if ($guardOutput) {
    $guardOutput | ForEach-Object { Write-Output $_ }
}
else {
    Write-Output 'Unchanged.'
}

# --- 2. Style, formatting and member ordering ---------------------------------------------------------
# Applied, not just checked. All three are build errors, so leaving them to step 3 only means a slower
# way of finding out. The editor hooks format on every edit, but they deliberately skip the two slow
# tools - the code-style fixers and the member reordering - and this is where those run.
#
# It rewrites files. That is the point, and it is why this runs before the build.

if (-not $SkipTidy) {
    Write-Section 'Style, formatting and ordering'

    # Snapshot the dirty .cs files BEFORE tidying. `git diff` afterwards lists your own edits too, so
    # reporting its count would say "tidied 40 files" when the tools touched one of them. What the run
    # actually changed is the difference between the two lists.
    $dirtyBefore = @(& git -C $repositoryRoot diff --name-only -- '*.cs' 2>$null | Where-Object { $_ })

    & pwsh -NoProfile -NonInteractive -File $tidy -Scope all
    if ($LASTEXITCODE -ne 0) {
        $failures.Add('tidy')
        Write-Output 'FAIL - tidy-cs could not finish. Run `dotnet tool restore` if the tools are missing.'
    }
    else {
        $dirtyAfter = @(& git -C $repositoryRoot diff --name-only -- '*.cs' 2>$null | Where-Object { $_ })
        $tidied = @($dirtyAfter | Where-Object { $_ -notin $dirtyBefore })

        Write-Output ''
        if ($tidied) {
            Write-Output "Tidied $($tidied.Count) file(s) that you had not already changed:"
            $tidied | ForEach-Object { Write-Output "  $_" }
            Write-Output 'Review the diff and include it in your commit.'
        }
        elseif ($dirtyBefore) {
            # Everything the tools touched was already in your diff, so there is nothing new to point at -
            # but the tools may still have rewritten those files, and that is worth one line.
            Write-Output "Tidy ran clean. Your $($dirtyBefore.Count) changed .cs file(s) may have been rewritten - review the diff."
        }
        else {
            Write-Output 'Already tidy.'
        }
    }
}
else {
    Write-Section 'Style, formatting and ordering'
    Write-Output 'Skipped (-SkipTidy).'
}

# --- 3. Build ----------------------------------------------------------------------------------------

if (-not $SkipBuild) {
    Write-Section "Build ($Configuration)"

    & dotnet build $solution -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        $failures.Add('build')
        Write-Output 'FAIL - build did not succeed. TreatWarningsAsErrors=true, so a style slip or an'
        Write-Output 'IL2xxx/IL3050 trim diagnostic fails here too. Never suppress an IL warning to get green.'
    }
}
else {
    Write-Section 'Build'
    Write-Output 'Skipped (-SkipBuild).'
}

# --- 4. Unit tests -----------------------------------------------------------------------------------

if (-not $SkipBuild -and -not $SkipTests -and -not $failures.Contains('build')) {
    Write-Section 'Unit tests'

    & dotnet test --project $unitTests -c $Configuration --no-build
    if ($LASTEXITCODE -ne 0) { $failures.Add('unit tests') }
}
else {
    Write-Section 'Unit tests'
    Write-Output 'Skipped.'
}

# --- Summary -----------------------------------------------------------------------------------------

Write-Section 'Preflight summary'

if ($failures.Count -gt 0) {
    Write-Output "FAILED: $($failures -join ', ')"
    Write-Output 'Do not commit over this - fix it and re-run.'
    exit 1
}

Write-Output 'PASSED. Not covered here: integration tests (.agents/skills/integration-db/SKILL.md) and the'
Write-Output 'Native AOT gate (scripts/verify-package-aot.ps1 -Pack, after a reflection-path change).'
exit 0
