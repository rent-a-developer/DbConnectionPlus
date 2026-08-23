// ReSharper disable UnusedParameter.Local

namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_TemporaryTableTests : UnitTestsBase
{
    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() => TemporaryTable(new List<string>()));

    [Fact]
    public void TemporaryTable_ShouldInferTableNameFromValuesExpressionIfPossible()
    {
        var entityIds = Generate.Ids();
        static List<long> Get() => Generate.Ids();
        static List<long> GetEntityIds() => Generate.Ids();
#pragma warning disable RCS1163 // Unused parameter
        static List<long> GetEntityIdsByCategory(string category) => Generate.Ids();
#pragma warning restore RCS1163 // Unused parameter

        TemporaryTable(entityIds).Name
            .Should().StartWith("EntityIds_");

        TemporaryTable(GetEntityIds()).Name
            .Should().StartWith("EntityIds_");

        TemporaryTable(GetEntityIdsByCategory("Shoes")).Name
            .Should().StartWith("EntityIdsByCategoryShoes_");

        TemporaryTable(this.testEntityIds).Name
            .Should().StartWith("TestEntityIds_");

        TemporaryTable(Get()).Name
            .Should().StartWith("Values_");
    }

    [Fact]
    public void TemporaryTable_ShouldReturnInterpolatedTemporaryTable()
    {
        var entityIds = Generate.Ids();

        var temporaryTable1 = TemporaryTable(entityIds);

        temporaryTable1.Values
            .Should().BeSameAs(entityIds);

        temporaryTable1.ValuesType
            .Should().Be(typeof(long));

        temporaryTable1.Name
            .Should().StartWith("EntityIds_");

        var entities = Generate.Multiple<Entity>();

        var temporaryTable2 = TemporaryTable(entities);

        temporaryTable2.Values
            .Should().BeSameAs(entities);

        temporaryTable2.ValuesType
            .Should().Be(typeof(Entity));

        temporaryTable2.Name
            .Should().StartWith("Entities_");
    }

    [Fact]
    public void TemporaryTable_ShouldTruncateInferredTableName()
    {
        // ReSharper disable once InconsistentNaming
        int[] longname_1234567890_1234567890_1234567890_1234567890_1234567890_1234567890 = [1, 2, 3];

        TemporaryTable(longname_1234567890_1234567890_1234567890_1234567890_1234567890_1234567890).Name
            .Should().HaveLength(60)
            .And.StartWith("Longname_1234567890_1234567_");
    }

    [Fact]
    public void TemporaryTable_TIsObject_ShouldThrow() =>
        Invoking(() => TemporaryTable(new List<object>()))
            .Should().Throw<ArgumentException>()
            .WithMessage(
                $"The type parameter T cannot be the type {typeof(object)}."
            );

    private readonly List<long> testEntityIds = Generate.Ids();
}
