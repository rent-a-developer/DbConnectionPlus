#pragma warning disable RCS1163, IDE0022

using BenchmarkDotNet.Running;

namespace RentADeveloper.DbConnectionPlus.Benchmarks;

public static class Program
{
    public static void Main(String[] args)
    {
        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args);
    }
}
