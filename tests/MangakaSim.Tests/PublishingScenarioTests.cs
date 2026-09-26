using Xunit;

namespace MangakaSim.Tests;

public class PublishingScenarioTests
{
    [Fact]
    public void Two_year_public_command_run_matches_saved_continuation_and_replay()
    {
        var state = SimulationFixture.Serialized();
        var continued = GameState.FromJson(state.ToJson());
        var target = GameClock.Start.AddYears(2);
        state.Advance(state.Clock.HoursUntil(target));
        while (continued.Clock.Now < target)
        {
            continued.Advance(Math.Min(24 * 90, continued.Clock.HoursUntil(target)));
            continued = GameState.FromJson(continued.ToJson());
        }
        Assert.Equal(state.ToJson(), continued.ToJson());
        var replay = GameState.NewGame(state.RngSeed);
        foreach (var entry in state.CommandLog)
        {
            replay.Advance(replay.Clock.HoursUntil(entry.Time));
            replay.Apply(entry.Command);
        }
        replay.Advance(replay.Clock.HoursUntil(target));
        Assert.Equal(state.ToJson(), replay.ToJson());
        Assert.Contains(state.Ledger, e => e.Reason == "doujin sales" && e.Amount > 0);
        Assert.Contains(state.Ledger, e => e.Reason == "chapter fee" && e.Amount > 0);
        Assert.Contains(state.Ledger, e => e.Reason.StartsWith("royalties") && e.Amount > 0);
        Assert.True(state.Series[0].ChaptersPublished >= 12);
        Assert.Contains(state.Series[0].Volumes, v => !v.IsDoujin && v.CopiesSold > 0);
        Assert.Equal(state.ControlledBusiness.Account.OpeningBalance + state.Ledger.Sum(e => e.Amount), state.Money);
        Assert.Contains(state.Markets, m => m.RetiredFillers.Count > 0);
    }

    [Fact]
    public void Every_command_kind_replays_with_lifecycle_and_queue_changes()
    {
        var state = SimulationFixture.Offered();
        var series = state.Series[0];
        state.Apply(new DeclineOfferCommand(series.Id));
        state.Apply(new StudioActionCommand(StudioAction.SetPipeline,series.Id,2,3,2));
        var chapter = series.Chapters.Last();
        state.Apply(new PinStageCommand(state.People[0].Id, chapter.Id, Stage.Name));
        state.Apply(new UnpinStageCommand(state.People[0].Id, chapter.Id, Stage.Name));
        state.Apply(new ReorderQueueCommand(state.People[0].Id, state.People[0].Queue.AsEnumerable().Reverse().ToList()));
        state.Apply(new SetScheduleCommand(state.People[0].Id, 8, 18, new() { DayOfWeek.Sunday }));
        state.Apply(new SetOvertimeAllowedCommand(state.People[0].Id, true));
        state.Apply(new SetCadenceCommand(series.Id, Cadence.Monthly));
        state.Apply(new SetPagesPerChapterCommand(series.Id, 19));
        state.Apply(new PauseSeriesCommand(series.Id));
        state.Apply(new ResumeSeriesCommand(series.Id));
        state.Apply(new SkipStageCommand(chapter.Id, Stage.Tones));
        PublishingTests.Until(state, () => chapter.Status == ChapterStatus.Complete);
        state.Apply(new EndSeriesCommand(series.Id));
        var replay = GameState.NewGame(state.RngSeed);
        foreach (var entry in GameState.FromJson(state.ToJson()).CommandLog)
        {
            replay.Advance(replay.Clock.HoursUntil(entry.Time));
            replay.Apply(entry.Command);
        }
        Assert.Equal(state.ToJson(), replay.ToJson());
        // Accept and Withdraw use the same saved public-command starting point.
        var serialized = SimulationFixture.Serialized();
        serialized.Apply(new WithdrawSeriesCommand(serialized.Series[0].Id));
        Assert.Equal(serialized.ToJson(), GameState.FromJson(serialized.ToJson()).ToJson());
        var secondReplay = GameState.NewGame(serialized.RngSeed);
        foreach (var entry in GameState.FromJson(serialized.ToJson()).CommandLog)
        {
            secondReplay.Advance(secondReplay.Clock.HoursUntil(entry.Time));
            secondReplay.Apply(entry.Command);
        }
        Assert.Equal(serialized.ToJson(), secondReplay.ToJson());
        var all = state.CommandLog.Concat(serialized.CommandLog).Select(e => e.Command.GetType()).Distinct();
        Assert.Equal(18, all.Count());
    }
}
