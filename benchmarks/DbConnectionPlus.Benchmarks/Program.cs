#pragma warning disable RCS1163, IDE0022

using BenchmarkDotNet.Running;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public static class Program
{
    public static void Main(String[] args)
    {
        /*BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args);*/

        var benchmarks = new Benchmarks();

        benchmarks.Setup_Global();
        benchmarks.DeleteEntities_Setup();
        benchmarks.DeleteEntities_DbCommand();

        benchmarks.Setup_Global();
        benchmarks.DeleteEntities_Setup();
        benchmarks.DeleteEntities_Dapper();

        benchmarks.Setup_Global();
        benchmarks.DeleteEntities_Setup();
        benchmarks.DeleteEntities_DbConnectionPlus();

        benchmarks.Setup_Global();
        benchmarks.DeleteEntity_Setup();
        benchmarks.DeleteEntity_DbCommand();

        benchmarks.Setup_Global();
        benchmarks.DeleteEntity_Setup();
        benchmarks.DeleteEntity_Dapper();

        benchmarks.Setup_Global();
        benchmarks.DeleteEntity_Setup();
        benchmarks.DeleteEntity_DbConnectionPlus();

        benchmarks.Setup_Global();
        benchmarks.ExecuteNonQuery_Setup();
        benchmarks.ExecuteNonQuery_DbCommand();

        benchmarks.Setup_Global();
        benchmarks.ExecuteNonQuery_Setup();
        benchmarks.ExecuteNonQuery_Dapper();

        benchmarks.Setup_Global();
        benchmarks.ExecuteNonQuery_Setup();
        benchmarks.ExecuteNonQuery_DbConnectionPlus();

        benchmarks.ExecuteReader_Setup();
        benchmarks.ExecuteReader_DbCommand();

        benchmarks.ExecuteReader_Setup();
        benchmarks.ExecuteReader_Dapper();

        benchmarks.ExecuteReader_Setup();
        benchmarks.ExecuteReader_DbConnectionPlus();

        benchmarks.ExecuteScalar_Setup();
        benchmarks.ExecuteScalar_DbCommand();

        benchmarks.ExecuteScalar_Setup();
        benchmarks.ExecuteScalar_Dapper();

        benchmarks.ExecuteScalar_Setup();
        benchmarks.ExecuteScalar_DbConnectionPlus();

        benchmarks.Exists_Setup();
        benchmarks.Exists_DbCommand();

        benchmarks.Exists_Setup();
        benchmarks.Exists_DbConnectionPlus();

        benchmarks.InsertEntities_Setup();
        benchmarks.InsertEntities_DbCommand();

        benchmarks.InsertEntities_Setup();
        benchmarks.InsertEntities_Dapper();

        benchmarks.InsertEntities_Setup();
        benchmarks.InsertEntities_DbConnectionPlus();

        benchmarks.InsertEntity_Setup();
        benchmarks.InsertEntity_DbCommand();

        benchmarks.InsertEntity_Setup();
        benchmarks.InsertEntity_Dapper();

        benchmarks.InsertEntity_Setup();
        benchmarks.InsertEntity_DbConnectionPlus();

        benchmarks.Parameter_Setup();
        benchmarks.Parameter_DbCommand();

        benchmarks.Parameter_Setup();
        benchmarks.Parameter_Dapper();

        benchmarks.Parameter_Setup();
        benchmarks.Parameter_DbConnectionPlus();

        benchmarks.Query_Dynamic_Setup();
        benchmarks.Query_Dynamic_DbCommand();

        benchmarks.Query_Dynamic_Setup();
        benchmarks.Query_Dynamic_Dapper();

        benchmarks.Query_Scalars_Setup();
        benchmarks.Query_Scalars_DbConnectionPlus();

        benchmarks.Query_Entities_Setup();
        benchmarks.Query_Entities_DbCommand();

        benchmarks.Query_Entities_Setup();
        benchmarks.Query_Entities_Dapper();

        benchmarks.Query_Entities_Setup();
        benchmarks.Query_Entities_DbConnectionPlus();

        benchmarks.Query_Dynamic_Setup();
        benchmarks.Query_Dynamic_DbConnectionPlus();

        benchmarks.Query_Scalars_Setup();
        benchmarks.Query_Scalars_DbCommand();

        benchmarks.Query_Scalars_Setup();
        benchmarks.Query_Scalars_Dapper();

        benchmarks.Query_ValueTuples_Setup();
        benchmarks.Query_ValueTuples_DbCommand();

        benchmarks.Query_ValueTuples_Setup();
        benchmarks.Query_ValueTuples_Dapper();

        benchmarks.Query_ValueTuples_Setup();
        benchmarks.Query_ValueTuples_DbConnectionPlus();

        benchmarks.TemporaryTable_ComplexObjects_Setup();
        benchmarks.TemporaryTable_ComplexObjects_DbCommand();

        benchmarks.TemporaryTable_ComplexObjects_Setup();
        benchmarks.TemporaryTable_ComplexObjects_DbConnectionPlus();

        benchmarks.TemporaryTable_ScalarValues_Setup();
        benchmarks.TemporaryTable_ScalarValues_DbCommand();

        benchmarks.TemporaryTable_ScalarValues_Setup();
        benchmarks.TemporaryTable_ScalarValues_DbConnectionPlus();

        benchmarks.UpdateEntities_Setup();
        benchmarks.UpdateEntities_DbCommand();

        benchmarks.UpdateEntities_Setup();
        benchmarks.UpdateEntities_Dapper();

        benchmarks.UpdateEntities_Setup();
        benchmarks.UpdateEntities_DbConnectionPlus();

        benchmarks.UpdateEntity_Setup();
        benchmarks.UpdateEntity_DbCommand();

        benchmarks.UpdateEntity_Setup();
        benchmarks.UpdateEntity_Dapper();

        benchmarks.UpdateEntity_Setup();
        benchmarks.UpdateEntity_DbConnectionPlus();
    }
}