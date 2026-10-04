namespace MangakaSim;

public sealed record PartRecord(string Id, DateTime At, bool Backfilled);

/// <summary>Which parts of the interface a career has opened (spec 2026-10-02). Absent from older saves (see EnsureDisclosure).</summary>
public sealed class DisclosureState
{
    public List<PartRecord> Opened { get; set; } = new();
    public bool ShowAll { get; set; }
}

public partial class GameState
{
    /// <summary>Null only in saves written before progressive disclosure, until EnsureDisclosure backfills it on load.</summary>
    public DisclosureState? Disclosure { get; set; }

    /// <summary>Whether a part is visible: opened, or show every screen. A state with no disclosure shows everything.</summary>
    public bool PartShown(string id) => Disclosure is not { } d || d.ShowAll || d.Opened.Any(r => r.Id == id);
    public bool PartOpened(string id) => Disclosure?.Opened.Any(r => r.Id == id) == true;

    /// <summary>Opens every part whose moment has happened or whose backstop chapter has begun. A backfill (older saves) emits nothing.</summary>
    internal void EvaluateParts(bool backfill = false)
    {
        if (Disclosure is not { } d) return;
        var chapter = Goals?.Chapter ?? 0;
        foreach (var part in DisclosureCatalog.Parts)
            if (!d.Opened.Any(r => r.Id == part.Id) && (chapter >= part.Backstop || part.Moment(this))) OpenPart(part, backfill);
    }

    private void OpenPart(PartDefinition part, bool backfill)
    {
        Disclosure!.Opened.Add(new(part.Id, Clock.Now, backfill));
        if (!backfill) Emit(EventType.PartOpened, $"Opened: {part.Name}.", personId: ProtagonistPersonId);
    }

    private void ApplyOpenPart(OpenPartCommand c)
    {
        if (!DisclosureCatalog.Known(c.Part)) throw new InvalidCommandException("That part of the studio does not exist.");
        if (Disclosure is { } d && !d.Opened.Any(r => r.Id == c.Part)) OpenPart(DisclosureCatalog.Get(c.Part), false);
    }

    /// <summary>Older saves have no disclosure: every part already reached opens silently (spec 2026-10-02), and the replay
    /// checkpoint is rebased so replays match. Runs after EnsureGoals so chapter backstops apply.</summary>
    internal void EnsureDisclosure()
    {
        if (Disclosure is not null) return;
        Disclosure = new();
        EvaluateParts(backfill: true);
        World.ReplayCheckpoint = null; World.ReplayLogStart = CommandLog.Count;
        ValidateSave(); World.ReplayCheckpoint = ToJson();
    }

    private void ValidateDisclosure()
    {
        if (Disclosure is not { } d) return;
        if (d.Opened is null || d.Opened.Any(r => r is null || !DisclosureCatalog.Known(r.Id)) || d.Opened.Select(r => r.Id).Distinct().Count() != d.Opened.Count)
            throw new InvalidDataException("Save file has invalid disclosure parts.");
    }
}
