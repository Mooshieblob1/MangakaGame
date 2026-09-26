namespace MangakaSim.Rules;

public static class PitchRules
{
    public static double QualityFactor(double quality) => .5 + (quality - 50) * .022;
    public static double ReputationFactor(double reputation) => .6 + reputation / 100;
    public static double Chance(int tier, double quality, double reputation, double affinity, double trend) =>
        Math.Clamp((tier switch { 1 => .12, 2 => .24, 3 => .40, _ => throw new ArgumentOutOfRangeException(nameof(tier)) }) *
            QualityFactor(quality) * ReputationFactor(reputation) * affinity * trend, .02, .95);
    public static string WeakestFactor(double quality, double reputation, double affinity, double trend) =>
        new[] { ("quality", QualityFactor(quality)), ("reputation", ReputationFactor(reputation)),
            ("affinity", affinity), ("trend", trend) }.OrderBy(p => p.Item2).First().Item1;
}
