namespace RentADeveloper.DbConnectionPlus.IntegrationTests;

public sealed class
    DbConnectionExtensions_ParameterTests_MySql :
    DbConnectionExtensions_ParameterTests<MySqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ParameterTests_Oracle :
    DbConnectionExtensions_ParameterTests<OracleTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ParameterTests_PostgreSql :
    DbConnectionExtensions_ParameterTests<PostgreSqlTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ParameterTests_Sqlite :
    DbConnectionExtensions_ParameterTests<SqliteTestDatabaseProvider>;

public sealed class
    DbConnectionExtensions_ParameterTests_SqlServer :
    DbConnectionExtensions_ParameterTests<SqlServerTestDatabaseProvider>;

public abstract class
    DbConnectionExtensions_ParameterTests<TTestDatabaseProvider> : IntegrationTestsBase<TTestDatabaseProvider>
    where TTestDatabaseProvider : ITestDatabaseProvider, new()
{
    [Fact]
    public void Parameter_EnumValue_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        // The variable name must be different from the variable name used in
        // Parameter_EnumValue_EnumSerializationModeIsStrings_ShouldSerializeEnumToString.
        // Otherwise, Oracle would complain, because it would try to reuse the execution plan for the query but the
        // parameters have different types (integer vs. string).
        var enumValue1 = Generate.Single<TestEnum>();

        this.Connection
            .ExecuteScalar<Int32>(
                $"SELECT {Parameter(enumValue1)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be((Int32)enumValue1);
    }

    [Fact]
    public void Parameter_EnumValue_EnumSerializationModeIsStrings_ShouldSerializeEnumToString()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        // The variable name must be different from the variable name used in
        // Parameter_EnumValue_EnumSerializationModeIsIntegers_ShouldSerializeEnumToInteger.
        // Otherwise, Oracle would complain, because it would try to reuse the execution plan for the query but the
        // parameters have different types (integer vs. string).
        var enumValue2 = Generate.Single<TestEnum>();

        this.Connection
            .ExecuteScalar<String>(
                $"SELECT {Parameter(enumValue2)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(enumValue2.ToString());
    }

    [Fact]
    public void Parameter_MultipleParameters_ShouldPassValuesAsParameters()
    {
        const Int64 int64 = 123L;
        var guid = Guid.NewGuid();
        var dateTime = new DateTime(2025, 12, 31, 23, 59, 59);

        this.Connection
            .QuerySingle<(Int64, Guid, DateTime)>(
                $"SELECT {Parameter(int64)}, {Parameter(guid)}, {Parameter(dateTime)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be((int64, guid, dateTime));
    }

    [Fact]
    public void Parameter_ShouldPassValueAsParameter()
    {
        const Int64 int64 = 123L;
        this.Connection
            .ExecuteScalar<Int64>(
                $"SELECT {Parameter(int64)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(int64);

        var guid = Guid.NewGuid();
        this.Connection
            .ExecuteScalar<Guid>(
                $"SELECT {Parameter(guid)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(guid);

        var dateTime = new DateTime(2025, 12, 31, 23, 59, 59);
        this.Connection
            .ExecuteScalar<DateTime>(
                $"SELECT {Parameter(dateTime)}",
                cancellationToken: TestContext.Current.CancellationToken
            )
            .Should().Be(dateTime);
    }
}
