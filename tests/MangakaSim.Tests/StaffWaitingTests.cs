using Xunit;
namespace MangakaSim.Tests;
// Fresh-player finding A4: assistants showed "Available for work" while their titles waited on the mangaka's storyboard.
public class StaffWaitingTests
{
    static (GameState State, Person Assistant, Series Series) Setup()
    {
        var s = GameState.NewGame(17);
        var candidate = s.Candidates.First();
        s.Apply(new HireStaffCommand(candidate.Id, s.Locations.Single(l => l.BusinessId == s.ControlledBusinessId).Id, candidate.ExpectedSalary));
        var assistant = s.FindPerson(candidate.Id)!;
        s.Advance(s.Clock.HoursUntil(assistant.Employment!.StartsAt));
        s.Apply(new CreateSeriesCommand("Waiting title", "adventure", Cadence.Monthly, 48));
        var series = s.Series.Last();
        s.Apply(new AssignStaffCommand(assistant.Id, series.Id, false));
        return (s, assistant, series);
    }
    [Fact]public void An_assistant_waiting_on_the_storyboard_says_whose_and_which_title()
    {
        var (s, assistant, _) = Setup();
        s.Advance(1);
        Assert.Null(assistant.CurrentTask);
        Assert.Equal($"Waiting for {s.Protagonist.Name}'s storyboard · Waiting title", s.WaitingReason(assistant));
    }
    [Fact]public void Someone_with_nothing_assigned_has_no_waiting_reason()
    {
        var s = GameState.NewGame(17);
        var candidate = s.Candidates.First();
        s.Apply(new HireStaffCommand(candidate.Id, s.Locations.Single(l => l.BusinessId == s.ControlledBusinessId).Id, candidate.ExpectedSalary));
        s.Advance(26);
        Assert.Null(s.WaitingReason(s.FindPerson(candidate.Id)!));
    }
}
// Fresh-player finding A5: the Awards page offered "Use existing manuscript" for titles the command then refused.
public class AdoptManuscriptOfferTests
{
    [Fact]public void Only_titles_the_command_accepts_are_offered()
    {
        var s = GameState.NewGame(0);
        s.Apply(new CreateSeriesCommand("Busy title", "adventure", Cadence.Monthly, 16));
        var series = s.Series.Last();
        for (int d = 0; d < 60 && series.Chapters[0].Status != ChapterStatus.Complete; d++) s.Advance(24);
        var first = series.Chapters[0];
        // An ongoing title with its next chapter still in progress cannot enter.
        Assert.Contains(series.Chapters, c => c.Status != ChapterStatus.Complete);
        Assert.False(s.CanAdoptManuscript(first));
        Assert.ThrowsAny<InvalidCommandException>(() => s.Apply(new AdoptManuscriptCommand(first.Id)));
        s.Apply(new PauseSeriesCommand(series.Id));
        foreach (var open in series.Chapters.Where(c => c.Status != ChapterStatus.Complete).ToList()) series.Chapters.Remove(open);
        Assert.True(s.CanAdoptManuscript(first));
        s.Apply(new AdoptManuscriptCommand(first.Id));
    }
}
