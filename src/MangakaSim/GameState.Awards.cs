using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    public static DateTime ContestDeadline(DateTime now)
    {
        var march = new DateTime(now.Year, 3, 31, 23, 0, 0);
        var september = new DateTime(now.Year, 9, 30, 23, 0, 0);
        return now <= march ? march : now <= september ? september : march.AddYears(1);
    }
    public static double JuryScore(double quality, double originality, double fit, double variation) =>
        quality * .60 + originality * .25 + fit * .15 + variation;
    private void ApplyRecognition(RecognitionCommand c)
    {
        if (!Enum.IsDefined(c.Action)) throw new InvalidCommandException("Choose a contest action.");
        if (c.Action == RecognitionAction.CreateManuscript)
        {
            if (string.IsNullOrWhiteSpace(c.Text) || c.Text.Length > 120 || c.Category is not ("story" or "comedy"))
                throw new InvalidCommandException("Give the manuscript a title and choose Story or Comedy.");
            var series = CreateSeries(c.Text.Trim(), c.Category == "comedy" ? "comedy" : "drama", Cadence.Monthly, 32);
            var chapter = CreateNextChapter(series, 32, ContestDeadline(Clock.Now));
            chapter.DoujinEligible = false;
            Progression.Manuscripts.Add(new() { Id = AllocateId(), SeriesId = series.Id, ChapterId = chapter.Id,
                CreatorId = series.LeadPersonId, Category = c.Category, Originality = 40 + Progression.AwardsRng.NextDouble() * 35 });
            return;
        }
        var manuscript = Progression.Manuscripts.FirstOrDefault(m => m.Id == c.Target);
        if (manuscript is null) throw new InvalidCommandException("Select a contest manuscript.");
        var s = RequireSeries(manuscript.SeriesId);
        var current = FindChapter(manuscript.ChapterId)!;
        if (manuscript.Released) throw new InvalidCommandException("This manuscript has been released from contest use.");
        if (Progression.Awards.Any(a => a.ManuscriptId == manuscript.Id && a.ResolvedAt is null))
            throw new InvalidCommandException("Wait for the active submission's result first.");
        if (c.Action == RecognitionAction.ReleaseManuscript)
        {
            manuscript.Released = true;
            current.DoujinEligible = true;
            if (current.Status == ChapterStatus.Complete && !s.Volumes.Any(v => v.ChapterIds.Contains(current.Id)))
                CreateVolume(s, [current], true, Clock.Now);
            return;
        }
        if (current.Status != ChapterStatus.Complete) throw new InvalidCommandException("Finish the manuscript first.");
        if (c.Action == RecognitionAction.Revise)
        {
            if (Progression.Awards.Any(a => a.ManuscriptId == manuscript.Id && a.Prize > 0))
                throw new InvalidCommandException("Prize-winning manuscripts cannot enter these newcomer contests again.");
            var revised = CreateNextChapter(s, current.Pages, ContestDeadline(Clock.Now));
            revised.DoujinEligible = false;
            manuscript.ChapterId = revised.Id;
            manuscript.Revision++;
            // A completed rewrite is work, not a free roll at submission time.
            manuscript.Originality = Math.Min(100, manuscript.Originality + 2);
            return;
        }
        if (current.PublishedAt is not null || s.Volumes.Any(v => v.ChapterIds.Contains(current.Id)) ||
            Progression.Awards.Any(a => a.ManuscriptId == manuscript.Id && (a.Prize > 0 || a.Revision == manuscript.Revision)))
            throw new InvalidCommandException("Enter an unpublished, revised manuscript that has not already won a prize.");
        var deadline = ContestDeadline(Clock.Now);
        var edition = $"{manuscript.Category}:{deadline:yyyy-MM}";
        if (Progression.Awards.Any(a => a.SeriesId == s.Id && a.Award == edition))
            throw new InvalidCommandException("This title has already entered this edition. Wait for the next deadline.");
        var field = Progression.Awards.FirstOrDefault(a => a.Award == edition)?.Competitors.ToList() ??
            Enumerable.Range(0, 24).Select(_ => 45 + Progression.AwardsRng.NextDouble() * 45).ToList();
        Progression.Awards.Add(new()
        {
            Id = AllocateId(), ManuscriptId = manuscript.Id, SeriesId = s.Id, CreatorId = manuscript.CreatorId,
            Revision = manuscript.Revision, Award = edition, SubmittedAt = Clock.Now,
            ResolvesAt = deadline.Date.AddMonths(2).AddHours(8), Quality = current.Quality!.Value,
            Originality = Math.Min(100, .5 * manuscript.Originality + .5 * current.StageWork(Stage.Name).Contribution / QualityRules.Weight(Stage.Name)),
            Fit = manuscript.Category == "comedy" ? (s.Genre == "comedy" ? 95 : 45) : 85,
            Jury = Progression.AwardsRng.NextDouble() * 10 - 5, Competitors = field
        });
        Emit(EventType.AwardNomination, $"{s.Title} entered the {manuscript.Category} newcomer contest. Results in {deadline.Date.AddMonths(2):MMMM yyyy}.", s.Id);
    }
    public void RegisterExistingManuscript(int chapterId) => Apply(new AdoptManuscriptCommand(chapterId));
    private void AdoptManuscript(int chapterId)
    {
        var chapter = FindChapter(chapterId) ?? throw new InvalidCommandException("Select a finished unpublished one-shot.");
        var series = RequireSeries(SeriesOf(chapter).Id);
        if (series.Publishing != PublishingStatus.Unpublished || chapter.Status != ChapterStatus.Complete || chapter.PublishedAt is not null ||
            chapter.Pages is < 16 or > 64 || series.Volumes.Any(v => v.ChapterIds.Contains(chapter.Id)) ||
            Progression.Manuscripts.Any(m => m.SeriesId == series.Id) || series.Chapters.Any(ch => ch.Status != ChapterStatus.Complete))
            throw new InvalidCommandException("Finish all current work and choose an uncollected 16–64 page unpublished manuscript.");
        chapter.DoujinEligible = false;
        Progression.Manuscripts.Add(new() { Id = AllocateId(), SeriesId = series.Id, ChapterId = chapter.Id,
            CreatorId = chapter.CreatorPersonId ?? series.LeadPersonId, Category = series.Genre == "comedy" ? "comedy" : "story",
            Originality = Math.Clamp(chapter.StageWork(Stage.Name).Contribution / QualityRules.Weight(Stage.Name), 0, 100) });
    }
    private void ResolveAwards()
    {
        if (Clock.Now.Month == 1 && Clock.Now.Day == 1)
        {
            var edition = $"annual:{Clock.Now.Year - 1}";
            var eligible = Series.Where(s => s.Chapters.Any(c => c.PublishedAt?.Year == Clock.Now.Year - 1) ||
                s.Volumes.Any(v => v.ReleasedAt?.Year == Clock.Now.Year - 1)).OrderBy(s => s.Id).ToArray();
            var field = eligible.Length == 0 ? new List<double>() : Markets.SelectMany(m => m.Fillers).Take(24)
                .Select(f => Math.Clamp(f.Popularity * .4 + 35 + Progression.AwardsRng.NextDouble() * 25, 0, 100)).ToList();
            foreach (var s in eligible)
            {
                if (Progression.Awards.Any(a => a.SeriesId == s.Id && a.Award == edition)) continue;
                var quality = s.Chapters.Where(c => c.PublishedAt?.Year == Clock.Now.Year - 1 ||
                    s.Volumes.Any(v => v.ReleasedAt?.Year == Clock.Now.Year - 1 && v.ChapterIds.Contains(c.Id)))
                    .Select(c => c.Quality ?? 0).DefaultIfEmpty(0).Average();
                if (quality < 65) continue;
                Progression.Awards.Add(new() { Id = AllocateId(), SeriesId = s.Id, CreatorId = s.RightsLeadPersonId,
                    Award = edition, SubmittedAt = Clock.Now, ResolvesAt = Clock.Now.Date.AddDays(14).AddHours(8),
                    Quality = quality, Originality = Math.Min(100, quality * .7 + s.CulturalImpact * .3), Fit = 85,
                    Jury = Progression.AwardsRng.NextDouble() * 10 - 5, Competitors = field.ToList() });
                Emit(EventType.AwardNomination, $"{s.Title} is shortlisted for the annual Manga Craft Award ({Clock.Now.Year - 1}, simulated).", s.Id);
                Progression.Effects.Add(new(Progression.Awards[^1].Id, s.Id, Clock.Now, Clock.Now.AddDays(42), .04));
            }
        }
        foreach (var a in Progression.Awards.Where(a => a.ResolvedAt is null && a.ResolvesAt <= Clock.Now).OrderBy(a => a.Id).ToArray())
        {
            var score = JuryScore(a.Quality, a.Originality, a.Fit, a.Jury);
            var other = Progression.Awards.Where(b => b.Award == a.Award && b.Id != a.Id)
                .Count(b => JuryScore(b.Quality, b.Originality, b.Fit, b.Jury) > score ||
                    JuryScore(b.Quality, b.Originality, b.Fit, b.Jury) == score && b.Id < a.Id);
            a.Placement = 1 + a.Competitors.Count(v => v > score) + other;
            a.ResolvedAt = Clock.Now;
            bool annual = a.ManuscriptId == 0;
            a.Prize = a.Placement == 1 && score >= 80 ? (annual ? 500000 : 300000) :
                !annual && a.Placement <= 3 && score >= 72 ? 100000 : !annual && a.Placement <= 6 && score >= 65 ? 30000 : 0;
            a.Result = a.Prize >= 300000 ? "Winner" : a.Prize >= 100000 ? "Runner-up" : a.Prize > 0 ? "Honorable mention" : a.Placement <= 8 ? "Shortlisted" : "Not placed";
            a.Feedback = (a.Quality >= 75 ? "Strong craft. " : "Develop the finished pages and clarity. ") +
                (a.Originality >= 75 ? "A distinctive voice. " : "Look for a more distinctive approach in the Name. ") +
                (a.Fit >= 80 ? "Good category fit. " : "This category was a difficult fit. ") +
                (a.Placement > 3 ? "The field was competitive; a stronger revision can still meet a different jury." : "The jury responded to the work's strengths.") +
                (a.Placement <= 8 ? " Editorial note: preserve the strongest scenes while checking transitions and the ending." : "");
            var series = FindSeries(a.SeriesId)!;
            if (a.Prize > 0)
            {
                AccountPost(FindPerson(a.CreatorId)!.PersonalAccount, a.Prize, $"{a.Award}: {a.Result}", AccountEntryKind.AwardPrize, series.Id);
                ChangeReputation(FindPerson(a.CreatorId)!, a.Prize >= 300000 ? 3 : 1);
                var earlierWins = Progression.Awards.Count(b => b.SeriesId == a.SeriesId && b.Id != a.Id && b.Prize > 0 && b.ResolvedAt <= Clock.Now);
                RecognitionImpact(series, (a.Prize >= 300000 ? 3d : 1) / (1 + earlierWins));
                MarkMilestone(annual ? "annual_award" : "contest_placement", series.Id, $"{series.Title}: {a.Result} in {a.Award}.");
                Progression.Effects.Add(new(a.Id, series.Id, Clock.Now, Clock.Now.AddDays(annual ? 126 : 42), (annual ? .15 : .04) / (1 + earlierWins)));
                if (!annual)
                {
                    ChangeTrackRecord(2, series.BusinessId);
                    Emit(EventType.IndustryNews, $"Editors noticed {series.Title}. Release the manuscript when ready and use the normal publisher pitch route; serialization is not guaranteed.", series.Id);
                }
            }
            Emit(EventType.AwardResult, $"{series.Title}: {a.Result}. {(a.Prize > 0 ? $"Personal prize ¥{a.Prize:N0}. " : "")}{a.Feedback}", series.Id, personId: a.CreatorId);
        }
    }
}
