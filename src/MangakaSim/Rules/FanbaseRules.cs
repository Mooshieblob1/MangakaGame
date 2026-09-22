namespace MangakaSim;

public static class FanbaseRules
{
    public const double IssueChurn = 0.995;
    public const double MissChurn = 0.97;
    public const double WithdrawChurn = 0.9;
    public const double IconicRankFactor = 1.5;
    public const double VolumeFanShare = 0.05;
    public const double DoujinFanShare = 0.30;
    public const double IconicImpact = 90;
    public const double IconicFanbase = 1_000_000;

    public static double BaseReaders(int tier) => tier switch
    {
        1 => 3000,
        2 => 1500,
        3 => 800,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };

    /// <summary>Piecewise linear: rank 1 -> 3.0, the line -> 0.5, the roster -> 0.2, below the roster 0.2.</summary>
    public static double RankFactor(int rank, int line, int roster)
    {
        if (rank <= 1) return 3.0;
        if (rank >= roster) return 0.2;
        if (rank <= line)
        {
            var t = (rank - 1.0) / (line - 1.0);
            return 3.0 + (0.5 - 3.0) * t;
        }
        var u = (rank - (double)line) / (roster - (double)line);
        return 0.5 + (0.2 - 0.5) * u;
    }

    public static double IssueGain(int tier, double rankFactor, int quality) =>
        BaseReaders(tier) * rankFactor * quality / 70.0;
}
