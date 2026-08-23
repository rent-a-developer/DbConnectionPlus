using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.NativeAot;
using Perfolizer.Horology;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public class BenchmarksConfig : ManualConfig
{
    public BenchmarksConfig()
    {
        this.Orderer = new BenchmarksOrderer();
        this.SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);

        // The Job column stays visible: it is what tells a JIT row apart from an AOT row.
        this.HideColumns("InvocationCount", "UnrollFactor");

        this.AddExporter(MarkdownExporter.Default);

        this.AddFilter(new AotJobFilter());

        this.AddJob(CreateJob(JitJobId));

        // The Native AOT job publishes this assembly with PublishAot=true and runs the native binary, so
        // RuntimeFeature.IsDynamicCodeSupported is false in it and DbConnectionPlus takes its reflection based
        // materializer path instead of the expression compiled one. That branch is the whole point of the second
        // job: it is the one thing in the library that behaves differently under Native AOT, and nothing else
        // measures it.
        //
        // A native publish needs a C++ toolchain (MSVC on Windows, clang and zlib1g-dev on Linux). Use
        // scripts/benchmarks.ps1, which puts vswhere.exe on PATH first - without it the link step fails with a
        // misleading MSB3073.
        this.AddJob(CreateJob(AotJobId).WithToolchain(NativeAotToolchain.Net10_0));
    }

    // The settings both jobs share, so that the only difference between them is the toolchain.
    private static Job CreateJob(string id) =>
        Job
            .Default.WithId(id)
            // The default adaptive warmup runs ~9 iterations, but every iteration already executes tens of
            // thousands of invocations, so the tiered JIT has reached steady state before the first warmup
            // iteration completes. Three is enough; the rest was pure wall time.
            .WithWarmupCount(3)
            // 300 ms instead of the default 500 ms. The confidence interval scales with the total measured
            // time, so this widens the reported error from ~1.0 % to ~1.0-2.0 % of the mean, which is still
            // well below the effect sizes these benchmarks compare. Measured on the Exists category, the two
            // settings together cut a benchmark case from ~21.7 s to ~13.4 s.
            .WithIterationTime(TimeInterval.FromMilliseconds(300))
            .WithMaxIterationCount(20)
            // Since DbConnectionPlus will mostly be used in server applications, we test with server GC.
            .WithGcServer(true);

    // The Job column of the summary shows these.
    public const string JitJobId = "JIT";
    public const string AotJobId = "AOT";
}
