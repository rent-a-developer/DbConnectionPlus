<#
.SYNOPSIS
    Formats C# files against .editorconfig, the way this repository's build expects them.

.DESCRIPTION
    This repo builds with TreatWarningsAsErrors=true, AnalysisLevel=latest-all, EnforceCodeStyleInBuild=true,
    and .editorconfig puts csharp_style_expression_bodied_* at `error` severity. A style slip is therefore a
    hard build break, and fixing it at edit time is much cheaper than discovering it at build time.

    AI agents call this from a PostToolUse hook after every .cs edit - Claude Code through
    .claude/hooks/format-cs.ps1 and Codex through .codex/hooks/format-cs.ps1. A human runs it before
    building. Keep the logic here rather than in a hook, so all three run the same thing.

.PARAMETER Path
    One or more .cs files to format. With no Path, every .cs file git reports as changed (tracked
    modifications plus untracked files) is formatted.

.PARAMETER Scope
    How much dotnet format does:
      style       ~4s  - IDExxxx code-style rules, incl. the expression-bodied ones. Default.
      all         ~9s  - whitespace + style + 3rd-party analyzers (Roslynator, ErrorProne.NET).
      whitespace  ~2s  - indentation and spacing only.

.EXAMPLE
    pwsh -File scripts/format-cs.ps1

.EXAMPLE
    pwsh -File scripts/format-cs.ps1 src/DbConnectionPlus/Entities/EntityHelper.cs
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [String[]] $Path,

    [ValidateSet('style', 'all', 'whitespace')]
    [String] $Scope = 'style'
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up.
$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Get-ChangedCSharpFile {
    param([String] $RepositoryRoot)

    # 2>$null: git warns about CRLF normalization per file, which is noise here.
    $tracked = & git -C $RepositoryRoot diff --name-only HEAD -- '*.cs' 2>$null
    $untracked = & git -C $RepositoryRoot ls-files --others --exclude-standard -- '*.cs' 2>$null

    return @($tracked) + @($untracked) |
        Where-Object { $_ } |
        ForEach-Object { Join-Path $RepositoryRoot $_ }
}

function Get-OwningProject {
    param([String] $FilePath)

    $directory = Split-Path -Parent $FilePath
    while ($directory) {
        $candidate = Get-ChildItem -LiteralPath $directory -Filter '*.csproj' -File -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($candidate) { return $candidate.FullName }
        $directory = Split-Path -Parent $directory
    }

    return $null
}

if (-not $Path -or $Path.Count -eq 0) {
    $Path = Get-ChangedCSharpFile -RepositoryRoot $repositoryRoot
}

$files = @($Path) |
    Where-Object { $_ } |
    Where-Object { [System.IO.Path]::GetExtension($_) -eq '.cs' } |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    ForEach-Object { (Resolve-Path -LiteralPath $_).Path } |
    # Generated and build output are not ours to format.
    Where-Object { $_ -notmatch '[\\/](bin|obj)[\\/]' } |
    Select-Object -Unique

if ($files.Count -eq 0) {
    Write-Output 'format-cs: nothing to format.'
    exit 0
}

$failed = $false

# One dotnet format invocation per owning project, so MSBuild loads one project rather than the solution.
$files | Group-Object { Get-OwningProject -FilePath $_ } | ForEach-Object {
    $project = $_.Name
    if ([String]::IsNullOrWhiteSpace($project) -or -not (Test-Path -LiteralPath $project)) {
        Write-Output "format-cs: no owning .csproj for $($_.Group -join ', ') - skipped."
        return
    }

    $projectDirectory = Split-Path -Parent $project

    # `--include` matches RELATIVE paths only. Handed an absolute path it matches nothing, reports success
    # and formats nothing - a silent no-op that looks exactly like a clean file. So run from the project
    # directory and pass each file relative to it.
    $relativePaths = $_.Group | ForEach-Object { [System.IO.Path]::GetRelativePath($projectDirectory, $_) }

    $arguments = @('format')
    if ($Scope -ne 'all') { $arguments += $Scope }
    $arguments += @($project, '--include') + $relativePaths + @('--no-restore', '-v', 'q')

    Push-Location -LiteralPath $projectDirectory
    try { $output = & dotnet @arguments 2>&1 }
    finally { Pop-Location }

    if ($LASTEXITCODE -ne 0) {
        $failed = $true
        Write-Output "format-cs: dotnet format failed for $(Split-Path -Leaf $project):`n$output"
    }
    else {
        Write-Output "format-cs: formatted $($relativePaths.Count) file(s) in $(Split-Path -Leaf $project)."
    }
}

if ($failed) { exit 1 }
exit 0
