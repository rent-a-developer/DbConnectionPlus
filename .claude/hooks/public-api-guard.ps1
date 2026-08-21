# PostToolUse hook: remind about the companion edits a public API change needs.
#
# The checklist itself lives in scripts/public-api-guard.ps1, so that Codex's hook and preflight
# produce exactly the same text. This file is only the hook wiring: read the tool payload off
# stdin, pull the edited path out of it, and delegate.
#
# Never fails the edit - a problem here is surfaced as text and the hook still exits 0.

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $filePath = $payload.tool_input.file_path

    if ([String]::IsNullOrWhiteSpace($filePath)) { exit 0 }

    $script = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'scripts/public-api-guard.ps1'
    if (-not (Test-Path -LiteralPath $script)) {
        Write-Output "public-api-guard hook: scripts/public-api-guard.ps1 not found at $script"
        exit 0
    }

    & $script -Path $filePath
}
catch {
    Write-Output "public-api-guard hook error: $($_.Exception.Message)"
}

exit 0
