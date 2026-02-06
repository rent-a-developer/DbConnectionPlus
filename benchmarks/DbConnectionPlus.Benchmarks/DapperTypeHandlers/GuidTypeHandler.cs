using Dapper;

namespace RentADeveloper.DbConnectionPlus.Benchmarks.DapperTypeHandlers;

public class GuidTypeHandler : SqlMapper.StringTypeHandler<Guid>
{
    /// <inheritdoc />
    protected override Guid Parse(String xml) =>
        Guid.Parse(xml);

    /// <inheritdoc />
    protected override String Format(Guid xml) =>
        xml.ToString();
}
