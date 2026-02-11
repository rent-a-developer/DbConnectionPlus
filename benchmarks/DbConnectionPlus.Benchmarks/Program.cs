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
            benchmarks.Parameter__Setup();

            for (int i = 0; i < 5000; i++)
            {
                benchmarks.Parameter_Command();
                benchmarks.Parameter_DbConnectionPlus();
            }
        }
    }
}
