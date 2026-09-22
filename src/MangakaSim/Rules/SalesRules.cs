namespace MangakaSim.Rules;

public static class SalesRules
{
    public static long Copies(double value)
    {
        if (!double.IsFinite(value) || value < 0 || value >= 9223372036854775808.0) throw new OverflowException("Copy count out of range.");
        return checked((long)Math.Floor(value));
    }
    public static long CommercialCopies(double fans, double quality, double trend, int week) => week is < 1 or > 52 ? 0 :
        Copies(week == 1 ? fans * .6 * (quality / 70) * trend : fans * .04 * (quality / 70) * Math.Pow(.93, week - 2));
    public static long DoujinCopies(double fans, double quality, double trend, double reach, int week) => week < 1 ? 0 :
        Copies((200 + fans * .3) * (quality / 70) * trend * (1 + .5 * reach) * (week == 1 ? 1 : .4));
    public static long Income(long copies, double priceIndex, bool doujin) => Economy.Yen(copies * (doujin ? 500 * .6 : 400 * .1) * priceIndex);
}
