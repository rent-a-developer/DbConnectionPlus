using AutoFixture;
using AutoFixture.AutoNSubstitute;
using RentADeveloper.DbConnectionPlus.UnitTests.Assertions;
using DataRow = RentADeveloper.DbConnectionPlus.Dynamic.DataRow;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Dynamic;

public class DataRowTests : UnitTestsBase
{
    [Fact]
    public void ShouldBeMutable()
    {
        var dictionary = new Dictionary<String, Object?>
        {
            { "ColumnA", Generate.ScalarValue() },
            { "ColumnB", Generate.ScalarValue() },
            { "ColumnC", Generate.ScalarValue() }
        };

        dynamic dataRow = new DataRow(dictionary);

        ((Object)dataRow.ColumnA)
            .Should().Be(dictionary["ColumnA"]);

        ((Object)dataRow.ColumnB)
            .Should().Be(dictionary["ColumnB"]);

        ((Object)dataRow.ColumnC)
            .Should().Be(dictionary["ColumnC"]);

        var newValueA = Generate.ScalarValue();
        dataRow.ColumnA = newValueA;

        ((Object)dataRow.ColumnA)
            .Should().Be(newValueA);

        var newValueB = Generate.ScalarValue();
        dataRow.ColumnB = newValueB;

        ((Object)dataRow.ColumnB)
            .Should().Be(newValueB);

        var newValueC = Generate.ScalarValue();
        dataRow.ColumnC = newValueC;

        ((Object)dataRow.ColumnC)
            .Should().Be(newValueC);
    }

    [Fact]
    public void ShouldForwardAllMethodCallsToDictionary()
    {
        var exceptions = new HashSet<String>
        {
            nameof(IDictionary<,>.TryGetValue)
        };

        var fixture = new Fixture();
        fixture.Customize(new AutoNSubstituteCustomization());
        fixture.Register(() => new DataTable());

        var dictionary = Substitute.For<IDictionary<String, Object?>>();
        var dataRow = new DataRow(dictionary);

        DecoratorAssertions.AssertDecoratorForwardsAllCalls(
            fixture,
            dataRow,
            dictionary,
            exceptions
        );
    }

    [Fact]
    public void ShouldProvideRowData()
    {
        var dictionary = new Dictionary<String, Object?>
        {
            { "ColumnA", Generate.ScalarValue() },
            { "ColumnB", Generate.ScalarValue() },
            { "ColumnC", Generate.ScalarValue() }
        };

        dynamic dataRow = new DataRow(dictionary);

        ((Object)dataRow.ColumnA)
            .Should().Be(dictionary["ColumnA"]);

        ((Object)dataRow.ColumnB)
            .Should().Be(dictionary["ColumnB"]);

        ((Object)dataRow.ColumnC)
            .Should().Be(dictionary["ColumnC"]);
    }

    [Fact]
    public void TryGetValue_ShouldForwardCallToDictionary()
    {
        var key = Generate.Single<String>();
        var value = Generate.ScalarValue();

        var dictionary = Substitute.For<IDictionary<String, Object?>>();

        dictionary.TryGetValue(key, out Arg.Any<Object?>()).Returns(a =>
            {
                a[1] = value;
                return true;
            }
        );

        var dataRow = new DataRow(dictionary);

        dataRow.TryGetValue(key, out var result)
            .Should().BeTrue();

        result
            .Should().Be(value);

        dictionary.Received().TryGetValue(key, out Arg.Any<Object?>());
    }
}
