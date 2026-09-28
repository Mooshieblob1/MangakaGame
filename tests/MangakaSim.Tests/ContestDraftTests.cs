using Xunit;
namespace MangakaSim.Tests;
// Fresh-player finding A9: a revised contest manuscript left its old draft looking like a waiting magazine chapter,
// so every save after the title was serialized was refused ("invalid publication slots").
public class ContestDraftTests
{
    static GameState SerializedAfterRevision()
    {
        var s = GameState.NewGame(0);
        s.Apply(new RecognitionCommand(RecognitionAction.CreateManuscript, Text: "Contest", Category: "story"));
        var manuscript = s.Progression.Manuscripts.Last(); var series = s.Series.Single(x => x.Id == manuscript.SeriesId);
        void Finish() { for (int d = 0; d < 200 && s.FindChapter(manuscript.ChapterId)!.Status != ChapterStatus.Complete; d++) s.Advance(24); }
        Finish();
        s.Apply(new RecognitionCommand(RecognitionAction.Revise, manuscript.Id));
        Finish();
        s.Apply(new RecognitionCommand(RecognitionAction.ReleaseManuscript, manuscript.Id));
        for (int day = 0; day < 700 && series.Publishing != PublishingStatus.Offered; day++)
        {
            if (series.Publishing == PublishingStatus.Unpublished && CareerGuidance.PitchOutlooks(s, series).FirstOrDefault(o => o.Open) is { } best)
                s.Apply(new PitchSeriesCommand(series.Id, best.Magazine.Id));
            s.Advance(24);
        }
        Assert.Equal(PublishingStatus.Offered, series.Publishing);
        s.Apply(new AcceptOfferCommand(series.Id));
        return s;
    }
    [Fact]public void Serializing_a_revised_contest_title_keeps_the_save_loadable()
    {
        var s = SerializedAfterRevision();
        var loaded = GameState.FromJson(s.ToJson());
        s.Advance(24 * 60); GameState.FromJson(s.ToJson());
        Assert.Equal(loaded.Clock.Now.AddDays(60), s.Clock.Now);
    }
    [Fact]public void The_old_draft_does_not_count_as_a_chapter_ready_for_the_magazine()
    {
        var s = SerializedAfterRevision();
        var series = s.Series.Single(x => x.Contract is not null);
        Assert.Equal(0, s.ChaptersReadyAhead(series).Ready);
    }
    [Fact]public void Saves_written_before_the_fix_are_repaired_on_load()
    {
        var json = SerializedAfterRevision().ToJson().Replace("\"Superseded\":true", "\"Superseded\":false");
        var loaded = GameState.FromJson(json);
        Assert.Contains(loaded.Series.SelectMany(x => x.Chapters), c => c.Superseded);
    }
}
