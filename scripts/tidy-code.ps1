<#
.SYNOPSIS
    Applies this repository's C# style, formatting and member ordering.

.DESCRIPTION
    Three concerns, three tools, no overlap between them:

      formatting   whitespace, line breaks, wrapping     CSharpier
      style        var, =>, this., null checks, usings   Roslyn analyzers, via `dotnet format style`
      ordering     the order of types and their members  ReSharper, via `jb cleanupcode`

    They run in that reverse order - style, then ordering, then formatting - because each one leaves
    whitespace behind for the next. CSharpier is always last and always has the final say.

    The build enforces all three, in the tests and benchmarks as much as in the libraries:
    EnforceCodeStyleInBuild with TreatWarningsAsErrors makes a style slip or a misplaced member an
    error, and CSharpier.MsBuild does the same for an unformatted file. Running this first is much
    cheaper than finding out at build time.

    Needs the local tools: run `dotnet tool restore` once per clone.

.PARAMETER Path
    One or more .cs files. With no Path, every .cs file git reports as changed - tracked modifications
    plus untracked files. Ignored by -Scope all, which always covers the whole solution.

.PARAMETER Scope
    How much runs:

      format  ~1s     CSharpier only. The default, and what the editor hooks use.
      style   ~15s    + the Roslyn code-style fixers.
      all     ~3min   + member reordering. Whole solution only - ReSharper loads all of it either way.
                      This is the one to run before committing; scripts/preflight.ps1 does it for you.

.PARAMETER Check
    Report violations instead of fixing them, and exit non-zero if there are any. This is what CI runs.

    -Check -Scope all is the exception: it still WRITES. ReSharper has no check mode, and neither it nor
    CSharpier is idempotent alone - cleanupcode re-indents the content of raw string literals and
    CSharpier puts it back - so the only honest question is whether tidying the whole tree changes it.
    That is what -Check -Scope all asks: it tidies for real, then compares. Do not point it at a working
    tree you are not ready to have tidied.

.EXAMPLE
    pwsh -File scripts/tidy-code.ps1

.EXAMPLE
    pwsh -File scripts/tidy-code.ps1 -Scope all

.EXAMPLE
    pwsh -File scripts/tidy-code.ps1 src/DbConnectionPlus/Entities/EntityHelper.cs
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [String[]] $Path,

    [ValidateSet('format', 'style', 'all')]
    [String] $Scope = 'format',

    [Switch] $Check
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up.
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot 'DbConnectionPlus.slnx'

function Get-ChangedCSharpFile {
    # 2>$null: git warns about CRLF normalization per file, which is noise here.
    $tracked = & git -C $repositoryRoot diff --name-only HEAD -- '*.cs' 2>$null
    $untracked = & git -C $repositoryRoot ls-files --others --exclude-standard -- '*.cs' 2>$null

    return @($tracked) + @($untracked) |
        Where-Object { $_ } |
        ForEach-Object { Join-Path $repositoryRoot $_ }
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

function Resolve-TargetFile {
    param([String[]] $Candidates)

    return @($Candidates) |
        Where-Object { $_ } |
        Where-Object { [System.IO.Path]::GetExtension($_) -eq '.cs' } |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        ForEach-Object { (Resolve-Path -LiteralPath $_).Path } |
        # Generated and build output are not ours to touch.
        Where-Object { $_ -notmatch '[\\/](bin|obj)[\\/]' } |
        Select-Object -Unique
}

$failures = New-Object System.Collections.Generic.List[String]

function Invoke-Tool {
    <#
        Runs `dotnet ...` and returns everything it printed. The caller decides success from $LASTEXITCODE.

        $ErrorActionPreference is deliberately relaxed for the call. With it at 'Stop', PowerShell turns
        anything a native program writes to stderr into a terminating error, so a harmless warning aborts
        the whole script with NativeCommandError. ReSharper prints one every run:

            Warning: Roslyn Source Generator error from DapperInterceptorGenerator from Dapper.AOT
            handled 1 of 1 possible call-sites ...

        That is a notice, not a failure - cleanupcode still exits 0 - but it was enough to kill the script.
        Exit codes decide success here, not stderr.
    #>
    param([Parameter(Mandatory)] [String[]] $Arguments)

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { return (& dotnet @Arguments 2>&1 | Out-String) }
    finally { $ErrorActionPreference = $previous }
}

# --- Style ----------------------------------------------------------------------------------------
# The IDExxxx code-style rules, and the third-party analyzer fixers that have one. Whitespace rules are
# deliberately not included: IDE0055 is off in .editorconfig, because whitespace belongs to CSharpier.

function Invoke-StyleFix {
    param([String[]] $Files, [Boolean] $VerifyOnly)

    $verify = if ($VerifyOnly) { @('--verify-no-changes') } else { @() }

    if (-not $Files) {
        $arguments = @('format', 'style', $solution, '--no-restore', '-v', 'q') + $verify
        $output = Invoke-Tool -Arguments $arguments
        if ($LASTEXITCODE -ne 0) { $failures.Add("dotnet format style:`n$output") }
        return
    }

    # tests/package-consumption/ is out of reach for this step, and the failure would be confusing rather
    # than useful: those projects restore from the PACKED packages, so `dotnet format style` on one cannot
    # even load it until `dotnet pack` has run. CSharpier still formats them - it needs no project - and the
    # style rules there are on the author. See the code-style reference.
    $consumers = [System.IO.Path]::Combine($repositoryRoot, 'tests', 'package-consumption')
    $skipped = @($Files) | Where-Object { $_.StartsWith($consumers, [StringComparison]::OrdinalIgnoreCase) }
    if ($skipped) {
        Write-Output "tidy-code: $($skipped.Count) file(s) under tests/package-consumption - no style pass, see AGENTS.md."
    }

    $Files = @($Files) | Where-Object { -not $_.StartsWith($consumers, [StringComparison]::OrdinalIgnoreCase) }
    if (-not $Files) { return }

    # One invocation per owning project, so MSBuild loads one project rather than the whole solution.
    $Files | Group-Object { Get-OwningProject -FilePath $_ } | ForEach-Object {
        $project = $_.Name
        if ([String]::IsNullOrWhiteSpace($project) -or -not (Test-Path -LiteralPath $project)) {
            Write-Output "tidy-code: no owning .csproj for $($_.Group -join ', ') - skipped."
            return
        }

        $projectDirectory = Split-Path -Parent $project

        # `--include` matches RELATIVE paths only. Handed an absolute path it matches nothing, reports
        # success and formats nothing - a silent no-op that looks exactly like a clean file. So run from
        # the project directory and pass each file relative to it.
        #
        # The PROJECT has to be relative too, and that is the half that is easy to miss. Given an absolute
        # project path, `dotnet format` reports "Formatted 0 of 0 files" and exits 0 whatever --include
        # says, so this whole step was doing nothing at all until both halves were relative. Neither
        # failure is visible without -v d.
        $relativePaths = $_.Group | ForEach-Object { [System.IO.Path]::GetRelativePath($projectDirectory, $_) }
        $projectFileName = Split-Path -Leaf $project

        $arguments =
            @('format', 'style', $projectFileName, '--include') +
            $relativePaths +
            @('--no-restore', '-v', 'q') +
            $verify

        Push-Location -LiteralPath $projectDirectory
        try { $output = Invoke-Tool -Arguments $arguments }
        finally { Pop-Location }

        if ($LASTEXITCODE -ne 0) {
            $failures.Add("dotnet format style ($(Split-Path -Leaf $project)):`n$output")
        }
    }
}

# --- Ordering -------------------------------------------------------------------------------------
# ReSharper is the only tool that can reorder C# members. StyleCop reports a wrong order but cannot fix
# one: its ElementOrderCodeFixProvider is marked [NoCodeFix] and never registered.
#
# The order itself is the file layout in DbConnectionPlus.slnx.DotSettings, and the ReorderMembers
# profile in the same file enables member reordering and nothing else - no reformatting, because that
# is CSharpier's job.

function Get-CSharpFingerprint {
    # A hash over the content of every .cs file the tools actually touch, used by -Check to tell whether
    # tidying changed anything.
    #
    # The file list comes from git, not from Get-ChildItem. Walking the directory tree finds 391 .cs
    # files where the tools see 283: build leftovers and the package consumers' NuGet cache under
    # tests/package-consumption/.packages/ are on disk but ignored, and CSharpier honours .gitignore.
    # Hashing those makes the check fail on files nothing tidied.
    #
    # `git diff` is not used either: a human running -Check usually has a dirty tree, and their own
    # edits are not an ordering violation. Comparing before against after answers the actual question.
    $relativePaths = & git -C $repositoryRoot ls-files --cached --others --exclude-standard -- '*.cs' 2>$null |
        Where-Object { $_ } |
        Sort-Object

    # Fail loudly on an empty list rather than hashing nothing. git's stderr goes to $null just above, so a
    # git that fails here returns no paths instead of an error - and two hashes of an empty stream compare
    # equal, which would make -Check report a tidy tree without having looked at a single file. This is CI's
    # only formatting gate; it must not be able to pass by accident.
    if (-not $relativePaths) {
        throw 'tidy-code: git listed no .cs files. Is this a git repository, and is git on PATH?'
    }

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $accumulator = New-Object System.IO.MemoryStream
        foreach ($relativePath in $relativePaths) {
            $fullPath = Join-Path $repositoryRoot $relativePath
            if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { continue }

            # The path goes into the hash too, so that adding or removing a file is a change.
            $pathBytes = [System.Text.Encoding]::UTF8.GetBytes($relativePath)
            $accumulator.Write($pathBytes, 0, $pathBytes.Length)

            $bytes = [System.IO.File]::ReadAllBytes($fullPath)
            $accumulator.Write($bytes, 0, $bytes.Length)
        }

        return [System.BitConverter]::ToString($sha.ComputeHash($accumulator.ToArray()))
    }
    finally { $sha.Dispose() }
}

function Invoke-ReorderMembers {
    $output = Invoke-Tool -Arguments @('jb', 'cleanupcode', $solution, '--profile=ReorderMembers', '--no-build')
    if ($LASTEXITCODE -ne 0) { $failures.Add("jb cleanupcode:`n$output") }
}

# --- Formatting -----------------------------------------------------------------------------------
# Always last: both steps above move code around and leave whitespace that is not CSharpier's.

function Invoke-Format {
    param([String[]] $Files, [Boolean] $VerifyOnly)

    # @(...) around the whole thing on purpose: an `if` writes its result to the pipeline, which
    # enumerates a one-element array back down to a bare string. Splatting that passes "C" as the path.
    $target = @(if ($Files) { $Files } else { $repositoryRoot })
    $command = if ($VerifyOnly) { 'check' } else { 'format' }

    $output = Invoke-Tool -Arguments (@('csharpier', $command) + $target)
    if ($LASTEXITCODE -ne 0) { $failures.Add("csharpier $command`:`n$output") }
}

# --- Run ------------------------------------------------------------------------------------------

if ($Scope -eq 'all') {
    if ($Path) { Write-Output 'tidy-code: -Scope all covers the whole solution; the paths given are ignored.' }

    # -Check does not reach the individual tools here, because two of them are not idempotent on their
    # own: cleanupcode re-indents the content of raw string literals and CSharpier puts it back. Asking
    # cleanupcode alone whether it changed anything therefore always says yes. So the whole pipeline
    # runs for real and the question is asked once, of the tree: did tidying it change anything?
    $before = if ($Check) { Get-CSharpFingerprint } else { $null }

    Invoke-StyleFix -Files @() -VerifyOnly $false
    Invoke-ReorderMembers
    Invoke-Format -Files @() -VerifyOnly $false

    if ($Check -and -not $failures.Count -and (Get-CSharpFingerprint) -ne $before) {
        $failures.Add('the tree is not tidy. Run: pwsh -File scripts/tidy-code.ps1 -Scope all')
    }

    if (-not $failures.Count) {
        Write-Output "tidy-code: solution $(if ($Check) { 'checked' } else { 'tidied' })."
    }
}
else {
    if (-not $Path) { $Path = Get-ChangedCSharpFile }

    $files = Resolve-TargetFile -Candidates $Path
    if (-not $files) {
        Write-Output 'tidy-code: nothing to do.'
        exit 0
    }

    if ($Scope -eq 'style') { Invoke-StyleFix -Files $files -VerifyOnly $Check.IsPresent }
    Invoke-Format -Files $files -VerifyOnly $Check.IsPresent

    if (-not $failures.Count) {
        Write-Output "tidy-code: $($files.Count) file(s) $(if ($Check) { 'checked' } else { 'tidied' })."
    }
}

if ($failures.Count) {
    $failures | ForEach-Object { Write-Output "tidy-code: $_" }
    exit 1
}

exit 0
