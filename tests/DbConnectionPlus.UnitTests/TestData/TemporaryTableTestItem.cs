namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public class TemporaryTableTestItem
{
    public bool Boolean { get; set; }
    public byte[] Bytes { get; set; } = null!;
    public char Char { get; set; }
    public DateOnly DateOnly { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Decimal { get; set; }
    public double Double { get; set; }
    public Guid Guid { get; set; }
    public short Int16 { get; set; }
    public int Int32 { get; set; }
    public long Int64 { get; set; }
    public float Single { get; set; }
    public string String { get; set; } = null!;
    public TimeOnly TimeOnly { get; set; }
    public TimeSpan TimeSpan { get; set; }
}
