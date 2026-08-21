# PostToolUse hook: format an edited C# file against .editorconfig.
#
# The formatting logic itself lives in scripts/format-cs.ps1, so that Codex's hook and a human run
# exactly the same thing. This file is only the hook wiring: read the tool payload off stdin, pull the
# edited path out of it, and delegate.
#
# Never fails the edit - a formatter problem is surfaced as text and the hook still exits 0.

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $filePath = $payload.tool_input.file_path

    if ([String]::IsNullOrWhiteSpace($filePath)) { exit 0 }
    if ([System.IO.Path]::GetExtension($filePath) -ne '.cs') { exit 0 }
    if (-not (Test-Path -LiteralPath $filePath)) { exit 0 }

    $script = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'scripts/format-cs.ps1'
    if (-not (Test-Path -LiteralPath $script)) {
        Write-Output "format-cs hook: scripts/format-cs.ps1 not found at $script"
        exit 0
    }

    & $script -Path $filePath
}
catch {
    Write-Output "format-cs hook error: $($_.Exception.Message)"
}

exit 0
