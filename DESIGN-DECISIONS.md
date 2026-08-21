# DbConnectionPlus - Design Decisions

**Version:** 4.0.0
**Last updated:** August 2026
**Author:** David Liebeherr

This document describes the design **as it is now**, and why it is that way. It is not a change log - see
[CHANGELOG.md](CHANGELOG.md) for what changed between versions, and [README.md](README.md) for how to use the
library.

## Table of contents

1. [Design principles](#design-principles)
2. [Core architecture](#core-architecture)
3. [Technology choices](#technology-choices)
4. [Performance decisions](#performance-decisions)
5. [Native AOT and trimming](#native-aot-and-trimming)
6. [Database adapters](#database-adapters)
7. [SQL statements and the interpolated string handler](#sql-statements-and-the-interpolated-string-handler)
8. [Entity mapping](#entity-mapping)
9. [Temporary tables](#temporary-tables)
10. [Testing strategy](#testing-strategy)
11. [Configuration and extensibility](#configuration-and-extensibility)
12. [Trade-offs and alternatives](#trade-offs-and-alternatives)
13. [Future considerations](#future-considerations)

---

## Design principles

Everything below follows from six principles:

1. **Minimal abstraction overhead** - performance close to hand-written ADO.NET code
2. **Type safety** - compile-time validation of SQL parameters and mappings
3. **Multi-database support** - MySQL, Oracle, PostgreSQL, SQLite and SQL Server, extensible to any other
4. **Developer productivity** - interpolated string syntax for natural SQL authoring
5. **Explicit behaviour over implicit magic** - and standard patterns over custom frameworks
6. **Native AOT without an opt-in** - the same API, the same behaviour and no diagnostics to suppress in an
   application published with `PublishAot` or `PublishTrimmed`

---

## Core architecture

### Database adapters - Strategy pattern

Database-specific behaviour is isolated behind `IDatabaseAdapter`, which exposes an `IEntityManipulator` and an
`ITemporaryTableBuilder` alongside seven methods (parameter binding and naming, type mapping, identifier and
temporary-table quoting, temporary-table support, cancellation detection). See
[Database adapters](#database-adapters) for what each of them does.

Databases differ in parameter prefixes (`@`, `:`), temporary-table syntax, CLR-to-SQL type mapping and how a
cancelled statement surfaces as an exception. The alternatives - one adapter with runtime switches, an
inheritance hierarchy, or a configuration-driven mapping table - all either couple the databases together or
give up compile-time safety. Strategy keeps each adapter a self-contained, independently testable unit, and
adding a database needs no change to core.

### Materializer factories

`EntityMaterializerFactory` and `ValueTupleMaterializerFactory` build the delegates that map a `DbDataReader`
to objects. A factory is warranted because building one is expensive, the strategies differ per target type,
and the results have to be cached.

Each factory builds one of **two** implementations for the same result-set shape, chosen at run time by
`RuntimeFeature.IsDynamicCodeSupported`:

```csharp
// Just-in-time: a compiled expression tree, conceptually
Func<DbDataReader, Product> materializer = reader => new Product
{
    Id = reader.GetInt64(0),
    Name = reader.GetString(1),
    Price = reader.GetDecimal(2)
};

// Native AOT: the same bindings resolved once into a closure and replayed per row with reflection
```

Both use the typed `DbDataReader.GetXxx` methods where possible, minimizing boxing, and both produce identical
entities - down to exception types and messages, which the unit tests assert verbatim. See
[Native AOT and trimming](#native-aot-and-trimming) for why the second implementation exists.

**Cached per result-set shape, not per type.** The cache key (`MaterializerCacheKey`) is the entity type - or,
for tuples, the tuple's field types - **plus** the field names and field types the reader reports. `SELECT Id,
Name` and `SELECT *` need different delegates, and the same column arrives as `Int64` from SQLite and as
`Int32` from SQL Server; a cache keyed by type alone would hand back a materializer for the wrong shape.

The cache is a `ConcurrentDictionary<MaterializerCacheKey, Delegate>`, read through `TryGetValue` and populated
with an already-built value - never through a `GetOrAdd` *factory lambda*, because
`[DynamicallyAccessedMembers]` does not flow into a lambda. See
[Correctness under trimming](#2-correctness-under-trimming).

### The `DbDataReader` decorator

`ExecuteReader` is the one entry point that returns *before* the work is finished: the caller reads rows
afterwards, so the `DbCommand` and any temporary tables the statement created cannot be disposed when the
method returns. Every other method owns its command start to finish and disposes it itself.

`CommandDisposingDataReaderDecorator` wraps the provider's reader and does two things:

- **Disposal**: disposing the reader disposes the `DbCommandDisposer`, which disposes the command, the
  cancellation-token registration and every `TemporaryTableDisposer` the statement created - synchronously
  through `Dispose`, asynchronously through `DisposeAsync`.
- **Cancellation**: each provider signals a cancelled statement differently (SQL Server a `SqlException` with
  class 11, number 0, state 0; Oracle `ORA-01013`). `Read`/`ReadAsync` route the exception through
  `IDatabaseAdapter.WasSqlStatementCancelledByCancellationToken` and rethrow it as
  `OperationCanceledException`, so the caller writes one `catch` regardless of database.

Inheritance is not an option here: `DbCommand.ExecuteReader()` returns a provider-specific concrete type. The
decorator wraps whatever it gets without knowing that type.

---

## Technology choices

### Target frameworks: `net8.0` and `net10.0`

.NET 8.0 is the floor: LTS until November 2026, TieredPGO on by default, C# 12, and modern BCL APIs.

The second target exists because the trim and AOT analyzers are meaningfully better on the newer runtime: the
feature guard that lets `RuntimeFeature.IsDynamicCodeSupported` silence an `IL3050` was only recognised from
.NET 9 on, so the same source produces a different diagnostic set on the two targets. That is why **both** are
gated in CI rather than only the newer one - see [Native AOT and trimming](#native-aot-and-trimming).

The trade-off is no .NET Framework support, and a doubled build matrix in which a diagnostic has to be resolved
on both targets rather than on the one the developer happens to build.

### Dependencies

The **core package** takes exactly two runtime dependencies; everything else comes from `System.Data.Common`:

| Library | Purpose | Why |
|---|---|---|
| **LinkDotNet.StringBuilder** | stack-allocated string building | builds SQL without the intermediate strings a `StringBuilder` allocates |
| **Humanizer.Core** | ordinals in error messages ("the 3rd column …") | well-maintained, and only its `Ordinalize` is used |

Each **adapter package** adds exactly one: its ADO.NET provider (`Microsoft.Data.SqlClient`, `MySqlConnector`,
`Npgsql`, `Microsoft.Data.Sqlite` or `Oracle.ManagedDataAccess.Core`). That split is why the adapters are
separate packages at all - a consumer that uses SQLite does not reference an Oracle client. Analyzer packages
(ErrorProne.NET, Roslynator, the public-API analyzers) are `PrivateAssets="all"` and do not flow to consumers.

**Deliberately absent: anything that emits IL at run time.** Property access goes through
`System.Reflection.MethodInvoker` / `ConstructorInvoker`, and the temporary-table read path through the
library's own `EnumerableReader`. Libraries of the `Fasterflect` / `FastMember` family are faster to reach for
and work on the JIT, but they build accessors with `Reflection.Emit`, which does not exist under Native AOT,
and they are unannotated third-party assemblies that produce an assembly-wide `IL2104` in a consumer's trimmed
publish. **Do not add one back.**

`LangVersion` is `latest`; the floor for what the library actually uses is C# 12, which `net8.0` supplies. The
one language feature the design genuinely rests on is the **interpolated string handler** (C# 10).

---

## Performance decisions

### Expression-tree materializers, where the runtime can compile them

| Approach | Performance | Flexibility | Used? |
|---|---|---|---|
| Compiled expressions | fast | high | yes - the JIT path |
| Reflection | slowest | high | yes - the Native AOT path |
| Source generators | fastest | medium | no - see [below](#1-reflection-not-source-generation) |
| Manual mapping | fastest | low | no |

Compiled expressions work with any type including third-party ones, need no build-time step, come close to
hand-written code, and can be inspected when debugging.

**The one place this does not hold:** an application published with Native AOT cannot compile an expression
tree. `Expression.Compile()` does not fail there - it silently falls back to an **interpreter**, tens of times
slower per row and far heavier on allocations, which is the worst outcome because it looks like it works.
Materializers therefore dispatch on `RuntimeFeature.IsDynamicCodeSupported`, and the reflection implementation
behind that branch is a full counterpart rather than a degraded one.

### Stack allocation for string building

SQL is composed into a `ValueStringBuilder` (LinkDotNet.StringBuilder) over a stack-allocated buffer:

```csharp
using var builder = new ValueStringBuilder(stackalloc Char[500]);
```

- **500 characters** where a whole statement is built (512 in `DbCommandBuilder`), **100** where only an
  identifier is. `NameHelper` sizes its buffer to the name it is about to produce and falls back to the heap
  above 512 characters.
- `ValueStringBuilder` grows onto the heap by itself when a statement does not fit, so the size is an
  optimization, never a limit.
- The saving is the intermediate strings a `String`-concatenating builder would have produced: most statements
  are built with zero heap allocations, and therefore without GC pressure.

The cost is larger stack frames, bounded by keeping the buffers small enough that they cannot overflow the
stack.

### Caching: forever, and as late as possible

Three caches, all populated on first use and never evicted (see
[Caching strategy](#4-caching-strategy---forever-vs-lru) for why there is no eviction policy):

| Cache | Key | Built when |
|---|---|---|
| Materializers | entity type (or tuple field types) + reader field names and types | a result-set shape is first seen |
| Entity metadata | entity type | an entity type is first used |
| Property accessors | - | a property is first read or written |

The last one is the least obvious. `EntityPropertyMetadata.PropertyGetter` / `PropertySetter` are
`Func<Object, Object?>?` / `Action<Object, Object?>?` closures that create their `MethodInvoker` on the first
call and keep it afterwards. Building metadata for a type with 30 properties therefore costs nothing per
property until a property is actually touched, and a query that reads five columns of a wide entity never pays
for the other 25.

In all three cases the first operation pays the one-off construction cost and every subsequent one is a
dictionary hit.

---

## Native AOT and trimming

A consumer references the package and publishes. There is no companion package, no source generator, no opt-in
attribute, no registration call and no diagnostic to suppress.

### 1. Reflection, not source generation

Native AOT forbids run-time *code generation*, not reflection. So no mechanism in the library emits IL, except
the one that needs it purely for speed - and that one sits behind a run-time guard:

| Mechanism | How it works | Paths |
|---|---|---|
| Entity property accessors | `MethodInvoker` / `ConstructorInvoker` | **single path** |
| Temporary tables from complex objects | the library's `EnumerableReader` over those accessors | **single path** |
| Materializers | expression tree on the JIT, reflection under AOT, chosen by `RuntimeFeature.IsDynamicCodeSupported` | two paths, one behaviour |

The AOT compiler folds that condition to a constant, so a published application does not carry the
expression-tree implementation at all, and a JIT-compiled one never reaches the reflection path.

**A source-generator design was prototyped, benchmarked and rejected.** It would have discovered the concrete
types at opted-in call sites and emitted reflection-free mappers, registered through a generated
`[ModuleInitializer]`. It buys run-time performance on the mapping step and nothing else.

What the repository still measures is the price of the reflection path itself - the same cost a generator would
have removed. From the [benchmark suite](README.md#benchmarks), on in-memory SQLite, where statement execution
is nearly free and mapping is therefore the largest possible share of the total:

| Category, JIT → Native AOT | End to end | Of which the runtime itself (raw `DbCommand` baseline) |
|---|---|---|
| `Query_Entities` | 1.31x | 1.08x |
| `Query_ValueTuples` | 1.35x | 1.12x |
| `TemporaryTable_ComplexObjects` | 1.22x | 1.17x |

Against a real database server the same absolute difference is a much smaller share of the total.

**What rejecting it bought:** nothing for consumers to do; no "type the generator never discovered" failure
mode, and no analyzer needed to detect one; two fewer permanently divergent code paths, because accessors and
the temporary-table reader stayed single-path; no third-party emit-based dependency and therefore no
assembly-wide `IL2104` in a consumer's publish; no Roslyn version-skew risk; and fluent-API configuration keeps
working with no special handling, because everything resolves at run time from the live entity metadata.

**What it costs:** the table above. This is not a dead end - generated mappings remain layerable on top later
as a pure optimization, because reflection is then a working fallback rather than a blocker.

### 2. Correctness under trimming

**This is the reason the design has a run-time guard rather than only annotations.**

Under trimming, if no `[DynamicallyAccessedMembers]` annotation told the trimmer to keep an entity's members,
`Type.GetProperties()` returns **fewer members - or none - with no error**. The library then binds no columns
and returns a default-valued entity. Measured: **6 columns of real data in, 0 bound, no exception.**

Three layers defend this, and all three are mandatory:

| Layer | What it is | What it depends on |
|---|---|---|
| 1 | `[DynamicallyAccessedMembers]` on every entry point that reaches reflection, flowed down to `EntityHelper.GetEntityTypeMetadata` | the annotations being complete |
| 2 | **No `IL2xxx` suppression on the entity path, and none at all at a public API** | developer discipline |
| 3 | The zero-binding guard: if a result set binds no writable property, throw | nothing |

Layer 2 is the project rule that keeps layer 1 honest: the `IL2xxx` warnings are the *only* build-time proof
that the annotation chain is complete, so silencing one where entity types flow silently voids the guarantee.
The default answer is to restructure. The annotation does not flow through a `ConcurrentDictionary.GetOrAdd`
lambda, for instance, so both the materializer cache and `EntityHelper.GetEntityTypeMetadata` build their value
first and pass it to `GetOrAdd` rather than suppressing the resulting warning.

The library contains exactly **two** `IL2xxx` suppressions, both on the value-tuple path and neither anywhere
near an entity type; [decision 4](#4-no-consumer-facing-diagnostics) sets out what makes each true and what
guards it. A third one is a regression, not a precedent.

Layer 3 is the only layer that does not depend on humans, and it is what a source generator would have given
for free: with a registry, an undiscovered type fails loudly. Measured under Native AOT:

| Case | Columns bound | Outcome |
|---|---|---|
| Broken annotation chain, **no** guard | 0 | **silent corruption** - default-valued entity, no exception |
| Broken annotation chain, **with** guard | - | **throws** |
| Correct annotation chain, with guard | 6 | **OK** - no false positive |

The guard also fixed a latent bug on the JIT: a result set matching no property previously returned
default-valued objects, so a typo in a `SELECT` alias produced a sequence of empty objects with no error.

⚠️ **All three cases pass on the JIT.** Nothing is trimmed there, so the entire unit and integration suite
passes with a broken annotation chain. That is why verification lives in a natively published smoke test - see
[Testing strategy](#testing-strategy) - and why that job must never be disabled or made non-blocking.

### 3. Where annotations run out: nested value tuples

`[DynamicallyAccessedMembers]` is not recursive. A value tuple with more than seven fields is represented by
the runtime as a *nested* value tuple, so the annotation on `Query<T>`'s type parameter preserves the outermost
type and says nothing about the inner one - which the trimmer then removes, fields and constructor alike.

Two things were needed, and only together:

- **Traverse through `Type.GetGenericArguments()`**, not `FieldInfo.FieldType` and `GetFields()`. The generic
  arguments of a value tuple *are* its field types, in field order, and they are type metadata that cannot be
  trimmed away.
- **Ship an embedded `ILLink.Descriptors.xml`** preserving the members of `System.ValueTuple`1` through
  `System.ValueTuple`8`. The traversal makes the nested types discoverable; only the descriptor makes their
  constructors survive.

A descriptor **preserves** members rather than silencing a diagnostic, so it does not conflict with layer 2. It
is also what made decision 4 possible: once the members are guaranteed by a mechanism that ships inside the
assembly, the one remaining diagnostic on this path can be answered where it occurs instead of being reported
to every consumer.

### 4. No consumer-facing diagnostics

The generic query methods carry **no** `[RequiresUnreferencedCode]` and **no** `[RequiresDynamicCode]`, so a
consumer publishing with `PublishAot` or `PublishTrimmed` sees no diagnostic at a call site.

Putting the attributes on them was considered and rejected. Carrying both would make every consumer publishing
with `PublishAot` or `PublishTrimmed` see an `IL2026` and an `IL3050` at *every* call site, including callers
that only ever query an entity. The argument for them rested on the nested value-tuple traversal being
un-analyzable, which decision 3 removes. Without them, the analyzers report exactly three distinct sites in the
library, all inside the two materializer factories, and each is answered where it occurs:

| Diagnostic | Where | How it is answered |
|---|---|---|
| `IL2060` | `MakeGenericMethod` specializing `ValueConverter.ConvertValueToType<TTarget>` (expression path) | suppressed at a one-line helper, `MaterializerFactoryHelper.MakeValueConverterConvertValueToTypeMethod`, that exists only to scope it. That method declares **no** `[DynamicallyAccessedMembers]` on `TTarget`, so the specialization has no requirements trimming could fail to preserve |
| `IL3050` | `Expression.Lambda` and `MakeGenericMethod` (expression path) | `[RequiresDynamicCode]` stays on the expression-tree materializer, where it is true. Its only caller reaches it from inside the `RuntimeFeature.IsDynamicCodeSupported` dispatcher, which `net9.0`+ recognises as a feature guard. `net8.0` needs a suppression on the dispatcher because its reference assembly lacks that annotation |
| `IL2065` | `GetConstructor` on a nested value tuple type (reflection path) | suppressed in `GetValueTupleConstructors`, justified by the embedded descriptor from decision 3, the caller's value-tuple validation, a unit test asserting the descriptor still lists all eight arities, and native smoke cases that materialize nested tuples for real |

**This is what layer 2 forbids and what it does not.** A suppression at the *public API* would hide a broken
`[DynamicallyAccessedMembers]` chain for a consumer's entity types - a real, measured failure mode. That is out
of bounds and untouched. The two sanctioned suppressions cover value-tuple **BCL** metadata: a closed set of
eight framework types, preserved by a shipped descriptor, guarded by a unit test and by native smoke cases.

**The `net8.0` `IL3050` suppression is a transcription, not an assertion.** The `net10.0` inner build compiles
the same source *without* it. That is why both target frameworks are gated in CI: the newer one verifies the
reasoning the older one has to state by hand.

**Why bother.** A warning a consumer cannot act on, and that does not correspond to any way their application
can break, trains them to ignore the warnings that do. The smoke test gates on zero diagnostics from anywhere,
so it fails immediately if a public API gains one of these attributes. If any of the three answers above stops
holding, the correct response is to put `[RequiresUnreferencedCode]` on the query methods, not to leave an
untrue suppression in place.

### 5. `DataRow` instead of `dynamic` for untyped result sets

The non-generic query methods return `DataRow`, whose string indexer - `row["Id"]` - is the documented way to
read a column. Member access through a `dynamic` reference still works on the JIT.

`dynamic` cannot work under Native AOT: the Dynamic Language Runtime binds call sites by generating code. That
is a fact about the consumer's own call site, and the C# compiler reports it there - the right place for it.
What the library must not do is charge every consumer for it. Two choices follow:

- **`DataRow` implements `IDynamicMetaObjectProvider`; it does not derive from `DynamicObject`.** As of .NET 10
  the `DynamicObject` **constructor** is `[RequiresDynamicCode]`, so a base class would push that attribute
  onto `DataRow`'s constructor and from there onto all ten non-generic query methods - reported even to a
  caller who only ever writes `row["Id"]`. The interface carries no such annotation.
- **`DataRowMetaObject` binds to a plain delegate that reads or writes the column**, never through
  `Expression.Lambda` / `LambdaExpression.Compile`, which are `[RequiresDynamicCode]` and would reintroduce
  exactly the problem the interface avoids.

**The consequence for behaviour is deliberate:** through a `dynamic` reference a *property* always addresses a
column, so `row.Count` reads the column named `Count` and throws `KeyNotFoundException` if there is none, while
*method* calls still resolve against `DataRow` itself. `row.X` and `row["X"]` are then the same operation in
every case, rather than the row's own members shadowing columns that share their name.

---

## Database adapters

### Responsibilities

Each `IDatabaseAdapter` implementation handles:

| Member | What it does |
|---|---|
| `BindParameterValue` | type conversions (`DateOnly` → `DateTime` for Oracle, `Guid` → string for Oracle), enum serialization |
| `FormatParameterName` | `@Name` for SQL Server, `:Name` for Oracle |
| `GetDataType` | CLR → SQL type mapping, e.g. `DateTime` → `DATETIME2` (SQL Server) vs `DATE` (Oracle) |
| `QuoteIdentifier` | `[Name]` (SQL Server), `` `Name` `` (MySQL), `"Name"` (PostgreSQL / Oracle / SQLite) |
| `QuoteTemporaryTableName` | separate from `QuoteIdentifier` because a temporary table is not always quoted like an ordinary one - SQL Server's `#Name` carries its scope in the name |
| `SupportsTemporaryTables` | lets an adapter refuse the feature. Oracle returns `false` unless `OracleDatabaseAdapter.AllowTemporaryTables` is set, because creating or dropping a private temporary table implicitly commits the caller's transaction |
| `EntityManipulator` | CRUD SQL generation, generated-key readback, concurrency checks |
| `TemporaryTableBuilder` | temporary-table creation and population |
| `WasSqlStatementCancelledByCancellationToken` | maps a provider exception to a cancellation - SQL Server class 11 / number 0 / state 0, Oracle `ORA-01013` |

### Registration

Each adapter ships as its own NuGet package and is registered explicitly at startup, through a `UseXxx()`
extension method in the `RentADeveloper.DbConnectionPlus.Configuration` namespace, so it appears on the
configuration object without an extra `using`:

```csharp
public static DbConnectionPlusConfiguration UseSqlite(this DbConnectionPlusConfiguration configuration)
{
    ArgumentNullException.ThrowIfNull(configuration);

    configuration.RegisterDatabaseAdapter<SqliteConnection>(new SqliteDatabaseAdapter());

    return configuration;
}
```

```csharp
DbConnectionExtensions.Configure(config => config.UseSqlServer().UseSqlite());
```

`RegisterDatabaseAdapter<TConnection>` stores the adapter against `typeof(TConnection)`. Every extension method
resolves the adapter from the runtime type of the `DbConnection` it was called on; an unregistered connection
type throws `InvalidOperationException` naming the type rather than failing later with something obscure.
Registration happens inside `Configure`, which freezes the configuration when it returns, so the map is
read-only for the rest of the process and the lookup needs no synchronization - see
[Global configuration](#global-configuration).

**Why explicit registration:**

- **Only the packages you use are referenced.** A SQLite consumer does not drag in
  `Oracle.ManagedDataAccess.Core` - which matters for deployment size generally, and under Native AOT
  specifically, because every referenced assembly enters the compilation closure.
- **No assembly scanning.** Discovery by reflecting over loaded assemblies is exactly the pattern Native AOT
  and trimming cannot follow: an adapter reached only that way can be trimmed away, and the failure is a
  missing registration at run time.
- **The same mechanism serves custom adapters.** There is no built-in/third-party asymmetry: implement
  `IDatabaseAdapter` (plus an `IEntityManipulator` and an `ITemporaryTableBuilder`), call
  `RegisterDatabaseAdapter<MyConnection>`, and optionally wrap that in a `UseMyDatabase()` extension method -
  which is all the built-in adapters are. The [README](README.md#custom-database-adapter) carries a worked
  example.

**Trade-off:** one line of startup configuration that a static auto-registering registry would not need, and a
run-time rather than compile-time error if it is forgotten.

---

## SQL statements and the interpolated string handler

### Why a handler

```csharp
// Unsafe
var sql = "SELECT * FROM Product WHERE Id = " + productId;

// Safe but error prone
command.CommandText = "SELECT * FROM Product WHERE Id = @Id";
command.Parameters.AddWithValue("@Id", productId);

// Safe and concise
connection.Query<Product>($"SELECT * FROM Product WHERE Id = {Parameter(productId)}");
```

`InterpolatedSqlStatement` is an `[InterpolatedStringHandler]`, so the compiler turns an interpolated string
passed to a query method into `AppendLiteral` / `AppendFormatted` calls at the call site. That gives
compile-time type checking, no run-time parsing and no reflection to recover parameters, and makes injection
safety **structural**: a value reaches the database as a `DbParameter` because it arrived as a fragment, not
because a string was escaped correctly.

The alternative - parsing the composed string at run time - costs a parser on every statement, gives up
compile-time validation, and turns injection safety into a property of how complete the parser is.

### Fragments

A statement is stored as an ordered list of immutable fragments rather than as a concatenated string. All four
fragment types are records implementing the internal marker interface `IInterpolatedSqlStatementFragment`:

| Fragment | Visibility | What it is |
|---|---|---|
| `Literal` | internal | raw SQL text - everything between the holes |
| `InterpolatedParameter` | public | a value captured by `Parameter(value)`, carrying the name inferred from the call site |
| `InterpolatedTemporaryTable` | public | a sequence captured by `TemporaryTable(values)`, replaced by the name of the table created for the statement |
| `Parameter` | internal | an explicitly named parameter, for the non-interpolated constructor |

```csharp
connection.Query<Product>($"SELECT * FROM Product WHERE Id = {Parameter(productId)}");

connection.Query<Product>(
    $"SELECT * FROM Product WHERE Id IN (SELECT Value FROM {TemporaryTable(productIds)})"
);

connection.Query<Product>(
    new InterpolatedSqlStatement("SELECT * FROM Product WHERE Id = @Id", ("Id", productId))
);
```

A plain `String` also converts implicitly (or explicitly through `FromString`), which is what makes
`connection.Query<Product>("SELECT * FROM Product")` compile into a single `Literal`.

Fragments buy **deferred concatenation** (the string is built only when needed - execution, logging, the debug
view), **inspection** before execution (which is what `InterceptDbCommand` and the temporary-table builder work
from), and a readable `InterpolatedSqlStatementDebugView` that shows SQL, parameters and temporary tables
apart. The cost is a small allocation against a monolithic string.

`InterpolatedSqlStatement` is a `struct` because the compiler creates one per interpolated string at the call
site; a class would allocate on every statement, which is precisely the overhead this design exists to avoid.

### Parameter name inference

Names come from `[CallerArgumentExpression]`:

```csharp
public static InterpolatedParameter Parameter(
    Object? value,
    [CallerArgumentExpression(nameof(value))] String? parameterValueExpression = null
)
```

`NameHelper.CreateNameFromCallerArgumentExpression` strips the leading noise words `this.`, `new` and `Get`,
drops every character that is not a letter, a digit or an underscore, uppercases the first character and
truncates to the maximum length - **60 characters**, which every major database supports. It writes into a
stack-allocated buffer, so inferring a name allocates only the resulting string.

| Expression | Name |
|---|---|
| `Parameter(productId)` | `ProductId` |
| `Parameter(product.Id)` | `ProductId` |
| `Parameter(user.Orders[0].Total)` | `UserOrders0Total` |
| `Parameter(42)` - nothing usable left | `Parameter_1`, `Parameter_2`, … assigned when the command is built |

The same helper names temporary tables, which is why `TemporaryTable(orderItems)` produces
`OrderItems_<guid>`.

---

## Entity mapping

### Attributes and fluent configuration

Standard .NET data annotations are supported - all of `[Table]`, `[Column]`, `[Key]`, `[DatabaseGenerated]`,
`[Timestamp]`, `[ConcurrencyCheck]` and `[NotMapped]` - rather than a custom attribute library, because they
are already documented, discoverable and shared with EF Core and other tools.

```csharp
[Table("Products")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Int64 Id { get; set; }

    [Column("ProductName")]
    public String Name { get; set; } = "";

    [Timestamp]
    public Byte[]? Version { get; set; }

    [NotMapped]
    public Decimal TotalPrice => UnitPrice * Quantity;
}
```

Without `[Table]` the table name is the entity type's name as written - no pluralization, no singularization.
Without `[Column]` the column name is the property name.

The same mapping is available fluently, for types you cannot or do not want to annotate - types from another
assembly, generated types, or a domain model kept free of persistence attributes:

```csharp
DbConnectionExtensions.Configure(config =>
{
    config.Entity<Product>().ToTable("Products");
    config.Entity<Product>().Property(a => a.Name).HasColumnName("ProductName");
    config.Entity<Product>().Property(a => a.Id).IsKey().IsIdentity();
    config.Entity<Product>().Property(a => a.DiscountedPrice).IsComputed();
    config.Entity<Product>().Property(a => a.IsOnSale).IsIgnored();
    config.Entity<Product>().Property(a => a.Version).IsRowVersion();
});
```

`EntityTypeBuilder<TEntity>` offers `ToTable` and `Property`; `EntityPropertyBuilder` offers `HasColumnName`,
`IsKey`, `IsIdentity`, `IsComputed`, `IsRowVersion`, `IsConcurrencyToken` and `IsIgnored`, each returning
itself so the calls chain. The shape follows EF Core's, so it needs no separate mental model.

**Precedence:** a fluent mapping wins over data annotations. Configuring an entity type fluently makes the
attributes on that type irrelevant; configuring a single property fluently makes the attributes on that
property irrelevant.

### Entity metadata

`EntityHelper.GetEntityTypeMetadata` derives one `EntityTypeMetadata` record per entity type and caches it:

```csharp
public static EntityTypeMetadata GetEntityTypeMetadata(
    [DynamicallyAccessedMembers(EntityMemberTypes)] Type entityType)
{
    ArgumentNullException.ThrowIfNull(entityType);

    if (entityTypeMetadataPerEntityType.TryGetValue(entityType, out var entityTypeMetadata))
    {
        return entityTypeMetadata;
    }

    return entityTypeMetadataPerEntityType.GetOrAdd(entityType, CreateEntityTypeMetadata(entityType));
}
```

Note the `[DynamicallyAccessedMembers]` annotation, and that the metadata is **not** created inside a `GetOrAdd`
*factory lambda*: the annotation does not flow into a lambda, so `entityType` would arrive at
`CreateEntityTypeMetadata` unannotated and the trimmer would be free to drop the entity's members. Reading
through `TryGetValue` first also keeps the hot path free of both the allocation and the reflection. The
`IL2xxx` the lambda version reports is answered by restructuring, not by a suppression - see
[layer 2](#2-correctness-under-trimming).

The record holds the table name; all instance properties, and the same set indexed by property name; the
mapped, key, identity, computed, database-generated, row-version and concurrency-token subsets; and the insert
and update projections the CRUD statements are built from, so an adapter does not re-derive them per call. The
per-property reflection accessors are *not* built here - see
[Caching](#caching-forever-and-as-late-as-possible).

### Constructor vs. property initialization

A materializer first looks for a constructor whose parameters match the columns of the result set by name and
compatible type; failing that it uses the parameterless constructor and property setters; failing both it
throws. Constructor injection is what makes records and other immutable entity types work, and the
property-setter path keeps traditional mutable classes working unchanged. Neither imposes a base type or an
attribute.

Both materializer implementations - the expression tree and the reflection counterpart - support both
strategies and choose between them the same way, so an entity behaves identically on the two runtimes.

### Optimistic concurrency

| Kind | Configure with | Who writes the value |
|---|---|---|
| Row version | `[Timestamp]` / `IsRowVersion()` | the database, on every insert and update |
| Concurrency token | `[ConcurrencyCheck]` / `IsConcurrencyToken()` | the application |

When an entity type has either kind, `UpdateEntity`, `UpdateEntities`, `DeleteEntity` and `DeleteEntities` add
the original values to the `WHERE` clause alongside the key. A row whose token changed since the entity was
read therefore matches nothing, the statement affects fewer rows than expected, and the library throws
`DbUpdateConcurrencyException`.

**Why an exception rather than a return value:** a silently ignored update is the failure this feature exists
to prevent, and a returned row count is easy to ignore. `DbUpdateConcurrencyException` carries the offending
`Entity`, so a caller that wants a "reload and merge" flow catches it and has the instance in hand; a caller
that does not gets a loud failure instead of lost data. Row versions and other database-generated values are
read back onto the entity after a successful insert or update, so the in-memory instance stays usable.

---

## Temporary tables

Passing a collection to a query otherwise means an `IN` clause (limited to small collections), one query per
item (round-trips), XML/JSON parameters (parsing overhead, version dependencies) or table-valued parameters
(SQL Server-specific). DbConnectionPlus creates a temporary table on the fly instead:

```csharp
var products = connection.Query<Product>(
    $"""
    SELECT  *
    FROM    Product
    WHERE   Id IN (SELECT Value FROM {TemporaryTable(productIds)})
    """
);
```

### Lifecycle

1. `TemporaryTable(values)` captures the collection reference and a name inferred from the call site.
2. `DbCommandBuilder` finds the temporary-table fragments of the statement, and for each one generates a unique
   name (the inferred name, or `Values`, plus a GUID suffix), infers the schema from the element type - a
   single `Value` column for scalars, one column per mapped property for complex objects - calls
   `ITemporaryTableBuilder.BuildTemporaryTable()` and populates the table.
3. The fragment is replaced by the quoted table name in the SQL.
4. Every command is built together with a `DbCommandDisposer` owning the command, the cancellation-token
   registration and the statement's `TemporaryTableDisposer` instances. Disposing it drops the tables.

`ExecuteNonQuery`, `ExecuteScalar`, `Exists`, `Query` and friends dispose it in a `finally`, so cleanup happens
even if the statement throws. `ExecuteReader` cannot - the caller reads rows after the method returns - so its
reader is wrapped in `CommandDisposingDataReaderDecorator` and disposing the reader disposes the same object.
That is why the reader returned by `ExecuteReader` must be disposed, and why `await using` works as well as
`using`.

**GUID suffixes rather than a sequence or a timestamp**, because they are unique across connections without a
shared counter, which keeps the naming thread-safe and collision-free under connection pooling. The names stay
within every database's identifier limits.

### Per-database implementation

| Database | Create | Populate |
|---|---|---|
| MySQL | ``CREATE TEMPORARY TABLE `ProductIds_abc123` (`Value` BIGINT)`` | `MySqlBulkCopy` (`LOAD DATA LOCAL INFILE`; needs `AllowLoadLocalInfile=true` in the connection string and `local_infile` on the server) |
| Oracle | `CREATE PRIVATE TEMPORARY TABLE "ORA$PTT_ProductIds_abc123" ("Value" NUMBER(19))` | one parameterized `INSERT`, re-executed per row |
| PostgreSQL | `CREATE TEMP TABLE "ProductIds_abc123" ("Value" bigint)` | `COPY … FROM STDIN (FORMAT BINARY)` through `NpgsqlBinaryImporter` |
| SQLite | `CREATE TEMP TABLE "ProductIds_abc123" ("Value" INTEGER)` | one parameterized `INSERT` into `temp."…"`, re-executed per row - the database is in-process, so there is no round-trip to amortize |
| SQL Server | `CREATE TABLE #ProductIds_abc123 (Value BIGINT)` | `SqlBulkCopy` |

Oracle's private temporary table name must carry the server's `private_temp_table_prefix`, which
`QuoteTemporaryTableName` reads from `v$parameter` rather than hard-coding.

**No silent fallback.** A failing bulk insert propagates; it is not retried row by row behind the caller's
back. The one exception it does translate is cancellation - SQL Server's `OperationAbortedException` becomes
`OperationCanceledException`, matching every other cancellation path in the library.

**One reader feeds all five.** The bulk-copy APIs and the `INSERT` loops both consume an `EnumerableReader`:  a
`DbDataReader` implementation over an `IEnumerable`, exposing a single `Value` column for scalars or one column
per mapped readable property for complex objects. Nothing materializes the sequence into an intermediate table
or array first, and the same code runs on the JIT and under Native AOT.

---

## Testing strategy

### Four tiers

| Tier | What it is | Scale |
|---|---|---|
| **Unit tests** (`DbConnectionPlus.UnitTests`) | core logic in isolation, `DbConnection` / `DbDataReader` substituted with NSubstitute | ~3,270 executed plus ~200 skipped per target framework, in a few seconds - which is what makes them the default verification loop |
| **Integration tests** (`DbConnectionPlus.IntegrationTests`) | real databases: Testcontainers-managed containers for MySQL, Oracle, PostgreSQL and SQL Server, SQLite in-process | ~600 s for all five, ~90 s for the default SQLite + SQL Server pair |
| **Package-consumption tests** (`tests/package-consumption/`) | console apps consuming the **packed packages**, not the projects. `AotConsumer` is published with Native AOT; `AllAdaptersConsumer` installs all six packages and builds on the .NET 8 SDK alone, which is what makes the documented `net8.0` floor a checked fact | two CI gates |
| **Benchmarks** (`DbConnectionPlus.Benchmarks`) | regression detection against a raw `DbCommand` baseline and against Dapper | BenchmarkDotNet |

### Unit tests

**Table-driven conversion tests.** `ValueConverter`'s matrix of source and target types is covered by
`[Theory]` / `[MemberData]` cases that assert `CanConvert` and the conversion itself from the same row, so a
new supported conversion is one table entry rather than two tests that can drift apart.

**Null-guard verification.** `ArgumentNullGuardVerifier` (from `RentADeveloper.ArgumentNullGuards`) re-invokes
a call once per reference-typed parameter with that parameter set to `null` and asserts an
`ArgumentNullException` naming it - so adding an unguarded parameter fails an existing test instead of needing
a new one.

**The public surface is not tested - it is declared.** Every shipping project carries `PublicAPI.Shipped.txt`
and `PublicAPI.Unshipped.txt`, and `Microsoft.CodeAnalysis.PublicApiAnalyzers` turns an undeclared public
member into `RS0016` and a declared-but-vanished one into `RS0017`. With `TreatWarningsAsErrors=true` that is a
build error in all six projects on both target frameworks, so an accidental break cannot compile, let alone
reach a test run. `scripts/update-public-api.ps1` records a deliberate change.

### Integration tests

Each database has an `ITestDatabaseProvider` implementation that creates the test database, runs the setup
scripts, hands out connections and cleans up. Every test method gets a fresh connection, except for SQLite,
which runs in-process.

**The suite starts its own databases.** [Testcontainers](https://dotnet.testcontainers.org/) runs the four
servers; the container definitions are the fixtures in
`tests/DbConnectionPlus.IntegrationTests/TestDatabase/Containers/`. There is no compose file to bring up, no
`testconfig.json`, and no `ConnectionString_*` environment variable: each fixture builds its connection string in
code from the free host port Docker published its container on, which is what removes both the port collision
with a locally installed server and the second set of connection strings CI used to carry. CI declares no service
containers either - it runs the same code path a developer does, so a container configured wrong fails in both
places or in neither.

**Started per database system, on demand.** A container would be wasted on a filtered run, so
`IntegrationTestsBase<T>` declares an `IClassFixture<TestDatabaseFixture<T>>` it never reads: xUnit creates a
class fixture immediately before the first test of a class and awaits its `InitializeAsync`, which is both late
enough to skip database systems the run does not touch and early enough for the constructor of the test class to
open a connection. The container behind it is shared by every test class of that database system and removed by
an assembly fixture when the run ends.

**What that costs.** Every run now starts from a freshly created server rather than from whatever a long-lived
compose stack had accumulated, and pays the startup. Measured against the numbers this suite used to record, the
full matrix went from 533 s to 597 s - about a minute for four containers (PostgreSQL 4.7 s, SQL Server 11.1 s,
MySQL 19.8 s, Oracle 23.9 s).
That is also why the Oracle fixture pins the `faststart` image variant, whose database is already created, over
the plain one that spends minutes creating `FREEPDB1` on first start.

Scoping rules and measured per-provider timings:
[`.agents/skills/integration-db/SKILL.md`](.agents/skills/integration-db/SKILL.md).

### The Native AOT smoke test

**Why a console application rather than more unit tests:** the defect it guards against is invisible to every
other test in the repository. Nothing is trimmed on the JIT, so a broken `[DynamicallyAccessedMembers]` chain
passes the entire unit and integration suite. Only a natively published binary can see it.

`tests/package-consumption/AotConsumer` installs the SQLite adapter package, is published with
`-p:PublishAot=true` and executed in a CI job with no database containers, on **both** target frameworks -
they produce different diagnostics, so gating only the newer one would miss regressions on the documented
floor. It runs locally too:

```bash
pwsh -File scripts/verify-package-aot.ps1 -Pack
```

**Why it consumes the package rather than referencing the project:** the DAM annotations, the embedded
`ILLink.Descriptors.xml` and the `[assembly: AssemblyMetadata("IsTrimmable", "True")]` marker all have to
survive `dotnet pack`, and a project reference hands the trimmer the freshly compiled assembly instead of the
one a consumer installs. A packaging mistake that dropped any of them would leave a project-referenced check
green while every consumer's trimmed build silently bound nothing.

**What it asserts:** every property of every materialized entity, never row counts - silent trimming damage
does not remove rows, it empties them. It covers `InsertEntity`, `Query<T>` for entities under both
materialization strategies, a second `SELECT` shape (materializers are cached per result-set shape, so one
correct shape does not imply another), value tuples including the nested case, `DataRow` and its indexer, both
kinds of temporary table, and the zero-binding guard.

It also covers enums as value-tuple fields - flat and nested, converted from an integer and parsed by name -
each with its own enum type whose members are never referenced in source. Nothing in the library preserves
those types: DAM reaches the tuple, the descriptor reaches `System.ValueTuple`1`-`8`, and neither reaches a
type used as a tuple *field*. Their members survive because the trimmer preserves the members of an enum it
keeps ([dotnet/runtime#100814](https://github.com/dotnet/runtime/pull/100814),
[dotnet/runtime#105351](https://github.com/dotnet/runtime/pull/105351)) - runtime behaviour the library depends
on but does not own, and therefore worth a regression guard.

**The warning gate is zero diagnostics, from anywhere** - the library, an adapter, a package in the closure, or
the consumer's own call sites. Because the query methods carry neither `[RequiresUnreferencedCode]` nor
`[RequiresDynamicCode]` (see [decision 4](#4-no-consumer-facing-diagnostics)), this program is a faithful
sample of what a consumer sees; any diagnostic at all is a regression, and one at its own call sites means a
public API has gained one of those attributes.

Two details keep the test honest. Its model types are plain classes, never records: a positional record's
compiler-generated `ToString`/`Equals` reference every property, which would root them and make the test pass
for the wrong reason. Asserting property values is nonetheless safe, because a statically rooted member is
still invisible to reflection unless an annotation preserved it - measured, not assumed.

### Benchmarks

Each category has a `_Command` variant (hand-written ADO.NET, the baseline), a `_DbConnectionPlus` variant and,
where a comparable API exists, a `_Dapper` one. **Everything runs on in-memory SQLite**, the worst case for a
library like this: the statement itself costs almost nothing, so the library's overhead is the largest possible
share of the measurement.

**Two jobs, and only three categories in the second.** Everything runs on the JIT; `Query_Entities`,
`Query_ValueTuples` and `TemporaryTable_ComplexObjects` additionally run as a Native AOT binary, because they
are the only categories that reach the `RuntimeFeature.IsDynamicCodeSupported` branch. Every other category
executes identical IL on both runtimes, so a second row would compare RyuJIT with ILC rather than say anything
about this library. Dapper cannot appear in the AOT job at all - it builds its materializers with
`Reflection.Emit` - so the AOT comparison uses the Dapper.AOT source generator where it applies and the raw
baseline alone where it does not.

Tracked: execution time (mean, error, standard deviation), allocations and allocation ratio, and the ratio
against the `_Command` baseline **within one job**. The JIT-versus-AOT comparison is read from the `Mean`
column of the two rows for the same method.

⚠️ **The benchmarks are not a trimming check.** BenchmarkDotNet reports a benchmark that returned
default-valued entities as a *fast* benchmark, not a broken one. Only the smoke test asserts values. Details:
[the benchmark suite's README](benchmarks/DbConnectionPlus.Benchmarks/README.md).

---

## Configuration and extensibility

### Global configuration

One `Configure` entry point covers everything global - settings, entity mappings and adapter registrations -
and is callable **once**, at application startup:

```csharp
DbConnectionExtensions.Configure(config =>
{
    config.UseSqlServer();
    config.EnumSerializationMode = EnumSerializationMode.Integers;
    config.InterceptDbCommand = (command, temporaryTables) => logger.LogDebug("{Sql}", command.CommandText);
});
```

The configuration is frozen (`IFreezable`) when `Configure` returns, so **any** later mutation throws
`InvalidOperationException` - whether it comes from a second `Configure` call or from a builder captured during
the first. Every read afterwards is lock-free, which is what keeps the per-statement path free of
synchronization. `Configure` also clears the entity metadata cache, because a fluent mapping has to win over
metadata already derived from attributes or conventions.

Configure before the first database operation: the settings are read while a command is built, so a change
part-way through a process would otherwise apply to some statements and not others - exactly the race the
freeze prevents.

---

## Trade-offs and alternatives

### 1. Extension methods vs. a fluent query builder

**Chosen:** extension methods on `DbConnection`. They work with existing connection objects without a wrapper,
are a familiar .NET pattern, allocate nothing extra, and let the SQL read like SQL through interpolated
strings.

A `QueryBuilder(connection).Select("*").From("Product").Where(…)` API would enable more sophisticated
programmatic query building, at the cost of verbosity and a learning curve.

### 2. Sync + async vs. async-only

**Chosen:** both, named `Query()` / `QueryAsync()`. Enterprise codebases still have synchronous code paths, and
async machinery is overhead in CPU-bound scenarios. This doubles the API surface.

### 3. Static global state vs. dependency injection

**Chosen:** one process-wide `DbConnectionPlusConfiguration.Instance`, populated through
`DbConnectionExtensions.Configure(…)` - enum serialization mode, command interceptor, entity mappings and
registered adapters all live there.

It needs no DI container and no ceremony to get started, and the library's entry points are extension methods
on `DbConnection`, which has nowhere to carry a scope. Because it is frozen after configuration, reading it
from any thread is safe without locking - and that is also why configuration belongs in application startup
rather than anywhere a request can reach.

Optional DI support (`services.AddDbConnectionPlus(…)`) remains possible later, for the rare case of per-scope
settings.

### 4. Caching strategy - forever vs. LRU

**Chosen:** cache materializers, entity metadata and property accessors forever, with no eviction.
Applications work with a finite set of entity types and result-set shapes, the per-entry footprint is tiny,
there are no misses after warm-up, and there is no eviction policy to tune.

The materializer cache is keyed by shape, not only by type, so an application querying the *same* entity with
many different `SELECT` lists has more entries than it has entity types. Both are still bounded by the
statements in the code. An application that generates entity types dynamically at run time (rare) would grow
the cache unboundedly.

---

## Future considerations

### Breaking changes to avoid

1. Never change the signature of a public method
2. Never remove a public member (mark it `[Obsolete]` instead)
3. Never change default behaviour (add opt-in flags for new behaviour)
4. Never change the exception types an existing method throws
5. Never weaken a thread-safety guarantee

The declared public API files make the first two mechanical: an undeclared public member is `RS0016`, a
vanished one `RS0017`, and both are build errors.

### Possible enhancements

**Source-generated materializers.** The one place an ahead-of-time compiled application is measurably slower
than a JIT-compiled one is the mapping step, measured in
[Reflection, not source generation](#1-reflection-not-source-generation). Generation was rejected as the
*primary* mechanism; it stays viable as an **optional** layer on top, because the reflection path is then a
working fallback rather than a blocker. Anything built here must not reintroduce the failure mode that
rejection avoided: a type the generator never discovered must not silently map to nothing.

**Dependency injection support.** Optional `IServiceCollection` extension methods and per-scope configuration
via `IOptions<…>`, for applications that need something other than the process-wide configuration.

**Multi-result sets.** `QueryMultiple<Product, OrderItem>(sql)` for queries returning more than one result set,
avoiding a round-trip per related collection.

**A custom exception hierarchy.** A `DbConnectionPlusException` base with specific derivations (e.g.
`AdapterNotRegisteredException`) would make errors easier to handle selectively than the BCL exception types
currently thrown - at the cost of a breaking change to every `catch` that names one of them.
