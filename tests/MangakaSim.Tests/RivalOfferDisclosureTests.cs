using Xunit;
namespace MangakaSim.Tests;

// Progressive disclosure, Q57: rival studios approach Aki only once Industry has opened.
public class RivalOfferDisclosureTests
{
    static bool OfferedToAki(GameState s) => s.World.Offers.Any(o => o.PersonId == s.ProtagonistPersonId);

    [Fact] public void A_solo_doujin_artist_gets_no_rival_job_offer()
    {
        var s = GameState.NewGame(0);
        s.Advance(24 * 365 * 4); // as long as the positive test allows, so it cannot pass vacuously
        Assert.False(s.PartShown("industry"));
        Assert.False(OfferedToAki(s));
    }

    [Fact] public void Once_industry_is_open_rival_offers_to_aki_can_arrive()
    {
        var s = GameState.NewGame(0); s.Apply(new OpenPartCommand("industry"));
        for (var week = 0; week < 52 * 4 && !OfferedToAki(s); week++) s.Advance(24 * 7);
        Assert.True(OfferedToAki(s));
    }
}
