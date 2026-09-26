namespace MangakaSim.Rules;

public static class SalesRules
{
    public static long Copies(double value)
    {
        if (!double.IsFinite(value) || value < 0 || value >= 9223372036854775808.0) throw new OverflowException("Copy count out of range.");
        return checked((long)Math.Floor(value));
    }
    // Smaller magazines put a series in front of fewer bookshop buyers.
    public static double TierDemand(int tier) => tier switch { 1 => 1, 2 => .6, 3 => .35, _ => throw new ArgumentOutOfRangeException(nameof(tier)) };
    public static long CommercialCopies(double fans, double quality, double trend, int week, int tier = 1) => week is < 1 or > 52 ? 0 :
        Copies(TierDemand(tier) * (week == 1 ? fans * .6 * (quality / 70) * trend : fans * .04 * (quality / 70) * Math.Pow(.93, week - 2)));
    public const double CommercialFanGain = .03;
    // Publishers pay royalties on copies printed: a first print run at release, then each reprint.
    public const long FirstPrintRun = 10000;
    public static long Rung(long copiesSold)
    {
        if (copiesSold < 0) throw new ArgumentOutOfRangeException(nameof(copiesSold));
        foreach (var rung in new[] { FirstPrintRun, 15000L, 20000, 30000, 40000, 50000, 70000, 100000 })
            if (copiesSold <= rung) return rung;
        var step = copiesSold <= 500000 ? 50000 : 100000;
        return checked((copiesSold + step - 1) / step * step);
    }
    public static long DoujinCopies(double fans, double quality, double trend, double reach, int week) => week < 1 ? 0 :
        Copies((200 + fans * .3) * (quality / 70) * trend * (1 + .5 * reach) * (week == 1 ? 1 : .4));
    public static long Income(long copies, double priceIndex, bool doujin) => Economy.Yen(copies * (doujin ? 500 * .6 : 400 * .1) * priceIndex);
}
