// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Runtime.CompilerServices;

namespace RentADeveloper.DbConnectionPlus.PackageConsumption.Aot;

/// <summary>
/// A console application that consumes the packed DbConnectionPlus NuGet packages and exercises the library's
/// feature paths against a real SQLite database, so that they can be published with Native AOT and run as a
/// native binary.
/// </summary>
/// <remarks>
/// <para>
/// This program exists because the defect it guards against is invisible to every other test in the
/// repository. Under trimming, a missing <c>[DynamicallyAccessedMembers]</c> annotation makes reflection
/// return fewer members with no error at all; on the JIT nothing is trimmed, so the unit and integration
/// suites pass with a broken annotation chain. Measured: 6 columns of real data in, 0 bound, no exception.
/// </para>
/// <para>
/// It reaches the library through <c>PackageReference</c>, never through a project reference: the trim
/// annotations, the embedded <c>ILLink.Descriptors.xml</c> and the
/// <c>[assembly: AssemblyMetadata("IsTrimmable", "True")]</c> marker all have to survive packing, and a
/// project reference would not prove that.
/// </para>
/// <para>Run it as an ordinary application to get the JIT baseline, and publish it with
/// <c>-p:PublishAot=true</c> to get the result that matters. See README.md.</para>
/// </remarks>
public static class Program
{
    /// <summary>The entry point.</summary>
    /// <returns>Zero if every assertion passed, otherwise one.</returns>
    public static Int32 Main()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"dbconnectionplus-aot-consumer-{Guid.NewGuid():N}.db");

        Console.WriteLine(new String('=', 100));
        Console.WriteLine("DbConnectionPlus - Native AOT package consumer");
        Console.WriteLine();
        var packageAssembly = typeof(DbConnectionPlusConfiguration).Assembly.GetName();

        Console.WriteLine($"  package assembly                 {packageAssembly.Name} {packageAssembly.Version}");
        Console.WriteLine($"  runtime                          {Environment.Version}");
        Console.WriteLine($"  RuntimeFeature.IsDynamicCodeSupported  {RuntimeFeature.IsDynamicCodeSupported}");
        Console.WriteLine($"  RuntimeFeature.IsDynamicCodeCompiled   {RuntimeFeature.IsDynamicCodeCompiled}");
        Console.WriteLine($"  database                         {databasePath}");
        Console.WriteLine();
        Console.WriteLine(
            RuntimeFeature.IsDynamicCodeSupported
                ? "  Dynamic code IS supported, so the expression-tree materializers run. This is the JIT baseline."
                : "  Dynamic code is NOT supported, so the reflection materializers run. This is the AOT result."
        );

        DbConnectionExtensions.Configure(configuration => configuration.UseSqlite());

        try
        {
            using var connection = new SqliteConnection($"Data Source={databasePath}");
            connection.Open();

            SmokeCases.CreateSchema(connection);
            SmokeCases.InsertEntity(connection);
            SmokeCases.QueryEntity(connection);
            SmokeCases.QueryEntityWithADifferentSelectList(connection);
            SmokeCases.QueryImmutableEntity(connection);
            SmokeCases.QueryValueTuple(connection);
            SmokeCases.QueryNestedValueTuple(connection);
            SmokeCases.QueryDataRow(connection);
            SmokeCases.SingleColumnTemporaryTable(connection);
            SmokeCases.MultiColumnTemporaryTable(connection);
            SmokeCases.ZeroBindingGuard(connection);
            SmokeCases.QueryValueTupleWithANumericEnum(connection);
            SmokeCases.QueryNestedValueTupleWithANumericEnum(connection);
            SmokeCases.QueryValueTupleWithANamedEnum(connection);
            SmokeCases.QueryNestedValueTupleWithANamedEnum(connection);
        }
        catch (Exception exception)
        {
            Console.WriteLine();
            // The whole exception: type, message, stack trace and any inner exception. Under Native AOT the
            // inner exception is often the only thing that says which reflection call failed.
            Console.WriteLine($"  ABORTED  {exception}");

            return 1;
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }

        Console.WriteLine();
        Console.WriteLine(new String('=', 100));

        if (Check.FailureCount == 0)
        {
            Console.WriteLine("RESULT: every assertion passed.");

            return 0;
        }

        Console.WriteLine($"RESULT: {Check.FailureCount} assertion(s) FAILED.");

        return 1;
    }
}
