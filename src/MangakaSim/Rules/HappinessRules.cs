namespace MangakaSim;

public static class HappinessRules
{
    public const double DriftRate = 0.1;

    // Shocks.
    public const double NeedZero = -5;
    public const double PayrollMissed = -20;
    public const double Raise = 10;
    public const double PayCut = -15;
    public const double Top3 = 2;
    public const double Cancelled = -5;
    public const double ColleagueQuit = -3;
    public const double BetterPremises = 5;
    public const double CrowdingPenaltyPerPerson = 8;

    public static double Equilibrium(double payFactor, double atmosphere, double overtimeShare, double breaksPerDay, double trackRecord) =>
        Math.Clamp(50 + 20 * (payFactor - 1) + atmosphere / 2 - 15 * overtimeShare - 10 * breaksPerDay
                   + 10 * Math.Min(1, trackRecord / 50), 0, 100);

    /// <summary>Moves a tenth of the way toward the equilibrium.</summary>
    public static double Step(double happiness, double equilibrium) =>
        Math.Clamp(happiness + DriftRate * (equilibrium - happiness), 0, 100);

    public static double Atmosphere(Premises premises, IEnumerable<Amenity> amenities, int people, int capacity) =>
        premises.Atmosphere + amenities.Sum(a => a.Atmosphere) - CrowdingPenaltyPerPerson * Math.Max(0, people - capacity);

    public static double Clamp(double value) => Math.Clamp(value, 0, 100);
}
