// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable InconsistentNaming

#pragma warning disable RCS1196

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public partial class Benchmarks
{
    [GlobalCleanup(
        Targets =
        [
            nameof(DeleteEntities_Command),
            nameof(DeleteEntities_Dapper),
            nameof(DeleteEntities_DbConnectionPlus)
        ]
    )]
    public void DeleteEntities__Cleanup() =>
        this.connection.Dispose();

    [GlobalSetup(
        Targets =
        [
            nameof(DeleteEntities_Command),
            nameof(DeleteEntities_Dapper),
            nameof(DeleteEntities_DbConnectionPlus)
        ]
    )]
    public void DeleteEntities__Setup()
    {
        this.SetupDatabase(DeleteEntities_EntitiesPerOperation * DeleteEntities_OperationsPerInvoke);

        // The batches are built once here so that the benchmarks do not slice the entity list inside the measured
        // region. The slicing was identical for all three implementations and therefore only compressed the ratios.
        this.deleteEntities_batches = [.. this.entitiesInDb.Chunk(DeleteEntities_EntitiesPerOperation)];
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = DeleteEntities_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntities_Category)]
    public void DeleteEntities_Command()
    {
        using var transaction = this.connection.BeginTransaction();

        foreach (var batch in this.deleteEntities_batches)
        {
            using var command = this.connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Entity WHERE Id = @Id";

            var idParameter = command.CreateParameter();

            idParameter.ParameterName = "@Id";

            command.Parameters.Add(idParameter);

            foreach (var entity in batch)
            {
                idParameter.Value = entity.Id;

                command.ExecuteNonQuery();
            }
        }

        transaction.Rollback();
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = DeleteEntities_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntities_Category)]
    public void DeleteEntities_Dapper()
    {
        using var transaction = this.connection.BeginTransaction();

        foreach (var batch in this.deleteEntities_batches)
        {
            SqlMapperExtensions.Delete(this.connection, batch, transaction);
        }

        transaction.Rollback();
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = DeleteEntities_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntities_Category)]
    public void DeleteEntities_DbConnectionPlus()
    {
        using var transaction = this.connection.BeginTransaction();

        foreach (var batch in this.deleteEntities_batches)
        {
            this.connection.DeleteEntities(batch, transaction);
        }

        transaction.Rollback();
    }

    private List<BenchmarkEntity[]> deleteEntities_batches = null!;

    private const String DeleteEntities_Category = "DeleteEntities";
    private const Int32 DeleteEntities_EntitiesPerOperation = 250;

    // Batches per invocation: one reported operation is one delete call over
    // DeleteEntities_EntitiesPerOperation entities.
    //
    // The transaction is rolled back rather than committed, so every invocation puts the rows back. See
    // DeleteEntity_OperationsPerInvoke for why that matters and for the measurement showing a rollback costs
    // what a commit costs. Twenty batches is 5 000 seeded rows, down from 75 000, and it amortizes the
    // transaction far past the point where it could affect the ratios.
    private const Int32 DeleteEntities_OperationsPerInvoke = 20;
}
