using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    private void ApplyPitch(PitchSeriesCommand command)
    {
        var series = RequireSeries(command.SeriesId);
        var magazine = PublisherCatalog.Magazines.FirstOrDefault(m => m.Id == command.MagazineId)
            ?? throw new InvalidCommandException("Choose an existing magazine.");
        if (series.Publishing != PublishingStatus.Unpublished || series.Status != SeriesStatus.Active)
            throw new InvalidCommandException("Only an active unpublished series can pitch.");
        if (series.PitchCooldowns.TryGetValue(magazine.Id, out var until) && until > Clock.Now)
            throw new InvalidCommandException($"This magazine will consider another pitch after {until:d MMM yyyy}.");
        var open = series.Chapters.Where(c => c.Status != ChapterStatus.Complete).ToArray();
        if (open.Length > 1 || open.Any(c => c.Stages.Any(w => w.Status != StageStatus.NotStarted ||
            w.HoursDone != 0 || w.OvertimeHours != 0 || w.HoursByPerson.Count != 0)))
            throw new InvalidCommandException("Finish the current chapter before pitching.");
        foreach (var chapter in open) DropChapter(series, chapter);
        series.Publishing = PublishingStatus.Pitching;
        var sample = CreateNextChapter(series, 31, IssueSchedule.FirstCloseAtOrAfter(magazine, Clock.Now.AddDays(14)));
        sample.IsOneShot = true;
        sample.PitchMagazineId = magazine.Id;
        sample.EditorMagazineId = magazine.Id;
        Emit(EventType.PitchSubmitted, $"{series.Title}: one-shot for {magazine.Name}.", series.Id, sample.Number,
            context: new(MagazineId: magazine.Id));
    }

    private void SubmitNameIfReady(Chapter chapter)
    {
        if (chapter.EditorMagazineId is null || chapter.Editor is EditorStatus.AwaitingReview or EditorStatus.Approved ||
            !chapter.StageWork(Stage.Name).IsDone) return;
        chapter.Editor = EditorStatus.AwaitingReview;
        chapter.EditorDecisionAt = Clock.Now.AddHours(EditorRules.Delay(PublisherCatalog.Get(chapter.EditorMagazineId).Tier));
    }

    internal void EditorStep()
    {
        foreach (var chapter in Series.SelectMany(s => s.Chapters).Where(c => c.Editor == EditorStatus.AwaitingReview &&
                     c.EditorDecisionAt <= Clock.Now).OrderBy(c => c.Id).ToArray())
        {
            var series = SeriesOf(chapter);
            var magazine = PublisherCatalog.Get(chapter.EditorMagazineId!);
            var name = chapter.StageWork(Stage.Name);
            var quality = name.Contribution / QualityRules.Weight(Stage.Name);
            var approved = chapter.RedoCount >= 2 || Rng.NextDouble() < EditorRules.Chance(magazine.Tier, quality, EffectiveReputation);
            chapter.EditorDecisionAt = null;
            if (approved)
            {
                chapter.Editor = EditorStatus.Approved;
                Emit(EventType.EditorApproved, $"{series.Title} ch.{chapter.Number}: Name approved.", series.Id, chapter.Number,
                    context: new(MagazineId: magazine.Id));
                CompleteChapterIfDone(chapter);
            }
            else
            {
                chapter.Editor = EditorStatus.RedoRequested;
                chapter.RedoCount++;
                name.Status = StageStatus.NotStarted;
                name.HoursDone = name.Contribution = name.OvertimeHours = 0;
                if (name.AssignedTo is { } id) ChangeReputation(FindPerson(id)!, -.5);
                ChangeTrackRecord(-.25);
                Emit(EventType.EditorRedoRequested, $"{series.Title} ch.{chapter.Number}: revise Name (quality {quality:F0}).", series.Id, chapter.Number,
                    context: new(MagazineId: magazine.Id));
            }
            RunPlanner();
        }
    }

    private void PitchStep(IReadOnlyList<IssueCloseContext> closes)
    {
        foreach (var series in Series.OrderBy(s => s.Id).ToArray())
        {
            if (series.PendingOffer is { } offer && Clock.Now >= offer.ExpiresAt)
            {
                ReleaseSample(series);
                Emit(EventType.OfferExpired, $"{series.Title}: serialization offer expired.", series.Id, context: new(MagazineId: offer.MagazineId));
            }
            if (series.Publishing != PublishingStatus.Pitching) continue;
            var sample = series.Chapters.Single(c => c.IsOneShot && !c.PitchResolved);
            if (sample.Status != ChapterStatus.Complete || sample.Editor != EditorStatus.Approved || sample.DueDate > Clock.Now ||
                !closes.Any(c => c.MagazineId == sample.PitchMagazineId)) continue;
            var magazine = PublisherCatalog.Get(sample.PitchMagazineId!);
            var genre = TrendRules.Normalise(series.Genre, TrendCatalog);
            var affinity = series.IsIconic ? 1 : magazine.Affinity(genre);
            var trend = series.IsIconic ? 1 : GenrePopularity(genre);
            var quality = sample.Quality!.Value;
            var reputation = EffectiveReputation;
            sample.PitchResolved = true;
            if (Rng.NextDouble() < PitchRules.Chance(magazine.Tier, quality, reputation, affinity, trend))
            {
                var first = IssueSchedule.AddIssues(magazine, Clock.Now, 4);
                series.PendingOffer = new(magazine.Id, ReputationRules.Fee(magazine.FeePerPageMin, magazine.FeePerPageMax,
                    reputation, Economy.PriceIndex(TrendCatalog, Clock.Now)), first, first);
                series.Publishing = PublishingStatus.Offered;
                Emit(EventType.SerializationOffered, $"{magazine.Name} offered to serialize {series.Title}.", series.Id, sample.Number,
                    context: new(MagazineId: magazine.Id, Amount: series.PendingOffer.FeePerPage));
            }
            else
            {
                series.Publishing = PublishingStatus.Unpublished;
                sample.DoujinEligible = true;
                series.PitchCooldowns[magazine.Id] = Clock.Now.AddDays(26 * 7);
                if (quality >= 70) ChangeTrackRecord(.25);
                Emit(EventType.PitchRejected, $"{series.Title}: pitch rejected; weakest factor: {PitchRules.WeakestFactor(quality, reputation, affinity, trend)}.",
                    series.Id, sample.Number, context: new(MagazineId: magazine.Id));
                CollectDoujin(series);
            }
            RunPlanner();
        }
    }

    private SerializationOffer RequireOffer(Series series)
    {
        if (series.Publishing != PublishingStatus.Offered || series.PendingOffer is not { } offer || Clock.Now >= offer.ExpiresAt)
            throw new InvalidCommandException("There is no live serialization offer.");
        return offer;
    }
    private void ApplyAcceptOffer(AcceptOfferCommand command)
    {
        var series = RequireSeries(command.SeriesId);
        var offer = RequireOffer(series);
        var magazine = PublisherCatalog.Get(offer.MagazineId);
        series.Contract = new(AllocateId(), magazine.Id, offer.FeePerPage, Clock.Now, offer.FirstIssueClose);
        series.PendingOffer = null;
        series.Publishing = PublishingStatus.Serialized;
        series.DoujinCadence = series.Cadence;
        series.Cadence = magazine.Cadence;
        ClearCancellation(series);
        series.LastRank = null;
        series.NextChapterDueOverride = offer.FirstIssueClose;
        var open = series.Chapters.FirstOrDefault(c => c.Status != ChapterStatus.Complete);
        if (open is not null)
        {
            open.DueDate = offer.FirstIssueClose;
            series.NextChapterDueOverride = null;
            open.DoujinEligible = false;
            open.EditorMagazineId = magazine.Id;
            if (open.StageWork(Stage.Pencils).HoursDone > 0 || open.StageWork(Stage.Pencils).Status == StageStatus.Complete)
                open.Editor = EditorStatus.Approved;
            else SubmitNameIfReady(open);
        }
        Emit(EventType.OfferAccepted, $"{series.Title} joins {magazine.Name}.", series.Id, context: new(MagazineId: magazine.Id));
    }
    private void ApplyDeclineOffer(DeclineOfferCommand command)
    {
        var series = RequireSeries(command.SeriesId);
        var offer = RequireOffer(series);
        ReleaseSample(series);
        Emit(EventType.OfferDeclined, $"{series.Title}: offer declined.", series.Id, context: new(MagazineId: offer.MagazineId));
    }
    private void ReleaseSample(Series series)
    {
        var sample = series.Chapters.Last(c => c.IsOneShot);
        sample.PitchResolved = true;
        sample.DoujinEligible = true;
        series.PendingOffer = null;
        series.Publishing = PublishingStatus.Unpublished;
        CollectDoujin(series);
        RunPlanner();
    }
    private void DropChapter(Series series, Chapter chapter)
    {
        foreach (var person in People)
        {
            person.Queue.RemoveAll(r => r.ChapterId == chapter.Id);
            person.Pins.RemoveAll(r => r.ChapterId == chapter.Id);
            person.ManualOrder?.RemoveAll(r => r.ChapterId == chapter.Id);
            if (person.CurrentTask?.ChapterId == chapter.Id) person.CurrentTask = null;
        }
        series.Chapters.Remove(chapter);
    }
}
