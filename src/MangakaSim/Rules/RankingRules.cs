namespace MangakaSim.Rules;

public static class RankingRules
{
    public static double FanScore(double fans, int tier) => 100 * (fans / (fans +
        (tier switch { 1 => 200000, 2 => 100000, 3 => 50000, _ => throw new ArgumentOutOfRangeException(nameof(tier)) })));
    // Reader surveys measure popularity, so readership outweighs craft: a skilled unknown still has to win readers.
    public const double QualityWeight = .4;
    public static double Score(int quality, double fans, int tier, double affinity, double trend, int others) =>
        (QualityWeight * quality + (1 - QualityWeight) * FanScore(fans, tier)) * affinity * trend * TrendRules.Crowding(others);
}
