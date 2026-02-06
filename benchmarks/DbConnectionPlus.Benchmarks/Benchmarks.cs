// @formatter:off
// ReSharper disable InconsistentNaming
// ReSharper disable InvokeAsExtensionMethod
#pragma warning disable RCS1196, IDE0017, IDE0305

using System.Data;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;
using RentADeveloper.DbConnectionPlus.UnitTests.TestData;
using System.Dynamic;
using System.Globalization;
using Dapper;
using Dapper.Contrib.Extensions;
using RentADeveloper.DbConnectionPlus.Benchmarks.DapperTypeHandlers;
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

// Note: All settings (i.e. *_EntitiesPerOperation and *_OperationsPerInvoke) are chosen so that each invoke
// takes at least 100 milliseconds to complete on a reasonably fast machine.

[MemoryDiagnoser]
[Config(typeof(BenchmarksConfig))]
public class Benchmarks
{
    static Benchmarks()
    {
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
        SqlMapper.AddTypeHandler(new TimeSpanTypeHandler());
    }

    [GlobalSetup]
    public void Setup_Global()
    {
        this.testDatabaseProvider = new();
        this.testDatabaseProvider.ResetDatabase();
    }

    private SqliteConnection CreateConnection() =>
        (SqliteConnection)this.testDatabaseProvider!.CreateConnection();

    private void PrepareEntitiesInDb(Int32 numberOfEntities)
    {
        var connection = this.CreateConnection();

        using var transaction = connection.BeginTransaction();

        connection.ExecuteNonQuery("DELETE FROM Entity", transaction);

        this.entitiesInDb = Generate.Multiple<BenchmarkEntity>(numberOfEntities);
        connection.InsertEntities(this.entitiesInDb, transaction);

        transaction.Commit();
    }

    private List<BenchmarkEntity> entitiesInDb = [];

    #region DeleteEntities
    private const String DeleteEntities_Category = "DeleteEntities";
    private const Int32 DeleteEntities_EntitiesPerOperation = 250;
    private const Int32 DeleteEntities_OperationsPerInvoke = 20;

    [IterationSetup(Targets = [nameof(DeleteEntities_DbCommand), nameof(DeleteEntities_DbConnectionPlus)])]
    public void DeleteEntities_Setup() =>
        this.PrepareEntitiesInDb(DeleteEntities_OperationsPerInvoke * DeleteEntities_EntitiesPerOperation);

    [Benchmark(Baseline = true, OperationsPerInvoke = DeleteEntities_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntities_Category)]
    public void DeleteEntities_DbCommand()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < DeleteEntities_OperationsPerInvoke; i++)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Entity WHERE Id = @Id";

            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "@Id";
            command.Parameters.Add(idParameter);

            foreach (var entity in this.entitiesInDb.Take(DeleteEntities_EntitiesPerOperation).ToList())
            {
                idParameter.Value = entity.Id;

                command.ExecuteNonQuery();

                this.entitiesInDb.Remove(entity);
            }
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = DeleteEntities_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntities_Category)]
    public void DeleteEntities_Dapper()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < DeleteEntities_OperationsPerInvoke; i++)
        {
            var entities = this.entitiesInDb.Take(DeleteEntities_EntitiesPerOperation).ToList();

            SqlMapperExtensions.Delete(connection, entities);

            foreach (var entity in entities)
            {
                this.entitiesInDb.Remove(entity);
            }
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = DeleteEntities_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntities_Category)]
    public void DeleteEntities_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < DeleteEntities_OperationsPerInvoke; i++)
        {
            var entities = this.entitiesInDb.Take(DeleteEntities_EntitiesPerOperation).ToList();

            connection.DeleteEntities(entities);

            foreach (var entity in entities)
            {
                this.entitiesInDb.Remove(entity);
            }
        }
    }
    #endregion DeleteEntities

    #region DeleteEntity
    private const String DeleteEntity_Category = "DeleteEntity";
    private const Int32 DeleteEntity_OperationsPerInvoke = 8000;

    [IterationSetup(Targets = [nameof(DeleteEntity_DbCommand), nameof(DeleteEntity_DbConnectionPlus)])]
    public void DeleteEntity_Setup() =>
        this.PrepareEntitiesInDb(DeleteEntity_OperationsPerInvoke);

    [Benchmark(Baseline = true, OperationsPerInvoke = DeleteEntity_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntity_Category)]
    public void DeleteEntity_DbCommand()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < DeleteEntity_OperationsPerInvoke; i++)
        {
            var entityToDelete = this.entitiesInDb[0];

            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Entity WHERE Id = @Id";
            command.Parameters.Add(new("@Id", entityToDelete.Id));

            command.ExecuteNonQuery();

            this.entitiesInDb.Remove(entityToDelete);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = DeleteEntity_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntity_Category)]
    public void DeleteEntity_Dapper()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < DeleteEntity_OperationsPerInvoke; i++)
        {
            var entityToDelete = this.entitiesInDb[0];

            SqlMapperExtensions.Delete(connection, entityToDelete);

            this.entitiesInDb.Remove(entityToDelete);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = DeleteEntity_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntity_Category)]
    public void DeleteEntity_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < DeleteEntity_OperationsPerInvoke; i++)
        {
            var entityToDelete = this.entitiesInDb[0];

            connection.DeleteEntity(entityToDelete);

            this.entitiesInDb.Remove(entityToDelete);
        }
    }
    #endregion DeleteEntity

    #region ExecuteNonQuery
    private const String ExecuteNonQuery_Category = "ExecuteNonQuery";
    private const Int32 ExecuteNonQuery_OperationsPerInvoke = 7700;

    [IterationSetup(Targets = [nameof(ExecuteNonQuery_DbCommand), nameof(ExecuteNonQuery_DbConnectionPlus)])]
    public void ExecuteNonQuery_Setup() =>
        this.PrepareEntitiesInDb(ExecuteNonQuery_OperationsPerInvoke);

    [Benchmark(Baseline = true, OperationsPerInvoke = ExecuteNonQuery_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteNonQuery_Category)]
    public void ExecuteNonQuery_DbCommand()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < ExecuteNonQuery_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[0];

            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM Entity WHERE Id = @Id";
            command.Parameters.Add(new("@Id", entity.Id));

            command.ExecuteNonQuery();

            this.entitiesInDb.Remove(entity);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = ExecuteNonQuery_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteNonQuery_Category)]
    public void ExecuteNonQuery_Dapper()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < ExecuteNonQuery_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[0];

            SqlMapper.Execute(connection, "DELETE FROM Entity WHERE Id = @Id", new { entity.Id });

            this.entitiesInDb.Remove(entity);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = ExecuteNonQuery_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteNonQuery_Category)]
    public void ExecuteNonQuery_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < ExecuteNonQuery_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[0];

            connection.ExecuteNonQuery($"DELETE FROM Entity WHERE Id = {Parameter(entity.Id)}");

            this.entitiesInDb.Remove(entity);
        }
    }
    #endregion ExecuteNonQuery

    #region ExecuteReader
    private const String ExecuteReader_Category = "ExecuteReader";
    private const Int32 ExecuteReader_OperationsPerInvoke = 700;
    private const Int32 ExecuteReader_EntitiesPerOperation = 100;

    [GlobalSetup(Targets = [nameof(ExecuteReader_DbCommand), nameof(ExecuteReader_DbConnectionPlus)])]
    public void ExecuteReader_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(ExecuteReader_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = ExecuteReader_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteReader_Category)]
    public List<BenchmarkEntity> ExecuteReader_DbCommand()
    {
        var connection = this.CreateConnection();

        var entities = new List<BenchmarkEntity>();

        for (var i = 0; i < ExecuteReader_OperationsPerInvoke; i++)
        {
            entities.Clear();

            using var command = connection.CreateCommand();
            command.CommandText = $"""
                                  SELECT
                                    Id,
                                    BooleanValue,
                                    BytesValue,
                                    ByteValue,
                                    CharValue,
                                    DateTimeValue,
                                    DecimalValue,
                                    DoubleValue,
                                    EnumValue,
                                    GuidValue,
                                    Int16Value,
                                    Int32Value,
                                    Int64Value,
                                    SingleValue,
                                    StringValue,
                                    TimeSpanValue
                                  FROM
                                    Entity
                                  LIMIT {ExecuteReader_EntitiesPerOperation}
                                  """;

            using var dataReader = command.ExecuteReader();

            while (dataReader.Read())
            {
                entities.Add(ReadEntity(dataReader));
            }
        }

        return entities;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = ExecuteReader_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteReader_Category)]
    public List<BenchmarkEntity> ExecuteReader_Dapper()
    {
        var connection = this.CreateConnection();

        var entities = new List<BenchmarkEntity>();

        for (var i = 0; i < ExecuteReader_OperationsPerInvoke; i++)
        {
            entities.Clear();

            using var dataReader = SqlMapper.ExecuteReader(
                connection,
                $"""
                 SELECT
                     Id,
                     BooleanValue,
                     BytesValue,
                     ByteValue,
                     CharValue,
                     DateTimeValue,
                     DecimalValue,
                     DoubleValue,
                     EnumValue,
                     GuidValue,
                     Int16Value,
                     Int32Value,
                     Int64Value,
                     SingleValue,
                     StringValue,
                     TimeSpanValue
                 FROM
                     Entity
                 LIMIT {ExecuteReader_EntitiesPerOperation}
                 """
            );

            while (dataReader.Read())
            {
                entities.Add(ReadEntity(dataReader));
            }
        }

        return entities;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = ExecuteReader_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteReader_Category)]
    public List<BenchmarkEntity> ExecuteReader_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        var entities = new List<BenchmarkEntity>();

        for (var i = 0; i < ExecuteReader_OperationsPerInvoke; i++)
        {
            entities.Clear();

            using var dataReader = connection.ExecuteReader(
                $"""
                 SELECT
                     Id,
                     BooleanValue,
                     BytesValue,
                     ByteValue,
                     CharValue,
                     DateTimeValue,
                     DecimalValue,
                     DoubleValue,
                     EnumValue,
                     GuidValue,
                     Int16Value,
                     Int32Value,
                     Int64Value,
                     SingleValue,
                     StringValue,
                     TimeSpanValue
                 FROM
                     Entity
                 LIMIT {ExecuteReader_EntitiesPerOperation}
                 """
            );

            while (dataReader.Read())
            {
                entities.Add(ReadEntity(dataReader));
            }
        }

        return entities;
    }
    #endregion ExecuteReader

    #region ExecuteScalar
    private const String ExecuteScalar_Category = "ExecuteScalar";
    private const Int32 ExecuteScalar_OperationsPerInvoke = 5000;

    [GlobalSetup(Targets = [nameof(ExecuteScalar_DbCommand), nameof(ExecuteScalar_DbConnectionPlus)])]
    public void ExecuteScalar_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(ExecuteScalar_OperationsPerInvoke);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = ExecuteScalar_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteScalar_Category)]
    public String ExecuteScalar_DbCommand()
    {
        var connection = this.CreateConnection();

        String result = null!;

        for (var i = 0; i < ExecuteScalar_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[i];

            using var command = connection.CreateCommand();

            command.CommandText = "SELECT StringValue FROM Entity WHERE Id = @Id";
            command.Parameters.Add(new("@Id", entity.Id));

            result = (String)command.ExecuteScalar()!;
        }

        return result;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = ExecuteScalar_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteScalar_Category)]
    public String ExecuteScalar_Dapper()
    {
        var connection = this.CreateConnection();

        String result = null!;

        for (var i = 0; i < ExecuteScalar_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[i];

            result = SqlMapper.ExecuteScalar<String>(
                connection,
                "SELECT StringValue FROM Entity WHERE Id = @Id",
                new { entity.Id }
            )!;
        }

        return result;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = ExecuteScalar_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteScalar_Category)]
    public String ExecuteScalar_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        String result = null!;

        for (var i = 0; i < ExecuteScalar_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[i];

            result = connection.ExecuteScalar<String>(
                $"SELECT StringValue FROM Entity WHERE Id = {Parameter(entity.Id)}"
            );
        }

        return result;
    }
    #endregion ExecuteScalar

    #region Exists
    private const String Exists_Category = "Exists";
    private const Int32 Exists_OperationsPerInvoke = 5000;

    [GlobalSetup(Targets = [nameof(Exists_DbCommand), nameof(Exists_DbConnectionPlus)])]
    public void Exists_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(Exists_OperationsPerInvoke);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Exists_OperationsPerInvoke)]
    [BenchmarkCategory(Exists_Category)]
    public Boolean Exists_DbCommand()
    {
        var connection = this.CreateConnection();

        var result = false;

        for (var i = 0; i < Exists_OperationsPerInvoke; i++)
        {
            var entityId = this.entitiesInDb[i].Id;

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM Entity WHERE Id = @Id";
            command.Parameters.Add(new("@Id", entityId));

            using var dataReader = command.ExecuteReader();

            result = dataReader.HasRows;
        }

        return result;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Exists_OperationsPerInvoke)]
    [BenchmarkCategory(Exists_Category)]
    public Boolean Exists_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        var result = false;

        for (var i = 0; i < Exists_OperationsPerInvoke; i++)
        {
            var entityId = this.entitiesInDb[i].Id;

            result = connection.Exists($"SELECT 1 FROM Entity WHERE Id = {Parameter(entityId)}");
        }

        return result;
    }
    #endregion Exists

    #region InsertEntities
    private const String InsertEntities_Category = "InsertEntities";
    private const Int32 InsertEntities_OperationsPerInvoke = 20;
    private const Int32 InsertEntities_EntitiesPerOperation = 140;

    [GlobalSetup(Targets = [nameof(InsertEntities_DbCommand), nameof(InsertEntities_DbConnectionPlus)])]
    public void InsertEntities_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(0);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = InsertEntities_OperationsPerInvoke)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_DbCommand()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < InsertEntities_OperationsPerInvoke; i++)
        {
            var entities = Generate.Multiple<BenchmarkEntity>(InsertEntities_EntitiesPerOperation);

            using var command = connection.CreateCommand();
            command.CommandText = """
                                  INSERT INTO Entity
                                  (
                                    Id,
                                    BooleanValue,
                                    BytesValue,
                                    ByteValue,
                                    CharValue,
                                    DateTimeValue,
                                    DecimalValue,
                                    DoubleValue,
                                    EnumValue,
                                    GuidValue,
                                    Int16Value,
                                    Int32Value,
                                    Int64Value,
                                    SingleValue,
                                    StringValue,
                                    TimeSpanValue
                                  )
                                  VALUES
                                  (
                                    @Id,
                                    @BooleanValue,
                                    @BytesValue,
                                    @ByteValue,
                                    @CharValue,
                                    @DateTimeValue,
                                    @DecimalValue,
                                    @DoubleValue,
                                    @EnumValue,
                                    @GuidValue,
                                    @Int16Value,
                                    @Int32Value,
                                    @Int64Value,
                                    @SingleValue,
                                    @StringValue,
                                    @TimeSpanValue
                                  )
                                  """;

            var idParameter = new SqliteParameter();
            idParameter.ParameterName = "@Id";

            var booleanValueParameter = new SqliteParameter();
            booleanValueParameter.ParameterName = "@BooleanValue";

            var bytesValueParameter = new SqliteParameter();
            bytesValueParameter.ParameterName = "@BytesValue";

            var byteValueParameter = new SqliteParameter();
            byteValueParameter.ParameterName = "@ByteValue";

            var charValueParameter = new SqliteParameter();
            charValueParameter.ParameterName = "@CharValue";

            var dateTimeValueParameter = new SqliteParameter();
            dateTimeValueParameter.ParameterName = "@DateTimeValue";

            var decimalValueParameter = new SqliteParameter();
            decimalValueParameter.ParameterName = "@DecimalValue";

            var doubleValueParameter = new SqliteParameter();
            doubleValueParameter.ParameterName = "@DoubleValue";

            var enumValueParameter = new SqliteParameter();
            enumValueParameter.ParameterName = "@EnumValue";

            var guidValueParameter = new SqliteParameter();
            guidValueParameter.ParameterName = "@GuidValue";

            var int16ValueParameter = new SqliteParameter();
            int16ValueParameter.ParameterName = "@Int16Value";

            var int32ValueParameter = new SqliteParameter();
            int32ValueParameter.ParameterName = "@Int32Value";

            var int64ValueParameter = new SqliteParameter();
            int64ValueParameter.ParameterName = "@Int64Value";

            var singleValueParameter = new SqliteParameter();
            singleValueParameter.ParameterName = "@SingleValue";

            var stringValueParameter = new SqliteParameter();
            stringValueParameter.ParameterName = "@StringValue";

            var timeSpanValueParameter = new SqliteParameter();
            timeSpanValueParameter.ParameterName = "@TimeSpanValue";

            command.Parameters.Add(idParameter);
            command.Parameters.Add(booleanValueParameter);
            command.Parameters.Add(bytesValueParameter);
            command.Parameters.Add(byteValueParameter);
            command.Parameters.Add(charValueParameter);
            command.Parameters.Add(dateTimeValueParameter);
            command.Parameters.Add(decimalValueParameter);
            command.Parameters.Add(doubleValueParameter);
            command.Parameters.Add(enumValueParameter);
            command.Parameters.Add(guidValueParameter);
            command.Parameters.Add(int16ValueParameter);
            command.Parameters.Add(int32ValueParameter);
            command.Parameters.Add(int64ValueParameter);
            command.Parameters.Add(singleValueParameter);
            command.Parameters.Add(stringValueParameter);
            command.Parameters.Add(timeSpanValueParameter);

            foreach (var entity in entities)
            {
                idParameter.Value = entity.Id;
                booleanValueParameter.Value = entity.BooleanValue ? 1 : 0;
                bytesValueParameter.Value = entity.BytesValue;
                byteValueParameter.Value = entity.ByteValue;
                charValueParameter.Value = entity.CharValue;
                dateTimeValueParameter.Value = entity.DateTimeValue.ToString(CultureInfo.InvariantCulture);
                decimalValueParameter.Value = entity.DecimalValue.ToString(CultureInfo.InvariantCulture);
                doubleValueParameter.Value = entity.DoubleValue;
                enumValueParameter.Value = entity.EnumValue.ToString();
                guidValueParameter.Value = entity.GuidValue.ToString();
                int16ValueParameter.Value = entity.Int16Value;
                int32ValueParameter.Value = entity.Int32Value;
                int64ValueParameter.Value = entity.Int64Value;
                singleValueParameter.Value = entity.SingleValue;
                stringValueParameter.Value = entity.StringValue;
                timeSpanValueParameter.Value = entity.TimeSpanValue.ToString();

                command.ExecuteNonQuery();
            }
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = InsertEntities_OperationsPerInvoke)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_Dapper()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < InsertEntities_OperationsPerInvoke; i++)
        {
            var entitiesToInsert = Generate.Multiple<BenchmarkEntity>(InsertEntities_EntitiesPerOperation);

            SqlMapperExtensions.Insert(connection, entitiesToInsert);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = InsertEntities_OperationsPerInvoke)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < InsertEntities_OperationsPerInvoke; i++)
        {
            var entitiesToInsert = Generate.Multiple<BenchmarkEntity>(InsertEntities_EntitiesPerOperation);

            connection.InsertEntities(entitiesToInsert);
        }
    }
    #endregion InsertEntities

    #region InsertEntity
    private const String InsertEntity_Category = "InsertEntity";
    private const Int32 InsertEntity_OperationsPerInvoke = 2500;

    [GlobalSetup(Targets = [nameof(InsertEntity_DbCommand), nameof(InsertEntity_DbConnectionPlus)])]
    public void InsertEntity_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(0);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = InsertEntity_OperationsPerInvoke)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_DbCommand()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < InsertEntity_OperationsPerInvoke; i++)
        {
            var entity = Generate.Single<BenchmarkEntity>();

            using var command = connection.CreateCommand();
            command.CommandText = """
                                  INSERT INTO Entity
                                  (
                                    Id,
                                    BooleanValue,
                                    BytesValue,
                                    ByteValue,
                                    CharValue,
                                    DateTimeValue,
                                    DecimalValue,
                                    DoubleValue,
                                    EnumValue,
                                    GuidValue,
                                    Int16Value,
                                    Int32Value,
                                    Int64Value,
                                    SingleValue,
                                    StringValue,
                                    TimeSpanValue
                                  )
                                  VALUES
                                  (
                                    @Id,
                                    @BooleanValue,
                                    @BytesValue,
                                    @ByteValue,
                                    @CharValue,
                                    @DateTimeValue,
                                    @DecimalValue,
                                    @DoubleValue,
                                    @EnumValue,
                                    @GuidValue,
                                    @Int16Value,
                                    @Int32Value,
                                    @Int64Value,
                                    @SingleValue,
                                    @StringValue,
                                    @TimeSpanValue
                                  )
                                  """;
            command.Parameters.Add(new("@Id", entity.Id));
            command.Parameters.Add(new("@BooleanValue", entity.BooleanValue ? 1 : 0));
            command.Parameters.Add(new("@BytesValue", entity.BytesValue));
            command.Parameters.Add(new("@ByteValue", entity.ByteValue));
            command.Parameters.Add(new("@CharValue", entity.CharValue));
            command.Parameters.Add(new("@DateTimeValue", entity.DateTimeValue.ToString(CultureInfo.InvariantCulture)));
            command.Parameters.Add(new("@DecimalValue", entity.DecimalValue));
            command.Parameters.Add(new("@DoubleValue", entity.DoubleValue));
            command.Parameters.Add(new("@EnumValue", entity.EnumValue.ToString()));
            command.Parameters.Add(new("@GuidValue", entity.GuidValue.ToString()));
            command.Parameters.Add(new("@Int16Value", entity.Int16Value));
            command.Parameters.Add(new("@Int32Value", entity.Int32Value));
            command.Parameters.Add(new("@Int64Value", entity.Int64Value));
            command.Parameters.Add(new("@SingleValue", entity.SingleValue));
            command.Parameters.Add(new("@StringValue", entity.StringValue));
            command.Parameters.Add(new("@TimeSpanValue", entity.TimeSpanValue.ToString()));

            command.ExecuteNonQuery();
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = InsertEntity_OperationsPerInvoke)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_Dapper()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < InsertEntity_OperationsPerInvoke; i++)
        {
            var entity = Generate.Single<BenchmarkEntity>();

            SqlMapperExtensions.Insert(connection, entity);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = InsertEntity_OperationsPerInvoke)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < InsertEntity_OperationsPerInvoke; i++)
        {
            var entity = Generate.Single<BenchmarkEntity>();

            connection.InsertEntity(entity);
        }
    }
    #endregion InsertEntity

    #region Parameter
    private const String Parameter_Category = "Parameter";
    private const Int32 Parameter_OperationsPerInvoke = 35_000;

    [GlobalSetup(Targets = [nameof(Parameter_DbCommand), nameof(Parameter_DbConnectionPlus)])]
    public void Parameter_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(0);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Parameter_OperationsPerInvoke)]
    [BenchmarkCategory(Parameter_Category)]
    public Object Parameter_DbCommand()
    {
        var connection = this.CreateConnection();

        var result = new List<Object>();

        for (var i = 0; i < Parameter_OperationsPerInvoke; i++)
        {
            result.Clear();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT @P1, @P2, @P3, @P4, @P5";
            command.Parameters.Add(new("@P1", 1));
            command.Parameters.Add(new("@P2", "Test"));
            command.Parameters.Add(new("@P3", DateTime.UtcNow));
            command.Parameters.Add(new("@P4", Guid.NewGuid()));
            command.Parameters.Add(new("@P5", true));

            using var dataReader = command.ExecuteReader();

            dataReader.Read();
            
            result.Add((Int32) dataReader.GetInt64(0));
            result.Add(dataReader.GetString(1));
            result.Add(dataReader.GetDateTime(2));
            result.Add(dataReader.GetGuid(3));
            result.Add(dataReader.GetBoolean(4));
        }

        return result;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Parameter_OperationsPerInvoke)]
    [BenchmarkCategory(Parameter_Category)]
    public Object Parameter_Dapper()
    {
        var connection = this.CreateConnection();

        var result = new List<Object>();

        for (var i = 0; i < Parameter_OperationsPerInvoke; i++)
        {
            result.Clear();

            using var dataReader = SqlMapper.ExecuteReader(
                connection,
                "SELECT @P1, @P2, @P3, @P4, @P5",
                new
                {
                    P1 = 1,
                    P2 = "Test",
                    P3 = DateTime.UtcNow,
                    P4 = Guid.NewGuid(),
                    P5 = true
                }
            );

            dataReader.Read();
            
            result.Add((Int32) dataReader.GetInt64(0));
            result.Add(dataReader.GetString(1));
            result.Add(dataReader.GetDateTime(2));
            result.Add(dataReader.GetGuid(3));
            result.Add(dataReader.GetBoolean(4));
        }

        return result;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Parameter_OperationsPerInvoke)]
    [BenchmarkCategory(Parameter_Category)]
    public Object Parameter_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        var result = new List<Object>();

        for (var i = 0; i < Parameter_OperationsPerInvoke; i++)
        {
            result.Clear();

            using var dataReader = connection.ExecuteReader(
                $"""
                 SELECT {Parameter(1)},
                        {Parameter("Test")},
                        {Parameter(DateTime.UtcNow)},
                        {Parameter(Guid.NewGuid())},
                        {Parameter(true)}
                 """);

            dataReader.Read();
            
            result.Add((Int32) dataReader.GetInt64(0));
            result.Add(dataReader.GetString(1));
            result.Add(dataReader.GetDateTime(2));
            result.Add(dataReader.GetGuid(3));
            result.Add(dataReader.GetBoolean(4));
        }

        return result;
    }
    #endregion Parameter

    #region Query_Dynamic
    private const String Query_Dynamic_Category = "Query_Dynamic";
    private const Int32 Query_Dynamic_OperationsPerInvoke = 600;
    private const Int32 Query_Dynamic_EntitiesPerOperation = 100;

    [GlobalSetup(Targets = [nameof(Query_Dynamic_DbCommand), nameof(Query_Dynamic_DbConnectionPlus)])]
    public void Query_Dynamic_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(Query_Dynamic_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Query_Dynamic_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_DbCommand()
    {
        var connection = this.CreateConnection();

        var entities = new List<dynamic>();

        for (var i = 0; i < Query_Dynamic_OperationsPerInvoke; i++)
        {
            entities.Clear();

            using var dataReader = connection.ExecuteReader(
                $"""
                 SELECT
                     Id,
                     BooleanValue,
                     BytesValue,
                     ByteValue,
                     CharValue,
                     DateTimeValue,
                     DecimalValue,
                     DoubleValue,
                     EnumValue,
                     GuidValue,
                     Int16Value,
                     Int32Value,
                     Int64Value,
                     SingleValue,
                     StringValue,
                     TimeSpanValue
                 FROM
                     Entity
                 LIMIT {Query_Dynamic_EntitiesPerOperation}
                 """
            );

            while (dataReader.Read())
            {
                var charBuffer = new Char[1];

                var ordinal = 0;
                dynamic entity = new ExpandoObject();

                entity.Id = dataReader.GetInt64(ordinal++);
                entity.BooleanValue = dataReader.GetInt64(ordinal++) == 1;
                entity.BytesValue = (Byte[])dataReader.GetValue(ordinal++);
                entity.ByteValue = dataReader.GetByte(ordinal++);
                entity.CharValue = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1 
                    ? charBuffer[0] 
                    : throw new();
                entity.DateTimeValue = DateTime.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture);
                entity.DecimalValue = Decimal.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture);
                entity.DoubleValue = dataReader.GetDouble(ordinal++);
                entity.EnumValue = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++));
                entity.GuidValue = Guid.Parse(dataReader.GetString(ordinal++));
                entity.Int16Value = (Int16) dataReader.GetInt64(ordinal++);
                entity.Int32Value = (Int32) dataReader.GetInt64(ordinal++);
                entity.Int64Value = dataReader.GetInt64(ordinal++);
                entity.SingleValue = dataReader.GetFloat(ordinal++);
                entity.StringValue = dataReader.GetString(ordinal++);
                entity.TimeSpanValue = TimeSpan.Parse(dataReader.GetString(ordinal), CultureInfo.InvariantCulture);

                entities.Add(entity);
            }
        }

        return entities;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Query_Dynamic_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        List<dynamic> entities = [];

        for (var i = 0; i < Query_Dynamic_OperationsPerInvoke; i++)
        {
            entities = connection
                .Query($"SELECT * FROM Entity LIMIT {Query_Dynamic_EntitiesPerOperation}")
                .ToList();
        }

        return entities;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Query_Dynamic_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_Dapper()
    {
        var connection = this.CreateConnection();

        List<dynamic> entities = [];

        for (var i = 0; i < Query_Dynamic_OperationsPerInvoke; i++)
        {
            entities = SqlMapper.Query(connection, $"SELECT * FROM Entity LIMIT {Query_Dynamic_EntitiesPerOperation}")
                .ToList();
        }

        return entities;
    }
    #endregion Query_Dynamic

    #region Query_Scalars
    private const String Query_Scalars_Category = "Query_Scalars";
    private const Int32 Query_Scalars_OperationsPerInvoke = 1500;
    private const Int32 Query_Scalars_EntitiesPerOperation = 500;

    [GlobalSetup(Targets = [nameof(Query_Scalars_DbCommand), nameof(Query_Scalars_DbConnectionPlus)])]
    public void Query_Scalars_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(Query_Scalars_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Query_Scalars_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Scalars_Category)]
    public List<Int64> Query_Scalars_DbCommand()
    {
        var connection = this.CreateConnection();

        var data = new List<Int64>();

        for (var i = 0; i < Query_Scalars_OperationsPerInvoke; i++)
        {
            data.Clear();

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT Id FROM Entity LIMIT {Query_Scalars_EntitiesPerOperation}";

            using var dataReader = command.ExecuteReader();

            while (dataReader.Read())
            {
                var id = dataReader.GetInt64(0);

                data.Add(id);
            }
        }

        return data;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Query_Scalars_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Scalars_Category)]
    public List<Int64> Query_Scalars_Dapper()
    {
        var connection = this.CreateConnection();

        List<Int64> data = [];

        for (var i = 0; i < Query_Scalars_OperationsPerInvoke; i++)
        {
            data = SqlMapper.Query<Int64>(
                    connection,
                    $"SELECT Id FROM Entity LIMIT {Query_Scalars_EntitiesPerOperation}"
                )
                .ToList();
        }

        return data;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Query_Scalars_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Scalars_Category)]
    public List<Int64> Query_Scalars_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        List<Int64> data = [];

        for (var i = 0; i < Query_Scalars_OperationsPerInvoke; i++)
        {
            data = connection
                .Query<Int64>($"SELECT Id FROM Entity LIMIT {Query_Scalars_EntitiesPerOperation}")
                .ToList();
        }

        return data;
    }
    #endregion Query_Scalars

    #region Query_Entities
    private const String Query_Entities_Category = "Query_Entities";
    private const Int32 Query_Entities_OperationsPerInvoke = 600;
    private const Int32 Query_Entities_EntitiesPerOperation = 100;

    [GlobalSetup(Targets = [nameof(Query_Entities_DbCommand), nameof(Query_Entities_DbConnectionPlus)])]
    public void Query_Entities_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(Query_Entities_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Query_Entities_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Entities_Category)]
    public List<BenchmarkEntity> Query_Entities_DbCommand()
    {
        var connection = this.CreateConnection();

        var entities = new List<BenchmarkEntity>();

        for (var i = 0; i < Query_Entities_OperationsPerInvoke; i++)
        {
            entities.Clear();

            using var dataReader = connection.ExecuteReader(
                $"""
                 SELECT
                     Id,
                     BooleanValue,
                     BytesValue,
                     ByteValue,
                     CharValue,
                     DateTimeValue,
                     DecimalValue,
                     DoubleValue,
                     EnumValue,
                     GuidValue,
                     Int16Value,
                     Int32Value,
                     Int64Value,
                     SingleValue,
                     StringValue,
                     TimeSpanValue
                 FROM
                     Entity
                 LIMIT {Query_Entities_EntitiesPerOperation}
                 """
            );

            while (dataReader.Read())
            {
                entities.Add(ReadEntity(dataReader));
            }
        }

        return entities;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Query_Entities_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Entities_Category)]
    public List<BenchmarkEntity> Query_Entities_Dapper()
    {
        var connection = this.CreateConnection();

        List<BenchmarkEntity> entities = [];

        for (var i = 0; i < Query_Entities_OperationsPerInvoke; i++)
        {
            entities = SqlMapper
                .Query<BenchmarkEntity>(
                    connection,
                    $"SELECT * FROM Entity LIMIT {Query_Entities_EntitiesPerOperation}"
                )
                .ToList();
        }

        return entities;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Query_Entities_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Entities_Category)]
    public List<BenchmarkEntity> Query_Entities_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        List<BenchmarkEntity> entities = [];

        for (var i = 0; i < Query_Entities_OperationsPerInvoke; i++)
        {
            entities = connection
                .Query<BenchmarkEntity>($"SELECT * FROM Entity LIMIT {Query_Entities_EntitiesPerOperation}")
                .ToList();
        }

        return entities;
    }
    #endregion Query_Entities

    #region Query_ValueTuples
    private const String Query_ValueTuples_Category = "Query_ValueTuples";
    private const Int32 Query_ValueTuples_OperationsPerInvoke = 1_000;
    private const Int32 Query_ValueTuples_EntitiesPerOperation = 150;

    [GlobalSetup(Targets = [nameof(Query_ValueTuples_DbCommand), nameof(Query_ValueTuples_DbConnectionPlus)])]
    public void Query_ValueTuples_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(Query_ValueTuples_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Query_ValueTuples_OperationsPerInvoke)]
    [BenchmarkCategory(Query_ValueTuples_Category)]
    public List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)> Query_ValueTuples_DbCommand()
    {
        var connection = this.CreateConnection();

        var tuples = new List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>();

        for (var i = 0; i < Query_ValueTuples_OperationsPerInvoke; i++)
        {
            tuples.Clear();

            using var command = connection.CreateCommand();
            command.CommandText = $"""
                                   SELECT   Id, DateTimeValue, EnumValue, StringValue
                                   FROM     Entity
                                   LIMIT    {Query_ValueTuples_EntitiesPerOperation}
                                   """;

            using var dataReader = command.ExecuteReader();

            while (dataReader.Read())
            {
                tuples.Add(
                    (
                        dataReader.GetInt64(0),
                        DateTime.Parse(dataReader.GetString(1), CultureInfo.InvariantCulture),
                        Enum.Parse<TestEnum>(dataReader.GetString(2)),
                        dataReader.GetString(3)
                    )
                );
            }
        }

        return tuples;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Query_ValueTuples_OperationsPerInvoke)]
    [BenchmarkCategory(Query_ValueTuples_Category)]
    public List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>
        Query_ValueTuples_Dapper()
    {
        var connection = this.CreateConnection();

        List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)> tuples = [];

        for (var i = 0; i < Query_ValueTuples_OperationsPerInvoke; i++)
        {
            tuples = SqlMapper
                .Query<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>(
                    connection,
                    $"""
                     SELECT   Id, DateTimeValue, EnumValue, StringValue
                     FROM     Entity
                     LIMIT    {Query_ValueTuples_EntitiesPerOperation}
                     """
                )
                .ToList();
        }

        return tuples;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Query_ValueTuples_OperationsPerInvoke)]
    [BenchmarkCategory(Query_ValueTuples_Category)]
    public List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>
        Query_ValueTuples_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)> tuples = [];

        for (var i = 0; i < Query_ValueTuples_OperationsPerInvoke; i++)
        {
            tuples = connection
                .Query<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>(
                    $"""
                     SELECT   Id, DateTimeValue, EnumValue, StringValue
                     FROM     Entity
                     LIMIT    {Query_ValueTuples_EntitiesPerOperation}
                     """
                )
                .ToList();
        }

        return tuples;
    }
    #endregion Query_ValueTuples

    #region TemporaryTable_ComplexObjects
    private const String TemporaryTable_ComplexObjects_Category = "TemporaryTable_ComplexObjects";
    private const Int32 TemporaryTable_ComplexObjects_OperationsPerInvoke = 50;
    private const Int32 TemporaryTable_ComplexObjects_EntitiesPerOperation = 200;

    [GlobalSetup(Targets = [
        nameof(TemporaryTable_ComplexObjects_DbCommand),
        nameof(TemporaryTable_ComplexObjects_DbConnectionPlus)
    ])]
    public void TemporaryTable_ComplexObjects_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(0);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = TemporaryTable_ComplexObjects_OperationsPerInvoke)]
    [BenchmarkCategory(TemporaryTable_ComplexObjects_Category)]
    public List<BenchmarkEntity> TemporaryTable_ComplexObjects_DbCommand()
    {
        var connection = this.CreateConnection();

        var entities = Generate.Multiple<BenchmarkEntity>(TemporaryTable_ComplexObjects_EntitiesPerOperation);

        var result = new List<BenchmarkEntity>();

        for (var i = 0; i < TemporaryTable_ComplexObjects_OperationsPerInvoke; i++)
        {
            result.Clear();

            using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText =
                """
                 CREATE TEMP TABLE Entities (
                     Id INTEGER,
                     BytesValue BLOB,
                     BooleanValue INTEGER,
                     ByteValue INTEGER,
                     CharValue TEXT,
                     DateTimeValue TEXT,
                     DecimalValue TEXT,
                     DoubleValue REAL,
                     EnumValue TEXT,
                     GuidValue TEXT,
                     Int16Value INTEGER,
                     Int32Value INTEGER,
                     Int64Value INTEGER,
                     SingleValue REAL,
                     StringValue TEXT,
                     TimeSpanValue TEXT
                 )
                 """;
            createTableCommand.ExecuteNonQuery();

            using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = 
            """
            INSERT INTO temp.Entities (
                Id,
                BooleanValue,
                BytesValue,
                ByteValue,
                CharValue,
                DateTimeValue,
                DecimalValue,
                DoubleValue,
                EnumValue,
                GuidValue,
                Int16Value,
                Int32Value,
                Int64Value,
                SingleValue,
                StringValue,
                TimeSpanValue
            )
            VALUES (
                @Id,
                @BooleanValue,
                @BytesValue,
                @ByteValue,
                @CharValue,
                @DateTimeValue,
                @DecimalValue,
                @DoubleValue,
                @EnumValue,
                @GuidValue,
                @Int16Value,
                @Int32Value,
                @Int64Value,
                @SingleValue,
                @StringValue,
                @TimeSpanValue
            )
            """;

            var idParameter = new SqliteParameter();
            idParameter.ParameterName = "@Id";

            var booleanValueParameter = new SqliteParameter();
            booleanValueParameter.ParameterName = "@BooleanValue";

            var bytesValueParameter = new SqliteParameter();
            bytesValueParameter.ParameterName = "@BytesValue";

            var byteValueParameter = new SqliteParameter();
            byteValueParameter.ParameterName = "@ByteValue";

            var charValueParameter = new SqliteParameter();
            charValueParameter.ParameterName = "@CharValue";

            var dateTimeValueParameter = new SqliteParameter();
            dateTimeValueParameter.ParameterName = "@DateTimeValue";

            var decimalValueParameter = new SqliteParameter();
            decimalValueParameter.ParameterName = "@DecimalValue";

            var doubleValueParameter = new SqliteParameter();
            doubleValueParameter.ParameterName = "@DoubleValue";

            var enumValueParameter = new SqliteParameter();
            enumValueParameter.ParameterName = "@EnumValue";

            var guidValueParameter = new SqliteParameter();
            guidValueParameter.ParameterName = "@GuidValue";

            var int16ValueParameter = new SqliteParameter();
            int16ValueParameter.ParameterName = "@Int16Value";

            var int32ValueParameter = new SqliteParameter();
            int32ValueParameter.ParameterName = "@Int32Value";

            var int64ValueParameter = new SqliteParameter();
            int64ValueParameter.ParameterName = "@Int64Value";

            var singleValueParameter = new SqliteParameter();
            singleValueParameter.ParameterName = "@SingleValue";

            var stringValueParameter = new SqliteParameter();
            stringValueParameter.ParameterName = "@StringValue";

            var timeSpanValueParameter = new SqliteParameter();
            timeSpanValueParameter.ParameterName = "@TimeSpanValue";

            insertCommand.Parameters.Add(idParameter);
            insertCommand.Parameters.Add(booleanValueParameter);
            insertCommand.Parameters.Add(bytesValueParameter);
            insertCommand.Parameters.Add(byteValueParameter);
            insertCommand.Parameters.Add(charValueParameter);
            insertCommand.Parameters.Add(dateTimeValueParameter);
            insertCommand.Parameters.Add(decimalValueParameter);
            insertCommand.Parameters.Add(doubleValueParameter);
            insertCommand.Parameters.Add(enumValueParameter);
            insertCommand.Parameters.Add(guidValueParameter);
            insertCommand.Parameters.Add(int16ValueParameter);
            insertCommand.Parameters.Add(int32ValueParameter);
            insertCommand.Parameters.Add(int64ValueParameter);
            insertCommand.Parameters.Add(singleValueParameter);
            insertCommand.Parameters.Add(stringValueParameter);
            insertCommand.Parameters.Add(timeSpanValueParameter);

            foreach (var entity in entities)
            {
                idParameter.Value = entity.Id;
                booleanValueParameter.Value = entity.BooleanValue ? 1 : 0;
                bytesValueParameter.Value = entity.BytesValue;
                byteValueParameter.Value = entity.ByteValue;
                charValueParameter.Value = entity.CharValue;
                dateTimeValueParameter.Value = entity.DateTimeValue.ToString(CultureInfo.InvariantCulture);
                decimalValueParameter.Value = entity.DecimalValue.ToString(CultureInfo.InvariantCulture);
                doubleValueParameter.Value = entity.DoubleValue;
                enumValueParameter.Value = entity.EnumValue.ToString();
                guidValueParameter.Value = entity.GuidValue.ToString();
                int16ValueParameter.Value = entity.Int16Value;
                int32ValueParameter.Value = entity.Int32Value;
                int64ValueParameter.Value = entity.Int64Value;
                singleValueParameter.Value = entity.SingleValue;
                stringValueParameter.Value = entity.StringValue;
                timeSpanValueParameter.Value = entity.TimeSpanValue.ToString();

                insertCommand.ExecuteNonQuery();
            }

            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText =
                """
                SELECT
                    Id,
                    BooleanValue,
                    BytesValue,
                    ByteValue,
                    CharValue,
                    DateTimeValue,
                    DecimalValue,
                    DoubleValue,
                    EnumValue,
                    GuidValue,
                    Int16Value,
                    Int32Value,
                    Int64Value,
                    SingleValue,
                    StringValue,
                    TimeSpanValue
                FROM
                    temp.Entities
                """;

            using var dataReader = selectCommand.ExecuteReader();

            while (dataReader.Read())
            {
                result.Add(ReadEntity(dataReader));
            }

            using var dropTableCommand = connection.CreateCommand();
            dropTableCommand.CommandText = "DROP TABLE temp.Entities";
            dropTableCommand.ExecuteNonQuery();
        }

        return result;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = TemporaryTable_ComplexObjects_OperationsPerInvoke)]
    [BenchmarkCategory(TemporaryTable_ComplexObjects_Category)]
    public List<BenchmarkEntity> TemporaryTable_ComplexObjects_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        var entities = Generate.Multiple<BenchmarkEntity>(TemporaryTable_ComplexObjects_EntitiesPerOperation);

        List<BenchmarkEntity> result = [];

        for (var i = 0; i < TemporaryTable_ComplexObjects_OperationsPerInvoke; i++)
        {
            result = connection.Query<BenchmarkEntity>($"SELECT * FROM {TemporaryTable(entities)}").ToList();
        }

        return result;
    }
    #endregion TemporaryTable_ComplexObjects

    #region TemporaryTable_ScalarValues
    private const String TemporaryTable_ScalarValues_Category = "TemporaryTable_ScalarValues";
    private const Int32 TemporaryTable_ScalarValues_OperationsPerInvoke = 30;
    private const Int32 TemporaryTable_ScalarValues_ValuesPerOperation = 5000;

    [GlobalSetup(Targets = [
        nameof(TemporaryTable_ScalarValues_DbCommand),
        nameof(TemporaryTable_ScalarValues_DbConnectionPlus)
    ])]
    public void TemporaryTable_ScalarValues_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(0);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = TemporaryTable_ScalarValues_OperationsPerInvoke)]
    [BenchmarkCategory(TemporaryTable_ScalarValues_Category)]
    public List<String> TemporaryTable_ScalarValues_DbCommand()
    {
        var connection = this.CreateConnection();

        var scalarValues = Enumerable
            .Range(0, TemporaryTable_ScalarValues_ValuesPerOperation)
            .Select(a => a.ToString(CultureInfo.InvariantCulture))
            .ToList();

        var result = new List<String>();

        for (var i = 0; i < TemporaryTable_ScalarValues_OperationsPerInvoke; i++)
        {
            result.Clear();

            using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = "CREATE TEMP TABLE \"Values\" (Value TEXT)";
            createTableCommand.ExecuteNonQuery();

            using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO temp.\"Values\" (Value) VALUES (@Value)";

            var valueParameter = new SqliteParameter();
            valueParameter.ParameterName = "@Value";
    
            insertCommand.Parameters.Add(valueParameter);
    
            foreach (var value in scalarValues)
            {
                valueParameter.Value = value;
                
                insertCommand.ExecuteNonQuery();
            }

            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT Value FROM temp.\"Values\"";

            using var dataReader = selectCommand.ExecuteReader();

            while (dataReader.Read())
            {
                result.Add(dataReader.GetString(0));
            }

            using var dropTableCommand = connection.CreateCommand();
            dropTableCommand.CommandText = "DROP TABLE temp.\"Values\"";
            dropTableCommand.ExecuteNonQuery();
        }

        return result;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = TemporaryTable_ScalarValues_OperationsPerInvoke)]
    [BenchmarkCategory(TemporaryTable_ScalarValues_Category)]
    public List<String> TemporaryTable_ScalarValues_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        var scalarValues = Enumerable
            .Range(0, TemporaryTable_ScalarValues_ValuesPerOperation)
            .Select(a => a.ToString(CultureInfo.InvariantCulture))
            .ToList();

        List<String> result = [];

        for (var i = 0; i < TemporaryTable_ScalarValues_OperationsPerInvoke; i++)
        {
            result = connection.Query<String>($"SELECT Value FROM {TemporaryTable(scalarValues)}").ToList();
        }

        return result;
    }
    #endregion TemporaryTable_ScalarValues

    #region UpdateEntities
    private const String UpdateEntities_Category = "UpdateEntities";
    private const Int32 UpdateEntities_OperationsPerInvoke = 25;
    private const Int32 UpdateEntities_EntitiesPerOperation = 100;

    [GlobalSetup(Targets = [
        nameof(UpdateEntities_DbCommand),
        nameof(UpdateEntities_DbConnectionPlus)
    ])]
    public void UpdateEntities_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(UpdateEntities_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = UpdateEntities_OperationsPerInvoke)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_DbCommand()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < UpdateEntities_OperationsPerInvoke; i++)
        {
            var updatedEntities = Generate.UpdateFor(this.entitiesInDb);

            using var command = connection.CreateCommand();
            command.CommandText = """
                                  UPDATE    Entity
                                  SET       BooleanValue = @BooleanValue,
                                            BytesValue = @BytesValue,
                                            ByteValue = @ByteValue,
                                            CharValue = @CharValue,
                                            DateTimeValue = @DateTimeValue,
                                            DecimalValue = @DecimalValue,
                                            DoubleValue = @DoubleValue,
                                            EnumValue = @EnumValue,
                                            GuidValue = @GuidValue,
                                            Int16Value = @Int16Value,
                                            Int32Value = @Int32Value,
                                            Int64Value = @Int64Value,
                                            SingleValue = @SingleValue,
                                            StringValue = @StringValue,
                                            TimeSpanValue = @TimeSpanValue
                                  WHERE     Id = @Id
                                  """;

            var idParameter = new SqliteParameter();
            idParameter.ParameterName = "@Id";

            var booleanValueParameter = new SqliteParameter();
            booleanValueParameter.ParameterName = "@BooleanValue";

            var bytesValueParameter = new SqliteParameter();
            bytesValueParameter.ParameterName = "@BytesValue";

            var byteValueParameter = new SqliteParameter();
            byteValueParameter.ParameterName = "@ByteValue";

            var charValueParameter = new SqliteParameter();
            charValueParameter.ParameterName = "@CharValue";

            var dateTimeValueParameter = new SqliteParameter();
            dateTimeValueParameter.ParameterName = "@DateTimeValue";

            var decimalValueParameter = new SqliteParameter();
            decimalValueParameter.ParameterName = "@DecimalValue";

            var doubleValueParameter = new SqliteParameter();
            doubleValueParameter.ParameterName = "@DoubleValue";

            var enumValueParameter = new SqliteParameter();
            enumValueParameter.ParameterName = "@EnumValue";

            var guidValueParameter = new SqliteParameter();
            guidValueParameter.ParameterName = "@GuidValue";

            var int16ValueParameter = new SqliteParameter();
            int16ValueParameter.ParameterName = "@Int16Value";

            var int32ValueParameter = new SqliteParameter();
            int32ValueParameter.ParameterName = "@Int32Value";

            var int64ValueParameter = new SqliteParameter();
            int64ValueParameter.ParameterName = "@Int64Value";

            var singleValueParameter = new SqliteParameter();
            singleValueParameter.ParameterName = "@SingleValue";

            var stringValueParameter = new SqliteParameter();
            stringValueParameter.ParameterName = "@StringValue";

            var timeSpanValueParameter = new SqliteParameter();
            timeSpanValueParameter.ParameterName = "@TimeSpanValue";

            command.Parameters.Add(idParameter);
            command.Parameters.Add(booleanValueParameter);
            command.Parameters.Add(bytesValueParameter);
            command.Parameters.Add(byteValueParameter);
            command.Parameters.Add(charValueParameter);
            command.Parameters.Add(dateTimeValueParameter);
            command.Parameters.Add(decimalValueParameter);
            command.Parameters.Add(doubleValueParameter);
            command.Parameters.Add(enumValueParameter);
            command.Parameters.Add(guidValueParameter);
            command.Parameters.Add(int16ValueParameter);
            command.Parameters.Add(int32ValueParameter);
            command.Parameters.Add(int64ValueParameter);
            command.Parameters.Add(singleValueParameter);
            command.Parameters.Add(stringValueParameter);
            command.Parameters.Add(timeSpanValueParameter);

            foreach (var updatedEntity in updatedEntities)
            {
                idParameter.Value = updatedEntity.Id;
                booleanValueParameter.Value = updatedEntity.BooleanValue ? 1 : 0;
                bytesValueParameter.Value = updatedEntity.BytesValue;
                byteValueParameter.Value = updatedEntity.ByteValue;
                charValueParameter.Value = updatedEntity.CharValue;
                dateTimeValueParameter.Value = updatedEntity.DateTimeValue.ToString(CultureInfo.InvariantCulture);
                decimalValueParameter.Value = updatedEntity.DecimalValue.ToString(CultureInfo.InvariantCulture);
                doubleValueParameter.Value = updatedEntity.DoubleValue;
                enumValueParameter.Value = updatedEntity.EnumValue.ToString();
                guidValueParameter.Value = updatedEntity.GuidValue.ToString();
                int16ValueParameter.Value = updatedEntity.Int16Value;
                int32ValueParameter.Value = updatedEntity.Int32Value;
                int64ValueParameter.Value = updatedEntity.Int64Value;
                singleValueParameter.Value = updatedEntity.SingleValue;
                stringValueParameter.Value = updatedEntity.StringValue;
                timeSpanValueParameter.Value = updatedEntity.TimeSpanValue.ToString();

                command.ExecuteNonQuery();
            }
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = UpdateEntities_OperationsPerInvoke)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_Dapper()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < UpdateEntities_OperationsPerInvoke; i++)
        {
            var updatesEntities = Generate.UpdateFor(this.entitiesInDb);

            SqlMapperExtensions.Update(connection, updatesEntities);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = UpdateEntities_OperationsPerInvoke)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < UpdateEntities_OperationsPerInvoke; i++)
        {
            var updatesEntities = Generate.UpdateFor(this.entitiesInDb);

            connection.UpdateEntities(updatesEntities);
        }
    }
    #endregion UpdateEntities

    #region UpdateEntity
    private const String UpdateEntity_Category = "UpdateEntity";
    private const Int32 UpdateEntity_OperationsPerInvoke = 1_600;

    [GlobalSetup(Targets = [
        nameof(UpdateEntity_DbCommand),
        nameof(UpdateEntity_DbConnectionPlus)
    ])]
    public void UpdateEntity_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(UpdateEntity_OperationsPerInvoke);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = UpdateEntity_OperationsPerInvoke)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_DbCommand()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < UpdateEntity_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[i];

            var updatedEntity = Generate.UpdateFor(entity);

            using var command = connection.CreateCommand();
            command.CommandText = """
                                  UPDATE    Entity
                                  SET       BooleanValue = @BooleanValue,
                                            BytesValue = @BytesValue,
                                            ByteValue = @ByteValue,
                                            CharValue = @CharValue,
                                            DateTimeValue = @DateTimeValue,
                                            DecimalValue = @DecimalValue,
                                            DoubleValue = @DoubleValue,
                                            EnumValue = @EnumValue,
                                            GuidValue = @GuidValue,
                                            Int16Value = @Int16Value,
                                            Int32Value = @Int32Value,
                                            Int64Value = @Int64Value,
                                            SingleValue = @SingleValue,
                                            StringValue = @StringValue,
                                            TimeSpanValue = @TimeSpanValue
                                  WHERE     Id = @Id
                                  """;
            command.Parameters.Add(new("@Id", updatedEntity.Id));
            command.Parameters.Add(new("@BooleanValue", updatedEntity.BooleanValue ? 1 :0));
            command.Parameters.Add(new("@BytesValue", updatedEntity.BytesValue));
            command.Parameters.Add(new("@ByteValue", updatedEntity.ByteValue));
            command.Parameters.Add(new("@CharValue", updatedEntity.CharValue));
            command.Parameters.Add(new("@DateTimeValue", updatedEntity.DateTimeValue.ToString(CultureInfo.InvariantCulture)));
            command.Parameters.Add(new("@DecimalValue", updatedEntity.DecimalValue.ToString(CultureInfo.InvariantCulture)));
            command.Parameters.Add(new("@DoubleValue", updatedEntity.DoubleValue));
            command.Parameters.Add(new("@EnumValue", updatedEntity.EnumValue.ToString()));
            command.Parameters.Add(new("@GuidValue", updatedEntity.GuidValue.ToString()));
            command.Parameters.Add(new("@Int16Value", updatedEntity.Int16Value));
            command.Parameters.Add(new("@Int32Value", updatedEntity.Int32Value));
            command.Parameters.Add(new("@Int64Value", updatedEntity.Int64Value));
            command.Parameters.Add(new("@SingleValue", updatedEntity.SingleValue));
            command.Parameters.Add(new("@StringValue", updatedEntity.StringValue));
            command.Parameters.Add(new("@TimeSpanValue", updatedEntity.TimeSpanValue.ToString()));

            command.ExecuteNonQuery();
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = UpdateEntity_OperationsPerInvoke)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_Dapper()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < UpdateEntity_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[i];

            var updatedEntity = Generate.UpdateFor(entity);

            SqlMapperExtensions.Update(connection, updatedEntity);
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = UpdateEntity_OperationsPerInvoke)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_DbConnectionPlus()
    {
        var connection = this.CreateConnection();

        for (var i = 0; i < UpdateEntity_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[i];

            var updatedEntity = Generate.UpdateFor(entity);

            connection.UpdateEntity(updatedEntity);
        }
    }
    #endregion UpdateEntity

    private static BenchmarkEntity ReadEntity(IDataReader dataReader)
    {
        var charBuffer = new Char[1];

        var ordinal = 0;
        return new()
        {
            Id = dataReader.GetInt64(ordinal++),
            BooleanValue = dataReader.GetInt64(ordinal++) == 1,
            BytesValue = (Byte[])dataReader.GetValue(ordinal++),
            ByteValue = dataReader.GetByte(ordinal++),
            CharValue = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1 ? charBuffer[0] : throw new(),
            DateTimeValue = DateTime.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture),
            DecimalValue = Decimal.Parse(dataReader.GetString(ordinal++), CultureInfo.InvariantCulture),
            DoubleValue = dataReader.GetDouble(ordinal++),
            EnumValue = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++)),
            GuidValue = Guid.Parse(dataReader.GetString(ordinal++)),
            Int16Value = (Int16)dataReader.GetInt64(ordinal++),
            Int32Value = (Int32)dataReader.GetInt64(ordinal++),
            Int64Value = dataReader.GetInt64(ordinal++),
            SingleValue = dataReader.GetFloat(ordinal++),
            StringValue = dataReader.GetString(ordinal++),
            TimeSpanValue = TimeSpan.Parse(dataReader.GetString(ordinal), CultureInfo.InvariantCulture)
        };
    }

    private SqliteTestDatabaseProvider? testDatabaseProvider;
}