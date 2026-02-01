# DbConnectionPlus - Design Decisions Document

**Version:** 1.1.0  
**Last Updated:** February 2026  
**Author:** David Liebeherr

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Core Architectural Patterns](#core-architectural-patterns)
3. [Technology Choices](#technology-choices)
4. [Performance Optimization Decisions](#performance-optimization-decisions)
5. [Database Adapter Architecture](#database-adapter-architecture)
6. [Interpolated String Handler Implementation](#interpolated-string-handler-implementation)
7. [Entity Mapping Strategy](#entity-mapping-strategy)
8. [Temporary Table Design](#temporary-table-design)
9. [Testing Strategy](#testing-strategy)
10. [Configuration and Extensibility](#configuration-and-extensibility)
11. [Trade-offs and Alternatives](#trade-offs-and-alternatives)
12. [Future Considerations](#future-considerations)

---

## Executive Summary

DbConnectionPlus is a lightweight .NET ORM and extension library for `System.Data.Common.DbConnection` that reduces 
boilerplate code while maintaining high performance and type safety.  
This document captures the key design decisions, rationale, and trade-offs made during development.

**Core Design Principles:**
1. **Minimal abstraction overhead** - Performance close to hand-written ADO.NET code
2. **Type safety** - Compile-time validation of SQL parameters and mappings
3. **Multi-database support** - Works with MySQL, Oracle, PostgreSQL, SQLite and SQL Server
4. **Developer productivity** - Interpolated string syntax for natural SQL authoring
5. **Production-ready** - Well-tested and comprehensive error handling

---

## Core Architectural Patterns

### 1. Strategy Pattern - Database Adapters

**Decision:** Use the Strategy pattern to isolate database-specific behavior.

**Implementation:**
```csharp
public interface IDatabaseAdapter
{
    void BindParameterValue(DbParameter parameter, Object? value);
    String FormatParameterName(String parameterName);
    String GetDataType(Type type, EnumSerializationMode enumSerializationMode);
    IEntityManipulator EntityManipulator { get; }
    ITemporaryTableBuilder TemporaryTableBuilder { get; }
    // ... other methods
}
```

**Rationale:**
- Different databases have varying syntax for parameter prefixes (`@`, `:`)
- Temporary table creation differs significantly (SQL Server `#table`, MySQL `CREATE TEMPORARY TABLE`)
- Type mapping varies (SQL Server `DATETIME2` vs PostgreSQL `TIMESTAMP`)
- Cancellation handling is database-specific

**Benefits:**
- Zero code changes required to add support for new database providers
- Database-specific optimizations can be implemented independently
- Testable in isolation from core logic

**Alternatives Considered:**
- **Single adapter with runtime switches**: Would create complex conditional logic
- **Inheritance hierarchy**: Would couple adapters together and reduce flexibility
- **Configuration-based approach**: Would require extensive mapping tables and less compile-time safety

**Why Strategy Pattern Won:**
Provides maximum flexibility and extensibility while maintaining clean separation of concerns. Each adapter is a 
self-contained unit that can be developed and tested independently.

---

### 2. Factory Pattern - Object Materialization

**Decision:** Use Factory pattern for creating materializers that map `DbDataReader` to objects.

**Implementation:**
- `EntityMaterializerFactory` - Creates materializers for entity types
- `ValueTupleMaterializerFactory` - Creates materializers for value tuples

**Rationale:**
- Materializer creation is expensive (requires reflection and expression compilation)
- Different types require different materialization strategies
- Compiled materializers should be cached and reused

**Benefits:**
- Lazy compilation - materializers only created when first needed
- Type-specific optimization strategies
- Centralized caching logic
- Allows use of typed DbDataReader.GetXXX methods for performance
- Minimizes boxing of value types

**Implementation Detail:**
Materializers are compiled LINQ expressions that generate IL code at runtime:

```csharp
// Conceptual example of compiled materializer
Func<DbDataReader, Product> materializer = reader => new Product
{
    Id = reader.GetInt64(0),
    Name = reader.GetString(1),
    Price = reader.GetDecimal(2)
};
```

**Performance Impact:**
- First query for a type: compilation overhead
- Subsequent queries: Near-zero overhead (reuses compiled delegate)
- Much faster than reflection-based mapping for bulk operations

---

### 3. Decorator Pattern - DbDataReader Enhancement

**Decision:** Use Decorator pattern to add cancellation and disposal tracking to `DbDataReader`.

**Implementation:**
```csharp
internal sealed class DisposeSignalingDataReaderDecorator : DbDataReader
{
    private readonly DbDataReader dataReader;
    private readonly Action? onDisposing;
    // ... wraps all DbDataReader methods
}
```

**Rationale:**
- Need to clean up temporary tables when reader is disposed
- Must handle cancellation exceptions from different databases
- Cannot modify the original `DbDataReader` instance

**Benefits:**
- Non-invasive enhancement of existing types
- Enables resource cleanup guarantees
- Maps database-specific exceptions to standard `OperationCanceledException`

**Why Not Inheritance:**
Not possible, because DbCommand.ExecuteReader() returns concrete implementations from database providers. 
Decorator allows wrapping without needing to know the concrete type.

---

### 4. Interpolated String Handler Pattern

**Decision:** Implement C# 11 Interpolated String Handler for SQL statement construction.

**Implementation:**
```csharp
[InterpolatedStringHandler]
public struct InterpolatedSqlStatement
{
    public void AppendLiteral(string literal);
    public void AppendFormatted<T>(T? value, Int32 alignment = 0, String? format = null);
    // ... compiler calls these methods during string construction
}
```

**Why This Is Revolutionary:**
Traditional approach (string concatenation):
```csharp
// Unsafe - vulnerable to SQL injection
var sql = "SELECT * FROM Product WHERE Id = " + productId;

// Safe but verbose
var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM Product WHERE Id = @Id";
command.Parameters.AddWithValue("@Id", productId);
```

Interpolated String Handler approach:
```csharp
// Safe AND concise
connection.ExecuteReader($"SELECT * FROM Product WHERE Id = {Parameter(productId)}");
```

**How It Works:**
1. Compiler sees interpolated string as method parameter
2. Compiler generates calls to `AppendLiteral()` and `AppendFormatted()`
3. Handler captures fragments during string construction
4. Parameter values are safely wrapped in `InterpolatedParameter` objects
5. SQL statement is built with proper parameterization

**Benefits:**
- **Compile-time safety**: Type checking at compilation
- **Zero runtime overhead**: No parsing or reflection
- **Natural syntax**: Reads like SQL, writes like SQL
- **SQL injection prevention**: Parameters automatically escaped

**Alternative Considered:**
Using string parsing at runtime would require:
- Runtime string parsing overhead
- Complex regex or parser implementation
- Risk of injection if parsing is incomplete
- No compile-time validation
---

## Technology Choices

### Target Framework: .NET 8.0

**Decision:** Target .NET 8.0 as the minimum supported version.

**Rationale:**
- **LTS Release**: Long-term support until November 2026
- **Performance**: TieredPGO (Profile-Guided Optimization) enabled by default
- **C# 12 Features**: Collection expressions, primary constructors, etc.
- **Modern APIs**: `ArgumentNullException.ThrowIfNull()`, improved pattern matching
- **Nullable Reference Types**: First-class language support

**Trade-off:**
Excludes .NET Framework and older .NET Core versions, but enables modern C# features and great performance.

---

### Key Library Dependencies

| Library | Purpose | Why Chosen |
|---------|---------|-----------|
| **System.Data.Common** | Core ADO.NET abstractions | Framework-agnostic database access |
| **Database Providers** | SQL Server, MySQL, PostgreSQL, SQLite, Oracle | Official providers from vendors/community |
| **fasterflect.reflect** | Fast reflection | Optimized property access |
| **FastMember** | ObjectReader | Efficient reading of sequences |
| **LinkDotNet.StringBuilder** | Stack-allocated strings | Reduces GC pressure for small strings |
| **Humanizer.Core** | Improved error messages | Best library for producing human readable output |

**Why Minimal Dependencies:**
- Reduces version conflicts in consuming applications
- Easier to maintain and update
- Smaller deployment footprint
- Less vulnerability surface area

---

### C# Language Features

**Leveraged Features:**

1. **Interpolated String Handlers (C# 10)**
   - Core to the library's value proposition
   - Enables natural SQL syntax with compile-time safety

2. **File-Scoped Namespaces (C# 10)**
   - Reduces indentation
   - Cleaner, more readable code

3. **Global Usings (C# 10)**
   - Eliminates repetitive using statements
   - Improves consistency across files

4. **Nullable Reference Types (C# 8)**
   - Compile-time null safety
   - Prevents `NullReferenceException` at runtime
   - Documents nullability intent

5. **Expression-Bodied Members (C# 6)**
   - Enforced via `.editorconfig`
   - Consistent, concise syntax

6. **Pattern Matching Enhancements (C# 7-12)**
   - Used extensively in type conversion logic
   - More readable than cascading `if-else` statements

---

## Performance Optimization Decisions

### 1. Expression Tree Compilation for Materializers

**Decision:** Use compiled LINQ expressions for mapping `DbDataReader` to objects.

**Implementation:**
```csharp
// Simplified conceptual example
Expression<Func<DbDataReader, Product>> mapperExpression = reader => new Product
{
    Id = ConvertValue<long>(reader[0]),
    Name = ConvertValue<string>(reader[1]),
    Price = ConvertValue<decimal>(reader[2])
};

Func<DbDataReader, Product> compiledMapper = mapperExpression.Compile();
```

**Why Not Alternatives:**

| Approach | Performance | Flexibility | Chosen? |
|----------|-------------|-------------|---------|
| **Reflection** | Slowest | High | No |
| **Compiled Expressions** | Fast | High | Yes |
| **Source Generators** | Fastest | Medium | No |
| **Manual Mapping** | Fastest | Low | No |

**Why Compiled Expressions Won:**
- **Runtime flexibility**: Works with any type, including third-party classes
- **Good performance**: Close to that of hand-written code after compilation
- **No build-time requirements**: No additional build steps or tools
- **Debuggable**: Generated expression trees can be inspected

**Why Not Source Generators:**
- Requires compile-time knowledge of all types
- Cannot map dynamically loaded types
- More complex debugging experience
- Additional build complexity

### 2. Stack Allocation for String Building

**Decision:** Use `stackalloc` for SQL statement construction when statement size is relatively small.

**Implementation:**
```csharp
// LinkDotNet.StringBuilder with stack allocation
var builder = new ValueStringBuilder(stackalloc char[500]);
builder.Append("SELECT * FROM ");
builder.Append(tableName);
// ... build SQL statement
```

**Benefits:**
- **Zero heap allocations** for most SQL statements
- **Faster execution**: No GC pressure, better cache locality
- **Predictable performance**: No GC pauses

**Trade-off:**
- Larger methods (stack frames include buffer space)
- Stack overflow risk if overused (mitigated by reasonable limits)

**Measurements:**
- 95% of SQL statements are small enough to fit within 500 characters
- For statements > 500 chars, falls back to heap allocation
- Typical savings: hundreds of bytes to a few KB per query (eliminating intermediate strings)

---

### 3. Fragment-Based SQL Construction

**Decision:** Store SQL statements as fragments (literals, parameters, temporary tables) rather than concatenated 
strings.

**Structure:**
```csharp
public sealed class InterpolatedSqlStatement
{
    internal IReadOnlyList<IInterpolatedSqlStatementFragment> Fragments { get; }
    // Fragments can be:
    // - InterpolatedParameter: $"SELECT * FROM Product WHERE Id = {Parameter(id)}"
    // - InterpolatedTemporaryTable: $"SELECT * FROM Product WHERE Id IN (SELECT Value FROM {TemporaryTable(ids)})"
    // - Literal: "SELECT * FROM Product"
    // - Parameter: Non-interpolated parameter
}
```

**Benefits:**
- **Deferred concatenation**: Build string only when needed (e.g., logging)
- **Parameter inspection**: Can enumerate parameters before execution
- **Immutable design**: Thread-safe, cacheable
- **Debuggable**: Clear separation of SQL code, parameters and temporary tables

**Cost:**
Small allocation overhead vs. monolithic string, but gains outweigh costs.

---

### 4. Lazy Materializer Compilation

**Decision:** Compile materializers only when first needed, then cache indefinitely.

**Implementation:**
```csharp
public static Func<DbDataReader, TEntity> GetMaterializer<T>()
{
    return (Func<DbDataReader, TEntity>) materializerCache.GetOrAdd(
        typeof(TEntity),
        _ => CreateMaterializer<TEntity>()
    );
}

private static readonly ConcurrentDictionary<MaterializerCacheKey, Delegate> materializerCache = [];
```

**Benefits:**
- No upfront cost for unused types
- Compilation overhead amortized across many queries
- Thread-safe caching with `ConcurrentDictionary`

**Benchmark Impact:**
- First query: few ms compilation overhead
- Subsequent queries: no overhead (cache hit)

---

## Database Adapter Architecture

### Adapter Responsibilities

Each `IDatabaseAdapter` implementation handles:

1. **Parameter Binding** - `BindParameterValue(DbParameter, object?)`
   - Type conversions (e.g., `DateOnly` -> `DateTime` for Oracle)
   - Enum serialization (string vs integer)
   - Database-specific type handling (e.g., `GUID` -> string for Oracle)

2. **Parameter Naming** - `FormatParameterName(string)`
   - SQL Server: `@ParameterName`
   - Oracle: `:ParameterName`

3. **Data Type Mapping** - `GetDataType(Type, EnumSerializationMode)`
   - Maps CLR types to SQL types
   - Example: `DateTime` -> `DATETIME2` (SQL Server) vs `DATE` (Oracle)

4. **Identifier Quoting** - `QuoteIdentifier(string)`
   - SQL Server: `[TableName]`
   - MySQL: `` `TableName` ``
   - PostgreSQL/Oracle: `"TableName"`

5. **Entity Manipulator** - `EntityManipulator`
   - CRUD operations for entities
   - Database-specific SQL generation

6. **Temporary Table Builder** - `TemporaryTableBuilder`
   - Creates and populates temporary tables
   - Database-specific syntax and bulk insert strategies

7. **Cancellation Detection** - `WasSqlStatementCancelledByCancellationToken(Exception, CancellationToken)`
   - Maps database exceptions to cancellation events
   - SQL Server: Exception with class: 11, number: 0, state: 0
   - Oracle: ORA-01013 error code

---

### Adapter Registration

**Decision:** Use static registry.

**Implementation:**
```csharp
public static class DatabaseAdapterRegistry
{
    static DatabaseAdapterRegistry()
    {
        // Auto-register known adapters
        adapters.TryAdd(typeof(SqlConnection), new SqlServerDatabaseAdapter());
        adapters.TryAdd(typeof(SqliteConnection), new SqliteDatabaseAdapter());
        adapters.TryAdd(typeof(MySqlConnection), new MySqlDatabaseAdapter());
        adapters.TryAdd(typeof(NpgsqlConnection), new PostgreSqlDatabaseAdapter());
        adapters.TryAdd(typeof(OracleConnection), new OracleDatabaseAdapter());
    }

    private static readonly ConcurrentDictionary<Type, IDatabaseAdapter> adapters = [];
}
```

**Benefits:**
- Zero configuration for supported databases
- Discovery via connection type
- Custom adapters can be registered at runtime

**Alternative Considered:**
Reflection-based discovery (scan assemblies for adapters) - rejected due to:
- Performance overhead
- Dependency on adapter assembly structure

---

## Interpolated String Handler Implementation

### Design Overview

**Goal:** Enable natural SQL syntax with compile-time safety and zero runtime parsing overhead.

**C# Compiler Integration:**
When the compiler sees an interpolated string as a method parameter of a type with an `[InterpolatedStringHandler]` 
attribute, it generates calls to the handler's `AppendLiteral()` and `AppendFormatted()` methods during string 
construction.

### Handler Structure

```csharp
[InterpolatedStringHandler]
public struct InterpolatedSqlStatement : IEquatable<InterpolatedSqlStatement>
{
    public InterpolatedSqlStatement(int literalLength, int formattedCount)
    {
        // Pre-allocate fragment list based on compiler hints
        this.fragments = new(formattedCount);
    }
    
    public void AppendLiteral(String? value)
    {
        if (value is not null)
        {
            this.fragments.Add(new Literal(value));
        }
    }
    
    public void AppendFormatted<T>(T? value, Int32 alignment = 0, String? format = null)
    {
        switch (value)
        {
            case InterpolatedParameter interpolatedParameter:
                this.fragments.Add(interpolatedParameter);
                break;

            case InterpolatedTemporaryTable interpolatedTemporaryTable:
                this.fragments.Add(interpolatedTemporaryTable);
                break;

            ...
        }
    }

    private readonly List<IInterpolatedSqlStatementFragment> fragments;
}
```

### Fragment Types

1. **InterpolatedParameter** - Interpolated parameter
   ```csharp
   connection.Query<Product>($"SELECT * FROM Product WHERE Id = {Parameter(productId)}")
   ```

2. **InterpolatedTemporaryTable** - Temporary table reference
   ```csharp
   connection.Query<Product>($"SELECT * FROM Product WHERE Id IN (SELECT Value FROM {TemporaryTable(productIds)})")
   ```

1. **Literal** - Raw SQL text
   ```csharp
   connection.Query<Product>("SELECT * FROM Product")
   ```

2. **Parameter** - Parameterized value
   ```csharp
   connection.Query<Product>(
       new InterpolatedSqlStatement("SELECT * FROM Product WHERE Id = @Id", ("Id", productId))
   )
   ```

### Parameter Name Inference

**Challenge:** Generate meaningful parameter names from expressions.

**Solution:** Use `[CallerArgumentExpression]` attribute (C# 10):

```csharp
public static InterpolatedParameter Parameter(
    Object? value,
    [CallerArgumentExpression(nameof(value))] String? parameterValueExpression = null
)
{
    // expression = "productId" when called as Parameter(productId)
    var nameFromCallerArgumentExpression = NameHelper.CreateNameFromCallerArgumentExpression(
        parameterValueExpression,
        maximumLength: MaximumParameterNameLength
    );

    return new InterpolatedParameter(nameFromCallerArgumentExpression, value);
}
```

**Name Generation Logic:**
1. Simple identifier (`productId`) -> `ProductId`
2. Member access (`product.Id`) -> `ProductId`
3. Complex expression (`user.Orders[0].Total`) -> `UserOrders0Total`
4. Unable to infer -> `Parameter_1`, `Parameter_2`, etc.

**Maximum Name Length:** 60 characters (all major databases support this length)

---

## Entity Mapping Strategy

### Fluent API-based Configuration

**Decision:** Provide optional fluent API for entity configuration.

**Example:**
```csharp
DbConnectionExtensions.Configure(config =>
    {
        // Table name mapping:
        config.Entity<Product>()
            .ToTable("Products");

        // Column name mapping:
        config.Entity<Product>()
            .Property(a => a.Name)
            .HasColumnName("ProductName");

        // Key column mapping:
        config.Entity<Product>()
            .Property(a => a.Id)
            .IsKey();

        // Database generated column mapping:
        config.Entity<Product>()
            .Property(a => a.DiscountedPrice)
            .IsDatabaseGenerated();

        // Ignored property mapping:
        config.Entity<Product>()
            .Property(a => a.IsOnSale)
            .Ignore();
    }
);
```

**Benefits:**
- **Mostly EF Core compatible**: Similar API as of EF core
- **Convenient**: Provides convinient way to configure entities without attributes

### Attribute-Based Configuration

**Decision:** Use standard .NET data annotations for entity metadata.

**Supported Attributes:**
```csharp
[Table("Products")]              // Override table name
public class Product
{
    [Key]                         // Identify primary key
    public Int64 Id { get; set; }
    
    [NotMapped]                   // Exclude from mapping
    public Decimal TotalPrice => UnitPrice * Quantity;
}
```

**Benefits:**
- **Standard framework**: No custom attribute library
- **EF Core compatible**: Works with existing entity definitions
- **Well-documented**: Developers already familiar

**Why Not Custom Attributes:**
Avoid NIH (Not Invented Here) syndrome. Standard attributes are:
- Better documented
- More discoverable
- Compatible with other ORMs/tools

---

### Entity Metadata Caching

**Decision:** Cache entity metadata (table name, properties, key property) in static dictionary.

**Implementation:**
```csharp
public static class EntityHelper
{
    public static EntityTypeMetadata GetEntityTypeMetadata(Type entityType)
    {
        return entityTypeMetadataPerEntityType.GetOrAdd(
            entityType,
            static entityType2 => CreateEntityTypeMetadata(entityType2)
        );
    }

    private static readonly
        ConcurrentDictionary<Type, EntityTypeMetadata> entityTypeMetadataPerEntityType = [];
}
```

**Cached Information:**
- Table name (from `[Table]` attribute, fluent API config or type name)
- Metadata of properties:
  - Mapped properties (excluding ignored properties)
  - Key properties
  - Computed properties
  - Identity property
  - Database generated properties
  - Insert properties (properties to be included when inserting an entity)
  - Update properties (properties to be included when updating an entity)

**Performance Impact:**
- First entity operation: few ms for metadata extraction
- Subsequent operations: Near-zero overhead (cache hit)

---

### Constructor vs Property Initialization

**Decision:** Support both constructor-based and property-based entity initialization.

**Priority Order:**
1. Try constructor with parameters matching database columns
2. Fall back to parameterless constructor + property setters
3. Throw if neither approach works

**Example:**
```csharp
// Constructor-based (preferred for immutable types)
public class Product
{
    public Product(Int64 id, String name, Decimal price)
    {
        this.Id = id;
        this.Name = name;
        this.Price = price;
    }
    
    public Int64 Id { get; }
    public String Name { get; }
    public Decimal Price { get; }
}

// Property-based (traditional approach)
public class Product
{
    public Int64 Id { get; set; }
    public String Name { get; set; } = "";
    public Decimal Price { get; set; }
}
```

**Benefits:**
- Supports modern C# patterns (records, init-only properties)
- Backward compatible with traditional entity classes
- No framework lock-in

---

## Temporary Table Design

### Why Temporary Tables?

**Problem:** Need to pass collections of values to SQL queries efficiently.

**Traditional Approaches:**
1. **IN clause with parameters**: Limited to small collections
2. **Multiple queries**: Network round-trips kill performance
3. **XML/JSON parameters**: Parsing overhead, database version dependencies
4. **Table-valued parameters**: SQL Server-specific, complex setup

**DbConnectionPlus Approach:** On-the-fly temporary tables

```csharp
var productIds = GetProductIds(); // Could be 10,000+ items

var products = connection.Query<Product>(
    $"""
    SELECT  *
    FROM    Product
    WHERE   Id IN (SELECT Value FROM {TemporaryTable(productIds)})
    """
);
```

---

### Temporary Table Lifecycle

**Creation:**
1. `TemporaryTable(values)` captures collection reference
2. `DbCommandBuilder` detects temporary table fragments
3. For each temporary table:
   - Generate unique table name with GUID suffix
   - Infer schema from collection element type
   - Call `ITemporaryTableBuilder.BuildTemporaryTable()` for each temporary table
   - Populate table using bulk insert (if supported) or individual inserts

**Usage:**
- `{TemporaryTable(values)}` is replaced with actual table name in SQL

**Cleanup (ExecuteNonQuery, Query, ...):**
- Extension methods calls `TemporaryTableDisposer.Dispose()` when SQL statement execution completes
- Cleanup is guaranteed even if exception occurs

**Cleanup (ExecuteReader):**
- `DisposeSignalingDataReaderDecorator` tracks reader disposal
- When reader disposed, calls `TemporaryTableDisposer.Dispose()`
- Cleanup is guaranteed even if exception occurs

---

### Database-Specific Implementations

**MySQL:**
```sql
-- Create
CREATE TEMPORARY TABLE `ProductIds_abc123` (`Value` BIGINT)
-- Bulk insert via MySqlBulkCopy
```

**Oracle:**
```sql
-- Create
CREATE PRIVATE TEMPORARY TABLE "ProductIds_abc123" ("Value" NUMBER(19))
INSERT INTO "ProductIds_abc123" ("Value") VALUES (1), (2), (3)
```

**PostgreSQL:**
```sql
-- Create
CREATE TEMP TABLE "ProductIds_abc123" ("Value" bigint)
-- Bulk insert via NpgsqlBinaryImporter
```

**SQLite:**
```sql
-- Create
CREATE TEMP TABLE "ProductIds_abc123" ("Value" INTEGER)
INSERT INTO "ProductIds_abc123" ("Value") VALUES (1), (2), (3)
```

**SQL Server:**
```sql
-- Create
CREATE TABLE #ProductIds_abc123 (Value BIGINT)
-- Bulk insert via SqlBulkCopy
```

---

### Bulk Insert Optimization

**Decision:** Use database-specific bulk insert when available.

**Benefits:**
- Much faster than individual inserts for large collections
- Reduced network round-trips
- Lower transaction log overhead

**Implementations:**
- MySQL: `LOAD DATA LOCAL INFILE` via `MySqlBulkCopy`
- Oracle: INSERT statements (no native bulk insert)
- PostgreSQL: `COPY` command via `NpgsqlBinaryImporter`
- SQLite: INSERT statements (no native bulk insert)
- SQL Server: `SqlBulkCopy`

**Fallback:** If bulk insert fails, fall back to individual `INSERT` statements.

---

## Testing Strategy

### Test Organization

**Three-Tier Approach:**

1. **Unit Tests** (`DbConnectionPlus.UnitTests`)
   - Test core logic in isolation
   - Mock `DbConnection`, `DbDataReader`, etc.
   - Fast execution (~1-2 seconds total)
   - ~1000 tests covering edge cases

2. **Integration Tests** (`DbConnectionPlus.IntegrationTests`)
   - Test against real databases
   - Docker containers for MySQL, Oracle, PostgreSQL and SQL Server
   - Slower execution (couple of minutes)
   - Database-specific behavior validation

3. **Benchmarks** (`DbConnectionPlus.Benchmarks`)
   - Performance regression detection
   - Compare against hand-written ADO.NET code
   - BenchmarkDotNet for accurate measurements

---

### Unit Test Highlights

**Property-Based Testing:**
```csharp
[Theory]
[MemberData(nameof(GetCanConvertTestData))]
public void CanConvert_ShouldDetermineIfConversionIsPossible(
        Type sourceType,
        Type targetType,
        Boolean expectedCanConvert,
        Object? sourceValue,
        Object? expectedTargetValue
)
{
    ValueConverter.CanConvert(sourceType, targetType)
        .Should().Be(
            expectedCanConvert,
            because: $"{sourceType} should {(expectedCanConvert ? "" : "not ")}be convertible to {targetType}"
        );

    if (expectedCanConvert)
    {
        MaterializerFactoryHelper.ValueConverterConvertValueToTypeMethod.MakeGenericMethod(targetType)
            .Invoke(null, [sourceValue])
            .Should().Be(expectedTargetValue);
    }
    else
    {
        Invoking(() =>
                MaterializerFactoryHelper.ValueConverterConvertValueToTypeMethod.MakeGenericMethod(targetType)
                    .Invoke(null, [sourceValue])
            )
            .Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidCastException>()
            .WithMessage(
                $"Could not convert the value {sourceValue.ToDebugString()} to the type {targetType}.*"
            );
    }
}
```

**Snapshot Testing:**
```csharp
[Fact]
public async Task PublicApiHasNotChanged()
{
    var publicApi = typeof(DbConnectionExtensions).Assembly.GeneratePublicApi(apiGeneratorOptions);

    return Verify(publicApi); // Verify.XunitV3
}
```

**Null Guard Verification:**
```csharp
[Fact]
public void ShouldGuardAgainstNullArguments()
{
    ArgumentNullGuardVerifier.Verify(() => EntityHelper.FindParameterlessConstructor(typeof(ItemWithConstructor)));
    ArgumentNullGuardVerifier.Verify(() => EntityHelper.GetEntityReadableProperties(typeof(Entity)));
    ArgumentNullGuardVerifier.Verify(() => EntityHelper.GetEntityReadablePropertyNames(typeof(Entity)));
    ArgumentNullGuardVerifier.Verify(() => EntityHelper.GetEntityTypeMetadata<Entity>(this.MockDatabaseAdapter));
    ArgumentNullGuardVerifier.Verify(() => EntityHelper.GetEntityWritableProperties(typeof(Entity)));
}
```

---

### Integration Test Strategy

**Database Providers:**
Each database has a `ITestDatabaseProvider` implementation that:
1. Creates test database
2. Executes setup scripts
3. Provides connection
4. Cleans up after tests

**Docker Compose:**
```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest

  mysql:
    image: mysql:8.0-debian

  postgresql:
    image: postgres:latest

  oracle:
    image: gvenzl/oracle-free:latest
```

**Test Database Lifecycle:**
1. `docker-compose up` - Start database containers
2. Each test method gets a fresh connection (except SQLite)
4. `docker-compose down` - Cleanup

**Configuration:**
Connection strings defined in `Local.runsettings`:
```xml
<RunSettings>
	<RunConfiguration>
		<EnvironmentVariables>
			<ConnectionString_SqlServer>
				Data Source=localhost,11433;User ID=sa;Password=TestTest123!;Encrypt=False;MultipleActiveResultSets=True
			</ConnectionString_SqlServer>
			<ConnectionString_MySql>
				server=localhost;port=13306;uid=root;pwd=TestTest123!;AllowLoadLocalInfile=true
			</ConnectionString_MySql>
			<ConnectionString_PostgreSQL>
				Host=localhost;Port=15432;Username=postgres;Password=TestTest123!
			</ConnectionString_PostgreSQL>
			<ConnectionString_Oracle>
				Data Source=localhost:11521/FREEPDB1;User Id=SYSTEM;Password=TestTest123!
			</ConnectionString_Oracle>
		</EnvironmentVariables>
	</RunConfiguration>
</RunSettings>
```

---

### Benchmark Methodology

**Comparison Approach:**
Each benchmark includes two variants:
1. **Manual** - Hand-written ADO.NET code
2. **DbConnectionPlus** - Equivalent operation using extension methods

**Example:**
```csharp
[Benchmark(Baseline = true)]
public List<Product> Query_Entities_Manually()
{
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT Id, Name, Price FROM Product";
    
    using var reader = command.ExecuteReader();
    
    var products = new List<Product>();
    
    while (reader.Read())
    {
        products.Add(new Product
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            Price = reader.GetDecimal(2)
        });
    }

    return products;
}

[Benchmark]
public List<Product> Query_Entities_DbConnectionPlus()
{
    return connection.Query<Product>("SELECT Id, Name, Price FROM Product").ToList();
}
```

**Metrics Tracked:**
- Execution time (mean, median, P90, P95)
- Memory allocations
- Ratio compared to baseline

---

## Configuration and Extensibility

### Global Configuration

**Decision:** Provide a config method for configuring global settings.

**Best Practice:**
Set during application startup before any database operations:
```csharp
// In Program.cs or Startup.cs

DbConnectionExtensions.Configure(config =>
{
    config.EnumSerializationMode = EnumSerializationMode.Integers;
});
```

### Entity type mapping configuration

**Decision:** Provide a Fluent API to configure entity type mapping.

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

DbConnectionExtensions.Configure(config =>
{
    config.Entity<Product>()
        .ToTable("Products");

    config.Entity<Product>()
        .Property(a => a.Id)
        .HasColumnName("ProductId");
        .IsIdentity()
        .IsKey();

    config.Entity<Product>()
        .Property(a => a.DiscountedPrice)
        .IsComputed();

    config.Entity<Product>()
        .Property(a => a.IsOnSale)
        .IsIgnored();
});
```

---

### Custom Database Adapter

**Extensibility Point:** Support for databases not included out-of-the-box.

**Example:**
```csharp
public class MyCustomDatabaseAdapter : IDatabaseAdapter
{
    public void BindParameterValue(DbParameter parameter, Object? value)
    {
        // Custom parameter binding logic
    }
    
    public String FormatParameterName(String parameterName)
        => "%" + parameterName; // Custom prefix
    
    // ... implement other methods
}

// Register adapter
DbConnectionExtensions.Configure(config =>
{
    config.RegisterDatabaseAdapter<MyCustomConnection>(new MyCustomDatabaseAdapter());
});
```

**Use Cases:**
- Legacy databases (e.g., DB2, Informix)
- Cloud-native databases (e.g., Snowflake, BigQuery)
- Proprietary databases

---

## Trade-offs and Alternatives

### 1. Extension Methods vs. Fluent API

**Chosen:** Extension methods on `DbConnection`

**Alternative:** Fluent API builder pattern
```csharp
// Fluent API (not chosen)
var query = new QueryBuilder(connection)
    .Select("*")
    .From("Product")
    .Where("Id", Operator.Equals, productId)
    .Build();
```

**Why Extension Methods:**
- Works with existing `DbConnection` objects (no wrapper)
- Familiar pattern for .NET developers
- Natural SQL syntax via interpolated strings
- No object allocation overhead

**Trade-off:**
Fluent API would enable more sophisticated query building but at the cost of verbosity and learning curve.

---

### 2. Synchronous + Asynchronous vs. Async-Only

**Chosen:** Both sync and async versions of all methods

**Alternative:** Async-only API (modern trend)

**Why Both:**
- Enterprise codebases still have synchronous code paths
- Performance overhead of async machinery in CPU-bound scenarios

**Naming Convention:**
- Synchronous: `Query()`, `ExecuteReader()`, etc.
- Asynchronous: `QueryAsync()`, `ExecuteReaderAsync()`, etc.

**Trade-off:**
Doubles the API surface but provides better developer experience and compatibility.

---

### 3. Static Global State vs. Dependency Injection

**Current:** Static global state (`EnumSerializationMode`, `DatabaseAdapterRegistry`)

**Alternative:** Dependency injection with `IDbConnectionPlusOptions`

**Why Static:**
- Simpler for quick adoption
- Zero ceremony for getting started
- No DI container required
- Configuration rarely changes at runtime

**Future Consideration:**
Could add optional DI support in future version:
```csharp
services.AddDbConnectionPlus(options => 
{
    options.EnumSerializationMode = EnumSerializationMode.Integers;
});
```

**Current Recommendation:**
Use static configuration unless you need per-scope settings (rare).

---

### 4. Caching Strategy - Forever vs. LRU

**Chosen:** Cache compiled materializers forever (no eviction)

**Alternative:** LRU cache with size limit

**Why Forever:**
- Applications typically work with finite set of entity types
- Materializer memory footprint is tiny
- No cache misses after warm-up
- Simpler implementation

**Risk Mitigation:**
If application dynamically generates many unique types (rare), materializer cache could grow unbounded.

**Typical Memory Usage:**
- 100 entity types x few KB per materializer = few hundred KB (negligible)

---

### 5. Temporary Table Naming - GUID vs. Sequence

**Chosen:** GUID suffix for temporary table names

**Alternative:** Sequential counter or timestamp

**Why GUID:**
- Guaranteed uniqueness across connections
- Thread-safe (no shared counter)
- No collision risk in connection pooling scenarios

**Format:**
```
TableName_abc123def456789...  (GUID without hyphens)
```

**Trade-off:**
Longer names (but still within database limits), better safety.

---

## Future Considerations

### Potential Enhancements

1. **Dependency Injection Support**
   - Optional extension methods for `IServiceCollection`
   - Per-scope configuration via `IOptions<DbConnectionPlusOptions>`
   - Better integration with ASP.NET Core

2. **Custom Exception Hierarchy**
   - `DbConnectionPlusException` base class
   - Specific exceptions (e.g., `AdapterNotRegisteredException`)
   - Easier error handling for library consumers

3. **Query Result Caching**
   - Optional in-memory cache for query results
   - Configurable cache policies (TTL, size limits)
   - Useful for read-heavy scenarios

4. **Batch Operations**
   - `InsertEntitiesBatch()` with chunking
   - Automatic batching for large collections
   - Progress reporting for long-running operations

5. **Connection Resiliency**
   - Automatic retry policies for transient failures
   - Integration with Polly or similar libraries
   - Configurable retry strategies

---

### Breaking Changes to Avoid

To maintain backward compatibility in future versions:

1. **Never change method signatures** of public API
2. **Never remove public methods** (mark as `[Obsolete]` instead)
3. **Never change default behavior** (add opt-in flags for new behavior)
4. **Never change exception types** thrown by existing methods
5. **Never change thread-safety guarantees** (can only make stronger, not weaker)

---

### Performance Improvement Opportunities

1. **Source Generators (C# 9)**
   - Generate materializers at compile time
   - Eliminate reflection and expression compilation overhead
   - Requires users to reference source generator NuGet package

2. **Span-Based APIs**
   - `ReadOnlySpan<T>` for temporary collections
   - Reduce allocations for parameter arrays
   - Requires .NET 6+ for Span support in more places

3. **Multi-Result Sets**
   - Support for queries that return multiple result sets
   - Avoid multiple round-trips for related data
   - Example: `QueryMultiple<Product, OrderItem>(sql)`

---

## Conclusion

DbConnectionPlus demonstrates a careful balance between:
- **Developer Productivity** - Natural SQL syntax with compile-time safety
- **Performance** - Close to that of hand-written ADO.NET code
- **Extensibility** - Support for any database via adapter pattern
- **Simplicity** - Minimal abstraction, easy to understand
- **Production Quality** - Well-tested and comprehensive error handling

The design decisions documented here prioritize:
1. **Type safety** over dynamic flexibility
2. **Compile-time validation** over runtime checking
3. **Performance** over convenience (but achieving both where possible)
4. **Explicit behavior** over implicit magic
5. **Standard patterns** over custom frameworks

These principles have resulted in a library that is both powerful and easy to use, suitable for everything from 
prototypes to production enterprise applications.

---

**Document Version:** 1.0  
**Last Updated:** January 2026  
**Questions or Feedback:** Please open an issue on GitHub