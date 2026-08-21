using RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;
using Xunit.Sdk;
using Xunit.v3;

[assembly: Parallelization(Mode = ParallelMode.None)]
[assembly: CaptureConsole]

// Removes the database containers the run started. The containers themselves are started on demand, by the first
// test class that needs one.
[assembly: AssemblyFixture(typeof(TestDatabaseContainerCleanup))]
