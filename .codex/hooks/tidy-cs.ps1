# Codex PostToolUse hook: format the C# files an edit just touched, with CSharpier.
#
# The logic itself lives in scripts/tidy-cs.ps1, which Claude Code's hook runs too. This file is only
# the hook wiring.
#
# Why it does not read a path out of the payload: for a file edit Codex reports tool_name "apply_patch" and
# puts the patch text in tool_input.command, not a file path. The script with no arguments formats
# every .cs file git reports as changed, which covers the edit that just happened and costs nothing when
# there is none.
#
# Formatting only, which is the default scope. Style and member ordering are build errors and
# scripts/preflight.ps1 runs `-Scope all` before a commit; neither belongs on the critical path of
# every edit.
#
# Contract (https://learn.chatgpt.com/docs/hooks): exit 0 and write the response JSON to stdout. Exit code 2
# would block the operation - this hook never does that, because a formatter problem must not stop an edit.

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

    $script = Join-Path $repositoryRoot 'scripts/tidy-cs.ps1'
    if (-not (Test-Path -LiteralPath $script)) {
        Write-HookResult -AdditionalContext "tidy-cs hook: scripts/tidy-cs.ps1 not found at $script"
        exit 0
    }

    $output = & pwsh -NoProfile -NonInteractive -File $script 2>&1 | Out-String

    # Quiet on success: the agent does not need to be told that nothing needed formatting. A failure is
    # reported, because it means the next build breaks on a formatting rule.
    if ($LASTEXITCODE -ne 0) {
        Write-HookResult -AdditionalContext "CSharpier failed. The build treats formatting as an error, so fix this before building:`n$output"
    }
    else {
        Write-HookResult
    }
}
catch {
    Write-HookResult -AdditionalContext "tidy-cs hook error: $($_.Exception.Message)"
}

exit 0
