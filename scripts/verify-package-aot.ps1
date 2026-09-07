<#
.SYNOPSIS
    Publishes the Native AOT package consumer, gates its IL diagnostics, and runs the native binary.

.DESCRIPTION
    This is the only check in the repository that can catch silent trimming damage. Under trimming, a missing
    [DynamicallyAccessedMembers] annotation makes reflection return fewer members with NO error, so the
    library binds no columns and hands back default-valued entities - measured at 6 columns of real data in, 0
    bound, no exception. Nothing is trimmed on the JIT, which is why the unit and integration suites pass with a
    broken annotation chain. See the "Native AOT and Trimming" section of docs/DESIGN-DECISIONS.md.

    The consumer reaches the library through PackageReference, never through a project reference. That matters:
    the DAM annotations, the embedded ILLink.Descriptors.xml and the IsTrimmable assembly marker all have to
    survive packing, and a project-referenced test would pass even if packing dropped every one of them.

    Four things have to hold, and all four are gated here:

    1. The packages restore into a consumer that has nothing but a PackageReference.
    2. The Native AOT publish succeeds.
    3. NO IL2xxx/IL3xxx diagnostic is reported, from anywhere - not from the library, not from an adapter, not
       from a package in the closure, and not at the consumer's own call sites. The generic query methods
       carry neither [RequiresUnreferencedCode] nor [RequiresDynamicCode], so a consumer publishing with
       PublishAot or PublishTrimmed sees nothing for any supported scenario. Any diagnostic is a regression.
    4. The native binary runs and every assertion in it passes.

.PARAMETER Framework
    The target framework to publish. net8.0 is the documented AOT floor, net10.0 the recommended target.

.PARAMETER Runtime
    The runtime identifier to publish for. Defaults to win-x64 on Windows and linux-x64 elsewhere.

.PARAMETER PackageVersion
    The version of the packages to consume. Defaults to the <Version> in the repository-root
    Directory.Build.props, which is what `dotnet pack` produces.

.PARAMETER Configuration
    Build configuration. Release by default, because that is what CI uses.

.PARAMETER Pack
    Pack the shipping projects into artifacts/packages first, and clear the consumer's isolated package
    cache so the fresh build of an unchanged version number is actually picked up. CI does not use this - it
    downloads the exact packages the publish job produced.

.NOTES
    A native publish needs a C++ toolchain. On Windows that is MSVC, and vswhere.exe must be resolvable or the
    link step fails with a misleading MSB3073 - this script puts the Visual Studio Installer directory on PATH
    for that reason. On Linux it needs clang and zlib1g-dev.

.EXAMPLE
    pwsh -File scripts/verify-package-aot.ps1 -Pack

.EXAMPLE
    pwsh -File scripts/verify-package-aot.ps1 -Framework net8.0
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('net8.0', 'net10.0')]
    [String] $Framework = 'net10.0',

    [String] $Runtime,

    [String] $PackageVersion,

    [String] $Configuration = 'Release',

    [Switch] $Pack
)

$ErrorActionPreference = 'Stop'

if (-not $Runtime)
{
    $Runtime = $IsWindows ? 'win-x64' : 'linux-x64'
}

# scripts/<this file> - the repository root is one level up, whatever the current directory is. Every path
# below is anchored to it.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$solution = Join-Path $repositoryRoot 'DbConnectionPlus.slnx'
$consumerDirectory = Join-Path $repositoryRoot 'tests/package-consumption/AotConsumer'
$project = Join-Path $consumerDirectory 'AotConsumer.csproj'
$packageDirectory = Join-Path $repositoryRoot 'artifacts/packages'
$packageCache = Join-Path $repositoryRoot 'tests/package-consumption/.packages'
$consumerNuGetConfig = Join-Path $repositoryRoot 'tests/package-consumption/nuget.config'
$publishDirectory = Join-Path $repositoryRoot "artifacts/package-aot/$Framework-$Runtime"

if (-not $PackageVersion)
{
    # The single source of truth for the version of every package. Reading it here keeps this script
    # correct across a release bump without a second place to edit.
    $sharedProperties = Join-Path $repositoryRoot 'Directory.Build.props'
    $PackageVersion = ([Xml] (Get-Content -Raw $sharedProperties)).Project.PropertyGroup.Version |
        Where-Object { $_ } |
        Select-Object -First 1

    if (-not $PackageVersion)
    {
        Write-Host "FAILED. No <Version> found in $sharedProperties." -ForegroundColor Red

        exit 1
    }
}

# Everything below runs inside a try/finally that restores the caller's PATH and working location, so an
# interrupted run leaves the shell as it found it. `exit` inside a try block still runs the finally.
#
# The working directory is not cosmetic: `dotnet` resolves global.json from the CURRENT directory upward,
# and this repository's global.json is what pins the SDK the packages are built with.
$originalPath = $env:PATH
$originalNuGetPackages = $env:NUGET_PACKAGES

Push-Location -LiteralPath $repositoryRoot
try
{
    if ($IsWindows)
    {
        # Without vswhere.exe on PATH the native link step fails with MSB3073 and a misleading error message.
        $visualStudioInstaller = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer'

        if ((Test-Path -LiteralPath $visualStudioInstaller) -and ($env:PATH -notlike "*$visualStudioInstaller*"))
        {
            $env:PATH = "$visualStudioInstaller;$env:PATH"
        }
    }

    # --- The packages under test -------------------------------------------------------------------------------

    if ($Pack)
    {
        Write-Host "Packing the shipping projects ($PackageVersion)..." -ForegroundColor Cyan

        & dotnet pack $solution --configuration $Configuration --output $packageDirectory
        if ($LASTEXITCODE -ne 0)
        {
            Write-Host 'FAILED. dotnet pack did not succeed.' -ForegroundColor Red

            exit 1
        }
    }

    # The consumer's package cache is emptied on EVERY run, not only after a pack. NuGet resolves by version
    # and not by content, so a cache entry for 4.0.0 satisfies a reference to 4.0.0 whatever bytes produced
    # it - and this script exists to test THESE bytes. A stale entry would turn the gate into a re-run of
    # whatever passed last time.
    if (Test-Path -LiteralPath $packageCache)
    {
        Remove-Item -Recurse -Force -LiteralPath $packageCache
    }

    # --- The artifact set, validated from the nuspec ------------------------------------------------------------
    #
    # From the metadata inside each package, never from its file name. A file name is a claim: renaming
    # DbConnectionPlus.3.9.9.nupkg to DbConnectionPlus.4.0.0.nupkg would satisfy a name check and then publish
    # a package whose nuspec still says 3.9.9. The id and the version that matter are the ones NuGet reads.

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    function Get-NuspecMetadata
    {
        param([Parameter(Mandatory)] [String] $PackagePath)

        $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
        try
        {
            $entry = $archive.Entries | Where-Object { $_.FullName -like '*.nuspec' -and $_.FullName -notlike '*/*' } |
                Select-Object -First 1

            if ($null -eq $entry) { return $null }

            $reader = [System.IO.StreamReader]::new($entry.Open())
            try { $nuspec = [Xml] $reader.ReadToEnd() }
            finally { $reader.Dispose() }
        }
        finally
        {
            $archive.Dispose()
        }

        return [PSCustomObject] @{
            Id = $nuspec.package.metadata.id
            Version = $nuspec.package.metadata.version
        }
    }

    $expectedPackageIds = @(
        'DbConnectionPlus'
        'DbConnectionPlus.DatabaseAdapters.MySql'
        'DbConnectionPlus.DatabaseAdapters.Oracle'
        'DbConnectionPlus.DatabaseAdapters.PostgreSql'
        'DbConnectionPlus.DatabaseAdapters.Sqlite'
        'DbConnectionPlus.DatabaseAdapters.SqlServer'
    )

    if (-not (Test-Path -LiteralPath $packageDirectory))
    {
        Write-Host "FAILED. $packageDirectory does not exist." -ForegroundColor Red
        Write-Host ''
        Write-Host 'This script consumes the packed packages, not the projects. Produce them first:' -ForegroundColor Red
        Write-Host '  pwsh -File scripts/verify-package-aot.ps1 -Pack' -ForegroundColor Red

        exit 1
    }

    $available = @{}
    foreach ($package in (Get-ChildItem -LiteralPath $packageDirectory -Filter '*.nupkg'))
    {
        $metadata = Get-NuspecMetadata -PackagePath $package.FullName
        if ($null -eq $metadata)
        {
            Write-Host "FAILED. $($package.Name) contains no nuspec." -ForegroundColor Red

            exit 1
        }

        $available["$($metadata.Id)/$($metadata.Version)"] = $package.Name
    }

    $missing = @($expectedPackageIds | Where-Object { -not $available.ContainsKey("$_/$PackageVersion") })

    if ($missing.Count -gt 0)
    {
        Write-Host "FAILED. $packageDirectory does not contain version $PackageVersion of:" -ForegroundColor Red
        $missing | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        Write-Host ''
        Write-Host 'Present, by the id and version in each nuspec:' -ForegroundColor Red
        if ($available.Count -eq 0)
        {
            Write-Host '  (nothing)' -ForegroundColor Red
        }
        else
        {
            $available.GetEnumerator() | Sort-Object Key | ForEach-Object {
                Write-Host "  $($_.Key)   ($($_.Value))" -ForegroundColor Red
            }
        }
        Write-Host ''
        Write-Host 'Produce them with: pwsh -File scripts/verify-package-aot.ps1 -Pack' -ForegroundColor Red

        exit 1
    }

    Write-Host "Every package is present at $PackageVersion, by their nuspec metadata." -ForegroundColor Cyan

    Write-Host "Publishing the Native AOT package consumer ($Framework, $Runtime, packages $PackageVersion)..." `
        -ForegroundColor Cyan

    # NUGET_PACKAGES is set explicitly, and to the CONSUMER's isolated cache. The environment variable takes
    # precedence over the globalPackagesFolder setting in tests/package-consumption/nuget.config, so an
    # inherited one - CI sets NUGET_PACKAGES to a workspace-wide cache - would silently defeat the isolation
    # the config file exists to provide, and the consumer could restore a DbConnectionPlus assembly that never
    # came out of these packages. It is restored in the finally block at the end of the script.
    $env:NUGET_PACKAGES = $packageCache

    # PublishAot is passed here rather than set in the project file, so that an ordinary `dotnet run` of the
    # consumer stays a genuine JIT baseline. See the comment in the .csproj.
    #
    # RestoreConfigFile names the CONSUMER's NuGet configuration explicitly rather than relying on NuGet's
    # upward search finding it. That file is what maps DbConnectionPlus and DbConnectionPlus.* to the local
    # artifact feed, and it <clear />s the sources first - so a missing local package fails instead of
    # resolving from nuget.org, where a published package of the same version exists and would look like a
    # pass.
    #
    # Run from the consumer directory, which is where a consumer would run it.
    Push-Location -LiteralPath $consumerDirectory
    try
    {
        $publishOutput = & dotnet publish 'AotConsumer.csproj' `
            --configuration $Configuration `
            --framework $Framework `
            --runtime $Runtime `
            --self-contained true `
            -p:RestoreConfigFile=$consumerNuGetConfig `
            -p:PublishAot=true `
            -p:DbConnectionPlusVersion=$PackageVersion `
            --output $publishDirectory 2>&1
    }
    finally
    {
        Pop-Location
    }

    $publishExitCode = $LASTEXITCODE

    $publishOutput | ForEach-Object { Write-Host $_ }

    if ($publishExitCode -ne 0)
    {
        Write-Host ''
        Write-Host "FAILED. The Native AOT publish exited with code $publishExitCode." -ForegroundColor Red

        exit 1
    }

    # --- The warning gate -------------------------------------------------------------------------------------

    $diagnostics = $publishOutput |
        Select-String -Pattern 'IL[23]\d{3}' |
        ForEach-Object {
            # Every diagnostic line ends with the MSBuild project suffix "[...csproj::TargetFramework=...]", which
            # names this project no matter which assembly the diagnostic came from. Strip it before deciding
            # where the diagnostic originated, or everything would look like it came from the consumer.
            $origin = $_.Line.Trim() -replace '\s*\[[^\[\]]*\]\s*$', ''

            [PSCustomObject] @{
                Origin = $origin
                Code = [Regex]::Match($_.Line, 'IL[23]\d{3}').Value
                Text = $_.Line.Trim()
            }
        }

    # The gate is ZERO diagnostics, from anywhere.
    #
    # The generic query methods carry neither [RequiresUnreferencedCode] nor [RequiresDynamicCode]: each of the three
    # underlying reflection sites is answered inside the library, where it occurs, so a consumer publishing with
    # PublishAot or PublishTrimmed sees nothing for any supported scenario.
    # See the "No consumer-facing diagnostics" section of docs/DESIGN-DECISIONS.md for the full argument.
    #
    # That makes this the strongest form of the gate: a warning appearing anywhere - in the library, in an adapter,
    # in a package in the closure, or at this consumer's own call sites - is a regression. The split below only
    # shapes the failure message, because "the library started warning" and "our own call site started warning"
    # have different causes. The same site is reported twice, once by the Roslyn analyzer and once by ILC, so the
    # list is de-duplicated.
    $fromConsumer = @($diagnostics | Where-Object { $_.Origin.StartsWith($consumerDirectory, [StringComparison]::OrdinalIgnoreCase) })
    $fromElsewhere = @($diagnostics | Where-Object { -not $_.Origin.StartsWith($consumerDirectory, [StringComparison]::OrdinalIgnoreCase) })

    Write-Host ''

    if ($diagnostics.Count -eq 0)
    {
        Write-Host 'IL diagnostics: none, from anywhere. A consumer publishing this way sees no warnings.' -ForegroundColor Cyan
    }
    else
    {
        Write-Host 'FAILED. The publish reported IL diagnostics, and the gate is zero:' -ForegroundColor Red

        if ($fromElsewhere.Count -gt 0)
        {
            Write-Host ''
            Write-Host "  From the library, an adapter or a package ($($fromElsewhere.Count) before de-duplication):" -ForegroundColor Red
            $fromElsewhere | Sort-Object Text -Unique | ForEach-Object { Write-Host "    $($_.Text)" -ForegroundColor Red }
        }

        if ($fromConsumer.Count -gt 0)
        {
            Write-Host ''
            Write-Host "  At the consumer's own call sites ($($fromConsumer.Count) before de-duplication):" -ForegroundColor Red
            $fromConsumer | Sort-Object Origin -Unique | Group-Object Code | Sort-Object Name | ForEach-Object {
                Write-Host ("    {0,-8} {1} call site(s)" -f $_.Name, $_.Count) -ForegroundColor Red
            }
            Write-Host ''
            Write-Host '  A diagnostic here means a public API started carrying [RequiresUnreferencedCode] or' -ForegroundColor Red
            Write-Host '  [RequiresDynamicCode] again, which is exactly the consumer experience this work removed.' -ForegroundColor Red
        }

        Write-Host ''
        Write-Host 'Do not silence these at the call site or with NoWarn. An IL2xxx warning is the only' -ForegroundColor Red
        Write-Host 'build-time evidence that the [DynamicallyAccessedMembers] chain is complete - restructure' -ForegroundColor Red
        Write-Host 'the code, or answer the diagnostic where it occurs with a justified, tested suppression.' -ForegroundColor Red

        exit 1
    }

    # --- Running the native binary ----------------------------------------------------------------------------

    $executableName = $IsWindows ? 'AotConsumer.exe' : 'AotConsumer'
    $executable = Join-Path $publishDirectory $executableName

    if (-not (Test-Path $executable))
    {
        Write-Host ''
        Write-Host "FAILED. The native binary was not produced at $executable." -ForegroundColor Red

        exit 1
    }

    Write-Host ''
    Write-Host 'Running the native binary...' -ForegroundColor Cyan
    Write-Host ''

    & $executable

    $runExitCode = $LASTEXITCODE

    Write-Host ''

    if ($runExitCode -ne 0)
    {
        Write-Host "FAILED. The native binary exited with code $runExitCode." -ForegroundColor Red

        exit 1
    }

    Write-Host "PASSED. $Framework/$Runtime published from the packages with no IL diagnostics at all, and every assertion passed." `
        -ForegroundColor Green

    exit 0
}
finally
{
    $env:PATH = $originalPath
    $env:NUGET_PACKAGES = $originalNuGetPackages
    Pop-Location
}
