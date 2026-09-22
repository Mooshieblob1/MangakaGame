namespace MangakaSim;

/// <summary>Fatigue accrues with overtime and long days, recovers with rest, and lowers the skill used for quality.</summary>
public static class FatigueRules
{
    public const double MaxSkillLoss = 0.3;

    /// <summary>Daily accrual: 1.5 per overtime hour plus 0.5 per regular hour beyond eight.</summary>
    public static double Accrue(int overtimeHours, int regularHours) =>
        1.5 * overtimeHours + 0.5 * Math.Max(0, regularHours - 8);

    /// <summary>Daily recovery: 6 on a day off, 3 otherwise.</summary>
    public static double Recover(bool dayOff) => dayOff ? 6 : 3;

    public static double Clamp(double fatigue) => Math.Clamp(fatigue, 0, 100);

    /// <summary>skill x (1 - 0.3 x fatigue / 100).</summary>
    public static double EffectiveSkill(double skill, double fatigue) =>
        skill * (1 - MaxSkillLoss * Math.Clamp(fatigue, 0, 100) / 100.0);
}
