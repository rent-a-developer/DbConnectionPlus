using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.Sqlite;

public class SqliteDatabaseAdapterTests : UnitTestsBase
{
    [Fact]
    public void BindParameterValue_BytesValue_ShouldSetDbTypeAndValue()
    {
        var parameter = Substitute.For<DbParameter>();

        var value = Generate.Single<Byte[]>();

        this.adapter.BindParameterValue(parameter, value);

        parameter.DbType
            .Should().Be(DbType.Binary);

        parameter.Value
            .Should().Be(value);
    }

    [Fact]
    public void BindParameterValue_DateTimeValue_ShouldSetDbTypeAndValue()
    {
        var parameter = Substitute.For<DbParameter>();

        var value = DateTime.UtcNow;

        this.adapter.BindParameterValue(parameter, value);

        parameter.DbType
            .Should().Be(DbType.DateTime);

        parameter.Value
            .Should().Be(value);
    }

    [Fact]
    public void BindParameterValue_EnumValue_EnumSerializationModeIsIntegers_ShouldBindEnumAsInteger()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Integers;

        var parameter = Substitute.For<DbParameter>();

        var enumValue = Generate.Single<TestEnum>();

        this.adapter.BindParameterValue(parameter, enumValue);

        parameter.DbType
            .Should().Be(DbType.Int32);

        parameter.Value
            .Should().Be((Int32)enumValue);
    }

    [Fact]
    public void BindParameterValue_EnumValue_EnumSerializationModeIsStrings_ShouldBindEnumAsString()
    {
        DbConnectionExtensions.EnumSerializationMode = EnumSerializationMode.Strings;

        var parameter = Substitute.For<DbParameter>();

        var enumValue = Generate.Single<TestEnum>();

        this.adapter.BindParameterValue(parameter, enumValue);

        parameter.DbType
            .Should().Be(DbType.String);

        parameter.Value
            .Should().Be(enumValue.ToString());
    }

    [Fact]
    public void BindParameterValue_ShouldSetValue()
    {
        var parameter = Substitute.For<DbParameter>();

        var value = Generate.ScalarValue();

        this.adapter.BindParameterValue(parameter, value);

        parameter.Value
            .Should().Be(value);
    }

    [Fact]
    public void EntityManipulator_ShouldReturnManipulator() =>
        this.adapter.EntityManipulator
            .Should().BeOfType<SqliteEntityManipulator>();

    [Fact]
    public void FormatParameterName_ShouldFormatParameterName() =>
        this.adapter.FormatParameterName("Param1")
            .Should().Be("@Param1");

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsInteger_ShouldReturnInteger()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Integers)
            .Should().Be("INTEGER");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Integers)
            .Should().Be("INTEGER");
    }

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsNotSupported_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(TestEnum), (EnumSerializationMode)999))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                $"The {nameof(EnumSerializationMode)} '999' ({typeof(EnumSerializationMode)}) is not supported.*"
            );

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsString_ShouldReturnText()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Strings)
            .Should().Be("TEXT");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Strings)
            .Should().Be("TEXT");
    }

    [Theory]
    [InlineData(typeof(Boolean?), "INTEGER")]
    [InlineData(typeof(Boolean), "INTEGER")]
    [InlineData(typeof(Byte?), "INTEGER")]
    [InlineData(typeof(Byte), "INTEGER")]
    [InlineData(typeof(Byte[]), "BLOB")]
    [InlineData(typeof(Char?), "TEXT")]
    [InlineData(typeof(Char), "TEXT")]
    [InlineData(typeof(DateOnly?), "TEXT")]
    [InlineData(typeof(DateOnly), "TEXT")]
    [InlineData(typeof(DateTime?), "TEXT")]
    [InlineData(typeof(DateTime), "TEXT")]
    [InlineData(typeof(DateTimeOffset?), "TEXT")]
    [InlineData(typeof(DateTimeOffset), "TEXT")]
    [InlineData(typeof(Decimal?), "TEXT")]
    [InlineData(typeof(Decimal), "TEXT")]
    [InlineData(typeof(Double?), "REAL")]
    [InlineData(typeof(Double), "REAL")]
    [InlineData(typeof(Guid?), "TEXT")]
    [InlineData(typeof(Guid), "TEXT")]
    [InlineData(typeof(Int16?), "INTEGER")]
    [InlineData(typeof(Int16), "INTEGER")]
    [InlineData(typeof(Int32?), "INTEGER")]
    [InlineData(typeof(Int32), "INTEGER")]
    [InlineData(typeof(Int64?), "INTEGER")]
    [InlineData(typeof(Int64), "INTEGER")]
    [InlineData(typeof(Single?), "REAL")]
    [InlineData(typeof(Single), "REAL")]
    [InlineData(typeof(String), "TEXT")]
    [InlineData(typeof(TimeOnly?), "TEXT")]
    [InlineData(typeof(TimeOnly), "TEXT")]
    [InlineData(typeof(TimeSpan?), "TEXT")]
    [InlineData(typeof(TimeSpan), "TEXT")]
    public void GetDataType_SupportedTypeType_ShouldReturnSqliteDataType(Type type, String expectedResult) =>
        this.adapter.GetDataType(type, EnumSerializationMode.Strings)
            .Should().Be(expectedResult);

    [Fact]
    public void GetDataType_UnsupportedType_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(Entity), EnumSerializationMode.Strings))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"Could not map the type {typeof(Entity)} to an SQLite data type.*");

    [Fact]
    public void QuoteIdentifier_ShouldQuoteIdentifier() =>
        this.adapter.QuoteIdentifier("MyTable")
            .Should().Be("\"MyTable\"");

    [Fact]
    public void QuoteTemporaryTableName_ShouldQuoteTableName() =>
        this.adapter.QuoteTemporaryTableName("TempTable", this.MockDbConnection)
            .Should().Be("temp.\"TempTable\"");

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            this.adapter.BindParameterValue(Substitute.For<DbParameter>(), null)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.adapter.WasSqlStatementCancelledByCancellationToken(new(), CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.adapter.GetDataType(typeof(Int32), EnumSerializationMode.Integers)
        );
    }

    [Fact]
    public void TemporaryTableBuilder_ShouldReturnBuilder() =>
        this.adapter.TemporaryTableBuilder
            .Should().BeOfType<SqliteTemporaryTableBuilder>();

    [Fact]
    public void WasSqlStatementCancelledByCancellationToken_ShouldAlwaysReturnFalse() =>
        this.adapter.WasSqlStatementCancelledByCancellationToken(new(), CancellationToken.None)
            .Should().BeFalse();

    private readonly SqliteDatabaseAdapter adapter = new();
}
