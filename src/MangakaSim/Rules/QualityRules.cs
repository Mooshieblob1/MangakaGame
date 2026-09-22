namespace MangakaSim;

public static class QualityRules
{
    public static double Weight(Stage stage) => stage switch
    {
        Stage.Name => 0.35,
        Stage.Pencils => 0.30,
        Stage.Inks => 0.15,
        Stage.Backgrounds => 0.10,
        Stage.Tones => 0.10,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null),
    };

    /// <summary>
    /// Piecewise linear: skill 0 -> 0.2, 50 -> 0.6, 100 -> 1.0. Redo bonus +0.05 per redo, capped at 1.0.
    /// Fatigue lowers the skill first (sub-project 3): 100 fatigue removes 30% of it.
    /// </summary>
    public static double SkillFactor(double skill, int redoCount = 0, double fatigue = 0)
    {
        var s = Math.Clamp(FatigueRules.EffectiveSkill(skill, fatigue), 0, 100);
        var factor = s <= 50 ? 0.2 + 0.4 * (s / 50.0) : 0.6 + 0.4 * ((s - 50) / 50.0);
        return Math.Min(1.0, factor + 0.05 * Math.Max(0, redoCount));
    }

    /// <summary>Hours-weighted mean of a per-person value; the fallback when nobody has logged hours.</summary>
    public static double WeightedSkill(IReadOnlyDictionary<int, double> hoursByPerson, Func<int, double> valueOf, double fallback)
    {
        var total = hoursByPerson.Values.Sum();
        if (total <= 0) return fallback;
        return hoursByPerson.Sum(kv => kv.Value * valueOf(kv.Key)) / total;
    }

    /// <summary>Overtime costs 5% per 10% of the stage's hours, floor 0.7.</summary>
    public static double RushFactor(double overtimeHours, double hoursRequired)
    {
        if (hoursRequired <= 0) return 1.0;
        return Math.Max(0.7, 1.0 - 0.5 * overtimeHours / hoursRequired);
    }

    /// <summary>Quality points a completed stage contributes. The redo bonus applies to Name only.</summary>
    public static double Contribution(Stage stage, double skill, double overtimeHours, double hoursRequired, int redoCount, double fatigue = 0)
    {
        var skillFactor = SkillFactor(skill, stage == Stage.Name ? redoCount : 0, fatigue);
        return Weight(stage) * 100.0 * skillFactor * RushFactor(overtimeHours, hoursRequired);
    }

    public static int Quality(IEnumerable<double> contributions) =>
        (int)Math.Clamp(Math.Round(contributions.Sum(), MidpointRounding.AwayFromZero), 0, 100);

    /// <summary>The Name stage's contribution scaled to 0..100.</summary>
    public static double NameQuality(double nameContribution) => nameContribution / Weight(Stage.Name);
}
