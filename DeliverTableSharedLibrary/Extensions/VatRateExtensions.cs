namespace DeliverTableSharedLibrary.Extensions;

using DeliverTableSharedLibrary.Enums;

public static class VatRateExtensions
{
    public static decimal ToDecimal(this VatRate rate) => rate switch
    {
        VatRate.Zero => 0m,
        VatRate.Special2_1 => 2.1m,
        VatRate.Reduced5_5 => 5.5m,
        VatRate.Intermediate10 => 10m,
        VatRate.Normal20 => 20m,
        _ => throw new ArgumentOutOfRangeException(nameof(rate), rate, "Unknown VAT rate"),
    };
}
