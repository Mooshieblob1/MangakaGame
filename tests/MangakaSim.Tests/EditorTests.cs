using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class EditorTests
{
    /// <summary>A monthly series forced into serialization at Flowers (tier 3, 24h reviews). No overtime interferes.</summary>
    internal static GameState SerializedAtFlowers(int seed = 0, int nameSkill = 80)
    {
        var state = GameState.NewGame(seed);
        state.People[0].Skills[Stage.Name] = nameSkill;
        state.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Monthly, 19));
        var series = state.Series[0];
        series.Publishing = PublishingStatus.Serialized;
        series.Contract = new Contract { MagazineId = "hoshigaku-flowers", FeePerPage = 6000, SignedAt = state.Clock.Now };
        return state;
    }

    internal static int FindSeed(Func<int, GameState> build, Func<GameState, bool> outcome, int maxSeeds = 500)
    {
        for (var seed = 0; seed < maxSeeds; seed++)
            if (outcome(build(seed))) return seed;
        throw new InvalidOperationException("no seed produced the outcome");
    }

    /// <summary>Advances hour by hour until the first chapter's Name stage is done (Tuesday 13:00 at skill 80).</summary>
    private static GameState AfterName(int seed, int nameSkill = 80)
    {
        var state = SerializedAtFlowers(seed, nameSkill);
        var name = state.Series[0].Chapters[0].StageWork(Stage.Name);
        for (var i = 0; i < 24 * 7 && name.Status != StageStatus.Complete; i++) state.Advance(1);
        Assert.Equal(StageStatus.Complete, name.Status);
        return state;
    }

    [Fact]
    public void Name_completion_enters_review_and_blocks_pencils()
    {
        var state = AfterName(0);
        var chapter = state.Series[0].Chapters[0];
        Assert.Equal(new DateTime(1996, 4, 2, 13, 0, 0), state.Clock.Now);
        Assert.Equal(EditorStatus.AwaitingReview, chapter.Editor);
        Assert.Equal(new DateTime(1996, 4, 3, 13, 0, 0), chapter.EditorDecisionAt);
        Assert.False(state.IsStartable(new QueueRef(chapter.Id, Stage.Pencils)));
        Assert.Null(state.People[0].CurrentTask);
        Assert.Contains(new QueueRef(chapter.Id, Stage.Pencils), state.People[0].Queue);

        state.Advance(23); // Wednesday 12:00: still under review, nothing worked
        Assert.Equal(0, chapter.StageWork(Stage.Pencils).HoursDone);
        Assert.Equal(EditorStatus.AwaitingReview, chapter.Editor);
    }

    [Fact]
    public void Approval_unblocks_pencils_at_the_decision_time()
    {
        var seed = FindSeed(s => { var g = AfterName(s); g.Advance(24); return g; },
            g => g.Series[0].Chapters[0].Editor == EditorStatus.Approved);
        var state = AfterName(seed);
        state.Advance(24); // Wednesday 13:00
        var chapter = state.Series[0].Chapters[0];
        Assert.Equal(EditorStatus.Approved, chapter.Editor);
        var approved = Assert.Single(state.Events, e => e.Type == EventType.EditorApproved);
        Assert.Equal("hoshigaku-flowers", approved.MagazineId);
        Assert.Contains("name quality 84", approved.Message);
        Assert.Equal(new QueueRef(chapter.Id, Stage.Pencils), state.People[0].CurrentTask);
        state.Advance(1);
        Assert.Equal(1.6, chapter.StageWork(Stage.Pencils).HoursDone, 6);
        Assert.Equal(10, state.People[0].Reputation + 0); // unchanged by approval
    }

    [Fact]
    public void Redo_resets_the_name_and_costs_reputation()
    {
        // Name skill 30 puts the name quality near the tier 3 threshold, so both outcomes are common.
        var seed = FindSeed(s => { var g = AfterName(s, 30); g.Advance(24); return g; },
            g => g.Series[0].Chapters[0].Editor == EditorStatus.RedoRequested);
        var state = AfterName(seed, 30);
        var repBefore = state.People[0].Reputation;
        state.Advance(24);
        var chapter = state.Series[0].Chapters[0];
        var name = chapter.StageWork(Stage.Name);
        Assert.Equal(EditorStatus.RedoRequested, chapter.Editor);
        Assert.Equal(1, chapter.RedoCount);
        Assert.Equal(StageStatus.NotStarted, name.Status);
        Assert.Equal(0, name.HoursDone);
        Assert.Equal(0, name.Contribution);
        Assert.Equal(state.People[0].Id, name.AssignedTo);
        Assert.Equal(repBefore - 0.5, state.People[0].Reputation, 6);
        Assert.Equal(0, state.StudioTrackRecord); // clamped at zero
        var redo = Assert.Single(state.Events, e => e.Type == EventType.EditorRedoRequested);
        Assert.Contains("name quality 44", redo.Message);
        Assert.Equal(new QueueRef(chapter.Id, Stage.Name), state.People[0].CurrentTask);

        // The redone name re-enters review with a redo bonus on its contribution.
        for (var i = 0; i < 24 * 7 && name.Status != StageStatus.Complete; i++) state.Advance(1);
        Assert.Equal(StageStatus.Complete, name.Status);
        Assert.Equal(EditorStatus.AwaitingReview, chapter.Editor);
        Assert.Equal(35 * QualityRules.SkillFactor(30, 1), name.Contribution, 6);
        Assert.Equal(state.Clock.Now.AddHours(24), chapter.EditorDecisionAt);
    }

    [Fact]
    public void Third_submission_is_always_approved()
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var state = SerializedAtFlowers(seed, nameSkill: 0); // name quality 20: 5% otherwise
            state.Series[0].Chapters[0].RedoCount = 2;
            state.Advance(24 * 6);
            Assert.Equal(EditorStatus.Approved, state.Series[0].Chapters[0].Editor);
        }
    }

    [Fact]
    public void Skipping_the_name_submits_a_zero_quality_name()
    {
        var state = SerializedAtFlowers();
        var chapter = state.Series[0].Chapters[0];
        state.Apply(new SkipStageCommand(chapter.Id, Stage.Name));
        Assert.Equal(EditorStatus.AwaitingReview, chapter.Editor);
        Assert.Equal(state.Clock.Now.AddHours(24), chapter.EditorDecisionAt);
        Assert.Null(state.People[0].CurrentTask);
    }

    [Fact]
    public void Doujin_chapters_never_see_an_editor()
    {
        var state = GameState.NewGame();
        state.Apply(new CreateSeriesCommand("Calm", "slice of life", Cadence.Monthly, 19));
        state.Advance(24 + 6);
        var chapter = state.Series[0].Chapters[0];
        Assert.Equal(StageStatus.Complete, chapter.StageWork(Stage.Name).Status);
        Assert.Equal(EditorStatus.NotRequired, chapter.Editor);
        Assert.Null(chapter.EditorDecisionAt);
        Assert.Equal(StageStatus.InProgress, chapter.StageWork(Stage.Pencils).Status);
    }

    [Fact]
    public void Review_state_survives_a_save_and_load()
    {
        var state = AfterName(0);
        var loaded = GameState.FromJson(state.ToJson());
        Assert.Equal(EditorStatus.AwaitingReview, loaded.Series[0].Chapters[0].Editor);
        state.Advance(30);
        loaded.Advance(30);
        Assert.Equal(state.ToJson(), loaded.ToJson());
    }
}
