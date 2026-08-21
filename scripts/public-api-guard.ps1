<#
.SYNOPSIS
    Prints the CONTRIBUTING.md companion-edit checklist when a project's public API files change.

.DESCRIPTION
    The public surface of the six shipping projects is declared in their PublicAPI.Shipped.txt and
    PublicAPI.Unshipped.txt files and enforced by Microsoft.CodeAnalysis.PublicApiAnalyzers - the build
    fails on a public member that is not declared (RS0016) or declared but gone (RS0017), so the build
    already stops an *accidental* change.

    What the build cannot know is whether a *deliberate* one was accompanied by its companion edits.
    CONTRIBUTING.md requires three (CHANGELOG entry, README update, SemVer bump) and they are easy to
    forget, so this reminds you when one of those files moves.

    This is the shared implementation. AI agents call it from a PostToolUse hook - Claude Code through
    .claude/hooks/public-api-guard.ps1 and Codex through .codex/hooks/public-api-guard.ps1 - and
    scripts/preflight.ps1 runs it over the whole working tree. It only ever reports; it never fails
    anything.

.PARAMETER Path
    One or more paths that were just edited. With no Path, every file git reports as changed (tracked
    modifications plus untracked files) is examined, which is what an agent whose tool payload does not
    carry a file path needs.

.EXAMPLE
    pwsh -File scripts/public-api-guard.ps1

.EXAMPLE
    pwsh -File scripts/public-api-guard.ps1 src/DbConnectionPlus/PublicAPI.Unshipped.txt
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [String[]] $Path
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up.
$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Get-ChangedFile {
    param([String] $RepositoryRoot)

    # 2>$null: git warns about CRLF normalization per file, which is noise here.
    $tracked = & git -C $RepositoryRoot diff --name-only HEAD 2>$null
    $untracked = & git -C $RepositoryRoot ls-files --others --exclude-standard 2>$null

    return @($tracked) + @($untracked) | Where-Object { $_ }
}

if (-not $Path -or $Path.Count -eq 0) {
    $Path = Get-ChangedFile -RepositoryRoot $repositoryRoot
}

$changedApiFiles = @($Path) |
    Where-Object { $_ } |
    Where-Object { (Split-Path -Leaf $_) -in @('PublicAPI.Shipped.txt', 'PublicAPI.Unshipped.txt') } |
    ForEach-Object { $_ -replace '\\', '/' } |
    Select-Object -Unique

if ($changedApiFiles.Count -eq 0) { exit 0 }

Write-Output @"
The declared public API changed:

$(($changedApiFiles | ForEach-Object { "  $_" }) -join "`n")

Per CONTRIBUTING.md a public-surface change also requires:

  1. CHANGELOG.md  - entry under [Unreleased] / the next version, Keep-a-Changelog format.
                     Prefix breaking changes with BREAKING.
  2. README.md     - update the 'API summary' section and any affected examples.
  3. <Version>     - SemVer bump in src/Directory.Build.props (one edit for all six projects).

Review the diff line by line first - it is the guard that this change is deliberate, not accidental. An
entry starting with *REMOVED* is a break: it means a member that shipped is gone.
"@

exit 0
