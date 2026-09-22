using MangakaSim;
using Xunit;
using Xunit.Abstractions;
using static MangakaSim.Tests.AssignmentTests;

namespace MangakaSim.Tests;

/// <summary>Balance guardrails for the staff constants. Each year is simulated once and shared.</summary>
public class StudioScenarioTests
{
    private readonly ITestOutputHelper _out;
    public StudioScenarioTests(ITestOutputHelper output) => _out = output;

    internal sealed record YearResult(GameState State, int Published, int Missed, double MeanQuality, int Moonlighting, long Fees, long Costs);

    /// <summary>A weekly action series at Tokiwa Jump from day one, with or without two skill-60 assistants.</summary>
    internal static GameState WeeklyJumpStudio(bool staffed, int endHour = 18, int salary = 240_000, bool amenities = true, int seed = 7)
    {
        var state = GameState.NewGame(seed);
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        var series = state.Series[0];
        series.Publishing = PublishingStatus.Serialized;
        series.Contract = new Contract { MagazineId = "tokiwa-jump", FeePerPage = 10_000, SignedAt = state.Clock.Now, FirstChapterNumber = 1 };
        series.OpenChapter!.DueDate = state.MarketOf("tokiwa-jump").NextIssueClose.AddDays(7);
        state.Apply(new SetScheduleCommand(state.People[0].Id, 8, endHour, new HashSet<DayOfWeek> { DayOfWeek.Sunday }));
        if (staffed)
        {
            state.Apply(new MovePremisesCommand("apartment"));
            if (amenities)
            {
                state.Apply(new BuyAmenityCommand("fridge"));
                state.Apply(new BuyAmenityCommand("office-chairs"));
            }
            foreach (var name in new[] { "Ren Ogawa", "Mio Sakai" })
            {
                var assistant = AddAssistant(state, name, 60, salary);
                state.Apply(new SetScheduleCommand(assistant.Id, 8, endHour, new HashSet<DayOfWeek> { DayOfWeek.Sunday }));
            }
        }
        state.RunPlanner();
        return state;
    }

    private static YearResult RunYear(GameState state)
    {
        state.Advance(24 * 7 * 52);
        var series = state.Series[0];
        var published = series.Chapters.Where(c => c.IsPublished).ToList();
        return new YearResult(state, published.Count,
            state.Events.Count(e => e.Type == EventType.IssueMissed),
            published.Count > 0 ? published.Average(c => c.Quality ?? 0) : 0,
            state.Events.Count(e => e.Type == EventType.MoonlightingStarted),
            state.Ledger.Where(l => l.Reason == "chapter fee").Sum(l => l.Amount),
            -state.Ledger.Where(l => l.Reason is "salary" or "rent" or "upkeep").Sum(l => l.Amount));
    }

    private static readonly Lazy<YearResult> Staffed = new(() => RunYear(WeeklyJumpStudio(staffed: true)));
    private static readonly Lazy<YearResult> Solo = new(() => RunYear(WeeklyJumpStudio(staffed: false)));
    private static readonly Lazy<YearResult> LongDays = new(() => RunYear(WeeklyJumpStudio(staffed: false, endHour: 20)));
    private static readonly Lazy<YearResult> Underpaid = new(() => RunYear(WeeklyJumpStudio(staffed: true, endHour: 20, salary: 150_000, amenities: false)));

    private void Report(string name, YearResult r) =>
        _out.WriteLine($"{name}: published {r.Published}, missed {r.Missed}, mean quality {r.MeanQuality:0.0}, moonlighting {r.Moonlighting}, fees {r.Fees:N0}, costs {r.Costs:N0}, money {r.State.Money:N0}, lead fatigue {r.State.People[0].Fatigue:0.0}");

    [Fact]
    public void A_staffed_studio_holds_a_weekly_flagship_slot()
    {
        var r = Staffed.Value;
        Report("staffed", r);
        Assert.True(r.Published >= 48, $"published {r.Published}");
        Assert.True(r.Missed <= 2, $"missed {r.Missed}");
        Assert.Equal(PublishingStatus.Serialized, r.State.Series[0].Publishing);
        Assert.DoesNotContain(r.State.Events, e => e.Type == EventType.SeriesCancelled);
        Assert.True(r.MeanQuality >= 75, $"quality {r.MeanQuality}"); // skill-60 hands draw below Aki's own 84
        Assert.Equal(0, r.Moonlighting);
        Assert.Equal(3, r.State.People.Count);
        Assert.All(r.State.People, p => Assert.True(p.Happiness >= 50, $"{p.Name} at {p.Happiness}"));
    }

    [Fact]
    public void The_mangaka_alone_misses_issues_the_staffed_studio_does_not()
    {
        var solo = Solo.Value;
        var staffed = Staffed.Value;
        Report("solo", solo);
        Assert.True(solo.Missed >= 3, $"missed {solo.Missed}");
        Assert.True(solo.Published < staffed.Published, $"solo {solo.Published} vs staffed {staffed.Published}");
        Assert.True(solo.State.People[0].Happiness < staffed.State.People[0].Happiness); // the garage is bleak
    }

    [Fact]
    public void Long_days_grind_the_solo_mangaka_down_and_cost_quality()
    {
        var clean = Solo.Value;
        var long12 = LongDays.Value;
        Report("long days", long12);
        Assert.True(long12.State.People[0].Fatigue >= 60, $"fatigue {long12.State.People[0].Fatigue}");
        Assert.True(long12.MeanQuality <= clean.MeanQuality - 5, $"{long12.MeanQuality} vs {clean.MeanQuality}");
        Assert.True(long12.Published >= clean.Published, $"long {long12.Published} vs clean {clean.Published}"); // the hours buy chapters; the quality pays
        Assert.Equal(0, Staffed.Value.State.People[0].Fatigue); // three desks on ten-hour days leave slack
    }

    [Fact]
    public void Underpaid_assistants_in_a_bare_studio_moonlight()
    {
        var r = Underpaid.Value;
        Report("underpaid", r);
        Assert.True(r.Moonlighting >= 1, $"moonlighting events {r.Moonlighting}");
        Assert.Contains(r.State.People, p => p.Role == PersonRole.Assistant && p.Happiness < 50);
        Assert.Equal(0, Staffed.Value.Moonlighting);
    }

    [Fact]
    public void Chapter_fees_alone_cover_the_running_costs_of_a_staffed_studio()
    {
        var r = Staffed.Value;
        Assert.True(r.Fees >= r.Costs, $"fees {r.Fees:N0} against costs {r.Costs:N0}");
        Assert.True(r.State.Money > 500_000);
        Assert.Equal(22, r.State.Ledger.Count(l => l.Reason == "salary")); // two assistants, May to March
        Assert.Equal(11, r.State.Ledger.Count(l => l.Reason == "rent"));
        GameState.FromJson(r.State.ToJson());
    }
}
