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

    Needs the local tools: run `dotnet tool restore` once per clone. This script never installs them.

.PARAMETER Path
    One or more .cs files. With no Path, every .cs file git reports as changed - tracked modifications
    plus untracked files. Ignored by -Scope all, which always covers the whole solution.

    A path given explicitly must exist and must be a .cs file. It is an error if it does not, rather than
    a silent skip: a typo that formats nothing looks exactly like a file that needed nothing.

.PARAMETER Scope
    How much runs:

      format  ~1s     CSharpier only. The default, and what the editor hooks use.
      style   ~15s    + the Roslyn code-style fixers.
      all     ~3min   + member reordering. Whole solution only - ReSharper loads all of it either way.
                      This is the one to run before committing; scripts/preflight.ps1 does it for you.

.PARAMETER Check
    Report violations instead of fixing them, and exit non-zero if there are any. This is what CI runs.

    -Check NEVER writes to this working tree, at any scope, and never touches the git index. At the
    format and style scopes the tools have a verify mode of their own. At the `all` scope they do not -
    ReSharper has no check mode, and neither it nor CSharpier is idempotent alone - so the pipeline runs
    for real on a DISPOSABLE COPY of the current tree, outside the repository, and the diff it produced
    there is printed here. Nothing is ever copied back.

.EXAMPLE
    pwsh -File scripts/tidy-code.ps1

.EXAMPLE
    pwsh -File scripts/tidy-code.ps1 -Scope all

.EXAMPLE
    pwsh -File scripts/tidy-code.ps1 src/DbConnectionPlus/Entities/EntityHelper.cs
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [String[]] $Path,

    [ValidateSet('format', 'style', 'all')]
    [String] $Scope = 'format',

    [Switch] $Check
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up. Every path below is anchored to this, so the
# script behaves the same whatever the current directory is.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$solutionFileName = 'DbConnectionPlus.slnx'

$failures = New-Object System.Collections.Generic.List[String]

# --- Running things -------------------------------------------------------------------------------

function Invoke-Git
{
    <#
        Runs git and returns its output lines. Arguments are passed as an ARRAY, never as a command
        string, so a path with a space or a quote in it cannot become two arguments or a shell fragment.
    #>
    param(
        [Parameter(Mandatory)] [String] $WorkingDirectory,
        [Parameter(Mandatory)] [String[]] $Arguments,
        [Switch] $AllowFailure
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try
    {
        # 2>$null: git warns about CRLF normalization per file, which is noise here.
        $output = & git -C $WorkingDirectory @Arguments 2>$null
    }
    finally
    {
        $ErrorActionPreference = $previous
    }

    if ($LASTEXITCODE -ne 0 -and -not $AllowFailure)
    {
        throw "tidy-code: git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }

    return @($output)
}

function Invoke-Tool
{
    <#
        Runs `dotnet ...` from $WorkingDirectory and returns everything it printed. The caller decides
        success from $LASTEXITCODE, which is checked immediately after the call.

        The working directory matters and is not cosmetic: `dotnet` resolves global.json from the current
        directory upward, and this repository's global.json is what pins the SDK and selects the
        Microsoft.Testing.Platform runner. Run from somewhere else and a different SDK answers.

        $ErrorActionPreference is deliberately relaxed for the call. With it at 'Stop', PowerShell turns
        anything a native program writes to stderr into a terminating error, so a harmless warning aborts
        the whole script with NativeCommandError. ReSharper prints one every run:

            Warning: Roslyn Source Generator error from DapperInterceptorGenerator from Dapper.AOT
            handled 1 of 1 possible call-sites ...

        That is a notice, not a failure - cleanupcode still exits 0 - but it was enough to kill the script.
        Exit codes decide success here, not stderr.
    #>
    param(
        [Parameter(Mandatory)] [String[]] $Arguments,
        [Parameter(Mandatory)] [String] $WorkingDirectory
    )

    $previous = $ErrorActionPreference
    Push-Location -LiteralPath $WorkingDirectory
    try
    {
        $ErrorActionPreference = 'Continue'

        return (& dotnet @Arguments 2>&1 | Out-String)
    }
    finally
    {
        $ErrorActionPreference = $previous
        Pop-Location
    }
}

function Assert-Prerequisite
{
    <#
        A missing prerequisite is an explicit failure, never a silent skip. This script does not install
        anything: `dotnet tool restore` is a deliberate act, and a formatting hook that installs software
        behind your back is a worse problem than an unformatted file.

        The tool manifest is READ rather than `dotnet tool list` being run. This function is on the critical
        path of every editor hook, which formats a single file in about a second; spawning a dotnet process
        just to be told what the manifest already says would roughly double that. A tool that is declared but
        not restored is caught by the invocation that needs it, and Add-RestoreHint says what to do about it.
    #>
    param([Parameter(Mandatory)] [String] $Root)

    foreach ($executable in @('git', 'dotnet'))
    {
        if (-not (Get-Command -Name $executable -CommandType Application -ErrorAction SilentlyContinue))
        {
            throw "tidy-code: $executable is not on PATH."
        }
    }

    $manifestPath = Join-Path $Root '.config/dotnet-tools.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf))
    {
        throw "tidy-code: $manifestPath does not exist. The formatter and the reordering tool are declared there."
    }

    try
    {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    }
    catch
    {
        throw "tidy-code: $manifestPath is not valid JSON: $($_.Exception.Message)"
    }

    $declared = @($manifest.tools.PSObject.Properties.Name)

    foreach ($tool in @('csharpier', 'jetbrains.resharper.globaltools'))
    {
        if ($tool -notin $declared)
        {
            throw "tidy-code: '$tool' is not declared in $manifestPath."
        }
    }
}

function Add-RestoreHint
{
    <#
        A tool that is declared in the manifest but not restored fails with a message about the command not
        being found, which reads like a bug in this script rather than a missing `dotnet tool restore`.
    #>
    param([Parameter(Mandatory)] [String] $Output)

    if ($Output -match 'was not found|could not be found|is not recognized')
    {
        $hint = 'Run "dotnet tool restore" once per clone - this script never installs tools.'

        return "$Output`n$hint"
    }

    return $Output
}

# --- Selecting files ------------------------------------------------------------------------------

function Get-ChangedCSharpFile
{
    param([Parameter(Mandatory)] [String] $Root)

    $tracked = Invoke-Git -WorkingDirectory $Root -Arguments @('diff', '--name-only', 'HEAD', '--', '*.cs')
    $untracked = Invoke-Git -WorkingDirectory $Root -Arguments @('ls-files', '--others', '--exclude-standard', '--', '*.cs')

    return @($tracked) + @($untracked) |
        Where-Object { $_ } |
        ForEach-Object { Join-Path $Root $_ } |
        # A file that git reports as changed can be one that was DELETED. Nothing to format there.
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }
}

function Get-OwningProject
{
    param([Parameter(Mandatory)] [String] $FilePath)

    $directory = Split-Path -Parent $FilePath
    while ($directory)
    {
        $candidate = Get-ChildItem -LiteralPath $directory -Filter '*.csproj' -File -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($candidate) { return $candidate.FullName }
        $directory = Split-Path -Parent $directory
    }

    return $null
}

function Resolve-ExplicitFile
{
    <#
        Paths the caller typed. Every one of them has to be a .cs file that exists - a typo must fail
        rather than quietly format nothing.
    #>
    param([Parameter(Mandatory)] [String[]] $Candidates)

    $resolved = New-Object System.Collections.Generic.List[String]

    foreach ($candidate in $Candidates)
    {
        if (-not $candidate) { continue }

        if ([System.IO.Path]::GetExtension($candidate) -ne '.cs')
        {
            throw "tidy-code: '$candidate' is not a .cs file."
        }

        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf))
        {
            throw "tidy-code: '$candidate' does not exist."
        }

        $full = (Resolve-Path -LiteralPath $candidate).Path

        # Generated and build output are not ours to touch.
        if ($full -match '[\\/](bin|obj)[\\/]')
        {
            throw "tidy-code: '$candidate' is build output. Nothing under bin/ or obj/ is formatted."
        }

        if (-not $resolved.Contains($full)) { $resolved.Add($full) }
    }

    return $resolved.ToArray()
}

function Resolve-DerivedFile
{
    <#
        Paths this script worked out for itself, from git. Anything unsuitable is dropped rather than
        reported: git listing a deleted or generated file is normal, not a mistake the caller made.
    #>
    param([String[]] $Candidates)

    return @($Candidates) |
        Where-Object { $_ } |
        Where-Object { [System.IO.Path]::GetExtension($_) -eq '.cs' } |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        ForEach-Object { (Resolve-Path -LiteralPath $_).Path } |
        Where-Object { $_ -notmatch '[\\/](bin|obj)[\\/]' } |
        Select-Object -Unique
}

# --- Style ----------------------------------------------------------------------------------------
# The IDExxxx code-style rules, and the third-party analyzer fixers that have one. Whitespace rules are
# deliberately not included: IDE0055 is off in .editorconfig, because whitespace belongs to CSharpier.

function Invoke-StyleFix
{
    <#
        `--no-restore` is deliberately NOT passed, and that is not a performance oversight.

        `dotnet format style` fixes IDE0005 - "unnecessary using directive" - by DELETING the directive. It
        decides what is unnecessary from the compilation, and on an unrestored project the compilation has no
        package references at all, so every using of a type from a package looks unnecessary. Measured on a
        fresh tree: it removed `using Humanizer;` and `using LinkDotNet.StringBuilder;` from the shipping
        library and left source that does not compile. It reported success while doing it.

        A restore is a second or two on a warm cache and is a no-op when the tree is already restored. That is
        the entire cost of the guarantee that this step cannot delete code it only thinks is unused.
    #>
    param(
        [String[]] $Files,
        [Boolean] $VerifyOnly,
        [Parameter(Mandatory)] [String] $Root
    )

    $verify = if ($VerifyOnly) { @('--verify-no-changes') } else { @() }

    if (-not $Files)
    {
        $arguments = @('format', 'style', $solutionFileName) + @('-v', 'q') + $verify
        $output = Invoke-Tool -Arguments $arguments -WorkingDirectory $Root
        if ($LASTEXITCODE -ne 0) { $failures.Add("dotnet format style:`n$output") }

        return
    }

    # tests/package-consumption/ is out of reach for this step, and the failure would be confusing rather
    # than useful: those projects restore from the PACKED packages, so `dotnet format style` on one cannot
    # even load it until `dotnet pack` has run. CSharpier still formats them - it needs no project - and the
    # style rules there are on the author. See the code-style reference.
    $consumers = [System.IO.Path]::Combine($Root, 'tests', 'package-consumption')
    $skipped = @($Files) | Where-Object { $_.StartsWith($consumers, [StringComparison]::OrdinalIgnoreCase) }
    if ($skipped)
    {
        Write-Output "tidy-code: $($skipped.Count) file(s) under tests/package-consumption - no style pass, see AGENTS.md."
    }

    $Files = @($Files) | Where-Object { -not $_.StartsWith($consumers, [StringComparison]::OrdinalIgnoreCase) }
    if (-not $Files) { return }

    # One invocation per owning project, so MSBuild loads one project rather than the whole solution.
    $Files | Group-Object { Get-OwningProject -FilePath $_ } | ForEach-Object {
        $project = $_.Name
        if ([String]::IsNullOrWhiteSpace($project) -or -not (Test-Path -LiteralPath $project))
        {
            $failures.Add("no owning .csproj for $($_.Group -join ', ')")

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
            @('-v', 'q') +
            $verify

        $output = Invoke-Tool -Arguments $arguments -WorkingDirectory $projectDirectory

        if ($LASTEXITCODE -ne 0)
        {
            $failures.Add("dotnet format style ($projectFileName):`n$output")
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

function Invoke-ReorderMembers
{
    param([Parameter(Mandatory)] [String] $Root)

    $output = Invoke-Tool `
        -Arguments @('jb', 'cleanupcode', $solutionFileName, '--profile=ReorderMembers', '--no-build') `
        -WorkingDirectory $Root
    if ($LASTEXITCODE -ne 0) { $failures.Add("jb cleanupcode:`n$(Add-RestoreHint -Output $output)") }
}

# --- Formatting -----------------------------------------------------------------------------------
# Always last: both steps above move code around and leave whitespace that is not CSharpier's.

function Invoke-Format
{
    param(
        [String[]] $Files,
        [Boolean] $VerifyOnly,
        [Parameter(Mandatory)] [String] $Root
    )

    # @(...) around the whole thing on purpose: an `if` writes its result to the pipeline, which
    # enumerates a one-element array back down to a bare string. Splatting that passes "C" as the path.
    $target = @(if ($Files) { $Files } else { $Root })
    $command = if ($VerifyOnly) { 'check' } else { 'format' }

    $output = Invoke-Tool -Arguments (@('csharpier', $command) + $target) -WorkingDirectory $Root
    if ($LASTEXITCODE -ne 0) { $failures.Add("csharpier $command`:`n$(Add-RestoreHint -Output $output)") }
}

# --- The disposable copy --------------------------------------------------------------------------
# What -Check -Scope all runs on. It is a copy of the CURRENT tree - tracked content as it stands on
# disk, including your uncommitted edits, plus the untracked files git does not ignore - placed outside
# the repository so that nothing the tools do can reach the original.
#
# It is a git repository of its own, with one commit, because the tools want one: CSharpier reads
# .gitignore to decide what to skip, and the commit is what makes `git diff` inside the copy state the
# proposed change exactly. Staging and committing THERE is not the same act as staging in your
# repository, which this script never does.

function New-DisposableTreeCopy
{
    param([Parameter(Mandatory)] [String] $Root)

    $temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "dbconnectionplus-tidy-$([Guid]::NewGuid())"
    New-Item -ItemType Directory -Path $temporaryDirectory -Force | Out-Null

    # Tracked content plus untracked-but-not-ignored files: exactly the files a commit from here could
    # contain. Caches and build output are ignored, so they are excluded by construction rather than by a
    # list this script would have to keep in step.
    $relativePaths = Invoke-Git -WorkingDirectory $Root -Arguments @('ls-files', '--cached', '--others', '--exclude-standard') |
        Where-Object { $_ } |
        Sort-Object -Unique

    if (-not $relativePaths)
    {
        throw 'tidy-code: git listed no files. Is this a git repository, and is git on PATH?'
    }

    $copied = 0
    foreach ($relativePath in $relativePaths)
    {
        $source = Join-Path $Root $relativePath

        # `--cached` lists a file that is tracked but deleted in the working tree. There is nothing to copy.
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { continue }

        $destination = Join-Path $temporaryDirectory $relativePath
        $destinationDirectory = Split-Path -Parent $destination
        if (-not (Test-Path -LiteralPath $destinationDirectory))
        {
            New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        }

        Copy-Item -LiteralPath $source -Destination $destination -Force
        $copied++
    }

    if (-not (Get-ChildItem -LiteralPath $temporaryDirectory -Recurse -File -Filter '*.cs' | Select-Object -First 1))
    {
        throw 'tidy-code: the copied tree contains no .cs file, so a check of it would prove nothing.'
    }

    # The identity is supplied per command rather than written into a config, and it never touches the
    # original repository: -c applies to this invocation only, and the invocation runs in the copy.
    $identity = @(
        '-c', 'user.name=tidy-code',
        '-c', 'user.email=tidy-code@localhost',
        '-c', 'commit.gpgsign=false'
    )

    Invoke-Git -WorkingDirectory $temporaryDirectory -Arguments @('init', '--quiet') | Out-Null
    Invoke-Git -WorkingDirectory $temporaryDirectory -Arguments (@('add', '--all')) | Out-Null
    Invoke-Git -WorkingDirectory $temporaryDirectory -Arguments ($identity + @('commit', '--quiet', '-m', 'tidy-code baseline')) | Out-Null

    # Write-Host, not Write-Output: anything written to the pipeline here would be returned to the caller
    # alongside the path, and the caller wants one string.
    Write-Host "tidy-code: checking a disposable copy of $copied file(s) in $temporaryDirectory"

    return $temporaryDirectory
}

function Remove-DisposableTreeCopy
{
    param([Parameter(Mandatory)] [String] $TemporaryDirectory)

    # Only ever the directory this script created, identified by the prefix it created it with. A path
    # that does not look like one is left alone rather than deleted on the strength of a variable.
    $expectedPrefix = Join-Path ([System.IO.Path]::GetTempPath()) 'dbconnectionplus-tidy-'

    if (-not $TemporaryDirectory.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase))
    {
        Write-Output "tidy-code: refusing to delete '$TemporaryDirectory' - it is not a directory this script created."

        return
    }

    if (Test-Path -LiteralPath $TemporaryDirectory)
    {
        Remove-Item -LiteralPath $TemporaryDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-CheckOnCopy
{
    param([Parameter(Mandatory)] [String] $Root)

    $copy = New-DisposableTreeCopy -Root $Root
    try
    {
        # The copy has no obj/, so nothing in it can load until it is restored. Restoring here rather than
        # leaving it to the first tool that needs it turns "the copy could not restore" into its own, legible
        # failure instead of a confusing formatter error.
        $output = Invoke-Tool -Arguments @('restore', $solutionFileName) -WorkingDirectory $copy
        if ($LASTEXITCODE -ne 0)
        {
            $failures.Add("dotnet restore (in the disposable copy):`n$output")

            return
        }

        Invoke-StyleFix -Files @() -VerifyOnly $false -Root $copy
        if ($failures.Count) { return }

        Invoke-ReorderMembers -Root $copy
        if ($failures.Count) { return }

        Invoke-Format -Files @() -VerifyOnly $false -Root $copy
        if ($failures.Count) { return }

        $changed = Invoke-Git -WorkingDirectory $copy -Arguments @('diff', '--name-only')
        if (-not $changed)
        {
            Write-Output 'tidy-code: solution checked - the tree is tidy.'

            return
        }

        $statistics = Invoke-Git -WorkingDirectory $copy -Arguments @('diff', '--stat')
        $diff = Invoke-Git -WorkingDirectory $copy -Arguments @('--no-pager', 'diff')

        $failures.Add(
            "the tree is not tidy. $($changed.Count) file(s) would change:`n" +
            "$($statistics -join "`n")`n`n" +
            "$($diff -join "`n")`n`n" +
            'Apply it with: pwsh -File scripts/tidy-code.ps1 -Scope all'
        )
    }
    finally
    {
        Remove-DisposableTreeCopy -TemporaryDirectory $copy
    }
}

# --- Run ------------------------------------------------------------------------------------------

Assert-Prerequisite -Root $repositoryRoot

if ($Scope -eq 'all')
{
    if ($Path) { Write-Output 'tidy-code: -Scope all covers the whole solution; the paths given are ignored.' }

    if ($Check)
    {
        Invoke-CheckOnCopy -Root $repositoryRoot
    }
    else
    {
        Invoke-StyleFix -Files @() -VerifyOnly $false -Root $repositoryRoot
        Invoke-ReorderMembers -Root $repositoryRoot
        Invoke-Format -Files @() -VerifyOnly $false -Root $repositoryRoot

        if (-not $failures.Count) { Write-Output 'tidy-code: solution tidied.' }
    }
}
else
{
    if ($Path)
    {
        $files = Resolve-ExplicitFile -Candidates $Path
    }
    else
    {
        $files = Resolve-DerivedFile -Candidates (Get-ChangedCSharpFile -Root $repositoryRoot)
    }

    if (-not $files)
    {
        Write-Output 'tidy-code: nothing to do.'

        exit 0
    }

    # Both tools have a verify mode that reads and reports without writing, so -Check needs no copy here.
    if ($Scope -eq 'style') { Invoke-StyleFix -Files $files -VerifyOnly $Check.IsPresent -Root $repositoryRoot }
    Invoke-Format -Files $files -VerifyOnly $Check.IsPresent -Root $repositoryRoot

    if (-not $failures.Count)
    {
        Write-Output "tidy-code: $($files.Count) file(s) $(if ($Check) { 'checked' } else { 'tidied' })."
    }
}

if ($failures.Count)
{
    $failures | ForEach-Object { Write-Output "tidy-code: $_" }

    exit 1
}

exit 0
