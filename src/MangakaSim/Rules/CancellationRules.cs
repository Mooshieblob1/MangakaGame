namespace MangakaSim;

public static class CancellationRules
{
    public const int StrikesForRoll = 3;

    /// <summary>Consecutive issues below the line before a warning.</summary>
    public static int WarningClock(double protection) => (int)Math.Round(3 + 9 * Math.Clamp(protection, 0, 1), MidpointRounding.AwayFromZero);

    /// <summary>Issues under warning before the cancellation roll.</summary>
    public static int CancelClock(double protection) => (int)Math.Round(3 + 23 * Math.Clamp(protection, 0, 1), MidpointRounding.AwayFromZero);

    /// <summary>Issues a strike stays live.</summary>
    public static int StrikeLifetime(double protection) => (int)Math.Round(8 - 6 * Math.Clamp(protection, 0, 1), MidpointRounding.AwayFromZero);

    /// <summary>Strikes younger than their lifetime, measured in whole issues of the magazine's cadence.</summary>
    public static List<DateTime> LiveStrikes(IEnumerable<DateTime> strikes, DateTime now, int cadenceDays, int lifetimeIssues) =>
        strikes.Where(s => Math.Floor((now - s).TotalDays / cadenceDays) < lifetimeIssues).OrderBy(s => s).ToList();

    public static double CancelChance(double effectiveRep) => 1 - 0.4 * Math.Clamp(effectiveRep, 0, 100) / 100.0;
}
