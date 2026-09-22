using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class BalanceTests
{
    private readonly Balance _balance = new();

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(25, 0.75)]
    [InlineData(50, 1.0)]
    [InlineData(75, 1.5)]
    [InlineData(80, 1.6)]
    [InlineData(100, 2.0)]
    public void Skill_multiplier_is_piecewise_linear(int skill, double expected)
    {
        Assert.Equal(expected, _balance.SkillMultiplier(skill), precision: 6);
    }

    [Fact]
    public void Skill_multiplier_clamps_out_of_range_skill()
    {
        Assert.Equal(0.5, _balance.SkillMultiplier(-10), precision: 6);
        Assert.Equal(2.0, _balance.SkillMultiplier(150), precision: 6);
    }

    [Theory]
    [InlineData(Stage.Name, 22.8)]
    [InlineData(Stage.Pencils, 28.5)]
    [InlineData(Stage.Inks, 19.0)]
    [InlineData(Stage.Backgrounds, 19.0)]
    [InlineData(Stage.Tones, 9.5)]
    public void Hours_required_for_19_page_chapter(Stage stage, double expected)
    {
        Assert.Equal(expected, _balance.HoursRequired(stage, 19), precision: 6);
    }

    [Fact]
    public void Overtime_cap_is_two_hours()
    {
        Assert.Equal(2, _balance.OvertimeCap);
    }

    [Theory]
    [InlineData(Cadence.Weekly, "1996-04-08T00:00:00")]
    [InlineData(Cadence.Biweekly, "1996-04-15T00:00:00")]
    [InlineData(Cadence.Monthly, "1996-05-01T00:00:00")]
    public void Next_due_adds_cadence_interval(Cadence cadence, string expected)
    {
        var from = new DateTime(1996, 4, 1, 0, 0, 0);
        Assert.Equal(DateTime.Parse(expected), CadenceRules.NextDue(from, cadence));
    }

    [Fact]
    public void Default_settings_auto_pause_on_recap_chapter_complete_and_deadline_missed()
    {
        var settings = Settings.Default();
        Assert.True(settings.AutoPause[EventType.DailyRecap]);
        Assert.True(settings.AutoPause[EventType.ChapterCompleted]);
        Assert.True(settings.AutoPause[EventType.DeadlineMissed]);
        Assert.False(settings.AutoPause[EventType.DayStarted]);
        Assert.False(settings.AutoPause[EventType.StageCompleted]);
        Assert.Equal(Enum.GetValues<EventType>().Length, settings.AutoPause.Count);
    }

    [Fact]
    public void Default_settings_auto_pause_on_publishing_events()
    {
        var settings = Settings.Default();
        foreach (var type in new[]
                 {
                     EventType.SerializationOffered, EventType.PitchRejected, EventType.EditorRedoRequested,
                     EventType.CancellationWarning, EventType.SeriesCancelled, EventType.VolumeMilestone,
                     EventType.ConventionRecap, EventType.SeriesBecameIconic,
                 })
            Assert.True(settings.AutoPause[type], type.ToString());
        Assert.False(settings.AutoPause[EventType.ChapterPublished]);
        Assert.False(settings.AutoPause[EventType.RankingPublished]);
    }

    [Fact]
    public void Publishing_event_types_are_appended_in_spec_order()
    {
        Assert.Equal(35, Enum.GetValues<EventType>().Length);
        Assert.Equal(10, (int)EventType.CommandApplied);
        Assert.Equal(11, (int)EventType.PitchSubmitted);
        Assert.Equal(19, (int)EventType.ChapterPublished);
        Assert.Equal(34, (int)EventType.WentOnline);
    }
}
