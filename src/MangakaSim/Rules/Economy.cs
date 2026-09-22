namespace MangakaSim;

/// <summary>Inflation. Catalog yen are 1996 prices; multiply by the index at the moment of use.</summary>
public static class Economy
{
    public const long StartingMoney = 500_000;
    public const int InternetCost1996 = 120_000;

    /// <summary>
    /// Real Japanese CPI normalised to 1996 = 1.00, then 2% per year after the last keyframe.
    /// Economic curves step by calendar year so prices are stable within a year (1996 is exactly 1.00).
    /// </summary>
    public static double PriceIndex(TrendCatalog catalog, DateTime now) =>
        Curves.Interpolate(catalog.PriceIndex, now.Year, growAfterLast: true);

    /// <summary>Online reach by calendar year, held flat after the last keyframe.</summary>
    public static double InternetReach(TrendCatalog catalog, DateTime now) =>
        Curves.Interpolate(catalog.InternetReach, now.Year, growAfterLast: false);

    /// <summary>A 1996 yen amount at today's prices, rounded to whole yen.</summary>
    public static long Inflate(long yen1996, double priceIndex) => (long)Math.Round(yen1996 * priceIndex);
}
