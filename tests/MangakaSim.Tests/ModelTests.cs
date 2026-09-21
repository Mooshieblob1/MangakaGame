using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class ModelTests
{
    [Fact]
    public void Rng_from_same_seed_produces_same_sequence()
    {
        var a = Rng.FromSeed(42);
        var b = Rng.FromSeed(42);
        for (var i = 0; i < 5; i++) Assert.Equal(a.NextUInt64(), b.NextUInt64());
    }

    [Fact]
    public void Rng_NextInt_stays_below_max()
    {
        var rng = Rng.FromSeed(7);
        for (var i = 0; i < 100; i++) Assert.InRange(rng.NextInt(10), 0, 9);
    }

    [Fact]
    public void Rng_state_can_be_copied_to_continue_identically()
    {
        var a = Rng.FromSeed(1);
        a.NextUInt64();
        var b = new Rng { State = a.State };
        Assert.Equal(a.NextUInt64(), b.NextUInt64());
    }

    [Fact]
    public void StageWork_IsDone_for_complete_and_skipped_only()
    {
        Assert.False(new StageWork { Status = StageStatus.NotStarted }.IsDone);
        Assert.False(new StageWork { Status = StageStatus.InProgress }.IsDone);
        Assert.True(new StageWork { Status = StageStatus.Complete }.IsDone);
        Assert.True(new StageWork { Status = StageStatus.Skipped }.IsDone);
    }

    [Fact]
    public void Chapter_IsFinished_when_all_stages_done()
    {
        var chapter = new Chapter
        {
            Stages = StageOrder.All.Select(s => new StageWork { Stage = s, Status = StageStatus.Complete }).ToList(),
        };
        Assert.True(chapter.IsFinished);
        chapter.Stages[2].Status = StageStatus.InProgress;
        Assert.False(chapter.IsFinished);
    }

    [Fact]
    public void Chapter_StageWork_finds_by_stage()
    {
        var chapter = new Chapter
        {
            Stages = StageOrder.All.Select(s => new StageWork { Stage = s }).ToList(),
        };
        Assert.Equal(Stage.Inks, chapter.StageWork(Stage.Inks).Stage);
    }

    [Fact]
    public void Schedule_IsDayOff_and_IsRegularHour()
    {
        var schedule = new Schedule { WorkStartHour = 8, WorkEndHour = 18, DaysOff = { DayOfWeek.Sunday } };
        Assert.True(schedule.IsRegularHour(new DateTime(1996, 4, 1, 8, 0, 0)));
        Assert.True(schedule.IsRegularHour(new DateTime(1996, 4, 1, 17, 0, 0)));
        Assert.False(schedule.IsRegularHour(new DateTime(1996, 4, 1, 18, 0, 0)));
        Assert.False(schedule.IsRegularHour(new DateTime(1996, 4, 1, 7, 0, 0)));
        Assert.False(schedule.IsRegularHour(new DateTime(1996, 4, 7, 10, 0, 0))); // Sunday
    }
}
