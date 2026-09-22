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
}
