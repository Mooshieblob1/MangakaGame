namespace MangakaSim;

public static class ReputationRules
{
    public const int ProperEndingChapters = 12;
    public const int GraceChapters = 8;

    // Studio track record deltas.
    public const double Rank1 = 0.5;
    public const double Rank2To3 = 0.3;
    public const double Volume100k = 3;
    public const double Volume1M = 10;
    public const double DoujinQualityVolume = 0.5;
    public const double RejectedGoodPitch = 0.25;
    public const double MissedIssue = -1;
    public const double EditorRedo = -0.25;
    public const double Cancellation = -8;

    // Personal reputation deltas.
    public const double PersonTop3 = 0.5;
    public const double PersonRedo = -0.5;
    public const double PersonCancellation = -2;

    /// <summary>Weighted mean of the top three reputations, weights 0.5/0.3/0.2 renormalised to those present.</summary>
    public static double StaffTerm(IEnumerable<double> reputations)
    {
        var top = reputations.OrderByDescending(r => r).Take(3).ToList();
        if (top.Count == 0) return 0;
        var weights = new[] { 0.5, 0.3, 0.2 };
        var total = weights.Take(top.Count).Sum();
        return top.Select((r, i) => r * weights[i]).Sum() / total;
    }

    public static double Effective(double trackRecord, double staffTerm) => 0.5 * trackRecord + 0.5 * staffTerm;

    public static double Protection(int chaptersPublished, double fanbase, double culturalImpact) =>
        Math.Clamp(0.4 * Math.Min(chaptersPublished, 300) / 300.0
                   + 0.35 * fanbase / (fanbase + 500_000)
                   + 0.25 * culturalImpact / 100.0, 0, 1);

    /// <summary>Proper ending bonus, rounded to one decimal.</summary>
    public static double EndingBonus(int line, double averageRank, long totalCopies)
    {
        var rankTerm = Math.Clamp((line - averageRank) / line, 0, 1);
        var salesTerm = Math.Min(1.0, totalCopies / 500_000.0);
        return Math.Round(1 + 4 * rankTerm * salesTerm, 1, MidpointRounding.AwayFromZero);
    }

    public static double WithdrawPenalty(int chaptersPublished) => Math.Max(-10, -3 - 0.05 * chaptersPublished);

    /// <summary>Reputation change for a contributor when a chapter completes.</summary>
    public static double ChapterCompletionDelta(int quality, double hourShare, bool doujin) =>
        (quality - 60) / 20.0 * hourShare * (doujin ? 0.5 : 1.0);

    public static double Clamp(double value) => Math.Clamp(value, 0, 100);
}
