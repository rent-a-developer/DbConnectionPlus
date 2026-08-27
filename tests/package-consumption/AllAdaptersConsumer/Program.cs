// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using System.Reflection;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace RentADeveloper.DbConnectionPlus.PackageConsumption.AllAdapters;

/// <summary>
/// A console application that installs all six packed DbConnectionPlus packages the way a real consumer
/// would, and asserts that the package graph is sound.
/// </summary>
/// <remarks>
/// <para>
/// The Native AOT consumer next door covers one adapter deeply; this one covers all five broadly. Everything
/// it asserts is a property of the <b>packages</b> rather than of the code, and is therefore invisible to a
/// build over the solution, where every assembly arrives through a project reference:
/// </para>
/// <list type="bullet">
/// <item>every adapter package restores, and its driver dependency (MySqlConnector, Npgsql,
/// Oracle.ManagedDataAccess.Core, Microsoft.Data.SqlClient, Microsoft.Data.Sqlite) flows transitively - none
/// of them is referenced by this project,</item>
/// <item>all five adapters resolve <b>one</b> DbConnectionPlus assembly rather than five copies,</item>
/// <item>the <c>net8.0</c> asset of each multi-targeted package is the one that gets picked, and it runs.</item>
/// </list>
/// <para>
/// CI builds this project with the .NET 8 SDK and nothing else installed, which is what turns the documented
/// <c>net8.0</c> floor from a claim into a checked fact.
/// </para>
/// <para>
/// No adapter but SQLite can be exercised against a server here - that is the integration suite's job, and it
/// needs four containers. What this program proves is that a consumer who installs the packages can reach the
/// point where a connection string would be the only thing missing.
/// </para>
/// </remarks>
public static class Program
{
    /// <summary>The entry point.</summary>
    /// <returns>Zero if every assertion passed, otherwise one.</returns>
    public static int Main()
    {
        Console.WriteLine(new string('=', 100));
        Console.WriteLine("DbConnectionPlus - all-adapters package consumer");
        Console.WriteLine();
        Console.WriteLine($"  runtime                          {Environment.Version}");
        Console.WriteLine($"  target framework                 net8.0");
        Console.WriteLine();

        try
        {
            RegisterEveryAdapter();
            AssertDriverPackagesFlowedTransitively();
            AssertOneSharedLibraryAssembly();
            RunAgainstSqlite();
        }
        catch (Exception exception)
        {
            Console.WriteLine();
            // The whole exception. A failure here is usually a missing or mis-targeted package asset, and the
            // inner exception is what names the assembly that could not be loaded.
            Console.WriteLine($"  ABORTED  {exception}");

            return 1;
        }

        Console.WriteLine();
        Console.WriteLine(new string('=', 100));

        if (Check.FailureCount == 0)
        {
            Console.WriteLine("RESULT: every assertion passed.");

            return 0;
        }

        Console.WriteLine($"RESULT: {Check.FailureCount} assertion(s) FAILED.");

        return 1;
    }

    /// <summary>
    /// Registers all five adapters in a single <c>Configure</c> call, the way the README tells a consumer to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each <c>UseXxx</c> lives in a different package and names its driver's connection type as a generic
    /// argument, so this call alone loads all five adapter assemblies and all five driver assemblies. A
    /// package whose dependency did not flow fails here with a <see cref="FileNotFoundException" /> rather
    /// than at some later assertion.
    /// </para>
    /// </remarks>
    private static void RegisterEveryAdapter()
    {
        Check.Section("1. Configure - all five adapter packages register in one call");

        DbConnectionPlusConfiguration? configured = null;

        Configure(configuration =>
            configured = configuration.UseMySql().UseOracle().UsePostgreSql().UseSqlite().UseSqlServer()
        );

        Check.True("the five UseXxx calls chain and return the configuration", configured is not null);

        Check.True(
            "the configured instance is the singleton the library uses",
            ReferenceEquals(configured, DbConnectionPlusConfiguration.Instance)
        );
    }

    /// <summary>
    /// Asserts that each adapter's driver package arrived transitively, by constructing its connection type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// None of these drivers is a <c>PackageReference</c> of this project. Each type therefore resolves only
    /// if its adapter package declared the dependency and NuGet flowed it, which is exactly the packaging
    /// mistake a project-referenced test cannot see. The connections are never opened - no server exists here.
    /// </para>
    /// </remarks>
    private static void AssertDriverPackagesFlowedTransitively()
    {
        Check.Section("2. Transitive driver packages - one connection type per adapter, constructed");

        AssertConnectionType("MySql", new MySqlConnection(), "MySqlConnector");
        AssertConnectionType("Oracle", new OracleConnection(), "Oracle.ManagedDataAccess");
        AssertConnectionType("PostgreSql", new NpgsqlConnection(), "Npgsql");
        AssertConnectionType("Sqlite", new SqliteConnection(), "Microsoft.Data.Sqlite");
        AssertConnectionType("SqlServer", new SqlConnection(), "Microsoft.Data.SqlClient");
    }

    /// <summary>
    /// Asserts that one driver's connection type constructed, and that it came out of the driver assembly the
    /// adapter package is supposed to depend on.
    /// </summary>
    /// <param name="adapter">The name of the adapter the driver belongs to.</param>
    /// <param name="connection">The freshly constructed, unopened connection.</param>
    /// <param name="expectedAssemblyName">The simple name of the assembly the type must come from.</param>
    private static void AssertConnectionType(string adapter, DbConnection connection, string expectedAssemblyName)
    {
        using (connection)
        {
            Check.Equal(
                $"{adapter}: {connection.GetType().Name} came from the expected driver assembly",
                expectedAssemblyName,
                connection.GetType().Assembly.GetName().Name
            );
        }
    }

    /// <summary>
    /// Asserts that exactly one DbConnectionPlus assembly is loaded, shared by all five adapters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Five adapter packages each depend on the DbConnectionPlus package. If a version range in one of them
    /// were wrong, NuGet would either fail the restore or resolve a second copy, and every adapter would then
    /// register into a different <c>DbConnectionPlusConfiguration.Instance</c> - a failure that shows up as
    /// "no database adapter is registered" at run time, long after the build was green.
    /// </para>
    /// </remarks>
    private static void AssertOneSharedLibraryAssembly()
    {
        Check.Section("3. One shared DbConnectionPlus assembly, not one copy per adapter");

        var libraryAssemblies = AppDomain
            .CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetName())
            .Where(name => name.Name == "RentADeveloper.DbConnectionPlus")
            .ToList();

        Check.Equal("exactly one DbConnectionPlus assembly is loaded", 1, libraryAssemblies.Count);

        var libraryVersion = typeof(DbConnectionPlusConfiguration).Assembly.GetName().Version;

        foreach (var adapter in new[] { "MySql", "Oracle", "PostgreSql", "Sqlite", "SqlServer" })
        {
            var adapterAssembly = Assembly.Load($"RentADeveloper.DbConnectionPlus.DatabaseAdapters.{adapter}");

            // All six packages are versioned and released together, so a mismatch here means the consumer
            // resolved an adapter from a different release than the core package - the exact situation the
            // shared <Version> in src/Directory.Build.props exists to prevent.
            Check.Equal(
                $"the {adapter} adapter package is the same version as the core package",
                libraryVersion,
                adapterAssembly.GetName().Version
            );
        }
    }

    /// <summary>
    /// Runs a real query through the packaged library, against the one provider that needs no server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything above proves the packages resolve. This proves the resolved assemblies actually work when a
    /// consumer calls them: an in-memory SQLite database, a table, a row, and the row read back as an entity.
    /// It is the shallow end of what the AOT consumer does, and it runs on the .NET 8 SDK.
    /// </para>
    /// </remarks>
    private static void RunAgainstSqlite()
    {
        Check.Section("4. A real round trip through the packaged library (SQLite, in memory)");

        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        connection.ExecuteNonQuery(
            """
            CREATE TABLE Widget (
                Id   INTEGER NOT NULL PRIMARY KEY,
                Name TEXT    NOT NULL
            )
            """
        );

        var affectedRows = connection.InsertEntity(new Widget { Id = 7, Name = "packaged" });

        Check.Equal("one row inserted", 1, affectedRows);

        var widget = connection.Query<Widget>($"SELECT Id, Name FROM Widget WHERE Id = {7}").Single();

        Check.Equal("Id round-tripped", 7L, widget.Id);
        Check.Equal("Name round-tripped", "packaged", widget.Name);
    }
}

/// <summary>The entity the SQLite round trip writes and reads back.</summary>
public sealed class Widget
{
    /// <summary>The primary key. Not database-generated, so it takes part in the INSERT.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>A plain string column.</summary>
    public string Name { get; set; } = string.Empty;
}
