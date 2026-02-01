using NpgsqlTypes;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.PostgreSql;

public class PostgreSqlDatabaseAdapterTests : UnitTestsBase
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
            .Should().BeOfType<PostgreSqlEntityManipulator>();

    [Fact]
    public void FormatParameterName_ShouldFormatParameterName() =>
        this.adapter.FormatParameterName("Param1")
            .Should().Be("@Param1");

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsInteger_ShouldReturnInteger()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Integers)
            .Should().Be("integer");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Integers)
            .Should().Be("integer");
    }

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsNotSupported_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(TestEnum), (EnumSerializationMode)999))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                $"The {nameof(EnumSerializationMode)} '999' ({typeof(EnumSerializationMode)}) is not supported.*"
            );

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsString_ShouldReturnCharacterVarying()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Strings)
            .Should().Be("character varying(200)");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Strings)
            .Should().Be("character varying(200)");
    }

    [Theory]
    [InlineData(typeof(Boolean?), "boolean")]
    [InlineData(typeof(Boolean), "boolean")]
    [InlineData(typeof(Byte?), "smallint")]
    [InlineData(typeof(Byte), "smallint")]
    [InlineData(typeof(Byte[]), "bytea")]
    [InlineData(typeof(Char?), "char(1)")]
    [InlineData(typeof(Char), "char(1)")]
    [InlineData(typeof(DateOnly?), "date")]
    [InlineData(typeof(DateOnly), "date")]
    [InlineData(typeof(DateTime?), "timestamp without time zone")]
    [InlineData(typeof(DateTime), "timestamp without time zone")]
    [InlineData(typeof(Decimal?), "decimal")]
    [InlineData(typeof(Decimal), "decimal")]
    [InlineData(typeof(Double?), "double precision")]
    [InlineData(typeof(Double), "double precision")]
    [InlineData(typeof(Guid?), "uuid")]
    [InlineData(typeof(Guid), "uuid")]
    [InlineData(typeof(Int16?), "smallint")]
    [InlineData(typeof(Int16), "smallint")]
    [InlineData(typeof(Int32?), "integer")]
    [InlineData(typeof(Int32), "integer")]
    [InlineData(typeof(Int64?), "bigint")]
    [InlineData(typeof(Int64), "bigint")]
    [InlineData(typeof(Single?), "real")]
    [InlineData(typeof(Single), "real")]
    [InlineData(typeof(String), "text")]
    [InlineData(typeof(TimeOnly?), "time")]
    [InlineData(typeof(TimeOnly), "time")]
    [InlineData(typeof(TimeSpan?), "interval")]
    [InlineData(typeof(TimeSpan), "interval")]
    public void GetDataType_SupportedTypeType_ShouldReturnPostgreSqlDataType(Type type, String expectedResult) =>
        this.adapter.GetDataType(type, EnumSerializationMode.Strings)
            .Should().Be(expectedResult);

    [Fact]
    public void GetDataType_UnsupportedType_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(Entity), EnumSerializationMode.Strings))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"Could not map the type {typeof(Entity)} to a PostgreSQL data type.*");

    [Theory]
    [InlineData(typeof(Boolean?), NpgsqlDbType.Boolean)]
    [InlineData(typeof(Boolean), NpgsqlDbType.Boolean)]
    [InlineData(typeof(Byte?), NpgsqlDbType.Smallint)]
    [InlineData(typeof(Byte), NpgsqlDbType.Smallint)]
    [InlineData(typeof(Byte[]), NpgsqlDbType.Bytea)]
    [InlineData(typeof(Char?), NpgsqlDbType.Char)]
    [InlineData(typeof(Char), NpgsqlDbType.Char)]
    [InlineData(typeof(DateOnly?), NpgsqlDbType.Date)]
    [InlineData(typeof(DateOnly), NpgsqlDbType.Date)]
    [InlineData(typeof(DateTime?), NpgsqlDbType.Timestamp)]
    [InlineData(typeof(DateTime), NpgsqlDbType.Timestamp)]
    [InlineData(typeof(Decimal?), NpgsqlDbType.Numeric)]
    [InlineData(typeof(Decimal), NpgsqlDbType.Numeric)]
    [InlineData(typeof(Double?), NpgsqlDbType.Double)]
    [InlineData(typeof(Double), NpgsqlDbType.Double)]
    [InlineData(typeof(Guid?), NpgsqlDbType.Uuid)]
    [InlineData(typeof(Guid), NpgsqlDbType.Uuid)]
    [InlineData(typeof(Int16?), NpgsqlDbType.Smallint)]
    [InlineData(typeof(Int16), NpgsqlDbType.Smallint)]
    [InlineData(typeof(Int32?), NpgsqlDbType.Integer)]
    [InlineData(typeof(Int32), NpgsqlDbType.Integer)]
    [InlineData(typeof(Int64?), NpgsqlDbType.Bigint)]
    [InlineData(typeof(Int64), NpgsqlDbType.Bigint)]
    [InlineData(typeof(Single?), NpgsqlDbType.Real)]
    [InlineData(typeof(Single), NpgsqlDbType.Real)]
    [InlineData(typeof(String), NpgsqlDbType.Text)]
    [InlineData(typeof(TimeOnly?), NpgsqlDbType.Time)]
    [InlineData(typeof(TimeOnly), NpgsqlDbType.Time)]
    [InlineData(typeof(TimeSpan?), NpgsqlDbType.Interval)]
    [InlineData(typeof(TimeSpan), NpgsqlDbType.Interval)]
    public void GetDbType_SupportedTypeType_ShouldReturnDbDataType(Type type, NpgsqlDbType expectedResult) =>
        this.adapter.GetDbType(type, EnumSerializationMode.Strings)
            .Should().Be(expectedResult);

    [Fact]
    public void GetDbType_UnsupportedType_ShouldThrow() =>
        Invoking(() => this.adapter.GetDbType(typeof(Entity), EnumSerializationMode.Strings))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"Could not map the type {typeof(Entity)} to a {typeof(NpgsqlDbType)} value.*");

    [Fact]
    public void QuoteIdentifier_ShouldQuoteIdentifier() =>
        this.adapter.QuoteIdentifier("MyTable")
            .Should().Be("\"MyTable\"");

    [Fact]
    public void QuoteTemporaryTableName_ShouldQuoteTableName() =>
        this.adapter.QuoteTemporaryTableName("TempTable", this.MockDbConnection)
            .Should().Be("\"TempTable\"");

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

        ArgumentNullGuardVerifier.Verify(() =>
            this.adapter.GetDbType(typeof(Int32), EnumSerializationMode.Integers)
        );
    }

    [Fact]
    public void TemporaryTableBuilder_ShouldReturnBuilder() =>
        this.adapter.TemporaryTableBuilder
            .Should().BeOfType<PostgreSqlTemporaryTableBuilder>();

    private readonly PostgreSqlDatabaseAdapter adapter = new();
}
