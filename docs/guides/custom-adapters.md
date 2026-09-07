# Custom database adapters

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

    public void BindParameterValue(DbParameter parameter, object? value)
    {
        ...
    }

    public string FormatParameterName(string parameterName)
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
