using MangakaSim.Catalog;

namespace MangakaSim.Rules;

public static class TrendRules
{
    public static string Normalise(string genre, TrendCatalog c)
    {
        var key = genre.Trim().ToLowerInvariant();
        return c.Genres.Contains(key) ? key : "other";
    }
    public static double FractionalYear(DateTime now) => now.Year +
        (now - new DateTime(now.Year, 1, 1)).TotalDays / (DateTime.IsLeapYear(now.Year) ? 366 : 365);
    public static double Interpolate(IReadOnlyList<Keyframe> frames, double year)
    {
        if (year <= frames[0].Year) return frames[0].Value;
        for (var i = 1; i < frames.Count; i++)
            if (year <= frames[i].Year)
                return frames[i - 1].Value + (frames[i].Value - frames[i - 1].Value) *
                    (year - frames[i - 1].Year) / (frames[i].Year - frames[i - 1].Year);
        return frames[^1].Value;
    }
    public static double Baseline(TrendCatalog c, string genre, DateTime now) => Interpolate(c.Baselines[Normalise(genre, c)], FractionalYear(now));
    public static double Effective(double baseline, double noise, double boom, double influence) => Math.Clamp(baseline + noise + boom + influence, .2, 1.8);
    public static double Crowding(int sameGenreOthers) => Math.Max(.85, 1 - .03 * Math.Max(0, sameGenreOthers - 2));
    public static double Noise(double old, double sample) => Math.Clamp(old + sample - .1 * old, -.15, .15);
    public static double Fade(double peak, double floor, DateTime start, DateTime end, DateTime now) =>
        peak + (floor - peak) * Math.Clamp((now - start).TotalHours / (end - start).TotalHours, 0, 1);
}
