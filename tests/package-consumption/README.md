# Package consumption tests

These console apps consume the **packed NuGet packages** (never project references) and are the only place
the packaging itself is exercised end to end. Every assembly they touch came out of a `.nupkg`, which is what
makes them able to see defects that a solution build cannot:

- the `[DynamicallyAccessedMembers]` annotations, the embedded `ILLink.Descriptors.xml` and the
  `[assembly: AssemblyMetadata("IsTrimmable", "True")]` marker survive packing,
- each adapter package really declares its driver dependency (`MySqlConnector`, `Npgsql`,
  `Oracle.ManagedDataAccess.Core`, `Microsoft.Data.SqlClient`, `Microsoft.Data.Sqlite`),
- all five adapters resolve **one** `DbConnectionPlus` assembly, not five copies,
- the `net8.0` asset of the multi-targeted packages is the one a `net8.0` consumer gets, and it runs.

| Consumer | Packages | What it is for |
|---|---|---|
| `AotConsumer` | core + SQLite | The Native AOT gate. Multi-targets `net8.0;net10.0`, published natively for both. See [its README](AotConsumer/README.md) — it is the only check in the repository that can see silent trimming damage. |
| `AllAdaptersConsumer` | all six | Breadth. Registers all five adapters, asserts the driver packages flowed transitively and that one core assembly is shared. Built by CI with the **.NET 8 SDK alone**, which is what makes the documented `net8.0` floor a checked fact rather than a claim. |

Both apps exit non-zero on failure and hand-roll their assertions (`Check.cs`, linked into both): xUnit,
NSubstitute and AwesomeAssertions all need run-time code generation, which a Native AOT binary does not have.

## The isolation boundary

The folder carries its own empty `Directory.Build.props` / `Directory.Build.targets`. They stop MSBuild's
upward search so the repository's `Directory.Build.props` — authorship, analyzers, multi-targeting — does not
apply here. Without that boundary these projects would be testing the repository's build instead of the
packages.

`nuget.config` points at `artifacts/packages` and sets an isolated package cache (`.packages`). NuGet resolves
by version and not by content, so without the isolated cache a re-pack of an unchanged version number would
silently restore the previous build.

The consumers are deliberately **not** in `DbConnectionPlus.slnx`: they cannot restore until `dotnet pack` has
run, and a clean clone must still be able to `dotnet build DbConnectionPlus.slnx`.

## Running locally

Pack, then publish the AOT consumer natively and run it — one command does all three:

```bash
pwsh -File scripts/verify-package-aot.ps1 -Pack
```

Pass `-Framework net8.0` for the documented AOT floor; the default is `net10.0`. CI runs both.

The all-adapters consumer is an ordinary `dotnet run`, once the packages exist:

```bash
dotnet run --project tests/package-consumption/AllAdaptersConsumer/AllAdaptersConsumer.csproj -c Release
```

After re-packing without `-Pack`, delete `tests/package-consumption/.packages` yourself, or NuGet will hand
the consumers the previous build of the same version number.
