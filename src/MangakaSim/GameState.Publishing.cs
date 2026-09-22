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
}
