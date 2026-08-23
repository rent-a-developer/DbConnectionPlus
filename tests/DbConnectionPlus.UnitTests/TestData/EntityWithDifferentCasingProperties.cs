// ReSharper disable InconsistentNaming

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithDifferentCasingProperties
{
    public bool BooleanVALUE { get; set; }
    public byte[] BytesVALUE { get; set; } = null!;
    public byte ByteVALUE { get; set; }
    public char CharVALUE { get; set; }
    public DateOnly DateOnlyVALUE { get; set; }
    public DateTime DateTimeVALUE { get; set; }
    public decimal DecimalVALUE { get; set; }
    public double DoubleVALUE { get; set; }
    public TestEnum EnumVALUE { get; set; }
    public Guid GuidVALUE { get; set; }

    [Key]
    public long Id { get; set; }

    public short Int16VALUE { get; set; }
    public int Int32VALUE { get; set; }
    public long Int64VALUE { get; set; }

    [NotMapped]
    public string? NotMappedProperty { get; set; }

    public bool? NullableBooleanVALUE { get; set; }
    public float SingleVALUE { get; set; }
    public string StringVALUE { get; set; } = null!;
    public TimeOnly TimeOnlyVALUE { get; set; }
    public TimeSpan TimeSpanVALUE { get; set; }
}
