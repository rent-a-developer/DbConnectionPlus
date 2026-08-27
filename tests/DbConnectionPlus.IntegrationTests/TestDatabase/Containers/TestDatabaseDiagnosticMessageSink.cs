// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

using Xunit.Sdk;

namespace RentADeveloper.DbConnectionPlus.IntegrationTests.TestDatabase.Containers;

/// <summary>
/// Forwards what Testcontainers logs while it pulls an image, creates a container and waits for the database
/// server inside it into xUnit's diagnostic messages.
/// </summary>
/// <remarks>
/// The fixtures take an <see cref="IMessageSink" /> because they normally receive xUnit's own, but this assembly
/// creates them itself - so it has to supply one. Writing to the console instead would go nowhere:
/// <c>[assembly: CaptureConsole]</c> redirects console output into the output of the running test, and a
/// container starts when no test is running. Show these messages with:
/// <code>
/// dotnet test --project tests/DbConnectionPlus.IntegrationTests/DbConnectionPlus.IntegrationTests.csproj --xunit-diagnostics
/// </code>
/// </remarks>
internal sealed class TestDatabaseDiagnosticMessageSink : IMessageSink
{
    /// <summary>
    /// The single instance of the <see cref="TestDatabaseDiagnosticMessageSink" /> class.
    /// </summary>
    public static readonly TestDatabaseDiagnosticMessageSink Instance = new();

    /// <inheritdoc />
    public bool OnMessage(IMessageSinkMessage message)
    {
        if (message is IDiagnosticMessage diagnosticMessage)
        {
            TestContext.Current.SendDiagnosticMessage(diagnosticMessage.Message);
        }

        return true;
    }
}
