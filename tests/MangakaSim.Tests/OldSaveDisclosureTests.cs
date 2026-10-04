using System.Text.Json.Nodes;
using Xunit;
namespace MangakaSim.Tests;

// Progressive disclosure (spec 2026-10-02): older saves open every part already reached, silently.
public class OldSaveDisclosureTests
{
    static GameState OldCareer()
    {
        var s = GameState.NewGame(0); s.Disclosure = null;
        s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && s.Series[0].Volumes.Count == 0; d++) s.Advance(24);
        return s;
    }

    static void AssertBackfilled(GameState loaded)
    {
        Assert.True(loaded.PartOpened("books"));
        Assert.True(loaded.Disclosure!.Opened.Single(r => r.Id == "books").Backfilled);
        Assert.DoesNotContain(loaded.Events, e => e.Type == EventType.PartOpened);
        Assert.False(loaded.PartShown("industry"));
        Assert.Equal(loaded.ToJson(), loaded.ReplayTimeline().ToJson());
    }

    [Fact] public void A_save_with_no_disclosure_opens_the_parts_it_has_reached_silently() =>
        AssertBackfilled(GameState.FromJson(OldCareer().ToJson()));

    [Fact] public void A_save_written_before_the_disclosure_property_existed_loads_the_same_way()
    {
        var node = JsonNode.Parse(OldCareer().ToJson())!.AsObject(); node.Remove("Disclosure");
        AssertBackfilled(GameState.FromJson(node.ToJsonString()));
    }
}
