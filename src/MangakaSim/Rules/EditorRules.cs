namespace MangakaSim.Rules;

public static class EditorRules
{
    public static int Delay(int tier) => tier switch { 1 => 48, 2 => 36, 3 => 24, _ => throw new ArgumentOutOfRangeException(nameof(tier)) };
    public static double Threshold(int tier, double reputation) =>
        (tier switch { 1 => 65, 2 => 55, 3 => 45, _ => throw new ArgumentOutOfRangeException(nameof(tier)) }) - .15 * reputation;
    public static double Chance(int tier, double nameQuality, double reputation) =>
        Math.Clamp(.5 + .0225 * (nameQuality - Threshold(tier, reputation)), .05, .95);
}
