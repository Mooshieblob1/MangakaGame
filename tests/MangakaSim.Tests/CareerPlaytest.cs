using System.Text;
using MangakaSim.Catalog;
using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

/// <summary>
/// Sub-project 10 guided-player playtest. A scripted player follows Helper-Chan's guidance and
/// ordinary player choices for three in-game years, recording milestones, money, estimated real
/// time and every rejected action. Excluded from the normal suite; run with
/// dotnet test --filter Category=Playtest
/// </summary>
[Trait("Category", "Playtest")]
public class CareerPlaytest
{
    // Mirrors the Godot driver: 36 s per game hour at 1x, player at 8x during work hours,
    // 2.5 s per hour at 1x played at 32x overnight.
    private const double WorkSecondsPerHour = 36.0 / 8, NightSecondsPerHour = 2.5 / 32;
    private const int Years = 3;

    [Theory]
    [InlineData(0, CareerDifficulty.Standard)]
    [InlineData(1, CareerDifficulty.Standard)]
    [InlineData(42, CareerDifficulty.Standard)]
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

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MangakaGame.sln"))) dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }

    private sealed class GuidedPlayer
    {
        public GameState State { get; }
        private readonly int _seed;
        private readonly CareerDifficulty _difficulty;
        private readonly GuidancePreferences _guide = new();
        private readonly List<(DateTime At, double Minutes, string Name)> _milestones = new();
        private readonly List<(DateTime At, string Action, string Message)> _rejections = new();
        private readonly List<string> _decisions = new();
        private readonly List<string> _monthly = new();
        private readonly List<(DateTime From, string Id, string Title)> _guidance = new();
        private readonly HashSet<string> _seen = new();
        private readonly Dictionary<int, int> _printRuns = new();
        private readonly DateTime _start;
        private double _seconds, _workSeconds;
        private int _eventCursor, _contributions, _recruitAttempts;
        private long _contributed;
        private bool _hired;

        public GuidedPlayer(int seed, CareerDifficulty difficulty)
        {
            _seed = seed; _difficulty = difficulty;
            State = GameState.NewGame(seed);
            if (difficulty != CareerDifficulty.Standard) Try("Difficulty", new DifficultyCommand(difficulty));
            _start = State.Clock.Now;
        }

        private double Minutes => _seconds / 60;
        private void Mark(string name) { if (_seen.Add(name)) _milestones.Add((State.Clock.Now, Minutes, name)); }
        private void Note(string text) => _decisions.Add($"{State.Clock.Now:yyyy-MM-dd} {text}");

        private bool Try(string label, ICommand command)
        {
            try { State.Apply(command); return true; }
            catch (InvalidCommandException e) { _rejections.Add((State.Clock.Now, label, e.Message)); return false; }
        }

        public void Play(int years)
        {
            var end = _start.AddYears(years);
            var nextMonth = _start;
            while (State.Clock.Now < end)
            {
                if (State.Clock.Hour == 9) Decide();
                var working = State.ControlledStaff.Any(p => p.Schedule.IsRegularHour(State.Clock.Now));
                var cost = working ? WorkSecondsPerHour : NightSecondsPerHour;
                _seconds += cost; if (working) _workSeconds += cost;
                State.Advance(1);
                ReadEvents();
                if (State.Clock.Now >= nextMonth)
                {
                    _monthly.Add($"| {State.Clock.Now:yyyy-MM} | {Minutes / 60:F1} | {State.PersonalMoney:N0} | {State.Money:N0} | " +
                        $"{State.Series.Count(s => s.Publishing == PublishingStatus.Serialized)} | {State.ControlledStaff.Count()} | {Guide().Id} |");
                    nextMonth = nextMonth.AddMonths(1);
                }
            }
        }

        private GuidanceStep Guide()
        {
            CareerGuidance.Observe(State, _guide);
            return CareerGuidance.Evaluate(State, _guide);
        }

        private void ReadEvents()
        {
            for (; _eventCursor < State.Events.Count; _eventCursor++)
            {
                var e = State.Events[_eventCursor];
                switch (e.Type)
                {
                    case EventType.ChapterCompleted: Mark("First chapter completed"); break;
                    case EventType.PitchSubmitted: Mark("First pitch submitted"); break;
                    case EventType.EditorRedoRequested: Mark("First editor redo"); break;
                    case EventType.PitchRejected: Mark("First pitch rejected"); Note("Pitch rejected: " + e.Message); break;
                    case EventType.SerializationOffered: Mark("First serialization offer"); Note(e.Message); break;
                    case EventType.OfferExpired: Mark("Offer expired"); break;
                    case EventType.ChapterPublished: Mark("First magazine chapter published"); break;
                    case EventType.DeadlineMissed: Mark("First deadline missed"); break;
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
            // After the first readers the player picks the doujin growth route, as the direction card offers.
            if (step.Id == "direction") { _guide.Route = "doujin"; Mark("Guidance: direction chosen"); }

            var owned = State.Series.Where(s => s.BusinessId == State.ControlledBusinessId).ToArray();
            if (owned.Length == 0) { if (Try("Create doujin", new CreateDoujinCommand("First pages", "adventure"))) Mark("First doujin created"); return; }

            FundBusiness();
            SellBooks(owned);
            if (_seen.Contains("First sale")) PursueMagazine(owned);
            if (State.Series.Any(s => s.Publishing == PublishingStatus.Serialized) || State.Money > 600_000) GrowStudio();
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
                if (Try("Print", new StudioActionCommand(StudioAction.Print, v.Id, Amount: copies, Value: (int)PrintTier.CopyShop)))
                { _printRuns[v.Id] = runs + 1; Mark(runs == 0 ? "First print order" : "First reprint"); }
            }
        }

        private (Magazine Magazine, string Genre, double Chance) BestPitch(Series? series)
        {
            var genres = series is null ? State.PublisherCatalog.Magazines.SelectMany(m => m.GenreAffinities.Keys).Distinct() : [series.Genre];
            return State.PublisherCatalog.Magazines.SelectMany(m => genres.Select(g => (m, g)))
                .Where(p => series is null || !series.PitchCooldowns.TryGetValue(p.m.Id, out var until) || until <= State.Clock.Now)
                .Select(p => (p.m, p.g, PitchRules.Chance(p.m.Tier, 60, State.EffectiveReputation, p.m.Affinity(p.g), State.GenrePopularity(p.g))))
                .OrderByDescending(p => p.Item3).ThenBy(p => p.m.Id).FirstOrDefault();
        }

        private void PursueMagazine(Series[] owned)
        {
            foreach (var offered in owned.Where(s => s.Publishing == PublishingStatus.Offered))
                if (Try("Accept offer", new AcceptOfferCommand(offered.Id))) { Mark("First serialization accepted"); Note($"Accepted serialization of {offered.Title}."); }
            if (owned.Any(s => s.Status == SeriesStatus.Active && s.Publishing is PublishingStatus.Serialized or PublishingStatus.Pitching or PublishingStatus.Offered)) return;
            var candidate = owned.LastOrDefault(s => !s.StandaloneDoujin && s.Status == SeriesStatus.Active && s.Publishing == PublishingStatus.Unpublished);
            if (candidate is null)
            {
                var best = BestPitch(null);
                var title = $"Pitch project {owned.Length + 1}";
                if (!Try("Create series", new CreateSeriesCommand(title, best.Genre, Cadence.Monthly, 16))) return;
                candidate = State.Series.Last();
                Mark("First ongoing series created"); Note($"Created {title} ({best.Genre}) aiming at {best.Magazine.Name}.");
            }
            var target = BestPitch(candidate);
            if (target.Magazine is null) return;
            if (Try("Pitch", new PitchSeriesCommand(candidate.Id, target.Magazine.Id)))
                Note($"Pitched {candidate.Title} to {target.Magazine.Name} (tier {target.Magazine.Tier}, estimated chance {target.Chance:P0} at quality 60).");
        }

        private void GrowStudio()
        {
            // A first assistant once serialized, then one more whenever the account covers six months of the whole payroll.
            var staff = State.ControlledStaff.ToArray();
            var payroll = staff.Sum(p => p.Employment?.MonthlySalary ?? 0);
            if (staff.Length >= 4 || (_hired && staff.Length > 1 && State.AvailableBusinessCash < 6 * (payroll + 180_000))) return;
            if (_hired && staff.Length > 1) Mark("Growth hire attempted");
            var candidates = State.Candidates.Where(c => !c.Recruited && c.ExpiresAt > State.Clock.Now).OrderByDescending(c => c.Skills.Values.Sum()).ToArray();
            if (candidates.Length == 0)
            {
                if (_recruitAttempts < 12 && Try("Recruit", new RecruitStaffCommand())) { _recruitAttempts++; Mark("First recruitment started"); }
                return;
            }
            var location = State.Protagonist.Employment!.LocationId;
            var c = candidates[0];
            if (Try("Hire at current workplace", new HireStaffCommand(c.Id, location, c.ExpectedSalary))) { _hired = true; return; }
            // Furnish the current workplace if the only problem is a missing desk.
            var arrangement = State.ArrangeOffice(location, true, true);
            if (arrangement.Purchases.Count > 0 && Try("Furnish", new ApplyOfficeLayoutCommand(location, State.OfficeRevision, arrangement.Placements, arrangement.Purchases, [])))
            { Mark("First furniture purchase"); if (Try("Hire after furnishing", new HireStaffCommand(c.Id, location, c.ExpectedSalary))) { _hired = true; return; } }
            // Otherwise move the studio to the cheapest property with room for two.
            if (State.Locations.Single(l => l.Id == location).IsFamilyHome)
            {
                var offer = TokyoProperties.All.Where(p => p.Seats >= 2).OrderBy(p => p.Rent).First();
                if (State.AvailableBusinessCash < offer.Rent * 6) return;
                if (Try("Move studio", new StudioActionCommand(StudioAction.Move, offer.Id)))
                {
                    Mark("First studio move"); Note($"Moved to {offer.District} (rent {offer.Rent:N0} yen a month).");
                    location = State.Protagonist.Employment!.LocationId;
                    arrangement = State.ArrangeOffice(location, true, true);
                    Try("Furnish new studio", new ApplyOfficeLayoutCommand(location, State.OfficeRevision, arrangement.Placements, arrangement.Purchases, []));
                    if (Try("Hire after move", new HireStaffCommand(c.Id, location, c.ExpectedSalary))) _hired = true;
                }
            }
        }

        public string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Guided career playtest: seed {_seed}, {_difficulty}\r\n");
            sb.AppendLine($"Start {_start:yyyy-MM-dd}, end {State.Clock.Now:yyyy-MM-dd}. Estimated clock time at the fastest speeds: {Minutes / 60:F1} hours " +
                $"({_workSeconds / 3600:F1} hours of daytime play). Excludes reading, pausing and menus.\r\n");
            sb.AppendLine("## Milestones\r\n\r\n| Date | Day | Real time (h:mm) | Milestone |\r\n|---|---|---|---|");
            foreach (var m in _milestones)
                sb.AppendLine($"| {m.At:yyyy-MM-dd} | {(m.At - _start).TotalDays:F0} | {(int)(m.Minutes / 60)}:{(int)(m.Minutes % 60):00} | {m.Name} |");
            sb.AppendLine("\r\n## Guidance timeline\r\n\r\n| From | Day | Step | Title |\r\n|---|---|---|---|");
            foreach (var g in _guidance) sb.AppendLine($"| {g.From:yyyy-MM-dd} | {(g.From - _start).TotalDays:F0} | {g.Id} | {g.Title} |");
            sb.AppendLine("\r\n## Monthly snapshot\r\n\r\n| Month | Real hours | Personal yen | Business yen | Serialized | Staff | Guidance |\r\n|---|---|---|---|---|---|---|");
            foreach (var line in _monthly) sb.AppendLine(line);
            sb.AppendLine($"\r\nSavings contributions: {_contributions} totalling {_contributed:N0} yen. Recruitment searches: {_recruitAttempts}.\r\n");
            sb.AppendLine("## Series\r\n\r\n| Title | Status | Publishing | Chapters complete | Volumes | Copies sold |\r\n|---|---|---|---|---|---|");
            foreach (var s in State.Series)
                sb.AppendLine($"| {s.Title} | {s.Status} | {s.Publishing} | {s.Chapters.Count(c => c.Status == ChapterStatus.Complete)} | {s.Volumes.Count} | {s.Volumes.Sum(v => v.CopiesSold):N0} |");
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
