using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public class BenchmarksConfig : ManualConfig
{
    /// <inheritdoc />
    public BenchmarksConfig()
    {
        this.Orderer = new BenchmarksOrderer();
        this.SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);

        this.AddExporter(MarkdownExporter.Default);

        this.AddJob(
            Job.Default
                .WithMinIterationTime(TimeInterval.FromMilliseconds(150))

                // Since DbConnectionPlus will mostly be used in server applications, we test with server GC.
                .WithGcServer(true)
        );
    }
}
