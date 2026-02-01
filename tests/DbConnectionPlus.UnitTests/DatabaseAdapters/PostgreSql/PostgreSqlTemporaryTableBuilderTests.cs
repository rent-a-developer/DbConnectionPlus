using RentADeveloper.DbConnectionPlus.DatabaseAdapters.PostgreSql;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.PostgreSql;

public class PostgreSqlTemporaryTableBuilderTests : UnitTestsBase
{
    [Fact]
    public void BuildTemporaryTable_NameIsNullOrEmptyOrWhitespace_ShouldThrow()
    {
        Invoking(() =>
                this.builder.BuildTemporaryTable(this.MockDbConnection, null, "", new[] { 1 }, typeof(Int32))
            )
            .Should().Throw<ArgumentException>();

        Invoking(() =>
                this.builder.BuildTemporaryTable(this.MockDbConnection, null, " ", new[] { 1 }, typeof(Int32))
            )
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_NameIsNullOrEmptyOrWhitespace_ShouldThrow()
    {
        await Invoking(() =>
                this.builder.BuildTemporaryTableAsync(this.MockDbConnection, null, "", new[] { 1 }, typeof(Int32))
            )
            .Should().ThrowAsync<ArgumentException>();

        await Invoking(() =>
                this.builder.BuildTemporaryTableAsync(this.MockDbConnection, null, " ", new[] { 1 }, typeof(Int32))
            )
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            new PostgreSqlTemporaryTableBuilder(new())
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.builder.BuildTemporaryTable(this.MockDbConnection, null, "Name", new[] { 1 }, typeof(Int32))
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.builder.BuildTemporaryTableAsync(this.MockDbConnection, null, "Name", new[] { 1 }, typeof(Int32))
        );
    }

    private readonly PostgreSqlTemporaryTableBuilder builder = new(new());
}
