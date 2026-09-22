using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MangakaSim;

public enum Demographic { Shonen, Shojo, Seinen, Josei }

public sealed class Publisher
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class Magazine
{
    public string Id { get; set; } = "";
    public string PublisherId { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>1 flagship .. 3 entry.</summary>
    public int Tier { get; set; }
    public Cadence Cadence { get; set; }
    public Demographic Demographic { get; set; }
    /// <summary>0.75..1.25 per normalised genre; unlisted genres are 1.0.</summary>
    public Dictionary<string, double> GenreAffinities { get; set; } = new();
    public int RosterSize { get; set; }
    public int CancellationRank { get; set; }
    /// <summary>1996 yen per page.</summary>
    public int FeePerPageMin { get; set; }
    public int FeePerPageMax { get; set; }
    public DayOfWeek IssueCloseDay { get; set; }
    public int IssueCloseHour { get; set; }
    public int ChaptersPerVolume { get; set; }

    public double Affinity(string normalisedGenre) =>
        GenreAffinities.TryGetValue(normalisedGenre, out var value) ? value : 1.0;

    /// <summary>Days between issue closes: 7, 14 or 28. Monthly magazines close every four weeks.</summary>
    [JsonIgnore]
    public int CadenceDays => Cadence switch
    {
        Cadence.Weekly => 7,
        Cadence.Biweekly => 14,
        Cadence.Monthly => 28,
        _ => throw new ArgumentOutOfRangeException(nameof(Cadence), Cadence, null),
    };

    /// <summary>First issue close at or after the given time.</summary>
    public DateTime FirstCloseAtOrAfter(DateTime from)
    {
        var candidate = new DateTime(from.Year, from.Month, from.Day, IssueCloseHour, 0, 0);
        while (candidate.DayOfWeek != IssueCloseDay || candidate < from) candidate = candidate.AddDays(1);
        return candidate;
    }
}

public sealed class PublisherCatalog
{
    private static readonly Lazy<PublisherCatalog> Default = new(() => FromJson(CatalogResources.Read("publishers.json")));

    public IReadOnlyList<Publisher> Publishers { get; }
    public IReadOnlyList<Magazine> Magazines { get; }

    private PublisherCatalog(List<Publisher> publishers, List<Magazine> magazines)
    {
        Publishers = publishers;
        Magazines = magazines;
    }

    public static PublisherCatalog LoadDefault() => Default.Value;

    public Magazine? Find(string id) => Magazines.FirstOrDefault(m => m.Id == id);

    public Magazine Require(string id) =>
        Find(id) ?? throw new InvalidDataException($"Unknown magazine id '{id}'.");

    public static PublisherCatalog FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<PublisherCatalogDto>(json, CatalogResources.JsonOptions)
                  ?? throw new InvalidDataException("Publisher catalog is empty.");
        var publishers = dto.Publishers ?? throw new InvalidDataException("Publisher catalog has no publishers.");
        var magazines = dto.Magazines ?? throw new InvalidDataException("Publisher catalog has no magazines.");
        Validate(publishers, magazines);
        return new PublisherCatalog(publishers, magazines);
    }

    private static void Validate(List<Publisher> publishers, List<Magazine> magazines)
    {
        static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidDataException($"Publisher catalog: {message}");
        }

        var publisherIds = new HashSet<string>();
        foreach (var publisher in publishers)
        {
            Check(!string.IsNullOrWhiteSpace(publisher.Id) && !string.IsNullOrWhiteSpace(publisher.Name), "publisher needs id and name");
            Check(publisherIds.Add(publisher.Id), $"duplicate publisher id '{publisher.Id}'");
        }

        var magazineIds = new HashSet<string>();
        Check(magazines.Count > 0, "at least one magazine is required");
        foreach (var m in magazines)
        {
            Check(!string.IsNullOrWhiteSpace(m.Id) && !string.IsNullOrWhiteSpace(m.Name), "magazine needs id and name");
            Check(magazineIds.Add(m.Id), $"duplicate magazine id '{m.Id}'");
            Check(publisherIds.Contains(m.PublisherId), $"magazine '{m.Id}' has unknown publisher '{m.PublisherId}'");
            Check(m.Tier is >= 1 and <= 3, $"magazine '{m.Id}' tier must be 1..3");
            Check(Enum.IsDefined(m.Cadence) && Enum.IsDefined(m.Demographic), $"magazine '{m.Id}' has an unknown cadence or demographic");
            Check(m.RosterSize > m.CancellationRank && m.CancellationRank >= 1, $"magazine '{m.Id}' roster must be larger than its cancellation line");
            Check(m.FeePerPageMin > 0 && m.FeePerPageMin < m.FeePerPageMax, $"magazine '{m.Id}' fee range must be positive and increasing");
            Check(Enum.IsDefined(m.IssueCloseDay) && m.IssueCloseHour is >= 0 and <= 23, $"magazine '{m.Id}' issue close must be a weekday and an hour 0..23");
            Check(m.ChaptersPerVolume >= 1, $"magazine '{m.Id}' chapters per volume must be at least 1");
            Check(m.GenreAffinities is not null, $"magazine '{m.Id}' affinities missing");
            foreach (var (genre, affinity) in m.GenreAffinities!)
            {
                Check(!string.IsNullOrWhiteSpace(genre) && genre == genre.Trim().ToLowerInvariant(), $"magazine '{m.Id}' affinity genre '{genre}' must be a trimmed lowercase name");
                Check(affinity is >= 0.75 and <= 1.25, $"magazine '{m.Id}' affinity for '{genre}' must be within 0.75..1.25");
            }
        }
    }

    private sealed class PublisherCatalogDto
    {
        public List<Publisher>? Publishers { get; set; }
        public List<Magazine>? Magazines { get; set; }
    }
}

internal static class CatalogResources
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string Read(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream($"MangakaSim.Data.{fileName}")
                           ?? throw new InvalidDataException($"Embedded resource {fileName} is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
