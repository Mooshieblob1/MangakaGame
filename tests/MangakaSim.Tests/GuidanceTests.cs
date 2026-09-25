using Xunit;

namespace MangakaSim.Tests;

/// <summary>Tier 1 fix 1: the continuous career path after the first sale and Helper-Chan's message thread.</summary>
public class GuidanceTests
{
    private static GameState Sold(int seed = 0)
    {
        var s = GameState.NewGame(seed); s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (int day = 0; day < 180 && s.Series[0].Volumes.Count == 0; day++) s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes.Single().Id, Amount: 10, Value: (int)PrintTier.CopyShop));
        s.Advance(24 * 8);
        Assert.True(s.Series[0].Volumes.Single().CopiesSold > 0);
        return s;
    }

    private static string Step(GameState s, GuidancePreferences p) { CareerGuidance.Observe(s, p); return CareerGuidance.Evaluate(s, p).Id; }

    /// <summary>Advances a day at a time, recording each distinct step, until the predicate holds.</summary>
    private static void Until(GameState s, GuidancePreferences p, HashSet<string> seen, Func<string, bool> done, int days = 400)
    {
        for (int day = 0; day < days; day++)
        {
            var id = Step(s, p); seen.Add(id);
            if (done(id)) return;
            s.Advance(24);
        }
        Assert.Fail($"Guidance stuck on {Step(s, p)} after {days} days.");
    }

    [Theory][InlineData(0)][InlineData(1)][InlineData(42)]
    public void Career_path_leads_from_first_sale_to_a_settled_studio(int seed)
    {
        var s = Sold(seed); var p = new GuidancePreferences(); var seen = new HashSet<string>();
        var step = CareerGuidance.Evaluate(s, p);
        Assert.Equal("continue-series", step.Id); Assert.Equal("series", step.Target);
        s.Apply(new ContinueOneShotCommand(step.Project));
        var series = s.Series[0];

        // Pitch, following guidance, until a magazine makes an offer.
        for (int pitch = 0; pitch < 12 && series.Publishing != PublishingStatus.Offered; pitch++)
        {
            Until(s, p, seen, id => id is "pitch" or "pitch-rejected" or "pitch-wait" or "offer");
            if (Step(s, p) == "offer") break;
            if (Step(s, p) == "pitch-wait") { Until(s, p, seen, id => id != "pitch-wait", 200); continue; }
            var best = CareerGuidance.PitchOutlooks(s, series).First(o => o.Open);
            if (Step(s, p) == "pitch") Assert.Contains(best.Magazine.Name, CareerGuidance.Evaluate(s, p).Text);
            s.Apply(new PitchSeriesCommand(series.Id, best.Magazine.Id));
            Until(s, p, seen, id => id is "pitch-rejected" or "offer" or "pitch-wait");
        }
        Assert.Contains("pitch", seen); Assert.Contains("pitch-waiting", seen);
        Assert.True(seen.Contains("pitch-sample") || seen.Contains("pitch-name-review"));
        Assert.Equal("offer", Step(s, p));

        s.Apply(new AcceptOfferCommand(series.Id));
        Assert.Equal("first-deadline", Step(s, p));
        Until(s, p, seen, id => id is "first-hire" or "serial-rhythm");

        // Hire the cheapest candidate once guidance allows it, furnishing a desk if needed.
        for (int round = 0; round < 12 && !s.ControlledStaff.Any(x => x.Id != s.ProtagonistPersonId); round++)
        {
            if (!s.Candidates.Any(c => !c.Recruited && c.ExpiresAt > s.Clock.Now)) s.Apply(new RecruitStaffCommand());
            if (s.AvailableBusinessCash < 1_000_000) s.ControlledBusiness.Account.Balance += 1_000_000;
            Until(s, p, seen, id => id == "first-hire");
            var c = s.Candidates.Where(c => !c.Recruited && c.ExpiresAt > s.Clock.Now).OrderBy(c => c.ExpectedSalary).First();
            var location = s.Protagonist.Employment!.LocationId;
            var arrangement = s.ArrangeOffice(location, true, true);
            if (arrangement.Purchases.Count > 0) s.Apply(new ApplyOfficeLayoutCommand(location, s.OfficeRevision, arrangement.Placements, arrangement.Purchases, []));
            s.Apply(new HireStaffCommand(c.Id, location, Math.Max(StudioRules.MinimumMonthlySalary, c.ExpectedSalary)));
        }
        Assert.Equal("career-settled", Step(s, p));
        Assert.All(p.Thread.SelectMany(m => m.Texts), t => Assert.True(t.Length <= CareerGuidance.TextLimit, t));
    }

    [Fact]
    public void Old_routes_join_the_career_path()
    {
        var s = Sold();
        foreach (var route in new[] { "opening", "doujin", "grow", "career" })
        {
            var p = new GuidancePreferences { Route = route };
            Assert.Equal("continue-series", CareerGuidance.Evaluate(s, p).Id);
            CareerGuidance.Observe(s, p); Assert.Equal("career", p.Route);
        }
        var fresh = new GuidancePreferences { Route = "doujin" };
        Assert.Equal("create", CareerGuidance.Evaluate(GameState.NewGame(), fresh).Id);
    }

    [Fact]
    public void Thread_adds_one_message_per_step_and_tracks_unread()
    {
        var s = GameState.NewGame(); var p = new GuidancePreferences();
        CareerGuidance.Observe(s, p); CareerGuidance.Observe(s, p);
        Assert.Single(p.Thread); Assert.Equal("create", p.Thread[0].Step); Assert.Equal(1, CareerGuidance.Unread(p));
        CareerGuidance.Say(p, s.Clock.Now, "Here is the create button!\nPick a genre.");
        Assert.Equal(["Here is the create button!", "Pick a genre."], p.Thread[^1].Texts);
        CareerGuidance.Observe(s, p); Assert.Equal(2, p.Thread.Count);
        CareerGuidance.MarkRead(p); Assert.Equal(0, CareerGuidance.Unread(p));
        s.Apply(new CreateDoujinCommand("Small", "drama")); CareerGuidance.Observe(s, p);
        Assert.Equal("produce", p.Thread[^1].Step); Assert.Equal(1, CareerGuidance.Unread(p));
        for (int i = 0; i < CareerGuidance.ThreadLimit + 20; i++) CareerGuidance.Say(p, s.Clock.Now, "Hello");
        Assert.Equal(CareerGuidance.ThreadLimit, p.Thread.Count);
    }

    [Fact]
    public void Thread_saves_with_the_career_and_never_changes_game_state()
    {
        var root = Path.Combine(Path.GetTempPath(), "mangaka-guidance-" + Guid.NewGuid().ToString("N"));
        try
        {
            var s = Sold(); var p = new GuidancePreferences(); var json = s.ToJson();
            CareerGuidance.Observe(s, p); CareerGuidance.Say(p, s.Clock.Now, "Nice work!"); CareerGuidance.Evaluate(s, p);
            CareerGuidance.PitchOutlooks(s, s.Series[0]);
            Assert.Equal(json, s.ToJson());
            var store = new CareerStore(root); var info = store.Save(Guid.NewGuid().ToString("N"), "Guided", s, new CareerPresentation { Guidance = p });
            var loaded = store.Load(info).View.Guidance;
            Assert.Equal(p.Thread.Select(m => (m.Step, m.Time, string.Join("|", m.Texts), m.Read)), loaded.Thread.Select(m => (m.Step, m.Time, string.Join("|", m.Texts), m.Read)));
            Assert.Equal(json, store.Load(info).State.ToJson());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void Rejection_explains_the_factor_and_the_cooldown_date()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            var s = Sold(seed); var p = new GuidancePreferences(); var series = s.Series[0];
            s.Apply(new ContinueOneShotCommand(series.Id));
            var best = CareerGuidance.PitchOutlooks(s, series).First();
            s.Apply(new PitchSeriesCommand(series.Id, best.Magazine.Id));
            for (int day = 0; day < 200 && series.Publishing == PublishingStatus.Pitching; day++) s.Advance(24);
            if (series.Publishing != PublishingStatus.Unpublished) continue;
            var step = CareerGuidance.Evaluate(s, p);
            Assert.Equal("pitch-rejected", step.Id);
            Assert.Contains($"{series.PitchCooldowns[best.Magazine.Id]:d MMM yyyy}", step.Text);
            Assert.All(step.Texts, t => Assert.True(t.Length <= CareerGuidance.TextLimit, t));
            Assert.Null(CareerGuidance.Outlook(s, series, best.Magazine.Id).CooldownUntil is null ? "open" : null);
            return;
        }
        Assert.Fail("No seed produced a rejected first pitch.");
    }
}
