using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class PitchTests
{
    private const string Flowers = "hoshigaku-flowers";

    private static GameState Fresh(int seed = 0)
    {
        var state = GameState.NewGame(seed);
        state.Apply(new CreateSeriesCommand("Petals", "romance", Cadence.Monthly, 19));
        return state;
    }

    /// <summary>Pitches, then advances until the one-shot's pitch is resolved (offer or rejection).</summary>
    private static GameState Resolved(int seed)
    {
        var state = Fresh(seed);
        state.Apply(new PitchSeriesCommand(state.Series[0].Id, Flowers));
        for (var i = 0; i < 24 * 60 && state.Series[0].Publishing == PublishingStatus.Pitching; i++) state.Advance(1);
        Assert.NotEqual(PublishingStatus.Pitching, state.Series[0].Publishing);
        return state;
    }

    private static int AcceptingSeed() => EditorTests.FindSeed(Resolved, s => s.Series[0].Publishing == PublishingStatus.Offered);
    private static int RejectingSeed() => EditorTests.FindSeed(Resolved, s => s.Series[0].Publishing == PublishingStatus.Unpublished);

    [Fact]
    public void Pitch_replaces_the_untouched_chapter_with_a_31_page_one_shot()
    {
        var state = Fresh();
        var series = state.Series[0];
        var before = series.Chapters[0].Id;
        state.Apply(new PitchSeriesCommand(series.Id, Flowers));

        var oneShot = Assert.Single(series.Chapters);
        Assert.NotEqual(before, oneShot.Id);
        Assert.Equal(1, oneShot.Number);
        Assert.True(oneShot.IsOneShot);
        Assert.Equal(Flowers, oneShot.PitchMagazineId);
        Assert.Equal(31, oneShot.Pages);
        Assert.Equal(37.2, oneShot.StageWork(Stage.Name).HoursRequired, 6);
        Assert.Equal(new DateTime(1996, 5, 1, 18, 0, 0), oneShot.DueDate); // Flowers closes every 28 days from 3 April; first close >= 15 April
        Assert.Equal(PublishingStatus.Pitching, series.Publishing);
        Assert.Equal(EditorStatus.NotRequired, oneShot.Editor);
        var submitted = Assert.Single(state.Events, e => e.Type == EventType.PitchSubmitted);
        Assert.Equal(Flowers, submitted.MagazineId);
        Assert.Equal(new QueueRef(oneShot.Id, Stage.Name), state.People[0].CurrentTask);
        Assert.Equal(19, series.PagesPerChapter);
    }

    [Fact]
    public void Pitch_validates_and_leaves_state_untouched_on_failure()
    {
        var state = Fresh();
        var id = state.Series[0].Id;
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PitchSeriesCommand(id, "nowhere")));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PitchSeriesCommand(999, Flowers)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PitchSeriesCommand(id, null!)));
        Assert.Equal(before, state.ToJson());

        state.Apply(new PauseSeriesCommand(id));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PitchSeriesCommand(id, Flowers)));
        state.Apply(new ResumeSeriesCommand(id));

        state.Advance(1); // Name has an hour on it now
        var ex = Assert.Throws<InvalidCommandException>(() => state.Apply(new PitchSeriesCommand(id, Flowers)));
        Assert.Contains("Finish the current chapter", ex.Message);
    }

    [Fact]
    public void A_pitching_series_cannot_pitch_again()
    {
        var state = Fresh();
        var id = state.Series[0].Id;
        state.Apply(new PitchSeriesCommand(id, Flowers));
        var ex = Assert.Throws<InvalidCommandException>(() => state.Apply(new PitchSeriesCommand(id, "tokiwa-jump")));
        Assert.Contains("Pitching", ex.Message);
    }

    [Fact]
    public void One_shot_goes_through_the_editor_and_resolves_at_the_close()
    {
        var state = Fresh(AcceptingSeed());
        state.Apply(new PitchSeriesCommand(state.Series[0].Id, Flowers));
        var oneShot = state.Series[0].Chapters[0];
        for (var i = 0; i < 24 * 30 && oneShot.Status != ChapterStatus.Complete; i++) state.Advance(1);
        Assert.Equal(ChapterStatus.Complete, oneShot.Status);
        Assert.Equal(EditorStatus.Approved, oneShot.Editor);
        Assert.Contains(state.Events, e => e.Type == EventType.EditorApproved && e.MagazineId == Flowers);
        Assert.True(state.Clock.Now < oneShot.DueDate, "the prodigy finishes a one-shot in under two weeks");
        Assert.Equal(PublishingStatus.Pitching, state.Series[0].Publishing);
        Assert.Equal(2, state.Series[0].Chapters.Count); // the planner started the next doujin chapter

        state.Advance(state.Clock.HoursUntil(oneShot.DueDate) - 1);
        Assert.Equal(PublishingStatus.Pitching, state.Series[0].Publishing);
        state.Advance(1);
        Assert.Equal(oneShot.DueDate, state.Clock.Now);
        Assert.Equal(PublishingStatus.Offered, state.Series[0].Publishing);
    }

    [Fact]
    public void Rejection_sets_a_26_week_cooldown_and_names_the_weakest_factor()
    {
        var state = Resolved(RejectingSeed());
        var series = state.Series[0];
        var rejected = Assert.Single(state.Events, e => e.Type == EventType.PitchRejected);
        Assert.Equal(Flowers, rejected.MagazineId);
        Assert.Contains("passed on Petals", rejected.Message);
        Assert.Equal(state.Clock.Now.AddDays(7 * 26), series.PitchCooldowns[Flowers]);
        Assert.Equal(PublishingStatus.Unpublished, series.Publishing);
        Assert.Null(series.PendingOffer);
        Assert.Equal(0.25, state.StudioTrackRecord); // quality 84 >= 70
        Assert.True(series.Chapters[0].IsOneShot && series.Chapters[0].Status == ChapterStatus.Complete);

        // Cooldown blocks a second pitch at Flowers but not elsewhere.
        var open = series.OpenChapter!;
        foreach (var w in open.Stages) { w.HoursDone = 0; w.Status = StageStatus.NotStarted; }
        var ex = Assert.Throws<InvalidCommandException>(() => state.Apply(new PitchSeriesCommand(series.Id, Flowers)));
        Assert.Contains("will not look", ex.Message);
        state.Apply(new PitchSeriesCommand(series.Id, "kaidan-afternoon"));
        Assert.Equal(PublishingStatus.Pitching, series.Publishing);
    }

    [Fact]
    public void Success_creates_an_offer_four_issues_out()
    {
        var state = Resolved(AcceptingSeed());
        var series = state.Series[0];
        var offer = series.PendingOffer!;
        var flowers = PublisherCatalog.LoadDefault().Require(Flowers);
        Assert.Equal(Flowers, offer.MagazineId);
        Assert.Equal(PitchRules.FeePerPage(flowers, state.EffectiveReputation, 1.0), offer.FeePerPage);
        Assert.InRange(offer.FeePerPage, 6000, 11000);
        Assert.Equal(0, offer.FeePerPage % 100);
        Assert.Equal(new DateTime(1996, 5, 1, 18, 0, 0), state.Clock.Now);
        Assert.Equal(new DateTime(1996, 8, 21, 18, 0, 0), offer.FirstIssueClose); // 29 May + 3 x 28 days
        Assert.Equal(offer.FirstIssueClose, offer.ExpiresAt);
        var offered = Assert.Single(state.Events, e => e.Type == EventType.SerializationOffered);
        Assert.Equal(offer.FeePerPage, offered.Amount);
        Assert.Null(series.Contract);
        Assert.Empty(series.Volumes); // the one-shot waits; no doujin volume from one chapter
    }

    [Fact]
    public void Accepting_signs_the_contract_and_redirects_the_open_chapter()
    {
        var state = Resolved(AcceptingSeed());
        var series = state.Series[0];
        var offer = series.PendingOffer!;
        var open = series.OpenChapter!;
        var nameDone = open.StageWork(Stage.Name).IsDone;
        var pencilsStarted = open.StageWork(Stage.Pencils).HoursDone > 0;

        state.Apply(new AcceptOfferCommand(series.Id));

        Assert.Equal(PublishingStatus.Serialized, series.Publishing);
        Assert.Null(series.PendingOffer);
        Assert.Equal(Flowers, series.Contract!.MagazineId);
        Assert.Equal(offer.FeePerPage, series.Contract.FeePerPage);
        Assert.Equal(state.Clock.Now, series.Contract.SignedAt);
        Assert.Equal(0, series.Contract.ChaptersPublished);
        Assert.Equal(Cadence.Monthly, series.Cadence);
        Assert.Equal(offer.FirstIssueClose, open.DueDate);
        var expectedEditor = !nameDone ? EditorStatus.NotRequired : pencilsStarted ? EditorStatus.Approved : EditorStatus.AwaitingReview;
        Assert.Equal(expectedEditor, open.Editor);
        Assert.Contains(state.Events, e => e.Type == EventType.OfferAccepted && e.MagazineId == Flowers);

        // The chapter after the redirected one is due at the following close (the pipeline may already have opened it).
        var next = series.Chapters.Last() == open ? state.CreateNextChapter(series) : series.Chapters.Last();
        Assert.Equal(offer.FirstIssueClose.AddDays(28), next.DueDate);
        Assert.Equal(19, next.Pages);
    }

    [Fact]
    public void Declining_returns_to_doujin_without_a_cooldown()
    {
        var state = Resolved(AcceptingSeed());
        var series = state.Series[0];
        state.Apply(new DeclineOfferCommand(series.Id));
        Assert.Equal(PublishingStatus.Unpublished, series.Publishing);
        Assert.Null(series.PendingOffer);
        Assert.Null(series.Contract);
        Assert.Empty(series.PitchCooldowns);
        Assert.Contains(state.Events, e => e.Type == EventType.OfferDeclined);
        Assert.Throws<InvalidCommandException>(() => state.Apply(new DeclineOfferCommand(series.Id)));
        Assert.Throws<InvalidCommandException>(() => state.Apply(new AcceptOfferCommand(series.Id)));
    }

    [Fact]
    public void An_unanswered_offer_expires_at_the_first_issue_close()
    {
        var state = Resolved(AcceptingSeed());
        var series = state.Series[0];
        var expires = series.PendingOffer!.ExpiresAt;
        state.Advance(state.Clock.HoursUntil(expires) - 1);
        Assert.Equal(PublishingStatus.Offered, series.Publishing);
        state.Advance(1);
        Assert.Equal(PublishingStatus.Unpublished, series.Publishing);
        Assert.Null(series.PendingOffer);
        Assert.Empty(series.PitchCooldowns);
        var expired = Assert.Single(state.Events, e => e.Type == EventType.OfferExpired);
        Assert.Equal(expires, expired.Time);
        // Enough doujin chapters accumulated meanwhile for a doujin volume including the one-shot.
        Assert.NotEmpty(series.Volumes);
        Assert.Equal(1, series.Volumes[0].FirstChapter);
    }

    [Fact]
    public void Pitch_flow_round_trips_through_a_save()
    {
        var state = Fresh(AcceptingSeed());
        state.Apply(new PitchSeriesCommand(state.Series[0].Id, Flowers));
        state.Advance(24 * 5);
        var loaded = GameState.FromJson(state.ToJson());
        Assert.Equal(PublishingStatus.Pitching, loaded.Series[0].Publishing);
        state.Advance(24 * 40);
        loaded.Advance(24 * 40);
        Assert.Equal(state.ToJson(), loaded.ToJson());
        Assert.Equal(PublishingStatus.Offered, loaded.Series[0].Publishing);
        GameState.FromJson(loaded.ToJson()); // validation passes with an offer pending
    }
}
