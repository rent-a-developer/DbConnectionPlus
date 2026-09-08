# Configuration

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

## EnumSerializationMode
Use `EnumSerializationMode` to configure how enum values are serialized when they are sent to a database.
`EnumSerializationMode.Strings` (the default) serializes them as their string representation,
`EnumSerializationMode.Integers` as integers. It applies to entity properties, parameters and temporary table
columns alike - see [Enum support](entity-mapping-and-crud.md#enum-support).

```csharp
DbConnectionExtensions.Configure(config =>
{
    config.EnumSerializationMode = EnumSerializationMode.Integers;
});
```

## InterceptDbCommand
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

See [DbCommandLogger](https://github.com/rent-a-developer/DbConnectionPlus/tree/main/tests/DbConnectionPlus.IntegrationTests) 
for an example of logging executed commands.
