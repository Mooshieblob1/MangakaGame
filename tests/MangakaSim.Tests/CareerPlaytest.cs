using System.Text;
using MangakaSim.Catalog;
using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

/// <summary>
/// Sub-project 10 guided-player playtest, extended to ten in-game years (1996 to 2006) for the Tier 2 balance pass.
/// A scripted player follows Helper-Chan's guidance and ordinary player choices; after the first sale it acts only on the
/// current guidance step, and after the first hire on the goals board (moving out, staff, incorporation, a second series,
/// licences). It records milestones, money, estimated real time, mid-career activity and every rejected action.
/// Excluded from the normal suite; run with
/// dotnet test --filter Category=Playtest
/// </summary>
[Trait("Category", "Playtest")]
public class CareerPlaytest
{
    // Mirrors the Godot driver: 36 s per game hour at 1x, player at 8x during work hours,
    // 2.5 s per hour at 1x played at 32x overnight.
    private const double WorkSecondsPerHour = 36.0 / 8, NightSecondsPerHour = 2.5 / 32;
    // Mid-career pacing (Q26 to Q28): daytime at 32x instead of 8x, for the per-year comparison.
    private const double QuietWorkSecondsPerHour = 36.0 / 32;
    // Tier 2 balance covers 1996 to 2006 (early access considerations Q3).
    private const int Years = 10;
    // The practice career fixture looks for its warning in the first three years only.
    private const int PracticeYears = 3;

    [Theory]
    [InlineData(0, CareerDifficulty.Standard)]
    [InlineData(1, CareerDifficulty.Standard)]
    [InlineData(42, CareerDifficulty.Standard)]
    [InlineData(7, CareerDifficulty.Standard)]
    [InlineData(7, CareerDifficulty.Relaxed)]
    [InlineData(7, CareerDifficulty.Challenging)]
    public void Ten_year_guided_career(int seed, CareerDifficulty difficulty)
    {
        var run = new GuidedPlayer(seed, difficulty);
        run.Play(Years);
        var report = run.Report();
        var dir = Path.Combine(RepoRoot(), "TestResults", "career-playtest");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"seed{seed}-{difficulty}.md"), report);
        // The playtest records findings rather than asserting balance; only integrity is required.
        var json = run.State.ToJson();
        Assert.Equal(json, GameState.FromJson(json).ToJson());
        Assert.Equal(json, run.State.ReplayTimeline().ToJson());
    }

    // T1.10 practice career (Part 3): seed 0 on Standard, saved two in-game weeks before the first cancellation warning.
    [Fact]
    public void Practice_career_fixture()
    {
        var scout = new GuidedPlayer(0, CareerDifficulty.Standard);
        var warning = DateTime.MinValue;
        for (var year = 1; year <= PracticeYears && warning == DateTime.MinValue; year++)
        {
            scout.Play(year);
            warning = scout.State.Events.FirstOrDefault(e => e.Type == EventType.CancellationWarning &&
                scout.State.Series.Any(s => s.Id == e.SeriesId && s.BusinessId == scout.State.ControlledBusinessId))?.Time ?? DateTime.MinValue;
        }
        Assert.True(warning != DateTime.MinValue, "Seed 0 on Standard had no cancellation warning in three years");
        Assert.True(warning.Year < 1999, $"Warning came late: {warning:d MMM yyyy}");

        var player = new GuidedPlayer(0, CareerDifficulty.Standard);
        player.PlayUntil(warning.AddDays(-14));
        var state = player.State;
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.CancellationWarning &&
            state.Series.Any(s => s.Id == e.SeriesId && s.BusinessId == state.ControlledBusinessId));
        Assert.Contains(state.Series, s => s.BusinessId == state.ControlledBusinessId && s.Publishing == PublishingStatus.Serialized);

        var view = new CareerPresentation { Guidance = player.Preferences, Page = "Office" };
        CareerGuidance.Observe(state, view.Guidance);
        CareerGuidance.MarkRead(view.Guidance);
        var root = Path.Combine(Path.GetTempPath(), "mangaka-practice-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new CareerStore(root);
            var info = store.Save(CareerStore.PracticeCareer, "Practice: a struggling series", state, view);
            var path = Path.Combine(RepoRoot(), "tests", "MangakaSim.Tests", "Fixtures", "Practice - a struggling series.mangaka");
            File.WriteAllBytes(path, store.Export(info));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MangakaGame.sln"))) dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }

    private sealed class GuidedPlayer
    {
        public GameState State { get; }
        public GuidancePreferences Preferences => _guide;
        private readonly int _seed;
        private readonly CareerDifficulty _difficulty;
        private readonly GuidancePreferences _guide = new();
        private readonly List<(DateTime At, double Minutes, string Name)> _milestones = new();
        private readonly List<(DateTime At, string Action, string Message)> _rejections = new();
        private readonly List<string> _decisions = new();
        private readonly List<string> _monthly = new();
        private readonly List<(DateTime From, string Id, string Title)> _guidance = new();
        private readonly HashSet<string> _seen = new();
        private readonly List<(DateTime At, string Step, string Problem)> _stale = new();
        private readonly Dictionary<int, int> _printRuns = new();
        private readonly DateTime _start;
        private DateTime _nextMonth;
        private double _seconds, _workSeconds;
        private int _eventCursor, _contributions, _recruitAttempts;
        private long _contributed;
        private bool _hired;
        // Per career year: daytime hours, night hours, daily recaps (a click each at 8x), 32x stop events and new Helper-Chan texts.
        private readonly Dictionary<int, (int Work, int Night, int Recaps, int Stops, int Texts)> _pace = new();
        // The thread keeps its newest 200 messages, so new texts are counted from the last one seen, not from the count.
        private GuidanceMessage? _lastText;
        // Ten-year view: successful player commands, moments that call for the player, and month-end snapshots.
        private readonly List<(DateTime At, string Action)> _actions = new();
        private readonly List<DateTime> _attention = new();
        private readonly List<(DateTime At, long Personal, long Business, int Serialized, int Staff, double Fans, int Chapter)> _snapshots = new();
        private readonly HashSet<int> _licenceTried = new();

        public GuidedPlayer(int seed, CareerDifficulty difficulty)
        {
            _seed = seed; _difficulty = difficulty;
            State = GameState.NewGame(seed);
            if (difficulty != CareerDifficulty.Standard) Try("Difficulty", new DifficultyCommand(difficulty));
            _start = State.Clock.Now;
            _nextMonth = _start;
        }

        private double Minutes => _seconds / 60;
        private void Mark(string name) { if (_seen.Add(name)) _milestones.Add((State.Clock.Now, Minutes, name)); }
        private void Note(string text) => _decisions.Add($"{State.Clock.Now:yyyy-MM-dd} {text}");

        private bool Try(string label, ICommand command)
        {
            try { State.Apply(command); _actions.Add((State.Clock.Now, label)); return true; }
            catch (InvalidCommandException e) { _rejections.Add((State.Clock.Now, label, e.Message)); return false; }
        }

        public void Play(int years) => PlayUntil(_start.AddYears(years));

        public void PlayUntil(DateTime end)
        {
            while (State.Clock.Now < end)
            {
                if (State.Clock.Hour == 9) Decide();
                var working = State.ControlledStaff.Any(p => p.Schedule.IsRegularHour(State.Clock.Now));
                var cost = working ? WorkSecondsPerHour : NightSecondsPerHour;
                _seconds += cost; if (working) _workSeconds += cost;
                var year = (int)((State.Clock.Now - _start).TotalDays / 365.25);
                var pace = _pace.GetValueOrDefault(year);
                if (working) pace.Work++; else pace.Night++;
                _pace[year] = pace;
                State.Advance(1);
                ReadEvents();
                if (State.Clock.Now >= _nextMonth)
                {
                    var serializedNow = State.Series.Count(s => s.BusinessId == State.ControlledBusinessId && s.Publishing == PublishingStatus.Serialized);
                    var fans = State.Series.Where(s => s.BusinessId == State.ControlledBusinessId).Select(s => s.Fanbase).DefaultIfEmpty(0).Max();
                    _snapshots.Add((State.Clock.Now, State.PersonalMoney, State.Money, serializedNow, State.ControlledStaff.Count(), fans, State.Goals?.Chapter ?? 0));
                    _monthly.Add($"| {State.Clock.Now:yyyy-MM} | {Minutes / 60:F1} | {State.PersonalMoney:N0} | {State.Money:N0} | " +
                        $"{serializedNow} | {State.ControlledStaff.Count()} | {fans:N0} | {Guide().Id} |");
                    _nextMonth = _nextMonth.AddMonths(1);
                }
            }
        }

        private GuidanceStep Guide()
        {
            CareerGuidance.Observe(State, _guide);
            // Each new Helper-Chan text stops a 32x day, like the stop events. Goal and part texts never stop 32x (spec 2026-10-01).
            var thread = _guide.Thread;
            var fresh = thread.Skip(_lastText is null ? 0 : thread.LastIndexOf(_lastText) + 1)
                .Count(m => !m.Step.StartsWith(CareerGuidance.GoalStep, StringComparison.Ordinal));
            _lastText = thread.LastOrDefault();
            if (fresh > 0)
            {
                var year = (int)((State.Clock.Now - _start).TotalDays / 365.25);
                var pace = _pace.GetValueOrDefault(year); pace.Texts += fresh; _pace[year] = pace;
                _attention.Add(State.Clock.Now);
            }
            return CareerGuidance.Evaluate(State, _guide);
        }

        private void ReadEvents()
        {
            for (; _eventCursor < State.Events.Count; _eventCursor++)
            {
                var e = State.Events[_eventCursor];
                var year = (int)((e.Time - _start).TotalDays / 365.25);
                var pace = _pace.GetValueOrDefault(year);
                if (e.Type == EventType.DailyRecap) pace.Recaps++;
                if (CareerGuidance.FastSpeedStops.Contains(e.Type)) { pace.Stops++; _attention.Add(e.Time); }
                _pace[year] = pace;
                switch (e.Type)
                {
                    case EventType.ChapterCompleted: Mark("First chapter completed"); break;
                    case EventType.PitchSubmitted: Mark("First pitch submitted"); break;
                    case EventType.EditorRedoRequested: Mark("First editor redo"); break;
                    case EventType.PitchRejected: Mark("First pitch rejected"); Note("Pitch rejected: " + e.Message); break;
                    case EventType.SerializationOffered: Mark("First serialization offer"); Note(e.Message); break;
                    case EventType.OfferExpired: Mark("Offer expired"); break;
                    case EventType.ChapterPublished: Mark("First magazine chapter published"); break;
                    case EventType.DeadlineMissed: Mark("First deadline missed");
                        if (State.Protagonist.OutsideJob != OutsideJob.None && Try("Stop part-time job", new SetOutsideJobCommand(OutsideJob.None))) Note("Stopped the part-time job after a missed deadline.");
                        break;
                    case EventType.IssueMissed: Mark("First magazine issue missed"); Note(e.Message); break;
                    case EventType.RankingPublished when Owned(e.SeriesId): Mark("First own ranking"); break;
                    case EventType.CancellationWarning: Mark("First cancellation warning"); Note(e.Message); break;
                    case EventType.CancellationSurvived: Mark("Survived a cancellation review"); break;
                    case EventType.SeriesCancelled: Mark("First cancellation"); Note(e.Message); break;
                    case EventType.VolumeReleased: Mark("First volume released"); break;
                    case EventType.RecruitmentCompleted: Mark("First recruitment results"); break;
                    case EventType.StaffHired: Mark("First hire"); break;
                    case EventType.StaffDeparted: Mark("First staff departure"); Note(e.Message); break;
                    case EventType.WageArrears: Mark("First wage arrears"); Note(e.Message); break;
                    case EventType.AwardResult: Mark("First award result"); break;
                    case EventType.LicenseOffered: Mark("First licence offer"); break;
                    case EventType.LicenseReleased: Mark("First licence released"); Note(e.Message); break;
                    case EventType.SeriesBecameIconic: Mark("First iconic series"); Note(e.Message); break;
                    case EventType.CareerMilestone: Note("Milestone: " + e.Message); break;
                    case EventType.AwardNomination when Owned(e.SeriesId): Note("Nomination: " + e.Message); break;
                    case EventType.GoalCompleted: Mark(e.Message.Split(". Reward")[0]); break;
                    case EventType.GoalChapterCompleted: Mark(e.Message.Split("! Reward")[0]); break;
                    case EventType.PartOpened: Mark(e.Message); break;
                }
            }
        }

        private bool Owned(int? seriesId) => State.Series.Any(s => s.Id == seriesId && s.BusinessId == State.ControlledBusinessId);

        private void Decide()
        {
            var step = Guide();
            if (_guidance.Count == 0 || _guidance[^1].Id != step.Id) _guidance.Add((State.Clock.Now, step.Id, step.Title));

            var owned = State.Series.Where(s => s.BusinessId == State.ControlledBusinessId).ToArray();
            if (owned.Length == 0) { if (Try("Create doujin", new CreateDoujinCommand("First pages", "adventure"))) Mark("First doujin created"); return; }

            FundBusiness();
            SellBooks(owned);
            // After the first sale the player does what Helper-Chan's latest message asks, nothing more.
            if (_seen.Contains("First sale")) FollowGuidance();
            if (_hired) { GrowStudio(); MidCareer(); }
        }

        // After the first hire Helper-Chan points at the goals board (Q51). A player reads the whole board, so the bot works on
        // every unfinished goal of the current chapter that a player can act on, not only the first one.
        private void MidCareer()
        {
            AnswerLicences();
            if (State.Goals is not { } goals || goals.Chapter >= GoalCatalog.Chapters.Length) return;
            foreach (var goal in GoalCatalog.In(goals.Chapter).Where(g => !State.GoalDone(g.Id)))
                switch (goal.Id)
                {
                    case "moved-out" when State.Locations.Single(l => l.Id == State.Protagonist.Employment!.LocationId).IsFamilyHome:
                        // "Move once income covers the rent with room to spare": a year of the cheapest rent in the account.
                        var home = TokyoProperties.All.Where(p => p.Seats >= 4).OrderBy(p => p.Rent).First();
                        if (State.AvailableBusinessCash >= home.Rent * 12) MoveTo(home.Seats);
                        break;
                    case "incorporated":
                        // The player sees the capital rule on the button and waits for it (¥3,000,000 before May 2006).
                        var capital = (State.Clock.Now < new DateTime(2006, 5, 1) ? 3_000_000 : 0) + 200_000;
                        if (State.AvailableBusinessCash >= capital && Try("Incorporate", new StudioActionCommand(StudioAction.Incorporate)))
                        { Mark("Incorporated"); Note("Incorporated the studio."); }
                        break;
                    case "two-series":
                        SecondSeries();
                        break;
                    case "anime":
                        PitchLicence([LicenseKind.Anime]);
                        break;
                    case "merchandise":
                        PitchLicence([LicenseKind.Figures, LicenseKind.Stationery, LicenseKind.Clothing]);
                        break;
                }
            PitchSpare();
        }

        private Series[] OwnedActive() => State.Series.Where(s => s.BusinessId == State.ControlledBusinessId && s.Status == SeriesStatus.Active).ToArray();

        private void SecondSeries()
        {
            // "Start a second ongoing series once your team has spare desks."
            var active = OwnedActive();
            if (active.Count(s => s.Publishing == PublishingStatus.Serialized) != 1 || State.ControlledStaff.Count() < 3) return;
            if (active.Any(s => !s.StandaloneDoujin && s.Publishing is PublishingStatus.Unpublished or PublishingStatus.Pitching or PublishingStatus.Offered)) return;
            var genre = BestGenre();
            if (Try("Create second series", new CreateSeriesCommand($"Second series {State.Series.Count + 1}", genre, Cadence.Monthly, 16)))
            { Mark("Second series created"); Note($"Created a second {genre} series to pitch alongside the first."); }
        }

        // With one series already running, guidance shows goals rather than pitch steps, so the player pitches spare titles directly.
        private void PitchSpare()
        {
            if (!OwnedActive().Any(s => s.Publishing == PublishingStatus.Serialized)) return;
            foreach (var spare in OwnedActive().Where(s => !s.StandaloneDoujin && s.Publishing == PublishingStatus.Unpublished && !s.Chapters.Any(c => c.IsOneShot && !c.PitchResolved)))
            {
                var best = CareerGuidance.PitchOutlooks(State, spare).FirstOrDefault(o => o.Open);
                if (best is not null && Try("Pitch second series", new PitchSeriesCommand(spare.Id, best.Magazine.Id)))
                    Note($"Pitched {spare.Title} to {best.Magazine.Name} (tier {best.Magazine.Tier}, guidance chance {best.Chance:P0}).");
            }
        }

        private void AnswerLicences()
        {
            foreach (var p in State.Progression.Projects.Where(p => p.BusinessId == State.ControlledBusinessId).ToArray())
            {
                var kind = p.Kind == LicenseKind.Anime ? "anime" : "merchandise";
                if (p.Phase == LicensePhase.Offer && p.DueAt > State.Clock.Now && _licenceTried.Add(p.Id))
                {
                    if (Try("Accept licence", new LicenseCommand(LicenseAction.Accept, p.Id)))
                    { Mark($"First {kind} licence signed"); Note($"Signed a {p.Kind} licence with {p.Partner} for ¥{p.Payment:N0}."); }
                }
                else if (p.Decision.Length > 0 && p.Phase is LicensePhase.PreProduction or LicensePhase.Production or LicensePhase.Released)
                    Try("Licence decision", new LicenseCommand(p.Decision == "Source material" ? LicenseAction.Wait : LicenseAction.RespondDelay, p.Id));
            }
        }

        private void PitchLicence(LicenseKind[] kinds)
        {
            var series = OwnedActive().Where(s => s.Fanbase >= 1000).OrderByDescending(s => s.Fanbase).FirstOrDefault();
            if (series is null || State.Protagonist.BusyUntil > State.Clock.Now) return;
            foreach (var kind in kinds)
            {
                if (State.Progression.PitchCooldowns.GetValueOrDefault($"{series.Id}:{kind}") > State.Clock.Now ||
                    State.Progression.Projects.Any(p => p.SeriesId == series.Id && p.Kind == kind && p.Phase is LicensePhase.Offer or LicensePhase.PreProduction or LicensePhase.Production or LicensePhase.Released)) continue;
                if (Try("Pitch licence", new LicenseCommand(LicenseAction.Pitch, series.Id, kind))) { Mark("First licence pitch"); Note($"Pitched {series.Title} for a {kind} licence."); }
                return;
            }
        }

        // Moves the studio to the cheapest property with at least this many seats and furnishes it, when six months' rent is in the account.
        private bool MoveTo(int seats)
        {
            var offer = TokyoProperties.All.Where(p => p.Seats >= seats).OrderBy(p => p.Rent).First();
            var current = State.Locations.Single(l => l.Id == State.Protagonist.Employment!.LocationId);
            if (current.PropertyOfferId == offer.Id || State.AvailableBusinessCash < offer.Rent * 6) return false;
            if (!Try("Move studio", new StudioActionCommand(StudioAction.Move, offer.Id))) return false;
            Mark(current.IsFamilyHome ? "First studio move" : "Moved to a bigger studio");
            Note($"Moved to {offer.District} ({offer.Seats} seats, rent {offer.Rent:N0} yen a month).");
            var location = State.Protagonist.Employment!.LocationId;
            var arrangement = State.ArrangeOffice(location, true, true);
            Try("Furnish new studio", new ApplyOfficeLayoutCommand(location, State.OfficeRevision, arrangement.Placements, arrangement.Purchases, []));
            return true;
        }

        private void FundBusiness()
        {
            // A cautious player tops up the doujin budget from savings when it runs low.
            if (State.Money >= 20_000 || State.PersonalMoney < 60_000) return;
            var amount = Math.Min(50_000, State.PersonalMoney - 30_000);
            if (Try("Contribute funds", new ContributeFundsCommand(amount)))
            { _contributions++; _contributed += amount; Mark("First savings contribution"); }
        }

        private void SellBooks(Series[] owned)
        {
            foreach (var v in owned.SelectMany(s => s.Volumes).Where(v => v.IsDoujin && v.BusinessId == State.ControlledBusinessId))
            {
                if (v.CopiesSold > 0 || State.DoujinDownloadsSold(v.Id) > 0) Mark("First sale");
                if (!State.DoujinOnlineListed(v.Id) && Try("List online", new PublishDoujinOnlineCommand(v.Id))) Mark("First online listing");
                var pending = State.PrintRuns.Any(r => r.VolumeId == v.Id && !r.Delivered);
                var runs = _printRuns.GetValueOrDefault(v.Id);
                if (pending || State.Stock(v.Id) > 0 || runs >= 3) continue;
                var copies = runs == 0 ? 30 : 50;
                var cost = GameState.PrintingCost(PrintTier.CopyShop, v.PrintedPages, copies);
                if (State.AvailableBusinessCash < cost) continue;
                // A player sees the storage line and does not order copies with nowhere to keep them.
                if (!State.Locations.Any(l => l.BusinessId == v.BusinessId && !l.Closed &&
                    l.Storage - State.PrintRuns.Where(r => r.LocationId == l.Id).Sum(r => r.Remaining) >= copies)) continue;
                if (Try("Print", new StudioActionCommand(StudioAction.Print, v.Id, Amount: copies, Value: (int)PrintTier.CopyShop)))
                { _printRuns[v.Id] = runs + 1; Mark(runs == 0 ? "First print order" : "First reprint"); }
            }
        }

        private string BestGenre() => State.PublisherCatalog.Magazines.SelectMany(m => m.GenreAffinities.Keys.Select(g => (m, g)))
            .OrderByDescending(p => PitchRules.Chance(p.m.Tier, 60, State.EffectiveReputation, p.m.Affinity(p.g), State.GenrePopularity(p.g)))
            .ThenBy(p => p.m.Id).ThenBy(p => p.g).First().g;

        private void Stale(GuidanceStep step, string problem) => _stale.Add((State.Clock.Now, step.Id, problem));

        private bool Guided(GuidanceStep step, string label, ICommand command)
        {
            if (Try(label, command)) return true;
            Stale(step, _rejections[^1].Message);
            return false;
        }

        private void FollowGuidance()
        {
            var step = Guide();
            var series = State.Series.FirstOrDefault(s => s.Id == step.Project);
            switch (step.Id)
            {
                case "continue-series":
                    if (Guided(step, "Continue as ongoing series", new ContinueOneShotCommand(step.Project)))
                    { Mark("First ongoing series created"); Note($"Continued {series!.Title} as an ongoing series."); }
                    break;
                case "next-series":
                    var genre = BestGenre();
                    if (Guided(step, "Create series", new CreateSeriesCommand($"Pitch project {State.Series.Count + 1}", genre, Cadence.Monthly, 16)))
                    { Mark("First ongoing series created"); Note($"Created a new {genre} series to pitch."); }
                    break;
                case "pitch":
                case "pitch-rejected":
                    var best = CareerGuidance.PitchOutlooks(State, series!).FirstOrDefault(o => o.Open);
                    if (best is null) { if (step.Id == "pitch") Stale(step, "Suggested a pitch with every magazine on cooldown."); break; }
                    if (Guided(step, "Pitch", new PitchSeriesCommand(series!.Id, best.Magazine.Id)))
                        Note($"Pitched {series.Title} to {best.Magazine.Name} (tier {best.Magazine.Tier}, guidance chance {best.Chance:P0}).");
                    break;
                case "offer":
                    if (Guided(step, "Accept offer", new AcceptOfferCommand(step.Project))) { Mark("First serialization accepted"); Note($"Accepted serialization of {series!.Title}."); }
                    break;
                case "first-hire":
                    var cheapest = State.Candidates.Where(c => !c.Recruited && c.ExpiresAt > State.Clock.Now &&
                            (c.IntroductionBusinessId is null || c.IntroductionBusinessId == State.ControlledBusinessId))
                        .OrderBy(c => Math.Max(StudioRules.MinimumMonthlySalary, c.ExpectedSalary)).ThenBy(c => c.Id).FirstOrDefault();
                    if (cheapest is null) { Stale(step, "Suggested hiring with no candidate available."); break; }
                    if (!Hire(cheapest)) Stale(step, _rejections.Count > 0 ? _rejections[^1].Message : "Hiring failed.");
                    break;
                case "debut-wait-hire" or "debut-wait-desk":
                    Mark("Pre-debut stock ready");
                    if (_seen.Add("debut-hire-advice")) Note($"Debut wait advice: {step.Title}");
                    var early = State.Candidates.Where(c => !c.Recruited && c.ExpiresAt > State.Clock.Now &&
                            (c.IntroductionBusinessId is null || c.IntroductionBusinessId == State.ControlledBusinessId))
                        .OrderBy(c => Math.Max(StudioRules.MinimumMonthlySalary, c.ExpectedSalary)).ThenBy(c => c.Id).FirstOrDefault();
                    if (early is not null) { if (Hire(early)) Mark("Pre-debut hire"); }
                    else if (State.Recruitment is null && _recruitAttempts < 12 && Try("Recruit", new RecruitStaffCommand())) { _recruitAttempts++; Mark("First recruitment started"); }
                    break;
                case "debut-wait-convention":
                    Mark("Pre-debut stock ready");
                    if (_seen.Add("debut-convention-advice")) Note($"Debut wait advice: {step.Title} {step.Text.Split((char)10)[1]}");
                    // Same event choice as the advice; a player waits until booking opens 28 days before.
                    var debutClose = State.Series.First(x => x.Id == step.Project || x.Contract is not null && x.ChaptersPublished == 0).Contract?.FirstIssueClose ?? DateTime.MaxValue;
                    var scale = State.NextConvention(2) < debutClose ? 2 : 1;
                    if (State.NextConvention(scale) <= State.Clock.Now.Date.AddDays(28) &&
                        Guided(step, "Book convention", new StudioActionCommand(StudioAction.BookConvention, State.ProtagonistPersonId, Amount: step.Project, Value: scale)))
                    { Mark("Pre-debut convention booked"); Note($"Booked a scale {scale} convention on {State.NextConvention(scale):d MMM yyyy}."); }
                    break;
                case "debut-wait-doujin":
                    Mark("Pre-debut stock ready");
                    if (Guided(step, "Create side doujin", new CreateDoujinCommand($"Side story {State.Series.Count + 1}", "comedy")))
                    { Mark("Pre-debut side doujin"); Note("Started a short side doujin while waiting for the debut."); }
                    break;
                case "debut-wait-job":
                    Mark("Pre-debut stock ready");
                    if (State.Protagonist.OutsideJob == OutsideJob.None && Guided(step, "Part-time job", new SetOutsideJobCommand(OutsideJob.Evenings)))
                    { Mark("Pre-debut part-time job"); Note("Took an evening part-time job while waiting for the debut."); }
                    break;
                case "cancellation-warning":
                    // A careful player keeps the series going and lets the next chapters answer the warning.
                    if (_seen.Add($"warning-advice-{series?.Id}")) Note($"Warning advice: {step.Title}");
                    break;
                case "series-cancelled":
                    // Nothing to do yet: after the wait Helper-Chan moves on to the next series or magazine.
                    if (_seen.Add($"cancelled-advice-{series?.Id}")) Note($"Cancellation advice: {step.Title}");
                    break;
                case "serial-rhythm" when !State.Candidates.Any(c => !c.Recruited && c.ExpiresAt > State.Clock.Now) && _recruitAttempts < 12:
                    if (Try("Recruit", new RecruitStaffCommand())) { _recruitAttempts++; Mark("First recruitment started"); }
                    break;
            }
        }

        private void GrowStudio()
        {
            // After the guided first hire, one more whenever the account covers six months of the whole payroll:
            // up to four people for one series, six once two series run.
            var staff = State.ControlledStaff.ToArray();
            var payroll = staff.Sum(p => p.Employment?.MonthlySalary ?? 0);
            var cap = OwnedActive().Count(s => s.Publishing == PublishingStatus.Serialized) >= 2 ? 6 : 4;
            if (staff.Length >= cap || State.AvailableBusinessCash < 6 * (payroll + 180_000)) return;
            Mark("Growth hire attempted");
            var candidates = State.Candidates.Where(c => !c.Recruited && c.ExpiresAt > State.Clock.Now).OrderByDescending(c => c.Skills.Values.Sum()).ToArray();
            if (candidates.Length == 0)
            {
                if (_recruitAttempts < 12 && Try("Recruit", new RecruitStaffCommand())) _recruitAttempts++;
                return;
            }
            Hire(candidates[0]);
        }

        private bool Hire(Candidate c)
        {
            var location = State.Protagonist.Employment!.LocationId;
            var salary = Math.Max(StudioRules.MinimumMonthlySalary, c.ExpectedSalary);
            if (Try("Hire at current workplace", new HireStaffCommand(c.Id, location, salary))) { _hired = true; return true; }
            // Furnish the current workplace if the only problem is a missing desk.
            var arrangement = State.ArrangeOffice(location, true, true);
            if (arrangement.Purchases.Count > 0 && Try("Furnish", new ApplyOfficeLayoutCommand(location, State.OfficeRevision, arrangement.Placements, arrangement.Purchases, [])))
            { Mark("First furniture purchase"); if (Try("Hire after furnishing", new HireStaffCommand(c.Id, location, salary))) { _hired = true; return true; } }
            // Otherwise move the studio to the cheapest property with room for the new team.
            if (!MoveTo(Math.Max(4, State.ControlledStaff.Count() + 1))) return false;
            location = State.Protagonist.Employment!.LocationId;
            if (Try("Hire after move", new HireStaffCommand(c.Id, location, salary))) { _hired = true; return true; }
            return false;
        }

        private int CareerYear(DateTime at) => (int)((at - _start).TotalDays / 365.25);

        // Per career year (April to March): money at year end, business and personal flows, and how much there was to do.
        // "Decisions" are successful player commands; "quiet days" is the longest stretch with no decision, stop event or new text.
        private void YearSummary(StringBuilder sb)
        {
            static bool Flow(LedgerEntry e) => e.Kind is not (AccountEntryKind.Transfer or AccountEntryKind.Credit);
            var business = State.Ledger.Where(Flow).GroupBy(e => CareerYear(e.Time)).ToDictionary(g => g.Key, g => g.ToArray());
            var personal = State.Protagonist.PersonalAccount.Entries.Where(Flow).GroupBy(e => CareerYear(e.Time)).ToDictionary(g => g.Key, g => g.ToArray());
            var goalsDone = State.Events.Where(e => e.Type == EventType.GoalCompleted).GroupBy(e => CareerYear(e.Time)).ToDictionary(g => g.Key, g => g.Count());
            var published = State.Events.Where(e => e.Type == EventType.ChapterPublished && Owned(e.SeriesId)).GroupBy(e => CareerYear(e.Time)).ToDictionary(g => g.Key, g => g.Count());
            sb.AppendLine("## Ten-year summary\r\n\r\nCareer years run April to March. Flows exclude transfers between savings and the business and loan movements.\r\n");
            sb.AppendLine("| Year | Business at end | Personal at end | Business in | Business out | Biggest income | Personal in | Staff | Serialized | Top fanbase | Chapters published | Goal chapter |\r\n|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var group in _snapshots.Where(s => s.At > _start).GroupBy(s => CareerYear(s.At.AddDays(-1))).OrderBy(g => g.Key))
            {
                var end = group.Last();
                var entries = business.GetValueOrDefault(group.Key) ?? [];
                var top = entries.Where(e => e.Amount > 0).GroupBy(e => e.Reason).OrderByDescending(g => g.Sum(e => e.Amount)).FirstOrDefault();
                sb.AppendLine($"| {group.Key + 1} ({_start.AddYears(group.Key):yyyy}) | {end.Business:N0} | {end.Personal:N0} | {entries.Where(e => e.Amount > 0).Sum(e => e.Amount):N0} | " +
                    $"{-entries.Where(e => e.Amount < 0).Sum(e => e.Amount):N0} | {(top is null ? "none" : $"{top.Key} {top.Sum(e => e.Amount):N0}")} | " +
                    $"{(personal.GetValueOrDefault(group.Key) ?? []).Where(e => e.Amount > 0).Sum(e => e.Amount):N0} | {end.Staff} | {end.Serialized} | {end.Fans:N0} | " +
                    $"{published.GetValueOrDefault(group.Key)} | {(end.Chapter < GoalCatalog.Chapters.Length ? GoalCatalog.Chapters[end.Chapter].Name : "all done")} |");
            }
            sb.AppendLine("\r\n## Middle game activity\r\n\r\n| Year | Decisions | Kinds of decision | 32x stops | Of which texts | Goals done | Longest quiet stretch (days) |\r\n|---|---|---|---|---|---|---|");
            var moments = _actions.Select(a => a.At).Concat(_attention).OrderBy(t => t).ToList();
            foreach (var (year, p) in _pace.OrderBy(p => p.Key))
            {
                var acts = _actions.Where(a => CareerYear(a.At) == year).ToArray();
                var from = _start.AddDays(year * 365.25);
                var until = from.AddDays(365.25) < State.Clock.Now ? from.AddDays(365.25) : State.Clock.Now;
                var marks = moments.Where(t => t >= from && t < until).Prepend(from).Append(until).ToList();
                var quiet = marks.Zip(marks.Skip(1)).Select(m => (m.Second - m.First).TotalDays).DefaultIfEmpty(0).Max();
                var kinds = string.Join(", ", acts.GroupBy(a => a.Action).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}"));
                sb.AppendLine($"| {year + 1} | {acts.Length} | {(kinds.Length == 0 ? "none" : kinds)} | {p.Stops + p.Texts} | {p.Texts} | {goalsDone.GetValueOrDefault(year)} | {quiet:F0} |");
            }
            sb.AppendLine();
        }

        public string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Guided career playtest: seed {_seed}, {_difficulty}\r\n");
            sb.AppendLine($"Start {_start:yyyy-MM-dd}, end {State.Clock.Now:yyyy-MM-dd}. Estimated clock time at the fastest speeds: {Minutes / 60:F1} hours " +
                $"({_workSeconds / 3600:F1} hours of daytime play). Excludes reading, pausing and menus.\r\n");
            sb.AppendLine("## Pacing by speed\r\n\r\nEstimated real time per career year with daytime at 8x (recap click every working day) " +
                "and at 32x (routine days run on; the game stops only for stop events and new Helper-Chan texts). Nights at 32x in both.\r\n");
            sb.AppendLine("| Year | Working hours | 8x hours | 8x recap clicks | 32x hours | 32x stops | Of which texts |\r\n|---|---|---|---|---|---|---|");
            foreach (var (year, p) in _pace.OrderBy(p => p.Key))
            {
                var night = p.Night * NightSecondsPerHour;
                sb.AppendLine($"| {year + 1} | {p.Work:N0} | {(p.Work * WorkSecondsPerHour + night) / 3600:F1} | {p.Recaps} | " +
                    $"{(p.Work * QuietWorkSecondsPerHour + night) / 3600:F1} | {p.Stops + p.Texts} | {p.Texts} |");
            }
            sb.AppendLine();
            YearSummary(sb);
            sb.AppendLine("## Milestones\r\n\r\n| Date | Day | Real time (h:mm) | Milestone |\r\n|---|---|---|---|");
            foreach (var m in _milestones)
                sb.AppendLine($"| {m.At:yyyy-MM-dd} | {(m.At - _start).TotalDays:F0} | {(int)(m.Minutes / 60)}:{(int)(m.Minutes % 60):00} | {m.Name} |");
            sb.AppendLine("\r\n## Guidance timeline\r\n\r\n| From | Day | Step | Title |\r\n|---|---|---|---|");
            foreach (var g in _guidance) sb.AppendLine($"| {g.From:yyyy-MM-dd} | {(g.From - _start).TotalDays:F0} | {g.Id} | {g.Title} |");
            sb.AppendLine("\r\n## Guidance problems\r\n");
            var ends = _guidance.Skip(1).Select(g => g.From).Append(State.Clock.Now);
            foreach (var (g, until) in _guidance.Zip(ends).Where(p => (p.Second - p.First.From).TotalDays > 180))
                sb.AppendLine($"- Long wait: {g.Id} unchanged for {(until - g.From).TotalDays:F0} days from {g.From:yyyy-MM-dd}.");
            foreach (var g in _stale.GroupBy(x => (x.Step, x.Problem)))
                sb.AppendLine($"- Stale: {g.Key.Step} failed {g.Count()} times from {g.First().At:yyyy-MM-dd}: {g.Key.Problem}");
            if (_stale.Count == 0) sb.AppendLine("- No guided action failed.");
            sb.AppendLine("\r\n## Monthly snapshot\r\n\r\n| Month | Real hours | Personal yen | Business yen | Serialized | Staff | Top fanbase | Guidance |\r\n|---|---|---|---|---|---|---|---|");
            foreach (var line in _monthly) sb.AppendLine(line);
            sb.AppendLine($"\r\nSavings contributions: {_contributions} totalling {_contributed:N0} yen. Recruitment searches: {_recruitAttempts}.\r\n");
            sb.AppendLine("## Business ledger by year\r\n\r\n| Year | Reason | Entries | Yen |\r\n|---|---|---|---|");
            foreach (var g in State.Ledger.GroupBy(e => (e.Time.Year, e.Reason)).OrderBy(g => g.Key.Year).ThenBy(g => g.Sum(e => e.Amount)))
                sb.AppendLine($"| {g.Key.Year} | {g.Key.Reason} | {g.Count()} | {g.Sum(e => e.Amount):N0} |");
            sb.AppendLine();
            sb.AppendLine("## Series\r\n\r\n| Title | Status | Publishing | Chapters complete | Volumes | Copies sold |\r\n|---|---|---|---|---|---|");
            foreach (var s in State.Series)
                sb.AppendLine($"| {s.Title} | {s.Status} | {s.Publishing} | {s.Chapters.Count(c => c.Status == ChapterStatus.Complete)} | {s.Volumes.Count} | {s.Volumes.Sum(v => v.CopiesSold):N0} |");
            foreach (var s in State.Series.Where(s => s.Chapters.Any(c => c.Rank is not null)))
            {
                var ranks = s.Chapters.Where(c => c.Rank is not null).Select(c => c.Rank!.Value).ToList();
                var sorted = ranks.OrderBy(r => r).ToList();
                sb.AppendLine($"\r\nRanks for {s.Title}: best {sorted[0]}, median {sorted[sorted.Count / 2]}, worst {sorted[^1]}; by issue {string.Join(" ", ranks)}.");
                sb.AppendLine($"Quality by issue: {string.Join(" ", s.Chapters.Where(c => c.Rank is not null).Select(c => c.Quality))}.");
                var debut = State.Career.Rankings.FirstOrDefault(r => r.Rows.Any(e => e.SeriesId == s.Id));
                if (debut is not null)
                    sb.AppendLine($"Debut ranking: {string.Join(", ", debut.Rows.Select(e => $"{e.Rank}. {(e.SeriesId == s.Id ? "*" : "")}{e.Score:0.0}"))}.");
            }
            sb.AppendLine("\r\n## Decisions and notable events\r\n");
            foreach (var d in _decisions) sb.AppendLine("- " + d);
            sb.AppendLine("\r\n## Rejected actions (friction)\r\n\r\n| Action | Count | First date | Message |\r\n|---|---|---|---|");
            foreach (var g in _rejections.GroupBy(r => (r.Action, r.Message)).OrderBy(g => g.First().At))
                sb.AppendLine($"| {g.Key.Action} | {g.Count()} | {g.First().At:yyyy-MM-dd} | {g.Key.Message} |");
            sb.AppendLine("\r\n## Event counts\r\n");
            foreach (var g in State.Events.GroupBy(e => e.Type).Where(g => g.Key is not (EventType.StageStarted or EventType.StageCompleted or EventType.DayStarted or EventType.WeekStarted or EventType.DailyRecap or EventType.CommandApplied or EventType.IndustryNews)).OrderBy(g => g.Key))
                sb.AppendLine($"- {g.Key}: {g.Count()}");
            return sb.ToString();
        }
    }
}
