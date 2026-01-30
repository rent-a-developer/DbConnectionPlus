// ReSharper disable UnusedParameter.Local

#pragma warning disable NS1001

using RentADeveloper.DbConnectionPlus.SqlStatements;
using DbCommandBuilder = RentADeveloper.DbConnectionPlus.DbCommands.DbCommandBuilder;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DbCommands;

public class DbCommandBuilderTests : UnitTestsBase
{
    [Fact]
    public void BuildDbCommand_CancellationToken_ShouldUseCancellationToken()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        cancellationTokenSource.Cancel();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            cancellationToken: cancellationToken
        );

        command.Received().Cancel();
    }

    [Fact]
    public void BuildDbCommand_Code_Parameters_ShouldStoreCodeAndParameters()
    {
        var statement = new InterpolatedSqlStatement(
            "Code",
            ("Parameter1", "Value1"),
            ("Parameter2", "Value2"),
            ("Parameter3", "Value3")
        );

        var (command, _) = DbCommandBuilder.BuildDbCommand(statement, this.MockDatabaseAdapter, this.MockDbConnection);

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

    [Fact]
    public void BuildDbCommand_CommandTimeout_ShouldUseCommandTimeout()
    {
        var timeout = Generate.Single<TimeSpan>();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            commandTimeout: timeout
        );

        command.CommandTimeout
            .Should().Be((Int32)timeout.TotalSeconds);
    }

    [Fact]
    public void BuildDbCommand_CommandType_ShouldUseCommandType()
    {
        var (command, _) = DbCommandBuilder.BuildDbCommand(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            commandType: CommandType.StoredProcedure
        );

        command.CommandType
            .Should().Be(CommandType.StoredProcedure);
    }

    [Fact]
    public void BuildDbCommand_InterpolatedParameter_DuplicateInferredName_ShouldAppendSuffix()
    {
        var productId = Generate.Id();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
            $"SELECT {Parameter(productId)}, {Parameter(productId)}",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be("SELECT @ProductId, @ProductId2");

        command.Parameters.Count
            .Should().Be(2);

        command.Parameters[0].ParameterName
            .Should().Be("ProductId");

        command.Parameters[1].ParameterName
            .Should().Be("ProductId2");
    }

    [Fact]
    public void BuildDbCommand_InterpolatedParameter_DuplicateInferredNameWithDifferentCasing_ShouldAppendSuffix()
    {
        var productId = Generate.Id();
        var productid = Generate.Id();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
            $"SELECT {Parameter(productId)}, {Parameter(productid)}",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be("SELECT @ProductId, @Productid2");

        command.Parameters.Count
            .Should().Be(2);

        command.Parameters[0].ParameterName
            .Should().Be("ProductId");

        command.Parameters[1].ParameterName
            .Should().Be("Productid2");
    }

    [Fact]
    public void
        BuildDbCommand_InterpolatedParameter_EnumValue_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var enumValue = Generate.Single<TestEnum>();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
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

    [Fact]
    public void
        BuildDbCommand_InterpolatedParameter_EnumValue_EnumSerializationModeIsStrings_ShouldSerializeEnumToString()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var enumValue = Generate.Single<TestEnum>();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
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

    [Fact]
    public void BuildDbCommand_InterpolatedParameter_ShouldHandleNullAndNonNullValues()
    {
        Int64? id1 = Generate.Id();
        Int64? id2 = null;
        Object value1 = Generate.Single<String>();
        Object? value2 = null;

        var (command, _) = DbCommandBuilder.BuildDbCommand(
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

    [Fact]
    public void BuildDbCommand_InterpolatedParameter_ShouldInferNameFromValueExpressionIfPossible()
    {
        var productId = Generate.Id();
        static Int64 GetProductId() => Generate.Id();
#pragma warning disable RCS1163 // Unused parameter
        static Int64 GetProductIdByCategory(String category) => Generate.Id();
#pragma warning restore RCS1163 // Unused parameter
        var productIds = Generate.Ids().ToArray();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
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

    [Fact]
    public void BuildDbCommand_InterpolatedParameter_ShouldStoreParameter()
    {
        var value = Generate.ScalarValue();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
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

    [Fact]
    public void BuildDbCommand_InterpolatedParameter_ShouldSupportComplexExpressions()
    {
        const Double baseDiscount = 0.1;
        var entityIds = Generate.Ids(20);

        var (command, _) = DbCommandBuilder.BuildDbCommand(
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

    [Fact]
    public void BuildDbCommand_InterpolatedTemporaryTable_DatabaseAdapterDoesNotSupportTemporaryTables_ShouldThrow()
    {
        var entityIds = Generate.Ids();

        this.MockDatabaseAdapter.SupportsTemporaryTables(Arg.Any<DbConnection>()).Returns(false);

        Invoking(() => DbCommandBuilder.BuildDbCommand(
                    $"SELECT Value FROM {TemporaryTable(entityIds)}",
                    this.MockDatabaseAdapter,
                    this.MockDbConnection
                )
            )
            .Should().Throw<NotSupportedException>()
            .WithMessage(
                $"The database adapter {this.MockDatabaseAdapter.GetType()} does not support " +
                "(local / session-scoped) temporary tables. Therefore the temporary tables feature of " +
                "DbConnectionPlus can not be used with this database."
            );

        // No temporary table used - should not throw.
        Invoking(() => DbCommandBuilder.BuildDbCommand(
                    "SELECT 1",
                    this.MockDatabaseAdapter,
                    this.MockDbConnection
                )
            )
            .Should().NotThrow();
    }

    [Fact]
    public void BuildDbCommand_InterpolatedTemporaryTable_ShouldInferTableNameFromValuesExpressionIfPossible()
    {
        var entityIds = Generate.Ids();
        static List<Int64> Get() => Generate.Ids();
        static List<Int64> GetEntityIds() => Generate.Ids();
#pragma warning disable RCS1163 // Unused parameter
        static List<Int64> GetEntityIdsByCategory(String category) => Generate.Ids();
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

        var (command, _) = DbCommandBuilder.BuildDbCommand(statement, this.MockDatabaseAdapter, this.MockDbConnection);

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

    [Fact]
    public void BuildDbCommand_InterpolatedTemporaryTable_ShouldStoreTemporaryTable()
    {
        var entities = Generate.Multiple<Entity>();
        var entityIds = Generate.Ids();

        InterpolatedSqlStatement statement =
            $"""
             SELECT Id
             FROM   {TemporaryTable(entities)} Entities
             WHERE  Entities.Id IN (SELECT Value FROM {TemporaryTable(entityIds)})
             """;

        var (command, _) = DbCommandBuilder.BuildDbCommand(statement, this.MockDatabaseAdapter, this.MockDbConnection);

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

    [Fact]
    public void BuildDbCommand_MultipleInterpolatedParameters_ShouldStoreParameters()
    {
        var value1 = Generate.ScalarValue();
        var value2 = Generate.ScalarValue();
        var value3 = Generate.ScalarValue();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
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

    [Fact]
    public void BuildDbCommand_Parameter_EnumValue_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var enumValue = Generate.Single<TestEnum>();

        var statement = new InterpolatedSqlStatement(
            "Code",
            ("Parameter1", enumValue)
        );

        var (command, _) = DbCommandBuilder.BuildDbCommand(statement, this.MockDatabaseAdapter, this.MockDbConnection);

        command.Parameters.Count
            .Should().Be(1);

        command.Parameters[0].ParameterName
            .Should().Be("Parameter1");

        command.Parameters[0].Value
            .Should().Be((Int32)enumValue);
    }

    [Fact]
    public void BuildDbCommand_Parameter_EnumValue_EnumSerializationModeIsStrings_ShouldSerializeEnumToString()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var enumValue = Generate.Single<TestEnum>();

        var statement = new InterpolatedSqlStatement(
            "Code",
            ("Parameter1", enumValue)
        );

        var (command, _) = DbCommandBuilder.BuildDbCommand(statement, this.MockDatabaseAdapter, this.MockDbConnection);

        command.Parameters.Count
            .Should().Be(1);

        command.Parameters[0].ParameterName
            .Should().Be("Parameter1");

        command.Parameters[0].Value
            .Should().Be(enumValue.ToString());
    }

    [Fact]
    public void BuildDbCommand_ShouldFormatAndStoreLiteral()
    {
        var (command, _) = DbCommandBuilder.BuildDbCommand(
            $"SELECT {123.45,10:N2}, {123.45,-10:N2}",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be("SELECT     123.45, 123.45    ");
    }

    [Fact]
    public void BuildDbCommand_ShouldReturnCommandDisposer()
    {
        var (_, commandDisposer) = DbCommandBuilder.BuildDbCommand(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        commandDisposer
            .Should().NotBeNull();
    }

    [Fact]
    public void BuildDbCommand_ShouldStoreLiteral()
    {
        var (command, _) = DbCommandBuilder.BuildDbCommand(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be("SELECT 1");
    }

    [Fact]
    public void BuildDbCommand_Transaction_ShouldUseTransaction()
    {
        using var transaction = this.MockDbConnection.BeginTransaction();

        var (command, _) = DbCommandBuilder.BuildDbCommand(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            transaction
        );

        command.Transaction
            .Should().BeSameAs(transaction);
    }

    [Fact]
    public async Task BuildDbCommandAsync_CancellationToken_ShouldUseCancellationToken()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await cancellationTokenSource.CancelAsync();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            cancellationToken: cancellationToken
        );

        command.Received().Cancel();
    }

    [Fact]
    public async Task BuildDbCommandAsync_Code_Parameters_ShouldStoreCodeAndParameters()
    {
        var statement = new InterpolatedSqlStatement(
            "Code",
            ("Parameter1", "Value1"),
            ("Parameter2", "Value2"),
            ("Parameter3", "Value3")
        );

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task BuildDbCommandAsync_CommandTimeout_ShouldUseCommandTimeout()
    {
        var timeout = Generate.Single<TimeSpan>();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            commandTimeout: timeout
        );

        command.CommandTimeout
            .Should().Be((Int32)timeout.TotalSeconds);
    }

    [Fact]
    public async Task BuildDbCommandAsync_CommandType_ShouldUseCommandType()
    {
        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            commandType: CommandType.StoredProcedure
        );

        command.CommandType
            .Should().Be(CommandType.StoredProcedure);
    }

    [Fact]
    public async Task
        BuildDbCommandAsync_InterpolatedParameter_EnumValue_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var enumValue = Generate.Single<TestEnum>();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task
        BuildDbCommandAsync_InterpolatedParameter_EnumValue_EnumSerializationModeIsStrings_ShouldSerializeEnumToString()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var enumValue = Generate.Single<TestEnum>();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task BuildDbCommandAsync_InterpolatedParameter_ShouldHandleNullAndNonNullValues()
    {
        Int64? id1 = Generate.Id();
        Int64? id2 = null;
        Object value1 = Generate.Single<String>();
        Object? value2 = null;

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task BuildDbCommandAsync_InterpolatedParameter_ShouldInferNameFromValueExpressionIfPossible()
    {
        var productId = Generate.Id();
        static Int64 GetProductId() => Generate.Id();
#pragma warning disable RCS1163 // Unused parameter
        static Int64 GetProductIdByCategory(String category) => Generate.Id();
#pragma warning restore RCS1163 // Unused parameter
        var productIds = Generate.Ids().ToArray();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task BuildDbCommandAsync_InterpolatedParameter_ShouldStoreParameter()
    {
        var value = Generate.ScalarValue();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task BuildDbCommandAsync_InterpolatedParameter_ShouldSupportComplexExpressions()
    {
        const Double baseDiscount = 0.1;
        var entityIds = Generate.Ids(20);

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task
        BuildDbCommandAsync_InterpolatedTemporaryTable_DatabaseAdapterDoesNotSupportTemporaryTables_ShouldThrow()
    {
        var entityIds = Generate.Ids();

        this.MockDatabaseAdapter.SupportsTemporaryTables(Arg.Any<DbConnection>()).Returns(false);

        await Invoking(() => DbCommandBuilder.BuildDbCommandAsync(
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
        await Invoking(() => DbCommandBuilder.BuildDbCommandAsync(
                    "SELECT 1",
                    this.MockDatabaseAdapter,
                    this.MockDbConnection
                )
            )
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task
        BuildDbCommandAsync_InterpolatedTemporaryTable_ShouldInferTableNameFromValuesExpressionIfPossible()
    {
        var entityIds = Generate.Ids();
        static List<Int64> Get() => Generate.Ids();
        static List<Int64> GetEntityIds() => Generate.Ids();
#pragma warning disable RCS1163 // Unused parameter
        static List<Int64> GetEntityIdsByCategory(String category) => Generate.Ids();
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

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task BuildDbCommandAsync_InterpolatedTemporaryTable_ShouldStoreTemporaryTable()
    {
        var entities = Generate.Multiple<Entity>();
        var entityIds = Generate.Ids();

        InterpolatedSqlStatement statement =
            $"""
             SELECT Id
             FROM   {TemporaryTable(entities)} Entities
             WHERE  Entities.Id IN (SELECT Value FROM {TemporaryTable(entityIds)})
             """;

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task BuildDbCommandAsync_MultipleInterpolatedParameters_ShouldStoreParameters()
    {
        var value1 = Generate.ScalarValue();
        var value2 = Generate.ScalarValue();
        var value3 = Generate.ScalarValue();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task
        BuildDbCommandAsync_Parameter_EnumValue_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var enumValue = Generate.Single<TestEnum>();

        var statement = new InterpolatedSqlStatement(
            "Code",
            ("Parameter1", enumValue)
        );

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task
        BuildDbCommandAsync_Parameter_EnumValue_EnumSerializationModeIsStrings_ShouldSerializeEnumToString()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var enumValue = Generate.Single<TestEnum>();

        var statement = new InterpolatedSqlStatement(
            "Code",
            ("Parameter1", enumValue)
        );

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
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

    [Fact]
    public async Task BuildDbCommandAsync_ShouldFormatAndStoreLiteral()
    {
        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            $"SELECT {123.45,10:N2}, {123.45,-10:N2}",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be("SELECT     123.45, 123.45    ");
    }

    [Fact]
    public async Task BuildDbCommandAsync_ShouldReturnCommandDisposer()
    {
        var (_, commandDisposer) = await DbCommandBuilder.BuildDbCommandAsync(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        commandDisposer
            .Should().NotBeNull();
    }

    [Fact]
    public async Task BuildDbCommandAsync_ShouldStoreLiteral()
    {
        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection
        );

        command.CommandText
            .Should().Be("SELECT 1");
    }

    [Fact]
    public async Task BuildDbCommandAsync_Transaction_ShouldUseTransaction()
    {
        await using var transaction = await this.MockDbConnection.BeginTransactionAsync();

        var (command, _) = await DbCommandBuilder.BuildDbCommandAsync(
            "SELECT 1",
            this.MockDatabaseAdapter,
            this.MockDbConnection,
            transaction
        );

        command.Transaction
            .Should().BeSameAs(transaction);
    }

    private readonly List<Int64> testEntityIds = Generate.Ids();
    private readonly Int64 testProductId = Generate.Id();
}
