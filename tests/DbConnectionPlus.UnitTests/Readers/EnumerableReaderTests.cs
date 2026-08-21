// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable GenericEnumeratorNotDisposed

using System.Diagnostics.CodeAnalysis;
using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Readers;

public class EnumerableReaderTests : UnitTestsBase
{
    /// <inheritdoc />
    public EnumerableReaderTests()
    {
        this.testValues = Generate.Single<Int32[]>();
        this.enumerableReader = new(this.testValues, typeof(Int32), FieldName);
    }

    [Fact]
    public void Close_ShouldCloseReader()
    {
        this.enumerableReader.IsClosed
            .Should().BeFalse();

        this.enumerableReader.Close();

        this.enumerableReader.IsClosed
            .Should().BeTrue();
    }

    [Fact]
    public void Close_ShouldDisposeEnumerator()
    {
        var enumerable = Substitute.For<IEnumerable>();
        var enumerator = Substitute.For<IEnumerator, IDisposable>();

        enumerable.GetEnumerator().Returns(enumerator);

        var reader = new EnumerableReader(enumerable, typeof(Int32), FieldName);

        reader.Close();

        ((IDisposable)enumerator).Received().Dispose();
    }

    [Fact]
    public async Task CloseAsync_ShouldDisposeEnumerator()
    {
        var enumerable = Substitute.For<IEnumerable>();
        var enumerator = Substitute.For<IEnumerator, IDisposable>();

        enumerable.GetEnumerator().Returns(enumerator);

        var reader = new EnumerableReader(enumerable, typeof(Int32), FieldName);

        await reader.CloseAsync();

        ((IDisposable)enumerator).Received().Dispose();
    }

    [Fact]
    public void Constructor_FieldNameEmptyOrWhitespace_ShouldThrow()
    {
        Invoking(() => new EnumerableReader(this.testValues, typeof(Int32), String.Empty))
            .Should().Throw<ArgumentException>();

        Invoking(() => new EnumerableReader(this.testValues, typeof(Int32), " "))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Depth_ShouldAlwaysReturnZero() =>
        this.enumerableReader.Depth
            .Should().Be(0);

    [Fact]
    public void Dispose_ShouldDisposeEnumerator()
    {
        var enumerable = Substitute.For<IEnumerable>();
        var enumerator = Substitute.For<IEnumerator, IDisposable>();

        enumerable.GetEnumerator().Returns(enumerator);

        var reader = new EnumerableReader(enumerable, typeof(Int32), FieldName);

        reader.Dispose();

        ((IDisposable)enumerator).Received().Dispose();
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeEnumerator()
    {
        var enumerable = Substitute.For<IEnumerable>();
        var enumerator = Substitute.For<IEnumerator, IDisposable>();

        enumerable.GetEnumerator().Returns(enumerator);

        var reader = new EnumerableReader(enumerable, typeof(Int32), FieldName);

        await reader.DisposeAsync();

        ((IDisposable)enumerator).Received().Dispose();
    }

    [Fact]
    public void FieldCount_ShouldAlwaysReturnOne() =>
        this.enumerableReader.FieldCount
            .Should().Be(1);

    [Fact]
    public void GetDataTypeName_InvalidOrdinal_ShouldThrow() =>
        Invoking(() => this.enumerableReader.GetDataTypeName(1))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("The specified ordinal 1 is not supported. The only supported ordinal is zero.*");

    [Fact]
    public void GetDataTypeName_ValidOrdinal_ShouldReturnNameOfValuesTypePassedToConstructor() =>
        this.enumerableReader.GetDataTypeName(0)
            .Should().Be(nameof(Int32));

    [Fact]
    public void GetFieldType_InvalidOrdinal_ShouldThrow() =>
        Invoking(() => this.enumerableReader.GetFieldType(1))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("The specified ordinal 1 is not supported. The only supported ordinal is zero.*");

    [Fact]
    public void GetFieldType_ValidOrdinal_ShouldReturnValuesTypePassedToConstructor() =>
        this.enumerableReader.GetFieldType(0)
            .Should().Be(typeof(Int32));

    [Fact]
    public void GetName_InvalidOrdinal_ShouldThrow() =>
        Invoking(() => this.enumerableReader.GetName(1))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("The specified ordinal 1 is not supported. The only supported ordinal is zero.*");

    [Fact]
    public void GetName_ValidOrdinal_ShouldReturnFieldNamePassedToConstructor() =>
        this.enumerableReader.GetName(0)
            .Should().Be(FieldName);

    [Fact]
    public void GetOrdinal_InvalidFieldName_ShouldThrow() =>
        Invoking(() => this.enumerableReader.GetOrdinal("nonExistentField"))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                "The specified field name 'nonExistentField' is not supported. The only supported field name is " +
                "'Value'.*"
            );

    [Fact]
    public void GetOrdinal_ValidFieldName_ShouldReturnOrdinal() =>
        this.enumerableReader.GetOrdinal(FieldName)
            .Should().Be(0);

    [Fact]
    public void GetTypedValue_SingleColumn_ShouldReturnCurrentValue()
    {
        AssertSingleColumnAccessor(true, typeof(Boolean), a => a.GetBoolean(0));
        AssertSingleColumnAccessor((Byte)7, typeof(Byte), a => a.GetByte(0));
        AssertSingleColumnAccessor('R', typeof(Char), a => a.GetChar(0));
        AssertSingleColumnAccessor(new DateTime(2026, 8, 20), typeof(DateTime), a => a.GetDateTime(0));
        AssertSingleColumnAccessor(12.34m, typeof(Decimal), a => a.GetDecimal(0));
        AssertSingleColumnAccessor(12.34d, typeof(Double), a => a.GetDouble(0));
        AssertSingleColumnAccessor(12.34f, typeof(Single), a => a.GetFloat(0));
        AssertSingleColumnAccessor(Guid.NewGuid(), typeof(Guid), a => a.GetGuid(0));
        AssertSingleColumnAccessor((Int16)7, typeof(Int16), a => a.GetInt16(0));
        AssertSingleColumnAccessor(7, typeof(Int32), a => a.GetInt32(0));
        AssertSingleColumnAccessor(7L, typeof(Int64), a => a.GetInt64(0));
        AssertSingleColumnAccessor("value", typeof(String), a => a.GetString(0));
    }

    [Fact]
    public void GetValue_InvalidOrdinal_ShouldThrow()
    {
        this.enumerableReader.Read();

        Invoking(() => this.enumerableReader.GetValue(1))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("The specified ordinal 1 is not supported. The only supported ordinal is zero.*");
    }

    [Fact]
    public void GetValue_ValidOrdinal_ShouldReturnCurrentValue()
    {
        foreach (var value in this.testValues)
        {
            this.enumerableReader.Read();

            this.enumerableReader.GetValue(0)
                .Should().Be(value);
        }
    }

    [Fact]
    public void GetValues_BufferTooSmall_ShouldThrow()
    {
        this.enumerableReader.Read();

        Invoking(() => this.enumerableReader.GetValues([]))
            .Should().Throw<ArgumentException>()
            .WithMessage("The specified array must have a length greater than or equal to 1.*");
    }

    [Fact]
    public void GetValues_ShouldAlwaysReturnOne()
    {
        var values = new Object[1];

        foreach (var _ in this.testValues)
        {
            this.enumerableReader.Read();

            this.enumerableReader.GetValues(values)
                .Should().Be(1);
        }
    }

    [Fact]
    public void GetValues_ShouldFillBufferWithValue()
    {
        var buffer = new Object[1];

        foreach (var value in this.testValues)
        {
            this.enumerableReader.Read();

            this.enumerableReader.GetValues(buffer);

            buffer[0]
                .Should().Be(value);
        }
    }

    [Fact]
    public void Fields_MultiColumn_ShouldMatchMappedReadableProperties()
    {
        var properties = EntityHelper.GetEntityTypeMetadata(typeof(Entity)).MappedProperties.Where(a => a.CanRead)
            .ToArray();

        using var reader = new EnumerableReader(new Entity[] { new() }, properties, EnumerableReaderOptions.None);

        reader.FieldCount
            .Should().Be(properties.Length);

        Enumerable.Range(0, properties.Length).Select(reader.GetName)
            .Should().Equal(properties.Select(a => a.PropertyName));

        properties.Select(a => reader.GetOrdinal(a.PropertyName))
            .Should().Equal(Enumerable.Range(0, properties.Length));

        reader.GetOrdinal("NonExistentField")
            .Should().Be(-1);
    }

    [Fact]
    public void GetValues_MultiColumnShortBuffer_ShouldFillAvailableEntries()
    {
        var entity = Generate.Single<Entity>();
        var properties = EntityHelper.GetEntityTypeMetadata(typeof(Entity)).MappedProperties.Where(a => a.CanRead)
            .ToArray();

        using var reader = new EnumerableReader(new[] { entity }, properties, EnumerableReaderOptions.None);

        reader.Read();

        var values = new Object[2];

        reader.GetValues(values)
            .Should().Be(values.Length);

        values.Should().Equal(properties.Take(values.Length).Select(a => a.PropertyGetter!(entity) ?? DBNull.Value));
    }

    [Fact]
    public void HasRows_ShouldAlwaysReturnTrue() =>
        this.enumerableReader.HasRows
            .Should().BeTrue();

    [Fact]
    public void Indexer_InvalidFieldName_ShouldThrow()
    {
        this.enumerableReader.Read();

        Invoking(() => this.enumerableReader["NonExistentField"])
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                "The specified field name 'NonExistentField' is not supported. The only supported field name is " +
                "'Value'.*"
            );
    }

    [Fact]
    public void Indexer_InvalidOrdinal_ShouldThrow()
    {
        this.enumerableReader.Read();

        Invoking(() => this.enumerableReader[1])
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("The specified ordinal 1 is not supported. The only supported ordinal is zero.*");
    }

    [Fact]
    public void Indexer_ValidName_ShouldReturnCurrentValue()
    {
        foreach (var value in this.testValues)
        {
            this.enumerableReader.Read();

            this.enumerableReader[FieldName]
                .Should().Be(value);
        }
    }

    [Fact]
    public void Indexer_ValidOrdinal_ShouldReturnCurrentValue()
    {
        foreach (var value in this.testValues)
        {
            this.enumerableReader.Read();

            this.enumerableReader[0]
                .Should().Be(value);
        }
    }

    [Fact]
    public void IsClosed_ShouldReturnWhetherReaderIsClosed()
    {
        this.enumerableReader.IsClosed
            .Should().BeFalse();

        this.enumerableReader.Close();

        this.enumerableReader.IsClosed
            .Should().BeTrue();
    }

    [Fact]
    public void IsDBNull_InvalidOrdinal_ShouldThrow() =>
        Invoking(() => this.enumerableReader.IsDBNull(1))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("The specified ordinal 1 is not supported. The only supported ordinal is zero.*");

    [Fact]
    public void IsDBNull_ValidOrdinal_ShouldReturnWhetherCurrentValueIsNull()
    {
        var valuesWithNulls = Generate.MultipleNullable<Int32>();
        var readerWithNulls = new EnumerableReader(valuesWithNulls, typeof(Int32), FieldName);

        foreach (var value in valuesWithNulls)
        {
            readerWithNulls.Read();

            readerWithNulls.IsDBNull(0)
                .Should().Be(value is null);
        }
    }

    [Fact]
    public void NextResult_ShouldAlwaysReturnFalse() =>
        this.enumerableReader.NextResult()
            .Should().BeFalse();

    [Fact]
    public void Read_ReaderIsClosed_ShouldThrow()
    {
        this.enumerableReader.Close();

        Invoking(() => this.enumerableReader.Read())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid attempt to call Read when reader is closed.*");
    }

    [Fact]
    public void Read_ShouldReturnTrueUntilAllValuesAreRead()
    {
        foreach (var _ in this.testValues)
        {
            this.enumerableReader.Read()
                .Should().BeTrue();
        }

        this.enumerableReader.Read()
            .Should().BeFalse();
    }

    [Fact]
    public void RecordsAffected_ShouldAlwaysReturnMinusOne() =>
        this.enumerableReader.RecordsAffected
            .Should().Be(-1);

    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() => new EnumerableReader(this.testValues, typeof(Int32), FieldName));

    private static void AssertSingleColumnAccessor(
        Object value,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type valuesType,
        Func<EnumerableReader, Object> accessor
    )
    {
        using var reader = new EnumerableReader(new[] { value }, valuesType, FieldName);

        reader.Read();

        accessor(reader)
            .Should().Be(value);
    }

    private readonly EnumerableReader enumerableReader;
    private readonly Int32[] testValues;
    private const String FieldName = "Value";
}
