using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;

namespace RentADeveloper.DbConnectionPlus.UnitTests.DatabaseAdapters.Oracle;

public class OracleTemporaryTableBuilderTests : UnitTestsBase
{
    [Fact]
    public void BuildTemporaryTable_AllowTemporaryTablesIsFalse_ShouldThrow()
    {
        OracleDatabaseAdapter.AllowTemporaryTables = false;

        Invoking(() => this.builder.BuildTemporaryTable(this.MockDbConnection, null, "Name", new[] { 1 }, typeof(int))
            )
            .Should().Throw<InvalidOperationException>()
            .WithMessage(
                "The temporary tables feature of DbConnectionPlus is currently disabled for Oracle databases. " +
                $"To enable it set {typeof(OracleDatabaseAdapter)}.AllowTemporaryTables to true, but be sure to " +
                "read the documentation first, because enabling this feature has implications for transaction " +
                "management."
            );
    }

    [Fact]
    public void BuildTemporaryTable_NameIsNullOrEmptyOrWhitespace_ShouldThrow()
    {
        Invoking(() =>
                this.builder.BuildTemporaryTable(this.MockDbConnection, null, "", new[] { 1 }, typeof(int))
            )
            .Should().Throw<ArgumentException>();

        Invoking(() =>
                this.builder.BuildTemporaryTable(this.MockDbConnection, null, " ", new[] { 1 }, typeof(int))
            )
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public Task BuildTemporaryTableAsync_AllowTemporaryTablesIsFalse_ShouldThrow()
    {
        OracleDatabaseAdapter.AllowTemporaryTables = false;

        return Invoking(() => this.builder.BuildTemporaryTableAsync(
                    this.MockDbConnection,
                    null,
                    "Name",
                    new[] { 1 },
                    typeof(int)
                )
            )
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The temporary tables feature of DbConnectionPlus is currently disabled for Oracle databases. " +
                $"To enable it set {typeof(OracleDatabaseAdapter)}.AllowTemporaryTables to true, but be sure to " +
                "read the documentation first, because enabling this feature has implications for transaction " +
                "management."
            );
    }

    [Fact]
    public async Task BuildTemporaryTableAsync_NameIsNullOrEmptyOrWhitespace_ShouldThrow()
    {
        await Invoking(() =>
                this.builder.BuildTemporaryTableAsync(this.MockDbConnection, null, "", new[] { 1 }, typeof(int))
            )
            .Should().ThrowAsync<ArgumentException>();

        await Invoking(() =>
                this.builder.BuildTemporaryTableAsync(this.MockDbConnection, null, " ", new[] { 1 }, typeof(int))
            )
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        ArgumentNullGuardVerifier.Verify(() =>
            new OracleTemporaryTableBuilder(new())
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.builder.BuildTemporaryTable(this.MockDbConnection, null, "Name", new[] { 1 }, typeof(int))
        );

        ArgumentNullGuardVerifier.Verify(() =>
            this.builder.BuildTemporaryTableAsync(this.MockDbConnection, null, "Name", new[] { 1 }, typeof(int))
        );
    }

    private readonly OracleTemporaryTableBuilder builder = new(new());
}
