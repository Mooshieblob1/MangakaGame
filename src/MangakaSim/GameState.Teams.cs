namespace MangakaSim;

public partial class GameState
{
    private void ApplyAssignStaff(AssignStaffCommand c)
    {
        var person = RequirePerson(c.PersonId);
        var series = RequireSeries(c.SeriesId);
        if (person.Employment is not { NoticeEndsAt: null } job || job.LocationId != series.LocationId)
            throw new InvalidCommandException("Choose an active employee at the series workplace.");
        if (c.Lead && IsHistoricalAssistant(person.Id)) throw new InvalidCommandException("This temporary assistant cannot lead a series.");
        if (c.Lead && Series.Count(s => s.Status != SeriesStatus.Ended && s.LeadPersonId == person.Id && s.Id != series.Id) >= 2)
            throw new InvalidCommandException("A creator can lead at most two active series.");
        // A permanent replacement changes prospective title rights, never old chapter attribution.
        if (c.Lead) { series.LeadPersonId = person.Id; series.RightsLeadPersonId = person.Id; }
        person.MainSeriesId = series.Id;
    }

    private void ApplyAssignStage(AssignStageCommand c)
    {
        var chapter = RequireChapter(c.ChapterId);
        RequireStage(c.Stage);
        var work = chapter.StageWork(c.Stage);
        if (work.IsDone) throw new InvalidCommandException("Finished work cannot be reassigned.");
        if (c.PersonId is { } id)
        {
            var person = RequirePerson(id);
            var series = SeriesOf(chapter);
            if (person.Employment is not { NoticeEndsAt: null } job || job.LocationId != series.LocationId)
                throw new InvalidCommandException("Choose an active employee at this workplace.");
            if (c.Stage == Stage.Name && id != (chapter.CreatorPersonId ?? series.LeadPersonId))
                throw new InvalidCommandException("Only the chapter's lead creator can do Name.");
        }
        work.ManualAssignee = c.PersonId;
        work.AssignedTo = c.PersonId;
    }
}
