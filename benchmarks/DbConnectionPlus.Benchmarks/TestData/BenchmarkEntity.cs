namespace RentADeveloper.DbConnectionPlus.Benchmarks.TestData;

// The entity the benchmarks read and write.
//
// There is deliberately no Guid and no TimeSpan property. SQLite stores both as TEXT and neither is
// IConvertible, so Dapper.AOT's generated row factory - which never consults the handlers registered through
// SqlMapper.AddTypeHandler - cannot map them and throws an invalid-cast error.
// See the README next to this file.
[System.ComponentModel.DataAnnotations.Schema.Table("Entity")]
public record BenchmarkEntity
{
    public bool BooleanValue { get; set; }
    public byte[] BytesValue { get; set; } = null!;
    public byte ByteValue { get; set; }
    public char CharValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public decimal DecimalValue { get; set; }
    public double DoubleValue { get; set; }
    public TestEnum EnumValue { get; set; }

    [System.ComponentModel.DataAnnotations.Key]
    public long Id { get; set; }

    public short Int16Value { get; set; }
    public int Int32Value { get; set; }
    public long Int64Value { get; set; }

    public float SingleValue { get; set; }
    public string StringValue { get; set; } = null!;
}
