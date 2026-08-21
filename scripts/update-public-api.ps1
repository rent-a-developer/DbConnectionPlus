<#
.SYNOPSIS
    Records the current public surface of the shipping projects in their PublicAPI.Unshipped.txt files.

.DESCRIPTION
    The six shipping projects are guarded by Microsoft.CodeAnalysis.PublicApiAnalyzers: a public member that
    is not listed in the project's PublicAPI.Shipped.txt or PublicAPI.Unshipped.txt is RS0016, and a listed
    member that no longer exists is RS0017. Both are build errors here, because TreatWarningsAsErrors is on -
    so an unintended change to the public surface breaks the build rather than slipping through review.

    This script applies the RS0016 code fix, which writes the missing entries into PublicAPI.Unshipped.txt.
    It creates the two files first where they are missing: dotnet format applies the fix but will not create
    the files, and without them the analyzer reports nothing at all.

    Review the diff it produces. That diff IS the public-API change, and per CONTRIBUTING.md a real one also
    needs a CHANGELOG entry, a README update and a SemVer bump in src/Directory.Build.props.

    At release time the accumulated entries move from PublicAPI.Unshipped.txt to PublicAPI.Shipped.txt, and
    a removal is recorded in PublicAPI.Unshipped.txt as `*REMOVED*<signature>`.

.PARAMETER Project
    One or more project files to update. Defaults to all six shipping projects under src/.

.PARAMETER MarkShipped
    The release step instead of the edit step: fold PublicAPI.Unshipped.txt into PublicAPI.Shipped.txt and
    leave Unshipped empty. `*REMOVED*` entries delete the matching Shipped line rather than being carried
    over. Run this when a version is released, so that the next release's Unshipped.txt again means "new
    since the last release".

.EXAMPLE
    pwsh -File scripts/update-public-api.ps1

.EXAMPLE
    pwsh -File scripts/update-public-api.ps1 src/DbConnectionPlus/DbConnectionPlus.csproj

.EXAMPLE
    pwsh -File scripts/update-public-api.ps1 -MarkShipped
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [String[]] $Project,

    [Switch] $MarkShipped
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot

if (-not $Project -or $Project.Count -eq 0) {
    $Project = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src') -Recurse -File -Filter '*.csproj' |
        ForEach-Object { $_.FullName }
}

$failed = $false

foreach ($projectFile in $Project) {
    if (-not (Test-Path -LiteralPath $projectFile)) {
        Write-Output "update-public-api: $projectFile not found - skipped."
        continue
    }

    $projectFile = (Resolve-Path -LiteralPath $projectFile).Path
    $projectDirectory = Split-Path -Parent $projectFile
    $projectName = Split-Path -Leaf $projectFile

    # dotnet format applies the RS0016 fix but never creates these files, and the analyzer stays silent
    # while they are absent - so creating them is what switches the guard on for a project.
    #
    # PublicAPI.Shipped.txt starts with '#nullable enable': the shipping projects compile with
    # Nullable=enable, and without that header the analyzer records no nullability at all and reports
    # RS0037 for every annotated member. With it, `string` and `string?` are different API entries, so a
    # nullability change to a public signature shows up as the source-breaking change it is.
    $headerPerFileName = @{
        'PublicAPI.Shipped.txt' = '#nullable enable'
        'PublicAPI.Unshipped.txt' = ''
    }

    foreach ($fileName in $headerPerFileName.Keys) {
        $filePath = Join-Path $projectDirectory $fileName

        if (-not (Test-Path -LiteralPath $filePath)) {
            Set-Content -LiteralPath $filePath -Value $headerPerFileName[$fileName] -NoNewline:($headerPerFileName[$fileName] -eq '')
            Write-Output "update-public-api: created $fileName for $projectName."
        }
    }

    if ($MarkShipped) {
        $shippedPath = Join-Path $projectDirectory 'PublicAPI.Shipped.txt'
        $unshippedPath = Join-Path $projectDirectory 'PublicAPI.Unshipped.txt'

        $unshipped = @(Get-Content -LiteralPath $unshippedPath | Where-Object { $_.Trim() })

        if ($unshipped.Count -eq 0) {
            Write-Output "update-public-api: nothing unshipped in $projectName."
            continue
        }

        $shipped = @(Get-Content -LiteralPath $shippedPath | Where-Object { $_.Trim() -and $_ -ne '#nullable enable' })

        $removed = @($unshipped | Where-Object { $_.StartsWith('*REMOVED*', [StringComparison]::Ordinal) }) |
            ForEach-Object { $_.Substring('*REMOVED*'.Length) }
        $added = @($unshipped | Where-Object { -not $_.StartsWith('*REMOVED*', [StringComparison]::Ordinal) })

        $shipped = @($shipped | Where-Object { $removed -notcontains $_ }) + $added |
            Sort-Object -Unique

        Set-Content -LiteralPath $shippedPath -Value (@('#nullable enable') + $shipped)
        Set-Content -LiteralPath $unshippedPath -Value '' -NoNewline

        Write-Output "update-public-api: marked $($added.Count) added and $($removed.Count) removed API(s) as shipped in $projectName."

        continue
    }

    $output = & dotnet format analyzers $projectFile --diagnostics RS0016 --severity info -v q 2>&1

    if ($LASTEXITCODE -ne 0) {
        $failed = $true
        Write-Output "update-public-api: dotnet format failed for ${projectName}:`n$output"
    }
    else {
        Write-Output "update-public-api: updated $projectName."
    }
}

if ($failed) { exit 1 }

Write-Output ''

if ($MarkShipped) {
    Write-Output 'PublicAPI.Unshipped.txt is empty again. The next entry that appears there is new since this release.'
}
else {
    Write-Output 'Review the PublicAPI.*.txt diff - it is the public-API change, and a real one also needs a'
    Write-Output 'CHANGELOG entry, a README update and a SemVer bump (see CONTRIBUTING.md).'
}

exit 0
