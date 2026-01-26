// @formatter:off
// ReSharper disable InconsistentNaming
#pragma warning disable IDE0017, IDE0305

using System.Dynamic;
using BenchmarkDotNet.Attributes;
using FastMember;
using Microsoft.Data.SqlClient;
using RentADeveloper.DbConnectionPlus.Entities;
using RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase;
using RentADeveloper.DbConnectionPlus.Readers;
using RentADeveloper.DbConnectionPlus.UnitTests.TestData;
using static RentADeveloper.DbConnectionPlus.DbConnectionExtensions;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

// Note: All settings (i.e. *_EntitiesPerOperation and *_OperationsPerInvoke) are chosen so that each invoke
// takes at least 100 milliseconds to complete on a reasonably fast machine.

[MemoryDiagnoser]
[Config(typeof(BenchmarksConfig))]
public class Benchmarks
{
    [GlobalSetup]
    public void Setup_Global()
    {
        this.testDatabaseProvider.ResetDatabase();

        // Warm up connection pool.
        for (var i = 0; i < 20; i++)
        {
            using var warmUpConnection = this.CreateConnection();
            warmUpConnection.ExecuteScalar<Int32>("SELECT 1");
        }

        using var connection = this.CreateConnection();
        connection.ExecuteNonQuery("CHECKPOINT");
        connection.ExecuteNonQuery("DBCC DROPCLEANBUFFERS");
        connection.ExecuteNonQuery("DBCC FREEPROCCACHE");
    }

    private SqlConnection CreateConnection() =>
        (SqlConnection)this.testDatabaseProvider.CreateConnection();

    private void PrepareEntitiesInDb(Int32 numberOfEntities)
    {
        using var connection = this.CreateConnection();

        using var transaction = connection.BeginTransaction();

        connection.ExecuteNonQuery("DELETE FROM Entity", transaction);

        this.entitiesInDb = Generate.Multiple<Entity>(numberOfEntities);
        connection.InsertEntities(this.entitiesInDb, transaction);

        transaction.Commit();
    }

    private List<Entity> entitiesInDb = [];

    #region DeleteEntities
    private const String DeleteEntities_Category = "DeleteEntities";
    private const Int32 DeleteEntities_EntitiesPerOperation = 100;
    private const Int32 DeleteEntities_OperationsPerInvoke = 20;

    [IterationSetup(Targets = [nameof(DeleteEntities_Manually), nameof(DeleteEntities_DbConnectionPlus)])]
    public void DeleteEntities_Setup() =>
        this.PrepareEntitiesInDb(DeleteEntities_OperationsPerInvoke * DeleteEntities_EntitiesPerOperation);

    [Benchmark(Baseline = true, OperationsPerInvoke = DeleteEntities_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntities_Category)]
    public void DeleteEntities_Manually()
    {
        using var connection = this.CreateConnection();

        for (var i = 0; i < DeleteEntities_OperationsPerInvoke; i++)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Entity WHERE Id = @Id";

            var idParameter = new SqlParameter();
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
    public void DeleteEntities_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

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
    private const Int32 DeleteEntity_OperationsPerInvoke = 1200;

    [IterationSetup(Targets = [nameof(DeleteEntity_Manually), nameof(DeleteEntity_DbConnectionPlus)])]
    public void DeleteEntity_Setup() =>
        this.PrepareEntitiesInDb(DeleteEntity_OperationsPerInvoke);

    [Benchmark(Baseline = true, OperationsPerInvoke = DeleteEntity_OperationsPerInvoke)]
    [BenchmarkCategory(DeleteEntity_Category)]
    public void DeleteEntity_Manually()
    {
        using var connection = this.CreateConnection();

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
    public void DeleteEntity_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

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
    private const Int32 ExecuteNonQuery_OperationsPerInvoke = 1100;

    [IterationSetup(Targets = [nameof(ExecuteNonQuery_Manually), nameof(ExecuteNonQuery_DbConnectionPlus)])]
    public void ExecuteNonQuery_Setup() =>
        this.PrepareEntitiesInDb(ExecuteNonQuery_OperationsPerInvoke);

    [Benchmark(Baseline = true, OperationsPerInvoke = ExecuteNonQuery_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteNonQuery_Category)]
    public void ExecuteNonQuery_Manually()
    {
        using var connection = this.CreateConnection();

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
    public void ExecuteNonQuery_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

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

    [GlobalSetup(Targets = [nameof(ExecuteReader_Manually), nameof(ExecuteReader_DbConnectionPlus)])]
    public void ExecuteReader_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(ExecuteReader_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = ExecuteReader_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteReader_Category)]
    public List<Entity> ExecuteReader_Manually()
    {
        using var connection = this.CreateConnection();

        var entities = new List<Entity>();

        for (var i = 0; i < ExecuteReader_OperationsPerInvoke; i++)
        {
            entities.Clear();

            using var command = connection.CreateCommand();
            command.CommandText = $"""
                                  SELECT
                                    TOP ({ExecuteReader_EntitiesPerOperation})
                                    [Id],
                                    [BooleanValue],
                                    [ByteValue],
                                    [CharValue],
                                    [DateOnlyValue],
                                    [DateTimeValue],
                                    [DecimalValue],
                                    [DoubleValue],
                                    [EnumValue],
                                    [GuidValue],
                                    [Int16Value],
                                    [Int32Value],
                                    [Int64Value],
                                    [SingleValue],
                                    [StringValue],
                                    [TimeOnlyValue],
                                    [TimeSpanValue]
                                  FROM
                                    Entity
                                  """;

            using var dataReader = command.ExecuteReader();

            while (dataReader.Read())
            {
                var charBuffer = new Char[1];

                var ordinal = 0;
                entities.Add(new()
                {
                    Id = dataReader.GetInt64(ordinal++),
                    BooleanValue = dataReader.GetBoolean(ordinal++),
                    ByteValue = dataReader.GetByte(ordinal++),
                    CharValue = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1 ? charBuffer[0] : throw new(),
                    DateOnlyValue = DateOnly.FromDateTime((DateTime) dataReader.GetValue(ordinal++)),
                    DateTimeValue = dataReader.GetDateTime(ordinal++),
                    DecimalValue = dataReader.GetDecimal(ordinal++),
                    DoubleValue = dataReader.GetDouble(ordinal++),
                    EnumValue = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++)),
                    GuidValue = dataReader.GetGuid(ordinal++),
                    Int16Value = dataReader.GetInt16(ordinal++),
                    Int32Value = dataReader.GetInt32(ordinal++),
                    Int64Value = dataReader.GetInt64(ordinal++),
                    SingleValue = dataReader.GetFloat(ordinal++),
                    StringValue = dataReader.GetString(ordinal++),
                    TimeOnlyValue = TimeOnly.FromTimeSpan((TimeSpan)dataReader.GetValue(ordinal++)),
                    TimeSpanValue = (TimeSpan)dataReader.GetValue(ordinal)
                });
            }
        }

        return entities;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = ExecuteReader_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteReader_Category)]
    public List<Entity> ExecuteReader_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        var entities = new List<Entity>();

        for (var i = 0; i < ExecuteReader_OperationsPerInvoke; i++)
        {
            entities.Clear();

            using var dataReader = connection.ExecuteReader(
                $"""
                 SELECT
                     TOP ({ExecuteReader_EntitiesPerOperation})
                     [Id],
                     [BooleanValue],
                     [ByteValue],
                     [CharValue],
                     [DateOnlyValue],
                     [DateTimeValue],
                     [DecimalValue],
                     [DoubleValue],
                     [EnumValue],
                     [GuidValue],
                     [Int16Value],
                     [Int32Value],
                     [Int64Value],
                     [SingleValue],
                     [StringValue],
                     [TimeOnlyValue],
                     [TimeSpanValue]
                 FROM
                     Entity
                 """
            );

            while (dataReader.Read())
            {
                var charBuffer = new Char[1];

                var ordinal = 0;
                entities.Add(new()
                {
                    Id = dataReader.GetInt64(ordinal++),
                    BooleanValue = dataReader.GetBoolean(ordinal++),
                    ByteValue = dataReader.GetByte(ordinal++),
                    CharValue = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1 ? charBuffer[0] : throw new(),
                    DateOnlyValue = DateOnly.FromDateTime((DateTime) dataReader.GetValue(ordinal++)),
                    DateTimeValue = dataReader.GetDateTime(ordinal++),
                    DecimalValue = dataReader.GetDecimal(ordinal++),
                    DoubleValue = dataReader.GetDouble(ordinal++),
                    EnumValue = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++)),
                    GuidValue = dataReader.GetGuid(ordinal++),
                    Int16Value = dataReader.GetInt16(ordinal++),
                    Int32Value = dataReader.GetInt32(ordinal++),
                    Int64Value = dataReader.GetInt64(ordinal++),
                    SingleValue = dataReader.GetFloat(ordinal++),
                    StringValue = dataReader.GetString(ordinal++),
                    TimeOnlyValue = TimeOnly.FromTimeSpan((TimeSpan)dataReader.GetValue(ordinal++)),
                    TimeSpanValue = (TimeSpan)dataReader.GetValue(ordinal)
                });
            }
        }

        return entities;
    }
    #endregion ExecuteReader

    #region ExecuteScalar
    private const String ExecuteScalar_Category = "ExecuteScalar";
    private const Int32 ExecuteScalar_OperationsPerInvoke = 5000;

    [GlobalSetup(Targets = [nameof(ExecuteScalar_Manually), nameof(ExecuteScalar_DbConnectionPlus)])]
    public void ExecuteScalar_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(ExecuteScalar_OperationsPerInvoke);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = ExecuteScalar_OperationsPerInvoke)]
    [BenchmarkCategory(ExecuteScalar_Category)]
    public String ExecuteScalar_Manually()
    {
        using var connection = this.CreateConnection();

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
    public String ExecuteScalar_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

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

    [GlobalSetup(Targets = [nameof(Exists_Manually), nameof(Exists_DbConnectionPlus)])]
    public void Exists_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(Exists_OperationsPerInvoke);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Exists_OperationsPerInvoke)]
    [BenchmarkCategory(Exists_Category)]
    public Boolean Exists_Manually()
    {
        using var connection = this.CreateConnection();

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
        using var connection = this.CreateConnection();

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
    private const Int32 InsertEntities_EntitiesPerOperation = 100;

    [GlobalSetup(Targets = [nameof(InsertEntities_Manually), nameof(InsertEntities_DbConnectionPlus)])]
    public void InsertEntities_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(0);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = InsertEntities_OperationsPerInvoke)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_Manually()
    {
        using var connection = this.CreateConnection();

        for (var i = 0; i < InsertEntities_OperationsPerInvoke; i++)
        {
            var entities = Generate.Multiple<Entity>(InsertEntities_EntitiesPerOperation);

            using var command = connection.CreateCommand();
            command.CommandText = """
                                  INSERT INTO [Entity]
                                  (
                                    [Id],
                                    [BooleanValue],
                                    [ByteValue],
                                    [CharValue],
                                    [DateOnlyValue],
                                    [DateTimeValue],
                                    [DecimalValue],
                                    [DoubleValue],
                                    [EnumValue],
                                    [GuidValue],
                                    [Int16Value],
                                    [Int32Value],
                                    [Int64Value],
                                    [SingleValue],
                                    [StringValue],
                                    [TimeOnlyValue],
                                    [TimeSpanValue]
                                  )
                                  VALUES
                                  (
                                    @Id,
                                    @BooleanValue,
                                    @ByteValue,
                                    @CharValue,
                                    @DateOnlyValue,
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
                                    @TimeOnlyValue,
                                    @TimeSpanValue
                                  )
                                  """;

            var idParameter = new SqlParameter();
            idParameter.ParameterName = "@Id";

            var booleanValueParameter = new SqlParameter();
            booleanValueParameter.ParameterName = "@BooleanValue";

            var byteValueParameter = new SqlParameter();
            byteValueParameter.ParameterName = "@ByteValue";

            var charValueParameter = new SqlParameter();
            charValueParameter.ParameterName = "@CharValue";

            var dateOnlyParameter = new SqlParameter();
            dateOnlyParameter.ParameterName = "@DateOnlyValue";

            var dateTimeValueParameter = new SqlParameter();
            dateTimeValueParameter.ParameterName = "@DateTimeValue";

            var decimalValueParameter = new SqlParameter();
            decimalValueParameter.ParameterName = "@DecimalValue";

            var doubleValueParameter = new SqlParameter();
            doubleValueParameter.ParameterName = "@DoubleValue";

            var enumValueParameter = new SqlParameter();
            enumValueParameter.ParameterName = "@EnumValue";

            var guidValueParameter = new SqlParameter();
            guidValueParameter.ParameterName = "@GuidValue";

            var int16ValueParameter = new SqlParameter();
            int16ValueParameter.ParameterName = "@Int16Value";

            var int32ValueParameter = new SqlParameter();
            int32ValueParameter.ParameterName = "@Int32Value";

            var int64ValueParameter = new SqlParameter();
            int64ValueParameter.ParameterName = "@Int64Value";

            var singleValueParameter = new SqlParameter();
            singleValueParameter.ParameterName = "@SingleValue";

            var stringValueParameter = new SqlParameter();
            stringValueParameter.ParameterName = "@StringValue";

            var timeOnlyValueParameter = new SqlParameter();
            timeOnlyValueParameter.ParameterName = "@TimeOnlyValue";

            var timeSpanValueParameter = new SqlParameter();
            timeSpanValueParameter.ParameterName = "@TimeSpanValue";

            command.Parameters.Add(idParameter);
            command.Parameters.Add(booleanValueParameter);
            command.Parameters.Add(byteValueParameter);
            command.Parameters.Add(charValueParameter);
            command.Parameters.Add(dateOnlyParameter);
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
            command.Parameters.Add(timeOnlyValueParameter);
            command.Parameters.Add(timeSpanValueParameter);

            foreach (var entity in entities)
            {
                idParameter.Value = entity.Id;
                booleanValueParameter.Value = entity.BooleanValue;
                byteValueParameter.Value = entity.ByteValue;
                charValueParameter.Value = entity.CharValue;
                dateOnlyParameter.Value = entity.DateOnlyValue;
                dateTimeValueParameter.Value = entity.DateTimeValue;
                decimalValueParameter.Value = entity.DecimalValue;
                doubleValueParameter.Value = entity.DoubleValue;
                enumValueParameter.Value = entity.EnumValue.ToString();
                guidValueParameter.Value = entity.GuidValue;
                int16ValueParameter.Value = entity.Int16Value;
                int32ValueParameter.Value = entity.Int32Value;
                int64ValueParameter.Value = entity.Int64Value;
                singleValueParameter.Value = entity.SingleValue;
                stringValueParameter.Value = entity.StringValue;
                timeOnlyValueParameter.Value = entity.TimeOnlyValue;
                timeSpanValueParameter.Value = entity.TimeSpanValue;

                command.ExecuteNonQuery();
            }
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = InsertEntities_OperationsPerInvoke)]
    [BenchmarkCategory(InsertEntities_Category)]
    public void InsertEntities_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        for (var i = 0; i < InsertEntities_OperationsPerInvoke; i++)
        {
            var entitiesToInsert = Generate.Multiple<Entity>(InsertEntities_EntitiesPerOperation);

            connection.InsertEntities(entitiesToInsert);
        }
    }
    #endregion InsertEntities

    #region InsertEntity
    private const String InsertEntity_Category = "InsertEntity";
    private const Int32 InsertEntity_OperationsPerInvoke = 700;

    [GlobalSetup(Targets = [nameof(InsertEntity_Manually), nameof(InsertEntity_DbConnectionPlus)])]
    public void InsertEntity_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(0);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = InsertEntity_OperationsPerInvoke)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_Manually()
    {
        using var connection = this.CreateConnection();

        for (var i = 0; i < InsertEntity_OperationsPerInvoke; i++)
        {
            var entity = Generate.Single<Entity>();

            using var command = connection.CreateCommand();
            command.CommandText = """
                                  INSERT INTO [Entity]
                                  (
                                    [Id],
                                    [BooleanValue],
                                    [ByteValue],
                                    [CharValue],
                                    [DateOnlyValue],
                                    [DateTimeValue],
                                    [DecimalValue],
                                    [DoubleValue],
                                    [EnumValue],
                                    [GuidValue],
                                    [Int16Value],
                                    [Int32Value],
                                    [Int64Value],
                                    [SingleValue],
                                    [StringValue],
                                    [TimeOnlyValue],
                                    [TimeSpanValue]
                                  )
                                  VALUES
                                  (
                                    @Id,
                                    @BooleanValue,
                                    @ByteValue,
                                    @CharValue,
                                    @DateOnlyValue,
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
                                    @TimeOnlyValue,
                                    @TimeSpanValue
                                  )
                                  """;
            command.Parameters.Add(new("@Id", entity.Id));
            command.Parameters.Add(new("@BooleanValue", entity.BooleanValue));
            command.Parameters.Add(new("@ByteValue", entity.ByteValue));
            command.Parameters.Add(new("@CharValue", entity.CharValue));
            command.Parameters.Add(new("@DateOnlyValue", entity.DateOnlyValue));
            command.Parameters.Add(new("@DateTimeValue", entity.DateTimeValue));
            command.Parameters.Add(new("@DecimalValue", entity.DecimalValue));
            command.Parameters.Add(new("@DoubleValue", entity.DoubleValue));
            command.Parameters.Add(new("@EnumValue", entity.EnumValue.ToString()));
            command.Parameters.Add(new("@GuidValue", entity.GuidValue));
            command.Parameters.Add(new("@Int16Value", entity.Int16Value));
            command.Parameters.Add(new("@Int32Value", entity.Int32Value));
            command.Parameters.Add(new("@Int64Value", entity.Int64Value));
            command.Parameters.Add(new("@SingleValue", entity.SingleValue));
            command.Parameters.Add(new("@StringValue", entity.StringValue));
            command.Parameters.Add(new("@TimeOnlyValue", entity.TimeOnlyValue));
            command.Parameters.Add(new("@TimeSpanValue", entity.TimeSpanValue));

            command.ExecuteNonQuery();
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = InsertEntity_OperationsPerInvoke)]
    [BenchmarkCategory(InsertEntity_Category)]
    public void InsertEntity_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        for (var i = 0; i < InsertEntity_OperationsPerInvoke; i++)
        {
            var entity = Generate.Single<Entity>();

            connection.InsertEntity(entity);
        }
    }
    #endregion InsertEntity

    #region Parameter
    private const String Parameter_Category = "Parameter";
    private const Int32 Parameter_OperationsPerInvoke = 2500;

    [GlobalSetup(Targets = [nameof(Parameter_Manually), nameof(Parameter_DbConnectionPlus)])]
    public void Parameter_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(0);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Parameter_OperationsPerInvoke)]
    [BenchmarkCategory(Parameter_Category)]
    public Object Parameter_Manually()
    {
        using var connection = this.CreateConnection();

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
            result.Add(dataReader.GetInt32(0));
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
        using var connection = this.CreateConnection();

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
            result.Add(dataReader.GetInt32(0));
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

    [GlobalSetup(Targets = [nameof(Query_Dynamic_Manually), nameof(Query_Dynamic_DbConnectionPlus)])]
    public void Query_Dynamic_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(Query_Dynamic_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Query_Dynamic_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_Manually()
    {
        using var connection = this.CreateConnection();

        var entities = new List<dynamic>();

        for (var i = 0; i < Query_Dynamic_OperationsPerInvoke; i++)
        {
            entities.Clear();

            using var dataReader = connection.ExecuteReader(
                $"""
                 SELECT
                     TOP ({Query_Dynamic_EntitiesPerOperation})
                     [Id],
                     [BooleanValue],
                     [ByteValue],
                     [CharValue],
                     [DateOnlyValue],
                     [DateTimeValue],
                     [DecimalValue],
                     [DoubleValue],
                     [EnumValue],
                     [GuidValue],
                     [Int16Value],
                     [Int32Value],
                     [Int64Value],
                     [SingleValue],
                     [StringValue],
                     [TimeOnlyValue],
                     [TimeSpanValue]
                 FROM
                     Entity
                 """
            );

            while (dataReader.Read())
            {
                var charBuffer = new Char[1];

                var ordinal = 0;
                dynamic entity = new ExpandoObject();

                entity.Id = dataReader.GetInt64(ordinal++);
                entity.BooleanValue = dataReader.GetBoolean(ordinal++);
                entity.ByteValue = dataReader.GetByte(ordinal++);
                entity.CharValue = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1 
                    ? charBuffer[0] 
                    : throw new();
                entity.DateOnlyValue = DateOnly.FromDateTime((DateTime)dataReader.GetValue(ordinal++));
                entity.DateTimeValue = dataReader.GetDateTime(ordinal++);
                entity.DecimalValue = dataReader.GetDecimal(ordinal++);
                entity.DoubleValue = dataReader.GetDouble(ordinal++);
                entity.EnumValue = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++));
                entity.GuidValue = dataReader.GetGuid(ordinal++);
                entity.Int16Value = dataReader.GetInt16(ordinal++);
                entity.Int32Value = dataReader.GetInt32(ordinal++);
                entity.Int64Value = dataReader.GetInt64(ordinal++);
                entity.SingleValue = dataReader.GetFloat(ordinal++);
                entity.StringValue = dataReader.GetString(ordinal++);
                entity.TimeOnlyValue = TimeOnly.FromTimeSpan((TimeSpan)dataReader.GetValue(ordinal++));
                entity.TimeSpanValue = (TimeSpan)dataReader.GetValue(ordinal);

                entities.Add(entity);
            }
        }

        return entities;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Query_Dynamic_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Dynamic_Category)]
    public List<dynamic> Query_Dynamic_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        List<dynamic> entities = [];

        for (var i = 0; i < Query_Dynamic_OperationsPerInvoke; i++)
        {
            entities = connection
                .Query($"SELECT TOP ({Query_Dynamic_EntitiesPerOperation}) * FROM Entity")
                .ToList();
        }

        return entities;
    }
    #endregion Query_Dynamic

    #region Query_Scalars
    private const String Query_Scalars_Category = "Query_Scalars";
    private const Int32 Query_Scalars_OperationsPerInvoke = 1500;
    private const Int32 Query_Scalars_EntitiesPerOperation = 100;

    [GlobalSetup(Targets = [nameof(Query_Scalars_Manually), nameof(Query_Scalars_DbConnectionPlus)])]
    public void Query_Scalars_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(Query_Scalars_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Query_Scalars_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Scalars_Category)]
    public List<Int64> Query_Scalars_Manually()
    {
        using var connection = this.CreateConnection();

        var data = new List<Int64>();

        for (var i = 0; i < Query_Scalars_OperationsPerInvoke; i++)
        {
            data.Clear();

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT TOP ({Query_Scalars_EntitiesPerOperation}) Id FROM Entity";

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
    public List<Int64> Query_Scalars_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        List<Int64> data = [];

        for (var i = 0; i < Query_Scalars_OperationsPerInvoke; i++)
        {
            data = connection
                .Query<Int64>($"SELECT TOP ({Query_Scalars_EntitiesPerOperation}) Id FROM Entity")
                .ToList();
        }

        return data;
    }
    #endregion Query_Scalars

    #region Query_Entities
    private const String Query_Entities_Category = "Query_Entities";
    private const Int32 Query_Entities_OperationsPerInvoke = 600;
    private const Int32 Query_Entities_EntitiesPerOperation = 100;

    [GlobalSetup(Targets = [nameof(Query_Entities_Manually), nameof(Query_Entities_DbConnectionPlus)])]
    public void Query_Entities_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(Query_Entities_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Query_Entities_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Entities_Category)]
    public List<Entity> Query_Entities_Manually()
    {
        using var connection = this.CreateConnection();

        var entities = new List<Entity>();

        for (var i = 0; i < Query_Entities_OperationsPerInvoke; i++)
        {
            entities.Clear();

            using var dataReader = connection.ExecuteReader(
                $"""
                 SELECT
                     TOP ({Query_Entities_EntitiesPerOperation})
                     [Id],
                     [BooleanValue],
                     [ByteValue],
                     [CharValue],
                     [DateOnlyValue],
                     [DateTimeValue],
                     [DecimalValue],
                     [DoubleValue],
                     [EnumValue],
                     [GuidValue],
                     [Int16Value],
                     [Int32Value],
                     [Int64Value],
                     [SingleValue],
                     [StringValue],
                     [TimeOnlyValue],
                     [TimeSpanValue]
                 FROM
                     Entity
                 """
            );

            while (dataReader.Read())
            {
                var charBuffer = new Char[1];

                var ordinal = 0;
                entities.Add(new()
                {
                    Id = dataReader.GetInt64(ordinal++),
                    BooleanValue = dataReader.GetBoolean(ordinal++),
                    ByteValue = dataReader.GetByte(ordinal++),
                    CharValue = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1 ? charBuffer[0] : throw new(),
                    DateOnlyValue = DateOnly.FromDateTime((DateTime) dataReader.GetValue(ordinal++)),
                    DateTimeValue = dataReader.GetDateTime(ordinal++),
                    DecimalValue = dataReader.GetDecimal(ordinal++),
                    DoubleValue = dataReader.GetDouble(ordinal++),
                    EnumValue = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++)),
                    GuidValue = dataReader.GetGuid(ordinal++),
                    Int16Value = dataReader.GetInt16(ordinal++),
                    Int32Value = dataReader.GetInt32(ordinal++),
                    Int64Value = dataReader.GetInt64(ordinal++),
                    SingleValue = dataReader.GetFloat(ordinal++),
                    StringValue = dataReader.GetString(ordinal++),
                    TimeOnlyValue = TimeOnly.FromTimeSpan((TimeSpan)dataReader.GetValue(ordinal++)),
                    TimeSpanValue = (TimeSpan)dataReader.GetValue(ordinal)
                });
            }
        }

        return entities;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = Query_Entities_OperationsPerInvoke)]
    [BenchmarkCategory(Query_Entities_Category)]
    public List<Entity> Query_Entities_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        List<Entity> entities = [];

        for (var i = 0; i < Query_Entities_OperationsPerInvoke; i++)
        {
            entities = connection
                .Query<Entity>($"SELECT TOP ({Query_Entities_EntitiesPerOperation}) * FROM Entity")
                .ToList();
        }

        return entities;
    }
    #endregion Query_Entities

    #region Query_ValueTuples
    private const String Query_ValueTuples_Category = "Query_ValueTuples";
    private const Int32 Query_ValueTuples_OperationsPerInvoke = 900;
    private const Int32 Query_ValueTuples_EntitiesPerOperation = 100;

    [GlobalSetup(Targets = [nameof(Query_ValueTuples_Manually), nameof(Query_ValueTuples_DbConnectionPlus)])]
    public void Query_ValueTuples_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(Query_ValueTuples_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Query_ValueTuples_OperationsPerInvoke)]
    [BenchmarkCategory(Query_ValueTuples_Category)]
    public List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)> Query_ValueTuples_Manually()
    {
        using var connection = this.CreateConnection();

        var tuples = new List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>();

        for (var i = 0; i < Query_ValueTuples_OperationsPerInvoke; i++)
        {
            tuples.Clear();

            using var command = connection.CreateCommand();
            command.CommandText = $"""
                                   SELECT   TOP ({Query_ValueTuples_EntitiesPerOperation})
                                            Id, DateTimeValue, EnumValue, StringValue
                                   FROM     Entity
                                   """;

            using var dataReader = command.ExecuteReader();

            while (dataReader.Read())
            {
                tuples.Add(
                    (
                        dataReader.GetInt64(0),
                        dataReader.GetDateTime(1),
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
        Query_ValueTuples_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        List<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)> tuples = [];

        for (var i = 0; i < Query_ValueTuples_OperationsPerInvoke; i++)
        {
            tuples = connection
                .Query<(Int64 Id, DateTime DateTimeValue, TestEnum EnumValue, String StringValue)>(
                    $"""
                     SELECT   TOP ({Query_ValueTuples_EntitiesPerOperation})
                             Id, DateTimeValue, EnumValue, StringValue
                     FROM     Entity
                     """
                )
                .ToList();
        }

        return tuples;
    }
    #endregion Query_ValueTuples

    #region TemporaryTable_ComplexObjects
    private const String TemporaryTable_ComplexObjects_Category = "TemporaryTable_ComplexObjects";
    private const Int32 TemporaryTable_ComplexObjects_OperationsPerInvoke = 25;
    private const Int32 TemporaryTable_ComplexObjects_EntitiesPerOperation = 100;

    [GlobalSetup(Targets = [
        nameof(TemporaryTable_ComplexObjects_Manually),
        nameof(TemporaryTable_ComplexObjects_DbConnectionPlus)
    ])]
    public void TemporaryTable_ComplexObjects_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(0);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = TemporaryTable_ComplexObjects_OperationsPerInvoke)]
    [BenchmarkCategory(TemporaryTable_ComplexObjects_Category)]
    public List<Entity> TemporaryTable_ComplexObjects_Manually()
    {
        using var connection = this.CreateConnection();

        var entities = Generate.Multiple<Entity>(TemporaryTable_ComplexObjects_EntitiesPerOperation);

        var result = new List<Entity>();

        for (var i = 0; i < TemporaryTable_ComplexObjects_OperationsPerInvoke; i++)
        {
            result.Clear();

            using var entitiesReader = new ObjectReader(
                typeof(Entity),
                entities,
                EntityHelper.GetEntityTypeMetadata(typeof(Entity)).
                    MappedProperties.Where(a => a.CanRead).Select(a => a.PropertyName).ToArray()
            );

            using var getCollationCommand = connection.CreateCommand();
            getCollationCommand.CommandText =
                "SELECT CONVERT (VARCHAR(256), DATABASEPROPERTYEX(DB_NAME(), 'collation'))";
            var databaseCollation = (String)getCollationCommand.ExecuteScalar()!;

            using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText =
                $"""
                 CREATE TABLE [#Entities] (
                    [BooleanValue] BIT,
                    [ByteValue] TINYINT,
                    [CharValue] CHAR(1),
                    [DateOnlyValue] DATE,
                    [DateTimeValue] DATETIME2,
                    [DecimalValue] DECIMAL(28, 10),
                    [DoubleValue] FLOAT,
                    [EnumValue] NVARCHAR(200) COLLATE {databaseCollation},
                    [GuidValue] UNIQUEIDENTIFIER,
                    [Id] BIGINT,
                    [Int16Value] SMALLINT,
                    [Int32Value] INT,
                    [Int64Value] BIGINT,
                    [SingleValue] REAL,
                    [StringValue] NVARCHAR(MAX) COLLATE {databaseCollation},
                    [TimeOnlyValue] TIME,
                    [TimeSpanValue] TIME
                 )
                 """;
            createTableCommand.ExecuteNonQuery();

            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.DestinationTableName = "#Entities";
                bulkCopy.WriteToServer(entitiesReader);
            }

            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText =
                """
                SELECT
                    [Id],
                    [BooleanValue],
                    [ByteValue],
                    [CharValue],
                    [DateOnlyValue],
                    [DateTimeValue],
                    [DecimalValue],
                    [DoubleValue],
                    [EnumValue],
                    [GuidValue],
                    [Int16Value],
                    [Int32Value],
                    [Int64Value],
                    [SingleValue],
                    [StringValue],
                    [TimeOnlyValue],
                    [TimeSpanValue]
                FROM
                    #Entities
                """;

            using var dataReader = selectCommand.ExecuteReader();

            while (dataReader.Read())
            {
                var charBuffer = new Char[1];

                var ordinal = 0;
                result.Add(new()
                {
                    Id = dataReader.GetInt64(ordinal++),
                    BooleanValue = dataReader.GetBoolean(ordinal++),
                    ByteValue = dataReader.GetByte(ordinal++),
                    CharValue = dataReader.GetChars(ordinal++, 0, charBuffer, 0, 1) == 1 ? charBuffer[0] : throw new(),
                    DateOnlyValue = DateOnly.FromDateTime((DateTime)dataReader.GetValue(ordinal++)),
                    DateTimeValue = dataReader.GetDateTime(ordinal++),
                    DecimalValue = dataReader.GetDecimal(ordinal++),
                    DoubleValue = dataReader.GetDouble(ordinal++),
                    EnumValue = Enum.Parse<TestEnum>(dataReader.GetString(ordinal++)),
                    GuidValue = dataReader.GetGuid(ordinal++),
                    Int16Value = dataReader.GetInt16(ordinal++),
                    Int32Value = dataReader.GetInt32(ordinal++),
                    Int64Value = dataReader.GetInt64(ordinal++),
                    SingleValue = dataReader.GetFloat(ordinal++),
                    StringValue = dataReader.GetString(ordinal++),
                    TimeOnlyValue = TimeOnly.FromTimeSpan((TimeSpan)dataReader.GetValue(ordinal++)),
                    TimeSpanValue = (TimeSpan)dataReader.GetValue(ordinal)
                });
            }

            using var dropTableCommand = connection.CreateCommand();
            dropTableCommand.CommandText = "DROP TABLE #Entities";
            dropTableCommand.ExecuteNonQuery();
        }

        return result;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = TemporaryTable_ComplexObjects_OperationsPerInvoke)]
    [BenchmarkCategory(TemporaryTable_ComplexObjects_Category)]
    public List<Entity> TemporaryTable_ComplexObjects_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        var entities = Generate.Multiple<Entity>(TemporaryTable_ComplexObjects_EntitiesPerOperation);

        List<Entity> result = [];

        for (var i = 0; i < TemporaryTable_ComplexObjects_OperationsPerInvoke; i++)
        {
            result = connection.Query<Entity>($"SELECT * FROM {TemporaryTable(entities)}").ToList();
        }

        return result;
    }
    #endregion TemporaryTable_ComplexObjects

    #region TemporaryTable_ScalarValues
    private const String TemporaryTable_ScalarValues_Category = "TemporaryTable_ScalarValues";
    private const Int32 TemporaryTable_ScalarValues_OperationsPerInvoke = 30;
    private const Int32 TemporaryTable_ScalarValues_ValuesPerOperation = 5000;

    [GlobalSetup(Targets = [
        nameof(TemporaryTable_ScalarValues_Manually),
        nameof(TemporaryTable_ScalarValues_DbConnectionPlus)
    ])]
    public void TemporaryTable_ScalarValues_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(0);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = TemporaryTable_ScalarValues_OperationsPerInvoke)]
    [BenchmarkCategory(TemporaryTable_ScalarValues_Category)]
    public List<String> TemporaryTable_ScalarValues_Manually()
    {
        using var connection = this.CreateConnection();

        var scalarValues = Enumerable
            .Range(0, TemporaryTable_ScalarValues_ValuesPerOperation)
            .Select(a => a.ToString())
            .ToList();

        var result = new List<String>();

        for (var i = 0; i < TemporaryTable_ScalarValues_OperationsPerInvoke; i++)
        {
            result.Clear();

            using var valuesReader = new EnumerableReader(scalarValues, typeof(String), "Value");

            using var getCollationCommand = connection.CreateCommand();
            getCollationCommand.CommandText =
                "SELECT CONVERT (VARCHAR(256), DATABASEPROPERTYEX(DB_NAME(), 'collation'))";
            var databaseCollation = (String)getCollationCommand.ExecuteScalar()!;

            using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = $"CREATE TABLE #Values (Value NVARCHAR(4) COLLATE {databaseCollation})";
            createTableCommand.ExecuteNonQuery();

            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.DestinationTableName = "#Values";
                bulkCopy.WriteToServer(valuesReader);
            }

            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT Value FROM #Values";

            using var dataReader = selectCommand.ExecuteReader();

            while (dataReader.Read())
            {
                result.Add(dataReader.GetString(0));
            }

            using var dropTableCommand = connection.CreateCommand();
            dropTableCommand.CommandText = "DROP TABLE #Values";
            dropTableCommand.ExecuteNonQuery();
        }

        return result;
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = TemporaryTable_ScalarValues_OperationsPerInvoke)]
    [BenchmarkCategory(TemporaryTable_ScalarValues_Category)]
    public List<String> TemporaryTable_ScalarValues_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        var scalarValues = Enumerable
            .Range(0, TemporaryTable_ScalarValues_ValuesPerOperation)
            .Select(a => a.ToString())
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
    private const Int32 UpdateEntities_OperationsPerInvoke = 10;
    private const Int32 UpdateEntities_EntitiesPerOperation = 100;

    [GlobalSetup(Targets = [
        nameof(UpdateEntities_Manually),
        nameof(UpdateEntities_DbConnectionPlus)
    ])]
    public void UpdateEntities_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(UpdateEntities_EntitiesPerOperation);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = UpdateEntities_OperationsPerInvoke)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_Manually()
    {
        using var connection = this.CreateConnection();

        for (var i = 0; i < UpdateEntities_OperationsPerInvoke; i++)
        {
            var updatedEntities = Generate.UpdatesFor(this.entitiesInDb);

            using var command = connection.CreateCommand();
            command.CommandText = """
                                  UPDATE    [Entity]
                                  SET       [BooleanValue] = @BooleanValue,
                                            [ByteValue] = @ByteValue,
                                            [CharValue] = @CharValue,
                                            [DateOnlyValue] = @DateOnlyValue,
                                            [DateTimeValue] = @DateTimeValue,
                                            [DecimalValue] = @DecimalValue,
                                            [DoubleValue] = @DoubleValue,
                                            [EnumValue] = @EnumValue,
                                            [GuidValue] = @GuidValue,
                                            [Int16Value] = @Int16Value,
                                            [Int32Value] = @Int32Value,
                                            [Int64Value] = @Int64Value,
                                            [SingleValue] = @SingleValue,
                                            [StringValue] = @StringValue,
                                            [TimeOnlyValue] = @TimeOnlyValue,
                                            [TimeSpanValue] = @TimeSpanValue
                                  WHERE     [Id] = @Id
                                  """;

            var idParameter = new SqlParameter();
            idParameter.ParameterName = "@Id";

            var booleanValueParameter = new SqlParameter();
            booleanValueParameter.ParameterName = "@BooleanValue";

            var byteValueParameter = new SqlParameter();
            byteValueParameter.ParameterName = "@ByteValue";

            var charValueParameter = new SqlParameter();
            charValueParameter.ParameterName = "@CharValue";

            var dateOnlyValueParameter = new SqlParameter();
            dateOnlyValueParameter.ParameterName = "@DateOnlyValue";

            var dateTimeValueParameter = new SqlParameter();
            dateTimeValueParameter.ParameterName = "@DateTimeValue";

            var decimalValueParameter = new SqlParameter();
            decimalValueParameter.ParameterName = "@DecimalValue";

            var doubleValueParameter = new SqlParameter();
            doubleValueParameter.ParameterName = "@DoubleValue";

            var enumValueParameter = new SqlParameter();
            enumValueParameter.ParameterName = "@EnumValue";

            var guidValueParameter = new SqlParameter();
            guidValueParameter.ParameterName = "@GuidValue";

            var int16ValueParameter = new SqlParameter();
            int16ValueParameter.ParameterName = "@Int16Value";

            var int32ValueParameter = new SqlParameter();
            int32ValueParameter.ParameterName = "@Int32Value";

            var int64ValueParameter = new SqlParameter();
            int64ValueParameter.ParameterName = "@Int64Value";

            var singleValueParameter = new SqlParameter();
            singleValueParameter.ParameterName = "@SingleValue";

            var stringValueParameter = new SqlParameter();
            stringValueParameter.ParameterName = "@StringValue";

            var timeOnlyValueParameter = new SqlParameter();
            timeOnlyValueParameter.ParameterName = "@TimeOnlyValue";

            var timeSpanValueParameter = new SqlParameter();
            timeSpanValueParameter.ParameterName = "@TimeSpanValue";

            command.Parameters.Add(idParameter);
            command.Parameters.Add(booleanValueParameter);
            command.Parameters.Add(byteValueParameter);
            command.Parameters.Add(charValueParameter);
            command.Parameters.Add(dateOnlyValueParameter);
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
            command.Parameters.Add(timeOnlyValueParameter);
            command.Parameters.Add(timeSpanValueParameter);

            foreach (var updatedEntity in updatedEntities)
            {
                idParameter.Value = updatedEntity.Id;
                booleanValueParameter.Value = updatedEntity.BooleanValue;
                byteValueParameter.Value = updatedEntity.ByteValue;
                charValueParameter.Value = updatedEntity.CharValue;
                dateOnlyValueParameter.Value = updatedEntity.DateOnlyValue;
                dateTimeValueParameter.Value = updatedEntity.DateTimeValue;
                decimalValueParameter.Value = updatedEntity.DecimalValue;
                doubleValueParameter.Value = updatedEntity.DoubleValue;
                enumValueParameter.Value = updatedEntity.EnumValue.ToString();
                guidValueParameter.Value = updatedEntity.GuidValue;
                int16ValueParameter.Value = updatedEntity.Int16Value;
                int32ValueParameter.Value = updatedEntity.Int32Value;
                int64ValueParameter.Value = updatedEntity.Int64Value;
                singleValueParameter.Value = updatedEntity.SingleValue;
                stringValueParameter.Value = updatedEntity.StringValue;
                timeOnlyValueParameter.Value = updatedEntity.TimeOnlyValue;
                timeSpanValueParameter.Value = updatedEntity.TimeSpanValue;

                command.ExecuteNonQuery();
            }
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = UpdateEntities_OperationsPerInvoke)]
    [BenchmarkCategory(UpdateEntities_Category)]
    public void UpdateEntities_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        for (var i = 0; i < UpdateEntities_OperationsPerInvoke; i++)
        {
            var updatesEntities = Generate.UpdatesFor(this.entitiesInDb);

            connection.UpdateEntities(updatesEntities);
        }
    }
    #endregion UpdateEntities

    #region UpdateEntity
    private const String UpdateEntity_Category = "UpdateEntity";
    private const Int32 UpdateEntity_OperationsPerInvoke = 700;

    [GlobalSetup(Targets = [
        nameof(UpdateEntity_Manually),
        nameof(UpdateEntity_DbConnectionPlus)
    ])]
    public void UpdateEntity_Setup()
    {
        this.Setup_Global();
        this.PrepareEntitiesInDb(UpdateEntity_OperationsPerInvoke);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = UpdateEntity_OperationsPerInvoke)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_Manually()
    {
        using var connection = this.CreateConnection();

        for (var i = 0; i < UpdateEntity_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[i];

            var updatedEntity = Generate.UpdateFor(entity);

            using var command = connection.CreateCommand();
            command.CommandText = """
                                  UPDATE    [Entity]
                                  SET       [BooleanValue] = @BooleanValue,
                                            [ByteValue] = @ByteValue,
                                            [CharValue] = @CharValue,
                                            [DateOnlyValue] = @DateOnlyValue,
                                            [DateTimeValue] = @DateTimeValue,
                                            [DecimalValue] = @DecimalValue,
                                            [DoubleValue] = @DoubleValue,
                                            [EnumValue] = @EnumValue,
                                            [GuidValue] = @GuidValue,
                                            [Int16Value] = @Int16Value,
                                            [Int32Value] = @Int32Value,
                                            [Int64Value] = @Int64Value,
                                            [SingleValue] = @SingleValue,
                                            [StringValue] = @StringValue,
                                            [TimeOnlyValue] = @TimeOnlyValue,
                                            [TimeSpanValue] = @TimeSpanValue
                                  WHERE     [Id] = @Id
                                  """;
            command.Parameters.Add(new("@Id", updatedEntity.Id));
            command.Parameters.Add(new("@BooleanValue", updatedEntity.BooleanValue));
            command.Parameters.Add(new("@ByteValue", updatedEntity.ByteValue));
            command.Parameters.Add(new("@CharValue", updatedEntity.CharValue));
            command.Parameters.Add(new("@DateOnlyValue", updatedEntity.DateOnlyValue));
            command.Parameters.Add(new("@DateTimeValue", updatedEntity.DateTimeValue));
            command.Parameters.Add(new("@DecimalValue", updatedEntity.DecimalValue));
            command.Parameters.Add(new("@DoubleValue", updatedEntity.DoubleValue));
            command.Parameters.Add(new("@EnumValue", updatedEntity.EnumValue.ToString()));
            command.Parameters.Add(new("@GuidValue", updatedEntity.GuidValue));
            command.Parameters.Add(new("@Int16Value", updatedEntity.Int16Value));
            command.Parameters.Add(new("@Int32Value", updatedEntity.Int32Value));
            command.Parameters.Add(new("@Int64Value", updatedEntity.Int64Value));
            command.Parameters.Add(new("@SingleValue", updatedEntity.SingleValue));
            command.Parameters.Add(new("@StringValue", updatedEntity.StringValue));
            command.Parameters.Add(new("@TimeOnlyValue", updatedEntity.TimeOnlyValue));
            command.Parameters.Add(new("@TimeSpanValue", updatedEntity.TimeSpanValue));

            command.ExecuteNonQuery();
        }
    }

    [Benchmark(Baseline = false, OperationsPerInvoke = UpdateEntity_OperationsPerInvoke)]
    [BenchmarkCategory(UpdateEntity_Category)]
    public void UpdateEntity_DbConnectionPlus()
    {
        using var connection = this.CreateConnection();

        for (var i = 0; i < UpdateEntity_OperationsPerInvoke; i++)
        {
            var entity = this.entitiesInDb[i];

            var updatedEntity = Generate.UpdateFor(entity);

            connection.UpdateEntity(updatedEntity);
        }
    }
    #endregion UpdateEntity

    private readonly SqlServerTestDatabaseProvider testDatabaseProvider = new();
}
