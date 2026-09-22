using System.Collections.ObjectModel;

namespace MangakaSim.Catalog;

public sealed record Keyframe(int Year, double Value);
public sealed class TrendCatalog
{
    private sealed record Data(string[] Genres, Dictionary<string, Keyframe[]> Baselines, Keyframe[] InternetReach, Keyframe[] PriceIndex);
    private static readonly Lazy<TrendCatalog> Default = new(() => FromJson(CatalogJson.Resource("trends.json")));
    public IReadOnlyList<string> Genres { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<Keyframe>> Baselines { get; }
    public IReadOnlyList<Keyframe> InternetReach { get; }
    public IReadOnlyList<Keyframe> PriceIndex { get; }
    private TrendCatalog(Data d)
    {
        Genres = Array.AsReadOnly(d.Genres);
        Baselines = new ReadOnlyDictionary<string, IReadOnlyList<Keyframe>>(d.Baselines.ToDictionary(p => p.Key,
            p => (IReadOnlyList<Keyframe>)Array.AsReadOnly(p.Value)));
        InternetReach = Array.AsReadOnly(d.InternetReach);
        PriceIndex = Array.AsReadOnly(d.PriceIndex);
    }
    public static TrendCatalog LoadDefault() => Default.Value;
    public static TrendCatalog FromJson(string json)
    {
        var d = CatalogJson.Parse<Data>(json);
        CatalogJson.Require(d.Genres is { Length: > 0 } && d.Genres.Contains("other") &&
            d.Genres.All(g => !string.IsNullOrWhiteSpace(g) && g == g.Trim().ToLowerInvariant()) &&
            d.Genres.Distinct().Count() == d.Genres.Length && d.Baselines is not null &&
            d.Baselines.Keys.ToHashSet().SetEquals(d.Genres), "genres");
        foreach (var curve in d.Baselines.Values.Append(d.InternetReach).Append(d.PriceIndex))
        {
            CatalogJson.Require(curve is { Length: > 0 } && curve.All(k => k is not null && k.Year is >= 1 and <= 9998 &&
                double.IsFinite(k.Value) && k.Value > 0), "curve values");
            CatalogJson.Require(curve.Zip(curve.Skip(1)).All(pair => pair.First.Year < pair.Second.Year), "curve order");
        }
        var catalog = new TrendCatalog(d);
        foreach (var year in d.Baselines.Values.SelectMany(c => c).Select(k => k.Year).Distinct())
        {
            var values = d.Genres.Where(g => g != "other")
                .Select(g => Rules.TrendRules.Baseline(catalog, g, new DateTime(year, 1, 1))).ToArray();
            CatalogJson.Require(values.Count(v => v <= .7) >= 2 && values.Count(v => v >= 1.2) >= 2 &&
                values.Average() is >= .9 and <= 1.1, "genre spread");
        }
        return catalog;
    }
}
