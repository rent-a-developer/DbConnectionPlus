// ReSharper disable UnusedParameter.Local

namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class DbConnectionExtensions_ParameterTests : UnitTestsBase
{
    [Fact]
    public void Parameter_ShouldInferParameterNameFromValueExpressionIfPossible()
    {
        var productId = Generate.Id();
        static Int64 GetProductId() => Generate.Id();
#pragma warning disable RCS1163 // Unused parameter
        static Int64 GetProductIdByCategory(String category) => Generate.Id();
#pragma warning restore RCS1163 // Unused parameter
        var productIds = Generate.Ids().ToArray();

        Parameter(productId).InferredName
            .Should().Be("ProductId");

        Parameter(GetProductId()).InferredName
            .Should().Be("ProductId");

        Parameter(GetProductIdByCategory("Shoes")).InferredName
            .Should().Be("ProductIdByCategoryShoes");

        Parameter(productIds[1]).InferredName
            .Should().Be("ProductIds1");

        Parameter(TestProductId).InferredName
            .Should().Be("TestProductId");

        Parameter(new { }).InferredName
            .Should().BeNull();
    }

    [Fact]
    public void Parameter_ShouldReturnInterpolatedParameter()
    {
        var value = Generate.ScalarValue();

        var interpolatedParameter = Parameter(value);

        interpolatedParameter.InferredName
            .Should().Be("Value");

        interpolatedParameter.Value
            .Should().Be(value);
    }

    [Fact]
    public void Parameter_ShouldTruncateInferredParameterName()
    {
        // ReSharper disable once InconsistentNaming
        const Int32 longname_1234567890_1234567890_1234567890_1234567890_1234567890_1234567890 = 1;

        Parameter(longname_1234567890_1234567890_1234567890_1234567890_1234567890_1234567890).InferredName
            .Should().HaveLength(60)
            .And.Be("Longname_1234567890_1234567890_1234567890_1234567890_1234567");
    }

    private const Int64 TestProductId = 106L;
}
