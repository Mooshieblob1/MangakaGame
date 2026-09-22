namespace MangakaSim.Rules;

public static class RankingRules
{
    public static double FanScore(double fans, int tier) => 100 * (fans / (fans +
        (tier switch { 1 => 200000, 2 => 100000, 3 => 50000, _ => throw new ArgumentOutOfRangeException(nameof(tier)) })));
    public static double Score(int quality, double fans, int tier, double affinity, double trend, int others) =>
        (.6 * quality + .4 * FanScore(fans, tier)) * affinity * trend * TrendRules.Crowding(others);
}
