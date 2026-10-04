using Xunit;
namespace MangakaSim.Tests;

// Progressive disclosure (spec 2026-10-02): parts open at their moment or chapter and never close.
public class DisclosureTests
{
    static readonly string[] All = ["books", "quiet-speed", "publishing", "contests", "staff", "studios", "industry", "money"];

    [Fact] public void A_new_career_shows_only_the_day_one_interface()
    {
        var s = GameState.NewGame(0);
        Assert.All(All, p => Assert.False(s.PartShown(p), p));
    }

    [Fact] public void The_first_book_and_sale_open_books_and_32x_only()
    {
        var s = GoalProgressTests.FirstSale();
        Assert.True(s.PartShown("books") && s.PartShown("quiet-speed"));
        Assert.False(s.PartShown("publishing") || s.PartShown("staff") || s.PartShown("industry"));
        Assert.Single(s.Events, e => e.Type == EventType.PartOpened && e.Message == "Opened: Books.");
    }

    [Fact] public void A_chapter_opens_every_part_it_backstops()
    {
        var s = GameState.NewGame(0); s.Goals!.Chapter = 2; s.Advance(1);
        foreach (var p in new[] { "books", "quiet-speed", "publishing", "contests", "staff", "industry" }) Assert.True(s.PartShown(p), p);
        Assert.False(s.PartShown("studios") || s.PartShown("money"));
    }

    [Fact] public void Opening_a_page_opens_its_part_and_it_never_closes()
    {
        var s = GameState.NewGame(0);
        s.Apply(new OpenPartCommand("staff")); s.Advance(48);
        Assert.True(s.PartOpened("staff"));
        Assert.Equal(s.ToJson(), s.ReplayTimeline().ToJson());
        Assert.Throws<InvalidCommandException>(() => s.Apply(new OpenPartCommand("not-a-part")));
    }

    [Fact] public void Show_every_screen_shows_all_and_turning_it_off_keeps_what_was_opened()
    {
        var s = GameState.NewGame(0); s.Apply(new OpenPartCommand("staff"));
        s.Apply(new ShowEveryScreenCommand(true));
        Assert.All(All, p => Assert.True(s.PartShown(p), p));
        Assert.False(s.PartOpened("industry"));
        s.Apply(new ShowEveryScreenCommand(false));
        Assert.True(s.PartShown("staff")); Assert.False(s.PartShown("industry"));
    }

    [Fact] public void Every_page_a_part_owns_maps_to_it()
    {
        Assert.Equal("books", DisclosureCatalog.PartFor("Conventions"));
        Assert.Equal("staff", DisclosureCatalog.PartFor("Recruitment"));
        Assert.Equal("studios", DisclosureCatalog.PartFor("Career moves"));
        Assert.Equal("industry", DisclosureCatalog.PartFor("Licenses"));
        Assert.Equal("contests", DisclosureCatalog.PartFor("Awards"));
        foreach (var page in new[] { "Office", "Goals", "Inbox", "Series", "Series details", "Finances", "Help", "Guidance", "Production", "New doujin", "New series", "Showcase", "Person", "Business actions" })
            Assert.Null(DisclosureCatalog.PartFor(page));
    }

    [Fact] public void Parts_survive_a_save_and_a_save_naming_an_unknown_part_is_refused()
    {
        var s = GoalProgressTests.FirstSale();
        Assert.True(GameState.FromJson(s.ToJson()).PartOpened("books"));
        s.Disclosure!.Opened.Add(new("not-a-part", s.Clock.Now, false));
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(s.ToJson()));
    }
}
