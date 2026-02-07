#pragma warning disable NS1004

using System.Globalization;
using NSubstitute.ClearExtensions;
using NSubstitute.DbConnection;
using RentADeveloper.DbConnectionPlus.Converters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters;
using RentADeveloper.DbConnectionPlus.DatabaseAdapters.Oracle;
using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.Extensions;
using RentADeveloper.DbConnectionPlus.UnitTests.Mocks;

namespace RentADeveloper.DbConnectionPlus.UnitTests;

/// <summary>
/// Base class for unit tests.
/// </summary>
public class UnitTestsBase
{
    public UnitTestsBase()
    {
        // Ensure consistent culture for tests.
        CultureInfo.CurrentCulture =
            CultureInfo.CurrentUICulture =
                Thread.CurrentThread.CurrentCulture =
                    Thread.CurrentThread.CurrentUICulture =
                        new("en-US");

        this.MockDatabaseAdapter = Substitute.For<IDatabaseAdapter>();
        this.MockEntityManipulator = Substitute.For<IEntityManipulator>();

        this.MockDbConnection = Substitute.For<DbConnection>();

        this.MockDbCommand = Substitute.For<DbConnection>().SetupCommands().CreateCommand();
        this.MockDbCommand.ClearSubstitute();

        this.MockDbCommand.CreateParameter().Returns(_ => Substitute.For<DbParameter>());

        var dbParameterCollection = new MockDbParameterCollection();
        this.MockDbCommand.Parameters.Returns(dbParameterCollection);

        this.MockDbDataReader = Substitute.For<DbDataReader>();

        this.MockDbCommand.ExecuteReader(Arg.Any<CommandBehavior>()).Returns(this.MockDbDataReader);
        this.MockDbCommand.ExecuteReaderAsync(Arg.Any<CommandBehavior>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(this.MockDbDataReader));

        this.MockDbConnection.CreateCommand().Returns(this.MockDbCommand);

        this.MockTemporaryTableBuilder = Substitute.For<ITemporaryTableBuilder>();

        this.MockTemporaryTableBuilder.BuildTemporaryTable(
            Arg.Any<DbConnection>(),
            Arg.Any<DbTransaction?>(),
            Arg.Any<String>(),
            Arg.Any<IEnumerable>(),
            Arg.Any<Type>(),
            Arg.Any<CancellationToken>()
        ).Returns(new TemporaryTableDisposer(Substitute.For<Action>(), Substitute.For<Func<ValueTask>>()));

        this.MockTemporaryTableBuilder.BuildTemporaryTableAsync(
            Arg.Any<DbConnection>(),
            Arg.Any<DbTransaction?>(),
            Arg.Any<String>(),
            Arg.Any<IEnumerable>(),
            Arg.Any<Type>(),
            Arg.Any<CancellationToken>()
        ).Returns(new TemporaryTableDisposer(Substitute.For<Action>(), Substitute.For<Func<ValueTask>>()));

        this.MockDatabaseAdapter.SupportsTemporaryTables(Arg.Any<DbConnection>()).Returns(true);

        this.MockDatabaseAdapter.TemporaryTableBuilder.Returns(this.MockTemporaryTableBuilder);

        this.MockDatabaseAdapter.QuoteIdentifier(Arg.Any<String>())
            .Returns(info => $"[{info.ArgAt<String>(0)}]");

        this.MockDatabaseAdapter.QuoteTemporaryTableName(Arg.Any<String>(), this.MockDbConnection)
            .Returns(info => $"[#{info.ArgAt<String>(0)}]");

        this.MockDatabaseAdapter.FormatParameterName(Arg.Any<String>())
            .Returns(info => $"@{info.ArgAt<String>(0)}");

        this.MockDatabaseAdapter
            .When(a => a.BindParameterValue(Arg.Any<DbParameter>(), Arg.Any<Object?>()))
            .Do(info =>
                {
                    var parameter = info.ArgAt<DbParameter>(0);
                    var value = info.ArgAt<Object?>(1);

                    if (value is Enum enumValue)
                    {
                        parameter.DbType = DbConnectionPlusConfiguration.Instance.EnumSerializationMode switch
                        {
                            EnumSerializationMode.Integers =>
                                DbType.Int32,

                            EnumSerializationMode.Strings =>
                                DbType.String,

                            _ =>
                                throw new NotSupportedException(
                                    $"The {nameof(EnumSerializationMode)} " +
                                    $"{DbConnectionPlusConfiguration.Instance.EnumSerializationMode.ToDebugString()} " +
                                    "is not supported."
                                )
                        };

                        parameter.Value =
                            EnumSerializer.SerializeEnum(
                                enumValue,
                                DbConnectionPlusConfiguration.Instance.EnumSerializationMode
                            );
                    }
                    else
                    {
                        parameter.Value = value ?? DBNull.Value;
                    }
                }
            );

        this.MockDatabaseAdapter.EntityManipulator.Returns(this.MockEntityManipulator);

        this.MockInterceptDbCommand = Substitute.For<InterceptDbCommand>();

        // Reset all settings to defaults before each test.
        DbConnectionPlusConfiguration.Instance = new()
        {
            EnumSerializationMode = EnumSerializationMode.Strings,
            InterceptDbCommand = this.MockInterceptDbCommand
        };
        EntityHelper.ResetEntityTypeMetadataCache();
        OracleDatabaseAdapter.AllowTemporaryTables = false;

        typeof(DbConnectionPlusConfiguration).GetMethod(nameof(DbConnectionPlusConfiguration.RegisterDatabaseAdapter))!
            .MakeGenericMethod(this.MockDbConnection.GetType())
            .Invoke(DbConnectionPlusConfiguration.Instance, [this.MockDatabaseAdapter]);
    }

    /// <summary>
    /// The mocked <see cref="IDatabaseAdapter" /> to use in tests.
    /// </summary>
    protected IDatabaseAdapter MockDatabaseAdapter { get; }

    /// <summary>
    /// The mocked <see cref="DbCommand" /> to use in tests.
    /// </summary>
    protected DbCommand MockDbCommand { get; }

    /// <summary>
    /// The mocked <see cref="DbConnection" /> to use in tests.
    /// </summary>
    protected DbConnection MockDbConnection { get; }

    /// <summary>
    /// The mocked <see cref="DbDataReader" /> to use in tests.
    /// </summary>
    protected DbDataReader MockDbDataReader { get; }

    /// <summary>
    /// The mocked <see cref="IEntityManipulator" /> to use in tests.
    /// </summary>
    protected IEntityManipulator MockEntityManipulator { get; }

    /// <summary>
    /// The mocked <see cref="InterceptDbCommand" /> delegate to use in tests.
    /// </summary>
    protected InterceptDbCommand MockInterceptDbCommand { get; }

    /// <summary>
    /// The mocked <see cref="ITemporaryTableBuilder" /> to use in tests.
    /// </summary>
    protected ITemporaryTableBuilder MockTemporaryTableBuilder { get; }
}
