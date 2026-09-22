namespace MangakaSim;

public static class PitchRules
{
    public const double MinChance = 0.02;
    public const double MaxChance = 0.95;
    public const int OneShotPages = 31;
    public const int CooldownWeeks = 26;
    public const int MinDaysBeforeDue = 14;
    public const int OfferIssueCount = 4;

    public static double Base(int tier) => tier switch
    {
        1 => 0.15,
        2 => 0.30,
        3 => 0.50,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };

    /// <summary>Linear through (50, 0.5) and (100, 1.6); not clamped below 50.</summary>
    public static double QualityFactor(int quality) => 0.5 + (quality - 50) / 50.0 * 1.1;

    /// <summary>Linear from 0.6 at reputation 0 to 1.6 at 100.</summary>
    public static double ReputationFactor(double effectiveRep) => 0.6 + Math.Clamp(effectiveRep, 0, 100) / 100.0;

    public static double Chance(int tier, int quality, double effectiveRep, double affinity, double genreTrend) =>
        Math.Clamp(Base(tier) * QualityFactor(quality) * ReputationFactor(effectiveRep) * affinity * genreTrend,
            MinChance, MaxChance);

    /// <summary>Names the smallest of the four factors for the rejection message.</summary>
    public static string WeakestFactor(int quality, double effectiveRep, double affinity, double genreTrend)
    {
        var factors = new (string Name, double Value)[]
        {
            ("quality", QualityFactor(quality)),
            ("reputation", ReputationFactor(effectiveRep)),
            ("fit", affinity),
            ("trend", genreTrend),
        };
        return factors.MinBy(f => f.Value).Name;
    }

    /// <summary>Fee per page: reputation interpolates the magazine's range, then inflation, rounded to 100 yen.</summary>
    public static int FeePerPage(Magazine magazine, double effectiveRep, double priceIndex)
    {
        var t = Math.Clamp(effectiveRep, 0, 100) / 100.0;
        var fee1996 = magazine.FeePerPageMin + (magazine.FeePerPageMax - magazine.FeePerPageMin) * t;
        return (int)(Math.Round(fee1996 * priceIndex / 100.0, MidpointRounding.AwayFromZero) * 100);
    }
}
