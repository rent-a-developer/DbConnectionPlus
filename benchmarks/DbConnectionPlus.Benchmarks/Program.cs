#pragma warning disable RCS1163, IDE0022

using BenchmarkDotNet.Running;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public static class Program
{
    public static void Main(String[] args)
    {
        if (args is not ["test"])
        {
            BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args);
        }
        else
        {
            var benchmarks = new Benchmarks();
            benchmarks.Exists__Setup();

            for (int i = 0; i < 50000; i++)
            {
                benchmarks.Exists_Command();
                benchmarks.Exists_Dapper();
                benchmarks.Exists_DbConnectionPlus();
            }
        }
    }
}
