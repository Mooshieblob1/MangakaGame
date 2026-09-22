namespace MangakaSim;

public static class TrendRules
{
    public const double MinEffective = 0.2;
    public const double MaxEffective = 1.8;
    public const double MaxNoise = 0.15;
    public const double MaxPlayerInfluence = 0.5;

    /// <summary>Trim, lower-case, and map anything not in the catalog to "other".</summary>
    public static string Normalise(string? genre, TrendCatalog catalog)
    {
        var key = (genre ?? "").Trim().ToLowerInvariant();
        return catalog.HasGenre(key) ? key : TrendCatalog.OtherGenre;
    }

    public static double Baseline(TrendCatalog catalog, string genre, DateTime now) =>
        Curves.Interpolate(catalog.Keyframes[Normalise(genre, catalog)], now, growAfterLast: false);

    public static double Effective(GenreTrend trend, TrendCatalog catalog, DateTime now) =>
        Math.Clamp(Baseline(catalog, trend.Genre, now) + trend.Noise + trend.Boom + trend.PlayerInfluence,
            MinEffective, MaxEffective);

    /// <summary>Each same-genre rival beyond the second costs 3%, floor 0.85.</summary>
    public static double Crowding(int sameGenreOthers) =>
        Math.Max(0.85, 1.0 - 0.03 * Math.Max(0, sameGenreOthers - 2));
}
