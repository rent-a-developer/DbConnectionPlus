// ReSharper disable UnusedParameter.Local

using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.SqlStatements;
using DbCommandBuilder = RentADeveloper.DbConnectionPlus.DbCommands.DbCommandBuilder;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DbCommands;

public class DbCommandBuilderTests : UnitTestsBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_CancellationToken_ShouldUseCancellationToken(Boolean useAsyncApi)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await cancellationTokenSource.CancelAsync();

        var (command, _) = await CallApi(
            useAsyncApi,
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            cancellationToken: cancellationToken
        );

        command.Received().Cancel();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_Code_Parameters_ShouldStoreCodeAndParameters(Boolean useAsyncApi)
    {
        var statement = new InterpolatedSqlStatement(
            "Code",
            ("Parameter1", "Value1"),
            ("Parameter2", "Value2"),
            ("Parameter3", "Value3")
        );

        var (command, _) = await CallApi(
            useAsyncApi,
            statement,
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be("Code");

        command.Parameters.Count
            .Should().Be(3);

        command.Parameters[0].ParameterName
            .Should().Be("Parameter1");

        command.Parameters[0].Value
            .Should().Be("Value1");

        command.Parameters[1].ParameterName
            .Should().Be("Parameter2");

        command.Parameters[1].Value
            .Should().Be("Value2");

        command.Parameters[2].ParameterName
            .Should().Be("Parameter3");

        command.Parameters[2].Value
            .Should().Be("Value3");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_CommandTimeout_ShouldUseCommandTimeout(Boolean useAsyncApi)
    {
        var timeout = Generate.Single<TimeSpan>();

        var (command, _) = await CallApi(
            useAsyncApi,
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            commandTimeout: timeout
        );

        command.CommandTimeout
            .Should().Be((Int32)timeout.TotalSeconds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_CommandType_ShouldUseCommandType(Boolean useAsyncApi)
    {
        var (command, _) = await CallApi(
            useAsyncApi,
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            commandType: CommandType.StoredProcedure
        );

        command.CommandType
            .Should().Be(CommandType.StoredProcedure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildDbCommand_InterpolatedParameter_EnumValue_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger(
            Boolean useAsyncApi
        )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var enumValue = Generate.Single<TestEnum>();

        var (command, _) = await CallApi(
            useAsyncApi,
            $"SELECT {Parameter(enumValue)}",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.Parameters.Count
            .Should().Be(1);

        command.Parameters[0].ParameterName
            .Should().Be("EnumValue");

        command.Parameters[0].Value
            .Should().Be((Int32)enumValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildDbCommand_InterpolatedParameter_EnumValue_EnumSerializationModeIsStrings_ShouldSerializeEnumToString(
            Boolean useAsyncApi
        )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var enumValue = Generate.Single<TestEnum>();

        var (command, _) = await CallApi(
            useAsyncApi,
            $"SELECT {Parameter(enumValue)}",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.Parameters.Count
            .Should().Be(1);

        command.Parameters[0].ParameterName
            .Should().Be("EnumValue");

        command.Parameters[0].Value
            .Should().Be(enumValue.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_InterpolatedParameter_ShouldHandleNullAndNonNullValues(Boolean useAsyncApi)
    {
        Int64? id1 = Generate.Id();
        Int64? id2 = null;
        Object value1 = Generate.Single<String>();
        Object? value2 = null;

        var (command, _) = await CallApi(
            useAsyncApi,
            $"SELECT {Parameter(id1)}, {Parameter(id2)}, {Parameter(value1)}, {Parameter(value2)}",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.Parameters.Count
            .Should().Be(4);

        command.CommandText
            .Should().Be("SELECT @Id1, @Id2, @Value1, @Value2");

        command.Parameters[0].ParameterName
            .Should().Be("Id1");

        command.Parameters[0].Value
            .Should().Be(id1);

        command.Parameters[1].ParameterName
            .Should().Be("Id2");

        command.Parameters[1].Value
            .Should().Be(DBNull.Value);

        command.Parameters[2].ParameterName
            .Should().Be("Value1");

        command.Parameters[2].Value
            .Should().Be(value1);

        command.Parameters[3].ParameterName
            .Should().Be("Value2");

        command.Parameters[3].Value
            .Should().Be(DBNull.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_InterpolatedParameter_ShouldInferNameFromValueExpressionIfPossible(
        Boolean useAsyncApi
    )
    {
        var productId = Generate.Id();
        static Int64 GetProductId() => Generate.Id();
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable IDE0060 // Remove unused parameter
        static Int64 GetProductIdByCategory(String category) => Generate.Id();
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter
        var productIds = Generate.Ids().ToArray();

        var (command, _) = await CallApi(
            useAsyncApi,
            $"""
             SELECT  {Parameter(productId)},
                     {Parameter(GetProductId())},
                     {Parameter(GetProductIdByCategory("Shoes"))},
                     {Parameter(productIds[1])},
                     {Parameter(this.testProductId)},
                     {Parameter(new { })}
             """,
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be(
                """
                SELECT  @ProductId,
                        @ProductId2,
                        @ProductIdByCategoryShoes,
                        @ProductIds1,
                        @TestProductId,
                        @Parameter_6
                """
            );

        command.Parameters.Count
            .Should().Be(6);

        command.Parameters[0].ParameterName
            .Should().Be("ProductId");

        command.Parameters[1].ParameterName
            .Should().Be("ProductId2");

        command.Parameters[2].ParameterName
            .Should().Be("ProductIdByCategoryShoes");

        command.Parameters[3].ParameterName
            .Should().Be("ProductIds1");

        command.Parameters[4].ParameterName
            .Should().Be("TestProductId");

        command.Parameters[5].ParameterName
            .Should().Be("Parameter_6");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_InterpolatedParameter_ShouldStoreParameter(Boolean useAsyncApi)
    {
        var value = Generate.ScalarValue();

        var (command, _) = await CallApi(
            useAsyncApi,
            $"SELECT {Parameter(value)}",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be("SELECT @Value");

        command.Parameters.Count
            .Should().Be(1);

        command.Parameters[0].ParameterName
            .Should().Be("Value");

        command.Parameters[0].Value
            .Should().Be(value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_InterpolatedParameter_ShouldSupportComplexExpressions(Boolean useAsyncApi)
    {
        const Double baseDiscount = 0.1;
        var entityIds = Generate.Ids(20);

        var (command, _) = await CallApi(
            useAsyncApi,
            $"""
             SELECT  {Parameter(baseDiscount * 5 / 3)},
                     {Parameter(entityIds.Where(a => a > 5).Select(a => a.ToString()).ToArray()[0])}
             """,
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be(
                """
                SELECT  @BaseDiscount53,
                        @EntityIdsWhereaa5SelectaaToStringToArray0
                """
            );

        command.Parameters.Count
            .Should().Be(2);

        command.Parameters[0].ParameterName
            .Should().Be("BaseDiscount53");

        command.Parameters[0].Value
            .Should().Be(baseDiscount * 5 / 3);

        command.Parameters[1].ParameterName
            .Should().Be("EntityIdsWhereaa5SelectaaToStringToArray0");

        command.Parameters[1].Value
            .Should().Be(entityIds.Where(a => a > 5).Select(a => a.ToString()).ToArray()[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildDbCommand_InterpolatedTemporaryTable_DatabaseAdapterDoesNotSupportTemporaryTables_ShouldThrow(
            Boolean useAsyncApi
        )
    {
        var entityIds = Generate.Ids();

        this.MockDatabaseAdapter.SupportsTemporaryTables(Arg.Any<DbConnection>()).Returns(false);

        await Invoking(() => CallApi(
                    useAsyncApi,
                    $"SELECT Value FROM {TemporaryTable(entityIds)}",
                    this.MockDatabaseAdapter,
                    this.MockDbConnection
                )
            )
            .Should().ThrowAsync<NotSupportedException>()
            .WithMessage(
                $"The database adapter {this.MockDatabaseAdapter.GetType()} does not support " +
                "(local / session-scoped) temporary tables. Therefore the temporary tables feature of " +
                "DbConnectionPlus can not be used with this database."
            );


        // No temporary table used - should not throw.
        await Invoking(() => CallApi(
                    useAsyncApi,
                    "SELECT 1",
                    this.MockDatabaseAdapter,
                    this.MockDbConnection
                )
            )
            .Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildDbCommand_InterpolatedTemporaryTable_ShouldInferTableNameFromValuesExpressionIfPossible(
            Boolean useAsyncApi
        )
    {
        var entityIds = Generate.Ids();
        static List<Int64> Get() => Generate.Ids();
        static List<Int64> GetEntityIds() => Generate.Ids();
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable IDE0060 // Remove unused parameter
        static List<Int64> GetEntityIdsByCategory(String category) => Generate.Ids();
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter

        InterpolatedSqlStatement statement =
            $"""
             SELECT Value FROM {TemporaryTable(entityIds)}
             UNION
             SELECT Value FROM {TemporaryTable(GetEntityIds())}
             UNION
             SELECT Value FROM {TemporaryTable(GetEntityIdsByCategory("Shoes"))}
             UNION
             SELECT Value FROM {TemporaryTable(this.testEntityIds)}
             UNION
             SELECT Value FROM {TemporaryTable(Get())}
             """;

        var (command, _) = await CallApi(
            useAsyncApi,
            statement,
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        var temporaryTables = statement.TemporaryTables;

        temporaryTables
            .Should().HaveCount(5);

        command.CommandText
            .Should().Be(
                $"""
                 SELECT Value FROM [#{temporaryTables[0].Name}]
                 UNION
                 SELECT Value FROM [#{temporaryTables[1].Name}]
                 UNION
                 SELECT Value FROM [#{temporaryTables[2].Name}]
                 UNION
                 SELECT Value FROM [#{temporaryTables[3].Name}]
                 UNION
                 SELECT Value FROM [#{temporaryTables[4].Name}]
                 """
            );

        temporaryTables[0].Name
            .Should().StartWith("EntityIds_");

        temporaryTables[1].Name
            .Should().StartWith("EntityIds_");

        temporaryTables[2].Name
            .Should().StartWith("EntityIdsByCategoryShoes_");

        temporaryTables[3].Name
            .Should().StartWith("TestEntityIds_");

        temporaryTables[4].Name
            .Should().StartWith("Values_");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_InterpolatedTemporaryTable_ShouldStoreTemporaryTable(Boolean useAsyncApi)
    {
        var entities = Generate.Multiple<Entity>();
        var entityIds = Generate.Ids();

        InterpolatedSqlStatement statement =
            $"""
             SELECT Id
             FROM   {TemporaryTable(entities)} Entities
             WHERE  Entities.Id IN (SELECT Value FROM {TemporaryTable(entityIds)})
             """;

        var (command, _) = await CallApi(
            useAsyncApi,
            statement,
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        var temporaryTables = statement.TemporaryTables;

        temporaryTables
            .Should().HaveCount(2);

        var table1 = temporaryTables[0];

        table1.Name
            .Should().StartWith("Entities_");

        table1.Values
            .Should().Be(entities);

        table1.ValuesType
            .Should().Be(typeof(Entity));

        var table2 = temporaryTables[1];

        table2.Name
            .Should().StartWith("EntityIds_");

        table2.Values
            .Should().BeEquivalentTo(entityIds);

        table2.ValuesType
            .Should().Be(typeof(Int64));

        command.CommandText
            .Should().Be(
                $"""
                 SELECT Id
                 FROM   [#{table1.Name}] Entities
                 WHERE  Entities.Id IN (SELECT Value FROM [#{table2.Name}])
                 """
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_MultipleInterpolatedParameters_ShouldStoreParameters(Boolean useAsyncApi)
    {
        var value1 = Generate.ScalarValue();
        var value2 = Generate.ScalarValue();
        var value3 = Generate.ScalarValue();

        var (command, _) = await CallApi(
            useAsyncApi,
            $"SELECT {Parameter(value1)}, {Parameter(value2)}, {Parameter(value3)}",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should()
            .Be("SELECT @Value1, @Value2, @Value3");

        command.Parameters.Count
            .Should().Be(3);

        command.Parameters[0].ParameterName
            .Should().Be("Value1");

        command.Parameters[0].Value
            .Should().Be(value1);

        command.Parameters[1].ParameterName
            .Should().Be("Value2");

        command.Parameters[1].Value
            .Should().Be(value2);

        command.Parameters[2].ParameterName
            .Should().Be("Value3");

        command.Parameters[2].Value
            .Should().Be(value3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildDbCommand_Parameter_EnumValue_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger(
            Boolean useAsyncApi
        )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var enumValue = Generate.Single<TestEnum>();

        var statement = new InterpolatedSqlStatement(
            "Code",
            ("Parameter1", enumValue)
        );

        var (command, _) = await CallApi(
            useAsyncApi,
            statement,
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.Parameters.Count
            .Should().Be(1);

        command.Parameters[0].ParameterName
            .Should().Be("Parameter1");

        command.Parameters[0].Value
            .Should().Be((Int32)enumValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task
        BuildDbCommand_Parameter_EnumValue_EnumSerializationModeIsStrings_ShouldSerializeEnumToString(
            Boolean useAsyncApi
        )
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var enumValue = Generate.Single<TestEnum>();

        var statement = new InterpolatedSqlStatement(
            "Code",
            ("Parameter1", enumValue)
        );

        var (command, _) = await CallApi(
            useAsyncApi,
            statement,
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.Parameters.Count
            .Should().Be(1);

        command.Parameters[0].ParameterName
            .Should().Be("Parameter1");

        command.Parameters[0].Value
            .Should().Be(enumValue.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldFormatAndStoreLiteral(Boolean useAsyncApi)
    {
        var (command, _) = await CallApi(
            useAsyncApi,
            $"SELECT {123.45,10:N2}, {123.45,-10:N2}",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be("SELECT     123.45, 123.45    ");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldReturnCommandDisposer(Boolean useAsyncApi)
    {
        var (_, commandDisposer) = await CallApi(
            useAsyncApi,
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        commandDisposer
            .Should().NotBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_ShouldStoreLiteral(Boolean useAsyncApi)
    {
        var (command, _) = await CallApi(
            useAsyncApi,
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be("SELECT 1");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BuildDbCommand_Transaction_ShouldUseTransaction(Boolean useAsyncApi)
    {
        await using var transaction = await this.MockDbConnection.BeginTransactionAsync();

        var (command, _) = await CallApi(
            useAsyncApi,
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            transaction
        );

        command.Transaction
            .Should().BeSameAs(transaction);
    }

    private static Task<(DbCommand, DbCommandDisposer)> CallApi(
        Boolean useAsyncApi,
        InterpolatedSqlStatement statement,
        IDatabaseAdapter databaseAdapter,
        DbConnection connection,
        DbTransaction? transaction = null,
        TimeSpan? commandTimeout = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default
    )
    {
        if (useAsyncApi)
        {
            return DbCommandBuilder.BuildDbCommandAsync(
                statement,
                databaseAdapter,
                connection,
                transaction,
                commandTimeout,
                commandType,
                cancellationToken
            );
        }

        try
        {
            return Task.FromResult(
                DbCommandBuilder.BuildDbCommand(
                    statement,
                    databaseAdapter,
                    connection,
                    transaction,
                    commandTimeout,
                    commandType,
                    cancellationToken
                )
            );
        }
        catch (Exception ex)
        {
            return Task.FromException<(DbCommand, DbCommandDisposer)>(ex);
        }
    }

    private readonly List<Int64> testEntityIds = Generate.Ids();
    private readonly Int64 testProductId = Generate.Id();
}
