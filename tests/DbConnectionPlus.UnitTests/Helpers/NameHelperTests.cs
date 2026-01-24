using RentADeveloper.DbConnectionPlus.Helpers;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Helpers;

public class NameHelperTests : UnitTestsBase
{
    [Theory]
    [InlineData("this.GetId()", 100, "Id")]
    [InlineData("this.supplierId", 100, "SupplierId")]
    [InlineData("new Product()", 100, "Product")]
    [InlineData("supplierId", 100, "SupplierId")]
    [InlineData("this.GetOrderItems()", 100, "OrderItems")]
    [InlineData("1234567890", 5, "12345")]
    [InlineData("abc[]-=/<>", 5, "Abc")]
    [InlineData("this.GetId()", 2, "Id")]
    [InlineData("[productId]", 10, "ProductId")]
    [InlineData("", 10, "")]
    public void CreateNameFromCallerArgumentExpression_ShouldCreateName(
        String expression,
        Int32 maximumLength,
        String expectedName
    ) =>
        NameHelper.CreateNameFromCallerArgumentExpression(expression, maximumLength)
            .Should().Be(expectedName);
}
