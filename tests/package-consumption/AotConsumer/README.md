# AotConsumer

A console application that installs the packed `DbConnectionPlus` and `DbConnectionPlus.DatabaseAdapters.Sqlite`
packages, exercises the library's feature paths against a real SQLite database, and is published with
**Native AOT** and run as a native binary.

## Why this exists and not a unit test

Under trimming, a missing `[DynamicallyAccessedMembers]` annotation makes reflection return **fewer members
with no error at all**. The library then binds no columns and hands back entities whose properties are all
left at their default values. Measured: 6 columns of real data in, 0 bound, no exception.

Nothing is trimmed on the JIT, so the entire unit and integration suite passes with a broken annotation
chain. **This program is the only check in the repository that can see the defect.** The design that defends
against it - annotations, no suppressions on the entity path, and the zero-binding guard - is recorded in the
[Native AOT and Trimming](../../../docs/DESIGN-DECISIONS.md#native-aot-and-trimming) section of
docs/DESIGN-DECISIONS.md.

That is also why every case asserts **values**, never row counts: silent trimming damage does not remove
rows, it empties them.

## Why it consumes packages, not projects

The annotations, the embedded `ILLink.Descriptors.xml` and the `IsTrimmable` assembly marker all have to
survive `dotnet pack`. A project-referenced version of this program would pass even if packing dropped every
one of them, because the trimmer would be reading the freshly compiled assembly rather than the one a
consumer installs. See the [folder README](../README.md) for the isolation boundary that keeps the
repository's own build wiring out of here.

## Running it

```bash
pwsh -File scripts/verify-package-aot.ps1 -Pack
```

That packs the shipping projects, publishes this consumer with `-p:PublishAot=true`, gates the IL diagnostics, and
runs the native binary. Pass `-Framework net8.0` for the documented AOT floor; the default is `net10.0`. CI
runs the same script for `net8.0` and `net10.0`, on Linux and Windows, against the exact packages it will publish.

For the JIT baseline - the same assertions with the expression-tree materializers instead of the reflection
ones - run it as an ordinary application:

```bash
dotnet run --project tests/package-consumption/AotConsumer -c Release -f net10.0
```

The banner reports `RuntimeFeature.IsDynamicCodeSupported`, so it is always visible which of the two
materializer paths produced the result.

### Prerequisites

A native publish needs a C++ toolchain: MSVC on Windows, `clang` and `zlib1g-dev` on Linux. On Windows
`vswhere.exe` must be resolvable or the link step fails with a misleading `MSB3073` - the script puts the
Visual Studio Installer directory on `PATH` for that reason.

## The warning gate

The gate is **zero IL diagnostics, from anywhere** - the library, an adapter, a package in the closure, or this
program's own call sites.

The generic query methods carry neither `[RequiresUnreferencedCode]` nor `[RequiresDynamicCode]`; the three
underlying reflection sites are answered inside the library instead, and
[No consumer-facing diagnostics](../../../docs/DESIGN-DECISIONS.md#4-no-consumer-facing-diagnostics) holds the
argument. So this program is a faithful sample of what a consumer sees, and what a consumer sees is nothing.

⚠️ **Both target frameworks have to stay in the gate.** `net8.0` needs an `IL3050` suppression on the two
`CreateMaterializer` dispatchers that `net10.0` does not, because only `net9.0`+ annotates
`RuntimeFeature.IsDynamicCodeSupported` as a `[FeatureGuard]`. The `net10.0` build compiling **without** that
suppression is what verifies it; running only one framework turns a checked fact into an assumption. Which
target framework a consumer gets is itself a packaging decision, so the gate covering both is also what
proves the multi-targeted package ships two correct assets rather than one.

⚠️ Never silence a diagnostic at a call site or with `NoWarn`. An `IL2xxx` warning is the only build-time proof
that the annotation chain is complete; silencing it is how silently unpopulated entities ship. Answer it where
it occurs, with a justification and a test, or restructure the code.

## What it covers

| # | Case | What it proves |
|--:|---|---|
| 1 | `InsertEntity` | Property reads through the accessor delegates work without run-time code generation. |
| 2 | `Query<SmokeEntity>` | Property-setter materialization, **every property asserted**. The DAM chain is intact. |
| 3 | `Query<SmokeEntity>`, different `SELECT` list | Materializers are cached per result-set shape; a second shape must map just as correctly. |
| 4 | `Query<ImmutableSmokeEntity>` | Constructor injection. |
| 5 | `Query<(Int64, String, Decimal)>` | Value tuples of seven fields or fewer. |
| 6 | `Query<(...8 fields)>` | Nested value tuples (`TRest`) - the case that needs `ILLink.Descriptors.xml`. |
| 7 | `Query` (non-generic) | `DataRow` and its string indexer, the AOT-safe way to read an untyped result set. |
| 8 | `TemporaryTable(IEnumerable<Int64>)` | Single-column temporary tables. |
| 9 | `TemporaryTable(IEnumerable<SmokeItem>)` | Multi-column temporary tables, i.e. `EnumerableReader`. |
| 10 | Zero-binding guard | A result set that binds no property throws instead of returning default-valued entities. |
| 11 | `Query<(Int64, enum)>` | An enum as a value tuple field, from an `INTEGER` column - `Enum.IsDefined` and `Enum.ToObject` need the enum's values. |
| 12 | `Query<(...8 fields)>`, enum in `TRest` | The same, one indirection deeper: the enum type is reached only through the nested tuple's generic arguments. |
| 13 | `Query<(Int64, enum)>` | An enum parsed **by name** from a `TEXT` column - `Enum.TryParse` needs the enum's member *names*, not just its values. |
| 14 | `Query<(...8 fields)>`, enum in `TRest` | Both hazards at once, and the narrowest case here. |

Cases 11-14 exist because nothing in the library preserves those enum types: `[DynamicallyAccessedMembers]` on
the query method's type parameter reaches the value tuple, and `ILLink.Descriptors.xml` reaches
`System.ValueTuple`1`-`8`, but neither reaches a type used as a tuple *field*. Their members survive because
the trimmer preserves the members of an enum it keeps ([dotnet/runtime#100814](https://github.com/dotnet/runtime/pull/100814),
[dotnet/runtime#105351](https://github.com/dotnet/runtime/pull/105351)). These cases are the regression guard
for that behaviour, which the library depends on but does not own.

Each case uses **its own** enum type, and no member of any of them is referenced anywhere in this project - the
rows are written with SQL literals, and the assertions compare the underlying integer (cases 13 and 14 also
compare `ToString()` against the member name as a string literal, which is what proves the *names* survived).
A static reference to a member would root it and make the case pass for the wrong reason.

Case 10 is layer 3 of the correctness design. A broken annotation chain is indistinguishable from it at the
point the materializer is built - reflection reports no writable property that any column matches - so the
guard covers both. It is also why the "broken chain, no guard, silent corruption" case cannot be reproduced
through the public API at all, which is the whole point of having the guard.

Cases 1-14 all run against SQLite, the one provider that needs no server. The other adapters are covered
for breadth by [`AllAdaptersConsumer`](../AllAdaptersConsumer) and against real databases by the integration
suite.

## A note on the model types

`Model.cs` deliberately uses plain classes, never records: a positional record's properties are init-only, so
the property-setter strategy case 2 exercises would not apply to them at all.

Reading a property in an assertion is fine. Under Native AOT, reflection metadata is a **separate output from
compiled code**: ILC emits a `PropertyInfo` only for members it decides are reflectable, and a direct call is
deliberately not evidence of that - if it were, every method in the closure would carry a name and a signature
in the binary. A statically rooted member therefore runs correctly and is still invisible to reflection, and
`GetProperties()` returns an empty array rather than throwing.

Measured on this toolchain, `net10.0`, 6 public properties per type:

| Rooted by | Native AOT | `PublishTrimmed`, no AOT |
|---|--:|--:|
| nothing | 0 | 0 |
| one property read by a direct call | **0** | 1 |
| a record's `ToString` and `Equals`, both invoked | **0** | 6 |
| `[DynamicallyAccessedMembers]` on a type parameter | 6 | 6 |

The record row is the sharpest one: `ToString()` returned its full member list - so every getter
demonstrably ran - and `GetProperties()` on that same type returned an empty array in the same process.

So rooting does not substitute for a preservation mechanism, and that mechanism does not have to be
`[DynamicallyAccessedMembers]`: `ILLink.Descriptors.xml`, `[DynamicDependency]`, and a literal
`typeof(X).GetProperties()` that the trimmer recognises intrinsically all work too. The last one is a trap when
re-running this measurement - obtain the `Type` opaquely, or the intrinsic fires and the members survive.

The right-hand column is why this is an **AOT** property and not a trimming property. IL trimming alone edits
ordinary assemblies in place, so a kept member keeps its metadata row and reflection does find it. This
consumer never publishes that way: the gate is `-p:PublishAot=true`, and the other leg is the untrimmed JIT
baseline.
