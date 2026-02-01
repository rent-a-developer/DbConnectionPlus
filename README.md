[![NuGet Version](https://img.shields.io/nuget/v/RentADeveloper.DbConnectionPlus)](https://www.nuget.org/packages/RentADeveloper.DbConnectionPlus/)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=rent-a-developer_DbConnectionPlus&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=rent-a-developer_DbConnectionPlus)
[![license](https://img.shields.io/badge/License-MIT-purple.svg)](LICENSE.md)
![semver](https://img.shields.io/badge/semver-1.1.0-blue)

# ![image icon](https://raw.githubusercontent.com/rent-a-developer/DbConnectionPlus/main/icon.png) DbConnectionPlus
A lightweight .NET ORM and extension library for the type
[DbConnection](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection)
that adds high-performance, type-safe helpers to reduce boilerplate code, boost productivity, and make working with 
SQL databases in C# more enjoyable.

Highlights:
- Parameterized interpolated-string support
- On-the-fly temporary tables from in-memory collections
- Entity mapping helpers (insert, update, delete, query)
- Designed to be used in synchronous and asynchronous code paths

The following database systems are supported out of the box:
- MySQL (via [MySqlConnector](https://www.nuget.org/packages/MySqlConnector/))
- Oracle Database (via [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core/))
- PostgreSQL (via [Npgsql](https://www.nuget.org/packages/Npgsql/))
- SQLite (via [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite/))
- SQL Server (via [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/))

Other database systems and database connectors can be supported by implementing a 
[custom database adapter](#custom-database-adapter).

All examples in this document use SQL Server.

## Table of contents
- **[Quick start](#quick-start)**
- [Examples](#examples)
- [Parameters via interpolated strings](#parameters-via-interpolated-strings)
- [On-the-fly temporary tables via interpolated strings](#on-the-fly-temporary-tables-via-interpolated-strings)
- [Enum support](#enum-support)
- [API summary](#api-summary)
- [Configuration](#configuration)
    - [EnumSerializationMode](#enumserializationmode)
    - [InterceptDbCommand](#interceptdbcommand)
    - [Entity Mapping](#entity-mapping)
        - [Fluent API](#fluent-api)
        - [Data annotation attributes](#data-annotation-attributes)
- [General-purpose methods](#general-purpose-methods)
    - [ExecuteNonQuery / ExecuteNonQueryAsync](#executenonquery--executenonqueryasync)
    - [ExecuteReader / ExecuteReaderAsync](#executereader--executereaderasync)
    - [ExecuteScalar / ExecuteScalarAsync](#executescalar--executescalarasync)
    - [Exists / ExistsAsync](#exists--existsasync)
- [Query methods](#query-methods)
    - [Query / QueryAsync](#query--queryasync)
    - [QueryFirst / QueryFirstAsync](#queryfirst--queryfirstasync)
    - [QueryFirstOrDefault / QueryFirstOrDefaultAsync](#queryfirstordefault--queryfirstordefaultasync)
    - [QuerySingle / QuerySingleAsync](#querysingle--querysingleasync)
    - [QuerySingleOrDefault / QuerySingleOrDefaultAsync](#querysingleordefault--querysingleordefaultasync)
    - [Query\<T\> / QueryAsync\<T\>](#queryt--queryasynct)
    - [QueryFirst\<T\> / QueryFirstAsync\<T\>](#queryfirstt--queryfirstasynct)
    - [QueryFirstOrDefault\<T\> / QueryFirstOrDefaultAsync\<T\>](#queryfirstordefaultt--queryfirstordefaultasynct)
    - [QuerySingle\<T\> / QuerySingleAsync\<T\>](#querysinglet--querysingleasynct)
    - [QuerySingleOrDefault\<T\> / QuerySingleOrDefaultAsync\<T\>](#querysingleordefaultt--querysingleordefaultasynct)
- [Entity manipulation methods](#entity-manipulation-methods)
    - [InsertEntities / InsertEntitiesAsync](#insertentities--insertentitiesasync)
    - [InsertEntity / InsertEntityAsync](#insertentity--insertentityasync)
    - [UpdateEntities / UpdateEntitiesAsync](#updateentities--updateentitiesasync)
    - [UpdateEntity / UpdateEntityAsync](#updateentity--updateentityasync)
    - [DeleteEntities / DeleteEntitiesAsync](#deleteentities--deleteentitiesasync)
    - [DeleteEntity / DeleteEntityAsync](#deleteentity--deleteentityasync)
- [Special helpers](#special-helpers)
    - [Parameter(value)](#parametervalue)
    - [TemporaryTable(values)](#temporarytablevalues)
- [Custom database adapter](#custom-database-adapter)
- [Benchmarks](#benchmarks)
- [Running the benchmarks](#running-the-benchmarks)
- [Running the unit tests](#running-the-unit-tests)
- [Running the integration tests](#running-the-integration-tests)
- [Contributing](#contributing)
- [License](#license)
- [Documentation](#documentation)
- [Change Log](#change-log)
- [Contributors](#contributors)

## Quick start
First, [install NuGet](https://docs.nuget.org/docs/start-here/installing-nuget).

Then install the [NuGet package](https://www.nuget.org/packages/RentADeveloper.DbConnectionPlus/) from the package
manager console:
```shell
PM> Install-Package RentADeveloper.DbConnectionPlus
```

Import the library and the static helpers:

```csharp
using Microsoft.Data.SqlClient;
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
```

Open or reuse a `DbConnection` and use the extension methods:

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

class Product
{
    [Key]
    public Int64 Id { get; set; }
    public Int32 UnitsInStock { get; set; }
}

class OrderItem
{
    [Key]
    public Int64 Id { get; set; }
    public Int64 ProductId { get; set; }
    public DateTime OrderDate { get; set; }
}

var lowStockThreshold = configuration.Thresholds.LowStock;

using var lowStockProductsReader = connection.ExecuteReader(
   $"""
    SELECT  *
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);

...

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

## Examples

### Parameters via interpolated strings
All extension methods accept interpolated strings where parameter values are captured via
[Parameter(value)](#parametervalue):

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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
> When using the temporary tables feature of DbConnectionPlus with an Oracle database, please be aware of the 
> following implications:  
> The temporary tables feature of DbConnectionPlus creates private temporary tables and drops them after use.  
> Unfortunately DDL statements (like creating and dropping a private temporary table) cause an implicit commit of the 
> current transaction in an Oracle database.  
> That means if you use the temporary tables feature inside an explicit transaction, the transaction will be committed 
> when the temporary table is created and again when it is dropped!  
> Therefore, when using DbConnectionPlus with Oracle databases, avoid using the temporary tables feature inside 
> explicit transactions or at least be aware of the implications.  
> For this reason the temporary tables feature is disabled by default for Oracle databases in DbConnectionPlus and 
> attempting to use it will throw an exception.  
> To enable the feature, set the property 
> `RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle.OracleDatabaseAdapter.AllowTemporaryTables` to `true`.

> [!NOTE]
> **Note for MySQL users**
> The temporary tables feature of DbConnectionPlus uses `MySqlBulkCopy` to populate temporary tables.
> Therefore, the option `AllowLoadLocalInfile=true` must be set in the connection string and the server side
> option `local_infile` must be enabled (e.g. via the statement `SET GLOBAL local_infile=1`).

Create a temporary table on the fly from an `IEnumerable<T>` and use it in statements via
[TemporaryTable(values)](#temporarytablevalues):

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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
Enum values are either mapped to their string representation or to integers when sent to the database:

When `DbConnectionExtensions.EnumSerializationMode` is set to `EnumSerializationMode.String`, enums are stored as
strings:

```sql
CREATE TABLE Users
(
    Id BIGINT,
    UserName NVARCHAR(255),
    Role NVARCHAR(200)
)
```

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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

var user = new User
{
    Id = 1,
    UserName = "adminuser",
    Role = UserRole.User
};

connection.InsertEntity(user); // Column "Role" will contain the string "User".
```

When `DbConnectionExtensions.EnumSerializationMode` is set to `EnumSerializationMode.Integer`, enums are stored as
integers:

```sql
CREATE TABLE Users
(
    Id BIGINT,
    UserName NVARCHAR(255),
    Role INT
)
```

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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

var user = new User
{
    Id = 1,
    UserName = "adminuser",
    Role = UserRole.User
};

connection.InsertEntity(user); // Column "Role" will contain the integer 2.
```

When reading data from the database, this library automatically maps string and integer values back to the
corresponding enum values.

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
- [Query / QueryAsync](#query--queryasync) - Map result set to dynamic objects
- [QueryFirst / QueryFirstAsync](#queryfirst--queryfirstasync) - Map first row of result set to a dynamic object
- [QueryFirstOrDefault / QueryFirstOrDefaultAsync](#queryfirstordefault--queryfirstordefaultasync) - Map first row of result set to a dynamic object or null if no rows are found
- [QuerySingle / QuerySingleAsync](#querysingle--querysingleasync) - Map single row of result set to a dynamic object
- [QuerySingleOrDefault / QuerySingleOrDefaultAsync](#querysingleordefault--querysingleordefaultasync) - Map single row of result set to a dynamic object or null if no rows are found
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
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

DbConnectionExtensions.Configure(config =>
{
    // Configuration options go here
});
```

> [!NOTE]
> `DbConnectionExtensions.Configure` can only be called once.
> After it has been called the configuration of DbConnectionPlus is frozen and cannot be changed anymore.

#### EnumSerializationMode
Use `EnumSerializationMode` to configure how enum values are serialized when they are sent to a database.  
The default value is `EnumSerializationMode.Strings`, which serializes enum values as their string representation.

When `EnumSerializationMode` is set to `EnumSerializationMode.Strings`, enum values are serialized as strings.  
When `EnumSerializationMode` is set to `EnumSerializationMode.Integers`, enum values are serialized as integers.

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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

var user = new User
{
    Id = 1,
    UserName = "adminuser",
    Role = UserRole.User
};

DbConnectionExtensions.Configure(config =>
{
    config.EnumSerializationMode = EnumSerializationMode.Strings;
});

connection.InsertEntity(user); // Column "Role" will contain the string "User".

DbConnectionExtensions.Configure(config =>
{
    config.EnumSerializationMode = EnumSerializationMode.Integers;
});

connection.InsertEntity(user); // Column "Role" will contain the integer 2.
```

#### InterceptDbCommand
Use `InterceptDbCommand` to configure a delegate that intercepts a `DbCommand` before it is executed. This can be 
useful for logging, modifying the command text, or applying additional configuration.

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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
> When a fluent mapping exist for an entity type, the data annotations on this entity type are ignored.
> When a fluent mapping exists for an entity property, the data annotations on this property are ignored.

##### Fluent API
You can use the fluent API to configure how entity types are mapped to database tables and columns.

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

##### Data annotation attributes 

You can use the following attributes to configure how entity types are mapped to database tables and columns:

###### `System.ComponentModel.DataAnnotations.Schema.TableAttribute`
Use this attribute to specify the name of the table where entities of an entity type are stored in the database:
```csharp
[Table("Products")]
public class Product { ... }
```
If you don't specify the table name using this attribute, the singular name of the entity type 
(not including its namespace) is used as the table name.

###### `System.ComponentModel.DataAnnotations.Schema.ColumnAttribute`
Use this attribute to specify the name of the column where a property of an entity type is stored in the database:
```csharp
class Product
{
  [Column("ProductName")]
  public String Name { get; set; }
}
```
If you don't specify the column name using this attribute, the property name is used as the column name.

###### `System.ComponentModel.DataAnnotations.KeyAttribute`
Use this attribute to specify the property / properties of an entity type by which entities of that type are 
identified (usually the primary key / keys):
```csharp
class Product
{
  [Key]
  public Int64 Id { get; set; }
}
```
 
###### `System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute`
Use this attribute to specify that a property of an entity type is generated by the database:
```csharp
class Product
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public Int64 Id { get; set; }
}
```
Properties marked with this attribute are ignored (unless `DatabaseGeneratedOption.None` is used) when inserting new 
entities into the database or updating existing entities.
When an entity is inserted or updated, the value of the property is read back from the database and set on the entity.

###### `System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute`
Use this attribute to specify that a property of an entity type should be ignored and not mapped to a column:
```csharp
public class OrderItem
{
    [NotMapped]
    public Decimal TotalPrice => this.UnitPrice * this.Quantity;
}
```
Properties marked with this attribute are ignored by DbConnectionPlus.
They are never read from the database and never written to the database.

### General-purpose methods

#### ExecuteNonQuery / ExecuteNonQueryAsync
Executes an SQL statement and returns the number of rows affected by the statement.

```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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

#### Query / QueryAsync
Executes an SQL statement and maps the result set to a sequence of dynamic objects.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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
    var id = product.Id;
    var unitsInStock = product.UnitsInStock;
    ...
}
```

#### QueryFirst / QueryFirstAsync
Executes an SQL statement and maps the first row of the result set to a dynamic object.
Throws if no rows are found.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var product = connection.QueryFirst($"SELECT * FROM Product WHERE Id = {Parameter(id)}");

var id = product.Id;
var name = product.Name;
...
```

#### QueryFirstOrDefault / QueryFirstOrDefaultAsync
Executes an SQL statement and maps the first row of the result set to a dynamic object or null if no rows are found.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var product = connection.QueryFirstOrDefault($"SELECT * FROM Product WHERE Id = {Parameter(id)}");

if (product is not null)
{
    var id = product.Id;
    var name = product.Name;
    ...
}
```

#### QuerySingle / QuerySingleAsync
Executes an SQL statement and maps the single row of the result set to a dynamic object.
Throws if no rows or more than one row are found.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var product = connection.QuerySingle($"SELECT * FROM Product WHERE Id = {Parameter(id)}");

var id = product.Id;
var name = product.Name;
...
```

#### QuerySingleOrDefault / QuerySingleOrDefaultAsync
Executes an SQL statement and maps the single row of the result set to a dynamic object or null if no rows are found.
Throws if more than one row are found.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var product = connection.QuerySingleOrDefault($"SELECT * FROM Product WHERE Id = {Parameter(id)}");

if (product is not null)
{
    var id = product.Id;
    var name = product.Name;
    ...
}
```

#### Query\<T\> / QueryAsync\<T\>
Executes an SQL statement and maps the result set to a sequence of scalar values, entities or value tuples of the 
specified type.

##### Scalar values
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

class OrderItem
{
    public Int64 ProductId { get; set; }
    public DateTime OrderDate { get; set; }
}

var orderItems = GetOrderItems();
var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

var idsOfProductsOrderedInPastSixMonths = connection.Query<Int64>(
    $"""
     SELECT     Id
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

##### Entities
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var lowStockThreshold = configuration.Thresholds.LowStock;

var lowStockProducts = connection.Query<Product>(
   $"""
    SELECT  *
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```

##### Value tuples
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

class OrderItem
{
    public Int64 ProductId { get; set; }
    public DateTime OrderDate { get; set; }
}

var orderItems = GetOrderItems();
var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

var productsOrderedInPastSixMonthsInfos = connection.Query<(Int64 ProductId, Int32 UnitsInStock)>(
    $"""
     SELECT     Id, UnitsInStock
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

#### QueryFirst\<T\> / QueryFirstAsync\<T\>
Executes an SQL statement and maps the first row of the result set to a scalar value, entity or value tuple of the 
specified type.
Throws if no rows are found.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var product = connection.QueryFirst<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

#### QueryFirstOrDefault\<T\> / QueryFirstOrDefaultAsync\<T\>
Executes an SQL statement and maps the first row of the result set to a scalar value, entity or value tuple of the 
specified type or default value if no rows are found.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var product = connection.QueryFirstOrDefault<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

#### QuerySingle\<T\> / QuerySingleAsync\<T\>
Executes an SQL statement and maps the single row of the result set to a scalar value, entity or value tuple of the 
specified type.
Throws if no rows or more than one row are found.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var product = connection.QuerySingle<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

#### QuerySingleOrDefault\<T\> / QuerySingleOrDefaultAsync\<T\>
Executes an SQL statement and maps the single row of the result set to a scalar value, entity or value tuple of the 
specified type or default value if no rows are found.
Throws if more than one row are found.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var product = connection.QuerySingleOrDefault<Product>($"SELECT * FROM Product WHERE Id = {Parameter(id)}");
```

### Entity manipulation methods

#### InsertEntities / InsertEntitiesAsync
Inserts a sequence of new entities into a database table.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

class Product
{
    [Key]
    public Int64 Id { get; set; }
    public Int64 SupplierId { get; set; }
    public String Name { get; set; }
    public Decimal UnitPrice { get; set; }
    public Int32 UnitsInStock { get; set; }
}

var newProducts = GetNewProducts();

connection.InsertEntities(newProducts);
```

#### InsertEntity / InsertEntityAsync
Inserts a new entity into a database table.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

class Product
{
    [Key]
    public Int64 Id { get; set; }
    public Int64 SupplierId { get; set; }
    public String Name { get; set; }
    public Decimal UnitPrice { get; set; }
    public Int32 UnitsInStock { get; set; }
}

var newProduct = GetNewProduct();

connection.InsertEntity(newProduct);
```

#### UpdateEntities / UpdateEntitiesAsync
Updates existing entities in a database table based on their keys.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

enum UserState
{
	Active,
	Inactive,
	Suspended
}
			
class User
{
    [Key]
    public Int64 Id { get; set; }
    public DateTime LastLoginDate { get; set; }
    public UserState State { get; set; }
}

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
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

enum UserState
{
	Active,
	Inactive,
	Suspended
}
			
class User
{
    [Key]
    public Int64 Id { get; set; }
    public DateTime LastLoginDate { get; set; }
    public UserState State { get; set; }
}

if (user.LastLoginDate < DateTime.UtcNow.AddYears(-1))
{
    user.State = UserState.Inactive;
    connection.UpdateEntity(user);
}
```

#### DeleteEntities / DeleteEntitiesAsync
Deletes a sequence of entities from a database table based on their keys.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

class Product
{
    [Key]
    public Int64 Id { get; set; }
    public Boolean IsDiscontinued { get; set; }
}

connection.DeleteEntities(products.Where(a => a.IsDiscontinued));
```

#### DeleteEntity / DeleteEntityAsync
Deletes an entity from a database table based on its key.
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

class Product
{
    [Key]
    public Int64 Id { get; set; }
    public Boolean IsDiscontinued { get; set; }
}

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

To use this method, first import `RentADeveloper.DbConnectionPlus.DbConnectionExtensions` with a using directive
with the static modifier:
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
```

Example:
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

var lowStockThreshold = configuration.Thresholds.LowStock;

using var lowStockProductsReader = connection.ExecuteReader(
   $"""
    SELECT  *
    FROM    Product
    WHERE   UnitsInStock < {Parameter(lowStockThreshold)}
    """
);
```
This will add a parameter with the name `LowStockThreshold` and the value of the variable `lowStockThreshold` to the
 SQL statement.

The name of the parameter will be inferred from the expression passed to `Parameter(value)`.
If the name cannot be inferred from the expression a generic name like `Parameter_1`, `Parameter_2`, and so on will
be used.

The expression `{Parameter(value)}` will be replaced with the name of the parameter (e.g. `LowStockThreshold`) in 
the SQL statement.

If you pass an enum value as a parameter, the enum value is serialized either as a string or as an integer according
to the setting `RentADeveloper.DbConnectionPlus.DbConnectionExtensions.EnumSerializationMode`.

#### TemporaryTable(values)
Use `TemporaryTable(values)` to pass a sequence of scalar values or complex objects in an interpolated string as a
temporary table to an SQL statement.

To use this method, first import `RentADeveloper.DbConnectionPlus.DbConnectionExtensions` with a using directive 
with the static modifier:
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;
```

You can pass a sequence of scalar values (e.g. `String`, `Int32`, `DateTime`, enums and so on) or a sequence of
complex objects.

If a sequence of scalar values is passed, the temporary table will have a single column named `Value` with a data
type that matches the type of the passed values.

Example:
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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
This will create a temporary table with a single column named `Value` and with a data type that matches the type of
the passed values:
```sql
CREATE TABLE #RetiredSupplierIds_48d42afd5d824a27bd9352676ab6c198
(
    Value BIGINT
)
```

If a sequence of complex objects is passed, the temporary table will have multiple columns.
The temporary table will contain a column for each public property of the passed objects.
The name of each column will be the name of the corresponding property.
The data type of each column will be the property type of the corresponding property.

Example:
```csharp
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

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
This will create a temporary table with columns matching the properties of the passed objects:
```sql
CREATE TABLE #OrderItems_d6545835d97148ab93709efe9ba1f110
(
    ProductId BIGINT,
    OrderDate DATETIME2
)
```

The name of the temporary table will be inferred from the expression passed to `TemporaryTable(values)` and suffixed
with a new Guid to avoid naming conflicts (e.g. `OrderItems_395c98f203514e81aa0098ec7f13e8a2`).
If the name cannot be inferred from the expression the name `Values` (also suffixed with a new Guid) will be used
(e.g. `Values_395c98f203514e81aa0098ec7f13e8a2`).

The expression `{TemporaryTable(values)}` will be replaced with the name of the temporary table 
(e.g. `OrderItems_395c98f203514e81aa0098ec7f13e8a2`) in the SQL statement.

If you pass enum values as a temporary table, the enum values are serialized either as strings or as integers
according to the setting `RentADeveloper.DbConnectionPlus.DbConnectionExtensions.EnumSerializationMode`.

If you pass objects containing enum properties as a temporary table, the enum values are serialized either as strings
or as integers according to the setting
`RentADeveloper.DbConnectionPlus.DbConnectionExtensions.EnumSerializationMode`.

When `DbConnectionExtensions.EnumSerializationMode` is set to `EnumSerializationMode.Strings`, the data type of the
corresponding column in the temporary table will be `NVARCHAR(200)`.

When `DbConnectionExtensions.EnumSerializationMode` is set to `EnumSerializationMode.Integers`, the data type of the
corresponding column in the temporary table will be `INT`.

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

See [SqlServerDatabaseAdapter](https://github.com/rent-a-developer/DbConnectionPlus/blob/main/src/DbConnectionPlus/DatabaseAdapters/SqlServer/SqlServerDatabaseAdapter.cs) 
for an example implementation of a database adapter.

## Benchmarks
DbConnectionPlus is designed to have a minimal performance and allocation overhead compared to using 
`DbCommand` manually.  

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7623/24H2/2024Update/HudsonValley)
12th Gen Intel Core i9-12900K 3.19GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 8.0.23 (8.0.23, 8.0.2325.60607), X64 RyuJIT x86-64-v3
  Job-ADQEJE : .NET 8.0.23 (8.0.23, 8.0.2325.60607), X64 RyuJIT x86-64-v3

MinIterationTime=100ms  OutlierMode=DontRemove  Server=True  
InvocationCount=1  MaxIterationCount=20  UnrollFactor=1  
WarmupCount=10  
```

| Method                                         | Mean         | Error        | StdDev       | Median       | P90          | P95          | Ratio        | RatioSD | Allocated | Alloc Ratio |
|----------------------------------------------- |-------------:|-------------:|-------------:|-------------:|-------------:|-------------:|-------------:|--------:|----------:|------------:|
| **DeleteEntities_Manually**                        | **14,672.73 μs** | **3,387.316 μs** | **3,900.839 μs** | **14,243.07 μs** | **19,825.26 μs** | **20,144.38 μs** |     **baseline** |        **** | **101.62 KB** |            **** |
| DeleteEntities_DbConnectionPlus                |  6,717.47 μs |   721.336 μs |   830.692 μs |  6,372.96 μs |  7,698.83 μs |  8,539.65 μs | 2.21x faster |   0.62x |  17.17 KB |  5.92x less |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **DeleteEntity_Manually**                          |    **188.68 μs** |    **24.244 μs** |    **27.920 μs** |    **198.98 μs** |    **212.97 μs** |    **217.73 μs** |     **baseline** |        **** |    **2.1 KB** |            **** |
| DeleteEntity_DbConnectionPlus                  |    191.09 μs |    27.642 μs |    31.833 μs |    197.78 μs |    230.76 μs |    235.60 μs | 1.04x slower |   0.24x |    2.1 KB |  1.00x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **ExecuteNonQuery_Manually**                       |    **158.13 μs** |    **24.189 μs** |    **27.856 μs** |    **157.52 μs** |    **169.30 μs** |    **178.27 μs** |     **baseline** |        **** |    **2.1 KB** |            **** |
| ExecuteNonQuery_DbConnectionPlus               |    165.12 μs |    13.165 μs |    15.161 μs |    166.52 μs |    177.50 μs |    180.82 μs | 1.07x slower |   0.19x |   2.81 KB |  1.33x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **ExecuteReader_Manually**                         |    **183.91 μs** |     **9.815 μs** |    **11.303 μs** |    **179.93 μs** |    **203.46 μs** |    **211.49 μs** |     **baseline** |        **** |  **50.54 KB** |            **** |
| ExecuteReader_DbConnectionPlus                 |    173.84 μs |     4.810 μs |     5.539 μs |    173.00 μs |    180.74 μs |    186.21 μs | 1.06x faster |   0.07x |  50.83 KB |  1.01x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **ExecuteScalar_Manually**                         |     **73.79 μs** |     **2.411 μs** |     **2.777 μs** |     **73.54 μs** |     **78.35 μs** |     **78.58 μs** |     **baseline** |        **** |   **3.04 KB** |            **** |
| ExecuteScalar_DbConnectionPlus                 |     77.81 μs |     5.661 μs |     6.519 μs |     76.63 μs |     81.00 μs |     87.09 μs | 1.06x slower |   0.09x |   3.77 KB |  1.24x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **Exists_Manually**                                |     **56.36 μs** |    **13.725 μs** |    **15.806 μs** |     **48.61 μs** |     **78.16 μs** |     **86.30 μs** |     **baseline** |        **** |   **2.63 KB** |            **** |
| Exists_DbConnectionPlus                        |     51.36 μs |     2.946 μs |     3.392 μs |     50.43 μs |     53.15 μs |     55.69 μs | 1.10x faster |   0.31x |   3.34 KB |  1.27x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **InsertEntities_Manually**                        | **17,619.46 μs** | **2,472.686 μs** | **2,847.548 μs** | **18,691.91 μs** | **20,290.38 μs** | **20,702.41 μs** |     **baseline** |        **** | **517.03 KB** |            **** |
| InsertEntities_DbConnectionPlus                | 21,575.08 μs | 2,280.957 μs | 2,626.754 μs | 23,062.28 μs | 23,656.92 μs | 24,692.07 μs | 1.25x slower |   0.24x | 437.87 KB |  1.18x less |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **InsertEntity_Manually**                          |    **256.13 μs** |    **16.084 μs** |    **18.522 μs** |    **257.27 μs** |    **264.82 μs** |    **285.02 μs** |     **baseline** |        **** |   **8.57 KB** |            **** |
| InsertEntity_DbConnectionPlus                  |    280.06 μs |    37.113 μs |    42.740 μs |    259.51 μs |    341.86 μs |    355.55 μs | 1.10x slower |   0.18x |   8.72 KB |  1.02x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **Parameter_Manually**                             |     **57.55 μs** |    **10.088 μs** |    **11.618 μs** |     **56.99 μs** |     **65.92 μs** |     **67.72 μs** |     **baseline** |        **** |   **5.43 KB** |            **** |
| Parameter_DbConnectionPlus                     |     52.35 μs |     5.561 μs |     6.404 μs |     50.31 μs |     55.65 μs |     57.76 μs | 1.11x faster |   0.24x |   7.34 KB |  1.35x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **Query_Dynamic_Manually**                         |    **315.14 μs** |    **12.468 μs** |    **14.358 μs** |    **312.52 μs** |    **327.40 μs** |    **333.20 μs** |     **baseline** |        **** | **195.41 KB** |            **** |
| Query_Dynamic_DbConnectionPlus                 |    203.51 μs |    16.883 μs |    19.442 μs |    197.45 μs |    215.26 μs |    224.23 μs | 1.56x faster |   0.13x | 136.38 KB |  1.43x less |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **Query_Scalars_Manually**                         |     **74.03 μs** |     **2.179 μs** |     **2.510 μs** |     **73.53 μs** |     **77.74 μs** |     **77.97 μs** |     **baseline** |        **** |   **2.11 KB** |            **** |
| Query_Scalars_DbConnectionPlus                 |     90.07 μs |    11.385 μs |    13.111 μs |     89.36 μs |    102.01 μs |    104.18 μs | 1.22x slower |   0.18x |   7.26 KB |  3.44x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **Query_Entities_Manually**                        |    **251.81 μs** |     **6.020 μs** |     **6.933 μs** |    **250.85 μs** |    **260.06 μs** |    **262.91 μs** |     **baseline** |        **** |   **51.3 KB** |            **** |
| Query_Entities_DbConnectionPlus                |    263.71 μs |     6.792 μs |     7.822 μs |    260.52 μs |    271.74 μs |    274.68 μs | 1.05x slower |   0.04x |  54.37 KB |  1.06x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **Query_ValueTuples_Manually**                     |    **180.00 μs** |     **8.115 μs** |     **9.345 μs** |    **177.02 μs** |    **185.67 μs** |    **194.46 μs** |     **baseline** |        **** |  **18.07 KB** |            **** |
| Query_ValueTuples_DbConnectionPlus             |    190.84 μs |     9.986 μs |    11.499 μs |    188.72 μs |    200.44 μs |    217.74 μs | 1.06x slower |   0.08x |  29.45 KB |  1.63x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **TemporaryTable_ComplexObjects_Manually**         |  **8,267.76 μs** | **2,480.979 μs** | **2,857.099 μs** |  **7,983.17 μs** | **11,502.49 μs** | **11,944.48 μs** |     **baseline** |        **** | **132.52 KB** |            **** |
| TemporaryTable_ComplexObjects_DbConnectionPlus |  6,636.36 μs |   614.018 μs |   707.104 μs |  6,582.66 μs |  7,309.96 μs |  7,595.85 μs | 1.26x faster |   0.44x | 137.92 KB |  1.04x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **TemporaryTable_ScalarValues_Manually**           |  **4,784.75 μs** |   **566.815 μs** |   **652.745 μs** |  **4,620.07 μs** |  **4,950.02 μs** |  **5,609.07 μs** |     **baseline** |        **** | **177.18 KB** |            **** |
| TemporaryTable_ScalarValues_DbConnectionPlus   |  4,897.28 μs |   393.307 μs |   452.933 μs |  4,735.95 μs |  5,696.50 μs |  5,701.09 μs | 1.04x slower |   0.14x | 304.21 KB |  1.72x more |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **UpdateEntities_Manually**                        | **23,744.24 μs** | **3,367.021 μs** | **3,877.466 μs** | **22,203.37 μs** | **30,059.37 μs** | **32,188.80 μs** |     **baseline** |        **** | **530.26 KB** |            **** |
| UpdateEntities_DbConnectionPlus                | 34,624.61 μs | 3,734.617 μs | 4,300.790 μs | 34,084.29 μs | 35,478.88 μs | 39,301.47 μs | 1.49x slower |   0.28x | 450.27 KB |  1.18x less |
|                                                |              |              |              |              |              |              |              |         |           |             |
| **UpdateEntity_Manually**                          |    **300.87 μs** |    **28.337 μs** |    **32.633 μs** |    **291.67 μs** |    **350.76 μs** |    **366.50 μs** |     **baseline** |        **** |    **9.5 KB** |            **** |
| UpdateEntity_DbConnectionPlus                  |    344.98 μs |    49.278 μs |    56.749 μs |    356.24 μs |    393.93 μs |    408.69 μs | 1.16x slower |   0.22x |   9.67 KB |  1.02x more |

Please keep in mind that benchmarking is tricky when SQL Server is involved.
So take these benchmark results with a grain of salt.

### Running the benchmarks
To run the benchmarks, ensure you have an SQL Server instance available.  
The benchmarks will create a database named `DbConnectionPlusTests`, so make sure your SQL user has the necessary 
rights.

Set the environment variable `ConnectionString_SqlServer` to the connection string to the SQL Server instance:
```shell
set ConnectionString_SqlServer="Data Source=.\SqlServer;Integrated Security=True;Encrypt=False;MultipleActiveResultSets=True"
```

Then run the following command:
```shell
dotnet run --configuration Release --project benchmarks\DbConnectionPlus.Benchmarks\DbConnectionPlus.Benchmarks.csproj
```

## Running the unit tests
Run the unit tests using the Test Explorer in Visual Studio or via the following command:
```shell
dotnet test tests\DbConnectionPlus.UnitTests\DbConnectionPlus.UnitTests.csproj --logger "console;verbosity=detailed"
```

## Running the integration tests
To run the integration tests, you need [Docker](https://www.docker.com/) installed and running on your machine to run
the containers for the test databases.

Open a terminal and run the following command to start the required database containers:
```shell
docker-compose -f tests\DbConnectionPlus.IntegrationTests\Docker\docker-compose.yml up -d
```

Check and if necessary change the connection strings in the file 
`tests\DbConnectionPlus.IntegrationTests\Local.runsettings`.  

Make sure the runsettings file is selected in Visual Studio:  
- In the Visual Studio menu, go to `Test` -> `Configure Run Settings` and click on 
`Select Solution Wide runsettings File`.  
- In the file dialog, select the file `tests\DbConnectionPlus.IntegrationTests\Local.runsettings`.  

Then run the tests using the Test Explorer in Visual Studio or via the following command:
```shell
dotnet test tests\DbConnectionPlus.IntegrationTests\DbConnectionPlus.IntegrationTests.csproj --settings tests\DbConnectionPlus.IntegrationTests\Local.runsettings --logger "console;verbosity=detailed"
```
  
## Contributing
Contributions and bug reports are welcome and appreciated.  
Please follow the repository's [CONTRIBUTING.md](CONTRIBUTING.md) and code style.  
Open a GitHub issue for problems or a pull request with tests and a clear description of changes.

## License
This library is licensed under the [MIT license](LICENSE.md).

## Documentation
Full API documentation is available
[here](https://rent-a-developer.github.io/DbConnectionPlus/api/RentADeveloper.DbConnectionPlus.html).

## Change Log
The change log is available [here](CHANGELOG.md).

## Contributors

- David Liebeherr ([info@rent-a-developer.de](mailto:info@rent-a-developer.de))

