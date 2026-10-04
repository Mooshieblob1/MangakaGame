using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    /// <summary>
    /// Studio island (Q59, 2026-10-03): an older parents' home still in the old default arrangement moves to the island
    /// on load. A home the player has rearranged keeps its layout, and Helper-Chan keeps her alcove there.
    /// </summary>
    internal void EnsureStudioIsland()
    {
        var changed = false;
        foreach (var l in Locations.Where(l => l.IsFamilyHome && !l.Closed && !l.StudioIsland))
        {
            var office = Offices.FirstOrDefault(o => o.LocationId == l.Id);
            if (office is null) continue;
            var available = AvailableFurniture(l.Id).ToList();
            var required = Math.Min(l.Seats, People.Count(p => p.Employment?.LocationId == l.Id));
            List<OfficePlacement> oldDefault, island;
            try { oldDefault = OfficeAutoArrange.Arrange(OfficeCatalog.Plan(l), available, [], required).Placements; }
            catch (InvalidCommandException) { continue; }
            if (!oldDefault.ToHashSet().SetEquals(office.Placements) || oldDefault.Count != office.Placements.Count) continue;
            l.StudioIsland = true;
            try { island = OfficeAutoArrange.Arrange(OfficeCatalog.Plan(l), available, [], required).Placements; }
            catch (InvalidCommandException) { l.StudioIsland = false; continue; }
            office.Placements = island; office.Assignments.Clear();
            OfficeRevision++; l.OfficeRevision++; changed = true;
        }
        if (!changed) return;
        RefreshOfficeAssignments();
        World.ReplayCheckpoint = null; World.ReplayLogStart = CommandLog.Count;
        ValidateSave(); World.ReplayCheckpoint = ToJson();
    }
}
