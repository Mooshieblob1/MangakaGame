using System.Text.Json;

namespace MangakaSim;

public sealed class Premises
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Capacity { get; set; }
    /// <summary>1996 yen per month.</summary>
    public int MonthlyRent { get; set; }
    public double Atmosphere { get; set; }
}

public sealed class Amenity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>hunger, thirst, comfort, or none.</summary>
    public string Need { get; set; } = "none";
    public int Cost { get; set; }
    public int MonthlyUpkeep { get; set; }
    public double RecoveryPerBreak { get; set; }
    public double Atmosphere { get; set; }
}

public sealed class ScheduledCandidate
{
    public string Name { get; set; } = "";
    public DateTime AppearsAt { get; set; }
    public int MonthsAvailable { get; set; }
    public Dictionary<Stage, int> Skills { get; set; } = new();
    /// <summary>Asking salary as a multiple of the market salary for the skills.</summary>
    public double AskingMultiplier { get; set; } = 1.0;
    public string Note { get; set; } = "";
}

/// <summary>Premises tiers, amenities, scheduled candidates and name pools.</summary>
public sealed class StaffCatalog
{
    public const int MinNames = 60;
    public static readonly string[] NeedNames = { "hunger", "thirst", "comfort" };

    private static readonly Lazy<StaffCatalog> Default = new(() => FromJson(CatalogResources.Read("staff.json")));

    public IReadOnlyList<Premises> Premises { get; }
    public IReadOnlyList<Amenity> Amenities { get; }
    public IReadOnlyList<ScheduledCandidate> ScheduledCandidates { get; }
    public IReadOnlyList<string> GivenNames { get; }
    public IReadOnlyList<string> FamilyNames { get; }

    private StaffCatalog(List<Premises> premises, List<Amenity> amenities, List<ScheduledCandidate> scheduled,
        List<string> given, List<string> family)
    {
        Premises = premises;
        Amenities = amenities;
        ScheduledCandidates = scheduled;
        GivenNames = given;
        FamilyNames = family;
    }

    public static StaffCatalog LoadDefault() => Default.Value;

    public Premises? FindPremises(string id) => Premises.FirstOrDefault(p => p.Id == id);
    public Premises RequirePremises(string id) => FindPremises(id) ?? throw new InvalidDataException($"Unknown premises id '{id}'.");
    public Amenity? FindAmenity(string id) => Amenities.FirstOrDefault(a => a.Id == id);
    public Amenity RequireAmenity(string id) => FindAmenity(id) ?? throw new InvalidDataException($"Unknown amenity id '{id}'.");

    public static StaffCatalog FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<StaffCatalogDto>(json, CatalogResources.JsonOptions)
                  ?? throw new InvalidDataException("Staff catalog is empty.");
        static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidDataException($"Staff catalog: {message}");
        }

        Check(dto.Premises is { Count: > 0 }, "premises are missing");
        Check(dto.Amenities is not null, "amenities are missing");
        Check(dto.ScheduledCandidates is not null, "scheduled candidates are missing");
        Check(dto.GivenNames is not null && dto.FamilyNames is not null, "name pools are missing");

        var premisesIds = new HashSet<string>();
        foreach (var p in dto.Premises!)
        {
            Check(!string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Name), "premises need id and name");
            Check(premisesIds.Add(p.Id), $"duplicate premises id '{p.Id}'");
            Check(p.Capacity >= 1, $"premises '{p.Id}' capacity must be at least 1");
            Check(p.MonthlyRent >= 0, $"premises '{p.Id}' rent must not be negative");
        }
        Check(premisesIds.Contains("garage"), "premises 'garage' is required as the starting studio");

        var amenityIds = new HashSet<string>();
        foreach (var a in dto.Amenities!)
        {
            Check(!string.IsNullOrWhiteSpace(a.Id) && !string.IsNullOrWhiteSpace(a.Name), "amenity needs id and name");
            Check(amenityIds.Add(a.Id), $"duplicate amenity id '{a.Id}'");
            Check(a.Need == "none" || NeedNames.Contains(a.Need), $"amenity '{a.Id}' need must be hunger, thirst, comfort or none");
            Check(a.Cost >= 0 && a.MonthlyUpkeep >= 0, $"amenity '{a.Id}' money values must not be negative");
            Check(a.RecoveryPerBreak is >= 0 and <= 100, $"amenity '{a.Id}' recovery per break must be within 0..100");
        }

        var names = new HashSet<string>();
        foreach (var c in dto.ScheduledCandidates!)
        {
            Check(!string.IsNullOrWhiteSpace(c.Name) && names.Add(c.Name), "scheduled candidate needs a unique name");
            Check(c.MonthsAvailable >= 1, $"scheduled candidate '{c.Name}' must be available at least one month");
            Check(c.Skills is not null && StageOrder.All.All(s => c.Skills.TryGetValue(s, out var v) && v is >= 0 and <= 100),
                $"scheduled candidate '{c.Name}' needs a skill 0..100 for every stage");
            Check(c.AskingMultiplier > 0, $"scheduled candidate '{c.Name}' asking multiplier must be positive");
        }

        Check(dto.GivenNames!.Count >= MinNames && dto.GivenNames.Distinct().Count() == dto.GivenNames.Count,
            $"given names must hold at least {MinNames} distinct entries");
        Check(dto.FamilyNames!.Count >= MinNames && dto.FamilyNames.Distinct().Count() == dto.FamilyNames.Count,
            $"family names must hold at least {MinNames} distinct entries");

        return new StaffCatalog(dto.Premises, dto.Amenities, dto.ScheduledCandidates, dto.GivenNames, dto.FamilyNames);
    }

    private sealed class StaffCatalogDto
    {
        public List<Premises>? Premises { get; set; }
        public List<Amenity>? Amenities { get; set; }
        public List<ScheduledCandidate>? ScheduledCandidates { get; set; }
        public List<string>? GivenNames { get; set; }
        public List<string>? FamilyNames { get; set; }
    }
}
