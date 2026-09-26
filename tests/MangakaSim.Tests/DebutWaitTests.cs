using Xunit;

namespace MangakaSim.Tests;

/// <summary>Tier 1 fix 5: the wait before a series debuts, chapters ready ahead and side-work suggestions (Q24, Q25).</summary>
public class DebutWaitTests
{
    private static GameState Sold(int seed)
    {
        var s = GameState.NewGame(seed); s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (int day = 0; day < 180 && s.Series[0].Volumes.Count == 0; day++) s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes.Single().Id, Amount: 10, Value: (int)PrintTier.CopyShop));
        s.Advance(24 * 8);
        return s;
    }

    /// <summary>A career that has just accepted its first serialization offer.</summary>
    private static GameState Signed()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            var s = Sold(seed); var series = s.Series[0];
            s.Apply(new ContinueOneShotCommand(series.Id));
            var best = CareerGuidance.PitchOutlooks(s, series).First(o => o.Open);
            s.Apply(new PitchSeriesCommand(series.Id, best.Magazine.Id));
            for (int day = 0; day < 200 && series.Publishing == PublishingStatus.Pitching; day++) s.Advance(24);
            if (series.Publishing != PublishingStatus.Offered) continue;
            s.Apply(new AcceptOfferCommand(series.Id));
            return s;
        }
        throw new InvalidOperationException("No seed produced a first offer.");
    }

    private static Series Debut(GameState s) => s.Series.Single(x => x.Publishing == PublishingStatus.Serialized);

    /// <summary>Advances a day at a time until the pre-debut stock is ready.</summary>
    private static GameState StockReady()
    {
        var s = Signed(); var series = Debut(s);
        for (int day = 0; day < 120 && s.ChaptersReadyAhead(series) is var a && a.Ready < a.Target; day++) s.Advance(24);
        Assert.Equal(0, series.ChaptersPublished);
        return s;
    }

    private static GuidanceStep Step(GameState s) => CareerGuidance.Evaluate(s, new GuidancePreferences());

    private static void WithinLimit(GuidanceStep step) =>
        Assert.All(step.Text.Split('\n'), t => Assert.True(t.Length <= CareerGuidance.TextLimit, $"{t.Length}: {t}"));

    [Fact]
    public void Ready_count_starts_at_zero_fills_the_buffer_and_follows_a_changed_buffer()
    {
        var s = Signed(); var series = Debut(s);
        Assert.Equal((0, Math.Max(1, series.BufferLimit)), s.ChaptersReadyAhead(series));

        s = StockReady(); series = Debut(s);
        var (ready, target) = s.ChaptersReadyAhead(series);
        Assert.True(ready >= target);
        // A chapter already under way may finish, but the planner starts no new ones.
        var chapters = series.Chapters.Count;
        s.Advance(24 * 14);
        Assert.Equal(chapters, series.Chapters.Count);

        ready = s.ChaptersReadyAhead(series).Ready;
        s.Apply(new StudioActionCommand(StudioAction.SetPipeline, series.Id, Value: series.PipelineLimit, Secondary: 4, Amount: series.MasterLimit));
        Assert.Equal((ready, 4), s.ChaptersReadyAhead(series));
        if (ready < 4) { s.Advance(24); Assert.True(series.Chapters.Count > chapters); }
    }

    [Fact]
    public void First_deadline_explains_the_debut_and_counts_ready_chapters()
    {
        var s = Signed(); var series = Debut(s);
        var step = Step(s);
        Assert.Equal("first-deadline", step.Id);
        Assert.Equal("production", step.Target);
        Assert.Contains($"0 of {s.ChaptersReadyAhead(series).Target} chapters ready", step.Text);
        Assert.Contains("Page fees only arrive after chapters publish", step.Text);
        WithinLimit(step);

        var p = new GuidancePreferences(); var ids = new List<string>();
        for (int day = 0; day < 120 && s.ChaptersReadyAhead(series) is var a && a.Ready < a.Target; day++)
        {
            CareerGuidance.Observe(s, p); s.Advance(24);
        }
        CareerGuidance.Observe(s, p);
        ids.AddRange(p.Thread.Select(m => m.Step));
        Assert.Contains("first-deadline-1", ids);
        Assert.Contains(ids, id => id.StartsWith("debut-wait-"));
    }

    [Fact]
    public void Each_suggestion_follows_the_agreed_order()
    {
        // 1. Early hire when the runway is safe.
        var s = StockReady(); var series = Debut(s);
        s.ControlledBusiness.Account.Balance += 5_000_000;
        var hire = Step(s);
        Assert.True(hire.Id is "debut-wait-hire" or "debut-wait-desk", hire.Id);
        Assert.Equal(hire.Id == "debut-wait-hire" ? "recruitment" : "furniture", hire.Target);
        Assert.Contains("Other ideas: a convention, a short doujin or a part-time job.", hire.Text);
        WithinLimit(hire);

        // 2. Convention when funds are tight and doujin copies are unsold.
        s = StockReady(); series = Debut(s);
        var reprint = s.Series.SelectMany(x => x.Volumes).First(v => v.IsDoujin);
        s.ControlledBusiness.Account.Balance += 200_000;
        s.Apply(new StudioActionCommand(StudioAction.Print, reprint.Id, Amount: 100, Value: (int)PrintTier.CopyShop));
        for (int day = 0; day < 14 && s.Stock(reprint.Id) == 0; day++) s.Advance(24);
        Assert.Equal(0, series.ChaptersPublished);
        // Enough for the booth and travel, far too little for a wage.
        s.ControlledBusiness.Account.Balance = 30_000;
        var doujin = s.Series.Where(x => x.Volumes.Any(v => v.IsDoujin && s.Stock(v.Id) > 0)).ToArray();
        Assert.NotEmpty(doujin);
        {
            var convention = Step(s);
            Assert.Equal("debut-wait-convention", convention.Id);
            Assert.Equal("conventions", convention.Target);
            Assert.Equal(doujin[0].Id, convention.Project);
            Assert.Contains("unsold doujin copies", convention.Text);
            WithinLimit(convention);
        }

        // 3. Short doujin when copies are gone or booked and no side doujin is in progress.
        s.Bookings.Add(new ConventionBooking { Id = 99999, BusinessId = s.ControlledBusinessId, PersonId = s.ProtagonistPersonId, Date = s.NextConvention(1), Scale = 1 });
        var side = Step(s);
        Assert.Equal("debut-wait-doujin", side.Id);
        Assert.Equal("create", side.Target);
        WithinLimit(side);

        // 4. Part-time job once a side doujin is under way.
        s.Apply(new CreateDoujinCommand("Side story", "comedy"));
        var job = Step(s);
        Assert.Equal("debut-wait-job", job.Id);
        Assert.Equal("finances", job.Target);
        Assert.Contains("Part-time work in Finances", job.Text);
        WithinLimit(job);

        // Finishing the side doujin does not bring the doujin suggestion back.
        var sideSeries = s.Series.Last();
        for (int day = 0; day < 30 && series.ChaptersPublished == 0 && sideSeries.Chapters.Any(c => c.Status != ChapterStatus.Complete); day++) s.Advance(24);
        Assert.All(sideSeries.Chapters, c => Assert.Equal(ChapterStatus.Complete, c.Status));
        // The booked convention may have passed by now, so the convention can return, but not the doujin.
        if (series.ChaptersPublished == 0) Assert.NotEqual("debut-wait-doujin", Step(s).Id);
    }

    [Fact]
    public void A_convention_is_only_suggested_when_the_booth_and_travel_are_affordable()
    {
        var s = StockReady();
        var reprint = s.Series.SelectMany(x => x.Volumes).First(v => v.IsDoujin);
        s.ControlledBusiness.Account.Balance += 200_000;
        s.Apply(new StudioActionCommand(StudioAction.Print, reprint.Id, Amount: 100, Value: (int)PrintTier.CopyShop));
        for (int day = 0; day < 14 && s.Stock(reprint.Id) == 0; day++) s.Advance(24);
        s.ControlledBusiness.Account.Balance = 0;
        var step = Step(s);
        Assert.NotEqual("debut-wait-convention", step.Id);
        Assert.True(step.Id is "debut-wait-doujin" or "debut-wait-job", step.Id);
    }

    [Fact]
    public void An_early_hire_needs_cash_to_last_until_the_debut_not_only_future_page_fees()
    {
        var s = StockReady(); var series = Debut(s);
        s.ControlledBusiness.Account.Balance = 0;
        var costs = s.HiringRunway(StudioRules.MinimumMonthlySalary).MonthlyCosts;
        // Two months of cash: future page fees make the runway look safe, but wages fall due before the first fee.
        s.ControlledBusiness.Account.Balance = costs * 2;
        var runway = s.HiringRunway(StudioRules.MinimumMonthlySalary);
        Assert.True(runway.Safe, $"{runway}");
        var step = Step(s);
        Assert.False(step.Id is "debut-wait-hire" or "debut-wait-desk", step.Id);

        var months = Math.Max(StudioRules.SafeRunwayMonths, (series.Contract!.FirstIssueClose - s.Clock.Now).TotalDays / 30 + 1);
        s.ControlledBusiness.Account.Balance = (long)Math.Ceiling(costs * months) + 1;
        Assert.True(Step(s).Id is "debut-wait-hire" or "debut-wait-desk", Step(s).Id);
    }

    [Fact]
    public void Wait_card_ends_when_the_first_chapter_publishes()
    {
        var s = StockReady(); var series = Debut(s);
        for (int day = 0; day < 200 && series.ChaptersPublished == 0; day++) s.Advance(24);
        Assert.True(series.ChaptersPublished > 0);
        var id = Step(s).Id;
        Assert.False(id.StartsWith("debut-wait-") || id.StartsWith("first-deadline"), id);
    }

    [Fact]
    public void Guidance_is_read_only()
    {
        var s = StockReady(); var json = s.ToJson();
        Step(s); s.ChaptersReadyAhead(Debut(s));
        Assert.Equal(json, s.ToJson());
    }
}
