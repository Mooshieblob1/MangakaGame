namespace MangakaSim.Rules;

public static class CancellationRules
{
    public static (int Warning, int Cancel, int StrikeLifetime) Clocks(double protection) =>
        ((int)Math.Ceiling(3 + 9 * protection), (int)Math.Ceiling(3 + 23 * protection), (int)Math.Ceiling(8 - 6 * protection));
    public static double Chance(double reputation) => 1 - .4 * reputation / 100;
    public static double IssueAge(DateTime since, DateTime now, Cadence cadence) => (now - since).TotalDays / IssueSchedule.Days(cadence);
    public static bool StrikeExpired(DateTime since, DateTime now, Cadence cadence, int lifetime) => IssueAge(since, now, cadence) > lifetime;
}
