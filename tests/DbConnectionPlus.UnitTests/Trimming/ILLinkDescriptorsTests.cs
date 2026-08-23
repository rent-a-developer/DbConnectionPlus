using System.Xml.Linq;

namespace RentADeveloper.DbConnectionPlus.UnitTests.Trimming;

/// <summary>
/// Verifies that the trimming descriptor which keeps the members of nested value tuple types still ships
/// inside the core assembly.
/// </summary>
/// <remarks>
/// <para>
/// Materializing a value tuple with more than seven fields reaches the nested value tuple type through the
/// generic arguments of another type at run time, which <see cref="DynamicallyAccessedMembersAttribute" />
/// cannot express. Without the descriptor the trimmer removes the nested type's fields and constructor and
/// the query fails at run time - only in a trimmed or Native AOT application, so no other unit test can see
/// it, and neither can the compiler.
/// </para>
/// <para>
/// The Native AOT smoke test is what proves the descriptor actually works. This test only guards against it
/// silently disappearing - a deleted file or a dropped <c>EmbeddedResource</c> item would otherwise be
/// noticed no earlier than in a consumer's published application.
/// </para>
/// </remarks>
public class ILLinkDescriptorsTests : UnitTestsBase
{
    private const string ILLinkDescriptorsResourceName = "ILLink.Descriptors.xml";

    [Fact]
    public void CoreAssembly_ShouldEmbedTheILLinkDescriptor() =>
        typeof(DbConnectionExtensions)
            .Assembly.GetManifestResourceNames()
            .Should()
            .Contain(ILLinkDescriptorsResourceName);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void ILLinkDescriptor_ShouldPreserveAllMembersOfEveryValueTupleArity(int arity)
    {
        var preservedTypes = ReadDescriptor()
            .Descendants("type")
            .Where(a => (string?)a.Attribute("preserve") == "all")
            .Select(a => (string?)a.Attribute("fullname"))
            .ToList();

        preservedTypes.Should().Contain($"System.ValueTuple`{arity}");
    }

    private static XDocument ReadDescriptor()
    {
        using var stream = typeof(DbConnectionExtensions).Assembly.GetManifestResourceStream(
            ILLinkDescriptorsResourceName
        )!;

        return XDocument.Load(stream);
    }
}
