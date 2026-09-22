using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class QualityStateTests
{
    // Monthly series is never at risk for the prodigy, so no overtime interferes.
    private static GameState Monthly()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Calm", "slice of life", Cadence.Monthly, 19));
        return state;
    }

    [Fact]
    public void Clean_solo_chapter_scores_84_and_reports_it()
    {
        var state = Monthly();
        state.Advance(7 * 24 + 3); // chapter 1 completes Monday 8 April 11:00
        var chapter = state.Series[0].Chapters[0];
        Assert.Equal(ChapterStatus.Complete, chapter.Status);
        Assert.Equal(84, chapter.Quality);
        Assert.All(chapter.Stages, s => Assert.Equal(0, s.OvertimeHours));
        Assert.Equal(29.4, chapter.StageWork(Stage.Name).Contribution, 6);
        var done = Assert.Single(state.Events, e => e.Type == EventType.ChapterCompleted);
        Assert.Contains("quality 84", done.Message);
        Assert.Null(state.Series[0].Chapters[1].Quality);
    }

    [Fact]
    public void Doujin_chapter_moves_reputation_at_half_weight()
    {
        var state = Monthly();
        state.Advance(7 * 24 + 3);
        Assert.Equal(10 + 0.6, state.People[0].Reputation, 6); // (84 - 60) / 20 x 1.0 x 0.5
    }

    [Fact]
    public void Skipped_stage_contributes_nothing()
    {
        var state = Monthly();
        var chapter = state.Series[0].Chapters[0];
        state.Apply(new SkipStageCommand(chapter.Id, Stage.Tones));
        state.Advance(7 * 24); // Tones was 6 working hours; the rest still needs 57
        Assert.Equal(ChapterStatus.Complete, chapter.Status);
        Assert.Equal(0, chapter.StageWork(Stage.Tones).Contribution);
        Assert.Equal(76, chapter.Quality);
    }

    [Fact]
    public void Overtime_hours_are_tracked_per_stage_and_lower_the_contribution()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19)); // at risk from the start
        state.Advance(12); // Monday: 10 regular + 2 overtime hours on Name
        var name = state.Series[0].Chapters[0].StageWork(Stage.Name);
        Assert.Equal(2, name.OvertimeHours);
        state.Advance(24 * 7);
        var chapter = state.Series[0].Chapters[0];
        Assert.Equal(ChapterStatus.Complete, chapter.Status);
        Assert.True(chapter.Stages.Sum(s => s.OvertimeHours) > 0);
        var expectedName = QualityRules.Contribution(Stage.Name, 80, name.OvertimeHours, name.HoursRequired, 0);
        Assert.InRange(name.Contribution, expectedName * 0.98, expectedName); // a little fatigue after the overtime day
        Assert.True(name.Contribution < 29.4);
        Assert.True(chapter.Quality < 84);
        Assert.True(chapter.Quality >= 70);
    }

    [Fact]
    public void Fifth_finished_chapter_releases_a_doujin_volume()
    {
        var state = Monthly();
        state.Advance(40 * 24);
        var series = state.Series[0];
        Assert.True(series.Chapters.Count(c => c.Status == ChapterStatus.Complete) >= 5);
        var volume = Assert.Single(series.Volumes);
        Assert.True(volume.IsDoujin);
        Assert.True(volume.IsReleased);
        Assert.Equal(1, volume.Number);
        Assert.Equal(1, volume.FirstChapter);
        Assert.Equal(5, volume.LastChapter);
        Assert.Equal(84, volume.AverageQuality, 6);
        Assert.Equal(0, volume.WeeksOnSale);
        Assert.Equal(series.Chapters[4].CompletedAt, volume.ReleaseDate);
        var released = Assert.Single(state.Events, e => e.Type == EventType.VolumeReleased);
        Assert.Equal(volume.Id, released.VolumeId);
        Assert.Equal(0.5, state.StudioTrackRecord);
        Assert.True(series.IsInVolume(3));
        Assert.False(series.IsInVolume(6));
    }

    [Fact]
    public void Tenth_chapter_releases_the_second_doujin_volume_and_low_quality_earns_no_track_record()
    {
        var state = Monthly();
        foreach (var stage in StageOrder.All) state.People[0].Skills[stage] = 60; // quality below 75
        state.Advance(110 * 24);
        var series = state.Series[0];
        Assert.True(series.Volumes.Count >= 2, $"{series.Volumes.Count} volumes");
        Assert.Equal(6, series.Volumes[1].FirstChapter);
        Assert.Equal(10, series.Volumes[1].LastChapter);
        Assert.Equal(0, state.StudioTrackRecord);
    }

    [Fact]
    public void Serialized_chapters_do_not_raise_deadline_missed()
    {
        var state = GameState.NewGame();
        state.People[0].OvertimeAllowed = false;
        state.Apply(new CreateSeriesCommand("Rush", "action", Cadence.Weekly, 19));
        var series = state.Series[0];
        series.Publishing = PublishingStatus.Serialized;
        series.Contract = new Contract { MagazineId = "tokiwa-jump", FeePerPage = 9000, SignedAt = state.Clock.Now };
        state.Advance(14 * 24); // 63 working hours plus a 48h review: well past the 8 April due date
        Assert.Equal(ChapterStatus.Complete, series.Chapters[0].Status);
        Assert.True(series.Chapters[0].IsLate);
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.DeadlineMissed);
    }
}
