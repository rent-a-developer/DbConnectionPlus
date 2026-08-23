// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(DeleteEntity_Command),
            nameof(DeleteEntity_Dapper),
            nameof(DeleteEntity_DbConnectionPlus)
        ]
    )]
    public void DeleteEntity__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(DeleteEntity_Command),
            nameof(DeleteEntity_Dapper),
            nameof(DeleteEntity_DbConnectionPlus)
        ]
    )]
    public void DeleteEntity__Setup() =>
        this.SetupDatabase(DeleteEntity_OperationsPerInvoke);

    [Benchmark(Baseline = true, OperationsPerInvoke = DeleteEntity_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntity_Category)]
    public void DeleteEntity_Command()
    {
        using var transaction = this.connection.BeginTransaction();

        for (var i = 0; i < DeleteEntity_OperationsPerInvoke; i++)
        {
            using var command = this.connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Entity WHERE Id = @Id";

            var idParameter = command.CreateParameter();

            idParameter.ParameterName = "@Id";
            idParameter.Value = this.entitiesInDb[i].Id;

            command.Parameters.Add(idParameter);

            command.ExecuteNonQuery();
        }

        transaction.Rollback();
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = DeleteEntity_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntity_Category)]
    public void DeleteEntity_Dapper()
    {
        using var transaction = this.connection.BeginTransaction();

        for (var i = 0; i < DeleteEntity_OperationsPerInvoke; i++)
        {
            SqlMapperExtensions.Delete(this.connection, this.entitiesInDb[i], transaction);
        }

        transaction.Rollback();
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = DeleteEntity_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntity_Category)]
    public void DeleteEntity_DbConnectionPlus()
    {
        using var transaction = this.connection.BeginTransaction();

        for (var i = 0; i < DeleteEntity_OperationsPerInvoke; i++)
        {
            this.connection.DeleteEntity(this.entitiesInDb[i], transaction);
        }

        transaction.Rollback();
    }

    private const string DeleteEntity_Category = "DeleteEntity";

    // Deletes per invocation, and also the number of rows seeded into the table.
    //
    // The transaction is rolled back rather than committed, so every invocation puts the rows back and the next
    // one deletes them again. That is what lets this category drop [IterationSetup], and dropping it is the
    // point: an iteration setup pins InvocationCount to 1, which makes the iteration time a function of this
    // constant and of how fast the machine is - and BenchmarkDotNet then warns that the iteration is too short
    // to measure reliably. Without it the pilot stage tunes the invocation count by itself, on any machine.
    //
    // Rolling back costs the same as committing: measured on this schema in an in-memory SQLite database at 1,
    // 100, 1 000, 10 000 and 40 000 rows, the rollback/commit ratio stayed between 0.98x and 1.03x with no
    // trend - and it is identical for all three implementations either way.
    //
    // It is 1 000 rather than 1 because BeginTransaction plus Rollback costs roughly 3 us against roughly
    // 0.5 us for the marginal delete. At one delete per invocation that fixed cost would be about two thirds
    // of the measurement - not a bias, since all three implementations pay it, but it would compress the
    // ratios this benchmark exists to show. Amortized over 1 000 deletes it is well under 1 %.
    private const int DeleteEntity_OperationsPerInvoke = 1000;
}
