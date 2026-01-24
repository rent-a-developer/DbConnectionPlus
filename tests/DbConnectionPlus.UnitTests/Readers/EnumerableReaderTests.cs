using RentADeveloper.DbConnectionPlus.Readers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Readers;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable GenericEnumeratorNotDisposed
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
        var valuesWithNulls = Generate.NullableNumbers();
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

    private readonly EnumerableReader enumerableReader;
    private readonly Int32[] testValues;
    private const String FieldName = "Value";
}
