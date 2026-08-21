using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

// Decides which benchmark runs in which job: the Native AOT job runs the names listed below and nothing else,
// the JIT job runs everything whose name does not end in _Aot.
//
// The list is short because DbConnectionPlus branches on RuntimeFeature.IsDynamicCodeSupported in exactly two
// places, both materializer factories, reached only by Query<T> where T is an entity type or a value tuple.
// Every other benchmark runs byte-for-byte identical IL on both runtimes, so an AOT row for it would only
// report how RyuJIT and ILC differ - a fact about .NET, not about this library. See the README next to this
// file for the measurements behind that.
public class AotJobFilter : IFilter
{
    public Boolean Predicate(BenchmarkCase benchmarkCase)
    {
        var isAotJob = benchmarkCase.Job.Id.Contains(BenchmarksConfig.AotJobId, StringComparison.Ordinal);
        var benchmarkName = benchmarkCase.Descriptor.WorkloadMethod.Name;
        var isAotOnlyBenchmark = benchmarkName.EndsWith(AotOnlyBenchmarkSuffix, StringComparison.Ordinal);

        return isAotJob
            ? AotJobBenchmarks.Contains(benchmarkName)
            : !isAotOnlyBenchmark;
    }

    // TemporaryTable_ComplexObjects is here because it ends in a Query<BenchmarkEntity> over the temporary
    // table: its write path is single path, its read path is not.
    //
    // Only Query_Entities has a Dapper entry. Dapper cannot run under Native AOT at all - it builds its
    // materializers with Reflection.Emit - so the only way to have it here is the Dapper.AOT build-time
    // generator, and that generator handles neither value tuples nor Dapper.Contrib, which is what the other
    // two categories compare against.
    private static readonly HashSet<String> AotJobBenchmarks =
    [
        nameof(Benchmarks.Query_Entities_Command),
        nameof(Benchmarks.Query_Entities_Dapper_Aot),
        nameof(Benchmarks.Query_Entities_DbConnectionPlus),
        nameof(Benchmarks.Query_ValueTuples_Command),
        nameof(Benchmarks.Query_ValueTuples_DbConnectionPlus),
        nameof(Benchmarks.TemporaryTable_ComplexObjects_Command),
        nameof(Benchmarks.TemporaryTable_ComplexObjects_DbConnectionPlus)
    ];

    // Marks a benchmark as Native AOT only, so that it is kept out of the JIT job.
    private const String AotOnlyBenchmarkSuffix = "_Aot";
}
