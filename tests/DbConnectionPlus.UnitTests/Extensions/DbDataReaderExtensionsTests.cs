using RentADeveloper.DbConnectionPlus.Extensions;
using DbDataReaderExtensions = RentADeveloper.DbConnectionPlus.Extensions.DbDataReaderExtensions;

// ReSharper disable InvokeAsExtensionMethod

namespace RentADeveloper.DbConnectionPlus.UnitTests.Extensions;

public class DbDataReaderExtensionsTests : UnitTestsBase
{
    [Fact]
    public void GetFieldNames_ShouldReturnFieldNames()
    {
        String[] fieldNames = ["FieldA", "FieldB", "FieldC"];

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(3);

        dataReader.GetName(0).Returns(fieldNames[0]);
        dataReader.GetName(1).Returns(fieldNames[1]);
        dataReader.GetName(2).Returns(fieldNames[2]);

        dataReader.GetFieldNames()
            .Should().BeEquivalentTo(fieldNames);
    }

    [Fact]
    public void GetFieldTypes_ShouldReturnFieldTypes()
    {
        Type[] fieldTypes = [typeof(Int32), typeof(String), typeof(DateTime)];

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(3);

        dataReader.GetFieldType(0).Returns(fieldTypes[0]);
        dataReader.GetFieldType(1).Returns(fieldTypes[1]);
        dataReader.GetFieldType(2).Returns(fieldTypes[2]);

        dataReader.GetFieldTypes()
            .Should().BeEquivalentTo(fieldTypes);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments()
    {
        var dataReader = Substitute.For<DbDataReader>();

        ArgumentNullGuardVerifier.Verify(() => DbDataReaderExtensions.GetFieldNames(dataReader));
        ArgumentNullGuardVerifier.Verify(() => DbDataReaderExtensions.GetFieldTypes(dataReader));
    }
}
