namespace MangakaSim;

public record MonthlyCharge(long Rent, long Upkeep, long Provider)
{
    public long Total => Rent + Upkeep + Provider;
}

public static class PremisesRules
{
    public const int ProviderFee1996 = 3_000;

    /// <summary>One month's rent of the target, at today's prices.</summary>
    public static long MoveCost(Premises target, double priceIndex) => Economy.Inflate(target.MonthlyRent, priceIndex);

    public static MonthlyCharge Charge(Premises premises, IEnumerable<Amenity> amenities, bool online, double priceIndex) =>
        new(Economy.Inflate(premises.MonthlyRent, priceIndex),
            Economy.Inflate(amenities.Sum(a => (long)a.MonthlyUpkeep), priceIndex),
            online ? Economy.Inflate(ProviderFee1996, priceIndex) : 0);
}
