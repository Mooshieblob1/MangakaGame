namespace MangakaSim;

public static class SalesRules
{
    public const int TankobonWeeks = 52;
    public const int TankobonCover1996 = 400;
    public const int DoujinCover1996 = 500;
    public const double RoyaltyRate = 0.10;
    public const double DoujinShare = 0.60;
    public const int ReleaseDelayWeeks = 6;
    public const int MinLeftoverForFinalVolume = 3;
    public const int DoujinChaptersPerVolume = 5;
    public const long Milestone100k = 100_000;
    public const long Milestone1M = 1_000_000;

    /// <summary>Copies of a tankobon in its given sales week (1-based), floored.</summary>
    public static long TankobonCopies(int week, double fanbase, double averageQuality, double genreTrend)
    {
        var qf = averageQuality / 70.0;
        var copies = week == 1
            ? fanbase * 0.6 * qf * genreTrend
            : fanbase * 0.04 * qf * Math.Pow(0.93, week - 2);
        return (long)Math.Floor(Math.Max(0, copies));
    }

    public static int DoujinWindow(bool online) => online ? 8 : 4;

    /// <summary>Copies of a doujin volume in its given sales week (1-based), floored.</summary>
    public static long DoujinCopies(int week, double fanbase, double averageQuality, double genreTrend, double reach)
    {
        var qf = averageQuality / 70.0;
        var week1 = (200 + fanbase * 0.3) * qf * genreTrend * (1 + 0.5 * reach);
        var copies = week == 1 ? week1 : week1 * 0.4;
        return (long)Math.Floor(Math.Max(0, copies));
    }

    public static double TankobonCover(double priceIndex) => TankobonCover1996 * priceIndex;
    public static double DoujinCover(double priceIndex) => DoujinCover1996 * priceIndex;

    public static long Royalty(long copies, double cover) => (long)Math.Round(copies * cover * RoyaltyRate, MidpointRounding.AwayFromZero);
    public const int PrintCost1996 = 120;

    /// <summary>The studio keeps 60% of the cover, less the per-copy printing cost (sub-project 3).</summary>
    public static long DoujinIncome(long copies, double cover, double printCost) =>
        (long)Math.Round(copies * (cover * DoujinShare - printCost), MidpointRounding.AwayFromZero);

    public static double PrintCost(double priceIndex) => PrintCost1996 * priceIndex;
}
