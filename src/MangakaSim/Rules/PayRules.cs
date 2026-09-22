namespace MangakaSim;

public static class PayRules
{
    public const int MinSalaryRatioPercent = 80;
    public const double ReturningSalaryMultiplier = 1.3;

    /// <summary>(120,000 + 2,000 x mean skill) x price index, to the nearest 1,000 yen.</summary>
    public static int MarketSalary(IEnumerable<int> skills, double priceIndex)
    {
        var mean = skills.DefaultIfEmpty(0).Average();
        return RoundToThousand((120_000 + 2_000 * mean) * priceIndex);
    }

    public static double PayFactor(int salary, int marketSalary) =>
        marketSalary <= 0 ? 1.0 : Math.Clamp((double)salary / marketSalary, 0.6, 1.4);

    /// <summary>Market x a draw in 0.9..1.15, to the nearest 1,000.</summary>
    public static int AskingSalary(int marketSalary, Rng rng) => RoundToThousand(marketSalary * rng.NextDouble(0.9, 1.15));

    public static bool Accepts(int offered, int asking) => offered * 100L >= (long)asking * MinSalaryRatioPercent;

    public static int RoundToThousand(double yen) => (int)(Math.Round(yen / 1000.0, MidpointRounding.AwayFromZero) * 1000);
}
