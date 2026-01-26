using NSubstitute.DbConnection;
using Oracle.ManagedDataAccess.Client;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.Oracle;

public class OracleDatabaseAdapterTests : UnitTestsBase
{
    [Fact]
    public void AllowTemporaryTables_ShouldReturnFalsePerDefault() =>
        OracleDatabaseAdapter.AllowTemporaryTables
            .Should().BeFalse();

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
    public void BindParameterValue_DateOnlyValue_ShouldSetDbTypeAndValue()
    {
        var parameter = Substitute.For<DbParameter>();

        var value = DateOnly.FromDateTime(DateTime.Now);

        this.adapter.BindParameterValue(parameter, value);

        parameter.DbType
            .Should().Be(DbType.Date);

        parameter.Value
            .Should().Be(value.ToDateTime(TimeOnly.MinValue));
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
    public void BindParameterValue_GuidValue_ShouldSetDbTypeAndValue()
    {
        var parameter = Substitute.For<DbParameter>();

        var value = Guid.NewGuid();

        this.adapter.BindParameterValue(parameter, value);

        parameter.DbType
            .Should().Be(DbType.Binary);

        parameter.Value
            .Should().Be(value);
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
    public void BindParameterValue_TimeOnlyValue_ShouldSetDbTypeAndValue()
    {
        var parameter = Substitute.For<DbParameter>();

        var value = TimeOnly.FromDateTime(DateTime.Now);

        this.adapter.BindParameterValue(parameter, value);

        parameter.DbType
            .Should().Be(DbType.Time);

        (parameter as OracleParameter)?.OracleDbType
            .Should().Be(OracleDbType.IntervalDS);

        parameter.Value
            .Should().Be(value.ToTimeSpan());
    }

    [Fact]
    public void EntityManipulator_ShouldReturnManipulator() =>
        this.adapter.EntityManipulator
            .Should().BeOfType<OracleEntityManipulator>();

    [Fact]
    public void FormatParameterName_ShouldFormatParameterName() =>
        this.adapter.FormatParameterName("Param1")
            .Should().Be(":\"Param1\"");

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsInteger_ShouldReturnNumber()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Integers)
            .Should().Be("NUMBER(10)");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Integers)
            .Should().Be("NUMBER(10)");
    }

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsNotSupported_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(TestEnum), (EnumSerializationMode)999))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                $"The {nameof(EnumSerializationMode)} '999' ({typeof(EnumSerializationMode)}) is not supported.*"
            );

    [Fact]
    public void GetDataType_EnumType_EnumSerializationModeIsString_ShouldReturnNVarchar2()
    {
        this.adapter.GetDataType(typeof(TestEnum), EnumSerializationMode.Strings)
            .Should().Be("NVARCHAR2(200)");

        this.adapter.GetDataType(typeof(TestEnum?), EnumSerializationMode.Strings)
            .Should().Be("NVARCHAR2(200)");
    }

    [Theory]
    [InlineData(typeof(Boolean?), "NUMBER(1)")]
    [InlineData(typeof(Boolean), "NUMBER(1)")]
    [InlineData(typeof(Byte), "NUMBER(3)")]
    [InlineData(typeof(Byte?), "NUMBER(3)")]
    [InlineData(typeof(Byte[]), "RAW(2000)")]
    [InlineData(typeof(Char?), "CHAR(1)")]
    [InlineData(typeof(Char), "CHAR(1)")]
    [InlineData(typeof(DateOnly?), "DATE")]
    [InlineData(typeof(DateOnly), "DATE")]
    [InlineData(typeof(DateTimeOffset?), "TIMESTAMP WITH TIME ZONE")]
    [InlineData(typeof(DateTimeOffset), "TIMESTAMP WITH TIME ZONE")]
    [InlineData(typeof(DateTime?), "TIMESTAMP")]
    [InlineData(typeof(DateTime), "TIMESTAMP")]
    [InlineData(typeof(Decimal?), "NUMBER(28,10)")]
    [InlineData(typeof(Decimal), "NUMBER(28,10)")]
    [InlineData(typeof(Double?), "BINARY_DOUBLE")]
    [InlineData(typeof(Double), "BINARY_DOUBLE")]
    [InlineData(typeof(Guid?), "RAW(16)")]
    [InlineData(typeof(Guid), "RAW(16)")]
    [InlineData(typeof(Int16?), "NUMBER(5)")]
    [InlineData(typeof(Int16), "NUMBER(5)")]
    [InlineData(typeof(Int32?), "NUMBER(10)")]
    [InlineData(typeof(Int32), "NUMBER(10)")]
    [InlineData(typeof(Int64?), "NUMBER(19)")]
    [InlineData(typeof(Int64), "NUMBER(19)")]
    [InlineData(typeof(Single?), "BINARY_FLOAT")]
    [InlineData(typeof(Single), "BINARY_FLOAT")]
    [InlineData(typeof(String), "NVARCHAR2(2000)")]
    [InlineData(typeof(TimeOnly?), "INTERVAL DAY TO SECOND")]
    [InlineData(typeof(TimeOnly), "INTERVAL DAY TO SECOND")]
    [InlineData(typeof(TimeSpan?), "INTERVAL DAY TO SECOND")]
    [InlineData(typeof(TimeSpan), "INTERVAL DAY TO SECOND")]
    public void GetDataType_SupportedTypeType_ShouldReturnSqlServerDataType(Type type, String expectedResult) =>
        this.adapter.GetDataType(type, EnumSerializationMode.Strings)
            .Should().Be(expectedResult);

    [Fact]
    public void GetDataType_UnsupportedType_ShouldThrow() =>
        Invoking(() => this.adapter.GetDataType(typeof(Entity), EnumSerializationMode.Strings))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"Could not map the type {typeof(Entity)} to an Oracle data type.*");

    [Fact]
    public void GetDbType_EnumType_EnumSerializationModeIsInteger_ShouldReturnInt32()
    {
        this.adapter.GetDbType(typeof(TestEnum), EnumSerializationMode.Integers)
            .Should().Be(DbType.Int32);

        this.adapter.GetDbType(typeof(TestEnum?), EnumSerializationMode.Integers)
            .Should().Be(DbType.Int32);
    }

    [Fact]
    public void GetDbType_EnumType_EnumSerializationModeIsNotSupported_ShouldThrow() =>
        Invoking(() => this.adapter.GetDbType(typeof(TestEnum), (EnumSerializationMode)999))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                $"The {nameof(EnumSerializationMode)} '999' ({typeof(EnumSerializationMode)}) is not supported.*"
            );

    [Fact]
    public void GetDbType_EnumType_EnumSerializationModeIsString_ShouldReturnString()
    {
        this.adapter.GetDbType(typeof(TestEnum), EnumSerializationMode.Strings)
            .Should().Be(DbType.String);

        this.adapter.GetDbType(typeof(TestEnum?), EnumSerializationMode.Strings)
            .Should().Be(DbType.String);
    }

    [Theory]
    [InlineData(typeof(Boolean?), DbType.Boolean)]
    [InlineData(typeof(Boolean), DbType.Boolean)]
    [InlineData(typeof(Byte), DbType.Byte)]
    [InlineData(typeof(Byte?), DbType.Byte)]
    [InlineData(typeof(Byte[]), DbType.Binary)]
    [InlineData(typeof(Char?), DbType.StringFixedLength)]
    [InlineData(typeof(Char), DbType.StringFixedLength)]
    [InlineData(typeof(DateOnly?), DbType.Date)]
    [InlineData(typeof(DateOnly), DbType.Date)]
    [InlineData(typeof(DateTimeOffset?), DbType.DateTimeOffset)]
    [InlineData(typeof(DateTimeOffset), DbType.DateTimeOffset)]
    [InlineData(typeof(DateTime?), DbType.DateTime)]
    [InlineData(typeof(DateTime), DbType.DateTime)]
    [InlineData(typeof(Decimal?), DbType.Decimal)]
    [InlineData(typeof(Decimal), DbType.Decimal)]
    [InlineData(typeof(Double?), DbType.Double)]
    [InlineData(typeof(Double), DbType.Double)]
    [InlineData(typeof(Guid?), DbType.Guid)]
    [InlineData(typeof(Guid), DbType.Guid)]
    [InlineData(typeof(Int16?), DbType.Int16)]
    [InlineData(typeof(Int16), DbType.Int16)]
    [InlineData(typeof(Int32?), DbType.Int32)]
    [InlineData(typeof(Int32), DbType.Int32)]
    [InlineData(typeof(Int64?), DbType.Int64)]
    [InlineData(typeof(Int64), DbType.Int64)]
    [InlineData(typeof(Single?), DbType.Single)]
    [InlineData(typeof(Single), DbType.Single)]
    [InlineData(typeof(String), DbType.String)]
    [InlineData(typeof(TimeOnly?), DbType.Time)]
    [InlineData(typeof(TimeOnly), DbType.Time)]
    [InlineData(typeof(TimeSpan?), DbType.Time)]
    [InlineData(typeof(TimeSpan), DbType.Time)]
    public void GetDbType_SupportedTypeType_ShouldReturnSqlServerDataType(Type type, DbType expectedResult) =>
        this.adapter.GetDbType(type, EnumSerializationMode.Strings)
            .Should().Be(expectedResult);

    [Fact]
    public void GetDbType_UnsupportedType_ShouldThrow() =>
        Invoking(() => this.adapter.GetDbType(typeof(Entity), EnumSerializationMode.Strings))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"Could not map the type {typeof(Entity)} to a {typeof(DbType)} value.*");

    [Fact]
    public void QuoteIdentifier_ShouldQuoteIdentifier() =>
        this.adapter.QuoteIdentifier("MyTable")
            .Should().Be("\"MyTable\"");

    [Fact]
    public void QuoteTemporaryTableName_ShouldQuoteTableName()
    {
        this.MockDbConnection.SetupQuery("SELECT VALUE FROM v$parameter WHERE NAME = 'private_temp_table_prefix'")
            .Returns(new { VALUE = "MockPrefix" });

        this.adapter.QuoteTemporaryTableName("TempTable", this.MockDbConnection)
            .Should().Be("\"MockPrefixTempTable\"");
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            this.adapter.BindParameterValue(Substitute.For<DbParameter>(), null)
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.adapter.WasSqlStatementCancelledByCancellationToken(new(), CancellationToken.None)
        );
    }

    [Fact]
    public void SupportsTemporaryTables_OracleVersionEqualToOrGreaterThan18_ShouldReturnTrue()
    {
        this.MockDbConnection.SetupQuery("SELECT 1 FROM v$instance WHERE version >= '18'")
            .Returns(new { Value = 1 });

        this.adapter.SupportsTemporaryTables(this.MockDbConnection)
            .Should().BeTrue();
    }

    [Fact]
    public void SupportsTemporaryTables_OracleVersionLessThan18_ShouldReturnFalse()
    {
        this.MockDbConnection.SetupQuery("SELECT 1 FROM v$instance WHERE version >= '18'")
            .Returns(Array.Empty<Object>());

        this.adapter.SupportsTemporaryTables(this.MockDbConnection)
            .Should().BeFalse();
    }

    [Fact]
    public void TemporaryTableBuilder_AllowTemporaryTablesIsFalse_ShouldThrow()
    {
        OracleDatabaseAdapter.AllowTemporaryTables = false;

        Invoking(() => this.adapter.TemporaryTableBuilder)
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "The temporary tables feature of DbConnectionPlus is currently disabled for Oracle databases. " +
                $"To enable it set {typeof(OracleDatabaseAdapter)}.AllowTemporaryTables to true, but be sure to " +
                "read the documentation first, because enabling this feature has implications for transaction " +
                "management."
            );
    }

    [Fact]
    public void TemporaryTableBuilder_AllowTemporaryTablesIsTrue_ShouldReturnBuilder()
    {
        OracleDatabaseAdapter.AllowTemporaryTables = true;

        this.adapter.TemporaryTableBuilder
            .Should().BeOfType<OracleTemporaryTableBuilder>();
    }

    private readonly OracleDatabaseAdapter adapter = new();
}
