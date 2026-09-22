using MangakaSim.Catalog;

namespace MangakaSim.Rules;

public static class Economy
{
    public static long Yen(double value)
    {
        if (!double.IsFinite(value) || value >= 9223372036854775808.0 || value < long.MinValue) throw new OverflowException("Yen amount out of range.");
        return checked((long)Math.Round(value, MidpointRounding.AwayFromZero));
    }
    private static double AnchoredCurve(IReadOnlyList<Keyframe> frames, DateTime now)
    {
        if (now <= GameClock.Start) return frames[0].Value;
        for (var i = 1; i < frames.Count; i++)
        {
            var end = new DateTime(frames[i].Year, 1, 1);
            var start = i == 1 ? GameClock.Start : new DateTime(frames[i - 1].Year, 1, 1);
            if (now <= end) return frames[i - 1].Value + (frames[i].Value - frames[i - 1].Value) * (now - start).TotalHours / (end - start).TotalHours;
        }
        return frames[^1].Value;
    }
    public static double PriceIndex(TrendCatalog c, DateTime now) => now.Year >= c.PriceIndex[^1].Year
        ? c.PriceIndex[^1].Value * Math.Pow(1.02, TrendRules.FractionalYear(now) - c.PriceIndex[^1].Year) : AnchoredCurve(c.PriceIndex, now);
    public static double InternetReach(TrendCatalog c, DateTime now) => AnchoredCurve(c.InternetReach, now);
    public static long InternetCost(TrendCatalog c, DateTime now) => Yen(120000 * PriceIndex(c, now));
}
