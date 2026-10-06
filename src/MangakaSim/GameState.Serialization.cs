using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace MangakaSim;

public partial class GameState
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Serializes the whole state, including the event list, RNG state and command log.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Deserializes a save. Throws InvalidDataException when the Version is missing
    /// or newer than this build understands.
    /// </summary>
    public static GameState FromJson(string json)
    {
        int version;
        using (var document = JsonDocument.Parse(json))
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty(nameof(Version), out var element) ||
                element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out version))
                throw new InvalidDataException("Save file has no Version number.");

            if (version == CurrentVersion || version is 5 or 6 or 7 or 8 or 9)
            {
                foreach (var property in new[] { nameof(Clock), nameof(People), nameof(Series), nameof(Events),
                             nameof(Rng), nameof(RngSeed), nameof(Settings), nameof(NextId), nameof(CommandLog),
                             nameof(RecapWindowStart), nameof(RecapFiredToday),
                             nameof(Businesses), nameof(Locations), nameof(Markets), nameof(Trends),
                             nameof(DoujinCopiesThisMonth), nameof(DoujinFansThisMonth) })
                    if (!document.RootElement.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
                        throw new InvalidDataException($"Save file is missing {property}.");
                if(document.RootElement.GetProperty(nameof(Locations)).ValueKind==JsonValueKind.Array)
                    foreach(var location in document.RootElement.GetProperty(nameof(Locations)).EnumerateArray())
                        foreach(var field in new[]{"PropertyOfferId","FloorPlanId","OfficeRevision","BaseAtmosphere","FixedBreakSeats"})
                            if(location.ValueKind!=JsonValueKind.Object||!location.TryGetProperty(field,out var officeValue)||officeValue.ValueKind==JsonValueKind.Null)
                                throw new InvalidDataException($"Save file is missing location {field}.");
                foreach (var property in new[] { nameof(LastTrendUpdateMonth), nameof(LastSalesAt) })
                    if (!document.RootElement.TryGetProperty(property, out _)) throw new InvalidDataException($"Save file is missing {property}.");
            }
        }

        if (version > CurrentVersion)
            throw new InvalidDataException(
                $"Save file version {version} is newer than the supported version {CurrentVersion}. Update the game to load it.");

        if (version != CurrentVersion)
            throw new InvalidDataException($"Save file version {version} is not supported. Use Import for version 3, 4, 5, 6, 7, 8 or 9 and keep the original file. Older saves require a new game.");

        GameState state;
        try
        {
            state = JsonSerializer.Deserialize<GameState>(json, JsonOptions)
                ?? throw new InvalidDataException("Save file is empty.");
        }
        catch (JsonException ex) { throw new InvalidDataException($"Save data is malformed: {ex.Message}", ex); }
        try { state.ValidateSave(); state.EnsureGoals(); state.EnsureDisclosure(); state.EnsureStudioIsland(); }
        catch (Exception ex) when (ex is OverflowException or ArgumentException or InvalidOperationException or NullReferenceException or JsonException or KeyNotFoundException)
        {
            throw new InvalidDataException("Save file contains malformed state, references, numeric totals or dates.", ex);
        }
        return state;
    }

    // Saves written before the fix for fresh-player finding A9 left older drafts of revised contest manuscripts looking
    // like magazine chapters, which made every save after a serialization unloadable. Mark those drafts before checking.
    private void RepairSupersededDrafts()
    {
        foreach (var manuscript in Progression?.Manuscripts ?? [])
            if (Series?.FirstOrDefault(s => s.Id == manuscript.SeriesId) is { } series)
                foreach (var draft in series.Chapters.Where(c => c.Id < manuscript.ChapterId && !c.Superseded && !c.IsOneShot && !c.DoujinEligible &&
                             c.PublishedAt is null && c.EditorMagazineId is null && c.Status == ChapterStatus.Complete))
                    draft.Superseded = true;
    }

    private void ValidateSave()
    {
        RepairSupersededDrafts();
        ValidateGoals();
        ValidateMilestones();
        ValidateDisclosure();
        static void Check([DoesNotReturnIf(false)] bool valid, string field)
        {
            if (!valid) throw new InvalidDataException($"Save file has invalid {field}.");
        }

        Check(Clock is not null && Clock.Now >= GameClock.Start && Clock.Now.Ticks % TimeSpan.TicksPerHour == 0, "clock");
        Check(People is { Count: > 0 } && People.All(p => p is not null), "people");
        Check(Series is not null && Events is not null && Rng is not null && CommandLog is not null, "state");
        Check(Settings?.Balance is not null && Settings.AutoPause is not null, "settings");
        var balance = Settings!.Balance;
        Check(balance.OvertimeCap is >= 0 and <= 24, "overtime cap");
        Check(new[] { balance.MultiplierAtSkill0, balance.MultiplierAtSkill50, balance.MultiplierAtSkill100 }
            .All(x => double.IsFinite(x) && x > 0), "skill multipliers");
        Check(balance.BaseHoursPerPage is not null && StageOrder.All.All(stage =>
            balance.BaseHoursPerPage.TryGetValue(stage, out var value) && double.IsFinite(value) && value > 0), "stage costs");
        Check(RecapWindowStart >= 0 && RecapWindowStart <= Events!.Count, "recap window");

        var ids = new HashSet<int>();
        void CheckId(int id) => Check(id > 0 && ids.Add(id), "unique id");
        foreach (var person in People!)
        {
            CheckId(person.Id);
            Check(person.Name is not null && person.Skills is not null && StageOrder.All.All(stage =>
                person.Skills.TryGetValue(stage, out var skill) && skill is >= 0 and <= 100), "person skills");
            Check(person.Schedule is not null && person.Schedule.DaysOff is not null &&
                person.Schedule.DaysOff.All(Enum.IsDefined) && person.Schedule.WorkStartHour >= 0 &&
                person.Schedule.WorkStartHour < person.Schedule.WorkEndHour &&
                person.Schedule.WorkEndHour <= 24 - balance.OvertimeCap, "schedule");
            Check(person.HoursWorkedToday is >= 0 and <= 24 && person.OvertimeHoursToday >= 0 &&
                person.OvertimeHoursToday <= person.HoursWorkedToday, "work counters");
            Check(person.Queue is not null && person.Pins is not null, "queue");
        }

        foreach (var series in Series!)
        {
            Check(series is not null, "series");
            CheckId(series!.Id);
            Check(!string.IsNullOrWhiteSpace(series.Title) && series.Genre is not null &&
                Enum.IsDefined(series.Cadence) && Enum.IsDefined(series.Status) && series.PagesPerChapter > 0 &&
                series.Chapters is not null, "series details");
            Check(series.StartDate.Ticks % TimeSpan.TicksPerHour == 0, "series start date");
            Check(!series.StandaloneDoujin || series.Chapters!.Count <= 1 && series.Publishing == PublishingStatus.Unpublished, "standalone doujin");
            var number = 0;
            foreach (var chapter in series.Chapters!)
            {
                Check(chapter is not null, "chapter");
                CheckId(chapter!.Id);
                Check(chapter.Number > number && Enum.IsDefined(chapter.Status) &&
                    chapter.DueDate.Ticks % TimeSpan.TicksPerHour == 0 && chapter.HoursOverdue >= 0, "chapter details");
                number = chapter.Number;
                Check(chapter.Stages is { Count: 5 } && chapter.Stages.All(s => s is not null) &&
                    chapter.Stages.Select(s => s.Stage).SequenceEqual(StageOrder.All), "chapter stages");
                foreach (var work in chapter.Stages!)
                    Check(Enum.IsDefined(work.Status) && double.IsFinite(work.HoursRequired) && work.HoursRequired > 0 &&
                        double.IsFinite(work.HoursDone) && work.HoursDone >= 0 && work.HoursDone <= work.HoursRequired &&
                        (work.AssignedTo is null || People.Any(p => p.Id == work.AssignedTo)), "stage work");
                Check((chapter.Status != ChapterStatus.Complete || (chapter.IsFinished &&
                    (chapter.EditorMagazineId is null || chapter.Editor == EditorStatus.Approved))) &&
                    (chapter.Status == ChapterStatus.Complete) == chapter.CompletedAt.HasValue, "chapter completion");
            }
        }
        ValidateStudioSave(ids);
        ValidateMarketSave(ids);
        ValidateOperationsSave(ids);
        if(Version >= 4) ValidateOfficeSave();
        if(Version >= 5) ValidateTimelineSave(ids);
        if(Version >= 6) ValidateCareer();
        if(Version >= 7) ValidateProgression(ids);
        Check(NextId > ids.Max(), "next id");
        bool ValidRef(QueueRef reference) => Enum.IsDefined(reference.Stage) && FindChapter(reference.ChapterId) is not null;
        foreach (var person in People!)
        {
            Check(person.Queue!.All(r => ValidRef(r) && IsStartableOrPending(r, person.Id)) &&
                person.Queue.Distinct().Count() == person.Queue.Count, "queue references");
            Check(person.Pins!.All(ValidRef) && person.Pins.Distinct().Count() == person.Pins.Count, "pins");
            Check(person.ManualOrder is null || person.ManualOrder.All(ValidRef), "manual queue");
            Check(person.CurrentTask is null || (person.Queue.Contains(person.CurrentTask.Value) && IsStartable(person.CurrentTask.Value)), "current task");
        }
        Check(Events!.All(e => e is not null && Enum.IsDefined(e.Type) && e.Message is not null), "events");
        foreach (var ev in Events.Where(e => e.Type == EventType.DailyRecap))
        {
            var recap = ev.Recap;
            Check(recap is not null && recap.HoursPerPerson is not null && recap.HoursPerPerson.All(p => p is not null) &&
                recap.StagesStarted is not null && recap.StagesStarted.All(s => s is not null) &&
                recap.StagesCompleted is not null && recap.StagesCompleted.All(s => s is not null) &&
                recap.ChaptersCompleted is not null && recap.ChaptersCompleted.All(c => c is not null) &&
                recap.ChaptersAtRisk is not null && recap.ChaptersAtRisk.All(c => c is not null) &&
                recap.DeadlinesMissed is not null && recap.DeadlinesMissed.All(c => c is not null) &&
                recap.ChaptersPublished is not null && recap.ChaptersPublished.All(c => c is not null) &&
                recap.IssuesMissed is not null && recap.YenEarned >= 0, "daily recap");
        }
        Check(CommandLog!.All(e => e is not null && e.Command is not null &&
            e.Time.Ticks % TimeSpan.TicksPerHour == 0 && e.Time <= Clock!.Now), "command log");
    }

    private bool IsStartableOrPending(QueueRef reference, int personId)
    {
        var chapter = FindChapter(reference.ChapterId)!;
        var work = chapter.StageWork(reference.Stage);
        return SeriesOf(chapter).Status == SeriesStatus.Active && !work.IsDone && work.AssignedTo == personId;
    }
}
