using RentADeveloper.DbConnectionPlus.DatabaseAdapters.SqlServer;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.SqlServer;

public class SqlServerDatabaseAdapterTests : UnitTestsBase
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
            .Should().Be(DbType.DateTime2);

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
            .Should().BeOfType<SqlServerEntityManipulator>();

    [Fact]
    public void FormatParameterName_ShouldFormatParameterName() =>
        this.adapter.FormatParameterName("Param1")
            .Should().Be("@Param1");

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsInteger_ShouldReturnInt()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Integers)
            .Should().Be("int");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Integers)
            .Should().Be("int");
    }

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsNotSupported_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(TestEnum), (EnumSerializationMode)999))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                $"The {nameof(EnumSerializationMode)} '999' ({typeof(EnumSerializationMode)}) is not supported.*"
            );

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsString_ShouldReturnNVarchar()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Strings)
            .Should().Be("nvarchar(200)");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Strings)
            .Should().Be("nvarchar(200)");
    }

    [Theory]
    [InlineData(typeof(Boolean?), "bit")]
    [InlineData(typeof(Boolean), "bit")]
    [InlineData(typeof(Byte), "tinyint")]
    [InlineData(typeof(Byte?), "tinyint")]
    [InlineData(typeof(Byte[]), "varbinary(max)")]
    [InlineData(typeof(Char?), "char(1)")]
    [InlineData(typeof(Char), "char(1)")]
    [InlineData(typeof(DateOnly?), "date")]
    [InlineData(typeof(DateOnly), "date")]
    [InlineData(typeof(DateTimeOffset?), "datetimeoffset")]
    [InlineData(typeof(DateTimeOffset), "datetimeoffset")]
    [InlineData(typeof(DateTime?), "datetime2")]
    [InlineData(typeof(DateTime), "datetime2")]
    [InlineData(typeof(Decimal?), "decimal(28,10)")]
    [InlineData(typeof(Decimal), "decimal(28,10)")]
    [InlineData(typeof(Double?), "float")]
    [InlineData(typeof(Double), "float")]
    [InlineData(typeof(Guid?), "uniqueidentifier")]
    [InlineData(typeof(Guid), "uniqueidentifier")]
    [InlineData(typeof(Int16?), "smallint")]
    [InlineData(typeof(Int16), "smallint")]
    [InlineData(typeof(Int32?), "int")]
    [InlineData(typeof(Int32), "int")]
    [InlineData(typeof(Int64?), "bigint")]
    [InlineData(typeof(Int64), "bigint")]
    [InlineData(typeof(Object), "sql_variant")]
    [InlineData(typeof(Single?), "real")]
    [InlineData(typeof(Single), "real")]
    [InlineData(typeof(String), "nvarchar(max)")]
    [InlineData(typeof(TimeOnly?), "time")]
    [InlineData(typeof(TimeOnly), "time")]
    [InlineData(typeof(TimeSpan?), "time")]
    [InlineData(typeof(TimeSpan), "time")]
    public void GetDataType_SupportedTypeType_ShouldReturnSqlServerDataType(Type type, String expectedResult) =>
        this.adapter.GetDataType(type, EnumSerializationMode.Strings)
            .Should().Be(expectedResult);

    [Fact]
    public void GetDataType_UnsupportedType_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(Entity), EnumSerializationMode.Strings))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"Could not map the type {typeof(Entity)} to an SQL Server data type.*");

    [Fact]
    public void QuoteIdentifier_ShouldQuoteIdentifier() =>
        this.adapter.QuoteIdentifier("MyTable")
            .Should().Be("[MyTable]");

    [Fact]
    public void QuoteTemporaryTableName_ShouldQuoteTableName() =>
        this.adapter.QuoteTemporaryTableName("TempTable", this.MockDbConnection)
            .Should().Be("[#TempTable]");

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
            .Should().BeOfType<SqlServerTemporaryTableBuilder>();

    private readonly SqlServerDatabaseAdapter adapter = new();
}
