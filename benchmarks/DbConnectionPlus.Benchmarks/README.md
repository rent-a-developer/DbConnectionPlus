# DbConnectionPlus.Benchmarks

A BenchmarkDotNet suite that measures DbConnectionPlus against a raw `DbCommand` baseline and against Dapper,
using an in-memory SQLite database.

Everything runs on the **JIT**. Three categories additionally run as a **Native AOT** compiled binary - the three
where DbConnectionPlus genuinely behaves differently there.

## Running them

```bash
pwsh -File scripts/benchmarks.ps1
```

Everything after the script name is forwarded to BenchmarkDotNet, so the usual switches work:

```bash
pwsh -File scripts/benchmarks.ps1 --filter *Query_Entities*
```

A full run is long: the Native AOT job publishes this assembly natively, so use `--filter` while iterating.

### Prerequisites

The AOT job needs a C++ toolchain - MSVC on Windows, `clang` and `zlib1g-dev` on Linux. On Windows `vswhere.exe`
must be resolvable or the native link step fails with a misleading `MSB3073`; the script puts the Visual Studio
Installer directory on `PATH` for that reason, exactly as `scripts/verify-package-aot.ps1` does.

## Why two jobs

DbConnectionPlus picks its materializer at run time:

```csharp
if (RuntimeFeature.IsDynamicCodeSupported)
{
    return CreateExpressionCompiledMaterializer(...);   // JIT
}

return CreateReflectionMaterializer(...);               // AOT
```

The AOT compiler folds that switch to `false` and removes the expression-tree branch entirely. So the reflection
path is the one every Native AOT consumer actually runs, and this job is the only thing in the repository that
measures it.

The two other AOT-safe replacements - entity accessors over `MethodInvoker` and the generalized
`EnumerableReader` - are single path, identical on both runtimes.

## Only three categories run under Native AOT

`Query_Entities`, `Query_ValueTuples` and `TemporaryTable_ComplexObjects`. Nothing else.

The branch above exists in exactly two places in the library - `EntityMaterializerFactory` and
`ValueTupleMaterializerFactory` - and both are reached only from `Query<T>` where `T` is an entity type or a value
tuple. Every other category runs byte-for-byte identical IL on both runtimes, so an AOT row for it would only
report how RyuJIT and ILC differ, which is a fact about .NET rather than about this library.
`TemporaryTable_ComplexObjects` makes the list because it ends in a `Query<BenchmarkEntity>` over the temporary
table: its write path is single path, its read path is not.

That was measured before the rows were dropped, and the allocation column is the clean discriminator - only the
three retained categories move at all:

| Category | Allocated, JIT → AOT | |
|---|---|---|
| `Query_ValueTuples` | 53,433 → 84,672 B | **1.58x** |
| `Query_Entities` | 80,641 → 113,462 B | **1.41x** |
| `TemporaryTable_ComplexObjects` | 1,883,218 → 1,965,494 B | 1.04x |
| `Query_Dynamic` | 147,680 → 147,721 B | flat |
| `TemporaryTable_ScalarValues` | 2,696,352 → 2,696,304 B | flat |
| `Query_Scalars` | 32,480 → 32,520 B | flat |
| `UpdateEntity` | 9,294 → 9,343 B | flat |
| `DeleteEntity`, `DeleteEntities`, `InsertEntity`, `InsertEntities`, `UpdateEntities`, `ExecuteNonQuery`, `ExecuteReader`, `ExecuteScalar`, `Exists`, `Parameter` | | flat |

The flat rows are the evidence that the CRUD accessors and the temporary-table writer really are single path -
they go through `MethodInvoker` and `EnumerableReader` on both runtimes, with no `IsDynamicCodeSupported` branch
anywhere. Keeping them in every run re-established the same fact at the cost of most of the suite's wall time;
the numbers above are the record instead. Re-run them by adding their names to `AotJobBenchmarks` in
`AotJobFilter` if the claim ever needs re-checking.

**This is not a trimming check.** BenchmarkDotNet reports a benchmark that returned default-valued entities as a
fast benchmark, not as a broken one - faster, in fact, since binding nothing is less work. Silent trimming damage
therefore looks like an improvement here. `tests/package-consumption/AotConsumer` is what asserts values, and it is
the only thing in the repository that can see that defect.

## Reading the summary

The `Job` column is `JIT` or `AOT`. The three categories above are reported as two groups; every other category has
a JIT group only.

`Ratio` compares against the `*_Command` raw-`DbCommand` baseline **within one job**, never across jobs. The
JIT-versus-AOT comparison is read from the `Mean` column of the two rows for the same method. `BenchmarksOrderer` documents why the logical group has to be the category **and** the job.

## What runs where

| Suffix | JIT | AOT | |
|---|:-:|:-:|---|
| `_Command` | ✓ | ✓ | Raw ADO.NET. The baseline. |
| `_DbConnectionPlus` | ✓ | ✓ | Expression-compiled path on the JIT, reflection path under AOT. |
| `_Dapper` | ✓ | — | Dapper generates IL at run time, which Native AOT has no runtime for. |
| `_Dapper_Aot` | — | ✓ | The same Dapper call, reached through the Dapper.AOT build-time generator. This *is* "Dapper under Native AOT", so it has no business running on the JIT, where nobody has to choose between the two. |

`AotJobFilter` holds the seven benchmark names the AOT job runs, and excludes `_Aot`-suffixed names from the JIT
job. Excluding the `_Dapper` cases from the AOT job is deliberate rather than an oversight: a benchmark that throws
aborts the whole run - they are not merely slow there, they cannot run at all. Measured, by letting
`Query_ValueTuples_Dapper` into the AOT job:

```
System.PlatformNotSupportedException: Dynamic code generation is not supported on this platform.
   at System.Reflection.Emit.ReflectionEmitThrower.ThrowPlatformNotSupportedException()
   at Dapper.SqlMapper.GetTypeDeserializerImpl(Type, DbDataReader, Int32, Int32, Boolean)
   at Dapper.SqlMapper.TypeDeserializerCache.GetReader(DbDataReader, Int32, Int32, Boolean)
```

Re-run that experiment the same way if the exclusion ever looks like an unfair handicap rather than a fact.

### Dapper.AOT coverage

Dapper.AOT is opted into **per method**, never at module level - the `_Dapper` benchmarks have to keep going
through Dapper's runtime IL emit, because that is the thing being compared against.

Of the three categories that run under Native AOT it covers exactly one - `Query_Entities`, which is the
comparison the whole AOT job exists to make:

| Category | Dapper under AOT | Why |
|---|:-:|---|
| `Query_Entities` | ✓ | Intercepted. `BenchmarkEntity` is shaped so that it can be - see below. |
| `Query_ValueTuples` | ✗ | The generator does not materialize value tuples. Annotating the call site produced no interceptor **and no diagnostic**; `[BindTupleByName]` only silences the DAP012 advisory. |
| `TemporaryTable_ComplexObjects` | ✗ | Compares against **Dapper.Contrib**, which Dapper.AOT does not cover: Contrib's calls into Dapper happen inside the Contrib assembly, and interceptors only rewrite call sites in the project being compiled. [DapperAOT#139](https://github.com/DapperLib/DapperAOT/issues/139) asks for exactly this and is still open. Annotating the benchmark anyway gets **three of its four** call sites intercepted - both `SqlMapper.Execute` calls and the `SqlMapper.Query<BenchmarkEntity>` - and then dies on the fourth; see the comment in `Benchmarks.TemporaryTable_ComplexObjects.cs` for the stack trace. |

So value tuples and temporary tables have no Dapper-family option under Native AOT at all, and those two compare
DbConnectionPlus against the raw `DbCommand` baseline alone. That is a result, not a hole in the harness.

### Why `BenchmarkEntity` has no `Guid` and no `TimeSpan`

The generated row factory reads every column straight off the `DbDataReader` and **never** consults the handlers
registered through `SqlMapper.AddTypeHandler` - known upstream as
[DapperAOT#92](https://github.com/DapperLib/DapperAOT/issues/92), open since 2023: *"AddTypeHandler is not yet AOT
friendly"*, and a registered handler *"never gets called"*. Where the reader's field type already matches the
target the factory calls `GetFieldValue<T>`; otherwise it falls back to a `Convert.ChangeType`-style path. SQLite
stores `Guid` and `TimeSpan` as TEXT and neither is `IConvertible`, so that fallback threw
`Invalid cast from 'System.String' to 'System.TimeSpan'` on the first row - on the JIT as much as under AOT.

Both properties were therefore removed from `BenchmarkEntity`. Everything else survives the fallback, including the
enum, which arrives as TEXT. The alternative - hand-writing a `RowFactory<T>` - would measure hand-written code
rather than the generator, which is not a comparison worth having.

> ⚠️ **Verify interception when you add a `_Dapper_Aot` benchmark.** Dapper.AOT leaves call sites it does not
> support as ordinary Dapper, silently - the FAQ's words are "we just leave the original Dapper code alone". Such a
> benchmark measures reflection-emit Dapper under a label that says otherwise, and throws in the AOT job. Three of
> the eight call sites annotated here turned out to be exactly that. To check:
>
> ```bash
> dotnet build benchmarks/DbConnectionPlus.Benchmarks -c Release --no-incremental -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=artifacts/dapper-aot-generated
> ```
>
> Then confirm the call site appears in an `InterceptsLocationAttribute` in the generated file. `DAP005` -
> "candidate Dapper methods detected, but none have Dapper.AOT enabled" - is the coarser signal.

## Test data

`TestData/BenchmarkData.cs` generates the entities. It is a plain seeded generator rather than the unit test
project's AutoFixture-based `Generate`, for two reasons.

Referencing the unit test project would pull AutoFixture, Bogus, Mapster, xunit.v3 and NSubstitute into the Native
AOT compilation closure, and all of them resolve members reflectively. Nothing but reflection reaches
`BenchmarkEntity`'s properties, so trimming would be free to remove them - and reflection over a trimmed type
returns fewer members with no error at all. The setup would seed default-valued entities and the benchmarks would
measure that quite happily.

The second reason applies to the JIT job just as much: the generator is **seeded**, so every process produces the
same entities. BenchmarkDotNet runs each job in its own process, so with an unseeded generator the two jobs would
be measured against different data and their means would not be comparable.

## The delete benchmarks roll back instead of committing

Deleting consumes rows, so the table has to be back to its original state before the next invocation. The obvious
way to arrange that is `[IterationSetup]`, restoring the entity table from a snapshot before every iteration.
**Do not.**

It has a cost that is easy to miss. `[IterationSetup]` forces `UnrollFactor=1` and **`InvocationCount=1`**
([docs](https://benchmarkdotnet.org/articles/features/setup-and-cleanup.html)), which switches off BenchmarkDotNet's
pilot stage. Iteration time then stops being something BenchmarkDotNet tunes and becomes a function of a
hard-coded `*_OperationsPerInvoke` constant and of how fast the machine is — which is how the suite ended up
emitting six *"the minimum observed iteration time is 45.9 ms which is very small"* warnings. That behaviour is by
design and will not change: [dotnet/BenchmarkDotNet#1127](https://github.com/dotnet/BenchmarkDotNet/issues/1127) is
this exact complaint, closed **wontfix**.

So the benchmarks **roll the transaction back** rather than committing it. Every invocation puts the rows back by
itself, no `[IterationSetup]` is needed, and the pilot stage tunes `InvocationCount` to the configured iteration
time on whatever machine the suite runs on.

Nothing is lost by doing so, because a rollback costs what a commit costs. Measured on this schema in an in-memory
SQLite database, timing `BeginTransaction → N × DELETE → Commit|Rollback` with the table restored from a snapshot
before every timed block in both variants:

| Rows deleted in the transaction | commit | rollback | rollback/commit |
|---:|---:|---:|---:|
| 1 | 3.5 µs | 3.6 µs | 1.03x |
| 100 | 47.9 µs | 48.6 µs | 1.01x |
| 1,000 | 491.7 µs | 486.4 µs | 0.99x |
| 10,000 | 5.245 ms | 5.133 ms | 0.98x |
| 40,000 | 23.217 ms | 23.403 ms | 1.01x |

Within 1–3% at every scale, deviating in both directions: noise, not a systematic difference. In-memory SQLite
journals in memory, so discarding the journal costs about what applying it does.

The ratios confirm it end to end. Converting the two categories moved `DeleteEntity_Dapper` from 1.43x to 1.40x
and `DeleteEntities_Dapper` from 1.35x to 1.29x, both inside the run-to-run spread — and allocations came out
within 56 bytes per operation of the previous numbers, which is the transaction bookkeeping and nothing else.

Two things worth knowing before changing the sizing:

- **The constants are not sized for a target iteration time any more.** They only have to amortize the transaction:
  `BeginTransaction` plus `Rollback` costs roughly 3 µs against roughly 0.5 µs for the marginal delete, so at one
  delete per invocation about two thirds of the measurement would be transaction overhead. That is common to all
  three implementations, so it is not a bias — but it compresses the ratios the benchmark exists to show. 1,000
  deletes (and 20 batches) put it well under 1%.
- **The rollback doubles as a correctness check.** `DeleteEntity` throws `DbUpdateConcurrencyException` when a
  delete affects an unexpected number of rows, so if the rollback ever stopped restoring, the second invocation
  would fail immediately rather than quietly measuring deletes that match nothing.

## Project references

Only the SQLite adapter is referenced, because every referenced assembly ends up in the AOT job's closure and the
benchmarks call nothing but `UseSqlite()`. The Oracle adapter in particular would pull in
`Oracle.ManagedDataAccess.Core`, which is not AOT-ready at all.

## One thing to watch in `Benchmarks.cs`

The instance constructor touches `Dapper.Contrib`'s `SqlMapperExtensions` behind a
`RuntimeFeature.IsDynamicCodeSupported` guard. That guard is not optional, and it belongs there rather than in the
individual benchmarks: the constructor runs for **every** benchmark in the class, so anything that throws in it
takes down the whole job - including `_Command` and `_DbConnectionPlus`, which have nothing to do with Dapper.

That is not hypothetical, and it is worth knowing the exact shape of it: registering Dapper's `Guid` and
`TimeSpan` type handlers there without the guard fails **every** AOT row in the suite, not just the Dapper ones,
because `SqlMapper.AddTypeHandler` reaches its registry through `SqlMapper.TypeHandlerCache<T>`, which Native AOT
cannot construct reflectively. Dapper setup in a shared constructor is Dapper setup charged to every benchmark.
