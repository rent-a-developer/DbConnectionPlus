# Native AOT and trimming

**Reference the package and publish. There is nothing to install and nothing to opt into** - no companion
package, no source generator, no attribute, no registration call. Everything below works in an application
published with `PublishAot` exactly as it does on the just-in-time compiler.

DbConnectionPlus targets `net8.0` and `net10.0`. `net8.0` is the supported floor; **`net10.0` is recommended**
for AOT, because from `net9.0` on the trim and AOT analyzers recognise `RuntimeFeature.IsDynamicCodeSupported`
as a feature guard and stop reporting code your own guard has already made unreachable. Either way this
library's own publish is warning-free on both — see [What you will see in your own
build](#what-you-will-see-in-your-own-build).

## What works

| Feature | Native AOT |
|---|---|
| `ExecuteNonQuery`, `ExecuteReader`, `Exists` | ✅ |
| `ExecuteScalar<T>` and scalar `Query<T>` | ✅ |
| `Query<T>` for entities - property setters *and* constructor injection (records, immutable entities) | ✅ |
| `Query<T>` for value tuples, including tuples with more than seven fields | ✅ |
| Non-generic `Query` / `QueryFirst` / … returning `DataRow`, read with `row["Id"]` | ✅ |
| `InsertEntity`, `UpdateEntity`, `DeleteEntity` and their bulk counterparts | ✅ |
| `TemporaryTable(...)` for scalar values and for complex objects | ✅ |
| Fluent-API mapping, `[Column]`/`[Key]` attributes, `EnumSerializationMode` | ✅ |
| `dynamic row.Id` member access on a `DataRow` | ❌ - use the `row["Id"]` indexer instead |

Mapping is somewhat slower under Native AOT, because reflection replaces the compiled expression tree. In the
[benchmark suite](../reference/performance.md) - an in-memory SQLite database, the worst case, because statement execution is
almost free there and nothing dilutes the mapping cost - querying entities takes **~1.31x** as long end to end
and querying value tuples **~1.35x**. Part of that is the ahead-of-time runtime rather than this library: the
raw `DbCommand` baseline in the same run slows by 1.08x and 1.12x respectively. Against a real database server,
where the query itself dominates, the difference is correspondingly smaller.

## Reading rows without a type: `row["Id"]`, not `row.Id`

The non-generic query methods return `DataRow`. The string indexer is the AOT-safe way to read a column and is
what the examples in this README use:

```csharp
var product = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
var name = product["Name"];
```

Member access through a `dynamic` reference still works wherever the runtime supports dynamic code generation,
but **not** under Native AOT - the Dynamic Language Runtime cannot bind without generating code. `DataRow`
itself is AOT-safe either way, and costs you nothing if you never write `dynamic`; the incompatibility is
reported by the compiler at your own call site. See [Query methods](querying.md#query-methods) for the full comparison.

## Supported providers

End-to-end AOT support is also bounded by your ADO.NET provider, which this library cannot fix:

| Database | Provider | Native AOT |
|---|---|---|
| SQLite | `Microsoft.Data.Sqlite` | ✅ Verified trim-clean, and the provider this library's own AOT smoke test runs against |
| MySQL | `MySqlConnector` | ✅ Fully managed and trim-friendly |
| SQL Server | `Microsoft.Data.SqlClient` | ✅ Publishes and runs clean. |
| PostgreSQL | `Npgsql` | ⚠️ Core is AOT-capable; some type plug-ins reflect |
| Oracle | `Oracle.ManagedDataAccess.Core` | ❌ Not AOT-ready. This is a limitation of the provider |

## What you will see in your own build

**No warnings.** Publishing with `PublishAot` or `PublishTrimmed` reports no `IL2xxx` and no `IL3xxx` diagnostic
for any scenario in the table above, on either target framework.

The public API carries no `[RequiresUnreferencedCode]` and no `[RequiresDynamicCode]`, so nothing is reported at
your call sites. The three underlying reflection sites are answered inside the library, where they occur:

| Site | How it is answered |
|---|---|
| Specializing the value converter over the column's type | The generic method declares no `[DynamicallyAccessedMembers]`, so a runtime specialization has no requirements trimming could fail to preserve |
| Compiling the expression tree | `[RequiresDynamicCode]` stays on the expression-tree materializer, and its only caller reaches it from inside a `RuntimeFeature.IsDynamicCodeSupported` branch that the AOT compiler removes |
| Finding the constructor of a nested value tuple | An `ILLink.Descriptors.xml` embedded in the package preserves `System.ValueTuple\`1`-`\`8`, so the constructors survive trimming |

This is verified rather than asserted: the repository publishes a Native AOT smoke test on `net8.0` and
`net10.0` and gates on **zero** IL diagnostics plus every asserted value coming back correctly, including
nested value tuples and enum fields inside them.
