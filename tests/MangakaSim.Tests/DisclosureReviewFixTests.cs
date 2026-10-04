using Xunit;
namespace MangakaSim.Tests;

// Progressive disclosure, final review fixes (2026-10-03).
public class DisclosureReviewFixTests
{
    [Fact] public void Aki_and_pay_pages_are_not_hidden_parts()
    {
        Assert.Null(DisclosureCatalog.PartFor("Person"));           // clicking your own character must not open Staff
        Assert.Null(DisclosureCatalog.PartFor("Business actions")); // salaries live there too; only loans and incorporation are hidden
    }

    [Fact] public void Announcements_never_claim_a_moment_a_route_may_have_skipped()
    {
        Assert.DoesNotContain("first book", DisclosureCatalog.Get("books").Announcement!);
        Assert.DoesNotContain("You're in", DisclosureCatalog.Get("industry").Announcement!);
    }

    [Fact] public void A_leased_studio_opens_studios_without_staff()
    {
        var s = GameState.NewGame(0);
        var home = s.Locations.Single(l => l.BusinessId == s.ControlledBusinessId);
        home.IsFamilyHome = false; home.PropertyOfferId = 1;
        s.Advance(1);
        Assert.True(s.PartOpened("studios"));
    }

    [Fact] public void Rival_offers_to_staff_still_arrive_while_industry_is_closed()
    {
        var s = GameState.NewGame(17);
        var candidate = s.Candidates.First();
        s.Apply(new HireStaffCommand(candidate.Id, s.Locations.Single(l => l.BusinessId == s.ControlledBusinessId).Id, candidate.ExpectedSalary));
        bool OfferedToStaff() => s.World.Offers.Any(o => o.PersonId != s.ProtagonistPersonId);
        for (var week = 0; week < 52 * 4 && !OfferedToStaff(); week++)
        {
            s.Advance(24 * 7);
            if (s.ControlledBusiness.Account.Balance < 500_000) s.ControlledBusiness.Account.Balance += 1_000_000; // keep the hire paid
        }
        Assert.False(s.PartShown("industry"));
        Assert.True(OfferedToStaff());
    }

    [Fact] public void A_mid_career_older_save_opens_every_part_its_chapter_has_reached()
    {
        var old = GameState.NewGame(0); old.Goals!.Chapter = 2; old.Disclosure = null;
        var loaded = GameState.FromJson(old.ToJson());
        foreach (var p in new[] { "books", "quiet-speed", "publishing", "contests", "staff", "industry" })
            Assert.True(loaded.Disclosure!.Opened.Single(r => r.Id == p).Backfilled, p);
        Assert.False(loaded.PartShown("studios") || loaded.PartShown("money"));
        Assert.DoesNotContain(loaded.Events, e => e.Type == EventType.PartOpened);
    }
}
