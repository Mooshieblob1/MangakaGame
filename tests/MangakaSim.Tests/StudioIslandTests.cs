using MangakaSim.Rules;
using Xunit;
namespace MangakaSim.Tests;

// Studio island (Q59, 2026-10-03): in the parents' home, Helper-Chan's desk faces the spare desk across an island and
// Aki's desk heads it, like a real manga studio. Older homes still in the old default layout move over on load.
public class StudioIslandTests
{
    private static (GameState State, StudioLocation Home, OfficeLayout Office) Career(int seed = 0)
    {
        var s = GameState.NewGame(seed);
        var home = s.Locations.Single(l => l.BusinessId == s.ControlledBusinessId);
        return (s, home, s.Offices.Single(o => o.LocationId == home.Id));
    }

    private static OfficePlacement DeskOf(GameState s, OfficeLayout office, int person) =>
        office.Placements.Single(p => p.ItemId == office.Assignments.Single(a => a.PersonId == person).DeskId);

    [Fact] public void A_new_career_seats_Aki_at_the_head_of_the_island()
    {
        var (s, home, office) = Career();
        Assert.True(home.StudioIsland);
        Assert.Equal((3, 7, 0), (DeskOf(s, office, s.ProtagonistPersonId) is var aki ? (aki.X, aki.Z, aki.Rotation) : default));
        var spare = office.Placements.Single(p => OfficeCatalog.Get(s.Furniture.Single(i => i.Id == p.ItemId).Kind).Desk && p.ItemId != DeskOf(s, office, s.ProtagonistPersonId).ItemId);
        Assert.Equal((5, 2, 3), (spare.X, spare.Z, spare.Rotation)); // faces Helper-Chan's desk across the island
    }

    [Fact] public void Furniture_and_walking_routes_keep_clear_of_Helper_Chans_desk()
    {
        var (_, home, _) = Career();
        var plan = OfficeCatalog.Plan(home);
        Assert.True(plan.Protected(3, 4) && plan.Protected(0, 4));
        Assert.Contains(new OfficeCell(3, 4), OfficeLayoutRules.ClearanceMask(plan, []));
        home.StudioIsland = false;
        Assert.False(OfficeCatalog.Plan(home).Protected(3, 4));
    }

    [Fact] public void Auto_arrange_keeps_the_island()
    {
        var (s, home, office) = Career();
        var arranged = s.ArrangeOffice(home.Id);
        Assert.Equal(office.Placements.OrderBy(p => p.ItemId), arranged.Placements.Select(p => p with { Locked = false }).OrderBy(p => p.ItemId));
    }

    private static GameState OlderHome(Action<GameState, StudioLocation, OfficeLayout>? change = null)
    {
        var (s, home, office) = Career();
        home.StudioIsland = false;
        office.Placements = OfficeAutoArrange.Arrange(OfficeCatalog.Plan(home), s.AvailableFurniture(home.Id), [], 1).Placements;
        change?.Invoke(s, home, office);
        office.Assignments = [new(s.ProtagonistPersonId, office.Placements.Where(p => p.DeskId is not null).Min(p => p.DeskId!.Value))];
        s.World.ReplayCheckpoint = null; s.World.ReplayLogStart = s.CommandLog.Count; s.World.ReplayCheckpoint = s.ToJson();
        return GameState.FromJson(s.ToJson());
    }

    [Fact] public void An_older_home_in_the_old_default_layout_moves_to_the_island()
    {
        var loaded = OlderHome();
        var home = loaded.Locations.Single(l => l.IsFamilyHome);
        Assert.True(home.StudioIsland);
        var office = loaded.Offices.Single(o => o.LocationId == home.Id);
        var aki = DeskOf(loaded, office, loaded.ProtagonistPersonId);
        Assert.Equal((3, 7, 0), (aki.X, aki.Z, aki.Rotation));
        Assert.NotNull(GameState.FromJson(loaded.ToJson())); // the migrated save still loads and replays
    }

    [Fact] public void An_older_home_the_player_rearranged_keeps_its_layout()
    {
        List<OfficePlacement>? kept = null;
        var loaded = OlderHome((s, home, office) =>
        {
            var spare = office.Placements.Where(p => p.DeskId is not null).OrderByDescending(p => p.DeskId).First();
            office.Placements.RemoveAll(p => p.ItemId == spare.ItemId || p.ItemId == spare.DeskId); // the spare desk went into storage
            kept = office.Placements.ToList();
        });
        var home = loaded.Locations.Single(l => l.IsFamilyHome);
        Assert.False(home.StudioIsland);
        Assert.Equal(kept, loaded.Offices.Single(o => o.LocationId == home.Id).Placements);
    }
}
