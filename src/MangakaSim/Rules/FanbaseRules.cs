namespace MangakaSim.Rules;

public static class FanbaseRules
{
    public static double RankFactor(int rank, int line, int roster) => rank <= line
        ? 3 - 2.5 * (Math.Max(1, rank) - 1) / (line - 1)
        : Math.Max(.2, .5 - .3 * (rank - line) / (roster - line));
    public static double Gain(int tier, double quality, double rankFactor) =>
        (tier switch { 1 => 3000, 2 => 1500, 3 => 800, _ => throw new ArgumentOutOfRangeException(nameof(tier)) }) * rankFactor * quality / 70;
    public static double AfterPublication(double fans, int tier, int quality, int rank, int line, int roster, bool iconic) =>
        fans * (iconic ? 1 : .995) + Gain(tier, quality, iconic ? 1.5 : RankFactor(rank, line, roster));
}
