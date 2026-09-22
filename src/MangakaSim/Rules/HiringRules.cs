namespace MangakaSim;

public static class HiringRules
{
    public const int PoolSize = 4;
    public const int MaxLevel = 75;
    public const int NameSkillPenalty = 10;
    public const int ReturnAfterMonths = 6;

    /// <summary>A candidate's overall level: 25..45 plus a quarter of the studio's track record, capped at 75.</summary>
    public static int Level(double trackRecord, Rng rng) =>
        Math.Min(MaxLevel, rng.NextInt(25, 45) + (int)Math.Round(trackRecord / 4, MidpointRounding.AwayFromZero));

    /// <summary>Each stage skill is the level plus a draw in -15..15, clamped to 5..95; Name is ten lower.</summary>
    public static Dictionary<Stage, int> Skills(int level, Rng rng)
    {
        var skills = new Dictionary<Stage, int>();
        foreach (var stage in StageOrder.All)
        {
            var value = level + rng.NextInt(-15, 15) - (stage == Stage.Name ? NameSkillPenalty : 0);
            skills[stage] = Math.Clamp(value, 5, 95);
        }
        return skills;
    }

    /// <summary>60 at the asking salary, 20 points per 100% above or below, clamped.</summary>
    public static double StartingHappiness(int salary, int asking) =>
        Math.Clamp(60 + 20 * ((double)salary / asking - 1), 0, 100);
}
