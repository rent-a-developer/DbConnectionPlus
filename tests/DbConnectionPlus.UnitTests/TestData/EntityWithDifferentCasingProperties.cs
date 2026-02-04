// ReSharper disable InconsistentNaming

namespace RentADeveloper.DbConnectionPlus.UnitTests.TestData;

public record EntityWithDifferentCasingProperties
{
    public Byte[] BytesVALUE { get; set; } = null!;
    public Boolean BooleanVALUE { get; set; }
    public Byte ByteVALUE { get; set; }
    public Char CharVALUE { get; set; }
    public DateOnly DateOnlyVALUE { get; set; }
    public DateTime DateTimeVALUE { get; set; }
    public Decimal DecimalVALUE { get; set; }
    public Double DoubleVALUE { get; set; }
    public TestEnum EnumVALUE { get; set; }
    public Guid GuidVALUE { get; set; }

    [Key]
    public Int64 Id { get; set; }

    public Int16 Int16VALUE { get; set; }
    public Int32 Int32VALUE { get; set; }
    public Int64 Int64VALUE { get; set; }

    [NotMapped]
    public String? NotMappedProperty { get; set; }

    public Single SingleVALUE { get; set; }
    public String StringVALUE { get; set; } = null!;
    public TimeOnly TimeOnlyVALUE { get; set; }
    public TimeSpan TimeSpanVALUE { get; set; }
}
