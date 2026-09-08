# Querying

Every method here is an extension method on `DbConnection`, every one has an `…Async` counterpart, and all of them accept an optional transaction, command timeout, command type and cancellation token.

The examples assume the static helpers are imported:

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
```

## General-purpose methods

### ExecuteNonQuery / ExecuteNonQueryAsync
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

### ExecuteReader / ExecuteReaderAsync
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

### ExecuteScalar / ExecuteScalarAsync
Executes an SQL statement and returns the value of the first column of the first row in the result set converted to 
the specified type.
```csharp
var lowStockThreshold = configuration.Thresholds.LowStock;

var numberOfLowStockProducts = connection.ExecuteScalar<int>(
   $"""
    SELECT  COUNT(*)
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```

### Exists / ExistsAsync
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

## Query methods

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
[Native AOT and trimming](native-aot.md).

### Query / QueryAsync
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

### QueryFirst / QueryFirstAsync
Executes an SQL statement and maps the first row of the result set to a `DataRow`.
Throws if no rows are found.
```csharp
var product = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(id)}");

var id = product["Id"];
var name = product["Name"];
...
```

### QueryFirstOrDefault / QueryFirstOrDefaultAsync
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

### QuerySingle / QuerySingleAsync
Executes an SQL statement and maps the single row of the result set to a `DataRow`.
Throws if no rows or more than one row are found.
```csharp
var product = connection.QuerySingle($"SELECT * FROM Product WHERE Id = {Parameter(id)}");

var id = product["Id"];
var name = product["Name"];
...
```

### QuerySingleOrDefault / QuerySingleOrDefaultAsync
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

### Query\<T\> / QueryAsync\<T\>
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
var lowStockProductIds = connection.Query<long>(
   $"""
    SELECT  Id
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);

// Value tuples
var lowStockProductInfos = connection.Query<(long ProductId, int UnitsInStock)>(
   $"""
    SELECT  Id, UnitsInStock
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```

### QueryFirst\<T\> / QueryFirstAsync\<T\>
Executes an SQL statement and maps the first row of the result set to a scalar value, entity or value tuple of the 
specified type.
Throws if no rows are found.
```csharp
var product = connection.QueryFirst<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

### QueryFirstOrDefault\<T\> / QueryFirstOrDefaultAsync\<T\>
Executes an SQL statement and maps the first row of the result set to a scalar value, entity or value tuple of the 
specified type or default value if no rows are found.
```csharp
var product = connection.QueryFirstOrDefault<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

### QuerySingle\<T\> / QuerySingleAsync\<T\>
Executes an SQL statement and maps the single row of the result set to a scalar value, entity or value tuple of the 
specified type.
Throws if no rows or more than one row are found.
```csharp
var product = connection.QuerySingle<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

### QuerySingleOrDefault\<T\> / QuerySingleOrDefaultAsync\<T\>
Executes an SQL statement and maps the single row of the result set to a scalar value, entity or value tuple of the 
specified type or default value if no rows are found.
Throws if more than one row are found.
```csharp
var product = connection.QuerySingleOrDefault<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```
