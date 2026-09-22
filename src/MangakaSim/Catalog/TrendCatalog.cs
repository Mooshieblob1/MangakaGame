using System.Text.Json;

namespace MangakaSim;

/// <summary>Genre list, baseline popularity keyframes, internet reach and price index curves.</summary>
public sealed class TrendCatalog
{
    public const string OtherGenre = "other";

    private static readonly Lazy<TrendCatalog> Default = new(() => FromJson(CatalogResources.Read("trends.json")));

    public IReadOnlyList<string> Genres { get; }
    public IReadOnlyDictionary<string, SortedDictionary<int, double>> Keyframes { get; }
    public SortedDictionary<int, double> InternetReach { get; }
    public SortedDictionary<int, double> PriceIndex { get; }

    private TrendCatalog(List<string> genres, Dictionary<string, SortedDictionary<int, double>> keyframes,
        SortedDictionary<int, double> internetReach, SortedDictionary<int, double> priceIndex)
    {
        Genres = genres;
        Keyframes = keyframes;
        InternetReach = internetReach;
        PriceIndex = priceIndex;
    }

    public static TrendCatalog LoadDefault() => Default.Value;

    public bool HasGenre(string normalisedGenre) => Keyframes.ContainsKey(normalisedGenre);

    public static TrendCatalog FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<TrendCatalogDto>(json, CatalogResources.JsonOptions)
                  ?? throw new InvalidDataException("Trend catalog is empty.");
        static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidDataException($"Trend catalog: {message}");
        }

        Check(dto.Genres is { Count: > 0 }, "genre list is missing");
        Check(dto.Baseline is { Count: > 0 }, "baseline keyframes are missing");
        Check(dto.InternetReach is { Count: > 0 }, "internet reach curve is missing");
        Check(dto.PriceIndex is { Count: > 0 }, "price index curve is missing");

        var genres = dto.Genres!;
        Check(genres.All(g => !string.IsNullOrWhiteSpace(g) && g == g.Trim().ToLowerInvariant()), "genres must be trimmed lowercase names");
        Check(genres.Distinct().Count() == genres.Count, "duplicate genre");
        Check(genres.Contains(OtherGenre), $"genre '{OtherGenre}' is required");

        var keyframes = new Dictionary<string, SortedDictionary<int, double>>();
        foreach (var (genre, curve) in dto.Baseline!)
        {
            Check(genres.Contains(genre), $"keyframes for unknown genre '{genre}'");
            keyframes[genre] = ToCurve(curve, $"baseline for '{genre}'");
        }
        foreach (var genre in genres) Check(keyframes.ContainsKey(genre), $"genre '{genre}' has no keyframes");

        var years = keyframes.Values.SelectMany(c => c.Keys).Distinct().OrderBy(y => y).ToList();
        foreach (var year in years)
        {
            var values = genres.Where(g => g != OtherGenre)
                .Select(g => Curves.Interpolate(keyframes[g], year, growAfterLast: false)).ToList();
            Check(values.Count(v => v <= 0.70) >= 2, $"spread rule: fewer than two genres at or below 0.70 in {year}");
            Check(values.Count(v => v >= 1.20) >= 2, $"spread rule: fewer than two genres at or above 1.20 in {year}");
            var mean = values.Average();
            Check(mean is >= 0.90 and <= 1.10, $"spread rule: mean {mean:0.000} outside 0.90..1.10 in {year}");
        }

        return new TrendCatalog(genres, keyframes,
            ToCurve(dto.InternetReach!, "internet reach"),
            ToCurve(dto.PriceIndex!, "price index"));
    }

    private static SortedDictionary<int, double> ToCurve(Dictionary<int, double> raw, string name)
    {
        if (raw.Count == 0) throw new InvalidDataException($"Trend catalog: {name} has no keyframes");
        var curve = new SortedDictionary<int, double>();
        foreach (var (year, value) in raw)
        {
            if (!double.IsFinite(value) || value < 0)
                throw new InvalidDataException($"Trend catalog: {name} has an invalid value at {year}");
            curve[year] = value;
        }
        return curve;
    }

    private sealed class TrendCatalogDto
    {
        public List<string>? Genres { get; set; }
        public Dictionary<string, Dictionary<int, double>>? Baseline { get; set; }
        public Dictionary<int, double>? InternetReach { get; set; }
        public Dictionary<int, double>? PriceIndex { get; set; }
    }
}

/// <summary>Piecewise-linear keyframe curves keyed by year.</summary>
public static class Curves
{
    /// <summary>Calendar year plus the elapsed fraction of that year.</summary>
    public static double FractionalYear(DateTime now)
    {
        var start = new DateTime(now.Year, 1, 1);
        var days = DateTime.IsLeapYear(now.Year) ? 366.0 : 365.0;
        return now.Year + (now - start).TotalDays / days;
    }

    public static double Interpolate(SortedDictionary<int, double> curve, DateTime now, bool growAfterLast) =>
        Interpolate(curve, FractionalYear(now), growAfterLast);

    /// <summary>
    /// Linear between keyframes, flat before the first. After the last keyframe the value is held,
    /// or grows 2% per year compounding when growAfterLast is set.
    /// </summary>
    public static double Interpolate(SortedDictionary<int, double> curve, double year, bool growAfterLast)
    {
        if (curve.Count == 0) throw new ArgumentException("curve has no keyframes", nameof(curve));
        var first = curve.First();
        if (year <= first.Key) return first.Value;
        var last = curve.Last();
        if (year >= last.Key)
        {
            return growAfterLast ? last.Value * Math.Pow(1.02, year - last.Key) : last.Value;
        }
        var previous = first;
        foreach (var point in curve)
        {
            if (point.Key >= year)
            {
                var t = (year - previous.Key) / (point.Key - previous.Key);
                return previous.Value + (point.Value - previous.Value) * t;
            }
            previous = point;
        }
        return last.Value;
    }
}
