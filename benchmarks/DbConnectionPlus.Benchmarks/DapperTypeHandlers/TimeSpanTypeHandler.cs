namespace RentADeveloper.DbConnectionPlus.Benchmarks.DapperTypeHandlers;

public class TimeSpanTypeHandler : SqlMapper.StringTypeHandler<TimeSpan>
{
    /// <inheritdoc />
    protected override String Format(TimeSpan xml) =>
        xml.ToString();

    /// <inheritdoc />
    protected override TimeSpan Parse(String xml) =>
        TimeSpan.Parse(xml);
}
