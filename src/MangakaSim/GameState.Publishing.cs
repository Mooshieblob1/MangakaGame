namespace MangakaSim;

public partial class GameState
{
    // ---------------------------------------------------------------- editor gate

    /// <summary>Only chapters of a Serialized series and pitched one-shots see an editor.</summary>
    internal bool RequiresEditor(Chapter chapter) =>
        chapter.IsOneShot ? chapter.PitchMagazineId is not null : SeriesOf(chapter).IsSerialized;

    /// <summary>The magazine reviewing a chapter, or null for doujin work.</summary>
    internal Magazine? ReviewMagazine(Chapter chapter)
    {
        if (chapter.IsOneShot) return chapter.PitchMagazineId is { } id ? Publishers.Find(id) : null;
        var series = SeriesOf(chapter);
        return series.Contract is { } contract ? Publishers.Find(contract.MagazineId) : null;
    }

    /// <summary>Name has just completed or been skipped: enter review when an editor is involved.</summary>
    private void SubmitForReviewIfRequired(Chapter chapter)
    {
        if (!RequiresEditor(chapter) || ReviewMagazine(chapter) is not { } magazine) return;
        if (chapter.Editor == EditorStatus.Approved) return;
        chapter.Editor = EditorStatus.AwaitingReview;
        chapter.EditorDecisionAt = Clock.Now.AddHours(EditorRules.ReviewHours(magazine.Tier));
    }

    /// <summary>Resolves every review whose decision time has come.</summary>
    internal void EditorStep()
    {
        foreach (var series in Series)
        {
            foreach (var chapter in series.Chapters)
            {
                if (chapter.Editor != EditorStatus.AwaitingReview || chapter.EditorDecisionAt is not { } due || Clock.Now < due) continue;
                ResolveReview(series, chapter);
            }
        }
    }

    private void ResolveReview(Series series, Chapter chapter)
    {
        var magazine = ReviewMagazine(chapter);
        var name = chapter.StageWork(Stage.Name);
        var nameQuality = QualityRules.NameQuality(name.Contribution);
        bool approved;
        if (magazine is null || chapter.RedoCount >= EditorRules.AlwaysApproveOnSubmission - 1)
        {
            approved = true;
        }
        else
        {
            var threshold = EditorRules.Threshold(magazine.Tier, EffectiveReputation);
            approved = Rng.NextDouble() < EditorRules.ApproveChance(nameQuality, threshold);
        }

        var what = chapter.IsOneShot ? "one-shot" : $"ch.{chapter.Number}";
        if (approved)
        {
            chapter.Editor = EditorStatus.Approved;
            Emit(EventType.EditorApproved,
                $"The editor approved the name for {series.Title} {what} (name quality {nameQuality:0}).",
                new EventContext(SeriesId: series.Id, ChapterNumber: chapter.Number, MagazineId: magazine?.Id));
        }
        else
        {
            chapter.Editor = EditorStatus.RedoRequested;
            chapter.RedoCount++;
            name.HoursDone = 0;
            name.Status = StageStatus.NotStarted;
            name.Contribution = 0;
            name.OvertimeHours = 0;
            if (chapter.Status == ChapterStatus.Complete)
            {
                // Only reachable when later stages were skipped during review; the chapter reopens.
                chapter.Status = ChapterStatus.InProgress;
                chapter.CompletedAt = null;
                chapter.Quality = null;
            }
            Emit(EventType.EditorRedoRequested,
                $"The editor sent the name for {series.Title} {what} back (name quality {nameQuality:0}, redo {chapter.RedoCount}).",
                new EventContext(SeriesId: series.Id, ChapterNumber: chapter.Number, PersonId: name.AssignedTo, MagazineId: magazine?.Id));
            if (name.AssignedTo is { } personId && FindPerson(personId) is { } person)
                AdjustReputation(person, ReputationRules.PersonRedo);
            AdjustTrackRecord(ReputationRules.EditorRedo);
        }
        RunPlanner();
        RiskStep();
    }

    // ---------------------------------------------------------------- pitching

    private void ApplyPitchSeries(PitchSeriesCommand c)
    {
        var series = RequireSeries(c.SeriesId);
        if (c.MagazineId is null) throw new InvalidCommandException("Magazine id must not be null.");
        var magazine = Publishers.Find(c.MagazineId) ?? throw new InvalidCommandException($"No magazine with id '{c.MagazineId}'.");
        if (series.Publishing != PublishingStatus.Unpublished)
            throw new InvalidCommandException($"Series '{series.Title}' is {series.Publishing}; only an unpublished series can pitch.");
        if (series.Status != SeriesStatus.Active)
            throw new InvalidCommandException($"Series '{series.Title}' is {series.Status}; only an active series can pitch.");
        if (series.PitchCooldowns.TryGetValue(magazine.Id, out var until) && until > Clock.Now)
            throw new InvalidCommandException($"{magazine.Name} will not look at '{series.Title}' again before {until:d MMM yyyy}.");
        var open = series.OpenChapter;
        if (open is not null && !open.IsUntouched)
            throw new InvalidCommandException($"Finish the current chapter of '{series.Title}' first; a one-shot needs a clean desk.");

        if (open is not null) series.Chapters.Remove(open);
        var oneShot = CreateNextChapter(series, magazine, dueOverride: null);
        series.Publishing = PublishingStatus.Pitching;
        Emit(EventType.PitchSubmitted,
            $"{series.Title} pitches a one-shot to {magazine.Name}; it is due {oneShot.DueDate:ddd d MMM HH:mm}.",
            new EventContext(SeriesId: series.Id, ChapterNumber: oneShot.Number, MagazineId: magazine.Id));
    }

    /// <summary>The one-shot a Pitching series is waiting on.</summary>
    internal Chapter? PendingOneShot(Series series) =>
        series.Publishing == PublishingStatus.Pitching
            ? series.Chapters.LastOrDefault(c => c.IsOneShot && c.PitchMagazineId is not null)
            : null;

    /// <summary>Resolves finished one-shots at their magazine's close and expires unanswered offers.</summary>
    internal void PitchStep()
    {
        foreach (var series in Series.ToList())
        {
            if (series.Publishing == PublishingStatus.Pitching)
            {
                var oneShot = PendingOneShot(series);
                if (oneShot is null || oneShot.Status != ChapterStatus.Complete || oneShot.Editor != EditorStatus.Approved) continue;
                if (Clock.Now < oneShot.DueDate) continue;
                var magazine = Publishers.Require(oneShot.PitchMagazineId!);
                if (MarketOf(magazine.Id).LastIssueClose != Clock.Now) continue;
                ResolvePitch(series, oneShot, magazine);
            }
            else if (series.Publishing == PublishingStatus.Offered && series.PendingOffer is { } offer && Clock.Now >= offer.ExpiresAt)
            {
                var magazine = Publishers.Require(offer.MagazineId);
                series.PendingOffer = null;
                series.Publishing = PublishingStatus.Unpublished;
                Emit(EventType.OfferExpired,
                    $"{magazine.Name}'s offer for {series.Title} lapsed unanswered.",
                    new EventContext(SeriesId: series.Id, MagazineId: magazine.Id));
                TryCreateDoujinVolume(series);
            }
        }
    }

    private void ResolvePitch(Series series, Chapter oneShot, Magazine magazine)
    {
        var quality = oneShot.Quality ?? 0;
        var rep = EffectiveReputation;
        var affinity = series.IsIconic ? 1.0 : magazine.Affinity(TrendRules.Normalise(series.Genre, TrendData));
        var trend = GenreTrendFor(series);
        var chance = PitchRules.Chance(magazine.Tier, quality, rep, affinity, trend);
        var market = MarketOf(magazine.Id);

        if (Rng.NextDouble() < chance)
        {
            var fee = PitchRules.FeePerPage(magazine, rep, PriceIndexNow);
            var first = market.NextIssueClose.AddDays(magazine.CadenceDays * (PitchRules.OfferIssueCount - 1));
            series.PendingOffer = new SerializationOffer
            {
                MagazineId = magazine.Id,
                FeePerPage = fee,
                FirstIssueClose = first,
                ExpiresAt = first,
            };
            series.Publishing = PublishingStatus.Offered;
            Emit(EventType.SerializationOffered,
                $"{magazine.Name} offers to serialize {series.Title} at {fee:N0} yen per page, first issue {first:d MMM yyyy}. The offer stands until then.",
                new EventContext(SeriesId: series.Id, ChapterNumber: oneShot.Number, MagazineId: magazine.Id, Amount: fee));
        }
        else
        {
            var reason = PitchRules.WeakestFactor(quality, rep, affinity, trend) switch
            {
                "quality" => "the one-shot's quality fell short",
                "reputation" => "the studio's name doesn't carry enough weight yet",
                "fit" => "it doesn't fit the magazine",
                _ => "the genre is out of favour",
            };
            series.Publishing = PublishingStatus.Unpublished;
            series.PitchCooldowns[magazine.Id] = Clock.Now.AddDays(7 * PitchRules.CooldownWeeks);
            Emit(EventType.PitchRejected,
                $"{magazine.Name} passed on {series.Title}: {reason} (quality {quality}).",
                new EventContext(SeriesId: series.Id, ChapterNumber: oneShot.Number, MagazineId: magazine.Id));
            if (quality >= 70) AdjustTrackRecord(ReputationRules.RejectedGoodPitch);
            TryCreateDoujinVolume(series);
        }
    }

    // ---------------------------------------------------------------- offers

    private Series RequireOffered(int seriesId)
    {
        var series = RequireSeries(seriesId);
        if (series.Publishing != PublishingStatus.Offered || series.PendingOffer is null)
            throw new InvalidCommandException($"Series '{series.Title}' has no pending offer.");
        return series;
    }

    private void ApplyAcceptOffer(AcceptOfferCommand c)
    {
        var series = RequireOffered(c.SeriesId);
        var offer = series.PendingOffer!;
        var magazine = Publishers.Require(offer.MagazineId);

        series.Contract = new Contract { MagazineId = magazine.Id, FeePerPage = offer.FeePerPage, SignedAt = Clock.Now };
        series.PendingOffer = null;
        series.Publishing = PublishingStatus.Serialized;
        series.Cadence = magazine.Cadence;
        series.Strikes.Clear();
        series.WeeksBelowLine = 0;
        series.WarningIssuedAt = null;
        series.LastRank = null;

        var open = series.OpenChapter;
        if (open is null)
        {
            var created = CreateNextChapter(series, pitchMagazine: null, dueOverride: offer.FirstIssueClose);
            series.Contract.FirstChapterNumber = created.Number;
        }
        else
        {
            series.Contract.FirstChapterNumber = open.Number;
            open.DueDate = offer.FirstIssueClose;
            var name = open.StageWork(Stage.Name);
            var pencils = open.StageWork(Stage.Pencils);
            if (!name.IsDone) open.Editor = EditorStatus.NotRequired;
            else if (pencils.Status == StageStatus.NotStarted && pencils.HoursDone == 0) SubmitForReviewIfRequired(open);
            else open.Editor = EditorStatus.Approved;
        }
        Emit(EventType.OfferAccepted,
            $"{series.Title} signs with {magazine.Name} at {offer.FeePerPage:N0} yen per page; first chapter due {offer.FirstIssueClose:d MMM yyyy}.",
            new EventContext(SeriesId: series.Id, MagazineId: magazine.Id, Amount: offer.FeePerPage));
    }

    private void ApplyDeclineOffer(DeclineOfferCommand c)
    {
        var series = RequireOffered(c.SeriesId);
        var magazine = Publishers.Require(series.PendingOffer!.MagazineId);
        series.PendingOffer = null;
        series.Publishing = PublishingStatus.Unpublished;
        Emit(EventType.OfferDeclined,
            $"{series.Title} turns down {magazine.Name}.",
            new EventContext(SeriesId: series.Id, MagazineId: magazine.Id));
        TryCreateDoujinVolume(series);
    }
}
