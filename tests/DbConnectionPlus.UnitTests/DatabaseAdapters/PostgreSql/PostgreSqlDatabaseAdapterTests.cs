using NpgsqlTypes;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.PostgreSql;

public class PostgreSqlDatabaseAdapterTests : UnitTestsBase
{
    private readonly PostgreSqlDatabaseAdapter adapter = new();

    [Fact]
    public void BindParameterValue_BytesValue_ShouldSetDbTypeAndValue()
    {
        var parameter = Substitute.For<DbParameter>();

        var value = Generate.Single<byte[]>();

        this.adapter.BindParameterValue(parameter, value);

        parameter.DbType.Should().Be(DbType.Binary);

        parameter.Value.Should().Be(value);
    }

    [Fact]
    public void BindParameterValue_DateTimeValue_ShouldSetDbTypeAndValue()
    {
        var parameter = Substitute.For<DbParameter>();

        var value = DateTime.UtcNow;

        this.adapter.BindParameterValue(parameter, value);

        parameter.DbType.Should().Be(DbType.DateTime2);

        parameter.Value.Should().Be(value);
    }

    [Fact]
    public void BindParameterValue_EnumValue_EnumSerializationModeIsIntegers_ShouldBindEnumAsInteger()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Integers;

        var parameter = Substitute.For<DbParameter>();

        var enumValue = Generate.Single<TestEnum>();

        this.adapter.BindParameterValue(parameter, enumValue);

        parameter.DbType.Should().Be(DbType.Int32);

        parameter.Value.Should().Be((int)enumValue);
    }

    [Fact]
    public void BindParameterValue_EnumValue_EnumSerializationModeIsStrings_ShouldBindEnumAsString()
    {
        DbConnectionPlusConfiguration.Instance.EnumSerializationMode = EnumSerializationMode.Strings;

        var parameter = Substitute.For<DbParameter>();

        var enumValue = Generate.Single<TestEnum>();

        this.adapter.BindParameterValue(parameter, enumValue);

        parameter.DbType.Should().Be(DbType.String);

        parameter.Value.Should().Be(enumValue.ToString());
    }

    [Fact]
    public void BindParameterValue_ShouldSetValue()
    {
        var parameter = Substitute.For<DbParameter>();

        var value = Generate.ScalarValue();

        this.adapter.BindParameterValue(parameter, value);

        parameter.Value.Should().Be(value);
    }

    [Fact]
    public void EntityManipulator_ShouldReturnManipulator() =>
        this.adapter.EntityManipulator.Should().BeOfType<PostgreSqlEntityManipulator>();

    [Fact]
    public void FormatParameterName_ShouldFormatParameterName() =>
        this.adapter.FormatParameterName("Param1").Should().Be("@Param1");

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsInteger_ShouldReturnInteger()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Integers).Should().Be("integer");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Integers).Should().Be("integer");
    }

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsNotSupported_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(TestEnum), (EnumSerializationMode)999))
            .Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                $"The {nameof(EnumSerializationMode)} '999' ({typeof(EnumSerializationMode)}) is not supported.*"
            );

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsString_ShouldReturnCharacterVarying()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Strings).Should().Be("character varying(200)");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Strings)
            .Should()
            .Be("character varying(200)");
    }

    [Theory]
    [InlineData(typeof(bool?), "boolean")]
    [InlineData(typeof(bool), "boolean")]
    [InlineData(typeof(byte?), "smallint")]
    [InlineData(typeof(byte), "smallint")]
    [InlineData(typeof(byte[]), "bytea")]
    [InlineData(typeof(char?), "char(1)")]
    [InlineData(typeof(char), "char(1)")]
    [InlineData(typeof(DateOnly?), "date")]
    [InlineData(typeof(DateOnly), "date")]
    [InlineData(typeof(DateTime?), "timestamp without time zone")]
    [InlineData(typeof(DateTime), "timestamp without time zone")]
    [InlineData(typeof(decimal?), "decimal")]
    [InlineData(typeof(decimal), "decimal")]
    [InlineData(typeof(double?), "double precision")]
    [InlineData(typeof(double), "double precision")]
    [InlineData(typeof(Guid?), "uuid")]
    [InlineData(typeof(Guid), "uuid")]
    [InlineData(typeof(short?), "smallint")]
    [InlineData(typeof(short), "smallint")]
    [InlineData(typeof(int?), "integer")]
    [InlineData(typeof(int), "integer")]
    [InlineData(typeof(long?), "bigint")]
    [InlineData(typeof(long), "bigint")]
    [InlineData(typeof(float?), "real")]
    [InlineData(typeof(float), "real")]
    [InlineData(typeof(string), "text")]
    [InlineData(typeof(TimeOnly?), "time")]
    [InlineData(typeof(TimeOnly), "time")]
    [InlineData(typeof(TimeSpan?), "interval")]
    [InlineData(typeof(TimeSpan), "interval")]
    public void GetDataType_SupportedTypeType_ShouldReturnPostgreSqlDataType(Type type, string expectedResult) =>
        this.adapter.GetDataType(type, EnumSerializationMode.Strings).Should().Be(expectedResult);

    [Fact]
    public void GetDataType_UnsupportedType_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(Entity), EnumSerializationMode.Strings))
            .Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage($"Could not map the type {typeof(Entity)} to a PostgreSQL data type.*");

    [Theory]
    [InlineData(typeof(bool?), NpgsqlDbType.Boolean)]
    [InlineData(typeof(bool), NpgsqlDbType.Boolean)]
    [InlineData(typeof(byte?), NpgsqlDbType.Smallint)]
    [InlineData(typeof(byte), NpgsqlDbType.Smallint)]
    [InlineData(typeof(byte[]), NpgsqlDbType.Bytea)]
    [InlineData(typeof(char?), NpgsqlDbType.Char)]
    [InlineData(typeof(char), NpgsqlDbType.Char)]
    [InlineData(typeof(DateOnly?), NpgsqlDbType.Date)]
    [InlineData(typeof(DateOnly), NpgsqlDbType.Date)]
    [InlineData(typeof(DateTime?), NpgsqlDbType.Timestamp)]
    [InlineData(typeof(DateTime), NpgsqlDbType.Timestamp)]
    [InlineData(typeof(decimal?), NpgsqlDbType.Numeric)]
    [InlineData(typeof(decimal), NpgsqlDbType.Numeric)]
    [InlineData(typeof(double?), NpgsqlDbType.Double)]
    [InlineData(typeof(double), NpgsqlDbType.Double)]
    [InlineData(typeof(Guid?), NpgsqlDbType.Uuid)]
    [InlineData(typeof(Guid), NpgsqlDbType.Uuid)]
    [InlineData(typeof(short?), NpgsqlDbType.Smallint)]
    [InlineData(typeof(short), NpgsqlDbType.Smallint)]
    [InlineData(typeof(int?), NpgsqlDbType.Integer)]
    [InlineData(typeof(int), NpgsqlDbType.Integer)]
    [InlineData(typeof(long?), NpgsqlDbType.Bigint)]
    [InlineData(typeof(long), NpgsqlDbType.Bigint)]
    [InlineData(typeof(float?), NpgsqlDbType.Real)]
    [InlineData(typeof(float), NpgsqlDbType.Real)]
    [InlineData(typeof(string), NpgsqlDbType.Text)]
    [InlineData(typeof(TimeOnly?), NpgsqlDbType.Time)]
    [InlineData(typeof(TimeOnly), NpgsqlDbType.Time)]
    [InlineData(typeof(TimeSpan?), NpgsqlDbType.Interval)]
    [InlineData(typeof(TimeSpan), NpgsqlDbType.Interval)]
    public void GetDbType_SupportedTypeType_ShouldReturnDbDataType(Type type, NpgsqlDbType expectedResult) =>
        this.adapter.GetDbType(type, EnumSerializationMode.Strings).Should().Be(expectedResult);

    [Fact]
    public void GetDbType_UnsupportedType_ShouldThrow() =>
        Invoking(() => this.adapter.GetDbType(typeof(Entity), EnumSerializationMode.Strings))
            .Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage($"Could not map the type {typeof(Entity)} to a {typeof(NpgsqlDbType)} value.*");

    [Fact]
    public void QuoteIdentifier_ShouldQuoteIdentifier() =>
        this.adapter.QuoteIdentifier("MyTable").Should().Be("\"MyTable\"");

    [Fact]
    public void QuoteTemporaryTableName_ShouldQuoteTableName() =>
        this.adapter.QuoteTemporaryTableName("TempTable", this.MockDbConnection).Should().Be("\"TempTable\"");

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() => this.adapter.BindParameterValue(Substitute.For<DbParameter>(), null));

        ArgumentNullGuardVerifier.Verify(() =>
            this.adapter.WasSqlStatementCancelledByCancellationToken(new(), CancellationToken.None)
        );

        ArgumentNullGuardVerifier.Verify(() => this.adapter.GetDataType(typeof(int), EnumSerializationMode.Integers));

        ArgumentNullGuardVerifier.Verify(() => this.adapter.GetDbType(typeof(int), EnumSerializationMode.Integers));
    }

    [Fact]
    public void TemporaryTableBuilder_ShouldReturnBuilder() =>
        this.adapter.TemporaryTableBuilder.Should().BeOfType<PostgreSqlTemporaryTableBuilder>();
}
