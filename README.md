[![NuGet Version](https://img.shields.io/nuget/v/RentADeveloper.DbConnectionPlus)](https://www.nuget.org/packages/RentADeveloper.DbConnectionPlus/)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=rent-a-developer_DbConnectionPlus&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=rent-a-developer_DbConnectionPlus)
[![license](https://img.shields.io/badge/License-MIT-purple.svg)](LICENSE.md)
![semver](https://img.shields.io/badge/semver-1.2.0-blue)

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

    config.Entity<Product>()
        .Property(a => a.Version)
        .IsRowVersion();

    config.Entity<User>()
        .Property(a => a.ConcurrencyToken)
        .IsConcurrencyToken();
});
```

###### `Entity<TEntity>()`
Use this method to start configuring the mapping for the entity type `TEntity`.

###### `ToTable(tableName)`
Use this method to specify the name of the table where entities of the entity type are stored in the database.

###### `Property(propertyExpression)`
Use this method to start configuring the mapping for a property of the entity type.

###### `HasColumnName(columnName)`
Use this method to specify the name of the column where the property is stored in the database.

###### `IsKey()`
Use this method to specify that the property is part of the key by which entities of the entity type are identified.

###### `IsIdentity()`
Use this method to specify that the property is generated by the database on insert.

###### `IsComputed()`
Use this method to specify that the property is generated by the database on insert and update.

###### `IsRowVersion()`
Use this method to specify that the property is a native database-generated concurrency token.

###### `IsConcurrencyToken()`
Use this method to specify that the property is an application-managed concurrency token.

###### `IsIgnored()`
Use this method to specify that the property should be ignored and not mapped to a column.

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
 
###### `System.ComponentModel.DataAnnotations.TimestampAttribute`
Use this attribute to specify that a property of an entity type is a native database-generated concurrency token:
```csharp
class Product
{
  [Timestamp]
  public Byte[] Version { get; set; }
}
```
Properties marked with this attribute will be checked during delete and update operations.
When their values in the database do not match the original values, the delete or update will fail.
When an entity is inserted or updated, the value of the property is read back from the database and set on the entity.
 
###### `System.ComponentModel.DataAnnotations.ConcurrencyCheckAttribute`
Use this attribute to specify that a property of an entity type is a application-managed concurrency token:
```csharp
class Product
{
  [ConcurrencyCheck]
  public Byte[] ConcurrencyToken { get; set; }
}
```
Properties marked with this attribute will be checked during delete and update operations.
When their values in the database do not match the original values, the delete or update will fail.

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
DbConnectionPlus is designed to have a minimal performance and allocation overhead compared to using `DbCommand` 
manually.  
  
All benchmarks are performed using SQLite in-memory databases, which is a worst-case scenario for DbConnectionPlus 
because the overhead of using DbConnectionPlus is more noticeable when the executed SQL statements are very fast.

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
| Method                                         | Mean         | Error         | StdDev        | Median       | P90          | P95          | Ratio        | RatioSD | Allocated  | Alloc Ratio |
|----------------------------------------------- |-------------:|--------------:|--------------:|-------------:|-------------:|-------------:|-------------:|--------:|-----------:|------------:|
| **DeleteEntities_Manually**                        | **15,812.58 μs** |  **3,269.782 μs** |  **3,765.486 μs** | **15,096.66 μs** | **20,554.52 μs** | **23,427.08 μs** |     **baseline** |        **** |   **19.32 KB** |            **** |
| DeleteEntities_DbConnectionPlus                | 26,344.72 μs |  7,115.300 μs |  8,193.990 μs | 24,756.73 μs | 30,942.44 μs | 44,994.48 μs | 1.75x slower |   0.65x |   19.69 KB |  1.02x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **DeleteEntity_Manually**                          |    **187.61 μs** |     **37.759 μs** |     **43.483 μs** |    **171.45 μs** |    **271.18 μs** |    **283.79 μs** |     **baseline** |        **** |    **1.29 KB** |            **** |
| DeleteEntity_DbConnectionPlus                  |    231.79 μs |     77.386 μs |     89.118 μs |    207.50 μs |    360.04 μs |    411.14 μs | 1.29x slower |   0.54x |    1.62 KB |  1.25x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **ExecuteNonQuery_Manually**                       |    **227.38 μs** |     **44.374 μs** |     **51.101 μs** |    **216.12 μs** |    **283.89 μs** |    **298.40 μs** |     **baseline** |        **** |    **1.29 KB** |            **** |
| ExecuteNonQuery_DbConnectionPlus               |    215.31 μs |     52.704 μs |     60.694 μs |    188.86 μs |    291.85 μs |    297.67 μs | 1.13x faster |   0.39x |       2 KB |  1.55x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **ExecuteReader_Manually**                         |    **249.64 μs** |     **45.099 μs** |     **51.936 μs** |    **239.96 μs** |    **319.38 μs** |    **352.49 μs** |     **baseline** |        **** |   **60.46 KB** |            **** |
| ExecuteReader_DbConnectionPlus                 |    299.73 μs |     80.094 μs |     92.236 μs |    293.57 μs |    408.75 μs |    440.39 μs | 1.24x slower |   0.44x |   61.46 KB |  1.02x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **ExecuteScalar_Manually**                         |     **77.82 μs** |     **17.007 μs** |     **19.586 μs** |     **71.95 μs** |    **101.45 μs** |    **124.05 μs** |     **baseline** |        **** |    **2.12 KB** |            **** |
| ExecuteScalar_DbConnectionPlus                 |     97.83 μs |      7.468 μs |      8.600 μs |     97.02 μs |    108.74 μs |    111.83 μs | 1.32x slower |   0.28x |    2.85 KB |  1.35x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **Exists_Manually**                                |     **61.17 μs** |      **7.029 μs** |      **8.095 μs** |     **59.87 μs** |     **70.26 μs** |     **72.24 μs** |     **baseline** |        **** |    **1.96 KB** |            **** |
| Exists_DbConnectionPlus                        |     80.57 μs |     17.980 μs |     20.706 μs |     74.52 μs |     93.71 μs |     97.91 μs | 1.34x slower |   0.38x |    2.67 KB |  1.36x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **InsertEntities_Manually**                        | **37,252.98 μs** | **16,064.582 μs** | **18,499.997 μs** | **31,081.23 μs** | **45,555.75 μs** | **88,905.67 μs** |     **baseline** |        **** |  **5726.4 KB** |            **** |
| InsertEntities_DbConnectionPlus                | 28,925.35 μs |  5,155.758 μs |  5,937.379 μs | 28,238.99 μs | 34,674.36 μs | 40,572.46 μs | 1.34x faster |   0.70x | 5760.81 KB |  1.01x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **InsertEntity_Manually**                          |    **509.25 μs** |    **137.899 μs** |    **158.805 μs** |    **480.71 μs** |    **691.81 μs** |    **716.65 μs** |     **baseline** |        **** |   **61.42 KB** |            **** |
| InsertEntity_DbConnectionPlus                  |    407.38 μs |     47.504 μs |     54.705 μs |    388.55 μs |    468.85 μs |    528.37 μs | 1.27x faster |   0.42x |   62.49 KB |  1.02x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **Parameter_Manually**                             |    **104.04 μs** |     **38.319 μs** |     **44.128 μs** |     **79.73 μs** |    **177.14 μs** |    **190.65 μs** |     **baseline** |        **** |    **4.38 KB** |            **** |
| Parameter_DbConnectionPlus                     |    110.40 μs |     43.333 μs |     49.902 μs |    106.35 μs |    155.04 μs |    163.53 μs | 1.21x slower |   0.66x |    6.31 KB |  1.44x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **Query_Dynamic_Manually**                         |    **683.58 μs** |    **172.616 μs** |    **198.785 μs** |    **656.92 μs** |    **817.59 μs** |    **846.18 μs** |     **baseline** |        **** |  **215.27 KB** |            **** |
| Query_Dynamic_DbConnectionPlus                 |    278.25 μs |     79.237 μs |     91.250 μs |    240.19 μs |    359.03 μs |    530.59 μs | 2.62x faster |   0.92x |  162.94 KB |  1.32x less |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **Query_Scalars_Manually**                         |    **117.42 μs** |     **12.106 μs** |     **13.941 μs** |    **119.76 μs** |    **128.44 μs** |    **135.42 μs** |     **baseline** |        **** |    **1.07 KB** |            **** |
| Query_Scalars_DbConnectionPlus                 |    116.26 μs |     23.145 μs |     26.653 μs |    118.95 μs |    146.82 μs |    155.42 μs | 1.00x slower |   0.26x |    6.24 KB |  5.85x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **Query_Entities_Manually**                        |    **391.99 μs** |     **32.586 μs** |     **37.526 μs** |    **383.52 μs** |    **442.81 μs** |    **459.92 μs** |     **baseline** |        **** |   **61.92 KB** |            **** |
| Query_Entities_DbConnectionPlus                |    497.88 μs |    169.969 μs |    195.737 μs |    495.08 μs |    768.03 μs |    773.60 μs | 1.28x slower |   0.51x |   72.26 KB |  1.17x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **Query_ValueTuples_Manually**                     |    **182.67 μs** |     **40.380 μs** |     **46.502 μs** |    **169.54 μs** |    **247.10 μs** |    **263.69 μs** |     **baseline** |        **** |   **16.73 KB** |            **** |
| Query_ValueTuples_DbConnectionPlus             |    183.50 μs |     34.047 μs |     39.209 μs |    182.46 μs |    228.38 μs |    246.76 μs | 1.06x slower |   0.33x |   28.67 KB |  1.71x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **TemporaryTable_ComplexObjects_Manually**         |  **9,132.97 μs** |  **1,686.089 μs** |  **1,941.703 μs** |  **8,385.77 μs** | **11,329.97 μs** | **13,415.61 μs** |     **baseline** |        **** |     **360 KB** |            **** |
| TemporaryTable_ComplexObjects_DbConnectionPlus |  9,045.06 μs |  1,076.753 μs |  1,239.991 μs |  8,996.34 μs | 10,270.86 μs | 10,485.35 μs | 1.03x slower |   0.23x |  373.24 KB |  1.04x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **TemporaryTable_ScalarValues_Manually**           |  **7,537.41 μs** |    **936.994 μs** |  **1,079.044 μs** |  **7,167.69 μs** |  **8,462.54 μs** |  **9,957.15 μs** |     **baseline** |        **** |  **176.13 KB** |            **** |
| TemporaryTable_ScalarValues_DbConnectionPlus   |  5,852.64 μs |    836.596 μs |    963.425 μs |  5,843.33 μs |  7,027.70 μs |  7,382.98 μs | 1.32x faster |   0.29x |  303.31 KB |  1.72x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **UpdateEntities_Manually**                        | **40,746.85 μs** | **12,388.517 μs** | **14,266.635 μs** | **34,575.09 μs** | **59,380.97 μs** | **61,547.65 μs** |     **baseline** |        **** | **5708.12 KB** |            **** |
| UpdateEntities_DbConnectionPlus                | 33,742.35 μs | 10,522.508 μs | 12,117.736 μs | 29,627.05 μs | 38,988.11 μs | 43,043.74 μs | 1.29x faster |   0.51x | 5743.07 KB |  1.01x more |
|                                                |              |               |               |              |              |              |              |         |            |             |
| **UpdateEntity_Manually**                          |    **368.24 μs** |     **53.938 μs** |     **62.115 μs** |    **346.09 μs** |    **470.61 μs** |    **487.82 μs** |     **baseline** |        **** |   **61.61 KB** |            **** |
| UpdateEntity_DbConnectionPlus                  |    397.23 μs |     36.233 μs |     41.726 μs |    394.08 μs |    435.77 μs |    452.75 μs | 1.10x slower |   0.20x |   62.65 KB |  1.02x more |

### Running the benchmarks
To run the benchmarks, run the following command:
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

