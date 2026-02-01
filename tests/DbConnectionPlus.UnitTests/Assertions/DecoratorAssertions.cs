#pragma warning disable NS5000, NS1001, NS1000

using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Assertions;

/// <summary>
/// Provides methods for asserting types that implement the decorator pattern.
/// </summary>
public static class DecoratorAssertions
{
    /// <summary>
    /// <para>
    /// Asserts that <paramref name="decorator" /> forwards all calls to <paramref name="decorated" />, meaning each
    /// public instance method of <paramref name="decorator" /> calls the respective method of
    /// <paramref name="decorated" /> with the same arguments and that the methods of <paramref name="decorator" />
    /// return the return values of the called methods of <paramref name="decorated" />.
    /// </para>
    /// <para>Methods specified in <paramref name="excludedMethods" /> are excluded from the assertion.</para>
    /// </summary>
    /// <typeparam name="TDecorator">The type of decorator and decorated.</typeparam>
    /// <param name="fixture">The fixture to use to create sample method arguments and return values.</param>
    /// <param name="decorator">The decorator instance to test.</param>
    /// <param name="decorated">The decorated instance to compare against.</param>
    /// <param name="excludedMethods">The methods to exclude from the assertion.</param>
    public static void AssertDecoratorForwardsAllCalls<TDecorator>(
        Fixture fixture,
        TDecorator decorator,
        TDecorator decorated,
        HashSet<String> excludedMethods
    )
        where TDecorator : class
    {
        var decoratorType = typeof(TDecorator);
        var decoratorMethods = decoratorType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(a => !excludedMethods.Contains(a.Name))
            .ToList();

        foreach (var method in decoratorMethods)
        {
            try
            {
                var methodParameters = method.GetParameters();

                var decoratorMethodArguments = new Object?[methodParameters.Length];

                for (var i = 0; i < methodParameters.Length; i++)
                {
                    var methodParameter = methodParameters[i];

                    if (methodParameter.IsOut)
                    {
                        decoratorMethodArguments[i] = null;
                    }
                    else
                    {
                        // Create a sample argument for the method parameter:
                        decoratorMethodArguments[i] = specimenFactoryCreateMethod
                            .MakeGenericMethod(methodParameter.ParameterType)
                            .Invoke(null, [fixture]);
                    }
                }

                Object? decoratedMethodReturnValue = null;

                if (method.ReturnType != typeof(void))
                {
                    // Create a sample return value to return from the decorated method:
                    decoratedMethodReturnValue = specimenFactoryCreateMethod
                        .MakeGenericMethod(method.ReturnType)
                        .Invoke(null, [fixture]);

                    method.Invoke(decorated, decoratorMethodArguments).Returns(decoratedMethodReturnValue);
                }

                var decoratorMethodReturnValue = method.Invoke(decorator, decoratorMethodArguments);

                if (method.ReturnType != typeof(void))
                {
                    // Make sure the decorator method returned the same value as the decorated method:
                    decoratorMethodReturnValue
                        .Should().Be(decoratedMethodReturnValue);
                }

                // Make sure the decorated method was called with the same arguments as the decorator method:
                method.Invoke(decorated.Received(), decoratorMethodArguments);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException targetInvocationException)
                {
                    ex = targetInvocationException.InnerException!;
                }

                throw new(
                    $"""
                     The forward call assertion failed for the following method:
                     Type: {decoratorType.FullName}
                     Method: {method}

                     Failure:
                     {ex}
                     """
                );
            }
        }
    }

    /// <summary>
    /// The <see cref="SpecimenFactory.Create{T}(AutoFixture.Kernel.ISpecimenBuilder)" /> method.
    /// </summary>
    private static readonly MethodInfo specimenFactoryCreateMethod = typeof(SpecimenFactory)
        .GetMethod(
            nameof(SpecimenFactory.Create),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(ISpecimenBuilder)]
        )!;
}
