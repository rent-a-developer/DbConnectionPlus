using AutoFixture;
using AutoFixture.AutoNSubstitute;
using RentADeveloper.DbConnectionPlus.Readers;
using RentADeveloper.DbConnectionPlus.UnitTests.Assertions;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Readers;

public class DisposeSignalingDataReaderDecoratorTests : UnitTestsBase
{
    /// <inheritdoc />
    public DisposeSignalingDataReaderDecoratorTests()
    {
        this.decoratedReader = Substitute.For<DbDataReader>();
        this.decorator = new(this.decoratedReader, this.MockDatabaseAdapter, CancellationToken.None);
    }

    [Fact]
    public void Dispose_ShouldInvokeOnDisposingFunction()
    {
        var onDisposingFunction = Substitute.For<Action>();
        this.decorator.OnDisposing = onDisposingFunction;

        this.decorator.Dispose();

        onDisposingFunction.Received()();
    }

    [Fact]
    public async Task DisposeAsync_ShouldInvokeOnDisposingAsyncFunction()
    {
        var onDisposingAsyncFunction = Substitute.For<Func<ValueTask>>();
        this.decorator.OnDisposingAsync = onDisposingAsyncFunction;

        await this.decorator.DisposeAsync();

        await onDisposingAsyncFunction.Received()();
    }

    [Fact]
    public void GetFieldValue_ShouldForwardToDecoratedReader()
    {
        var ordinal = Generate.SmallNumber();
        var returnValue = Generate.SmallNumber();

        this.decoratedReader.GetFieldValue<Int32>(ordinal).Returns(returnValue);

        this.decorator.GetFieldValue<Int32>(ordinal)
            .Should().Be(returnValue);

        this.decoratedReader.Received().GetFieldValue<Int32>(ordinal);
    }

    [Fact]
    public async Task GetFieldValueAsync_ShouldForwardToDecoratedReader()
    {
        var ordinal = Generate.SmallNumber();
        var returnValue = Generate.SmallNumber();

        this.decoratedReader.GetFieldValueAsync<Int32>(ordinal, CancellationToken.None)
            .Returns(Task.FromResult(returnValue));

        (await this.decorator.GetFieldValueAsync<Int32>(ordinal, CancellationToken.None))
            .Should().Be(returnValue);

        await this.decoratedReader.Received().GetFieldValueAsync<Int32>(ordinal, CancellationToken.None);
    }

    [Fact]
    public void ShouldForwardAllMethodCallsToDecoratedReader()
    {
        var exceptions = new HashSet<String>
        {
            nameof(DisposeSignalingDataReaderDecorator.Dispose),
            nameof(DisposeSignalingDataReaderDecorator.DisposeAsync),
            nameof(DisposeSignalingDataReaderDecorator.GetData),
            nameof(DisposeSignalingDataReaderDecorator.GetFieldValue),
            nameof(DisposeSignalingDataReaderDecorator.GetFieldValueAsync)
        };

        var fixture = new Fixture();
        fixture.Customize(new AutoNSubstituteCustomization());
        fixture.Register(() => new DataTable());

        DecoratorAssertions.AssertDecoratorForwardsAllCalls(
            fixture,
            this.decorator,
            this.decoratedReader,
            exceptions
        );
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() =>
            new DisposeSignalingDataReaderDecorator(
                this.decoratedReader,
                this.MockDatabaseAdapter,
                CancellationToken.None
            )
        );

    private readonly DbDataReader decoratedReader;
    private readonly DisposeSignalingDataReaderDecorator decorator;
}
