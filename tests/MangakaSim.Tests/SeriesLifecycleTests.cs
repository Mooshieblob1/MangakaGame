using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

public class SeriesLifecycleTests
{
    [Theory]
    [InlineData("decline")] [InlineData("expire")] [InlineData("end")]
    public void Offer_exit_paths_preserve_valid_history(string action)
    {
        var state = SimulationFixture.Offered();
        var series = state.Series[0];
        var sample = series.Chapters.Last();
        var expiry = series.PendingOffer!.ExpiresAt;
        if (action == "decline") state.Apply(new DeclineOfferCommand(series.Id));
        else if (action == "end") state.Apply(new EndSeriesCommand(series.Id));
        else state.Advance(state.Clock.HoursUntil(expiry));
        Assert.Null(series.PendingOffer);
        Assert.Equal(PublishingStatus.Unpublished, series.Publishing);
        if (action == "end") Assert.DoesNotContain(sample, series.Chapters);
        else { Assert.True(sample.DoujinEligible); Assert.Empty(series.PitchCooldowns); }
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new AcceptOfferCommand(series.Id)));
        Assert.Equal(before, state.ToJson());
    }

    [Fact]
    public void Withdrawal_preserves_publications_and_restores_doujin_work()
    {
        var state = SimulationFixture.EightPublished();
        var series = state.Series[0];
        var contract = series.Contract!;
        var fans = series.Fanbase;
        var open = series.Chapters.Last();
        var previousHours = open.Stages.Sum(w => w.HoursDone);
        state.Apply(new WithdrawSeriesCommand(series.Id));
        Assert.Null(series.Contract);
        Assert.Contains(contract, series.PastContracts);
        Assert.Equal(fans * .9, series.Fanbase);
        Assert.Equal(previousHours, open.Stages.Sum(w => w.HoursDone));
        Assert.Equal(Cadence.Monthly, series.Cadence);
        Assert.Equal(CadenceRules.NextDue(state.Clock.Now, Cadence.Monthly), open.DueDate);
        Assert.Equal(EditorStatus.NotRequired, open.Editor);
        Assert.All(series.Chapters.Where(c => c.PublishedAt is not null), c => Assert.Equal(contract.Id, c.PublishedContractId));
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
        PublishingTests.Until(state, () => open.Status == ChapterStatus.Complete);
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new PitchSeriesCommand(series.Id, contract.MagazineId)));
        Assert.Equal(before, state.ToJson());
        state.Apply(new PitchSeriesCommand(series.Id, "kaidan-afternoon"));
        Assert.Equal(PublishingStatus.Pitching, series.Publishing);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }

    [Fact]
    public void Serialized_cadence_change_is_atomic_and_fees_use_frozen_pages()
    {
        var state = SimulationFixture.Serialized();
        var series = state.Series[0];
        var chapter = series.Chapters.Last();
        var before = state.ToJson();
        Assert.Throws<InvalidCommandException>(() => state.Apply(new SetCadenceCommand(series.Id, Cadence.Weekly)));
        Assert.Equal(before, state.ToJson());
        state.Apply(new SetPagesPerChapterCommand(series.Id, 3));
        PublishingTests.Until(state, () => chapter.PublishedAt is not null);
        Assert.Equal(19, chapter.Pages);
        Assert.Equal(19 * series.Contract!.FeePerPage, state.Ledger.First(e => e.Reason == "chapter fee").Amount);
        Assert.All(series.Chapters.Where(c => c.Number > chapter.Number), c => Assert.Equal(3, c.Pages));
    }

    [Fact]
    public void Grace_excludes_eighth_publication_and_expired_strikes_do_not_cancel()
    {
        var state = SimulationFixture.EightPublished();
        var series = state.Series[0];
        Assert.Empty(series.Strikes);
        Assert.Equal(0, series.WeeksBelowLine);
        state.Apply(new PauseSeriesCommand(series.Id));
        // Completed stock is allowed to publish during a pause before misses begin.
        PublishingTests.Until(state, () => series.Strikes.Count > 0);
        Assert.Contains(state.Events, e => e.Type == EventType.IssueMissed && e.SeriesId == series.Id);
        Assert.Null(series.LastRank);
        var magazine = state.PublisherCatalog.Get(series.Contract!.MagazineId);
        var life = CancellationRules.Clocks(state.Protection(series)).StrikeLifetime;
        series.Strikes.Insert(0, state.Clock.Now.AddDays(-(life + 1) * IssueSchedule.Days(magazine.Cadence)));
        SimulationFixture.NextIssue(state);
        Assert.Equal(2, series.Strikes.Count);
    }

    [Fact]
    public void Three_misses_can_cancel_and_final_books_keep_selling()
    {
        var state = SimulationFixture.EightPublished();
        var series = state.Series[0];
        state.Apply(new PauseSeriesCommand(series.Id));
        PublishingTests.Until(state, () => series.Status == SeriesStatus.Ended);
        Assert.Contains(state.Events, e => e.Type == EventType.SeriesCancelled);
        Assert.Null(series.Contract);
        Assert.Empty(state.People[0].Queue);
        var before = series.Volumes.Sum(v => v.CopiesSold);
        state.Advance(24 * 56);
        Assert.True(series.Volumes.Sum(v => v.CopiesSold) > before);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }

    [Theory]
    [InlineData(2, false)] [InlineData(3, true)] [InlineData(12, true)]
    public void Ending_collects_only_eligible_final_commercial_books(int published, bool final)
    {
        var state = SimulationFixture.Serialized();
        var series = state.Series[0];
        PublishingTests.Until(state, () => series.ChaptersPublished == published);
        // At 12 the full books already contain every chapter, so no extra final book.
        var before = series.Volumes.Count;
        var track = state.StudioTrackRecord;
        state.Apply(new EndSeriesCommand(series.Id));
        Assert.Equal(before + (final && published == 3 ? 1 : 0), series.Volumes.Count);
        if (published >= 12) Assert.True(state.StudioTrackRecord > track);
        Assert.Equal(SeriesStatus.Ended, series.Status);
        Assert.Equal(state.ToJson(), GameState.FromJson(state.ToJson()).ToJson());
    }

    [Fact]
    public void Iconic_series_keep_fans_and_have_no_cancellation_penalty()
    {
        var state = SimulationFixture.EightPublished();
        var series = state.Series[0];
        series.Fanbase = 1100000;
        series.CulturalImpact = 90;
        // Transition is exercised by an actual issue, not assigning IsIconic.
        SimulationFixture.NextIssue(state);
        Assert.True(series.IsIconic);
        Assert.Single(state.Events, e => e.Type == EventType.SeriesBecameIconic);
        state.Apply(new PauseSeriesCommand(series.Id));
        state.Advance(24 * 28 * 10);
        Assert.Equal(SeriesStatus.Paused, series.Status);
        Assert.Empty(series.Strikes);
        Assert.Null(series.WarningIssuedAt);
        var fans = series.Fanbase;
        state.Apply(new WithdrawSeriesCommand(series.Id));
        Assert.Equal(fans, series.Fanbase);
        Assert.True(series.IsIconic);
    }

    [Fact]
    public void Regular_hours_count_matches_hourly_reference_over_long_spans()
    {
        var state = GameState.NewGame();
        var person = state.People[0];
        foreach (var offset in new[] { 0, 1, 14, 23, 71 })
        {
            var from = GameClock.Start.AddHours(offset);
            var end = from.AddDays(800).AddHours(13);
            var reference = Enumerable.Range(0, (int)(end - from).TotalHours).Count(h => person.Schedule.IsRegularHour(from.AddHours(h)));
            Assert.Equal(reference, state.RegularHoursBefore(person, from, end));
        }
    }
}
