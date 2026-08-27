using System.Dynamic;
using System.Linq.Expressions;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using RentADeveloper.DbConnectionPlus.UnitTests.Assertions;
using DataRow = RentADeveloper.DbConnectionPlus.Dynamic.DataRow;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Dynamic;

public class DataRowTests : UnitTestsBase
{
    [Fact]
    public void ShouldAllowDynamicMemberAccess()
    {
        var dictionary = new Dictionary<string, object?>
        {
            { "ColumnA", Generate.ScalarValue() },
            { "ColumnB", Generate.ScalarValue() },
        };

        dynamic dataRow = new DataRow(dictionary);

        ((object?)dataRow.ColumnA).Should().Be(dictionary["ColumnA"]);

        ((object?)dataRow.ColumnB).Should().Be(dictionary["ColumnB"]);
    }

    [Fact]
    public void ShouldAllowDynamicMemberAssignment()
    {
        var dictionary = new Dictionary<string, object?> { { "ColumnA", Generate.ScalarValue() } };

        var dataRow = new DataRow(dictionary);
        dynamic dynamicDataRow = dataRow;

        var newValue = Generate.ScalarValue();
        dynamicDataRow.ColumnA = newValue;

        dataRow["ColumnA"].Should().Be(newValue);

        dictionary["ColumnA"].Should().Be(newValue);
    }

    [Fact]
    public void ShouldAllowDynamicMemberAssignmentOfUnknownColumn()
    {
        var dataRow = new DataRow(new Dictionary<string, object?>());
        dynamic dynamicDataRow = dataRow;

        var value = Generate.ScalarValue();
        dynamicDataRow.NewColumn = value;

        dataRow["NewColumn"].Should().Be(value);
    }

    [Fact]
    public void ShouldBeMutable()
    {
        var dictionary = new Dictionary<string, object?>
        {
            { "ColumnA", Generate.ScalarValue() },
            { "ColumnB", Generate.ScalarValue() },
            { "ColumnC", Generate.ScalarValue() },
        };

        var dataRow = new DataRow(dictionary);

        dataRow["ColumnA"].Should().Be(dictionary["ColumnA"]);

        dataRow["ColumnB"].Should().Be(dictionary["ColumnB"]);

        dataRow["ColumnC"].Should().Be(dictionary["ColumnC"]);

        var newValueA = Generate.ScalarValue();
        dataRow["ColumnA"] = newValueA;

        dataRow["ColumnA"].Should().Be(newValueA);

        var newValueB = Generate.ScalarValue();
        dataRow["ColumnB"] = newValueB;

        dataRow["ColumnB"].Should().Be(newValueB);

        var newValueC = Generate.ScalarValue();
        dataRow["ColumnC"] = newValueC;

        dataRow["ColumnC"].Should().Be(newValueC);
    }

    [Fact]
    public void ShouldForwardAllMethodCallsToDictionary()
    {
        var exceptions = new HashSet<string> { nameof(IDictionary<,>.TryGetValue) };

        var fixture = new Fixture();
        fixture.Customize(new AutoNSubstituteCustomization());
        fixture.Register(() => new DataTable());

        var dictionary = Substitute.For<IDictionary<string, object?>>();
        var dataRow = new DataRow(dictionary);

        DecoratorAssertions.AssertDecoratorForwardsAllCalls(fixture, dataRow, dictionary, exceptions);
    }

    [Fact]
    public void ShouldProvideDynamicMemberNames()
    {
        var dictionary = new Dictionary<string, object?>
        {
            { "ColumnA", Generate.ScalarValue() },
            { "ColumnB", Generate.ScalarValue() },
        };

        IDynamicMetaObjectProvider dataRow = new DataRow(dictionary);

        var metaObject = dataRow.GetMetaObject(Expression.Constant(dataRow));

        metaObject.GetDynamicMemberNames().Should().BeEquivalentTo("ColumnA", "ColumnB");
    }

    [Fact]
    public void ShouldProvideRowData()
    {
        var dictionary = new Dictionary<string, object?>
        {
            { "ColumnA", Generate.ScalarValue() },
            { "ColumnB", Generate.ScalarValue() },
            { "ColumnC", Generate.ScalarValue() },
        };

        var dataRow = new DataRow(dictionary);

        dataRow["ColumnA"].Should().Be(dictionary["ColumnA"]);

        dataRow["ColumnB"].Should().Be(dictionary["ColumnB"]);

        dataRow["ColumnC"].Should().Be(dictionary["ColumnC"]);
    }

    [Fact]
    public void ShouldResolveDynamicMethodCallsToOwnMembers()
    {
        dynamic dataRow = new DataRow(new Dictionary<string, object?> { { "ColumnA", Generate.ScalarValue() } });

        ((bool)dataRow.ContainsKey("ColumnA")).Should().BeTrue();

        ((bool)dataRow.ContainsKey("ColumnB")).Should().BeFalse();
    }

    [Fact]
    public void ShouldResolveDynamicPropertyAccessToColumnsAndNotToOwnProperties()
    {
        dynamic dataRow = new DataRow(new Dictionary<string, object?> { { "ColumnA", Generate.ScalarValue() } });

        // "Count" is a property of DataRow, but through a dynamic reference it addresses a column of that name.
        Invoking(() => (object?)dataRow.Count).Should().Throw<KeyNotFoundException>();

        dynamic rowWithShadowingColumn = new DataRow(new Dictionary<string, object?> { { "Count", 42 } });

        ((object?)rowWithShadowingColumn.Count).Should().Be(42);
    }

    [Fact]
    public void ShouldThrowWhenDynamicallyReadingUnknownColumn()
    {
        dynamic dataRow = new DataRow(new Dictionary<string, object?>());

        Invoking(() => (object?)dataRow.UnknownColumn).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryGetValue_ShouldForwardCallToDictionary()
    {
        var key = Generate.Single<string>();
        var value = Generate.ScalarValue();

        var dictionary = Substitute.For<IDictionary<string, object?>>();

        dictionary
            .TryGetValue(key, out Arg.Any<object?>())
            .Returns(a =>
            {
                a[1] = value;
                return true;
            });

        var dataRow = new DataRow(dictionary);

        dataRow.TryGetValue(key, out var result).Should().BeTrue();

        result.Should().Be(value);

        dictionary.Received().TryGetValue(key, out Arg.Any<object?>());
    }
}
