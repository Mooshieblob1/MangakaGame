using System.Text.Json.Serialization;

namespace MangakaSim;

public partial class GameState
{
    public const int CurrentVersion = 10;

    public int Version { get; set; } = CurrentVersion;
    public GameClock Clock { get; set; } = GameClock.AtStart();
    public List<Series> Series { get; set; } = new();
    public List<Person> People { get; set; } = new();
    public List<GameEvent> Events { get; set; } = new();
    public int RngSeed { get; set; }
    public Rng Rng { get; set; } = Rng.FromSeed(0);
    public Settings Settings { get; set; } = Settings.Default();
    public int NextId { get; set; } = 1;
    public bool RecapFiredToday { get; set; }

    /// <summary>The hour that the most recent tick simulated: one hour before Clock.Now.</summary>
    [JsonIgnore]
    public DateTime TickStart => Clock.Now.AddHours(-1);

    public static GameState NewGame(int seed = 0, OwnershipMode ownership = OwnershipMode.StudioRetention, string? protagonistName = null)
    {
        if (!Enum.IsDefined(ownership)) throw new ArgumentOutOfRangeException(nameof(ownership));
        var name=string.IsNullOrWhiteSpace(protagonistName)?"Aki":protagonistName.Trim();
        if(name.Length>40||name.Any(char.IsControl))throw new ArgumentException("Choose a name of up to 40 characters without control characters.",nameof(protagonistName));
        var state = new GameState { RngSeed = seed, Rng = Rng.FromSeed(seed) };
        var mangaka = new Person
        {
            Id = state.AllocateId(),
            Name = name,
            Schedule = new Schedule { WorkStartHour = 8, WorkEndHour = 18, DaysOff = { DayOfWeek.Sunday } },
            OvertimeAllowed = true,
        };
        foreach (var stage in StageOrder.All) mangaka.Skills[stage] = 95;
        state.People.Add(mangaka);
        state.InitializeStudio(ownership);
        state.InitializeMarket();
        state.RefreshOfficeAssignments();
        state.InitializeTimeline();
        state.InitializeCareer();
        state.InitializeProgression();
        state.Goals = new();
        state.Disclosure = new();
        return state;
    }

    public int AllocateId() => NextId++;

    public void Advance(int hours)
    {
        if (hours < 0) throw new ArgumentOutOfRangeException(nameof(hours), hours, "hours must be >= 0");
        for (var i = 0; i < hours; i++) Tick();
    }

    internal void Tick()
    {
        Clock.Advance();
        var closes = Markets.Where(m => m.NextIssueClose <= Clock.Now)
            .Select(m => new IssueCloseContext(m.MagazineId, m.NextIssueClose, m.IssuesClosed + 1)).ToArray();
        StartOfficeHour();
        OutsideJobStep();
        WellbeingStep();
        ConventionStep();
        SpareHoursStep();
        LicensingConsultationStep();
        WorkStep();
        SandboxProductionStep();
        EditorStep();
        TimelineMarketStep();
        IssueCloseStep(closes);
        PrintingStep();
        SalesStep();
        StudioStep();
        ProgressionStep();
        FinanceStep();
        CareerStep();
        TimelineStaffStep();
        CareerNarrativeStep();
        MilestoneStep();
        RefreshOfficeAssignments();
        OfficeRevision++;
        PitchStep(closes);
        RiskStep();
        DayEndStep();
        EvaluateGoals();
        EvaluateParts();
        if (Clock.Hour == 0) StartNewDay();
        else RunPlanner();
    }

    private void StartNewDay()
    {
        foreach (var person in People)
        {
            person.HoursWorkedToday = 0;
            person.OvertimeHoursToday = 0;
            person.ManualOrder = null;
        }
        RecapFiredToday = false;
        RecapWindowStart = StartOfActivityDate(Clock.Now.Date);

        Emit(EventType.DayStarted, $"{Clock.Now:ddd d MMM yyyy} begins.");
        if (Clock.DayOfWeek == DayOfWeek.Monday)
        {
            Emit(EventType.WeekStarted, $"Week of {Clock.Now:d MMM yyyy} begins.");
        }
        RunPlanner();
    }

    internal GameEvent Emit(EventType type, string message, int? seriesId = null,
        int? chapterNumber = null, int? personId = null, Stage? stage = null, EventContext? context = null)
    {
        var ev = new GameEvent
        {
            Time = Clock.Now,
            ActivityDate = context?.ActivityDate ?? Clock.Now.Date,
            MagazineId = context?.MagazineId,
            VolumeId = context?.VolumeId,
            Rank = context?.Rank,
            Amount = context?.Amount,
            Type = type,
            Message = message,
            SeriesId = seriesId,
            ChapterNumber = chapterNumber,
            PersonId = personId,
            Stage = stage,
        };
        Events.Add(ev);
        return ev;
    }

    /// <summary>Events appended at or after the given index (use Events.Count before an Advance).</summary>
    public IEnumerable<GameEvent> EventsSince(int index) => Events.Skip(index);

    public Series? FindSeries(int id) => Series.FirstOrDefault(s => s.Id == id);
    public Person? FindPerson(int id) => People.FirstOrDefault(p => p.Id == id);
    public Chapter? FindChapter(int chapterId) =>
        Series.SelectMany(s => s.Chapters).FirstOrDefault(c => c.Id == chapterId);
    public Series SeriesOf(Chapter chapter) => Series.First(s => s.Chapters.Contains(chapter));

}
