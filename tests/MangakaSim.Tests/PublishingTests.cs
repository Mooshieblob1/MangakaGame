using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

public class PublishingTests
{
    internal static GameState Started(int seed = 0, int pages = 1)
    {
        var state = GameState.NewGame(seed);
        state.Apply(new CreateSeriesCommand("Paper Garden", "drama", Cadence.Monthly, pages));
        return state;
    }
    internal static void Until(GameState state, Func<bool> condition, int hours = 24 * 730)
    {
        for (var i = 0; i < hours && !condition(); i++) state.Advance(1);
        Assert.True(condition(), $"Timed out at {state.Clock.Now}: {string.Join("; ", state.Events.TakeLast(8).Select(e => e.Message))}");
    }

    [Fact]
    public void New_game_has_market_state_and_a_reconciled_founding_transfer()
    {
        var state = GameState.NewGame(42);
        Assert.Equal(300000, state.Money);
        Assert.Equal("founding contribution", Assert.Single(state.Ledger).Reason);
        Assert.Equal(6, state.Markets.Count);
        Assert.Equal(12, state.Trends.Count);
        Assert.Equal(10, state.People[0].Reputation);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }

    [Fact]
    public void Pitch_replaces_only_untouched_draft_and_freezes_31_pages()
    {
        var state = Started();
        var series = state.Series[0];
        var old = series.Chapters[0];
        state.Apply(new PinStageCommand(state.People[0].Id, old.Id, Stage.Name));
        state.Apply(new PitchSeriesCommand(series.Id, "hoshigaku-flowers"));
        var sample = Assert.Single(series.Chapters);
        Assert.True(sample.IsOneShot);
        Assert.Equal(31, sample.Pages);
        Assert.Equal(PublishingStatus.Pitching, series.Publishing);
        Assert.Empty(state.People[0].Pins);
        Assert.True(sample.Number > old.Number);
        state.Apply(new SetPagesPerChapterCommand(series.Id, 8));
        Assert.Equal(31, sample.Pages);
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PitchSeriesCommand(series.Id, "tokiwa-jump")));
        Assert.Equal(before, state.ToJson());
    }

    [Fact]
    public void Partial_work_cannot_be_discarded_by_pitching()
    {
        var state = Started();
        state.Advance(1);
        var draft=state.Series[0].Chapters[0];
        var hours=draft.Stages.Sum(w=>w.HoursDone);
        state.Apply(new PitchSeriesCommand(state.Series[0].Id, "hoshigaku-flowers"));
        Assert.Contains(draft,state.Series[0].Chapters);
        state.Advance(4);
        Assert.Equal(hours,draft.Stages.Sum(w=>w.HoursDone));
        Assert.Contains(state.Series[0].Chapters,c=>c.IsOneShot&&c.Pages==31&&c.Stages.Sum(w=>w.HoursDone)>0);
        Assert.Equal(state.ToJson(),GameState.FromJson(state.ToJson()).ToJson());
    }

    [Fact]
    public void All_skipped_sample_still_waits_for_editor_and_resumes_after_load()
    {
        var state = Started();
        state.Apply(new PitchSeriesCommand(state.Series[0].Id, "hoshigaku-flowers"));
        var c = state.Series[0].Chapters.Single();
        foreach (var stage in StageOrder.All) state.Apply(new SkipStageCommand(c.Id, stage));
        Assert.Equal(EditorStatus.AwaitingReview, c.Editor);
        Assert.NotEqual(ChapterStatus.Complete, c.Status);
        var loaded = GameState.FromJson(state.ToJson());
        state.Advance(1000);
        loaded.Advance(1000);
        Assert.Equal(state.ToJson(), loaded.ToJson());
        Assert.InRange(c.RedoCount, 0, 2);
        Assert.Equal(EditorStatus.Approved, c.Editor);
    }

    [Fact]
    public void Five_doujin_chapters_release_and_sell_then_internet_debits_once()
    {
        var state = Started();
        var series = state.Series[0];
        Until(state, () => series.Volumes.Count > 0);
        var volume = series.Volumes[0];
        Assert.True(volume.IsDoujin);
        Assert.Equal(5, volume.ChapterIds.Count);
        Assert.Null(volume.ReleasedAt);
        state.Apply(new StudioActionCommand(StudioAction.Print,volume.Id,Amount:100));
        state.Advance(24);
        Assert.NotNull(volume.ReleasedAt);
        Until(state, () => volume.WeeksOnSale > 0);
        Assert.True(volume.CopiesSold > 0);
        var cost = Economy.InternetCost(state.TrendCatalog, state.Clock.Now);
        var money = state.Money;
        state.Apply(new GetOnlineCommand());
        Assert.Equal(money - cost, state.Money);
        Assert.Equal(8, volume.SalesWindowWeeks);
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new GetOnlineCommand()));
        Assert.Equal(before, state.ToJson());
        Assert.Equal(state.ControlledBusiness.Account.OpeningBalance + state.Ledger.Sum(e => e.Amount), state.Money);
    }

    [Fact]
    public void Completed_pitch_waiting_for_close_can_be_ended_without_dangling_references()
    {
        var state = Started();
        var series = state.Series[0];
        state.Apply(new PitchSeriesCommand(series.Id, "hoshigaku-flowers"));
        var sample = series.Chapters.Single();
        Until(state, () => sample.Status == ChapterStatus.Complete);
        Assert.Equal(PublishingStatus.Pitching, series.Publishing);
        Assert.NotEmpty(series.LifetimeHoursByPerson);
        state.Apply(new EndSeriesCommand(series.Id));
        Assert.Equal(SeriesStatus.Ended, series.Status);
        Assert.Empty(series.Chapters);
        Assert.Empty(state.People[0].Queue);
        Assert.NotEmpty(series.LifetimeHoursByPerson);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }
}
