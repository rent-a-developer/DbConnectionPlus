using RentADeveloper.DbConnectionPlus.DatabaseAdapters.MySql;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.MySql;

public class MySqlDatabaseAdapterTests : UnitTestsBase
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
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

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
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

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
            .Should().BeOfType<MySqlEntityManipulator>();

    [Fact]
    public void FormatParameterName_ShouldFormatParameterName() =>
        this.adapter.FormatParameterName("Param1")
            .Should().Be("@Param1");

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsInteger_ShouldReturnInt()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Integers)
            .Should().Be("INT");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Integers)
            .Should().Be("INT");
    }

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsNotSupported_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(TestEnum), (EnumSerializationMode)999))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                $"The {nameof(EnumSerializationMode)} '999' ({typeof(EnumSerializationMode)}) is not supported.*"
            );

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsString_ShouldReturnVarchar()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Strings)
            .Should().Be("VARCHAR(200)");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Strings)
            .Should().Be("VARCHAR(200)");
    }

    [Theory]
    [InlineData(typeof(Boolean?), "TINYINT(1)")]
    [InlineData(typeof(Boolean), "TINYINT(1)")]
    [InlineData(typeof(Byte?), "TINYINT UNSIGNED")]
    [InlineData(typeof(Byte), "TINYINT UNSIGNED")]
    [InlineData(typeof(Byte[]), "BLOB")]
    [InlineData(typeof(Char?), "CHAR(1)")]
    [InlineData(typeof(Char), "CHAR(1)")]
    [InlineData(typeof(DateOnly?), "DATE")]
    [InlineData(typeof(DateOnly), "DATE")]
    [InlineData(typeof(DateTime?), "DATETIME")]
    [InlineData(typeof(DateTime), "DATETIME")]
    [InlineData(typeof(Decimal?), "DECIMAL(65,30)")]
    [InlineData(typeof(Decimal), "DECIMAL(65,30)")]
    [InlineData(typeof(Double?), "DOUBLE")]
    [InlineData(typeof(Double), "DOUBLE")]
    [InlineData(typeof(Guid?), "CHAR(36)")]
    [InlineData(typeof(Guid), "CHAR(36)")]
    [InlineData(typeof(Int16?), "SMALLINT")]
    [InlineData(typeof(Int16), "SMALLINT")]
    [InlineData(typeof(Int32?), "INT")]
    [InlineData(typeof(Int32), "INT")]
    [InlineData(typeof(Int64?), "BIGINT")]
    [InlineData(typeof(Int64), "BIGINT")]
    [InlineData(typeof(Single?), "FLOAT")]
    [InlineData(typeof(Single), "FLOAT")]
    [InlineData(typeof(String), "TEXT")]
    [InlineData(typeof(TimeOnly?), "TIME")]
    [InlineData(typeof(TimeOnly), "TIME")]
    [InlineData(typeof(TimeSpan?), "TIME")]
    [InlineData(typeof(TimeSpan), "TIME")]
    public void GetDataType_SupportedTypeType_ShouldReturnMySqlDataType(Type type, String expectedResult) =>
        this.adapter.GetDataType(type, EnumSerializationMode.Strings)
            .Should().Be(expectedResult);

    [Fact]
    public void GetDataType_UnsupportedType_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(Entity), EnumSerializationMode.Strings))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"Could not map the type {typeof(Entity)} to a MySQL data type.*");

    [Fact]
    public void QuoteIdentifier_ShouldQuoteIdentifier() =>
        this.adapter.QuoteIdentifier("MyTable")
            .Should().Be("`MyTable`");

    [Fact]
    public void QuoteTemporaryTableName_ShouldQuoteTableName() =>
        this.adapter.QuoteTemporaryTableName("TempTable", this.MockDbConnection)
            .Should().Be("`TempTable`");

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
            .Should().BeOfType<MySqlTemporaryTableBuilder>();

    [Fact]
    public void WasSqlStatementCancelledByCancellationToken_ShouldAlwaysReturnFalse() =>
        this.adapter.WasSqlStatementCancelledByCancellationToken(new(), CancellationToken.None)
            .Should().BeFalse();

    private readonly MySqlDatabaseAdapter adapter = new();
}
