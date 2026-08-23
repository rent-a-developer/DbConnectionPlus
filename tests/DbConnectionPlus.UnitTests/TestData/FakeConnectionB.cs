using System.Diagnostics.CodeAnalysis;

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public class FakeConnectionB : DbConnection
{
    /// <inheritdoc />
    [AllowNull]
    public override string ConnectionString { get; set; }

    /// <inheritdoc />
    public override string Database => null!;

    /// <inheritdoc />
    public override string DataSource => null!;

    /// <inheritdoc />
    public override string ServerVersion => null!;

    /// <inheritdoc />
    public override ConnectionState State => ConnectionState.Closed;

    /// <inheritdoc />
    public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();

    /// <inheritdoc />
    public override void Close() => throw new NotImplementedException();

    /// <inheritdoc />
    public override void Open() => throw new NotImplementedException();

    /// <inheritdoc />
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    protected override DbCommand CreateDbCommand() => throw new NotImplementedException();
}
