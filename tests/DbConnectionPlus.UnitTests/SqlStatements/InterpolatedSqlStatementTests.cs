using RentADeveloper.DbConnectionPlus.SqlStatements;

namespace RentADeveloper.DbConnectionPlus.UnitTests.SqlStatements;

public class InterpolatedSqlStatementTests : UnitTestsBase
{
    [Fact]
    public void AppendFormatted_InterpolatedParameter_ShouldStoreParameter()
    {
        var value1 = Generate.ScalarValue();

        InterpolatedSqlStatement statement = $"SELECT {Parameter(value1)}";

        statement.Fragments
            .Should().HaveCount(2);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT "));

        statement.Fragments[1]
            .Should().Be(new InterpolatedParameter("Value1", value1));
    }

    [Fact]
    public void AppendFormatted_InterpolatedParameter_ShouldSupportComplexExpressions()
    {
        const Double baseDiscount = 0.1;
        var entityIds = Generate.Ids(20);

        InterpolatedSqlStatement statement =
            $"""
             SELECT  {Parameter(baseDiscount * 5 / 3)},
                     {Parameter(entityIds.Where(a => a > 5).Select(a => a.ToString()).ToArray()[0])}
             """;

        statement.Fragments
            .Should().HaveCount(4);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT  "));

        statement.Fragments[1]
            .Should().Be(new InterpolatedParameter("BaseDiscount53", baseDiscount * 5 / 3));

        statement.Fragments[2]
            .Should().Be(new Literal($",{Environment.NewLine}        "));

        statement.Fragments[3]
            .Should().Be(
                new InterpolatedParameter(
                    "EntityIdsWhereaa5SelectaaToStringToArray0",
                    entityIds.Where(a => a > 5).Select(a => a.ToString()).ToArray()[0]
                )
            );
    }

    [Fact]
    public void AppendFormatted_InterpolatedTemporaryTables_ShouldStoreTemporaryTables()
    {
        var entities = Generate.Multiple<Entity>();
        var entityIds = Generate.Ids();

        InterpolatedSqlStatement statement =
            $"""
             SELECT Value FROM {TemporaryTable(entityIds)}
             UNION
             SELECT Id FROM {TemporaryTable(entities)}
             """;

        statement.TemporaryTables
            .Should().HaveCount(2);

        var table1 = statement.TemporaryTables[0];

        table1.Name
            .Should().StartWith("EntityIds_");

        table1.Values
            .Should().BeEquivalentTo(entityIds);

        table1.ValuesType
            .Should().Be(typeof(Int64));

        statement.Fragments[2]
            .Should().Be(new Literal($"{Environment.NewLine}UNION{Environment.NewLine}SELECT Id FROM "));

        var table2 = statement.TemporaryTables[1];

        table2.Name
            .Should().StartWith("Entities_");

        table2.Values
            .Should().BeEquivalentTo(entities);

        table2.ValuesType
            .Should().Be(typeof(Entity));

        statement.Fragments
            .Should().HaveCount(4);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT Value FROM "));

        statement.Fragments[1]
            .Should().Be(table1);

        statement.Fragments[2]
            .Should().Be(new Literal($"{Environment.NewLine}UNION{Environment.NewLine}SELECT Id FROM "));

        statement.Fragments[3]
            .Should().Be(table2);
    }

    [Fact]
    public void AppendFormatted_MultipleInterpolatedParameters_ShouldStoreParameters()
    {
        var value1 = Generate.ScalarValue();
        var value2 = Generate.ScalarValue();
        var value3 = Generate.ScalarValue();

        InterpolatedSqlStatement statement = $"SELECT {Parameter(value1)}, {Parameter(value2)}, {Parameter(value3)}";

        statement.Fragments
            .Should().HaveCount(6);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT "));

        statement.Fragments[1]
            .Should().Be(new InterpolatedParameter("Value1", value1));

        statement.Fragments[2]
            .Should().Be(new Literal(", "));

        statement.Fragments[3]
            .Should().Be(new InterpolatedParameter("Value2", value2));

        statement.Fragments[4]
            .Should().Be(new Literal(", "));

        statement.Fragments[5]
            .Should().Be(new InterpolatedParameter("Value3", value3));
    }

    [Fact]
    public void AppendFormatted_ShouldFormatAndStoreLiteral()
    {
        InterpolatedSqlStatement statement = $"SELECT {123.45,10:N2}, {123.45,-10:N2}";

        statement.Fragments
            .Should().HaveCount(4);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT "));

        statement.Fragments[1]
            .Should().Be(new Literal("    123.45"));

        statement.Fragments[2]
            .Should().Be(new Literal(", "));

        statement.Fragments[3]
            .Should().Be(new Literal("123.45    "));
    }

    [Fact]
    public void AppendLiteral_ShouldStoreLiteral()
    {
#pragma warning disable RCS1214 // Unnecessary interpolated string
        InterpolatedSqlStatement statement = $"SELECT 1";
#pragma warning restore RCS1214 // Unnecessary interpolated string

        statement.Fragments
            .Should().HaveCount(1);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT 1"));
    }

    [Fact]
    public void Constructor_Code_Parameters_DuplicateParameterName_ShouldThrow()
    {
        Invoking(() => new InterpolatedSqlStatement(
                    "Code",
                    new("Parameter1", "Value1"),
                    new("Parameter1", "Value2"),
                    new("Parameter2", "Value3"),
                    new("Parameter2", "Value4")
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The specified parameters have the following duplicate parameter names: " +
                "'Parameter1', 'Parameter1', 'Parameter2', 'Parameter2'. Make sure each parameter name is only used " +
                "once.*"
            );

        Invoking(() => new InterpolatedSqlStatement(
                    "Code",
                    new("Parameter1", "Value1"),
                    new("PARAMETER1", "Value2"),
                    new("Parameter2", "Value3"),
                    new("PARAMETER2", "Value4")
                )
            )
            .Should().Throw<ArgumentException>()
            .WithMessage(
                "The specified parameters have the following duplicate parameter names: " +
                "'Parameter1', 'PARAMETER1', 'Parameter2', 'PARAMETER2'. Make sure each parameter name is only used " +
                "once.*"
            );
    }

    [Fact]
    public void Constructor_Code_Parameters_ShouldStoreCodeAsLiteral()
    {
        var statement = new InterpolatedSqlStatement("SELECT 1");

        statement.Fragments
            .Should().HaveCount(1);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT 1"));
    }

    [Fact]
    public void Constructor_Code_Parameters_ShouldStoreParameters()
    {
        var value1 = Generate.ScalarValue();
        var value2 = Generate.ScalarValue();
        var value3 = Generate.ScalarValue();

        var statement = new InterpolatedSqlStatement(
            "SELECT @Parameter1, @Parameter2, @Parameter3",
            ("Parameter1", value1),
            ("Parameter2", value2),
            ("Parameter3", value3)
        );

        statement.Fragments
            .Should().HaveCount(4);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT @Parameter1, @Parameter2, @Parameter3"));

        statement.Fragments[1]
            .Should().Be(new Parameter("Parameter1", value1));

        statement.Fragments[2]
            .Should().Be(new Parameter("Parameter2", value2));

        statement.Fragments[3]
            .Should().Be(new Parameter("Parameter3", value3));
    }

    [Fact]
    public void Constructor_LiteralLength_FormattedCount_ShouldInitializeInstance()
    {
        var statement = new InterpolatedSqlStatement(100, 10);

        statement.Fragments
            .Should().BeEmpty();
    }

    [Fact]
    public void Fragments_ShouldGetFragments()
    {
        var value1 = Generate.ScalarValue();
        var value2 = Generate.ScalarValue();
        var value3 = Generate.ScalarValue();
        var entityIds = Generate.Ids();
        var entities = Generate.Multiple<Entity>();

        InterpolatedSqlStatement statement =
            $"""
             SELECT {Parameter(value1)}
             UNION
             SELECT {Parameter(value2)}
             UNION
             SELECT {Parameter(value3)}
             UNION
             SELECT Value FROM {TemporaryTable(entityIds)}
             UNION
             SELECT Id FROM {TemporaryTable(entities)}
             """;

        statement.Fragments
            .Should().HaveCount(10);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT "));

        statement.Fragments[1]
            .Should().Be(new InterpolatedParameter("Value1", value1));

        statement.Fragments[2]
            .Should().Be(new Literal($"{Environment.NewLine}UNION{Environment.NewLine}SELECT "));

        statement.Fragments[3]
            .Should().Be(new InterpolatedParameter("Value2", value2));

        statement.Fragments[4]
            .Should().Be(new Literal($"{Environment.NewLine}UNION{Environment.NewLine}SELECT "));

        statement.Fragments[5]
            .Should().Be(new InterpolatedParameter("Value3", value3));

        statement.Fragments[6]
            .Should().Be(new Literal($"{Environment.NewLine}UNION{Environment.NewLine}SELECT Value FROM "));

        var table1 = statement.Fragments[7]
            .Should().BeOfType<InterpolatedTemporaryTable>().Subject;

        table1.Name
            .Should().StartWith("EntityIds_");

        table1.Values
            .Should().BeEquivalentTo(entityIds);

        table1.ValuesType
            .Should().Be(typeof(Int64));

        statement.Fragments[8]
            .Should().Be(new Literal($"{Environment.NewLine}UNION{Environment.NewLine}SELECT Id FROM "));

        var table2 = statement.Fragments[9]
            .Should().BeOfType<InterpolatedTemporaryTable>().Subject;

        table2.Name
            .Should().StartWith("Entities_");

        table2.Values
            .Should().BeEquivalentTo(entities);

        table2.ValuesType
            .Should().Be(typeof(Entity));
    }

    [Fact]
    public void FromString_EmptyString_ShouldCreateEmptyStatement()
    {
        var statement = InterpolatedSqlStatement.FromString(String.Empty);

        statement.Fragments
            .Should().HaveCount(1);

        statement.Fragments[0]
            .Should().Be(new Literal(String.Empty));
    }

    [Fact]
    public void FromString_ShouldCreateSqlStatementFromString()
    {
        var statement = InterpolatedSqlStatement.FromString("SELECT 1");

        statement.Fragments
            .Should().HaveCount(1);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT 1"));
    }

    [Fact]
    public void ImplicitConversion_NullValue_ShouldThrow() =>
        Invoking(() =>
                {
                    const String? sql = null;
#pragma warning disable RCS1124 // Inline local variable
                    InterpolatedSqlStatement statement = sql!;
#pragma warning restore RCS1124 // Inline local variable
                    return statement;
                }
            )
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void ImplicitConversion_ShouldCreateSqlStatementFromString()
    {
        InterpolatedSqlStatement statement = "SELECT 1";

        statement.Fragments
            .Should().HaveCount(1);

        statement.Fragments[0]
            .Should().Be(new Literal("SELECT 1"));
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        (String, Object?)[] parameters = [("Parameter1", "Value1")];

        ArgumentNullGuardVerifier.Verify(() => new InterpolatedSqlStatement("SELECT 1", parameters));
        ArgumentNullGuardVerifier.Verify(() => InterpolatedSqlStatement.FromString("SELECT 1"));
    }

    [Fact]
    public void TemporaryTables_ShouldGetInterpolatedTemporaryTables()
    {
        var entities = Generate.Multiple<Entity>();
        var entityIds = Generate.Ids();

        InterpolatedSqlStatement statement =
            $"""
             SELECT Value FROM {TemporaryTable(entityIds)}
             UNION
             SELECT Id FROM {TemporaryTable(entities)}
             """;

        statement.TemporaryTables
            .Should().HaveCount(2);

        var table1 = statement.TemporaryTables[0]
            .Should().BeOfType<InterpolatedTemporaryTable>().Subject;

        table1.Name
            .Should().StartWith("EntityIds_");

        table1.Values
            .Should().BeEquivalentTo(entityIds);

        table1.ValuesType
            .Should().Be(typeof(Int64));
    }

    [Fact]
    public void ToString_ShouldReturnStringRepresentationOfStatement()
    {
        var items = new List<Item>
        {
            new(1, "A", TestEnum.Value1),
            new(2, "B", TestEnum.Value2),
            new(3, "C", TestEnum.Value3)
        };

        List<Int32> ids = [1, 2, 3];

        const String name = "B";
        const TestEnum enumValue = TestEnum.Value2;

        InterpolatedSqlStatement statement = $"""
                                              SELECT    *
                                              FROM      {TemporaryTable(items)} TItem
                                              WHERE     TItem.Id IN (
                                                            SELECT  Value
                                                            FROM    {TemporaryTable(ids)}
                                                        )
                                                        AND
                                                        TItem.Name = {Parameter(name)}
                                                        AND
                                                        TItem.Enum = {Parameter(enumValue)}
                                              """;
        var temporaryTables = statement.TemporaryTables;

        temporaryTables
            .Should().HaveCount(2);

        var itemsTable = temporaryTables[0];

        itemsTable.Name
            .Should().StartWith("Items_");

        itemsTable.Values
            .Should().Be(items);

        itemsTable.ValuesType
            .Should().Be(typeof(Item));

        var idsTable = temporaryTables[1];

        idsTable.Name
            .Should().StartWith("Ids_");

        idsTable.Values
            .Should().Be(ids);

        idsTable.ValuesType
            .Should().Be(typeof(Int32));

        statement.ToString()
            .Should().Be(
                $$"""
                  SQL Statement

                  Statement Code
                  --------------
                  SELECT    *
                  FROM      {{itemsTable.Name}} TItem
                  WHERE     TItem.Id IN (
                                SELECT  Value
                                FROM    {{idsTable.Name}}
                            )
                            AND
                            TItem.Name = @Name
                            AND
                            TItem.Enum = @EnumValue
                  --------------

                  Statement Parameters
                  --------------------
                  Name = 'B' (System.String)
                  EnumValue = 'Value2' (RentADeveloper.DbConnectionPlus.UnitTests.TestData.TestEnum)

                  Statement Temporary Tables
                  --------------------------

                  {{itemsTable.Name}}
                  --------------------------------------
                  '{"Id":1,"Name":"A","Enum":1}' (RentADeveloper.DbConnectionPlus.UnitTests.TestData.Item)
                  '{"Id":2,"Name":"B","Enum":2}' (RentADeveloper.DbConnectionPlus.UnitTests.TestData.Item)
                  '{"Id":3,"Name":"C","Enum":3}' (RentADeveloper.DbConnectionPlus.UnitTests.TestData.Item)

                  {{idsTable.Name}}
                  ------------------------------------
                  '1' (System.Int32)
                  '2' (System.Int32)
                  '3' (System.Int32)

                  """
            );
    }
}
