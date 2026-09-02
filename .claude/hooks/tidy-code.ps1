# PostToolUse hook: format an edited C# file with CSharpier.
#
# The logic itself lives in scripts/tidy-code.ps1, so that Codex's hook and a human run exactly the same
# thing. This file is only the hook wiring: read the tool payload off stdin, pull the edited path out of
# it, and delegate.
#
# Formatting only, which is the default scope and takes under a second. Style and member ordering are
# not run here: `dotnet format style` needs MSBuild and ReSharper loads the whole solution, and neither
# belongs on the critical path of every single edit. All three are build errors, and scripts/preflight.ps1
# runs `-Scope all` before a commit, so nothing slips through.
#
# Never fails the edit - a formatter problem is surfaced as text and the hook still exits 0.

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $filePath = $payload.tool_input.file_path

    if ([String]::IsNullOrWhiteSpace($filePath)) { exit 0 }
    if ([System.IO.Path]::GetExtension($filePath) -ne '.cs') { exit 0 }
    if (-not (Test-Path -LiteralPath $filePath)) { exit 0 }

    $script = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'scripts/tidy-code.ps1'
    if (-not (Test-Path -LiteralPath $script)) {
        Write-Output "tidy-code hook: scripts/tidy-code.ps1 not found at $script"
        exit 0
    }

    $output = & $script -Path $filePath 2>&1 | Out-String

    # Quiet on success. On failure say so explicitly and say what it means, because the build treats
    # formatting as an error - the same contract Codex's adapter reports through additionalContext.
    if ($LASTEXITCODE -ne 0) {
        Write-Output "CSharpier failed on $filePath. The build treats formatting as an error, so fix this before building:`n$output"
    }
}
catch {
    Write-Output "tidy-code hook error: $($_.Exception.Message)"
}

exit 0
