using System.Collections.Immutable;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

// Orders the benchmarks by category and, within a category, by job.
//
// The logical group is the pair of category and job, not the category alone, for two reasons:
//
//   - The *_Command baseline stays valid. BenchmarkDotNet allows exactly one baseline per logical group, and
//     with the category alone as the key a group would hold the JIT and the AOT copy of that method - two
//     baselines, which it rejects.
//   - Ratio keeps its meaning: implementation against the raw DbCommand baseline, inside one runtime. The JIT
//     against AOT comparison is read from the Mean column of the two rows for the same method.
public class BenchmarksOrderer : IOrderer
{
    public bool SeparateLogicalGroups => true;

    public IEnumerable<BenchmarkCase> GetExecutionOrder(
        ImmutableArray<BenchmarkCase> benchmarksCase,
        IEnumerable<BenchmarkLogicalGroupRule>? order = null
    ) =>
        Sort(benchmarksCase);

    public string? GetHighlightGroupKey(BenchmarkCase benchmarkCase) =>
        GetLogicalGroupKey(benchmarkCase);

    public string? GetLogicalGroupKey(
        ImmutableArray<BenchmarkCase> allBenchmarksCases,
        BenchmarkCase benchmarkCase
    ) =>
        GetLogicalGroupKey(benchmarkCase);

    public IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(
        IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups,
        IEnumerable<BenchmarkLogicalGroupRule>? order = null
    ) =>
        logicalGroups
            .OrderBy(it => it.First().Descriptor.Categories[0], StringComparer.Ordinal)
            .ThenBy(it => GetJobRank(it.First()));

    public IEnumerable<BenchmarkCase> GetSummaryOrder(
        ImmutableArray<BenchmarkCase> benchmarksCases,
        Summary summary
    ) =>
        Sort(benchmarksCases);

    private static IEnumerable<BenchmarkCase> Sort(ImmutableArray<BenchmarkCase> benchmarkCases) =>
        benchmarkCases
            .OrderBy(a => a.Descriptor.Categories[0], StringComparer.Ordinal)
            .ThenBy(GetJobRank)
            .ThenByDescending(a => a.Descriptor.Baseline)
            .ThenBy(a => a.Descriptor.WorkloadMethod.Name, StringComparer.Ordinal);

    private static string GetLogicalGroupKey(BenchmarkCase benchmarkCase) =>
        $"{benchmarkCase.Descriptor.Categories.FirstOrDefault()}-{benchmarkCase.Job.Id}";

    // Ranked rather than sorted by name, so that JIT is reported before AOT instead of alphabetically.
    private static int GetJobRank(BenchmarkCase benchmarkCase) =>
        benchmarkCase.Job.Id.Contains(BenchmarksConfig.JitJobId, StringComparison.Ordinal) ? 0 : 1;
}
