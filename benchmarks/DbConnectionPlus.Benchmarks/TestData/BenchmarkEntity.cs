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
    public Boolean BooleanValue { get; set; }
    public Byte[] BytesValue { get; set; } = null!;
    public Byte ByteValue { get; set; }
    public Char CharValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public Decimal DecimalValue { get; set; }
    public Double DoubleValue { get; set; }
    public TestEnum EnumValue { get; set; }

    [System.ComponentModel.DataAnnotations.Key]
    public Int64 Id { get; set; }

    public Int16 Int16Value { get; set; }
    public Int32 Int32Value { get; set; }
    public Int64 Int64Value { get; set; }

    public Single SingleValue { get; set; }
    public String StringValue { get; set; } = null!;
}
