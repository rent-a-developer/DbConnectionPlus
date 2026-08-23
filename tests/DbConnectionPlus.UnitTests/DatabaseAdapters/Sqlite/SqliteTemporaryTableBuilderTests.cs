using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Sqlite;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.Sqlite;

public class SqliteTemporaryTableBuilderTests : UnitTestsBase
{
    [Fact]
    public void BuildTemporaryTable_NameIsNullOrEmptyOrWhitespace_ShouldThrow()
    {
        Invoking(() => this.builder.BuildTemporaryTable(this.MockDbConnection, null, "", new[] { 1 }, typeof(int)))
            .Should()
            .Throw<ArgumentException>();

        Invoking(() => this.builder.BuildTemporaryTable(this.MockDbConnection, null, " ", new[] { 1 }, typeof(int)))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_NameIsNullOrEmptyOrWhitespace_ShouldThrow()
    {
        await Invoking(() =>
                this.builder.BuildTemporaryTableAsync(this.MockDbConnection, null, "", new[] { 1 }, typeof(int))
            )
            .Should()
            .ThrowAsync<ArgumentException>();

        await Invoking(() =>
                this.builder.BuildTemporaryTableAsync(this.MockDbConnection, null, " ", new[] { 1 }, typeof(int))
            )
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() => new SqliteTemporaryTableBuilder(new()));

        ArgumentNullGuardVerifier.Verify(() =>
            this.builder.BuildTemporaryTable(this.MockDbConnection, null, "Name", new[] { 1 }, typeof(int))
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.builder.BuildTemporaryTableAsync(this.MockDbConnection, null, "Name", new[] { 1 }, typeof(int))
        );
    }

    private readonly SqliteTemporaryTableBuilder builder = new(new());
}
