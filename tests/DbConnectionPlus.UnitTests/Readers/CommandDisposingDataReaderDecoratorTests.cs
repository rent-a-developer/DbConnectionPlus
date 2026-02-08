#pragma warning disable NS1001

using AutoFixture;
using AutoFixture.AutoNSubstitute;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DbCommands;
using RentADeveloper.DbConnectionPlus.Readers;
using RentADeveloper.DbConnectionPlus.UnitTests.Assertions;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Readers;

public class CommandDisposingDataReaderDecoratorTests : UnitTestsBase
{
    /// <inheritdoc />
    public CommandDisposingDataReaderDecoratorTests()
    {
        this.decoratedReader = Substitute.For<DbDataReader>();
        this.commandDisposer = Substitute.For<DbCommandDisposer>(
            Substitute.For<DbCommand>(),
            Array.Empty<TemporaryTableDisposer>(),
            default(CancellationTokenRegistration)
        );
        this.decorator = new(
            this.decoratedReader,
            this.MockDatabaseAdapter,
            this.commandDisposer,
            CancellationToken.None
        );
    }

    [Fact]
    public void Dispose_ShouldDisposeCommandDisposer()
    {
        this.decorator.Dispose();

        this.commandDisposer.Received().Dispose();
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeCommandDisposer()
    {
        await this.decorator.DisposeAsync();

        await this.commandDisposer.Received().DisposeAsync();
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
            nameof(CommandDisposingDataReaderDecorator.Dispose),
            nameof(CommandDisposingDataReaderDecorator.DisposeAsync),
            nameof(CommandDisposingDataReaderDecorator.GetData),
            nameof(CommandDisposingDataReaderDecorator.GetFieldValue),
            nameof(CommandDisposingDataReaderDecorator.GetFieldValueAsync)
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
            new CommandDisposingDataReaderDecorator(
                this.decoratedReader,
                this.MockDatabaseAdapter,
                this.commandDisposer,
                CancellationToken.None
            )
        );

    private readonly DbCommandDisposer commandDisposer;

    private readonly DbDataReader decoratedReader;
    private readonly CommandDisposingDataReaderDecorator decorator;
}
