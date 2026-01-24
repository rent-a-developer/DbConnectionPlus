using System.Diagnostics.CodeAnalysis;

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public class FakeConnectionC : FakeConnectionA
{
    /// <inheritdoc />
    [AllowNull]
    public override String ConnectionString { get; set; }

    /// <inheritdoc />
    public override String Database =>
        null!;

    /// <inheritdoc />
    public override String DataSource =>
        null!;

    /// <inheritdoc />
    public override String ServerVersion =>
        null!;

    /// <inheritdoc />
    public override ConnectionState State =>
        ConnectionState.Closed;

    /// <inheritdoc />
    public override void ChangeDatabase(String databaseName) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public override void Close() =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public override void Open() =>
        throw new NotImplementedException();

    /// <inheritdoc />
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    protected override DbCommand CreateDbCommand() =>
        throw new NotImplementedException();
}
