using RentADeveloper.DbConnectionPlus.Extensions;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Extensions;

public class Int32ExtensionsTests : UnitTestsBase
{
    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(5, "5th")]
    [InlineData(11, "11th")]
    [InlineData(21, "21st")]
    [InlineData(22, "22nd")]
    [InlineData(23, "23rd")]
    [InlineData(24, "24th")]
    [InlineData(25, "25th")]
    public void OrdinalizeEnglish_ShouldOrdinalizeNumberInEnglishFormat(Int32 number, String expectedResult) =>
        number.OrdinalizeEnglish()
            .Should().Be(expectedResult);
}
