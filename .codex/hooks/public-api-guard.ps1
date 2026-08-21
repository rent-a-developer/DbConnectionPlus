# Codex PostToolUse hook: remind about the companion edits a public API change needs.
#
# The checklist itself lives in scripts/public-api-guard.ps1, which Claude Code's hook runs too.
# This file is only the hook wiring.
#
# Called with no -Path, the shared script examines every file git reports as changed. That is what this hook
# needs: for a file edit Codex reports tool_name "apply_patch" and puts the patch text in tool_input.command,
# not a file path.
#
# Contract (https://learn.chatgpt.com/docs/hooks): exit 0 and write the response JSON to stdout. The checklist
# is returned as additionalContext, so it reaches the model rather than only the user.

$ErrorActionPreference = 'Stop'

function Write-HookResult {
    param([String] $AdditionalContext)

    if ([String]::IsNullOrWhiteSpace($AdditionalContext)) {
        $result = @{ continue = $true; suppressOutput = $true }
    }
    else {
        $result = @{
            continue = $true
            hookSpecificOutput = @{
                hookEventName = 'PostToolUse'
                additionalContext = $AdditionalContext
            }
        }
    }

    $result | ConvertTo-Json -Depth 5 -Compress | Write-Output
}

try {
    # The payload is read and discarded: draining stdin keeps Codex from blocking on the pipe.
    [Console]::In.ReadToEnd() | Out-Null

    $repositoryRoot = & git rev-parse --show-toplevel 2>$null
    if ([String]::IsNullOrWhiteSpace($repositoryRoot)) {
        $repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    }

    $script = Join-Path $repositoryRoot 'scripts/public-api-guard.ps1'
    if (-not (Test-Path -LiteralPath $script)) {
        Write-HookResult -AdditionalContext "public-api-guard hook: scripts/public-api-guard.ps1 not found at $script"
        exit 0
    }

    $output = & pwsh -NoProfile -NonInteractive -File $script 2>&1 | Out-String

    Write-HookResult -AdditionalContext $output.Trim()
}
catch {
    Write-HookResult -AdditionalContext "public-api-guard hook error: $($_.Exception.Message)"
}

exit 0
