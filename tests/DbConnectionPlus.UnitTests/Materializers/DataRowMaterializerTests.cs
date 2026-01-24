using RentADeveloper.DbConnectionPlus.Materializers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Materializers;

public class DataRowMaterializerTests : UnitTestsBase
{
    [Fact]
    public void Materialize_ReturnsDataRowWithAllColumnsAndValues()
    {
        var value1 = Generate.ScalarValue();
        var value2 = Generate.ScalarValue();
        var value3 = Generate.ScalarValue();

        var dataReader = Substitute.For<DbDataReader>();

        dataReader.FieldCount.Returns(3);

        dataReader.GetName(0).Returns("ColumnA");
        dataReader.GetName(1).Returns("ColumnB");
        dataReader.GetName(2).Returns("ColumnC");

        dataReader
            .GetValues(Arg.Any<Object[]>())
            .Returns(callInfo =>
                {
                    var array = callInfo.Arg<Object[]>();
                    array[0] = value1;
                    array[1] = value2;
                    array[2] = value3;
                    return 3;
                }
            );

        var dataRow = DataRowMaterializer.Materialize(dataReader);

        dataRow
            .Should().Contain("ColumnA", value1);

        dataRow
            .Should().Contain("ColumnB", value2);

        dataRow
            .Should().Contain("ColumnC", value3);
    }

    [Fact]
    public void ShouldGuardAgainstNullArguments() =>
        ArgumentNullGuardVerifier.Verify(() =>
            DataRowMaterializer.Materialize(Substitute.For<DbDataReader>())
        );
}
