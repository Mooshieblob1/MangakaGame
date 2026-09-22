using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangakaSim.Catalog;

public enum Demographic { Shonen, Shojo, Seinen, Josei }
public sealed record Publisher(string Id, string Name);
public sealed record Magazine(string Id, string PublisherId, string Name, int Tier, Cadence Cadence,
    Demographic Demographic, IReadOnlyDictionary<string, double> GenreAffinities, int RosterSize,
    int CancellationRank, int FeePerPageMin, int FeePerPageMax, DayOfWeek IssueCloseDay,
    int IssueCloseHour, int ChaptersPerVolume)
{
    public double Affinity(string genre) => GenreAffinities.GetValueOrDefault(genre, 1);
}

public sealed class PublisherCatalog
{
    private sealed record Data(Publisher[] Publishers, Magazine[] Magazines, string[] Adjectives, string[] Nouns);
    private static readonly Lazy<PublisherCatalog> Default = new(() => FromJson(CatalogJson.Resource("publishers.json")));
    public IReadOnlyList<Publisher> Publishers { get; }
    public IReadOnlyList<Magazine> Magazines { get; }
    public IReadOnlyList<string> Adjectives { get; }
    public IReadOnlyList<string> Nouns { get; }
    private PublisherCatalog(Data data)
    {
        Publishers = Array.AsReadOnly(data.Publishers);
        Magazines = Array.AsReadOnly(data.Magazines.Select(m => m with
        {
            GenreAffinities = new ReadOnlyDictionary<string, double>(new Dictionary<string, double>(m.GenreAffinities)),
        }).ToArray());
        Adjectives = Array.AsReadOnly(data.Adjectives);
        Nouns = Array.AsReadOnly(data.Nouns);
    }
    public static PublisherCatalog LoadDefault() => Default.Value;
    public Magazine Get(string id) => Magazines.FirstOrDefault(m => m.Id == id)
        ?? throw new InvalidDataException($"Unknown magazine '{id}'.");
    public static PublisherCatalog FromJson(string json)
    {
        var d = CatalogJson.Parse<Data>(json);
        CatalogJson.Require(d.Publishers is { Length: > 0 } && d.Magazines is { Length: > 0 }, "publishers/magazines");
        CatalogJson.Require(d.Publishers.All(p => p is not null && !string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Name)) &&
            d.Publishers.Select(p => p.Id).Distinct().Count() == d.Publishers.Length, "publisher ids");
        CatalogJson.Require(d.Magazines.All(m => m is not null && !string.IsNullOrWhiteSpace(m.Id)) &&
            d.Magazines.Select(m => m.Id).Distinct().Count() == d.Magazines.Length, "magazine ids");
        var genres = TrendCatalog.LoadDefault().Genres;
        foreach (var m in d.Magazines)
            CatalogJson.Require(!string.IsNullOrWhiteSpace(m.Name) && d.Publishers.Any(p => p.Id == m.PublisherId) &&
                m.Tier is >= 1 and <= 3 && Enum.IsDefined(m.Cadence) && Enum.IsDefined(m.Demographic) &&
                Enum.IsDefined(m.IssueCloseDay) && m.IssueCloseHour is >= 0 and <= 23 &&
                m.CancellationRank > 1 && m.RosterSize > m.CancellationRank && m.ChaptersPerVolume > 0 &&
                m.FeePerPageMin > 0 && m.FeePerPageMin < m.FeePerPageMax && m.GenreAffinities is not null &&
                m.GenreAffinities.All(a => genres.Contains(a.Key) && double.IsFinite(a.Value) && a.Value is >= .75 and <= 1.25), "magazine details");
        CatalogJson.Require(d.Adjectives is { Length: >= 40 } && d.Nouns is { Length: >= 40 } &&
            d.Adjectives.Concat(d.Nouns).All(s => !string.IsNullOrWhiteSpace(s)), "filler words");
        return new(d);
    }
}

internal static class CatalogJson
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new JsonStringEnumConverter() } };
    internal static T Parse<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json, Options) ?? throw new InvalidDataException("Empty catalog."); }
        catch (JsonException ex) { throw new InvalidDataException("Malformed catalog.", ex); }
    }
    internal static string Resource(string name)
    {
        using var stream = typeof(CatalogJson).Assembly.GetManifestResourceStream($"MangakaSim.Data.{name}")
            ?? throw new InvalidDataException($"Missing embedded catalog {name}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    internal static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool valid, string field)
    {
        if (!valid) throw new InvalidDataException($"Invalid catalog {field}.");
    }
}
