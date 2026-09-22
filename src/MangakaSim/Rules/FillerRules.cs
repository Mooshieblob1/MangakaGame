namespace MangakaSim;

/// <summary>Word lists and numeric ranges for generated roster series.</summary>
public static class FillerRules
{
    public const int MaxDrift = 3;
    public const double MinPopularity = 5;
    public const double MaxPopularity = 100;
    public const int RetireAfterIssuesBelowLine = 12;

    public static readonly string[] Adjectives =
    {
        "Crimson", "Silent", "Golden", "Wandering", "Iron", "Midnight", "Electric", "Hollow", "Radiant", "Broken",
        "Wild", "Frozen", "Burning", "Lost", "Sacred", "Velvet", "Thunder", "Paper", "Neon", "Ancient",
        "Little", "Grand", "Steel", "Rising", "Fallen", "Hidden", "Bright", "Dark", "Blue", "Scarlet",
        "Lucky", "Stray", "Royal", "Secret", "Rusty", "Gentle", "Fierce", "Cosmic", "Quiet", "Endless",
    };

    public static readonly string[] Nouns =
    {
        "Blade", "Garden", "Comet", "Fox", "Kingdom", "Tiger", "Harbor", "Lantern", "Empire", "Wolf",
        "Cafe", "Voyage", "Ronin", "Dragon", "Orchestra", "Detective", "Summer", "Circus", "Knight", "Ocean",
        "Academy", "Machine", "Phantom", "Crown", "Rocket", "Bakery", "Samurai", "Melody", "Fortress", "Rain",
        "Pirate", "Library", "Meteor", "Sparrow", "Mirror", "Festival", "Pilot", "Shrine", "Engine", "Star",
    };

    /// <summary>Hidden popularity draw range by magazine tier (inclusive).</summary>
    public static (int Min, int Max) PopularityRange(int tier) => tier switch
    {
        1 => (40, 95),
        2 => (35, 85),
        3 => (30, 75),
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };

    public static readonly (int Min, int Max) ReplacementRange = (45, 65);
    public static readonly (int Min, int Max) IconicRange = (85, 95);

    public static double Drift(double popularity, int step) =>
        Math.Clamp(popularity + step, MinPopularity, MaxPopularity);
}
