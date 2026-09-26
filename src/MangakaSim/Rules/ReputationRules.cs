namespace MangakaSim.Rules;

public static class ReputationRules
{
    public static double StaffTerm(IEnumerable<double> reputations)
    {
        var people = reputations.OrderDescending().Take(3).ToArray();
        var weights = new[] { .5, .3, .2 }.Take(people.Length).ToArray();
        return people.Length == 0 ? 0 : people.Zip(weights).Sum(p => p.First * p.Second) / weights.Sum();
    }
    public static double Effective(double trackRecord, IEnumerable<double> reputations) => .5 * trackRecord + .5 * StaffTerm(reputations);
    // Newcomers are offered fees as if reputation were at least NewcomerFeeReputation.
    public const double NewcomerFeeReputation = 50;
    public static long Fee(int min, int max, double reputation, double index) =>
        checked(Economy.Yen((min + (max - min) * Math.Max(reputation, NewcomerFeeReputation) / 100) * index / 100) * 100);
    public static double Protection(int chapters, double fans, double impact) =>
        Math.Clamp(.4 * Math.Min(chapters, 300) / 300 + .35 * (fans / (fans + 500000)) + .25 * impact / 100, 0, 1);
    public static double Completion(int quality, double share, bool doujin) => (quality - 60) / 20.0 * share * (doujin ? .5 : 1);
    public static double Withdraw(int published) => Math.Max(-10, -3 - .05 * published);
    public static double Ending(int line, double averageRank, long copies) => Math.Round(
        1 + 4 * Math.Clamp((line - averageRank) / line, 0, 1) * Math.Min(1, copies / 500000.0), 1, MidpointRounding.AwayFromZero);
}
