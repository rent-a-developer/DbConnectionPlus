using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public class BenchmarksConfig : ManualConfig
{
    /// <inheritdoc />
    public BenchmarksConfig()
    {
        this.Orderer = new BenchmarksOrderer();
        this.SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);

        this.AddColumn(StatisticColumn.Median);
        this.AddColumn(StatisticColumn.P90);
        this.AddColumn(StatisticColumn.P95);

        this.AddExporter(PlainExporter.Default);
        this.AddExporter(MarkdownExporter.Default);

        this.AddJob(
            Job.Default
                .WithWarmupCount(10)
                .WithMinIterationTime(TimeInterval.FromMilliseconds(100))
                .WithMaxIterationCount(20)
                .WithInvocationCount(1)
                .WithUnrollFactor(1)

                // Since DbConnectionPlus will mostly be used in server applications, we test with server GC.
                .WithGcServer(true)

                .WithOutlierMode(OutlierMode.DontRemove)
        );
    }
}
