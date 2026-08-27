// Copyright (c) 2026 David Liebeherr
// Licensed under the MIT License. See LICENSE.md in the project root for more information.

namespace RentADeveloper.DbConnectionPlus.PackageConsumption.Aot;

/// <summary>
/// The feature paths the smoke test exercises against a real SQLite database.
/// </summary>
/// <remarks>
/// <para>
/// Every case asserts <b>values</b>, never just row counts. Silent trimming damage does not remove rows; it
/// leaves every property of every returned entity at its default, which a "did any rows come back?" assertion
/// cannot see.
/// </para>
/// </remarks>
public static class SmokeCases
{
    /// <summary>The entity the smoke test inserts and reads back.</summary>
    private static readonly SmokeEntity ExpectedEntity = new()
    {
        Id = 1,
        Name = "first",
        Balance = 1234.56m,
        IsActive = true,
        CreatedAt = new DateTime(2026, 8, 11, 12, 34, 56, DateTimeKind.Unspecified),
        ExternalId = new Guid("6f9619ff-8b86-d011-b42d-00cf4fc964ff"),
        Status = SmokeStatus.Active,
        Quantity = 42,
    };

    /// <summary>The primary key of the single row the enum cases read.</summary>
    private const long EnumRowId = 1;

    /// <summary>
    /// Creates the tables the remaining cases read from, and writes the row the enum cases read.
    /// </summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// The enum row is written with SQL literals rather than through <c>InsertEntity</c>, because naming an
    /// enum member in this project would root it and defeat the purpose of those cases. See the comment above
    /// the enum declarations in <c>Model.cs</c>.
    /// </para>
    /// </remarks>
    public static void CreateSchema(DbConnection connection)
    {
        connection.ExecuteNonQuery(
            """
            CREATE TABLE SmokeEntity (
                Id         INTEGER NOT NULL PRIMARY KEY,
                Name       TEXT    NOT NULL,
                Balance    TEXT    NOT NULL,
                IsActive   INTEGER NOT NULL,
                CreatedAt  TEXT    NOT NULL,
                ExternalId TEXT    NOT NULL,
                Status     INTEGER NOT NULL,
                Quantity   INTEGER NOT NULL
            )
            """
        );

        connection.ExecuteNonQuery(
            """
            CREATE TABLE SmokeEnum (
                Id            INTEGER NOT NULL PRIMARY KEY,
                FlatNumeric   INTEGER NOT NULL,
                NestedNumeric INTEGER NOT NULL,
                FlatNamed     TEXT    NOT NULL,
                NestedNamed   TEXT    NOT NULL
            )
            """
        );

        connection.ExecuteNonQuery(
            """
            INSERT INTO SmokeEnum (Id, FlatNumeric, NestedNumeric, FlatNamed, NestedNamed)
            VALUES (1, 71, 81, 'FlatNamedChosen', 'NestedNamedChosen')
            """
        );
    }

    /// <summary>
    /// Writes the entity through <c>InsertEntity</c>, which reads every property through the accessor
    /// delegates <c>EntityPropertyMetadata</c> exposes.
    /// </summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    public static void InsertEntity(DbConnection connection)
    {
        Check.Section("1. InsertEntity - property reads through the accessor delegates");

        var affectedRows = connection.InsertEntity(ExpectedEntity);

        Check.Equal("one row inserted", 1, affectedRows);

        Check.Equal(
            "the row is readable again",
            1L,
            connection.ExecuteScalar<long>("SELECT COUNT(*) FROM SmokeEntity")
        );
    }

    /// <summary>
    /// Materializes the entity through the property-setter strategy and asserts <b>every</b> property.
    /// </summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// This is the regression test for silent trimming damage. It is not vacuous: the library reaches the
    /// properties of <see cref="SmokeEntity" /> only through its own
    /// <c>[DynamicallyAccessedMembers]</c>-annotated call path, and a statically rooted member is not
    /// reflection-visible on its own - measured, with 1 of 6 properties rooted by a direct call and no
    /// annotation, reflection found 0 members (see the comment at the top of <c>Model.cs</c> for why).
    /// If that annotation chain ever breaks, no reflection metadata is emitted for the properties, every
    /// column fails to bind, and this case is what notices.
    /// </para>
    /// </remarks>
    public static void QueryEntity(DbConnection connection)
    {
        Check.Section("2. Query<SmokeEntity> - property-setter strategy, every property asserted");

        var entity = connection
            .Query<SmokeEntity>(
                $"""
                SELECT Id, Name, Balance, IsActive, CreatedAt, ExternalId, Status, Quantity
                FROM   SmokeEntity
                WHERE  Id = {ExpectedEntity.Id}
                """
            )
            .Single();

        Check.Equal("Id", ExpectedEntity.Id, entity.Id);
        Check.Equal("Name", ExpectedEntity.Name, entity.Name);
        Check.Equal("Balance", ExpectedEntity.Balance, entity.Balance);
        Check.Equal("IsActive", ExpectedEntity.IsActive, entity.IsActive);
        Check.Equal("CreatedAt", ExpectedEntity.CreatedAt, entity.CreatedAt);
        Check.Equal("ExternalId", ExpectedEntity.ExternalId, entity.ExternalId);
        Check.Equal("Status", ExpectedEntity.Status, entity.Status);
        Check.Equal("Quantity", ExpectedEntity.Quantity, entity.Quantity);
    }

    /// <summary>
    /// Repeats the previous case with a different, reordered and shorter SELECT list.
    /// </summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// Materializers are cached per result-set shape, so a mapping bug can bind correctly for one SELECT list
    /// and incorrectly for another - field ordinals are baked into the delegate, and the same column arrives as
    /// a different type from different providers. The columns that are not selected must come back at their
    /// default value - that is the difference between "not selected" and "silently unbound".
    /// </para>
    /// </remarks>
    public static void QueryEntityWithADifferentSelectList(DbConnection connection)
    {
        Check.Section("3. Query<SmokeEntity> - different result-set shape (reordered, partial SELECT list)");

        var entity = connection
            .Query<SmokeEntity>($"SELECT Quantity, Name, Id FROM SmokeEntity WHERE Id = {ExpectedEntity.Id}")
            .Single();

        Check.Equal("Quantity", ExpectedEntity.Quantity, entity.Quantity);
        Check.Equal("Name", ExpectedEntity.Name, entity.Name);
        Check.Equal("Id", ExpectedEntity.Id, entity.Id);
        Check.Equal("Balance is not selected and stays default", 0m, entity.Balance);
        Check.Equal("Status is not selected and stays default", SmokeStatus.Unknown, entity.Status);
    }

    /// <summary>Materializes an entity that has no writable properties, through its constructor.</summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    public static void QueryImmutableEntity(DbConnection connection)
    {
        Check.Section("4. Query<ImmutableSmokeEntity> - constructor injection");

        var entity = connection
            .Query<ImmutableSmokeEntity>($"SELECT Id, Name, Balance FROM SmokeEntity WHERE Id = {ExpectedEntity.Id}")
            .Single();

        Check.Equal("Id", ExpectedEntity.Id, entity.Id);
        Check.Equal("Name", ExpectedEntity.Name, entity.Name);
        Check.Equal("Balance", ExpectedEntity.Balance, entity.Balance);
    }

    /// <summary>Materializes a value tuple of seven fields or fewer.</summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    public static void QueryValueTuple(DbConnection connection)
    {
        Check.Section("5. Query<(long, string, decimal)> - value tuple");

        var (id, name, balance) = connection
            .Query<(long Id, string Name, decimal Balance)>(
                $"SELECT Id, Name, Balance FROM SmokeEntity WHERE Id = {ExpectedEntity.Id}"
            )
            .Single();

        Check.Equal("Id", ExpectedEntity.Id, id);
        Check.Equal("Name", ExpectedEntity.Name, name);
        Check.Equal("Balance", ExpectedEntity.Balance, balance);
    }

    /// <summary>Materializes a value tuple of more than seven fields, which the runtime nests in a
    /// <c>TRest</c> field.</summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// This is the path where trimming safety rests on the embedded <c>ILLink.Descriptors.xml</c> rather than on an
    /// annotation: <c>[DynamicallyAccessedMembers]</c> is not recursive and cannot reach the nested value tuple type,
    /// which is only known at run time. It is therefore the case most likely to fail under trimming, and the one most
    /// worth running natively.
    /// </para>
    /// </remarks>
    public static void QueryNestedValueTuple(DbConnection connection)
    {
        Check.Section("6. Query<(...8 fields)> - nested value tuple (TRest)");

        var tuple = connection
            .Query<(long A, long B, long C, long D, long E, long F, long G, string H)>(
                $"""
                SELECT Id AS A, Quantity AS B, Id AS C, Quantity AS D, Id AS E, Quantity AS F, Id AS G, Name AS H
                FROM   SmokeEntity
                WHERE  Id = {ExpectedEntity.Id}
                """
            )
            .Single();

        Check.Equal("field 1", ExpectedEntity.Id, tuple.A);
        Check.Equal("field 2", (long)ExpectedEntity.Quantity, tuple.B);
        Check.Equal("field 7", ExpectedEntity.Id, tuple.G);
        Check.Equal("field 8 (nested in TRest)", ExpectedEntity.Name, tuple.H);
    }

    /// <summary>Reads a result set through the non-generic query API, which returns
    /// <see cref="Dynamic.DataRow" />.</summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// The string indexer is the AOT-safe form and the documented default. <c>dynamic</c> member access is the
    /// JIT-only alternative and is deliberately not used here.
    /// </para>
    /// </remarks>
    public static void QueryDataRow(DbConnection connection)
    {
        Check.Section("7. Query - non-generic, DataRow indexer");

        var row = connection.Query($"SELECT Id, Name FROM SmokeEntity WHERE Id = {ExpectedEntity.Id}").Single();

        Check.Equal("row[\"Id\"]", ExpectedEntity.Id, Convert.ToInt64(row["Id"], null));
        Check.Equal("row[\"Name\"]", ExpectedEntity.Name, row["Name"] as string);
    }

    /// <summary>Streams a single-column temporary table into the database and reads it back.</summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    public static void SingleColumnTemporaryTable(DbConnection connection)
    {
        Check.Section("8. TemporaryTable - single column");

        var values = new List<long> { 10, 20, 30 };

        var read = connection.Query<long>($"SELECT Value FROM {TemporaryTable(values)} ORDER BY Value").ToList();

        Check.Equal("three values round-trip", 3, read.Count);
        Check.True("the values are unchanged", read.SequenceEqual(values));
    }

    /// <summary>Streams a multi-column temporary table into the database and reads it back.</summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// This is the path <c>EnumerableReader</c> serves. It reads every property of every item
    /// through the same accessor delegates <c>InsertEntity</c> uses.
    /// </para>
    /// </remarks>
    public static void MultiColumnTemporaryTable(DbConnection connection)
    {
        Check.Section("9. TemporaryTable - multiple columns");

        var items = new List<SmokeItem>
        {
            new() { Id = 1, Label = "one" },
            new() { Id = 2, Label = "two" },
        };

        var read = connection.Query<SmokeItem>($"SELECT Id, Label FROM {TemporaryTable(items)} ORDER BY Id").ToList();

        Check.Equal("two items round-trip", 2, read.Count);
        Check.Equal("item 1 Id", 1L, read[0].Id);
        Check.Equal("item 1 Label", "one", read[0].Label);
        Check.Equal("item 2 Id", 2L, read[1].Id);
        Check.Equal("item 2 Label", "two", read[1].Label);
    }

    /// <summary>Asserts that a result set which binds no property at all fails loudly.</summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// This is layer 3 of the correctness design, the zero-binding guard - the only layer that does not depend
    /// on a human getting an annotation right. A broken <c>[DynamicallyAccessedMembers]</c> chain is
    /// indistinguishable from this case at the point the materializer is built: reflection reports no writable
    /// property that any column matches. Without the guard both produce a fully default-valued entity and no
    /// exception, which was measured as exactly that: 6 columns in, 0 bound, no error.
    /// </para>
    /// <para>
    /// The guard is why the "broken chain, no guard" case cannot be reproduced through the public API any
    /// more, which is the point of having it.
    /// </para>
    /// </remarks>
    public static void ZeroBindingGuard(DbConnection connection)
    {
        Check.Section("10. Zero-binding guard - a result set that binds nothing must throw");

        Check.Throws<InvalidOperationException>(
            "a result set matching no property throws instead of returning default-valued entities",
            "could be mapped to a writable property of the entity type",
            () =>
                connection
                    .Query<SmokeEntity>($"SELECT 1 AS Alpha, 2 AS Beta FROM SmokeEntity WHERE Id = {ExpectedEntity.Id}")
                    .ToList()
        );
    }

    /// <summary>
    /// Materializes an enum field of a value tuple with seven fields or fewer, from the integer SQLite returns.
    /// </summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// Nothing preserves <see cref="FlatTupleNumericEnum" />: the DAM annotation on the query method's type
    /// parameter reaches the value tuple type, not the types of its fields, and
    /// <c>ILLink.Descriptors.xml</c> names only <c>System.ValueTuple`1</c>-<c>`8</c>. The conversion calls
    /// <c>Enum.IsDefined</c> and <c>Enum.ToObject</c>, both of which need the enum's values, so a trimmer that
    /// removed them would fail here.
    /// </para>
    /// </remarks>
    public static void QueryValueTupleWithANumericEnum(DbConnection connection)
    {
        Check.Section("11. Query<(long, enum)> - enum field of a flat value tuple, from an INTEGER column");

        var (id, status) = connection
            .Query<(long Id, FlatTupleNumericEnum Status)>(
                $"SELECT Id, FlatNumeric AS Status FROM SmokeEnum WHERE Id = {EnumRowId}"
            )
            .Single();

        Check.Equal("Id", EnumRowId, id);
        Check.Equal("the enum field binds the stored value", 71, (int)status);
    }

    /// <summary>
    /// Materializes an enum field that the runtime keeps in the nested value tuple of a tuple with more than
    /// seven fields, from the integer SQLite returns.
    /// </summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// Case 11 with one more indirection: the enum is the only field of the nested tuple, so its type is
    /// reached through the generic arguments of a type that was itself reached through the generic arguments
    /// of the tuple. Neither hop carries an annotation.
    /// </para>
    /// </remarks>
    public static void QueryNestedValueTupleWithANumericEnum(DbConnection connection)
    {
        Check.Section("12. Query<(...8 fields)> - enum field nested in TRest, from an INTEGER column");

        var tuple = connection
            .Query<(long A, long B, long C, long D, long E, long F, long G, NestedTupleNumericEnum H)>(
                $"""
                SELECT Id AS A, Id AS B, Id AS C, Id AS D, Id AS E, Id AS F, Id AS G, NestedNumeric AS H
                FROM   SmokeEnum
                WHERE  Id = {EnumRowId}
                """
            )
            .Single();

        Check.Equal("field 1", EnumRowId, tuple.A);
        Check.Equal("field 8 (enum nested in TRest) binds the stored value", 81, (int)tuple.H);
    }

    /// <summary>
    /// Parses an enum field of a value tuple with seven fields or fewer by name, from a text column.
    /// </summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// This is the case that needs the enum's member <b>names</b> rather than its values:
    /// <c>EnumConverter</c> reaches <c>Enum.TryParse(Type, String, Boolean, out Object)</c> for a string
    /// source. It is the path <c>EnumSerializationMode.Strings</c> produces, and this case is what verifies
    /// it natively rather than only on the just-in-time compiler.
    /// </para>
    /// </remarks>
    public static void QueryValueTupleWithANamedEnum(DbConnection connection)
    {
        Check.Section("13. Query<(long, enum)> - enum field of a flat value tuple, parsed from a TEXT column");

        var (id, status) = connection
            .Query<(long Id, FlatTupleNamedEnum Status)>(
                $"SELECT Id, FlatNamed AS Status FROM SmokeEnum WHERE Id = {EnumRowId}"
            )
            .Single();

        Check.Equal("Id", EnumRowId, id);
        Check.Equal("the name in the column parsed to the right member", 91, (int)status);
        Check.Equal("the member name survived trimming", "FlatNamedChosen", status.ToString());
    }

    /// <summary>
    /// Parses an enum field that the runtime keeps in the nested value tuple of a tuple with more than seven
    /// fields by name, from a text column.
    /// </summary>
    /// <param name="connection">The open connection to the temporary SQLite database.</param>
    /// <remarks>
    /// <para>
    /// The narrowest case in the smoke test, and the one that combines both hazards: the enum type is reached
    /// only through the generic arguments of the nested tuple at run time, and the conversion needs its member
    /// names. If a future runtime stopped preserving the members of enums it keeps, this is the assertion that
    /// would notice first.
    /// </para>
    /// </remarks>
    public static void QueryNestedValueTupleWithANamedEnum(DbConnection connection)
    {
        Check.Section("14. Query<(...8 fields)> - enum field nested in TRest, parsed from a TEXT column");

        var tuple = connection
            .Query<(long A, long B, long C, long D, long E, long F, long G, NestedTupleNamedEnum H)>(
                $"""
                SELECT Id AS A, Id AS B, Id AS C, Id AS D, Id AS E, Id AS F, Id AS G, NestedNamed AS H
                FROM   SmokeEnum
                WHERE  Id = {EnumRowId}
                """
            )
            .Single();

        Check.Equal("field 1", EnumRowId, tuple.A);
        Check.Equal("field 8 (enum nested in TRest) parsed to the right member", 101, (int)tuple.H);
        Check.Equal("the member name survived trimming", "NestedNamedChosen", tuple.H.ToString());
    }
}
