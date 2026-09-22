using System.Text.Json.Serialization;

namespace MangakaSim;

public partial class GameState
{
    public const int CurrentVersion = 1;

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

    public static GameState NewGame(int seed = 0)
    {
        var state = new GameState { RngSeed = seed, Rng = Rng.FromSeed(seed) };
        var mangaka = new Person
        {
            Id = state.AllocateId(),
            Name = "Aki",
            Schedule = new Schedule { WorkStartHour = 8, WorkEndHour = 18, DaysOff = { DayOfWeek.Sunday } },
            OvertimeAllowed = true,
        };
        foreach (var stage in StageOrder.All) mangaka.Skills[stage] = 80;
        state.People.Add(mangaka);
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
        WorkStep();
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
    public Chapter? FindChapter(int chapterId) =>
        Series.SelectMany(s => s.Chapters).FirstOrDefault(c => c.Id == chapterId);
    public Series SeriesOf(Chapter chapter) => Series.First(s => s.Chapters.Contains(chapter));

}
