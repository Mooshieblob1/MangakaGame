using MangakaSim;

namespace MangakaSim.Tests;

internal static class SimulationFixture
{
    // Fixed seed 2 was selected once using an eight-seed bounded probe.
    private static readonly Lazy<string> OfferSave = new(() =>
    {
        var state = PublishingTests.Started(2, 19);
        state.Apply(new GetOnlineCommand());
        PublishingTests.Until(state, () => state.Series[0].Volumes.Count == 1);
        state.Apply(new StudioActionCommand(StudioAction.Print,state.Series[0].Volumes[0].Id,Amount:100));
        state.Apply(new PitchSeriesCommand(state.Series[0].Id, "hoshigaku-flowers"));
        PublishingTests.Until(state, () => state.Series[0].Publishing != PublishingStatus.Pitching);
        Xunit.Assert.Equal(PublishingStatus.Offered, state.Series[0].Publishing);
        return state.ToJson();
    });
    private static readonly Lazy<string> EightSave = new(() =>
    {
        var state = Serialized();
        // A demanding page count prevents years of completed stock from hiding
        // missed-issue and cancellation transitions in focused lifecycle tests.
        state.Apply(new SetPagesPerChapterCommand(state.Series[0].Id, 80));
        PublishingTests.Until(state, () => state.Series[0].ChaptersPublished == 8);
        return state.ToJson();
    });
    public static GameState Offered() => GameState.FromJson(OfferSave.Value);
    public static GameState Serialized()
    {
        var state = Offered();
        state.Apply(new AcceptOfferCommand(state.Series[0].Id));
        return state;
    }
    public static GameState EightPublished() => GameState.FromJson(EightSave.Value);
    public static void NextIssue(GameState state)
    {
        var market = state.Markets.Single(m => m.MagazineId == state.Series[0].Contract!.MagazineId);
        state.Advance(state.Clock.HoursUntil(market.NextIssueClose));
    }
}
