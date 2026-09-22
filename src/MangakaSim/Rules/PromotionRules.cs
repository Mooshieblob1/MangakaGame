namespace MangakaSim;

/// <summary>Idle staff online push doujin series: fans per idle hour.</summary>
public static class PromotionRules
{
    public const int WholeStudio = 0;

    public static double FansPerHour(double meanSkill, double reach) => 2 * (1 + meanSkill / 100.0) * reach;
}
