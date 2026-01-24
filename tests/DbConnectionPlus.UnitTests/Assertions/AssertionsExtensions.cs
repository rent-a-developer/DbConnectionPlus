using AwesomeAssertions.Execution;
using AwesomeAssertions.Types;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Assertions;

/// <summary>
/// Provides extension methods for custom assertions.
/// </summary>
[CustomAssertions]
public static class AssertionsExtensions
{
    /// <summary>
    /// Asserts that the subject type is any of the specified expectation types.
    /// </summary>
    /// <param name="assertions">The type assertion to check.</param>
    /// <param name="expectations">The expected types of which the subject should be one.</param>
    [CustomAssertion]
    public static void BeAnyOf(this TypeAssertions assertions, params Type[] expectations) =>
        AssertionChain.GetOrCreate()
            .ForCondition(expectations.Contains(assertions.Subject))
            .FailWith("Expected {context} to be any of {0}, but found {1}", expectations, assertions.Subject);
}
