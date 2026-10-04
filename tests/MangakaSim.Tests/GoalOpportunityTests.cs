using Xunit;
namespace MangakaSim.Tests;

// Career goals board (spec 2026-10-01): the studio desk unlock, the free convention table and the pitch bonus.
public class GoalOpportunityTests
{
    [Fact] public void The_rookie_chapter_unlocks_the_studio_desk()
    {
        var s = GameState.NewGame(0);
        Assert.False(s.EquipmentAvailable("desk-studio"));
        s.Goals!.Unlocked.Add("desk-studio");
        Assert.True(s.EquipmentAvailable("desk-studio"));
    }

    // Books the next Toshima convention at least minDaysAhead away, trying each day's booking on a copy first.
    static ConventionBooking Book(GameState s, int minDaysAhead = 0)
    {
        var book = new StudioActionCommand(StudioAction.BookConvention, s.ProtagonistPersonId, Value: 1);
        for (var d = 0; d < 120; d++)
        {
            var probe = GameState.FromJson(s.ToJson());
            try
            {
                probe.Apply(book);
                if ((probe.Bookings.Last().Date - s.Clock.Now.Date).TotalDays >= minDaysAhead) { s.Apply(book); return s.Bookings.Last(); }
            }
            catch (InvalidCommandException ex) when (ex.Message.StartsWith("Booking opens")) { }
            s.Advance(24);
        }
        throw new InvalidOperationException("No convention opened for booking.");
    }

    [Fact] public void A_free_table_is_used_once_and_comes_back_when_cancelled_in_time()
    {
        var s = GameState.NewGame(0); s.Goals!.FreeConventionTables = 1;
        var free = Book(s, minDaysAhead: 8); // cancelled a week or more ahead, so the refund rule applies
        Assert.Equal(0, free.Fee); Assert.Equal(0, s.Goals.FreeConventionTables);
        s.Apply(new StudioActionCommand(StudioAction.CancelConvention, free.Id));
        Assert.Equal(1, s.Goals.FreeConventionTables);
        var paid = GameState.NewGame(0);
        Assert.Equal(5000, Book(paid).Fee);
    }

    [Fact] public void The_pitch_bonus_is_used_by_the_players_own_pitch_once()
    {
        var s = GameState.NewGame(0); s.Apply(new CreateSeriesCommand("Pitch title", "adventure", Cadence.Monthly, 16));
        var series = s.Series.Last();
        var best = CareerGuidance.PitchOutlooks(s, series)[0];
        s.Goals!.PitchBoost = true;
        Assert.Equal(Math.Min(.95, best.Chance + .1), CareerGuidance.Outlook(s, series, best.Magazine.Id).Chance, 6);
        var rival = new Series { Id = -1, BusinessId = s.ControlledBusinessId + 1000 };
        Assert.Equal(0, s.TakePitchBoost(rival)); Assert.True(s.Goals.PitchBoost);
        Assert.Equal(.1, s.TakePitchBoost(series)); Assert.False(s.Goals.PitchBoost);
        Assert.Equal(0, s.TakePitchBoost(series));
    }
}
