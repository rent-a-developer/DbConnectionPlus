# Parameters and temporary tables

Two helpers turn an ordinary interpolated string into a parameterized statement: `Parameter(value)` binds a value, and `TemporaryTable(values)` puts a whole collection into the database for the statement to join against. Both are usable with any method that accepts an `InterpolatedSqlStatement`.

## Parameters via interpolated strings
All extension methods accept interpolated strings where parameter values are captured via
[Parameter(value)](#parametervalue):

```csharp
var lowStockThreshold = configuration.Thresholds.LowStock;

var lowStockProductInfos = connection.Query<(long ProductId, int UnitsInStock)>(
   $"""
    SELECT  Id, UnitsInStock
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```

This prevents SQL injection and keeps the SQL readable.

## On-the-fly temporary tables via interpolated strings
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
    public long ProductId { get; set; }
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

## Special helpers

The following special helpers can be used with any DbConnectionPlus extension method that accepts an instance of 
`InterpolatedSqlStatement`.

### Parameter(value)
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
[EnumSerializationMode](configuration.md#enumserializationmode).

### TemporaryTable(values)
Use `TemporaryTable(values)` to pass a sequence of scalar values or complex objects in an interpolated string as a
temporary table to an SQL statement.

A sequence of scalar values (e.g. `string`, `int`, `DateTime`, enums and so on) produces a temporary table
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
    public long ProductId { get; set; }
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
[EnumSerializationMode](configuration.md#enumserializationmode), and the column is typed `NVARCHAR(200)` for `Strings` and
`INT` for `Integers`.
