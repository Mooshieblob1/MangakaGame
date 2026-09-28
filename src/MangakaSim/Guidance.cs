using MangakaSim.Catalog;
using MangakaSim.Rules;

namespace MangakaSim;

public sealed class GuidancePreferences
{
    public bool Visible { get; set; } = true;
    public string Route { get; set; } = "career";
    public int Project { get; set; }
    public HashSet<string> Completed { get; set; } = new();
    /// <summary>Helper-Chan's message thread. Presentation data only; older saves start empty.</summary>
    public List<GuidanceMessage> Thread { get; set; } = new();
}

public sealed class GuidanceMessage
{
    public string Step { get; set; } = "";
    public DateTime Time { get; set; }
    public List<string> Texts { get; set; } = new();
    public bool Read { get; set; }
}

/// <summary>Text holds short messages separated by new lines, each about 140 characters at most.</summary>
public sealed record GuidanceStep(string Id, string Title, string Text, string Target, int Project = 0)
{
    public IReadOnlyList<string> Texts => Text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed record PitchOutlook(Magazine Magazine, double Chance, DateTime? CooldownUntil)
{
    public bool Open => CooldownUntil is null;
}

/// <summary>Read-only suggestions. Only presentation preferences remember observed progress.</summary>
public static class CareerGuidance
{
    public const int ThreadLimit = 200;
    public const int TextLimit = 140;
    public static readonly string[] Routes = ["career", "contest", "employment"];
    // Routes saved before Tier 1 fix 1 (alpha.11 and earlier); Observe moves them onto "career".
    public static readonly string[] LegacyRoutes = ["opening", "doujin"];
    // Contests and employment are detours (fresh-player finding A1): only these career steps give way to them.
    private static readonly HashSet<string> CalmCareerSteps = ["pitch", "pitch-wait", "next-series", "continue-series", "serial-rhythm", "career-settled"];

    public static GuidanceStep Evaluate(GameState state, GuidancePreferences preferences)
    {
        var owned = state.Series.Where(s => s.BusinessId == state.ControlledBusinessId && !s.AwaitingCreatorDestination &&
            (state.Control == ControlMode.OwnerDirector || s.LeadPersonId == state.ProtagonistPersonId)).ToArray();
        var protagonistTitles = state.Series.Where(s => WasCreator(state,s)).ToArray();
        bool sale = protagonistTitles.Any(s => s.Volumes.Any(v => v.CopiesSold > 0)) ||
            state.Career.Sales.Any(x => protagonistTitles.Any(s => s.Id == x.Series) && x.Physical + x.Digital + x.Overseas > 0);
        bool established = protagonistTitles.Any(s => s.Chapters.Any(c => c.PublishedAt is not null)) || sale || preferences.Completed.Contains("first-sale");
        if (preferences.Route is "contest" or "employment" && established && state.Control == ControlMode.OwnerDirector &&
            Career(state, preferences, owned) is { } career && !CalmCareerSteps.Contains(career.Id))
            return career;
        if (preferences.Route == "employment")
            return state.Control == ControlMode.OwnerDirector
                ? new("employment", "Explore studio employment", "Review the employer, pay and title rights before confirming a move.\nYour career follows the mangaka wherever you go.", "employment")
                : new("employed", "Settle into your studio", "You already have an employer.\nReview your team and work schedule, or pick another direction in Help.", "staff");
        if (preferences.Route == "contest")
        {
            var manuscript = state.Progression.Manuscripts.LastOrDefault(m => !m.Released && owned.Any(s => s.Id == m.SeriesId));
            return manuscript is null
                ? new("contest-create", "Prepare an unpublished manuscript", "Create a separate contest manuscript.\nA doujin already collected or sold cannot enter as unpublished work.", "awards")
                : new("contest-review", "Review your contest manuscript", "Finish its ordinary production first.\nThen check eligibility, the deadline and any pending result in Awards & contests.", "awards", manuscript.SeriesId);
        }
        if (established) return Career(state, preferences, owned);

        var project = owned.FirstOrDefault(s => s.Id == preferences.Project) ??
            owned.LastOrDefault(s => s.StandaloneDoujin) ?? owned.LastOrDefault(s => s.Publishing == PublishingStatus.Unpublished &&
                !state.Progression.Manuscripts.Any(m => m.SeriesId == s.Id && !m.Released));
        if (project is null)
            return new("create", "Make a small doujin", "Let's start with a 16-page one-shot: one story, one book, then stop.\nOngoing series continue later in short numbered issues.", "create");
        var book = project.Volumes.LastOrDefault(v => v.IsDoujin && v.BusinessId == state.ControlledBusinessId);
        if (book is null)
            return new("produce", "Finish the pages", "The planner assigns the work automatically.\nWatch the production queue and use the speed controls when you are ready.", "production", project.Id);
        if(state.DoujinOnlineListed(book.Id))return new("sell-online","Find your online readers","Your download is on sale!\nPurchases settle on Mondays, based on quality and genre interest. No printed stock is needed.","printing",project.Id);
        var pending = state.PrintRuns.FirstOrDefault(r => r.VolumeId == book.Id && !r.Delivered);
        if (pending is not null)
            return new("delivery", "Copies are on their way", $"Delivery is due {pending.DueAt:d MMM, HH:mm}.\nKeep working in the meantime. No second order is needed.", "printing", project.Id);
        if (state.Stock(book.Id) == 0)
            return new("print", "Print a small batch", "Choose printing, or an online release with no upfront cost.\nThe copy shop takes 1 to 100 copies; downloads need no stock.", "printing", project.Id);
        return new("sell", "Find your first readers", "Stock is ready!\nLocal sales settle on Mondays, and conventions offer another route. Profit is not guaranteed.", "printing", project.Id);
    }

    /// <summary>The continuous career path after the first sale: pitch, serialization, first deadline and first hire.</summary>
    private static GuidanceStep Career(GameState state, GuidancePreferences preferences, Series[] owned)
    {
        if (state.Control != ControlMode.OwnerDirector)
            return new("employed", "Settle into your studio", "You work for another studio now.\nKeep your chapters on schedule and review your team in Staff.", "staff");
        var active = owned.Where(s => s.Status == SeriesStatus.Active).ToArray();
        var offered = active.FirstOrDefault(s => s.Publishing == PublishingStatus.Offered && s.PendingOffer is not null);
        if (offered is not null)
        {
            var offer = offered.PendingOffer!;
            var magazine = state.PublisherCatalog.Get(offer.MagazineId);
            return new("offer", "A serialization offer!", $"{magazine.Name} wants to serialize {offered.Title}!\n" +
                $"They pay ¥{offer.FeePerPage:N0} per page. Accept or decline in Publishing before {offer.ExpiresAt:d MMM yyyy}.\n" +
                "Once you sign, chapters have real deadlines. A missed issue counts against the series.", "publishing", offered.Id);
        }
        var serialized = active.Where(s => s.Publishing == PublishingStatus.Serialized && s.Contract is not null).ToArray();
        var debut = serialized.FirstOrDefault(s => s.ChaptersPublished == 0);
        if (debut is not null) return Debut(state, owned, debut);
        var pitching = active.FirstOrDefault(s => s.Publishing == PublishingStatus.Pitching);
        if (pitching is not null) return Pitching(state, pitching);
        if (serialized.Length > 0)
        {
            if (serialized.FirstOrDefault(s => s.WarningIssuedAt is not null) is { } warned) return Warning(state, warned);
            var series = serialized[0];
            if (!state.ControlledStaff.Any(p => p.Id != state.ProtagonistPersonId))
            {
                var wage = CheapestWage(state);
                var runway = state.HiringRunway(wage ?? StudioRules.MinimumMonthlySalary);
                if (runway.Safe && state.WorkplaceWithFreeDesk is null)
                    return new("first-hire-desk", "An assistant needs a desk", "Funds can support an assistant now, but every desk is taken.\n" +
                        "Add a desk in Furniture first. If the room is full, compare bigger places in Properties.", "furniture", series.Id);
                if (runway.Safe)
                    return new("first-hire", "Time for a first assistant", (wage is { } monthly
                        ? $"Funds and confirmed page fees cover {RunwayText(runway)} of wages and costs, even with a ¥{monthly:N0} assistant.\n"
                        : "Funds and confirmed page fees can support an assistant now.\n") +
                        "An assistant can take Backgrounds or Tones so you keep up with deadlines.\n" + SearchText(state, wage is not null), "recruitment", series.Id);
                return new("serial-rhythm", "Keep the chapters coming", "Deliver each chapter by its issue close. Page fees arrive as chapters publish.\n" +
                    "Hiring is safe once funds cover about three months of wages and costs, counting confirmed page fees.\n" +
                    $"With an assistant, that is about {RunwayText(runway)} right now.", "production", series.Id);
            }
            return new("career-settled", "Your studio is running", "You have a serialization and a team. I'll message you when something needs you.\n" +
                "Contests and studio employment are side routes you can start from Help.", "help", series.Id);
        }

        if (state.Events.LastOrDefault(e => e.Type == EventType.SeriesCancelled && owned.Any(s => s.Id == e.SeriesId)) is { } cancelled &&
            cancelled.Time >= state.Clock.Now.AddDays(-28) && owned.First(s => s.Id == cancelled.SeriesId) is var ended)
        {
            var name = cancelled.MagazineId is { } id ? state.PublisherCatalog.Get(id).Name : "The magazine";
            var until = cancelled.MagazineId is { } m && ended.PitchCooldowns.TryGetValue(m, out var again) ? again : cancelled.Time.AddDays(52 * 7);
            return new("series-cancelled", "The series has ended", $"{name} cancelled {ended.Title}. It happens to many series, so please don't give up!\n" +
                "Your published volumes keep selling, and the readers you gained stay with you.\n" +
                $"{name} will look at this series again after {until:d MMM yyyy}. Pitch it to another magazine, or start a new series.", "publishing", ended.Id);
        }
        var project = active.FirstOrDefault(s => s.Id == preferences.Project && s.Publishing == PublishingStatus.Unpublished && !InContest(state, s)) ??
            active.LastOrDefault(s => !s.StandaloneDoujin && s.Publishing == PublishingStatus.Unpublished && !InContest(state, s)) ??
            active.LastOrDefault(s => s.StandaloneDoujin && !InContest(state, s));
        if (project is null)
            return new("next-series", "Start your next story", "A new series gives you something to pitch to a magazine.\nCreate one, and keep selling your doujin meanwhile.", "create");
        if (project.StandaloneDoujin)
            return new("continue-series", "Turn it into a series", $"Readers bought {project.Title}! Magazines only take ongoing series, though.\n" +
                "Open Series details and choose Continue as ongoing series. Then we can pitch it.", "series", project.Id);
        var outlooks = PitchOutlooks(state, project);
        var best = outlooks.FirstOrDefault(o => o.Open);
        if (LastRejection(state, project) is { } rejection && project.PitchCooldowns.Values.Any(t => t > state.Clock.Now))
        {
            var rejected = rejection.MagazineId is { } id && project.PitchCooldowns.TryGetValue(id, out var until) ? (state.PublisherCatalog.Get(id), until) : ((Magazine?)null, (DateTime?)null);
            var factor = rejection.Message.Split("weakest factor: ").ElementAtOrDefault(1)?.TrimEnd('.') ?? "";
            var text = $"{(rejected.Item1 is { } m ? m.Name : "The magazine")} passed on {project.Title}. That happens to most first pitches!\n" + FactorAdvice(factor) + "\n";
            if (rejected.Item2 is { } again) text += $"They will look again after {again:d MMM yyyy}. ";
            text += best is not null ? $"{best.Magazine.Name} is open now, about {best.Chance:P0} chance." :
                $"Every magazine is on cooldown until {outlooks.Min(o => o.CooldownUntil)!.Value:d MMM yyyy}. Keep selling doujin meanwhile.";
            return new("pitch-rejected", "Not this time", text, "publishing", project.Id);
        }
        if (best is null)
            return new("pitch-wait", "Magazines need a break", $"Every magazine is on cooldown for {project.Title} until {outlooks.Min(o => o.CooldownUntil)!.Value:d MMM yyyy}.\n" +
                "Keep selling doujin and improving your pages meanwhile.", "printing", project.Id);
        return new("pitch", "Pitch to a magazine", $"{project.Title} can be pitched now! {best.Magazine.Name} gives the best chance, about {best.Chance:P0}.\n" +
            "You draw a 31-page sample, and the editor answers at an issue close.\nMost first pitches are rejected. That is normal, and you can try again.", "publishing", project.Id);
    }

    private static GuidanceStep Warning(GameState state, Series series)
    {
        var magazine = state.PublisherCatalog.Get(series.Contract!.MagazineId);
        var clocks = state.CancellationClocks(series);
        var left = Math.Max(1, (int)Math.Ceiling(clocks.Cancel - CancellationRules.IssueAge(series.WarningIssuedAt!.Value, state.Clock.Now, magazine.Cadence)));
        var rank = series.LastRank is { } r ? $"ranked {r} of {magazine.RosterSize} in {magazine.Name}. Series below rank {magazine.CancellationRank} are at risk"
            : $"is below the safe rankings in {magazine.Name}";
        return new("cancellation-warning", "A cancellation warning", $"{series.Title} {rank}.\n" +
            $"The editor decides in about {left} {(left == 1 ? "issue" : "issues")} unless it climbs back.\n" + WarningAdvice(state, series, magazine) + "\n" +
            "You can also end the series on your own terms in Publishing.", "production", series.Id);
    }

    private static string WarningAdvice(GameState state, Series series, Magazine magazine)
    {
        var genre = TrendRules.Normalise(series.Genre, state.TrendCatalog);
        var quality = series.Chapters.Where(c => c.PublishedAt is not null && c.Quality is not null).OrderBy(c => c.PublishedAt).LastOrDefault()?.Quality ?? 60;
        if (magazine.Affinity(genre) * state.GenrePopularity(genre) < .9)
            return "Genre fit is the weakest part: readers of this magazine or this year want other stories. Strong pages still help most.";
        return quality < RankingRules.FanScore(series.Fanbase, magazine.Tier)
            ? "Page quality is the weakest part. Give each chapter more time, put a stronger assistant on it, or use fewer pages."
            : "Readership is the weakest part. Keep every issue on time and raise page quality: more time per chapter, a stronger assistant or fewer pages.";
    }

    private static GuidanceStep Pitching(GameState state, Series series)
    {
        var sample = series.Chapters.FirstOrDefault(c => c.IsOneShot && !c.PitchResolved);
        var magazine = sample?.PitchMagazineId is { } id ? state.PublisherCatalog.Get(id) : null;
        var name = magazine?.Name ?? "the magazine";
        if (sample is null || magazine is null)
            return new("pitch-waiting", "Waiting for an answer", $"{series.Title} is with {name}.\nThe editor answers at an issue close.", "publishing", series.Id);
        if (sample.Editor == EditorStatus.AwaitingReview)
            return new("pitch-name-review", "The editor is reading", $"The editor at {name} is reviewing your Name (storyboard).\n" +
                $"Expect an answer around {sample.EditorDecisionAt:d MMM, HH:mm}.", "production", series.Id);
        if (sample.Editor == EditorStatus.RedoRequested)
            return new("pitch-name-redo", "A revision request", "The editor asked for a revised Name. It is normal, so don't worry!\n" +
                "The planner redraws it automatically. After two revisions the editor accepts.", "production", series.Id);
        if (sample.Status != ChapterStatus.Complete)
            return new("pitch-sample", "Drawing the pitch sample", $"The 31-page sample for {name} is due {sample.DueDate:d MMM yyyy}.\n" +
                "The editor checks the Name first. Follow progress in Production.", "production", series.Id);
        var answer = IssueSchedule.FirstCloseAtOrAfter(magazine, sample.DueDate > state.Clock.Now ? sample.DueDate : state.Clock.Now);
        return new("pitch-waiting", "Waiting for an answer", $"The sample is finished! {name} answers at its issue close on {answer:d MMM yyyy}.\n" +
            "Keep selling doujin meanwhile.", "publishing", series.Id);
    }

    private static string FactorAdvice(string factor) => factor switch
    {
        "quality" => "The pages held it back. Give the Name and Pencils more time or stronger artists.",
        "reputation" => "Your name is still small. Published work and contests build reputation.",
        "affinity" => "That magazine rarely runs this genre. Try one that favours it.",
        "trend" => "This genre is out of fashion right now. Check Industry for what readers want.",
        _ => "Editors weigh quality, reputation, genre fit and trends.",
    };

    /// <summary>Estimated acceptance chance for each magazine, best first, using the latest finished chapter's quality.</summary>
    public static IReadOnlyList<PitchOutlook> PitchOutlooks(GameState state, Series series)
    {
        var quality = series.Chapters.Where(c => c.Status == ChapterStatus.Complete && c.Quality is not null).OrderBy(c => c.Id).LastOrDefault()?.Quality ?? 60;
        var reputation = state.BusinessReputation(series.BusinessId);
        var genre = TrendRules.Normalise(series.Genre, state.TrendCatalog);
        var trend = series.IsIconic ? 1 : state.GenrePopularity(genre);
        var recognition = state.Progression.Awards.Any(a => a.SeriesId == series.Id && a.Prize > 0 && a.ResolvedAt >= state.Clock.Now.AddDays(-365)) ? .1 : 0;
        return state.PublisherCatalog.Magazines.Select(m => new PitchOutlook(m,
                Math.Min(.95, PitchRules.Chance(m.Tier, quality, reputation, series.IsIconic ? 1 : m.Affinity(genre), trend) * state.PitchFactor(series.BusinessId) + recognition),
                series.PitchCooldowns.TryGetValue(m.Id, out var until) && until > state.Clock.Now ? until : null))
            .OrderByDescending(o => o.Open).ThenByDescending(o => o.Chance).ThenBy(o => o.Magazine.Id).ToArray();
    }

    public static PitchOutlook Outlook(GameState state, Series series, string magazineId) =>
        PitchOutlooks(state, series).First(o => o.Magazine.Id == magazineId);

    /// <summary>Before the first issue: explain the lead time, count chapters ready ahead, then suggest side work (Q24, Q25).</summary>
    private static GuidanceStep Debut(GameState state, Series[] owned, Series debut)
    {
        var contract = debut.Contract!;
        var magazine = state.PublisherCatalog.Get(contract.MagazineId);
        var (ready, target) = state.ChaptersReadyAhead(debut);
        if (ready < target)
            return new(ready == 0 ? "first-deadline" : $"first-deadline-{ready}", "Your first deadline",
                $"{debut.Title} debuts in {magazine.Name}. The first issue closes {contract.FirstIssueClose:d MMM yyyy}.\n" +
                $"{ready} of {target} chapters ready. Editors want chapters done early, so one slow week never misses an issue.\n" +
                "Page fees only arrive after chapters publish. Watch the progress bar and due dates in Production.", "production", debut.Id);

        const string Buffer = "For extra safety, raise the finished chapter buffer to 4 in Studio management, Team, Production limits.";
        var opening = $"{ready} of {target} chapters are ready for the {contract.FirstIssueClose:d MMM} debut. You have free time until then.";
        static string Others(string chosen) => new[] { "an early hire", "a convention", "a short doujin", "a part-time job" }
            .Where(o => o != chosen).ToArray() is var o ? $"Other ideas: {o[0]}, {o[1]} or {o[2]}." : "";

        if (!state.ControlledStaff.Any(p => p.Id != state.ProtagonistPersonId))
        {
            var wage = CheapestWage(state);
            var runway = state.HiringRunway(wage ?? StudioRules.MinimumMonthlySalary);
            // Wages fall due before the first page fee, so cash alone must carry the studio until the debut.
            var monthsToDebut = Math.Max(0, (contract.FirstIssueClose - state.Clock.Now).TotalDays / 30);
            bool cashCarries = runway.Cash >= runway.MonthlyCosts * Math.Max(StudioRules.SafeRunwayMonths, monthsToDebut + 1);
            if (runway.Safe && cashCarries && state.WorkplaceWithFreeDesk is null)
                return new("debut-wait-desk", "Room for an early assistant?", string.Join('\n', opening,
                    $"Suggestion: hire early. Funds cover {RunwayText(runway)} with an assistant, but every desk is taken. Add one in Furniture.",
                    Others("an early hire"), Buffer), "furniture", debut.Id);
            if (runway.Safe && cashCarries)
                return new("debut-wait-hire", "Hire before the debut?", string.Join('\n', opening,
                    $"Suggestion: hire early. Funds cover {RunwayText(runway)} with an assistant, who can settle in before deadlines start.",
                    SearchText(state, wage is not null), Others("an early hire"), Buffer), "recruitment", debut.Id);
        }

        var stocked = owned.Select(s => (Series: s, Copies: s.Volumes.Where(v => v.IsDoujin && v.BusinessId == state.ControlledBusinessId).Sum(v => state.Stock(v.Id))))
            .Where(x => x.Copies > 0).OrderByDescending(x => x.Copies).ToArray();
        var stock = stocked.Sum(x => x.Copies);
        bool booked = state.Bookings.Any(b => b.BusinessId == state.ControlledBusinessId && !b.Settled && !b.Cancelled);
        if (stock > 0 && !booked)
        {
            var major = state.NextConvention(2);
            var (date, name, fee) = major < contract.FirstIssueClose
                ? (major, major.Month == 8 ? "summer convention" : "winter convention", 8000)
                : (state.NextConvention(1), "regional event", 5000);
            // Only suggest a booth the business can pay for now, booth and return travel included.
            var home = state.Locations.FirstOrDefault(l => l.Id == state.Protagonist.Employment?.LocationId)?.District;
            var travel = home is null ? 0 : TokyoProperties.Travel(home, fee == 8000 ? "Ariake" : "Toshima").Fare * 2 * (fee == 8000 ? 2 : 1);
            if (home is not null && state.AvailableBusinessCash >= fee + travel)
            {
                var opens = date > state.Clock.Now.Date.AddDays(28) ? $" Booking opens {date.AddDays(-28):d MMM}." : "";
                return new("debut-wait-convention", "A convention before the debut?", string.Join('\n', opening,
                    $"Suggestion: sell your {stock:N0} unsold doujin copies at the {name} on {date:d MMM} (¥{fee:N0} booth).{opens}",
                    "Book it on the Conventions page.", Others("a convention"), Buffer), "conventions", stocked[0].Series.Id);
            }
        }

        // One side doujin per wait: once one is started after signing, finishing it does not bring the suggestion back.
        var signed = state.Events.LastOrDefault(e => e.Type == EventType.OfferAccepted && e.SeriesId == debut.Id)?.Time ?? DateTime.MaxValue;
        bool sideDoujin = owned.Any(s => s.Id != debut.Id && (s.StartDate >= signed ||
            s.Status == SeriesStatus.Active && s.Publishing == PublishingStatus.Unpublished &&
            (s.Chapters.Count == 0 || s.Chapters.Any(c => c.Status != ChapterStatus.Complete))));
        if (!sideDoujin)
            return new("debut-wait-doujin", "A short doujin meanwhile?", string.Join('\n', opening,
                "Suggestion: draw a short doujin or one-shot in New doujin. Magazine chapters always come first, so your debut stays safe.",
                Others("a short doujin"), Buffer), "create", debut.Id);

        return new("debut-wait-job", "Some extra income?", string.Join('\n', opening,
            state.Protagonist.OutsideJob == OutsideJob.None
                ? "Suggestion: a part-time job earns personal money while you wait. Set it under Part-time work in Finances. Stop it if chapters slip."
                : "Your part-time job keeps personal money coming in while you wait. Stop it in Finances if chapters slip.",
            Others("a part-time job"), Buffer), "finances", debut.Id);
    }

    private static string RunwayText(HiringRunway runway) => runway.Months is not { } months ? "every month" :
        months >= 12 ? "over a year" : $"about {Math.Floor(months * 10) / 10:0.#} months";

    private static string SearchText(GameState state, bool candidates) =>
        candidates ? "Compare candidates in Staff, then hire at a free desk." :
        state.Recruitment is { } search ? $"Your candidate search reports on {search.ReadyAt:d MMM yyyy}." :
        state.NextRecruitmentAt is { } next ? $"The next candidate search opens on {next:d MMM yyyy}." :
        "Start a candidate search in Staff to meet applicants.";

    private static long? CheapestWage(GameState state) => state.Candidates
        .Where(c => !c.Recruited && c.ExpiresAt > state.Clock.Now && (c.IntroductionBusinessId is null || c.IntroductionBusinessId == state.ControlledBusinessId))
        .Select(c => (long?)Math.Max(StudioRules.MinimumMonthlySalary, c.ExpectedSalary)).Min();

    private static GameEvent? LastRejection(GameState state, Series series) =>
        state.Events.LastOrDefault(e => e.Type == EventType.PitchRejected && e.SeriesId == series.Id);

    private static bool InContest(GameState state, Series series) =>
        state.Progression.Manuscripts.Any(m => m.SeriesId == series.Id && !m.Released);

    public static void Observe(GameState state, GuidancePreferences preferences)
    {
        // Presentation-only and idempotent; history remains authoritative and no popup backlog is created.
        if (LegacyRoutes.Contains(preferences.Route) || !Routes.Contains(preferences.Route)) preferences.Route = "career";
        // The contest detour ends once its manuscript is entered; the result arrives as its own notice.
        if (preferences.Route == "contest" && state.Progression.Awards.LastOrDefault(a => a.ResolvedAt is null &&
                state.Series.Any(s => s.Id == a.SeriesId && s.BusinessId == state.ControlledBusinessId)) is { } entry &&
            state.Progression.Manuscripts.Any(m => m.Id == entry.ManuscriptId && !m.Released) &&
            // Once per entry: choosing the contest route again during judging is the player's call (final review).
            !preferences.Thread.Any(m => m.Step == "contest-entered" && m.Time >= entry.SubmittedAt))
        {
            preferences.Route = "career";
            Append(preferences, new() { Step = "contest-entered", Time = state.Clock.Now, Texts =
                [$"Your manuscript is entered! Results come in {entry.ResolvesAt:MMMM yyyy}.", "Back to your career path meanwhile. I'll tell you when judging is done."] });
        }
        var titles = state.Series.Where(s => WasCreator(state,s)).ToArray();
        if (titles.Length > 0) preferences.Completed.Add("create");
        if (titles.Any(s => s.Chapters.Any(c => c.Status == ChapterStatus.Complete))) preferences.Completed.Add("produce");
        if (titles.Any(s => s.Volumes.Any(v => state.PrintRuns.Any(r => r.VolumeId == v.Id)))) preferences.Completed.Add("print");
        if (titles.Any(s => s.Volumes.Any(v => v.CopiesSold > 0)) || state.Career.Sales.Any(x => titles.Any(s => s.Id == x.Series) && x.Physical + x.Digital + x.Overseas > 0))
            preferences.Completed.Add("first-sale");
        var step = Evaluate(state, preferences);
        var last = preferences.Thread.LastOrDefault(m => m.Step is not ("notice" or ArrearsStep or QuietSpeedStep));
        if (last is null || last.Step != step.Id)
            Append(preferences, new() { Step = step.Id, Time = state.Clock.Now, Texts = step.Texts.ToList() });
    }

    public const string QuietSpeedStep = "quiet-speed";
    public const string QuietSpeedText = "Nothing needs you right now. Try 32× to skip ahead; I'll stop you if anything comes up.";

    /// <summary>Events that stop a 32x day and return to the slower speed (Q27). Routine recaps do not.</summary>
    public static readonly IReadOnlySet<EventType> FastSpeedStops = new HashSet<EventType>
    {
        EventType.IndustryDecision, EventType.SerializationOffered, EventType.PitchRejected, EventType.EditorRedoRequested,
        EventType.CancellationWarning, EventType.SeriesCancelled, EventType.DeadlineMissed, EventType.IssueMissed,
        EventType.ChapterAtRisk, EventType.WageArrears,
    };

    /// <summary>
    /// Helper-Chan's one-time 32x introduction (Q28): after the first sale, at the end of a working day
    /// since <paramref name="dayStart"/> with no stop event and no unread text. Marks the step complete when sent.
    /// </summary>
    public static bool OfferQuietSpeed(GameState state, GuidancePreferences preferences, DateTime dayStart)
    {
        if (!preferences.Completed.Contains("first-sale") || preferences.Completed.Contains(QuietSpeedStep) || Unread(preferences) > 0) return false;
        for (int i = state.Events.Count - 1; i >= 0 && state.Events[i].Time >= dayStart; i--)
            if (FastSpeedStops.Contains(state.Events[i].Type)) return false;
        preferences.Completed.Add(QuietSpeedStep);
        Append(preferences, new() { Step = QuietSpeedStep, Time = state.Clock.Now, Texts = [QuietSpeedText] });
        return true;
    }

    public const string ArrearsStep = "wage-arrears";

    /// <summary>Helper-Chan's missed-payday texts: amount, the staff member's deadlines and the ways to recover.</summary>
    public static string ArrearsText(GameState state, GameEvent arrears)
    {
        var name = arrears.PersonId is { } id ? state.FindPerson(id)?.Name ?? "your assistant" : "your assistant";
        var owed = state.WageArrears;
        var since = arrears.Time.Date;
        var cover = owed > 0 && state.PersonalMoney >= owed
            ? $"Your savings hold ¥{state.PersonalMoney:N0}. Tap Cover from savings and I'll move exactly ¥{owed:N0} into the business."
            : "Your savings can't cover it. Earn more from sales, let someone go in Staff, or borrow in Finances.";
        return string.Join('\n',
            $"Payday missed! The business is ¥{owed:N0} short on wages for {name}.",
            $"If it stays unpaid, {name} warns on {since.AddDays(14):d MMM}, stops work on {since.AddDays(21):d MMM} and gives notice on {since.AddDays(28):d MMM}.",
            cover);
    }

    /// <summary>Adds the missed-payday texts once per arrears event for the business the player runs.</summary>
    public static bool ReportArrears(GameState state, GuidancePreferences preferences, GameEvent arrears)
    {
        if (arrears.Type != EventType.WageArrears || arrears.PersonId is not { } person ||
            !state.WageObligations.Any(o => o.PersonId == person && o.BusinessId == state.ControlledBusinessId && o.Remaining > 0)) return false;
        if (preferences.Thread.Any(m => m.Step == ArrearsStep && m.Time == arrears.Time)) return false;
        Append(preferences, new() { Step = ArrearsStep, Time = arrears.Time, Texts = ArrearsText(state, arrears).Split('\n').ToList() });
        return true;
    }

    /// <summary>Adds one of Helper-Chan's notices to the thread, for example from the "Show me" reply.</summary>
    public static void Say(GuidancePreferences preferences, DateTime time, string text) =>
        Append(preferences, new() { Step = "notice", Time = time, Texts = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() });

    public static int Unread(GuidancePreferences preferences) => preferences.Thread.Count(m => !m.Read);

    public static void MarkRead(GuidancePreferences preferences)
    {
        foreach (var message in preferences.Thread) message.Read = true;
    }

    private static void Append(GuidancePreferences preferences, GuidanceMessage message)
    {
        preferences.Thread.Add(message);
        if (preferences.Thread.Count > ThreadLimit) preferences.Thread.RemoveRange(0, preferences.Thread.Count - ThreadLimit);
    }

    private static bool WasCreator(GameState state,Series s) => s.LeadPersonId==state.ProtagonistPersonId || s.RightsLeadPersonId==state.ProtagonistPersonId ||
        s.Chapters.Any(c=>c.CreatorPersonId==state.ProtagonistPersonId) || s.Volumes.Any(v=>v.CreatorShares.ContainsKey(state.ProtagonistPersonId));
}
