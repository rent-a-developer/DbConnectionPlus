<#
.SYNOPSIS
    Runs the BenchmarkDotNet suite, which measures every category on the JIT and as a Native AOT compiled binary.

.DESCRIPTION
    The benchmark project defines two jobs. The JIT job runs the benchmark assembly the ordinary way. The AOT job
    has BenchmarkDotNet publish that same assembly with PublishAot=true and runs the native binary, so
    RuntimeFeature.IsDynamicCodeSupported is false in it and DbConnectionPlus takes its reflection based
    materializer path instead of the expression compiled one. That branch is the reason the second job exists.

    This script only sets up the environment the AOT job needs and forwards everything else to BenchmarkDotNet, so
    all the usual switches work:

        pwsh -File scripts/benchmarks.ps1 --filter *Query_Entities*
        pwsh -File scripts/benchmarks.ps1 --list flat

.PARAMETER BenchmarkDotNetArguments
    Arguments forwarded verbatim to BenchmarkDotNet, e.g. --filter or --list.

    This is the only parameter, deliberately. A second declared parameter would take part in positional binding and
    silently swallow one of these values - a bare "*Query_Scalars*" ended up as the build configuration that way,
    and MSBuild then tried to create an "obj\*Query_Scalars*" directory. The configuration is fixed to Release,
    which is the only one BenchmarkDotNet will run anyway.

.NOTES
    The AOT job needs a C++ toolchain: MSVC on Windows, clang and zlib1g-dev on Linux. On Windows vswhere.exe must
    be resolvable or the native link step fails with a misleading MSB3073 - this script puts the Visual Studio
    Installer directory on PATH for that reason, the same way scripts/verify-package-aot.ps1 does.

    A full run publishes the benchmark assembly natively once per benchmark case in the AOT job, so it takes
    considerably longer than the JIT suite alone. Use --filter while iterating.

.EXAMPLE
    pwsh -File scripts/benchmarks.ps1

.EXAMPLE
    pwsh -File scripts/benchmarks.ps1 --filter *Query_Entities*
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [String[]] $BenchmarkDotNetArguments = @()
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot 'benchmarks/DbConnectionPlus.Benchmarks/DbConnectionPlus.Benchmarks.csproj'

if ($IsWindows)
{
    # Without vswhere.exe on PATH the native link step fails with MSB3073 and a misleading error message.
    $visualStudioInstaller = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer'

    if ((Test-Path $visualStudioInstaller) -and ($env:PATH -notlike "*$visualStudioInstaller*"))
    {
        $env:PATH = "$visualStudioInstaller;$env:PATH"
    }
}

Write-Host 'Running the benchmarks (JIT and Native AOT)...' -ForegroundColor Cyan
Write-Host ''

# The "--" separator is part of the argument array rather than a literal token in the invocation, because
# PowerShell consumes a bare "--" as its own end-of-parameters marker and everything after it would then be handed
# to "dotnet run" instead of to BenchmarkDotNet. That turned a second --filter value into an output path.
$arguments = @('run', '--project', $project, '--configuration', 'Release', '--') + $BenchmarkDotNetArguments

& dotnet @arguments

$exitCode = $LASTEXITCODE

if ($exitCode -ne 0)
{
    Write-Host ''
    Write-Host "FAILED. The benchmark run exited with code $exitCode." -ForegroundColor Red

    exit $exitCode
}

exit 0
