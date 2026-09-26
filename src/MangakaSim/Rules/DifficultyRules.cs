namespace MangakaSim.Rules;

/// <summary>What the two difficulty dials (0 low, 1 standard, 2 high) change. Standard is always 1 or the base value.</summary>
public static class DifficultyRules
{
    // Recovery grace: rent grace and how long editors wait before warning and cancelling.
    public static double RecoveryFactor(int recovery) => new[] { .75, 1, 1.5 }[recovery];
    // Chapters a new serialization runs before rankings can hurt it.
    public static int GraceChapters(int recovery) => new[] { 4, 6, 9 }[recovery];
    // Business pressure: optional costs, rival series strength in reader surveys and pitch odds.
    public static double CostFactor(int pressure) => new[] { .85, 1, 1.15 }[pressure];
    public static double RivalStrength(int pressure) => new[] { .9, 1, 1.1 }[pressure];
    public static double PitchFactor(int pressure) => new[] { 1.15, 1, .85 }[pressure];
    // Short clocks round down and long clocks round up, so the dial changes even the two-issue newcomer warning.
    public static (int Warning, int Cancel, int StrikeLifetime) Clocks((int Warning, int Cancel, int StrikeLifetime) clocks, int recovery) =>
        (Scale(clocks.Warning, recovery), Scale(clocks.Cancel, recovery), clocks.StrikeLifetime);
    private static int Scale(int issues, int recovery)
    {
        var scaled = issues * RecoveryFactor(recovery);
        return Math.Max(1, (int)(recovery < 1 ? Math.Floor(scaled) : Math.Ceiling(scaled)));
    }
}
