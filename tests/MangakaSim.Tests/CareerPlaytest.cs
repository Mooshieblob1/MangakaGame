using System.Text;
using MangakaSim.Catalog;
using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

/// <summary>
/// Sub-project 10 guided-player playtest. A scripted player follows Helper-Chan's guidance and
/// ordinary player choices for three in-game years; after the first sale it acts only on the current guidance step, recording milestones, money, estimated real
/// time and every rejected action. Excluded from the normal suite; run with
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
    private const int Years = 3;

    [Theory]
    [InlineData(0, CareerDifficulty.Standard)]
    [InlineData(1, CareerDifficulty.Standard)]
    [InlineData(42, CareerDifficulty.Standard)]
    [InlineData(7, CareerDifficulty.Standard)]
    [InlineData(7, CareerDifficulty.Relaxed)]
    [InlineData(7, CareerDifficulty.Challenging)]
    public void Three_year_guided_career(int seed, CareerDifficulty difficulty)
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
        for (var year = 1; year <= Years && warning == DateTime.MinValue; year++)
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
        private int _textsSeen;

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
            try { State.Apply(command); return true; }
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
                    _monthly.Add($"| {State.Clock.Now:yyyy-MM} | {Minutes / 60:F1} | {State.PersonalMoney:N0} | {State.Money:N0} | " +
                        $"{State.Series.Count(s => s.Publishing == PublishingStatus.Serialized)} | {State.ControlledStaff.Count()} | {Guide().Id} |");
                    _nextMonth = _nextMonth.AddMonths(1);
                }
            }
        }

        private GuidanceStep Guide()
        {
            CareerGuidance.Observe(State, _guide);
            // Each new Helper-Chan text stops a 32x day, like the stop events.
            var texts = _guide.Thread.Count;
            if (texts > _textsSeen)
            {
                var year = (int)((State.Clock.Now - _start).TotalDays / 365.25);
                var pace = _pace.GetValueOrDefault(year); pace.Texts += texts - _textsSeen; _pace[year] = pace;
                _textsSeen = texts;
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
                if (CareerGuidance.FastSpeedStops.Contains(e.Type)) pace.Stops++;
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
            if (_hired) GrowStudio();
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
            // After the guided first hire, one more whenever the account covers six months of the whole payroll.
            var staff = State.ControlledStaff.ToArray();
            var payroll = staff.Sum(p => p.Employment?.MonthlySalary ?? 0);
            if (staff.Length >= 4 || State.AvailableBusinessCash < 6 * (payroll + 180_000)) return;
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
            // Otherwise move the studio to the cheapest property with room for two.
            if (!State.Locations.Single(l => l.Id == location).IsFamilyHome) return false;
            var offer = TokyoProperties.All.Where(p => p.Seats >= 2).OrderBy(p => p.Rent).First();
            if (State.AvailableBusinessCash < offer.Rent * 6 || !Try("Move studio", new StudioActionCommand(StudioAction.Move, offer.Id))) return false;
            Mark("First studio move"); Note($"Moved to {offer.District} (rent {offer.Rent:N0} yen a month).");
            location = State.Protagonist.Employment!.LocationId;
            arrangement = State.ArrangeOffice(location, true, true);
            Try("Furnish new studio", new ApplyOfficeLayoutCommand(location, State.OfficeRevision, arrangement.Placements, arrangement.Purchases, []));
            if (Try("Hire after move", new HireStaffCommand(c.Id, location, salary))) { _hired = true; return true; }
            return false;
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
            sb.AppendLine("\r\n## Monthly snapshot\r\n\r\n| Month | Real hours | Personal yen | Business yen | Serialized | Staff | Guidance |\r\n|---|---|---|---|---|---|---|");
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
