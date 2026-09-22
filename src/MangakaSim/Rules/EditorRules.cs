namespace MangakaSim;

public static class EditorRules
{
    public const int AlwaysApproveOnSubmission = 3;

    private static readonly double[] T0 = { 65, 55, 45 };
    private static readonly double[] T100 = { 50, 40, 30 };

    public static double Threshold(int tier, double effectiveRep)
    {
        if (tier is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
        var t = Math.Clamp(effectiveRep, 0, 100) / 100.0;
        return T0[tier - 1] + (T100[tier - 1] - T0[tier - 1]) * t;
    }

    /// <summary>50% at the threshold, 95% twenty points above, 5% twenty points below.</summary>
    public static double ApproveChance(double nameQuality, double threshold) =>
        Math.Clamp(0.5 + 0.0225 * (nameQuality - threshold), 0.05, 0.95);

    public static int ReviewHours(int tier) => tier switch
    {
        1 => 48,
        2 => 36,
        3 => 24,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };
}
