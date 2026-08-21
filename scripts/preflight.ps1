<#
.SYNOPSIS
    The pre-commit gate: repo-hygiene checks, a Release build, and the unit test suite.

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
    [String] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot 'DbConnectionPlus.slnx'
$unitTests = Join-Path $repositoryRoot 'tests/DbConnectionPlus.UnitTests/DbConnectionPlus.UnitTests.csproj'
$publicApiGuard = Join-Path $repositoryRoot 'scripts/public-api-guard.ps1'

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

# --- 2. Build ----------------------------------------------------------------------------------------

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

# --- 3. Unit tests -----------------------------------------------------------------------------------

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
