using System.Text.Json.Serialization;

namespace MangakaSim;

public partial class GameState
{
    public const int CurrentVersion = 2;

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

    // Sub-project 2 state.
    public long Money { get; set; }
    /// <summary>Append-only; Money is the running sum from the starting balance.</summary>
    public List<LedgerEntry> Ledger { get; set; } = new();
    /// <summary>0..100.</summary>
    public double StudioTrackRecord { get; set; }
    /// <summary>One per catalog magazine, in catalog order.</summary>
    public List<MagazineState> Markets { get; set; } = new();
    /// <summary>One per catalog genre.</summary>
    public List<GenreTrend> Trends { get; set; } = new();
    public bool HasInternet { get; set; }
    /// <summary>Month precision: the last calendar month whose trend update ran.</summary>
    public DateTime LastTrendUpdateMonth { get; set; }
    public long DoujinCopiesThisMonth { get; set; }
    public double DoujinFansThisMonth { get; set; }

    /// <summary>Static catalog data, never saved.</summary>
    [JsonIgnore]
    public PublisherCatalog Publishers { get; set; } = PublisherCatalog.LoadDefault();
    [JsonIgnore]
    public TrendCatalog TrendData { get; set; } = TrendCatalog.LoadDefault();

    /// <summary>The hour that the most recent tick simulated: one hour before Clock.Now.</summary>
    [JsonIgnore]
    public DateTime TickStart => Clock.Now.AddHours(-1);

    public static GameState NewGame(int seed = 0) => NewGame(seed, PublisherCatalog.LoadDefault(), TrendCatalog.LoadDefault());

    public static GameState NewGame(int seed, PublisherCatalog publishers, TrendCatalog trends)
    {
        var state = new GameState { RngSeed = seed, Rng = Rng.FromSeed(seed), Publishers = publishers, TrendData = trends };
        var mangaka = new Person
        {
            Id = state.AllocateId(),
            Name = "Aki",
            Reputation = 10,
            Schedule = new Schedule { WorkStartHour = 8, WorkEndHour = 18, DaysOff = { DayOfWeek.Sunday } },
            OvertimeAllowed = true,
        };
        foreach (var stage in StageOrder.All) mangaka.Skills[stage] = 80;
        state.People.Add(mangaka);

        state.Money = Economy.StartingMoney;
        state.LastTrendUpdateMonth = new DateTime(state.Clock.Now.Year, state.Clock.Now.Month, 1);
        state.InitialiseTrends();
        state.InitialiseMarkets();
        return state;
    }

    /// <summary>Appends a ledger entry and updates the running balance.</summary>
    internal LedgerEntry AddLedger(long amount, string reason, int? seriesId = null)
    {
        var entry = new LedgerEntry { Time = Clock.Now, Amount = amount, Reason = reason, SeriesId = seriesId };
        Ledger.Add(entry);
        Money += amount;
        return entry;
    }

    public MagazineState MarketOf(string magazineId) => Markets.First(m => m.MagazineId == magazineId);
    public GenreTrend TrendOf(string genre) => Trends.First(t => t.Genre == TrendRules.Normalise(genre, TrendData));

    /// <summary>The genre's effective popularity multiplier now (1.0 for Iconic series).</summary>
    internal double GenreTrendFor(Series series) =>
        series.IsIconic ? 1.0 : TrendRules.Effective(TrendOf(series.Genre), TrendData, Clock.Now);

    public double PriceIndexNow => Economy.PriceIndex(TrendData, Clock.Now);

    public int AllocateId() => NextId++;

    public void Advance(int hours)
    {
        if (hours < 0) throw new ArgumentOutOfRangeException(nameof(hours), hours, "hours must be >= 0");
        for (var i = 0; i < hours; i++) Tick();
    }

    internal void Tick()
    {
        Clock.Advance();
        WorkStep();
        EditorStep();
        IssueCloseStep();
        SalesStep();
        PitchStep();
        RiskStep();
        DayEndStep();
        if (Clock.Hour == 0) StartNewDay();
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
        RecapWindowStart = Events.Count;
        LedgerWindowStart = Ledger.Count;

        Emit(EventType.DayStarted, $"{Clock.Now:ddd d MMM yyyy} begins.");
        if (Clock.DayOfWeek == DayOfWeek.Monday)
        {
            Emit(EventType.WeekStarted, $"Week of {Clock.Now:d MMM yyyy} begins.");
        }
        RunPlanner();
    }

    internal GameEvent Emit(EventType type, string message, int? seriesId = null,
        int? chapterNumber = null, int? personId = null, Stage? stage = null) =>
        Emit(type, message, new EventContext(seriesId, chapterNumber, personId, stage));

    internal GameEvent Emit(EventType type, string message, EventContext context)
    {
        var ev = new GameEvent
        {
            Time = Clock.Now,
            Type = type,
            Message = message,
            SeriesId = context.SeriesId,
            ChapterNumber = context.ChapterNumber,
            PersonId = context.PersonId,
            Stage = context.Stage,
            MagazineId = context.MagazineId,
            VolumeId = context.VolumeId,
            Rank = context.Rank,
            Amount = context.Amount,
        };
        Events.Add(ev);
        return ev;
    }

    /// <summary>Events appended at or after the given index (use Events.Count before an Advance).</summary>
    public IEnumerable<GameEvent> EventsSince(int index) => Events.Skip(index);

    public Series? FindSeries(int id) => Series.FirstOrDefault(s => s.Id == id);
    public Person? FindPerson(int id) => People.FirstOrDefault(p => p.Id == id);
    /// <summary>Current people and, after sub-project 3, former staff kept for history.</summary>
    public Person? FindAnyPerson(int id) => FindPerson(id) ?? FormerPeople.FirstOrDefault(p => p.Id == id);
    /// <summary>People who quit or were fired, kept so hours, ledger entries and events keep their names.</summary>
    public List<Person> FormerPeople { get; set; } = new();
    public Chapter? FindChapter(int chapterId) =>
        Series.SelectMany(s => s.Chapters).FirstOrDefault(c => c.Id == chapterId);
    public Series SeriesOf(Chapter chapter) => Series.First(s => s.Chapters.Contains(chapter));

}
