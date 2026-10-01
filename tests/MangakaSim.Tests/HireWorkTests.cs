using Xunit;
namespace MangakaSim.Tests;
// Fresh-player findings A4 and B1: a hire sat idle because the mangaka out-skilled them on every stage (Q52).
public class HireWorkTests
{
    static (GameState State, Person Assistant, Chapter Chapter) SoloDoujinWithHire()
    {
        var s = GameState.NewGame(17);
        var candidate = s.Candidates.First();
        s.Apply(new HireStaffCommand(candidate.Id, s.Locations.Single(l => l.BusinessId == s.ControlledBusinessId).Id, candidate.ExpectedSalary));
        var assistant = s.FindPerson(candidate.Id)!;
        s.Advance(s.Clock.HoursUntil(assistant.Employment!.StartsAt));
        s.Apply(new CreateDoujinCommand("Solo pages", "adventure"));
        s.Advance(1);
        return (s, assistant, s.Series.Last().Chapters.Single());
    }

    [Fact]public void A_hire_on_no_team_takes_backgrounds_and_tones_and_the_mangaka_keeps_the_rest()
    {
        var (s, assistant, chapter) = SoloDoujinWithHire();
        foreach (var stage in new[] { Stage.Name, Stage.Pencils, Stage.Inks })
            Assert.Equal(s.ProtagonistPersonId, chapter.StageWork(stage).AssignedTo);
        foreach (var stage in new[] { Stage.Backgrounds, Stage.Tones })
            Assert.Equal(assistant.Id, chapter.StageWork(stage).AssignedTo);
    }

    [Fact]public void The_waiting_hire_says_whose_stage_they_wait_for()
    {
        var (s, assistant, _) = SoloDoujinWithHire();
        Assert.Equal($"Waiting for {s.Protagonist.Name}'s storyboard · Solo pages", s.WaitingReason(assistant));
    }

    [Fact]public void A_team_choice_still_overrides_the_default()
    {
        var (s, assistant, chapter) = SoloDoujinWithHire();
        s.Apply(new AssignStaffCommand(s.ProtagonistPersonId, s.Series.Last().Id, false));
        s.Advance(1);
        Assert.Equal(s.ProtagonistPersonId, chapter.StageWork(Stage.Backgrounds).AssignedTo);
    }

    [Fact]public void A_hire_covers_the_mangakas_stage_while_the_mangaka_is_away()
    {
        var (s, assistant, chapter) = SoloDoujinWithHire();
        for (var i = 0; i < 24 * 14 && !chapter.StageWork(Stage.Name).IsDone; i++) s.Advance(1);
        Assert.True(chapter.StageWork(Stage.Name).IsDone);
        s.Protagonist.BusyUntil = s.Clock.Now.AddDays(3); // away, as at a convention or a part-time job
        var before = chapter.StageWork(Stage.Pencils).HoursDone;
        s.Advance(48);
        Assert.True(chapter.StageWork(Stage.Pencils).HoursDone > before, "the hire drew pencils while the mangaka was away");
    }
}
