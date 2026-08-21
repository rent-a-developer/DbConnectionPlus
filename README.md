<div align="center">

![logo](assets/logo-40.png)

# DbConnectionPlus

**A lightweight .NET ORM and extension library for the type
[DbConnection](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection)
that adds high-performance, type-safe helpers to reduce boilerplate code, boost productivity, and make working with 
SQL databases in C# more enjoyable.**

[![CI](https://github.com/rent-a-developer/DbConnectionPlus/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/rent-a-developer/DbConnectionPlus/actions/workflows/ci.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=rent-a-developer_DbConnectionPlus&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=rent-a-developer_DbConnectionPlus)
[![NuGet Version](https://img.shields.io/nuget/v/DbConnectionPlus)](https://www.nuget.org/packages/DbConnectionPlus/)
[![.NET 8 | 10](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![API docs](https://img.shields.io/badge/API%20docs-GitHub%20Pages-2b6cb0)](https://rent-a-developer.github.io/DbConnectionPlus/)
[![license](https://img.shields.io/badge/License-MIT-purple.svg)](LICENSE.md)
![semver](https://img.shields.io/badge/semver-4.0.0-blue)

</div>

If you frequently write SQL queries in your C# code and want to avoid boilerplate code, you will love DbConnectionPlus!

Highlights:
- [Parameterized interpolated-string support](#parameters-via-interpolated-strings)
- [On-the-fly temporary tables](#on-the-fly-temporary-tables-via-interpolated-strings) from in-memory collections
- Entity mapping helpers (insert, update, delete, query)
- Designed to be used in synchronous and asynchronous code paths
- Minimal performance and allocation overhead
- Fully [native AOT compatible](#native-aot-and-trimming)

The following database systems are supported out of the box:
- MySQL (via [MySqlConnector](https://www.nuget.org/packages/MySqlConnector/))
- Oracle Database (via [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/))
- PostgreSQL (via [Npgsql](https://www.nuget.org/packages/Npgsql/))
- SQLite (via [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite/))
- SQL Server (via [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/))

Other database systems and database connectors can be supported by implementing a 
[custom database adapter](#custom-database-adapter).

All examples in this document use SQL Server, and assume the static helpers are imported:

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
```

## Table of contents
- **[Why not just use Dapper?](#why-not-just-use-dapper)**
- **[Quick start](#quick-start)**
- [Examples](#examples) - [parameters](#parameters-via-interpolated-strings), [temporary tables](#on-the-fly-temporary-tables-via-interpolated-strings), [Enums](#enum-support)
- **[Native AOT and trimming](#native-aot-and-trimming)** - [what works](#what-works), [supported providers](#supported-providers),[what you will see in your own build](#what-you-will-see-in-your-own-build)
- **[API summary](#api-summary)**
- [Custom database adapter](#custom-database-adapter)
- [Benchmarks](#benchmarks)
- [Running the tests](#running-the-tests)
- [Contributing](#contributing)
- [Links](#links)

## Why not just use Dapper?

This library follows the same philosophy as Dapper: extension methods on `DbConnection`, your own SQL, no change tracking and no LINQ provider, but with the following differences and additions:

- Querying, CRUD and AOT support in one package. For dapper you need Dapper, Dapper.Contrib and Dapper.AOT.
- Pass parameters directly inside interpolated strings. SQL injection safe.
- Pass collections as on-the-fly temporary tables directly inside interpolated strings. Also SQL injection safe.
- Optimistic concurrency out-of-the-box.
- Full native AOT compatibility (including support for ValueTuples, which Dapper.AOT lacks).
- Customizable Enum serialization (string/integer).
- Great performance.

### Your values stay inside your query

```csharp
// Dapper
connection.Query<Product>(
    "SELECT * FROM Product WHERE SupplierId = @SupplierId AND UnitsInStock < @Threshold",
    new { SupplierId = supplierId, Threshold = threshold }
);

// DbConnectionPlus
connection.Query<Product>(
    $"SELECT * FROM Product WHERE SupplierId = {Parameter(supplierId)} AND UnitsInStock < {Parameter(threshold)}"
);
```

- No anonymous object to keep in sync with the SQL
- No renamed variable that compiles and then fails at run time
- Full auto-complete
- Full compiler verification

Each value is written where it is used, and still reaches the database as a real `DbParameter`. Nothing to name
twice, no anonymous object to keep in sync with the SQL, and no renamed variable that compiles happily and blows
up at run time.

### Throw a whole collection at the database

```csharp
var retiredSupplierIds = suppliers.Where(a => a.IsRetired).Select(a => a.Id);

var affectedProducts = connection.Query<Product>(
   $"""
    SELECT  *
    FROM    Product
    WHERE   SupplierId IN (SELECT Value FROM {TemporaryTable(retiredSupplierIds)})
    """
);
```

DbConnectionPlus creates a temporary table, bulk-loads your collection into it (`SqlBulkCopy` and the equivalent
for every other supported database) and drops it when the statement is done. Ten values or a million: no `IN` list
to build, no parameter limit to stay under, one query plan.

Objects work just as well - each property becomes a column - so you can `JOIN` straight against in-memory data:

```csharp
var orderedProducts = connection.Query<(Int64 ProductId, Int32 Quantity, Decimal UnitPrice)>(
   $"""
    SELECT  TOrderItem.ProductId, TOrderItem.Quantity, Product.UnitPrice
    FROM    Product
            JOIN {TemporaryTable(orderItems)} TOrderItem ON TOrderItem.ProductId = Product.Id
    """
);
```

Dapper expands `IN @ids` into one parameter per element instead - a fresh query plan for every list length, and a
hard ceiling at the provider's parameter limit (2,100 on SQL Server). Table-valued parameters lift that ceiling,
but only on SQL Server, and only after you have created a table type in the database and hand-built a `DataTable`.

### Full optimistic concurrence support

```csharp
class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Int64 Id { get; set; }

    [Timestamp]
    public Byte[] Version { get; set; }

    public Decimal UnitPrice { get; set; }
}

connection.InsertEntity(newProduct);            // Id and Version come back filled in
connection.UpdateEntity(product);               // throws if someone else changed the row first
connection.DeleteEntities(discontinuedProducts);
```

Insert, update and delete - one entity or a whole sequence, synchronous or `…Async` - and **every one of them
supports optimistic concurrency**. Mark a property with `[Timestamp]` or `[ConcurrencyCheck]` and its original
value joins the `WHERE` clause, so an update or delete against a row that changed in the meantime raises
`DbUpdateConcurrencyException` instead of quietly matching nothing. Database-generated values are read back onto
your entity, and composite keys are supported.

Dapper has no CRUD at all - you write every `INSERT`, `UPDATE` and `DELETE` by hand. Dapper.Contrib is the usual
answer, and it does add `Insert` / `Update` / `Delete` - but its `UPDATE` matches on the key alone, so **there is
no optimistic concurrency anywhere in the Dapper family**. It also cannot handle composite keys, has no Oracle
support, and has not shipped a release since 2020.

### The attributes you already use

```csharp
[Table("Products")]
class Product
{
    [Key] public Int64 Id { get; set; }
    [Column("ProductName")] public String Name { get; set; }
    [NotMapped] public Decimal TotalPrice => this.UnitPrice * this.Quantity;
}
```

Plain `System.ComponentModel.DataAnnotations` - the same attributes EF Core reads, so your entities usually need
no changes at all. Prefer to keep your domain model clean? A fluent API configures the same things from one place.
Dapper.Contrib brings its own smaller attribute set and cannot even map a column name.

### Native AOT support with no extra steps

```xml
<PublishAot>true</PublishAot>
```

That is the entire setup. No companion package, no source generator, no attribute on anything, and no `IL2xxx` or
`IL3xxx` warning in your build. Everything keeps working: entities, records and other immutable types, **value
tuples**, temporary tables, CRUD and enums.

Dapper cannot run under Native AOT at all; you need the separate Dapper.AOT package and a `[DapperAot]` opt-in on
every method, type or module you want covered. Even then it does not support value tuples, and it does not cover
Dapper.Contrib's CRUD - so those simply have no Dapper answer under Native AOT. Worse, a call site Dapper.AOT
cannot handle is left as ordinary Dapper without any warning: the build stays green and the failure shows up in
your published binary.

### Enums, your way, in one line

```csharp
DbConnectionExtensions.Configure(config => config.EnumSerializationMode = EnumSerializationMode.Strings);
```

Readable strings or compact integers - the choice applies everywhere at once: entity properties, parameters and
temporary tables. Reading understands both, whichever your columns happen to hold. Dapper writes the underlying
number unless you write and register a type handler yourself - which Dapper.AOT's generated code then ignores.

### Untyped rows without `dynamic`

```csharp
var row = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(productId)}");
var name = row["Name"];
```

No cast, no `dynamic`, and it works under Native AOT - where Dapper's `dynamic` rows cannot be bound at all. If you
do want member access, one `dynamic` reference gives you `row.Name`; it is an option here rather than the only way
in.

### See every command your app sends

```csharp
DbConnectionExtensions.Configure(config =>
{
    config.InterceptDbCommand = (command, _) => logger.LogDebug("SQL: {Sql}", command.CommandText);
});
```

One hook, every statement DbConnectionPlus builds, right before it executes - for logging, a timeout, or a query
hint. Dapper has nothing comparable.

### And it costs you nothing

The [benchmark suite](#benchmarks) in this repository runs every feature three ways - hand-written `DbCommand`,
Dapper and DbConnectionPlus - against in-memory SQLite, the harshest possible setting because the query itself is
almost free there. The two libraries trade places from category to category, both within a small multiple of raw
ADO.NET, and DbConnectionPlus allocates less than Dapper in eleven of the seventeen categories.

### When Dapper is still the better pick

Being honest about it: Dapper has multi-mapping (`splitOn`) and `QueryMultiple` for several result sets from one
command, custom `ITypeHandler` conversions, support for .NET Framework and .NET Standard 2.0, and fifteen years of
ecosystem. DbConnectionPlus has none of the first three, requires `net8.0` or later, and needs a
[custom adapter](#custom-database-adapter) for database systems beyond the [five it supports](#installation).

## Quick start

### Installation

Install the core package plus the adapter package for the database system you use:

```shell
dotnet add package DbConnectionPlus
dotnet add package DbConnectionPlus.DatabaseAdapters.SqlServer
```

| Database   | Adapter package                                |
|------------|------------------------------------------------|
| SQL Server | `DbConnectionPlus.DatabaseAdapters.SqlServer`  |
| MySQL      | `DbConnectionPlus.DatabaseAdapters.MySql`      |
| PostgreSQL | `DbConnectionPlus.DatabaseAdapters.PostgreSql` |
| Oracle     | `DbConnectionPlus.DatabaseAdapters.Oracle`     |
| SQLite     | `DbConnectionPlus.DatabaseAdapters.Sqlite`     |

### Register Database Adapters

Before using DbConnectionPlus, register the adapter(s) for the database system(s) you use. This should be done
once at application startup:

```csharp
using RentADeveloper.DbConnectionPlus.Configuration;

// Register one or more adapters:
DbConnectionExtensions.Configure(config => config.UseSqlServer());
```

### Use the extension methods

Open or reuse a `DbConnection` and call the extension methods on it:

```csharp
class Product
{
    [Key]
    public Int64 Id { get; set; }
    public Int32 UnitsInStock { get; set; }
}

var lowStockThreshold = configuration.Thresholds.LowStock;

// Query entities, scalars and value tuples
var lowStockProducts = connection.Query<Product>(
   $"""
    SELECT  *
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);

var numberOfProducts = connection.ExecuteScalar<Int32>($"SELECT COUNT(*) FROM Product");

// Rows without a type - read columns with the indexer
var row = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(productId)}");
var unitsInStock = row["UnitsInStock"];

// CRUD
connection.InsertEntity(newProduct);
connection.UpdateEntities(lowStockProducts);
connection.DeleteEntity(discontinuedProduct);
```

Every method has an `…Async` counterpart, and all of them accept an optional transaction, command timeout,
command type and cancellation token.

## Examples

### Parameters via interpolated strings
All extension methods accept interpolated strings where parameter values are captured via
[Parameter(value)](#parametervalue):

```csharp
var lowStockThreshold = configuration.Thresholds.LowStock;

var lowStockProductInfos = connection.Query<(Int64 ProductId, Int32 UnitsInStock)>(
   $"""
    SELECT  Id, UnitsInStock
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```

This prevents SQL injection and keeps the SQL readable.

### On-the-fly temporary tables via interpolated strings
> [!CAUTION]
> **Warning for Oracle users**  
> This feature creates private temporary tables and drops them after use. In Oracle, DDL statements cause an
> implicit commit of the current transaction — so inside an explicit transaction it is committed **twice**:
> once when the temporary table is created and once when it is dropped.  
> For that reason the feature is **disabled by default** for Oracle and using it throws. To enable it anyway,
> set `RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle.OracleDatabaseAdapter.AllowTemporaryTables` to
> `true` — and avoid the feature inside explicit transactions.

> [!NOTE]
> **Note for MySQL users**  
> Temporary tables are populated with `MySqlBulkCopy`, so the connection string needs
> `AllowLoadLocalInfile=true` and the server needs `local_infile` enabled (e.g. `SET GLOBAL local_infile=1`).

Create a temporary table on the fly from an `IEnumerable<T>` and use it in statements via
[TemporaryTable(values)](#temporarytablevalues):

```csharp
var retiredSupplierIds = suppliers.Where(a => a.IsRetired).Select(a => a.Id);

var retiredSupplierProducts = connection.Query<Product>(
   $"""
    SELECT  *
    FROM    Product
    WHERE   SupplierId IN (
                SELECT  Value
                FROM    {TemporaryTable(retiredSupplierIds)}
            )
    """
);
```

Complex objects are also supported - the library creates a temporary table with appropriate columns and types:

```csharp
class OrderItem
{
    public Int64 ProductId { get; set; }
    public DateTime OrderDate { get; set; }
}

var orderItems = GetOrderItems();
var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

var productsOrderedInPastSixMonths = connection.Query<Product>(
    $"""
     SELECT     *
     FROM       Product
     WHERE      EXISTS (
                    SELECT  1
                    FROM    {TemporaryTable(orderItems)} TOrderItem
                    WHERE   TOrderItem.ProductId = Product.Id AND
                            TOrderItem.OrderDate >= {Parameter(sixMonthsAgo)}
                )
     """
);
```

### Enum support
Enum values are sent to the database either as their string representation or as integers, controlled by
[EnumSerializationMode](#enumserializationmode). Reading maps both representations back to the enum value
automatically.

```csharp
enum UserRole
{
  Admin = 1,
  User = 2,
  Guest = 3
}

class User
{
    [Key]
    public Int64 Id { get; set; }
    public String UserName { get; set; }
    public UserRole Role { get; set; }
}

var user = new User { Id = 1, UserName = "adminuser", Role = UserRole.User };

connection.InsertEntity(user);
// Column "Role" contains the string "User" with EnumSerializationMode.Strings (the default),
// and the integer 2 with EnumSerializationMode.Integers.
```

The column type has to match the mode - `NVARCHAR(200)` for `Strings`, `INT` for `Integers`:

```sql
CREATE TABLE Users
(
    Id BIGINT,
    UserName NVARCHAR(255),
    Role NVARCHAR(200)   -- INT when EnumSerializationMode.Integers is used
)
```

## Native AOT and trimming

**Reference the package and publish. There is nothing to install and nothing to opt into** - no companion
package, no source generator, no attribute, no registration call. Everything below works in an application
published with `PublishAot` exactly as it does on the just-in-time compiler.

DbConnectionPlus targets `net8.0` and `net10.0`. `net8.0` is the supported floor; **`net10.0` is recommended**
for AOT, because from `net9.0` on the trim and AOT analyzers recognise `RuntimeFeature.IsDynamicCodeSupported`
as a feature guard and stop reporting code your own guard has already made unreachable. Either way this
library's own publish is warning-free on both — see [What you will see in your own
build](#what-you-will-see-in-your-own-build).

### What works

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
[benchmark suite](#benchmarks) - an in-memory SQLite database, the worst case, because statement execution is
almost free there and nothing dilutes the mapping cost - querying entities takes **~1.31x** as long end to end
and querying value tuples **~1.35x**. Part of that is the ahead-of-time runtime rather than this library: the
raw `DbCommand` baseline in the same run slows by 1.08x and 1.12x respectively. Against a real database server,
where the query itself dominates, the difference is correspondingly smaller.

### Reading rows without a type: `row["Id"]`, not `row.Id`

The non-generic query methods return `DataRow`. The string indexer is the AOT-safe way to read a column and is
what the examples in this README use:

```csharp
var product = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
var name = product["Name"];
```

Member access through a `dynamic` reference still works wherever the runtime supports dynamic code generation,
but **not** under Native AOT - the Dynamic Language Runtime cannot bind without generating code. `DataRow`
itself is AOT-safe either way, and costs you nothing if you never write `dynamic`; the incompatibility is
reported by the compiler at your own call site. See [Query methods](#query-methods) for the full comparison.

### Supported providers

End-to-end AOT support is also bounded by your ADO.NET provider, which this library cannot fix:

| Database | Provider | Native AOT |
|---|---|---|
| SQLite | `Microsoft.Data.Sqlite` | ✅ Verified trim-clean, and the provider this library's own AOT smoke test runs against |
| MySQL | `MySqlConnector` | ✅ Fully managed and trim-friendly |
| SQL Server | `Microsoft.Data.SqlClient` | ✅ Publishes and runs clean. |
| PostgreSQL | `Npgsql` | ⚠️ Core is AOT-capable; some type plug-ins reflect |
| Oracle | `Oracle.ManagedDataAccess.Core` | ❌ Not AOT-ready. This is a limitation of the provider |

### What you will see in your own build

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

## API summary

Configuration:
- [EnumSerializationMode](#enumserializationmode) - Configure how enum values are serialized when sent to the database
- [InterceptDbCommand](#interceptdbcommand) - Configure a delegate to intercept `DbCommand`s executed by DbConnectionPlus

Entity mapping:
- [Fluent API](#fluent-api) - Configure entity mapping via fluent API
- [Data annotation attributes](#data-annotation-attributes) - Configure entity mapping via data annotation attributes

General-purpose methods:
- [ExecuteNonQuery / ExecuteNonQueryAsync](#executenonquery--executenonqueryasync) - Execute a non-query and return 
number of affected rows
- [ExecuteReader / ExecuteReaderAsync](#executereader--executereaderasync) - Execute a query and return `DbDataReader` 
to read the results
- [ExecuteScalar / ExecuteScalarAsync](#executescalar--executescalarasync) - Read a single value
- [Exists / ExistsAsync](#exists--existsasync) - Check for existence of rows

Query methods:
- [Query / QueryAsync](#query--queryasync) - Map result set to `DataRow` instances
- [QueryFirst / QueryFirstAsync](#queryfirst--queryfirstasync) - Map first row of result set to a `DataRow`
- [QueryFirstOrDefault / QueryFirstOrDefaultAsync](#queryfirstordefault--queryfirstordefaultasync) - Map first row of result set to a `DataRow` or null if no rows are found
- [QuerySingle / QuerySingleAsync](#querysingle--querysingleasync) - Map single row of result set to a `DataRow`
- [QuerySingleOrDefault / QuerySingleOrDefaultAsync](#querysingleordefault--querysingleordefaultasync) - Map single row of result set to a `DataRow` or null if no rows are found
- [Query\<T\> / QueryAsync\<T\>](#queryt--queryasynct) - Map result set to scalar values, entities or value tuples
- [QueryFirst\<T\> / QueryFirstAsync\<T\>](#queryfirstt--queryfirstasynct) - Map first row of result set to a scalar value, entity or value tuple
- [QueryFirstOrDefault\<T\> / QueryFirstOrDefaultAsync\<T\>](#queryfirstordefaultt--queryfirstordefaultasynct) - Map first row of result set to a scalar value, entity or value tuple or default value if no rows are found
- [QuerySingle\<T\> / QuerySingleAsync\<T\>](#querysinglet--querysingleasynct) - Map single row of result set to a scalar value, entity or value tuple
- [QuerySingleOrDefault\<T\> / QuerySingleOrDefaultAsync\<T\>](#querysingleordefaultt--querysingleordefaultasynct) - Map single row of result set to a scalar value, entity or value tuple or default value if no rows are found

Entity manipulation methods:
- [InsertEntities / InsertEntitiesAsync](#insertentities--insertentitiesasync) - Insert a sequence of new entities
- [InsertEntity / InsertEntityAsync](#insertentity--insertentityasync) - Insert a new entity
- [UpdateEntities / UpdateEntitiesAsync](#updateentities--updateentitiesasync) - Update existing entities by keys
- [UpdateEntity / UpdateEntityAsync](#updateentity--updateentityasync) - Update an existing entity by key
- [DeleteEntities / DeleteEntitiesAsync](#deleteentities--deleteentitiesasync) - Delete existing entities by keys
- [DeleteEntity / DeleteEntityAsync](#deleteentity--deleteentityasync) - Delete an existing entity by key

Special helpers:
- [Parameter(value)](#parametervalue) - Create a parameter for an SQL statement from an interpolated value
- [TemporaryTable(values)](#temporarytablevalues) - Create a temporary table from a sequence of values and reference 
it inside an SQL statement

### Configuration

Use `DbConnectionExtensions.Configure` to configure DbConnectionPlus.

```csharp
DbConnectionExtensions.Configure(config =>
{
    // Configuration options go here
});
```

> [!NOTE]
> To prevent multi-threading issues `DbConnectionExtensions.Configure` can only be called once during the application lifetime.
> After it has been called the configuration of DbConnectionPlus is frozen and cannot be changed anymore.

#### EnumSerializationMode
Use `EnumSerializationMode` to configure how enum values are serialized when they are sent to a database.
`EnumSerializationMode.Strings` (the default) serializes them as their string representation,
`EnumSerializationMode.Integers` as integers. It applies to entity properties, parameters and temporary table
columns alike - see [Enum support](#enum-support).

```csharp
DbConnectionExtensions.Configure(config =>
{
    config.EnumSerializationMode = EnumSerializationMode.Integers;
});
```

#### InterceptDbCommand
Use `InterceptDbCommand` to configure a delegate that intercepts a `DbCommand` before it is executed. This can be 
useful for logging, modifying the command text, or applying additional configuration.

```csharp
DbConnectionExtensions.Configure(config =>
{
    config.InterceptDbCommand = (dbCommand, temporaryTables) =>
    {
        // Log the command text
        Console.WriteLine("Executing SQL Command: " + dbCommand.CommandText);
    
        // Modify the command text if needed
        dbCommand.CommandText += " OPTION (RECOMPILE)";

        // Apply additional configuration if needed
        dbCommand.CommandTimeout = 60;
    };
});
```

See [DbCommandLogger](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/tests/DbConnectionPlus.IntegrationTests/TestHelpers/DbCommandLogger.cs) 
for an example of logging executed commands.

#### Entity Mapping

You can configure how entity types are mapped to database tables and columns using either the fluent API or data 
annotation attributes.

> [!NOTE]
> Mapping configured via the fluent API takes precedence over mapping configured via data annotation attributes.
> When a fluent mapping exists for an entity type, the data annotations on this entity type are ignored.
> When a fluent mapping exists for an entity property, the data annotations on this property are ignored.

##### Fluent API
You can use the fluent API to configure how entity types are mapped to database tables and columns.

```csharp
DbConnectionExtensions.Configure(config =>
{
    config.Entity<Product>()
        .ToTable("Products");

    config.Entity<Product>()
        .Property(a => a.Id)
        .HasColumnName("ProductId")
        .IsIdentity()
        .IsKey();

    config.Entity<Product>()
        .Property(a => a.DiscountedPrice)
        .IsComputed();

    config.Entity<Product>()
        .Property(a => a.IsOnSale)
        .IsIgnored();

    config.Entity<Product>()
        .Property(a => a.Version)
        .IsRowVersion();

    config.Entity<User>()
        .Property(a => a.ConcurrencyToken)
        .IsConcurrencyToken();
});
```

| Method | Configures |
|---|---|
| `Entity<TEntity>()` | Starts configuring the mapping for the entity type `TEntity`. |
| `ToTable(tableName)` | The table where entities of that type are stored. |
| `Property(propertyExpression)` | Starts configuring the mapping for one property. |
| `HasColumnName(columnName)` | The column where the property is stored. |
| `IsKey()` | The property is part of the key by which entities are identified. |
| `IsIdentity()` | The property is generated by the database on insert. |
| `IsComputed()` | The property is generated by the database on insert and update. |
| `IsRowVersion()` | The property is a native database-generated concurrency token. |
| `IsConcurrencyToken()` | The property is an application-managed concurrency token. |
| `IsIgnored()` | The property is not mapped to a column. |

##### Data annotation attributes

Entity mapping can also be configured with the standard attributes from
`System.ComponentModel.DataAnnotations` and `System.ComponentModel.DataAnnotations.Schema`:

```csharp
[Table("Products")]                                     // Table name; defaults to the type name
class Product
{
    [Key]                                               // Identifies the entity (usually the primary key)
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Int64 Id { get; set; }

    [Column("ProductName")]                             // Column name; defaults to the property name
    public String Name { get; set; }

    [Timestamp]                                         // Native database-generated concurrency token
    public Byte[] Version { get; set; }

    [ConcurrencyCheck]                                  // Application-managed concurrency token
    public Byte[] ConcurrencyToken { get; set; }

    [NotMapped]                                         // Never read from or written to the database
    public Decimal TotalPrice => this.UnitPrice * this.Quantity;
}
```

| Attribute | Effect |
|---|---|
| `TableAttribute` | The table where entities of the type are stored. Without it, the entity type's name (excluding its namespace) is used. |
| `ColumnAttribute` | The column where the property is stored. Without it, the property name is used. |
| `KeyAttribute` | The property (or properties) by which entities of the type are identified. |
| `DatabaseGeneratedAttribute` | The property is generated by the database. Unless `DatabaseGeneratedOption.None` is used it is skipped when inserting and updating, and its value is read back from the database onto the entity afterwards. |
| `TimestampAttribute` | The property is a native database-generated concurrency token: it is checked during update and delete, which fail if the database value no longer matches the original, and it is read back after insert and update. |
| `ConcurrencyCheckAttribute` | The property is an application-managed concurrency token, checked during update and delete the same way. |
| `NotMappedAttribute` | The property is ignored entirely - never read from and never written to the database. |

### General-purpose methods

#### ExecuteNonQuery / ExecuteNonQueryAsync
Executes an SQL statement and returns the number of rows affected by the statement.

```csharp
if (supplier.IsRetired)
{
    var numberOfDeletedProducts = connection.ExecuteNonQuery(
       $"""
        DELETE FROM Product
        WHERE       SupplierId = {Parameter(supplier.Id)}
        """
    );
}
```

#### ExecuteReader / ExecuteReaderAsync
Executes an SQL statement and returns a `DbDataReader` to read the results.

```csharp
var lowStockThreshold = configuration.Thresholds.LowStock;

using var lowStockProductsReader = connection.ExecuteReader(
   $"""
    SELECT  *
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```

#### ExecuteScalar / ExecuteScalarAsync
Executes an SQL statement and returns the value of the first column of the first row in the result set converted to 
the specified type.
```csharp
var lowStockThreshold = configuration.Thresholds.LowStock;

var numberOfLowStockProducts = connection.ExecuteScalar<Int32>(
   $"""
    SELECT  COUNT(*)
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```

#### Exists / ExistsAsync
Checks if any rows exist that match the specified SQL statement.
```csharp
var lowStockThreshold = configuration.Thresholds.LowStock;

var existLowStockProducts = connection.Exists(
   $"""
    SELECT  1
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```

### Query methods

The non-generic query methods below return `DataRow` instances. There are two ways to read a column, and which
one you should use depends on how your application is published:

| | Access | Works on |
|---|---|---|
| **Recommended** | `product["Id"]` — string indexer, no cast | every runtime, **including Native AOT** |
| Optional | `product.Id` — member access through a `dynamic` reference | runtimes with dynamic code generation (**not** Native AOT) |

The examples in this section use the string indexer. To use member access instead, assign the row to a `dynamic`
reference — the static return type is `DataRow`, not `dynamic`, so the step is explicit:

```csharp
dynamic product = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
var name = product.Name;

foreach (dynamic p in connection.Query($"SELECT * FROM Product"))
{
    var unitsInStock = p.UnitsInStock;
}
```

Member access behaves exactly like the indexer, including throwing `KeyNotFoundException` for a column the row
does not contain. Note that through a `dynamic` reference a *property* always addresses a column — `row.Count`
reads the column named `Count`, not the number of columns — while *method* calls still resolve against `DataRow`,
so `row.ContainsKey("Id")` works as expected. Use a statically typed `DataRow` reference to reach `Count`, `Keys`
and `Values`.

If you publish with Native AOT, use the indexer: the C# compiler reports `dynamic` usage as an AOT
incompatibility at your own call site, and the Dynamic Language Runtime cannot bind it without run-time code
generation. `DataRow` itself is AOT-safe to construct and use either way. See
[Native AOT and trimming](#native-aot-and-trimming).

#### Query / QueryAsync
Executes an SQL statement and maps the result set to a sequence of `DataRow` instances. Access columns by name
through the string indexer.
```csharp
var lowStockThreshold = configuration.Thresholds.LowStock;

var lowStockProducts = connection.Query(
   $"""
    SELECT  *
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);

foreach (var product in lowStockProducts)
{
    var id = product["Id"];
    var unitsInStock = product["UnitsInStock"];
    ...
}
```

#### QueryFirst / QueryFirstAsync
Executes an SQL statement and maps the first row of the result set to a `DataRow`.
Throws if no rows are found.
```csharp
var product = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(id)}");

var id = product["Id"];
var name = product["Name"];
...
```

#### QueryFirstOrDefault / QueryFirstOrDefaultAsync
Executes an SQL statement and maps the first row of the result set to a `DataRow` or null if no rows are found.
```csharp
var product = connection.QueryFirstOrDefault($"SELECT * FROM Product WHERE Id = {Parameter(id)}");

if (product is not null)
{
    var id = product["Id"];
    var name = product["Name"];
    ...
}
```

#### QuerySingle / QuerySingleAsync
Executes an SQL statement and maps the single row of the result set to a `DataRow`.
Throws if no rows or more than one row are found.
```csharp
var product = connection.QuerySingle($"SELECT * FROM Product WHERE Id = {Parameter(id)}");

var id = product["Id"];
var name = product["Name"];
...
```

#### QuerySingleOrDefault / QuerySingleOrDefaultAsync
Executes an SQL statement and maps the single row of the result set to a `DataRow` or null if no rows are found.
Throws if more than one row are found.
```csharp
var product = connection.QuerySingleOrDefault($"SELECT * FROM Product WHERE Id = {Parameter(id)}");

if (product is not null)
{
    var id = product["Id"];
    var name = product["Name"];
    ...
}
```

#### Query\<T\> / QueryAsync\<T\>
Executes an SQL statement and maps the result set to a sequence of scalar values, entities or value tuples of the 
specified type.

```csharp
var lowStockThreshold = configuration.Thresholds.LowStock;

// Entities
var lowStockProducts = connection.Query<Product>(
   $"""
    SELECT  *
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);

// Scalar values
var lowStockProductIds = connection.Query<Int64>(
   $"""
    SELECT  Id
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);

// Value tuples
var lowStockProductInfos = connection.Query<(Int64 ProductId, Int32 UnitsInStock)>(
   $"""
    SELECT  Id, UnitsInStock
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```

#### QueryFirst\<T\> / QueryFirstAsync\<T\>
Executes an SQL statement and maps the first row of the result set to a scalar value, entity or value tuple of the 
specified type.
Throws if no rows are found.
```csharp
var product = connection.QueryFirst<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

#### QueryFirstOrDefault\<T\> / QueryFirstOrDefaultAsync\<T\>
Executes an SQL statement and maps the first row of the result set to a scalar value, entity or value tuple of the 
specified type or default value if no rows are found.
```csharp
var product = connection.QueryFirstOrDefault<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

#### QuerySingle\<T\> / QuerySingleAsync\<T\>
Executes an SQL statement and maps the single row of the result set to a scalar value, entity or value tuple of the 
specified type.
Throws if no rows or more than one row are found.
```csharp
var product = connection.QuerySingle<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

#### QuerySingleOrDefault\<T\> / QuerySingleOrDefaultAsync\<T\>
Executes an SQL statement and maps the single row of the result set to a scalar value, entity or value tuple of the 
specified type or default value if no rows are found.
Throws if more than one row are found.
```csharp
var product = connection.QuerySingleOrDefault<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

### Entity manipulation methods

The examples below use these entity types:

```csharp
class Product
{
    [Key]
    public Int64 Id { get; set; }
    public Int64 SupplierId { get; set; }
    public String Name { get; set; }
    public Decimal UnitPrice { get; set; }
    public Int32 UnitsInStock { get; set; }
    public Boolean IsDiscontinued { get; set; }
}

enum UserState { Active, Inactive, Suspended }

class User
{
    [Key]
    public Int64 Id { get; set; }
    public DateTime LastLoginDate { get; set; }
    public UserState State { get; set; }
}
```

#### InsertEntities / InsertEntitiesAsync
Inserts a sequence of new entities into a database table.
```csharp
connection.InsertEntities(GetNewProducts());
```

#### InsertEntity / InsertEntityAsync
Inserts a new entity into a database table.
```csharp
connection.InsertEntity(GetNewProduct());
```

#### UpdateEntities / UpdateEntitiesAsync
Updates existing entities in a database table based on their keys.
```csharp
var usersWithoutLoginInPastYear = connection.Query<User>(
    """
    SELECT  *
    FROM    Users
    WHERE   LastLoginDate < DATEADD(YEAR, -1, GETUTCDATE())
    """
);

foreach (var user in usersWithoutLoginInPastYear)
{
    user.State = UserState.Inactive;
}

connection.UpdateEntities(usersWithoutLoginInPastYear);
```

#### UpdateEntity / UpdateEntityAsync
Updates an existing entity in a database table based on its key.
```csharp
if (user.LastLoginDate < DateTime.UtcNow.AddYears(-1))
{
    user.State = UserState.Inactive;
    connection.UpdateEntity(user);
}
```

#### DeleteEntities / DeleteEntitiesAsync
Deletes a sequence of entities from a database table based on their keys.
```csharp
connection.DeleteEntities(products.Where(a => a.IsDiscontinued));
```

#### DeleteEntity / DeleteEntityAsync
Deletes an entity from a database table based on its key.
```csharp
if (product.IsDiscontinued)
{
    connection.DeleteEntity(product);
}
```

### Special helpers

The following special helpers can be used with any DbConnectionPlus extension method that accepts an instance of 
`InterpolatedSqlStatement`.

#### Parameter(value)
Use `Parameter(value)` to pass a value in an interpolated string as a parameter to an SQL statement.

```csharp
var lowStockThreshold = configuration.Thresholds.LowStock;

using var lowStockProductsReader = connection.ExecuteReader(
   $"""
    SELECT  *
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```
This adds a parameter holding the value of `lowStockThreshold` to the SQL statement, and replaces the
`{Parameter(value)}` expression with the parameter's name.

The parameter name is inferred from the expression passed to `Parameter(value)` - here `LowStockThreshold`. If
no name can be inferred (e.g. `Parameter(42)`), a generic name like `Parameter_1`, `Parameter_2` and so on is
used.

Enum values are serialized as strings or as integers according to
[EnumSerializationMode](#enumserializationmode).

#### TemporaryTable(values)
Use `TemporaryTable(values)` to pass a sequence of scalar values or complex objects in an interpolated string as a
temporary table to an SQL statement.

A sequence of scalar values (e.g. `String`, `Int32`, `DateTime`, enums and so on) produces a temporary table
with a single column named `Value`, typed to match the passed values:

```csharp
var retiredSupplierIds = suppliers.Where(a => a.IsRetired).Select(a => a.Id);

using var retiredSupplierProductsReader = connection.ExecuteReader(
   $"""
    SELECT  *
    FROM    Product
    WHERE   SupplierId IN (
                SELECT  Value
                FROM    {TemporaryTable(retiredSupplierIds)}
            )
    """
);
```
```sql
CREATE TABLE #RetiredSupplierIds_48d42afd5d824a27bd9352676ab6c198
(
    Value BIGINT
)
```

A sequence of complex objects produces one column per public property, named and typed after that property:

```csharp
class OrderItem
{
    public Int64 ProductId { get; set; }
    public DateTime OrderDate { get; set; }
}

var orderItems = GetOrderItems();
var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

using var productsOrderedInPastSixMonthsReader = connection.ExecuteReader(
    $"""
     SELECT     *
     FROM       Product
     WHERE      EXISTS (
                    SELECT  1
                    FROM    {TemporaryTable(orderItems)} TOrderItem
                    WHERE   TOrderItem.ProductId = Product.Id AND
                            TOrderItem.OrderDate >= {Parameter(sixMonthsAgo)}
                )
     """
);
```
```sql
CREATE TABLE #OrderItems_d6545835d97148ab93709efe9ba1f110
(
    ProductId BIGINT,
    OrderDate DATETIME2
)
```

The table name is inferred from the expression passed to `TemporaryTable(values)` and suffixed with a new Guid
to avoid naming conflicts (e.g. `OrderItems_395c98f203514e81aa0098ec7f13e8a2`); if no name can be inferred,
`Values` is used instead. The `{TemporaryTable(values)}` expression is replaced with that name in the SQL
statement.

Enum values - passed directly or as properties of complex objects - are serialized according to
[EnumSerializationMode](#enumserializationmode), and the column is typed `NVARCHAR(200)` for `Strings` and
`INT` for `Integers`.

### Custom database adapter
If you want to use DbConnectionPlus with a database system or a database connector that is not supported out of the 
box, you can implement a custom `IDatabaseAdapter`:

```csharp
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

public class MyDatabaseAdapter : IDatabaseAdapter
{
    // Write a class that implements RentADeveloper.DbConnectionPlus.DatabaseAdapters.IEntityManipulator and
    // return it here.
    public IEntityManipulator EntityManipulator => new MyEntityManipulator();

    // Write a class that implements RentADeveloper.DbConnectionPlus.DatabaseAdapters.ITemporaryTableBuilder and 
    // return it here.
    public ITemporaryTableBuilder TemporaryTableBuilder => new MyTemporaryTableBuilder();

    public void BindParameterValue(DbParameter parameter, Object? value)
    {
        ...
    }

    public String FormatParameterName(String parameterName)
    {
        ...
    }

    ...
}
```

Then register your custom database adapter before using DbConnectionPlus:
```csharp
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;

DbConnectionExtensions.Configure(config =>
{
    config.RegisterDatabaseAdapter<MyConnectionType>(new MyDatabaseAdapter());
});
```

You can also create an extension method for convenient registration:

```csharp
namespace RentADeveloper.DbConnectionPlus.Configuration;

public static class MyCustomConfigurationExtensions
{
    public static DbConnectionPlusConfiguration UseMyCustomDatabase(this DbConnectionPlusConfiguration configuration)
    {
        configuration.RegisterDatabaseAdapter<MyConnectionType>(new MyDatabaseAdapter());
        return configuration;
    }
}
```

Then register it like any built-in adapter:

```csharp
DbConnectionExtensions.Configure(config => config.UseMyCustomDatabase());
```

See [SqlServerDatabaseAdapter](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/src/DbConnectionPlus.DatabaseAdapters.SqlServer/SqlServerDatabaseAdapter.cs)
for an example implementation of a database adapter.

## Benchmarks
DbConnectionPlus is designed to have a minimal performance and allocation overhead compared to using `DbCommand` 
manually.  

All benchmarks are performed using SQLite in-memory databases, which is a worst-case scenario for DbConnectionPlus 
because the overhead of using DbConnectionPlus is more noticeable when the executed SQL statements are very fast.

The entity-querying categories are additionally measured as a Native AOT compiled binary, because DbConnectionPlus
selects its materializer on `RuntimeFeature.IsDynamicCodeSupported` and the reflection path behind that switch is
the one a Native AOT consumer runs. Only `Query_Entities`, `Query_ValueTuples` and `TemporaryTable_ComplexObjects`
reach that branch; every other category runs identical code on both runtimes, so measuring it twice would only
compare RyuJIT with ILC. The table below is the JIT snapshot. See
[benchmarks/DbConnectionPlus.Benchmarks/README.md](benchmarks/DbConnectionPlus.Benchmarks/README.md) for the
two-job summary and for which categories have a Dapper competitor under Native AOT at all.

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i9-12900K 3.19GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.303
  [Host] : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  JIT    : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  AOT    : .NET 10.0.11, X64 NativeAOT x86-64-v3

Server=True  InvocationCount=Default  IterationTime=300ms  
MaxIterationCount=20  UnrollFactor=16  WarmupCount=3  

```
| Method                                         | Job | Toolchain         | Mean         | Error       | StdDev      | Ratio        | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---- |------------------ |-------------:|------------:|------------:|-------------:|--------:|--------:|-------:|----------:|------------:|
| **DeleteEntities_Command**                         | **JIT** | **Default**           |   **136.627 μs** |   **1.2107 μs** |   **1.1324 μs** |     **baseline** |        **** |  **0.7813** |      **-** |   **68556 B** |            **** |
| DeleteEntities_Dapper                          | JIT | Default           |   168.392 μs |   1.8021 μs |   1.5975 μs | 1.23x slower |   0.02x |  1.2500 |      - |  133269 B |  1.94x more |
| DeleteEntities_DbConnectionPlus                | JIT | Default           |   167.147 μs |   1.0246 μs |   0.9584 μs | 1.22x slower |   0.01x |  1.0417 |      - |  116876 B |  1.70x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **DeleteEntity_Command**                           | **JIT** | **Default**           |     **1.464 μs** |   **0.0115 μs** |   **0.0102 μs** |     **baseline** |        **** |  **0.0078** |      **-** |     **769 B** |            **** |
| DeleteEntity_Dapper                            | JIT | Default           |     1.972 μs |   0.0148 μs |   0.0131 μs | 1.35x slower |   0.01x |  0.0263 |      - |    1705 B |  2.22x more |
| DeleteEntity_DbConnectionPlus                  | JIT | Default           |     1.749 μs |   0.0186 μs |   0.0165 μs | 1.19x slower |   0.01x |  0.0170 |      - |    1249 B |  1.62x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **ExecuteNonQuery_Command**                        | **JIT** | **Default**           |     **1.353 μs** |   **0.0168 μs** |   **0.0140 μs** |     **baseline** |        **** |  **0.0133** |      **-** |     **768 B** |            **** |
| ExecuteNonQuery_Dapper                         | JIT | Default           |     1.551 μs |   0.0145 μs |   0.0129 μs | 1.15x slower |   0.01x |  0.0153 |      - |    1072 B |  1.40x more |
| ExecuteNonQuery_DbConnectionPlus               | JIT | Default           |     1.697 μs |   0.0057 μs |   0.0048 μs | 1.25x slower |   0.01x |  0.0280 |      - |    1608 B |  2.09x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **ExecuteReader_Command**                          | **JIT** | **Default**           |   **281.423 μs** |   **1.7947 μs** |   **1.4987 μs** |     **baseline** |        **** |  **6.3406** |      **-** |  **411084 B** |            **** |
| ExecuteReader_Dapper                           | JIT | Default           |   282.375 μs |   2.5789 μs |   2.4123 μs | 1.00x slower |   0.01x |  5.8140 |      - |  411116 B |  1.00x more |
| ExecuteReader_DbConnectionPlus                 | JIT | Default           |   280.036 μs |   2.4280 μs |   2.2711 μs | 1.01x faster |   0.01x |  6.0976 |      - |  411724 B |  1.00x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **ExecuteScalar_Command**                          | **JIT** | **Default**           |     **1.914 μs** |   **0.0116 μs** |   **0.0103 μs** |     **baseline** |        **** |  **0.0188** |      **-** |    **1120 B** |            **** |
| ExecuteScalar_Dapper                           | JIT | Default           |     2.166 μs |   0.0142 μs |   0.0111 μs | 1.13x slower |   0.01x |  0.0215 |      - |    1424 B |  1.27x more |
| ExecuteScalar_DbConnectionPlus                 | JIT | Default           |     2.286 μs |   0.0171 μs |   0.0151 μs | 1.19x slower |   0.01x |  0.0307 |      - |    2088 B |  1.86x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Exists_Command**                                 | **JIT** | **Default**           |     **1.625 μs** |   **0.0106 μs** |   **0.0088 μs** |     **baseline** |        **** |  **0.0161** |      **-** |    **1000 B** |            **** |
| Exists_Dapper                                  | JIT | Default           |     1.847 μs |   0.0071 μs |   0.0060 μs | 1.14x slower |   0.01x |  0.0367 |      - |    1336 B |  1.34x more |
| Exists_DbConnectionPlus                        | JIT | Default           |     2.040 μs |   0.0238 μs |   0.0186 μs | 1.26x slower |   0.01x |  0.0338 |      - |    1944 B |  1.94x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **InsertEntities_Command**                         | **JIT** | **Default**           | **1,084.779 μs** |   **5.2549 μs** |   **4.1027 μs** |     **baseline** |        **** | **18.7500** |      **-** | **1129093 B** |            **** |
| InsertEntities_Dapper                          | JIT | Default           | 1,089.550 μs |  14.3120 μs |  12.6873 μs | 1.00x slower |   0.01x | 14.8148 |      - | 1247818 B |  1.11x more |
| InsertEntities_DbConnectionPlus                | JIT | Default           | 1,184.102 μs |   7.7167 μs |   6.8406 μs | 1.09x slower |   0.01x | 19.5313 |      - | 1139668 B |  1.01x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **InsertEntity_Command**                           | **JIT** | **Default**           |     **8.668 μs** |   **0.0370 μs** |   **0.0309 μs** |     **baseline** |        **** |  **0.1447** |      **-** |    **8480 B** |            **** |
| InsertEntity_Dapper                            | JIT | Default           |    14.258 μs |   0.0960 μs |   0.0851 μs | 1.64x slower |   0.01x |  0.2872 |      - |   17608 B |  2.08x more |
| InsertEntity_DbConnectionPlus                  | JIT | Default           |     9.003 μs |   0.0555 μs |   0.0492 μs | 1.04x slower |   0.01x |  0.1217 |      - |    8024 B |  1.06x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Parameter_Command**                              | **JIT** | **Default**           |     **3.390 μs** |   **0.0078 μs** |   **0.0061 μs** |     **baseline** |        **** |  **0.0452** |      **-** |    **2952 B** |            **** |
| Parameter_Dapper                               | JIT | Default           |     5.279 μs |   0.0373 μs |   0.0312 μs | 1.56x slower |   0.01x |  0.2117 |      - |    5016 B |  1.70x more |
| Parameter_DbConnectionPlus                     | JIT | Default           |     5.898 μs |   0.0504 μs |   0.0447 μs | 1.74x slower |   0.01x |  0.3523 |      - |    7376 B |  2.50x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_Dynamic_Command**                          | **JIT** | **Default**           |   **302.573 μs** |   **3.1295 μs** |   **2.6133 μs** |     **baseline** |        **** | **15.1210** | **1.0081** |  **532528 B** |            **** |
| Query_Dynamic_Dapper                           | JIT | Default           |   214.474 μs |   1.1667 μs |   1.0913 μs | 1.41x faster |   0.01x |  0.7267 |      - |   73880 B |  7.21x less |
| Query_Dynamic_DbConnectionPlus                 | JIT | Default           |   276.257 μs |   1.8022 μs |   1.5049 μs | 1.10x faster |   0.01x |  2.7174 |      - |  131944 B |  4.04x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_Entities_Command**                         | **JIT** | **Default**           |   **281.692 μs** |   **2.1036 μs** |   **1.8648 μs** |     **baseline** |        **** |  **7.1023** |      **-** |  **411084 B** |            **** |
| Query_Entities_Dapper                          | JIT | Default           |   234.856 μs |   0.8877 μs |   0.8303 μs | 1.20x faster |   0.01x |  0.7806 |      - |   74105 B |  5.55x less |
| Query_Entities_DbConnectionPlus                | JIT | Default           |   245.137 μs |   0.8514 μs |   0.7547 μs | 1.15x faster |   0.01x |  0.8244 |      - |   64025 B |  6.42x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_Entities_Command**                         | **AOT** | **Latest ILCompiler** |   **305.151 μs** |   **4.2016 μs** |   **3.5085 μs** |     **baseline** |        **** |  **7.0565** |      **-** |  **411091 B** |            **** |
| Query_Entities_Dapper_Aot                      | AOT | Latest ILCompiler |   245.874 μs |   1.4414 μs |   1.2778 μs | 1.24x faster |   0.02x |  2.4351 |      - |   60969 B |  6.74x less |
| Query_Entities_DbConnectionPlus                | AOT | Latest ILCompiler |   321.910 μs |   3.0612 μs |   2.7137 μs | 1.06x slower |   0.01x |  1.0593 |      - |   91244 B |  4.51x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_Scalars_Command**                          | **JIT** | **Default**           |    **81.212 μs** |   **0.2861 μs** |   **0.2389 μs** |     **baseline** |        **** |  **0.2717** |      **-** |   **17288 B** |            **** |
| Query_Scalars_Dapper                           | JIT | Default           |   111.449 μs |   0.6049 μs |   0.5051 μs | 1.37x slower |   0.01x |  0.3720 |      - |   36976 B |  2.14x more |
| Query_Scalars_DbConnectionPlus                 | JIT | Default           |   109.890 μs |   0.4992 μs |   0.4670 μs | 1.35x slower |   0.01x |  0.3655 |      - |   32480 B |  1.88x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_ValueTuples_Command**                      | **JIT** | **Default**           |    **98.529 μs** |   **0.5243 μs** |   **0.4905 μs** |     **baseline** |        **** |  **0.6649** |      **-** |   **47801 B** |            **** |
| Query_ValueTuples_Dapper                       | JIT | Default           |   131.177 μs |   0.5286 μs |   0.4945 μs | 1.33x slower |   0.01x |  1.3193 |      - |   71297 B |  1.49x more |
| Query_ValueTuples_DbConnectionPlus             | JIT | Default           |   130.361 μs |   1.5104 μs |   1.4128 μs | 1.32x slower |   0.02x |  0.9021 |      - |   53137 B |  1.11x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **Query_ValueTuples_Command**                      | **AOT** | **Latest ILCompiler** |   **110.324 μs** |   **0.4156 μs** |   **0.3684 μs** |     **baseline** |        **** |  **0.7267** |      **-** |   **47790 B** |            **** |
| Query_ValueTuples_DbConnectionPlus             | AOT | Latest ILCompiler |   175.588 μs |   1.3776 μs |   1.2212 μs | 1.59x slower |   0.01x |  1.1682 |      - |   84376 B |  1.77x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **TemporaryTable_ComplexObjects_Command**          | **JIT** | **Default**           | **2,728.061 μs** |  **14.5332 μs** |  **13.5944 μs** |     **baseline** |        **** | **46.8750** |      **-** | **3388440 B** |            **** |
| TemporaryTable_ComplexObjects_Dapper           | JIT | Default           | 1,918.002 μs |  34.1135 μs |  28.4863 μs | 1.42x faster |   0.02x | 16.6667 |      - | 1731239 B |  1.96x less |
| TemporaryTable_ComplexObjects_DbConnectionPlus | JIT | Default           | 2,169.543 μs |  39.4448 μs |  36.8967 μs | 1.26x faster |   0.02x | 17.8571 |      - | 1580135 B |  2.14x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **TemporaryTable_ComplexObjects_Command**          | **AOT** | **Latest ILCompiler** | **3,180.703 μs** |  **23.5846 μs** |  **20.9072 μs** |     **baseline** |        **** | **52.0833** |      **-** | **3388726 B** |            **** |
| TemporaryTable_ComplexObjects_DbConnectionPlus | AOT | Latest ILCompiler | 2,650.714 μs |  13.9937 μs |  13.0897 μs | 1.20x faster |   0.01x | 23.4375 |      - | 1648405 B |  2.06x less |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **TemporaryTable_ScalarValues_Command**            | **JIT** | **Default**           | **4,946.405 μs** |  **34.7151 μs** |  **27.1033 μs** |     **baseline** |        **** | **20.8333** |      **-** | **1493512 B** |            **** |
| TemporaryTable_ScalarValues_Dapper             | JIT | Default           | 5,993.998 μs | 117.4418 μs | 115.3436 μs | 1.21x slower |   0.02x | 39.2157 |      - | 3175374 B |  2.13x more |
| TemporaryTable_ScalarValues_DbConnectionPlus   | JIT | Default           | 5,844.893 μs |  38.7475 μs |  32.3559 μs | 1.18x slower |   0.01x | 38.4615 |      - | 2696352 B |  1.81x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **UpdateEntities_Command**                         | **JIT** | **Default**           |   **532.109 μs** |   **2.0057 μs** |   **1.8762 μs** |     **baseline** |        **** |  **6.9444** |      **-** |  **566049 B** |            **** |
| UpdateEntities_Dapper                          | JIT | Default           |   586.049 μs |   4.1645 μs |   3.8954 μs | 1.10x slower |   0.01x | 11.6054 |      - |  663867 B |  1.17x more |
| UpdateEntities_DbConnectionPlus                | JIT | Default           |   587.406 μs |   3.3649 μs |   2.8098 μs | 1.10x slower |   0.01x |  9.7656 |      - |  571057 B |  1.01x more |
|                                                |     |                   |              |             |             |              |         |         |        |           |             |
| **UpdateEntity_Command**                           | **JIT** | **Default**           |     **9.143 μs** |   **0.1013 μs** |   **0.0846 μs** |     **baseline** |        **** |  **0.1225** |      **-** |    **8551 B** |            **** |
| UpdateEntity_Dapper                            | JIT | Default           |    10.748 μs |   0.0497 μs |   0.0465 μs | 1.18x slower |   0.01x |  0.1789 |      - |   12031 B |  1.41x more |
| UpdateEntity_DbConnectionPlus                  | JIT | Default           |     9.630 μs |   0.0560 μs |   0.0468 μs | 1.05x slower |   0.01x |  0.1277 |      - |    8055 B |  1.06x less |

### Running the benchmarks
```shell
pwsh -File scripts/benchmarks.ps1
```

Anything after the script name is forwarded to BenchmarkDotNet, e.g. `--filter *Query_Entities*`. The Native AOT
job needs a C++ toolchain (MSVC on Windows, `clang` and `zlib1g-dev` on Linux); the script also puts `vswhere.exe`
on `PATH`, without which the native link step fails with a misleading `MSB3073`.

## Running the tests

The unit tests need nothing but the SDK:
```shell
dotnet test --project tests\DbConnectionPlus.UnitTests\DbConnectionPlus.UnitTests.csproj
```

The integration tests need a running [Docker](https://www.docker.com/) daemon, and nothing else - there is no
container to start by hand and no connection string to configure:

```shell
dotnet test --project tests\DbConnectionPlus.IntegrationTests\DbConnectionPlus.IntegrationTests.csproj
```

[Testcontainers](https://dotnet.testcontainers.org/) starts MySQL, Oracle, PostgreSQL and SQL Server, waits
until each one accepts connections, and removes them again when the run ends. Containers start **on demand**, so
a run filtered to one database system only pays for that one, and SQLite needs no container at all. Every
container publishes its port to a free port of the host, so nothing collides with a database server installed
locally.

## Contributing
Contributions and bug reports are welcome and appreciated.  
Please follow the repository's [CONTRIBUTING.md](CONTRIBUTING.md) and code style.  
Open a GitHub issue for problems or a pull request with tests and a clear description of changes.

## Links

- [API documentation](https://rent-a-developer.github.io/DbConnectionPlus/)
- [Change log](CHANGELOG.md)
- [Design decisions](DESIGN-DECISIONS.md) - why the library works the way it does
- Licensed under the [MIT license](LICENSE.md)

## Contributors

- David Liebeherr ([info@rent-a-developer.de](mailto:info@rent-a-developer.de))
