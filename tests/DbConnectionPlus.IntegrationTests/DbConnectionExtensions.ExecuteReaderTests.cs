namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_ExecuteReaderTests_MySql :
    DbConnectionExtensions_ExecuteReaderTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteReaderTests_Oracle :
    DbConnectionExtensions_ExecuteReaderTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteReaderTests_PostgreSql :
    DbConnectionExtensions_ExecuteReaderTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteReaderTests_Sqlite :
    DbConnectionExtensions_ExecuteReaderTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ExecuteReaderTests_SqlServer :
    DbConnectionExtensions_ExecuteReaderTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_ExecuteReaderTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void ExecuteReader_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        Invoking(() =>
                {
                    using var reader = this.Connection.ExecuteReader(
                        $"SELECT * FROM {Q("Entity")}",
                        cancellationToken: cancellationToken
                    );
                }
            )
            .Should().Throw<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public void ExecuteReader_CommandBehavior_ShouldPassUseCommandBehavior()
    {
        var reader = this.Connection.ExecuteReader(
            $"SELECT * FROM {Q("Entity")}",
            commandBehavior: CommandBehavior.CloseConnection,
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.Dispose();

        this.Connection.State
            .Should().Be(ConnectionState.Closed);
    }

    [Fact]
    public void ExecuteReader_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>();

        using var reader = this.Connection.ExecuteReader(
            Q("GetEntityIdsAndStringValues"),
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entity.Id);

            reader.GetString(1)
                .Should().Be(entity.StringValue);
        }
    }

    [Fact]
    public void ExecuteReader_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterDataReaderDisposal()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        InterpolatedSqlStatement statement = $"""
                                              SELECT     {Q("Id")}
                                              FROM       {TemporaryTable(entities)}
                                              """;

        var temporaryTableName = statement.TemporaryTables.First().Name;

        var reader = this.Connection.ExecuteReader(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        reader.Dispose();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void ExecuteReader_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        using var reader = this.Connection.ExecuteReader(
            $"""
             SELECT     {Q("Id")}, {Q("StringValue")}, {Q("DecimalValue")}
             FROM       {TemporaryTable(entities)}
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entity.Id);

            reader.GetString(1)
                .Should().Be(entity.StringValue);

            reader.GetDecimal(2)
                .Should().Be(entity.DecimalValue);
        }
    }

    [Fact]
    public void ExecuteReader_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        using var reader = this.Connection.ExecuteReader(
            $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.Read()
            .Should().BeTrue();

        reader.GetString(0)
            .Should().Be(entity.StringValue);
    }

    [Fact]
    public void ExecuteReader_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        using var reader = this.Connection.ExecuteReader(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader.Read()
            .Should().BeTrue();

        reader.GetString(0)
            .Should().Be(entity.StringValue);
    }

    [Fact]
    public void ExecuteReader_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterDataReaderDisposal()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables.First().Name;

        var reader = this.Connection.ExecuteReader(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        reader.Dispose();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public void ExecuteReader_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();

        using var reader = this.Connection.ExecuteReader(
            $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entityId in entityIds)
        {
            reader.Read()
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entityId);
        }
    }

    [Fact]
    public void ExecuteReader_ShouldReturnDataReaderForQueryResult()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        using var reader = this.Connection.ExecuteReader(
            $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            reader.Read()
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entity.Id);

            reader.GetString(1)
                .Should().Be(entity.StringValue);
        }

        reader.Read()
            .Should().BeFalse();
    }

    [Fact]
    public void ExecuteReader_Transaction_ShouldUseTransaction()
    {
        using (var transaction = this.Connection.BeginTransaction())
        {
            var entities = this.CreateEntitiesInDb<Entity>(null, transaction);

            var reader = this.Connection.ExecuteReader(
                $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            reader.HasRows
                .Should().BeTrue();

            foreach (var entity in entities)
            {
                reader.Read()
                    .Should().BeTrue();

                reader.GetInt64(0)
                    .Should().Be(entity.Id);

                reader.GetString(1)
                    .Should().Be(entity.StringValue);
            }

            reader.Dispose();

            transaction.Rollback();
        }

        using var reader2 = this.Connection.ExecuteReader(
            $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        reader2.HasRows
            .Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteReaderAsync_CancellationToken_ShouldCancelOperationIfCancellationIsRequested()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsProperCommandCancellation, "");

        var cancellationToken = CreateCancellationTokenThatIsCancelledAfter100Milliseconds();

        this.DbCommandFactory.DelayNextDbCommand = true;

        await Invoking(async () =>
                {
                    await using var reader = await this.Connection.ExecuteReaderAsync(
                        $"SELECT * FROM {Q("Entity")}",
                        cancellationToken: cancellationToken
                    );
                }
            )
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(a => a.CancellationToken == cancellationToken);
    }

    [Fact]
    public async Task ExecuteReaderAsync_CommandBehavior_ShouldUseCommandBehavior()
    {
        var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT * FROM {Q("Entity")}",
            commandBehavior: CommandBehavior.CloseConnection,
            cancellationToken: TestContext.Current.CancellationToken
        );

        await reader.DisposeAsync();

        this.Connection.State
            .Should().Be(ConnectionState.Closed);
    }

    [Fact]
    public async Task ExecuteReaderAsync_CommandType_ShouldUseCommandType()
    {
        Assert.SkipUnless(this.TestDatabaseProvider.SupportsStoredProceduresReturningResultSet, "");

        var entities = this.CreateEntitiesInDb<Entity>();

        await using var reader = await this.Connection.ExecuteReaderAsync(
            Q("GetEntityIdsAndStringValues"),
            commandType: CommandType.StoredProcedure,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            (await reader.ReadAsync(TestContext.Current.CancellationToken))
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entity.Id);

            reader.GetString(1)
                .Should().Be(entity.StringValue);
        }
    }

    [Fact]
    public async Task
        ExecuteReaderAsync_ComplexObjectsTemporaryTable_ShouldDropTemporaryTableAfterDataReaderDisposal()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        InterpolatedSqlStatement statement = $"""
                                              SELECT     {Q("Id")}
                                              FROM       {TemporaryTable(entities)}
                                              """;
        var temporaryTableName = statement.TemporaryTables.First().Name;

        var reader = await this.Connection.ExecuteReaderAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        await reader.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        ExecuteReaderAsync_ComplexObjectsTemporaryTable_ShouldPassInterpolatedObjectsAsMultiColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entities = Generate.Multiple<Entity>();

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"""
             SELECT     {Q("Id")}, {Q("StringValue")}, {Q("DecimalValue")}
             FROM       {TemporaryTable(entities)}
             """,
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            (await reader.ReadAsync(TestContext.Current.CancellationToken))
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entity.Id);

            reader.GetString(1)
                .Should().Be(entity.StringValue);

            reader.GetDecimal(2)
                .Should().Be(entity.DecimalValue);
        }
    }

    [Fact]
    public async Task ExecuteReaderAsync_InterpolatedParameter_ShouldPassInterpolatedParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {Parameter(entity.Id)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        (await reader.ReadAsync(TestContext.Current.CancellationToken))
            .Should().BeTrue();

        reader.GetString(0)
            .Should().Be(entity.StringValue);
    }

    [Fact]
    public async Task ExecuteReaderAsync_Parameter_ShouldPassParameter()
    {
        var entity = this.CreateEntityInDb<Entity>();

        var statement = new InterpolatedSqlStatement(
            $"SELECT {Q("StringValue")} FROM {Q("Entity")} WHERE {Q("Id")} = {P("Id")}",
            ("Id", entity.Id)
        );

        await using var reader = await this.Connection.ExecuteReaderAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        (await reader.ReadAsync(TestContext.Current.CancellationToken))
            .Should().BeTrue();

        reader.GetString(0)
            .Should().Be(entity.StringValue);
    }

    [Fact]
    public async Task ExecuteReaderAsync_ScalarValuesTemporaryTable_ShouldDropTemporaryTableAfterDataReaderDisposal()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids(2);

        InterpolatedSqlStatement statement = $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}";

        var temporaryTableName = statement.TemporaryTables.First().Name;

        var reader = await this.Connection.ExecuteReaderAsync(
            statement,
            cancellationToken: TestContext.Current.CancellationToken
        );

        if (this.TestDatabaseProvider.SupportsCommandExecutionWhileDataReaderIsOpen)
        {
            this.ExistsTemporaryTableInDb(temporaryTableName)
                .Should().BeTrue();
        }

        await reader.DisposeAsync();

        this.ExistsTemporaryTableInDb(temporaryTableName)
            .Should().BeFalse();
    }

    [Fact]
    public async Task
        ExecuteReaderAsync_ScalarValuesTemporaryTable_ShouldPassInterpolatedValuesAsSingleColumnTemporaryTable()
    {
        Assert.SkipUnless(this.DatabaseAdapter.SupportsTemporaryTables(this.Connection), "");

        var entityIds = Generate.Ids();

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT {Q("Value")} FROM {TemporaryTable(entityIds)}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entityId in entityIds)
        {
            (await reader.ReadAsync(TestContext.Current.CancellationToken))
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entityId);
        }
    }

    [Fact]
    public async Task ExecuteReaderAsync_ShouldReturnDataReaderForQueryResult()
    {
        var entities = this.CreateEntitiesInDb<Entity>();

        await using var reader = await this.Connection.ExecuteReaderAsync(
            $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        foreach (var entity in entities)
        {
            (await reader.ReadAsync(TestContext.Current.CancellationToken))
                .Should().BeTrue();

            reader.GetInt64(0)
                .Should().Be(entity.Id);

            reader.GetString(1)
                .Should().Be(entity.StringValue);
        }

        (await reader.ReadAsync(TestContext.Current.CancellationToken))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteReaderAsync_Transaction_ShouldUseTransaction()
    {
        await using (var transaction = await this.Connection.BeginTransactionAsync())
        {
            var entities = this.CreateEntitiesInDb<Entity>(null, transaction);

            var reader = await this.Connection.ExecuteReaderAsync(
                $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")}",
                transaction,
                cancellationToken: TestContext.Current.CancellationToken
            );

            reader.HasRows
                .Should().BeTrue();

            foreach (var entity in entities)
            {
                (await reader.ReadAsync(TestContext.Current.CancellationToken))
                    .Should().BeTrue();

                reader.GetInt64(0)
                    .Should().Be(entity.Id);

                reader.GetString(1)
                    .Should().Be(entity.StringValue);
            }

            await reader.DisposeAsync();

            await transaction.RollbackAsync();
        }

        (await this.Connection.ExecuteReaderAsync(
                $"SELECT {Q("Id")}, {Q("StringValue")} FROM {Q("Entity")}",
                cancellationToken: TestContext.Current.CancellationToken
            )).HasRows
            .Should().BeFalse();
    }
}
