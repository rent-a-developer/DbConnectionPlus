using System.Collections.Immutable;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public class BenchmarksOrderer : IOrderer
{
    /// <inheritdoc />
    public Boolean SeparateLogicalGroups => true;

    /// <inheritdoc />
    public IEnumerable<BenchmarkCase> GetExecutionOrder(
        ImmutableArray<BenchmarkCase> benchmarksCase,
        IEnumerable<BenchmarkLogicalGroupRule>? order = null
    ) =>
        benchmarksCase
            .OrderBy(a => a.Descriptor.Categories[0])
            .ThenByDescending(a => a.Descriptor.Baseline)
            .ThenBy(a => a.Descriptor.WorkloadMethod.Name);

    /// <inheritdoc />
    public String? GetHighlightGroupKey(BenchmarkCase benchmarkCase) =>
        benchmarkCase.Descriptor.Categories.FirstOrDefault();

    /// <inheritdoc />
    public String? GetLogicalGroupKey(
        ImmutableArray<BenchmarkCase> allBenchmarksCases,
        BenchmarkCase benchmarkCase
    ) =>
        benchmarkCase.Descriptor.Categories.FirstOrDefault();

    /// <inheritdoc />
    public IEnumerable<IGrouping<String, BenchmarkCase>> GetLogicalGroupOrder(
        IEnumerable<IGrouping<String, BenchmarkCase>> logicalGroups,
        IEnumerable<BenchmarkLogicalGroupRule>? order = null
    ) =>
        logicalGroups.OrderBy(it => it.Key);

    /// <inheritdoc />
    public IEnumerable<BenchmarkCase> GetSummaryOrder(
        ImmutableArray<BenchmarkCase> benchmarksCases,
        Summary summary
    ) =>
        benchmarksCases
            .OrderBy(a => a.Descriptor.Categories[0])
            .ThenByDescending(a => a.Descriptor.Baseline)
            .ThenBy(a => a.Descriptor.WorkloadMethod.Name);
}
