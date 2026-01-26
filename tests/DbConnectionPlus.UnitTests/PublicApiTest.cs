using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using PublicApiGenerator;

namespace RentADeveloper.DbConnectionPlus.UnitTests;

public class PublicApiTest : UnitTestsBase
{
    [Fact]
    [Description("Verifies that the public API of DbConnectionPlus has not been changed unnoticed.")]
    public Task PublicApiHasNotChanged()
    {
        var apiGeneratorOptions = new ApiGeneratorOptions
        {
            ExcludeAttributes =
            [
                typeof(InternalsVisibleToAttribute).FullName!,
                typeof(TargetFrameworkAttribute).FullName!,
                typeof(AsyncIteratorStateMachineAttribute).FullName!
            ],
            DenyNamespacePrefixes = [],
            TreatRecordsAsClasses = false
        };

        var publicApi = typeof(DbConnectionExtensions).Assembly.GeneratePublicApi(apiGeneratorOptions);

        return Verify(publicApi);
    }
}
