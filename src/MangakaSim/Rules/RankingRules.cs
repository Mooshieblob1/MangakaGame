namespace MangakaSim;

public static class RankingRules
{
    public static double K(int tier) => tier switch
    {
        1 => 200_000,
        2 => 100_000,
        3 => 50_000,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };

    public static double FanScore(double fanbase, int tier) => 100.0 * fanbase / (fanbase + K(tier));

    public static double PlayerScore(int quality, double fanbase, int tier, double affinity, double genreTrend, double crowding) =>
        (0.6 * quality + 0.4 * FanScore(fanbase, tier)) * affinity * genreTrend * crowding;

    public static double FillerScore(double popularity, double crowding) => popularity * crowding;
}
