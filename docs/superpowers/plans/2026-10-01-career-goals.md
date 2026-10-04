# Career Goals Board Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the player five career chapters of visible goals with progress bars and one-off rewards, on a Goals page, the office dashboard and Helper-Chan's phone, so there is always something to aim for.

**Architecture:** An engine-free goal catalogue (`src/MangakaSim/Goals.cs`) defines chapters, goals, measures and rewards; `GameState` keeps a small saved `GoalsState`, checks the current chapter's goals every hour and grants rewards exactly once. Older saves are backfilled on load (no cash or fans) and their replay checkpoint is rebased. Helper-Chan's guidance adds goal texts that never stop 32x; the Godot layer adds the Goals page, a rail badge, a dashboard card, How? tips, celebrations and reward furniture drawn from primitives.

**Tech Stack:** C# (.NET 8), Godot 4.7.2 .NET, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-01-career-goals-design.md`

## Global Constraints

- Chapters (Q50): Doujin Days, Rookie, Serialized, Studio Head, Legend, then optional Mastery; 4 to 6 goals each; only the current chapter is shown; goals in any order within it; a goal counts when reached by any route.
- Rewards (Q49): unlocks and one-off opportunities plus modest one-off cash or fans; no permanent bonuses to skill, sales or speed (awards decision 17).
- One system (Q51): the board shows what, Helper-Chan says how; guidance continues after the first hire.
- Cash goes to the business account (the doujin budget, or the company once incorporated), ledger reason "Goal reward: <goal>" or "Chapter reward: <chapter>".
- Fans go to the newest series of the business the player runs.
- Older saves: goals already met count as done, unlocks and decorations granted, cash, fans and opportunities not; no texts or pop-ups for them.
- A goal shows a notice and Helper-Chan's text and never stops 32x; a chapter stops the game, plays the good-news music, shows her scene and grants the chapter reward.
- Same goals and rewards on every difficulty and in Sandbox. Deterministic; replays and saves behave as before; older saves keep loading.
- Reward furniture is drawn from primitives in code (no art credits) and cannot be bought.
- Docs CRLF, code LF, metric, no em dashes. Do not commit: the user commits. Packaging is not approved.
- Commands (PowerShell, repository root):
  ```powershell
  dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~<Name>"
  dotnet build MangakaGame.sln -warnaserror
  pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Flags '--goals-smoke' -Timeout 600"
  ```
  The smoke runner starts Godot with a timeout so a hung run cannot block the session.

## Review Focus

1. An older save loaded mid-career: goals already met count as done with no cash, fans, events, pop-ups or phone texts, and its replay still equals the live state (Task 3 tests; Task 5 backfill test).
2. A goal text arriving together with a stopping text or stopping event: 32x still stops for the stopping one and never for the goal text (Task 5 stopping test; Task 7 smoke).
3. A free convention table that is cancelled in time comes back (Task 4 test).
4. The pitch bonus is used only by the player's own pitch, and only once (Task 4 test).
5. The rail badge and the goal cards at 1280 x 720 with 150% text, both themes: no cut-off text (Task 6 adds the Goals page to the display sweep; Task 9 runs it at 0 flagged).

---

### Task 1: Goal catalogue and reward furniture (engine-free)

**Files:**
- Create: `src/MangakaSim/Goals.cs`
- Create: `src/MangakaSim/GameState.GoalMeasures.cs`
- Modify: `src/MangakaSim/Office.cs:46` (`FurnitureDefinition`), `:70-79` (catalogue)
- Modify: `src/MangakaSim/GameState.Office.cs:97` (refuse reward-only purchases)
- Test: `tests/MangakaSim.Tests/GoalCatalogTests.cs`

**Interfaces:**
- Produces: `sealed record GoalProgress(double Have, double Need, string Text)` with `bool Done`, `double Fraction`; `sealed record GoalReward(long Cash = 0, int Fans = 0, string? Furniture = null, string? Unlock = null, bool FreeConventionTable = false, bool PitchBoost = false, string? Scene = null)` with `string Describe()`; `sealed record GoalDefinition(string Id, int Chapter, string Title, string Why, string Target, string Tip, GoalReward Reward, Func<GameState, GoalProgress> Measure)`; `sealed record GoalChapter(int Index, string Name, string Blurb, GoalReward Reward)`; `static class GoalCatalog` with `GoalChapter[] Chapters`, `GoalDefinition[] Goals`, `IEnumerable<GoalDefinition> In(int chapter)`, `GoalDefinition Get(string id)`, `string? UnlockedBy(string kind)`; `FurnitureDefinition.RewardOnly`; reward kinds `trophy-shelf`, `framed-letter`, `ranking-chart`, `award-plaque`, `plant-set`, `company-sign`, `gold-frame`, `display-cabinet`, `trophy`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/GoalCatalogTests.cs`:

```csharp
using Xunit;
namespace MangakaSim.Tests;

// Career goals board (spec 2026-10-01): the catalogue, its measures and the reward furniture.
public class GoalCatalogTests
{
    [Fact] public void Five_named_chapters_then_mastery_with_four_to_six_goals_each()
    {
        Assert.Equal(new[] { "Doujin Days", "Rookie", "Serialized", "Studio Head", "Legend", "Mastery" }, GoalCatalog.Chapters.Select(c => c.Name));
        foreach (var c in GoalCatalog.Chapters.Take(5)) Assert.InRange(GoalCatalog.In(c.Index).Count(), 4, 6);
        Assert.Equal(GoalCatalog.Goals.Length, GoalCatalog.Goals.Select(g => g.Id).Distinct().Count());
    }

    [Fact] public void Tips_fit_a_phone_bubble_and_every_goal_names_its_reward()
    {
        foreach (var g in GoalCatalog.Goals)
        {
            Assert.True(g.Tip.Length <= CareerGuidance.TextLimit, $"{g.Id}: {g.Tip.Length}");
            Assert.False(string.IsNullOrWhiteSpace(g.Reward.Describe()), g.Id);
        }
    }

    [Fact] public void Reward_furniture_exists_cannot_be_bought_and_sits_at_the_end_of_the_catalogue()
    {
        foreach (var r in GoalCatalog.Goals.Select(g => g.Reward).Concat(GoalCatalog.Chapters.Select(c => c.Reward)))
        {
            if (r.Furniture is { } f) OfficeCatalog.Get(f);
            if (r.Unlock is { } u) Assert.False(OfficeCatalog.Get(u).RewardOnly);
        }
        Assert.True(OfficeCatalog.Furniture.SkipWhile(f => !f.RewardOnly).All(f => f.RewardOnly), "reward-only items come last, so the buy list keeps its indexes");
        Assert.Equal("Rookie", GoalCatalog.UnlockedBy("desk-studio"));
        Assert.Null(GoalCatalog.UnlockedBy("desk"));
    }

    [Fact] public void A_new_career_measures_from_zero()
    {
        var s = GameState.NewGame(0);
        Assert.All(GoalCatalog.In(0), g => Assert.False(g.Measure(s).Done));
        Assert.Equal("¥0 of ¥50,000", GoalCatalog.Get("sales-50000").Measure(s).Text);
        Assert.Equal("0 of 100", GoalCatalog.Get("fans-100").Measure(s).Text);
    }

    [Fact] public void A_first_doujin_and_its_first_sale_meet_the_first_two_goals()
    {
        var s = GameState.NewGame(0); s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && s.Series[0].Volumes.Count == 0; d++) s.Advance(24);
        Assert.True(GoalCatalog.Get("doujin-finished").Measure(s).Done);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes.Single().Id, Amount: 10, Value: (int)PrintTier.CopyShop));
        for (var d = 0; d < 30 && !GoalCatalog.Get("first-copy").Measure(s).Done; d++) s.Advance(24);
        Assert.True(GoalCatalog.Get("first-copy").Measure(s).Done);
    }

    [Fact] public void Reward_only_furniture_cannot_be_bought()
    {
        var s = GameState.NewGame(0); var l = s.Locations.Single(x => x.BusinessId == s.ControlledBusinessId);
        var ex = Assert.Throws<InvalidCommandException>(() => s.Apply(new ApplyOfficeLayoutCommand(l.Id, s.OfficeRevision,
            s.OfficeAt(l.Id).Placements.ToList(), [new(-1, "trophy-shelf")], [])));
        Assert.Contains("goal reward", ex.Message);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~GoalCatalogTests"`
Expected: build FAILS with `CS0103: The name 'GoalCatalog' does not exist`.

- [ ] **Step 3: Add the reward furniture**

In `src/MangakaSim/Office.cs`, change the record header at line 46 to:

```csharp
public sealed record FurnitureDefinition(string Id, string Name, int Width, int Depth, long Price, string Art, int Variant = 0, bool RewardOnly = false)
```

In `OfficeCatalog.Furniture`, rename `"Professional drawing desk (reputation 20)"` to `"Professional drawing desk"` (the catalogue now names the unlocking chapter), and append after the `print` entry, before `];`:

```csharp
        // Goal rewards (spec 2026-10-01): given, never bought, and kept last so the buy list keeps its indexes.
        new("trophy-shelf","Trophy shelf",4,2,6000,"trophy-shelf",RewardOnly:true),
        new("framed-letter","Framed editor's letter",2,1,1000,"framed-letter",RewardOnly:true),
        new("ranking-chart","Ranking chart",3,1,1000,"ranking-chart",RewardOnly:true),
        new("award-plaque","Award plaque",2,1,3000,"award-plaque",RewardOnly:true),
        new("plant-set","Housewarming plants",4,2,3000,"plant-set",RewardOnly:true),
        new("company-sign","Company sign",4,1,5000,"company-sign",RewardOnly:true),
        new("gold-frame","Gold frame",2,1,5000,"gold-frame",RewardOnly:true),
        new("display-cabinet","Award display cabinet",4,2,20000,"display-cabinet",RewardOnly:true),
        new("trophy","Trophy",1,1,10000,"trophy",RewardOnly:true)
```

(Keep the comma after the existing `print` entry.)

In `src/MangakaSim/GameState.Office.cs`, inside `foreach(var p in c.Purchases)`, add as the first line of the loop body:

```csharp
            if(OfficeCatalog.Get(p.Kind).RewardOnly)throw new InvalidCommandException("That item is a goal reward and cannot be bought.");
```

- [ ] **Step 4: Write the measures**

`src/MangakaSim/GameState.GoalMeasures.cs`:

```csharp
namespace MangakaSim;

// What each career goal measures (spec 2026-10-01). Internal, so saves never serialize them.
public partial class GameState
{
    internal IEnumerable<Series> GoalTitles => Series.Where(s => s.BusinessId == ControlledBusinessId);
    internal bool GoalTitle(int? seriesId) => seriesId is { } id && FindSeries(id)?.BusinessId == ControlledBusinessId;

    internal static GoalProgress GoalCount(double have, double need) => new(have, need, $"{Math.Min(have, need):N0} of {need:N0}");
    internal static GoalProgress GoalYen(double have, double need) => new(have, need, $"¥{Math.Min(have, need):N0} of ¥{need:N0}");
    internal static GoalProgress GoalFlag(bool done, string waiting) => new(done ? 1 : 0, 1, done ? "Done" : waiting);

    internal bool GoalDoujinFinished => GoalTitles.Any(s => s.Volumes.Any(v => v.IsDoujin));
    internal long GoalCopiesSold => GoalTitles.Sum(s => SeriesCopiesSold(s.Id));
    internal int GoalConventions => Bookings.Count(b => b.BusinessId == ControlledBusinessId && b.Settled && !b.Cancelled && b.StaffedHours > 0);
    internal double GoalFans => Math.Floor(GoalTitles.Sum(s => s.Fanbase));
    internal long GoalOwnSales => Ledger.Where(e => e.Amount > 0 && e.Kind == AccountEntryKind.Publishing &&
        (e.Reason == "doujin sales" || e.Reason == "domestic digital receipts" && e.SeriesId is { } id && FindSeries(id)?.Volumes.Any(v => v.IsDoujin) == true)).Sum(e => e.Amount);
    internal bool GoalPitchedOrEntered => Events.Any(e => e.Type == EventType.PitchSubmitted && GoalTitle(e.SeriesId)) ||
        Progression.Awards.Any(a => a.ManuscriptId > 0 && GoalTitle(a.SeriesId));
    internal bool GoalVerdict => Events.Any(e => e.Type is EventType.PitchRejected or EventType.SerializationOffered && GoalTitle(e.SeriesId));
    internal bool GoalMilestone(string key) => Progression.Milestones.Any(m => m.Key == key);
    internal bool GoalSerializedOrPlaced => GoalTitles.Any(s => s.Publishing == PublishingStatus.Serialized || s.PastContracts.Count > 0) || GoalMilestone("contest_placement");
    internal int GoalMagazineChapters => GoalTitles.Sum(s => s.ChaptersPublished);
    internal GoalProgress GoalTopRank
    {
        get
        {
            var best = GoalTitles.SelectMany(s => s.Chapters).Where(c => c.Rank is not null).Select(c => c.Rank!.Value).DefaultIfEmpty(0).Min();
            return best == 0 ? new(0, 1, "No ranking yet") : new(best <= 5 ? 1 : 0, 1, $"Best rank so far: #{best}");
        }
    }
    internal bool GoalCollectedVolume => GoalTitles.Any(s => s.Volumes.Any(v => !v.IsDoujin && v.ReleasedAt is not null));
    internal int GoalStaff => ControlledStaff.Count(p => p.Id != ProtagonistPersonId);
    internal double GoalTopReaders => Math.Floor(GoalTitles.Where(s => s.LeadPersonId == ProtagonistPersonId).Select(s => s.Fanbase).DefaultIfEmpty(0).Max());
    internal bool GoalWorksFromStudio => Protagonist.Employment is { } e &&
        Locations.Any(l => l.Id == e.LocationId && l.BusinessId == ControlledBusinessId && !l.IsFamilyHome && l.PropertyOfferId > 0);
    internal int GoalActiveTitles => GoalTitles.Count(s => s.Status == SeriesStatus.Active);
    internal bool GoalShortlisted => Progression.Awards.Any(a => a.Award.StartsWith("annual:", StringComparison.Ordinal) && GoalTitle(a.SeriesId));
    internal int GoalReleasedBooks => GoalTitles.Sum(s => s.Volumes.Count(v => v.ReleasedAt is not null));
    internal GoalProgress GoalMilestoneOr(string key, GoalProgress progress) => GoalMilestone(key) ? new(1, 1, "Done") : progress;
}
```

- [ ] **Step 5: Write the catalogue**

`src/MangakaSim/Goals.cs`:

```csharp
namespace MangakaSim;

/// <summary>How far a goal has come, its target and the words the board shows (career goals spec 2026-10-01).</summary>
public sealed record GoalProgress(double Have, double Need, string Text)
{
    public bool Done => Have >= Need;
    public double Fraction => Need <= 0 ? 1 : Math.Clamp(Have / Need, 0, 1);
}

/// <summary>A one-off reward (Q49): cash, fans, an item, an unlock, an opportunity or a scene. Never a permanent bonus.</summary>
public sealed record GoalReward(long Cash = 0, int Fans = 0, string? Furniture = null, string? Unlock = null,
    bool FreeConventionTable = false, bool PitchBoost = false, string? Scene = null)
{
    public string Describe()
    {
        var parts = new List<string>();
        if (Cash > 0) parts.Add($"¥{Cash:N0}");
        if (Fans > 0) parts.Add($"{Fans:N0} fans for your newest series");
        if (Furniture is { } item) parts.Add("a " + OfficeCatalog.Get(item).Name.ToLowerInvariant());
        if (Unlock is { } unlock) parts.Add($"the {OfficeCatalog.Get(unlock).Name.ToLowerInvariant()} unlocked");
        if (FreeConventionTable) parts.Add("a free convention table");
        if (PitchBoost) parts.Add("a better chance on your next pitch");
        if (Scene is not null) parts.Add("a scene with Helper-Chan");
        return string.Join(", ", parts);
    }
}

public sealed record GoalDefinition(string Id, int Chapter, string Title, string Why, string Target, string Tip, GoalReward Reward, Func<GameState, GoalProgress> Measure);
public sealed record GoalChapter(int Index, string Name, string Blurb, GoalReward Reward);

/// <summary>Five career chapters, then optional mastery goals (Q50). Targets are Helper-Chan's guidance routes.</summary>
public static class GoalCatalog
{
    private static readonly GoalReward Trophy = new(Furniture: "trophy");

    public static readonly GoalChapter[] Chapters =
    [
        new(0, "Doujin Days", "Your parents' house, your first pages and your first readers.", new(Furniture: "trophy-shelf", FreeConventionTable: true, Scene: "goal-doujin-days")),
        new(1, "Rookie", "Getting noticed: editors, contests and a first chance in a magazine.", new(Unlock: "desk-studio", PitchBoost: true, Scene: "goal-rookie")),
        new(2, "Serialized", "Real deadlines, rankings and your first collected volume.", new(Cash: 100_000, Furniture: "award-plaque", Scene: "goal-serialized")),
        new(3, "Studio Head", "Your own studio, a team and a company.", new(Cash: 300_000, Furniture: "display-cabinet", Scene: "goal-studio-head")),
        new(4, "Legend", "The milestones that put a mangaka in manga history.", new(Scene: "goal-legend")),
        new(5, "Mastery", "Optional goals for a long career. Each gives a trophy.", new()),
    ];

    public static readonly GoalDefinition[] Goals =
    [
        new("doujin-finished", 0, "Finish your first doujin", "A finished book is something you can print, sell and pitch.", "production",
            "Pages move through each stage by themselves while time runs. Speed up time and watch Production.", new(Cash: 5_000),
            s => GameState.GoalFlag(s.GoalDoujinFinished, "Not finished yet")),
        new("first-copy", 0, "Sell your first copy", "Your first reader! Every sale also brings fans.", "books",
            "Print ten copies at the copy shop, or list the book online for free. Sales settle every Monday.", new(Furniture: "print"),
            s => GameState.GoalCount(s.GoalCopiesSold, 1)),
        new("convention", 0, "Attend a convention", "Conventions sell printed copies fast and bring new fans.", "conventions",
            "Book a table on the Conventions page and bring printed copies. You sell them on the day and meet new readers.", new(Cash: 10_000),
            s => GameState.GoalCount(s.GoalConventions, 1)),
        new("fans-100", 0, "Reach 100 fans", "Fans buy your next book and impress editors.", "books",
            "Fans grow with every copy sold and every convention. Good quality and a popular genre help most.", new(Furniture: "plant"),
            s => GameState.GoalCount(s.GoalFans, 100)),
        new("sales-50000", 0, "Earn ¥50,000 from your own sales", "Your own sales fund printing, conventions and your first hire.", "books",
            "Reprint books that sell out and keep an online listing going. Every copy adds up.", new(Cash: 10_000),
            s => GameState.GoalYen(s.GoalOwnSales, 50_000)),

        new("pitch-or-contest", 1, "Pitch a magazine or enter a contest", "Magazines bring page fees and many more readers.", "publishing",
            "Make a doujin an ongoing series in its Series details, then pitch it on Publishing. Or enter a contest in Awards.", new(Cash: 10_000),
            s => GameState.GoalFlag(s.GoalPitchedOrEntered, "Not yet")),
        new("editor-verdict", 1, "Hear an editor's verdict", "Every answer teaches you something, even a no.", "publishing",
            "Editors answer a pitch within a few weeks. Keep drawing meanwhile; a rejection says what to improve.", new(Furniture: "framed-letter"),
            s => GameState.GoalFlag(s.GoalVerdict, "Waiting for a pitch")),
        new("serialization-or-placement", 1, "Win a serialization or place in a contest", "This is your start as a professional.", "publishing",
            "A strong sample in a fitting magazine has the best chance. Contest placings count too, and impress editors.", new(Cash: 30_000),
            s => GameState.GoalFlag(s.GoalSerializedOrPlaced, "Not yet")),
        new("fans-1000", 1, "Reach 1,000 fans", "A bigger audience makes every pitch and book easier.", "books",
            "Keep books in print and online, and go to conventions. A serialization grows fans fastest.", new(Fans: 200),
            s => GameState.GoalCount(s.GoalFans, 1_000)),

        new("chapters-10", 2, "Publish 10 magazine chapters", "Steady chapters keep your series safe in the rankings.", "production",
            "Deliver each chapter by its issue close. Keep a chapter or two ready ahead for slow weeks.", new(Cash: 30_000),
            s => GameState.GoalCount(s.GoalMagazineChapters, 10)),
        new("top-5", 2, "Reach the top 5", "A top-5 series is safe from cancellation.", "production",
            "Quality and steady chapters lift your ranking. A genre in fashion helps too.", new(Furniture: "ranking-chart"),
            s => s.GoalTopRank),
        new("first-volume", 2, "Release your first collected volume", "Collected volumes earn royalties for years.", "production",
            "Every five magazine chapters make a collected volume. Keep the chapters coming and it follows.", new(Cash: 50_000),
            s => GameState.GoalFlag(s.GoalCollectedVolume, "Not yet")),
        new("first-hire", 2, "Hire your first assistant", "An assistant takes Backgrounds and Tones so you keep up.", "recruitment",
            "Hire once funds cover about three months of wages. The runway line shows what each wage does.", new(Furniture: "chair-support"),
            s => GameState.GoalCount(s.GoalStaff, 1)),
        new("readers-10000", 2, "Reach 10,000 readers", "Ten thousand readers is a real hit.", "production",
            "Steady quality and a long run grow readers. Rankings and collected volumes bring more.", new(Fans: 1_000),
            s => s.GoalMilestoneOr("readers_10000", GameState.GoalCount(s.GoalTopReaders, 10_000))),

        new("moved-out", 3, "Move out of your parents' house", "Your own studio has room for a team.", "studios",
            "Studios on the Studios page show rent and desks. Move once income covers the rent with room to spare.", new(Furniture: "plant-set"),
            s => GameState.GoalFlag(s.GoalWorksFromStudio, "Still at home")),
        new("staff-3", 3, "Employ 3 staff", "A team can run two series.", "recruitment",
            "Each hire needs a desk and wages. Check the runway before every offer.", new(Cash: 50_000),
            s => GameState.GoalCount(s.GoalStaff, 3)),
        new("incorporated", 3, "Incorporate", "A company can borrow and grow.", "business",
            "Incorporation costs ¥200,000 and opens business credit after 90 days. Find it in Finances.", new(Furniture: "company-sign"),
            s => GameState.GoalFlag(s.ControlledBusiness.Incorporated, "Not yet")),
        new("two-series", 3, "Run two series at once", "Two series double your chances in the rankings.", "series",
            "Start a second ongoing series once your team has spare desks. Assign staff to each title.", new(Cash: 80_000),
            s => GameState.GoalCount(s.GoalActiveTitles, 2)),
        new("award-shortlist", 3, "Be shortlisted for the Manga Craft Award", "A shortlist brings readers and respect.", "awards",
            "Each January the award shortlists the year's best work. High quality chapters are what count.", new(Furniture: "gold-frame"),
            s => GameState.GoalFlag(s.GoalShortlisted, "Not yet")),

        new("anime", 4, "An anime adaptation", "Anime brings readers who never read manga.", "licenses",
            "Licence offers come to popular series. Compare fit and terms on the Licenses page.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("anime_release"), "Not yet")),
        new("merchandise", 4, "A merchandise deal", "Merchandise keeps your characters in people's hands.", "licenses",
            "Merchandise offers follow popular series too. A steady fanbase makes them likelier.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("merchandise_license"), "Not yet")),
        new("readers-million", 4, "A million readers", "A million readers is a national hit.", "production",
            "Long runs, collected volumes and adaptations all grow readers.", Trophy,
            s => s.GoalMilestoneOr("readers_million", GameState.GoalCount(s.GoalTopReaders, 1_000_000))),
        new("iconic", 4, "An iconic series", "An iconic series is part of manga history.", "production",
            "Iconic series combine a huge readership with lasting cultural impact. Awards and anime help.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("iconic_series"), "Not yet")),
        new("sustainable", 4, "A sustainable studio year", "A profitable year with every bill paid.", "business",
            "Keep costs below income for a full year and pay every bill and loan on time.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("sustainable_studio"), "Not yet")),

        new("annual-award", 5, "Win the Manga Craft Award", "The highest honour of the year.", "awards",
            "The award goes to the year's best work. Top quality chapters all year give you the best chance.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("annual_award"), "Not yet")),
        new("anime-followup", 5, "A second anime season", "A second season means the first one worked.", "licenses",
            "A successful first anime brings follow-up offers. Keep the series strong meanwhile.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("anime_followup"), "Not yet")),
        new("five-books", 5, "Release five books", "A shelf of your own books.", "production",
            "Doujin and collected volumes both count. Keep finishing work and it adds up.", Trophy,
            s => GameState.GoalCount(s.GoalReleasedBooks, 5)),
    ];

    public static IEnumerable<GoalDefinition> In(int chapter) => Goals.Where(g => g.Chapter == chapter);
    public static GoalDefinition Get(string id) => Goals.First(g => g.Id == id);
    /// <summary>The chapter whose reward unlocks a furniture kind, or null.</summary>
    public static string? UnlockedBy(string kind) => Chapters.FirstOrDefault(c => c.Reward.Unlock == kind)?.Name;
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~GoalCatalogTests"`
Expected: PASS, 6 tests. If a tip is over 140 characters, shorten its words (a ruling is not needed for wording).

---

### Task 2: Goal state, hourly checks and rewards

**Files:**
- Create: `src/MangakaSim/GameState.Goals.cs`
- Modify: `src/MangakaSim/EventType.cs` (append two events), `src/MangakaSim/Studio.cs:8` (append `GoalReward`)
- Modify: `src/MangakaSim/GameState.cs` (`Tick` after `DayEndStep();`, `NewGame` before `return state;`)
- Modify: `src/MangakaSim/Career.cs` (`HelperStories` chapter scenes), `src/MangakaSim/GameState.Career.cs` (`ApplyStory` dequeues goal scenes)
- Modify: `src/MangakaSim/GameState.Serialization.cs` (`ValidateSave` calls `ValidateGoals`)
- Test: `tests/MangakaSim.Tests/GoalProgressTests.cs`

**Interfaces:**
- Consumes: Task 1 catalogue.
- Produces: `sealed record GoalRecord(string Id, DateTime At, bool Backfilled)`; `sealed class GoalsState { int Chapter; List<GoalRecord> Completed; List<string> Unlocked; int FreeConventionTables; bool PitchBoost; List<string> PendingScenes; }`; `GameState.Goals` (`GoalsState?`); `bool GameState.GoalDone(string id)`; `internal void GameState.EvaluateGoals(bool backfill = false)`; `EventType.GoalCompleted`, `EventType.GoalChapterCompleted`; `AccountEntryKind.GoalReward`; `HelperStories.Chapters`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/GoalProgressTests.cs`:

```csharp
using Xunit;
namespace MangakaSim.Tests;

// Career goals board (spec 2026-10-01): goals complete once, pay once, and chapters open in order.
public class GoalProgressTests
{
    internal static GameState FirstSale(int seed = 0, bool goals = true)
    {
        var s = GameState.NewGame(seed); if (!goals) s.Goals = null;
        s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && s.Series[0].Volumes.Count == 0; d++) s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes.Single().Id, Amount: 10, Value: (int)PrintTier.CopyShop));
        for (var d = 0; d < 30 && s.Series[0].Volumes.Single().CopiesSold == 0; d++) s.Advance(24);
        s.Advance(2);
        return s;
    }

    [Fact] public void Goals_complete_with_their_reward_exactly_once()
    {
        var prints = GameState.NewGame(0).Furniture.Count(f => f.Kind == "print");
        var s = FirstSale();
        Assert.True(s.GoalDone("doujin-finished") && s.GoalDone("first-copy"));
        s.Advance(24 * 7);
        Assert.Single(s.Ledger, e => e.Reason == "Goal reward: Finish your first doujin" && e.Amount == 5_000 && e.Kind == AccountEntryKind.GoalReward);
        Assert.Single(s.Events, e => e.Type == EventType.GoalCompleted && e.Message.StartsWith("Goal complete: Sell your first copy"));
        Assert.Equal(prints + 1, s.Furniture.Count(f => f.Kind == "print"));
    }

    [Fact] public void Finishing_a_chapter_opens_the_next_with_the_chapter_reward_and_scene()
    {
        var s = FirstSale();
        foreach (var id in new[] { "convention", "fans-100", "sales-50000" }) s.Goals!.Completed.Add(new(id, s.Clock.Now, false));
        s.Advance(1);
        Assert.Equal(1, s.Goals!.Chapter);
        Assert.Single(s.Events, e => e.Type == EventType.GoalChapterCompleted && e.Message.StartsWith("Chapter complete: Doujin Days"));
        Assert.Equal(1, s.Goals.FreeConventionTables);
        Assert.Contains(s.Furniture, f => f.Kind == "trophy-shelf" && f.Paid == 0);
        Assert.True(s.Career.PendingScene == "goal-doujin-days" || s.Goals.PendingScenes.Contains("goal-doujin-days"));
    }

    [Fact] public void A_queued_chapter_scene_follows_the_scene_in_front_of_it()
    {
        var s = FirstSale();
        s.Career.PendingScene = "beside"; s.Goals!.PendingScenes.Add("goal-doujin-days");
        s.Apply(new StoryCommand("beside", 0));
        Assert.Equal("goal-doujin-days", s.Career.PendingScene);
        Assert.Empty(s.Goals.PendingScenes);
    }

    [Fact] public void Goals_survive_a_save_and_load_and_play_the_same_twice()
    {
        var a = FirstSale(7); var b = FirstSale(7);
        Assert.Equal(a.ToJson(), b.ToJson());
        var loaded = GameState.FromJson(a.ToJson());
        Assert.Equal(a.Goals!.Completed.Count, loaded.Goals!.Completed.Count);
        Assert.Contains(loaded.Furniture, f => f.Kind == "print" && f.Paid == 0);
    }

    [Fact] public void A_save_naming_an_unknown_goal_is_refused()
    {
        var s = FirstSale(); s.Goals!.Completed.Add(new("not-a-goal", s.Clock.Now, false));
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(s.ToJson()));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~GoalProgressTests"`
Expected: build FAILS with `CS1061: 'GameState' does not contain a definition for 'Goals'`.

- [ ] **Step 3: Add the events, the ledger kind and the chapter scenes**

In `src/MangakaSim/EventType.cs`, after `AwardNomination, AwardResult, LicenseOffered, LicenseDecision, LicenseReleased, CareerMilestone,` add a line:

```csharp
    GoalCompleted, GoalChapterCompleted,
```

In `src/MangakaSim/Studio.cs:8`, change the enum to end `LicenseIncome, AwardPrize, GoalReward }`.

In `src/MangakaSim/Career.cs`, in `HelperStories`, add below `Everyday`:

```csharp
    public static readonly string[] Chapters = ["goal-doujin-days", "goal-rookie", "goal-serialized", "goal-studio-head", "goal-legend"];
```

change `Known` to:

```csharp
    public static bool Known(string id) => Arc.Contains(id) || Everyday.Contains(id) || Chapters.Contains(id);
```

and add these cases to the `Describe` switch, before the `_ =>` arm:

```csharp
            "goal-doujin-days" => new(id,"Doujin Days, done","Your first book, your first readers, your first convention. I put the first copy on the shelf where everyone can see it.","Let's aim for a magazine","Let's enjoy this for a moment","happy"),
            "goal-rookie" => new(id,"A professional now","Someone out there read your pages and said yes. Now they're waiting for your next chapter.","I won't keep them waiting","I'm nervous, honestly","happy"),
            "goal-serialized" => new(id,"Ten chapters and counting","Deadlines, rankings, a collected volume on real shelves. You kept every promise. The plaque is crooked; I'm leaving it.","Next, our own studio","Thank you for keeping track","happy"),
            "goal-studio-head" => new(id,"A studio of your own","A sign on the door, a team at the desks, a company in your name. I still get the desk beside yours, right?","Always","Who else would I want there?","happy"),
            "goal-legend" => new(id,"Part of manga history","Anime, merchandise, a million readers. People will remember these pages. I kept the first one, you know.","Let's keep making them","Thank you for staying","happy"),
```

- [ ] **Step 4: Write the goal state and the checks**

`src/MangakaSim/GameState.Goals.cs`:

```csharp
namespace MangakaSim;

public sealed record GoalRecord(string Id, DateTime At, bool Backfilled);

/// <summary>The career goals board's saved state (spec 2026-10-01). Absent from saves written before it (see EnsureGoals).</summary>
public sealed class GoalsState
{
    public int Chapter { get; set; }
    public List<GoalRecord> Completed { get; set; } = new();
    public List<string> Unlocked { get; set; } = new();
    public int FreeConventionTables { get; set; }
    public bool PitchBoost { get; set; }
    public List<string> PendingScenes { get; set; } = new();
}

public partial class GameState
{
    /// <summary>Null only in saves written before the goals board, until EnsureGoals backfills it on load.</summary>
    public GoalsState? Goals { get; set; }

    public bool GoalDone(string id) => Goals?.Completed.Any(r => r.Id == id) == true;

    /// <summary>Checks the current chapter's goals, grants each reward once and opens the next chapter when all are done.
    /// A backfill (older saves) grants unlocks and decorations only, with no events.</summary>
    internal void EvaluateGoals(bool backfill = false)
    {
        if (Goals is not { } goals) return;
        var progressed = true;
        while (progressed && goals.Chapter < GoalCatalog.Chapters.Length)
        {
            progressed = false;
            foreach (var goal in GoalCatalog.In(goals.Chapter))
            {
                if (goals.Completed.Any(r => r.Id == goal.Id) || !goal.Measure(this).Done) continue;
                goals.Completed.Add(new(goal.Id, Clock.Now, backfill));
                GrantGoalReward(goal.Reward, backfill, "Goal reward: " + goal.Title);
                if (!backfill) Emit(EventType.GoalCompleted, $"Goal complete: {goal.Title}. Reward: {goal.Reward.Describe()}.", personId: ProtagonistPersonId);
            }
            if (!GoalCatalog.In(goals.Chapter).All(g => goals.Completed.Any(r => r.Id == g.Id))) break;
            var chapter = GoalCatalog.Chapters[goals.Chapter];
            GrantGoalReward(chapter.Reward, backfill, "Chapter reward: " + chapter.Name);
            if (!backfill)
            {
                var reward = chapter.Reward.Describe();
                Emit(EventType.GoalChapterCompleted, reward.Length > 0 ? $"Chapter complete: {chapter.Name}! Reward: {reward}." : $"Chapter complete: {chapter.Name}!", personId: ProtagonistPersonId);
                if (chapter.Reward.Scene is { } scene) QueueGoalScene(scene);
            }
            goals.Chapter++; progressed = true;
        }
    }

    private void GrantGoalReward(GoalReward reward, bool backfill, string reason)
    {
        var goals = Goals!;
        if (!backfill && reward.Cash > 0) AccountPost(ControlledBusiness.Account, reward.Cash, reason, AccountEntryKind.GoalReward);
        if (!backfill && reward.Fans > 0 && GoalTitles.OrderBy(s => s.Id).LastOrDefault() is { } newest) newest.Fanbase += reward.Fans;
        if (reward.Furniture is { } kind)
            Furniture.Add(new() { Id = NextFurnitureId++, Kind = kind, Owner = FurnitureOwner.Business, BusinessId = ControlledBusinessId, Paid = 0 });
        if (reward.Unlock is { } unlock && !goals.Unlocked.Contains(unlock)) goals.Unlocked.Add(unlock);
        if (!backfill && reward.FreeConventionTable) goals.FreeConventionTables++;
        if (!backfill && reward.PitchBoost) goals.PitchBoost = true;
    }

    private void QueueGoalScene(string scene)
    {
        if (Career.PendingScene is null) Career.PendingScene = scene;
        else Goals!.PendingScenes.Add(scene);
    }

    private void ValidateGoals()
    {
        if (Goals is not { } g) return;
        static void Check(bool ok, string field) { if (!ok) throw new InvalidDataException($"Save file has invalid goals {field}."); }
        Check(g.Chapter >= 0 && g.Chapter <= GoalCatalog.Chapters.Length, "chapter");
        Check(g.Completed is not null && g.Completed.All(r => r is not null && GoalCatalog.Goals.Any(x => x.Id == r.Id)) &&
            g.Completed.Select(r => r.Id).Distinct().Count() == g.Completed.Count, "records");
        Check(g.Unlocked is not null && g.Unlocked.All(k => OfficeCatalog.Furniture.Any(f => f.Id == k)), "unlocks");
        Check(g.FreeConventionTables >= 0 && g.PendingScenes is not null && g.PendingScenes.All(HelperStories.Known), "opportunities");
    }
}
```

- [ ] **Step 5: Wire it in**

In `src/MangakaSim/GameState.cs`, in `Tick()`, after `DayEndStep();` add:

```csharp
        EvaluateGoals();
```

and in `NewGame`, directly before `return state;` (line 45), add:

```csharp
        state.Goals = new();
```

In `src/MangakaSim/GameState.Serialization.cs`, in `ValidateSave()`, after `RepairSupersededDrafts();` add:

```csharp
        ValidateGoals();
```

In `src/MangakaSim/GameState.Career.cs`, in `ApplyStory`, after `Career.PendingScene=null;Career.DeferredUntil=null;Career.NextOffer=Clock.Now.AddDays(7);` add:

```csharp
        if(Goals is {PendingScenes.Count:>0} goals){Career.PendingScene=goals.PendingScenes[0];goals.PendingScenes.RemoveAt(0);}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~GoalProgressTests|FullyQualifiedName~GoalCatalogTests"`
Expected: PASS, 11 tests.

- [ ] **Step 7: Run the whole unit suite**

Run: `dotnet test tests/MangakaSim.Tests`
Expected: all pass (683 before this plan, plus the new tests). A failing replay or save test means the goals changed a deterministic path; find the cause before going on.

---

### Task 3: Older saves

**Files:**
- Modify: `src/MangakaSim/GameState.Goals.cs` (add `EnsureGoals`)
- Modify: `src/MangakaSim/GameState.Serialization.cs` (`FromJson` calls it), `src/MangakaSim/GameState.Career.cs` (`ImportSupported` calls it)
- Test: `tests/MangakaSim.Tests/OldSaveGoalsTests.cs`

**Interfaces:**
- Consumes: `EvaluateGoals(bool backfill)`, `GoalProgressTests.FirstSale(int seed, bool goals)` (Task 2).
- Produces: `internal void GameState.EnsureGoals()`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/OldSaveGoalsTests.cs`:

```csharp
using System.Text.Json.Nodes;
using Xunit;
namespace MangakaSim.Tests;

// Career goals board (spec 2026-10-01): saves written before the board count goals already met, without a windfall.
public class OldSaveGoalsTests
{
    static void AssertBackfilled(GameState before, GameState loaded)
    {
        Assert.True(loaded.GoalDone("doujin-finished") && loaded.GoalDone("first-copy"));
        Assert.All(loaded.Goals!.Completed, r => Assert.True(r.Backfilled));
        Assert.DoesNotContain(loaded.Ledger, e => e.Kind == AccountEntryKind.GoalReward);
        Assert.DoesNotContain(loaded.Events, e => e.Type is EventType.GoalCompleted or EventType.GoalChapterCompleted);
        Assert.Equal(before.Furniture.Count(f => f.Kind == "print") + 1, loaded.Furniture.Count(f => f.Kind == "print"));
        Assert.Equal(before.Money, loaded.Money);
        Assert.Equal(loaded.ToJson(), loaded.ReplayTimeline().ToJson());
    }

    [Fact] public void A_save_with_no_goals_counts_goals_already_met_without_cash_fans_or_events()
    {
        var old = GoalProgressTests.FirstSale(goals: false);
        AssertBackfilled(old, GameState.FromJson(old.ToJson()));
    }

    [Fact] public void A_save_written_before_the_goals_property_existed_loads_the_same_way()
    {
        var old = GoalProgressTests.FirstSale(goals: false);
        var node = JsonNode.Parse(old.ToJson())!.AsObject(); node.Remove("Goals");
        AssertBackfilled(old, GameState.FromJson(node.ToJsonString()));
    }

    [Fact] public void Play_after_a_backfill_completes_new_goals_with_their_rewards()
    {
        var loaded = GameState.FromJson(GoalProgressTests.FirstSale(goals: false).ToJson());
        loaded.Goals!.Completed.RemoveAll(r => r.Id == "first-copy");
        loaded.Advance(1);
        Assert.Contains(loaded.Events, e => e.Type == EventType.GoalCompleted && e.Message.StartsWith("Goal complete: Sell your first copy"));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~OldSaveGoalsTests"`
Expected: FAIL; the loaded state has `Goals == null`, so `GoalDone` is false (`Assert.True() Failure`).

- [ ] **Step 3: Backfill on load**

In `src/MangakaSim/GameState.Goals.cs`, add inside the `GameState` partial class:

```csharp
    /// <summary>Older saves have no goals: goals already met count as done, with their unlocks and decorations but no cash,
    /// fans, opportunities or events (spec 2026-10-01). The replay checkpoint is rebased so replays match the backfilled state.</summary>
    internal void EnsureGoals()
    {
        if (Goals is not null) return;
        Goals = new();
        EvaluateGoals(backfill: true);
        World.ReplayCheckpoint = null; World.ReplayLogStart = CommandLog.Count;
        ValidateSave(); World.ReplayCheckpoint = ToJson();
    }
```

In `src/MangakaSim/GameState.Serialization.cs`, in `FromJson`, change `try { state.ValidateSave(); }` to:

```csharp
        try { state.ValidateSave(); state.EnsureGoals(); }
```

In `src/MangakaSim/GameState.Career.cs`, replace the body of `ImportSupported` after the `if(...)return FromJson(json);` line with:

```csharp
        var imported = number switch
        {3=>ImportStudioV3(json),4=>ImportTimelineV4(json),5=>ImportCareerV5(json),6=>ImportProgressionV6(json),7=>ImportAlphaV7(json),8=>ImportProductionV8(json),9=>ImportConvenienceV9(json),_=>FromJson(json)};
        imported.EnsureGoals();
        return imported;
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~OldSaveGoalsTests|FullyQualifiedName~OldSaveTests|FullyQualifiedName~ProgressionTests"`
Expected: PASS, including the existing old-save and progression tests.

---

### Task 4: Unlocks and one-off opportunities

**Files:**
- Modify: `src/MangakaSim/GameState.Progression.cs:13-19` (`EquipmentAvailable`)
- Modify: `src/MangakaSim/GameState.Outreach.cs:38-41` (free table), `:56-61` (`CancelBooking` returns it)
- Modify: `src/MangakaSim/GameState.Publishing.cs:93-94` (pitch bonus), `src/MangakaSim/Guidance.cs:227-229` (`PitchOutlooks`)
- Test: `tests/MangakaSim.Tests/GoalOpportunityTests.cs`

**Interfaces:**
- Consumes: `GoalsState.Unlocked`, `FreeConventionTables`, `PitchBoost` (Task 2).
- Produces: `internal double GameState.TakePitchBoost(Series series)`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/GoalOpportunityTests.cs`:

```csharp
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

    static ConventionBooking Book(GameState s)
    {
        for (var d = 0; d < 60; d++)
        {
            try { s.Apply(new StudioActionCommand(StudioAction.BookConvention, s.ProtagonistPersonId, Value: 1)); return s.Bookings.Last(); }
            catch (InvalidCommandException ex) when (ex.Message.StartsWith("Booking opens")) { s.Advance(24); }
        }
        throw new InvalidOperationException("No convention opened for booking.");
    }

    [Fact] public void A_free_table_is_used_once_and_comes_back_when_cancelled_in_time()
    {
        var s = GameState.NewGame(0); s.Goals!.FreeConventionTables = 1;
        var free = Book(s);
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
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~GoalOpportunityTests"`
Expected: build FAILS with `CS1061: 'GameState' does not contain a definition for 'TakePitchBoost'`.

- [ ] **Step 3: Implement the unlock, the free table and the bonus**

In `src/MangakaSim/GameState.Progression.cs`, change the `desk-studio` arm to:

```csharp
        "desk-studio" => Assist(SandboxAssist.UnlockEquipment) || Goals?.Unlocked.Contains("desk-studio") == true || ControlledBusiness.TrackRecord >= 20,
```

(The reputation route stays so no existing career loses the desk; the catalogue names the Rookie chapter.)

In `src/MangakaSim/GameState.Outreach.cs`, in the `BookConvention` branch, after `var reserved=AllocateConventionStock(title?.Id,date,c.ReservedCopies);` add:

```csharp
            // A free table from the Doujin Days chapter (spec 2026-10-01) pays this booking's fee.
            if(fee>0&&Goals is {FreeConventionTables:>0} goals){fee=0;goals.FreeConventionTables--;}
```

In `CancelBooking`, replace the refund line with:

```csharp
        if (Clock.Now.Date<=b.Date.AddDays(-7))
        {
            AccountPost(BusinessOf(b.BusinessId).Account,b.Fee+b.TravelCost,"convention refund",AccountEntryKind.Credit);
            if(b.Fee==0&&b.Scale>0&&b.BusinessId==ControlledBusinessId&&Goals is not null)Goals.FreeConventionTables++; // the free table comes back
        }
```

In `src/MangakaSim/GameState.Publishing.cs`, replace the roll line (`if (Rng.NextDouble() < Math.Min(.95, PitchRules.Chance(...) * PitchFactor(series.BusinessId) + recognition))`) with:

```csharp
            var boost = TakePitchBoost(series);
            if (Rng.NextDouble() < Math.Min(.95, PitchRules.Chance(magazine.Tier, quality, reputation, affinity, trend) * PitchFactor(series.BusinessId) + recognition + boost))
```

and add this method to the same partial class:

```csharp
    /// <summary>The Rookie chapter's one-off pitch bonus (spec 2026-10-01): used by the player's own next pitch only.</summary>
    internal double TakePitchBoost(Series series)
    {
        if (series.BusinessId != ControlledBusinessId || Goals is not { PitchBoost: true } goals) return 0;
        goals.PitchBoost = false; return .1;
    }
```

In `src/MangakaSim/Guidance.cs`, in `PitchOutlooks`, change the chance expression to add the bonus:

```csharp
                Math.Min(.95, PitchRules.Chance(m.Tier, quality, reputation, series.IsIconic ? 1 : m.Affinity(genre), trend) * state.PitchFactor(series.BusinessId) + recognition +
                    (series.BusinessId == state.ControlledBusinessId && state.Goals?.PitchBoost == true ? .1 : 0)),
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~GoalOpportunityTests"`
Expected: PASS, 3 tests. If `new Series { ... }` needs more required members to compile, use the protagonist series of a second `GameState.NewGame(1)` whose business id differs, and record a ruling.

---

### Task 5: Helper-Chan's goal texts

**Files:**
- Modify: `src/MangakaSim/Guidance.cs` (`ObserveGoals`, texts, stopping counts, `NextGoal`, `Evaluate` after career-settled, `Observe`, `FastSpeedStops`)
- Modify: `src/MangakaSim/MusicMoments.cs` (chapter good news)
- Test: `tests/MangakaSim.Tests/GoalGuidanceTests.cs`

**Interfaces:**
- Consumes: `GoalsState`, `GoalCatalog`.
- Produces: `const string CareerGuidance.GoalStep = "goal"`; `static void CareerGuidance.ObserveGoals(GameState, GuidancePreferences)`; `static string GoalDoneText(GoalDefinition)`; `static string ChapterOpenText(GoalChapter)`; `static int StoppingMessages(GuidancePreferences)`; `static int UnreadStopping(GuidancePreferences)`; `static GoalDefinition? NextGoal(GameState)`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/GoalGuidanceTests.cs`:

```csharp
using Xunit;
namespace MangakaSim.Tests;

// Career goals board (spec 2026-10-01, Q51): Helper-Chan says how; goal texts never stop 32x.
public class GoalGuidanceTests
{
    [Fact] public void Helper_chan_congratulates_each_goal_once()
    {
        var s = GoalProgressTests.FirstSale(); var prefs = new GuidancePreferences();
        CareerGuidance.ObserveGoals(s, prefs); CareerGuidance.ObserveGoals(s, prefs);
        Assert.Single(prefs.Thread, m => m.Texts.Single() == CareerGuidance.GoalDoneText(GoalCatalog.Get("first-copy")));
        Assert.Single(prefs.Thread, m => m.Texts.Single() == CareerGuidance.ChapterOpenText(GoalCatalog.Chapters[0]));
    }

    [Fact] public void Goals_counted_from_an_older_save_pass_silently()
    {
        var loaded = GameState.FromJson(GoalProgressTests.FirstSale(goals: false).ToJson()); var prefs = new GuidancePreferences();
        CareerGuidance.ObserveGoals(loaded, prefs);
        Assert.DoesNotContain(prefs.Thread, m => m.Texts.Any(t => t.StartsWith("Goal done")));
    }

    [Fact] public void Every_goal_text_fits_a_phone_bubble()
    {
        foreach (var g in GoalCatalog.Goals) Assert.True(CareerGuidance.GoalDoneText(g).Length <= CareerGuidance.TextLimit, g.Id);
        foreach (var c in GoalCatalog.Chapters) Assert.True(CareerGuidance.ChapterOpenText(c).Length <= CareerGuidance.TextLimit, c.Name);
    }

    [Fact] public void Goal_texts_do_not_count_as_stopping_but_her_other_texts_still_do()
    {
        var prefs = new GuidancePreferences();
        var before = CareerGuidance.StoppingMessages(prefs);
        CareerGuidance.ObserveGoals(GoalProgressTests.FirstSale(), prefs);
        Assert.Equal(before, CareerGuidance.StoppingMessages(prefs)); Assert.Equal(0, CareerGuidance.UnreadStopping(prefs));
        CareerGuidance.Say(prefs, GameClock.Start, "A stopping notice.");
        Assert.Equal(before + 1, CareerGuidance.StoppingMessages(prefs)); Assert.Equal(1, CareerGuidance.UnreadStopping(prefs));
    }

    [Fact] public void A_goal_text_does_not_make_her_repeat_the_current_step()
    {
        var s = GoalProgressTests.FirstSale(); var prefs = new GuidancePreferences();
        CareerGuidance.Observe(s, prefs); var step = CareerGuidance.Evaluate(s, prefs).Id;
        CareerGuidance.Observe(s, prefs);
        Assert.Single(prefs.Thread, m => m.Step == step);
    }

    [Fact] public void The_next_goal_is_the_first_unfinished_in_catalogue_order()
    {
        var s = GoalProgressTests.FirstSale();
        Assert.Equal("convention", CareerGuidance.NextGoal(s)!.Id);
    }

    [Fact] public void A_finished_chapter_stops_32x_and_plays_good_news_but_a_goal_does_not()
    {
        Assert.Contains(EventType.GoalChapterCompleted, CareerGuidance.FastSpeedStops);
        Assert.DoesNotContain(EventType.GoalCompleted, CareerGuidance.FastSpeedStops);
        var s = GameState.NewGame(0);
        var chapter = new GameEvent { Time = s.Clock.Now, Type = EventType.GoalChapterCompleted, Message = "Chapter complete" };
        Assert.Contains(MusicMoment.GoodNews, MusicMoments.Classify(s, [chapter], []));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~GoalGuidanceTests"`
Expected: build FAILS with `CS0117: 'CareerGuidance' does not contain a definition for 'ObserveGoals'`.

- [ ] **Step 3: Implement the goal texts**

In `src/MangakaSim/Guidance.cs`, inside `CareerGuidance`, add:

```csharp
    /// <summary>Thread step for goal texts. They light the phone but never stop 32x (spec 2026-10-01).</summary>
    public const string GoalStep = "goal";
    public static string GoalDoneText(GoalDefinition goal) => $"Goal done: {goal.Title}! Your reward: {goal.Reward.Describe()}.";
    public static string ChapterOpenText(GoalChapter chapter) => $"New goals: {chapter.Name}! They're on the Goals page, with my tip for each.";
    public static int StoppingMessages(GuidancePreferences preferences) => preferences.Thread.Count(m => !m.Step.StartsWith(GoalStep, StringComparison.Ordinal));
    public static int UnreadStopping(GuidancePreferences preferences) => preferences.Thread.Count(m => !m.Read && !m.Step.StartsWith(GoalStep, StringComparison.Ordinal));

    /// <summary>The first unfinished goal of the current chapter, in catalogue order so her tip does not jump about.</summary>
    public static GoalDefinition? NextGoal(GameState state) =>
        state.Goals is { } goals && goals.Chapter < GoalCatalog.Chapters.Length ? GoalCatalog.In(goals.Chapter).FirstOrDefault(g => !state.GoalDone(g.Id)) : null;

    /// <summary>Congratulates each finished goal once and introduces each chapter once. Goals counted from an older save pass silently.</summary>
    public static void ObserveGoals(GameState state, GuidancePreferences preferences)
    {
        if (state.Goals is not { } goals) return;
        foreach (var record in goals.Completed)
        {
            if (!preferences.Completed.Add("goal:" + record.Id) || record.Backfilled) continue;
            Append(preferences, new() { Step = GoalStep, Time = record.At, Texts = [GoalDoneText(GoalCatalog.Get(record.Id))] });
        }
        if (goals.Chapter < GoalCatalog.Chapters.Length && preferences.Completed.Add("goal-chapter:" + goals.Chapter))
            Append(preferences, new() { Step = GoalStep, Time = state.Clock.Now, Texts = [ChapterOpenText(GoalCatalog.Chapters[goals.Chapter])] });
    }
```

In `Observe(GameState state, GuidancePreferences preferences)`, add as its first line:

```csharp
        ObserveGoals(state, preferences);
```

and in the same method change the `last` lookup to skip goal texts:

```csharp
        var last = preferences.Thread.LastOrDefault(m => m.Step is not ("notice" or ArrearsStep or QuietSpeedStep or GoalStep));
```

In `Evaluate`, replace the `return new("career-settled", ...)` statement (lines 127-128) with:

```csharp
            // Guidance carries on past the first hire with the board's next goal (Q51).
            if (NextGoal(state) is { } goal) return new($"{GoalStep}-next:{goal.Id}", goal.Title, goal.Tip, goal.Target, series.Id);
            return new("career-settled", "Your studio is running", "You have a serialization and a team. I'll message you when something needs you.\n" +
                "Contests and studio employment are side routes you can start from Help.", "help", series.Id);
```

and in the detour check near the top of `Evaluate`, change `!CalmCareerSteps.Contains(career.Id)` to:

```csharp
!CalmCareerSteps.Contains(career.Id) && !career.Id.StartsWith(GoalStep + "-next:", StringComparison.Ordinal)
```

In `FastSpeedStops`, add `EventType.GoalChapterCompleted,` after `EventType.WageArrears,`.

In `src/MangakaSim/MusicMoments.cs`, add a case to the switch:

```csharp
                case EventType.GoalChapterCompleted:
                    moments.Add(MusicMoment.GoodNews); break;
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~GoalGuidanceTests|FullyQualifiedName~Guidance"`
Expected: PASS, including the existing guidance tests. Goal texts use the exact step `goal`, which the `last` lookup skips; a next-goal tip has the step `goal-next:<id>`, which it still sees, so each tip appears once per goal.

---

### Task 6: The Goals page, rail badge, dashboard card and How?

**Files:**
- Create: `godot/DebugMain.Goals.cs`, `godot/DebugMain.GoalsSmoke.cs`
- Modify: `godot/DebugMain.Management.cs:147` (rail), `RefreshManagement` (badge, dashboard goals)
- Modify: `godot/DebugMain.ManagementPanels.cs` (`BuildManagementPage` case)
- Modify: `godot/DebugMain.ManagementMenus.cs:194` (restorable pages)
- Modify: `godot/DebugMain.OfficeDashboard.cs` (THIS CHAPTER card)
- Modify: `godot/DebugMain.Alpha.cs` (`ShowGuidance` split into `RouteGuidance` and `HighlightGuidance`; new targets)
- Modify: `godot/DebugMain.cs` (dispatch `--goals-smoke`), `godot/DebugMain.DisplaySweepSmoke.cs` (goals screen)

**Interfaces:**
- Consumes: Tasks 1, 2, 5.
- Produces: `void BuildGoals()`, `string GoalsRailText()`, `(int Done, int Total) GoalTally()`, `void ShowGoalTip(GoalDefinition)`, `(Control? Focus, string Hint) RouteGuidance(string target, int project)`, `void HighlightGuidance(Control? focus)`, `void RefreshDashboardGoals()`, fields `_dashboardGoalsTitle`, `_dashboardGoalsList`; smoke `RunGoalsSmoke`, `CheckGoalsBoard`.

- [ ] **Step 1: Write the failing smoke check**

`godot/DebugMain.GoalsSmoke.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Career goals board (spec 2026-10-01).
    private async void RunGoalsSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "goals-" + Guid.NewGuid().ToString("N")));
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            await CheckGoalsBoard();
            GD.Print($"GOALS SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => tree.Quit(); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"GOALS SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    private async Task CheckGoalsBoard()
    {
        Check(_navigation.ContainsKey("Goals") && _navigation["Goals"].Text.EndsWith("0/5"), $"The rail shows Goals with this chapter's count ({(_navigation.TryGetValue("Goals", out var g) ? g.Text : "missing")})");
        Press("Goals"); await SettleUi();
        Check(_page == "Goals" && _sideContent.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text == "Doujin Days")
            && _sideContent.FindChildren("GoalHow-*", "Button", true, false).Count == 5, "The Goals page shows the chapter and its five goals with How? buttons");
        // A career past its first sale: two goals done.
        _presentation = new() { Page = "Office" };
        var s = GameState.NewGame(0); s.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && s.Series[0].Volumes.Count == 0; d++) s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print, s.Series[0].Volumes.Single().Id, Amount: 10, Value: (int)PrintTier.CopyShop));
        for (var d = 0; d < 30 && !s.GoalDone("first-copy"); d++) s.Advance(24);
        _state = s; ResetManagementSession(); ShowOffice(); _helperPopup.Hide(); _popupEvents.Clear(); await SettleUi();
        Check(_navigation["Goals"].Text.EndsWith("2/5") && _dashboardGoalsTitle.Text == "Doujin Days" && _dashboardGoalsList.GetChildCount() > 0,
            $"The badge and the office's This chapter card follow progress ({_navigation["Goals"].Text}, {_dashboardGoalsTitle.Text})");
        Press("Goals"); await SettleUi();
        ((Button)_sideContent.FindChild("GoalHow-convention", true, false)!).EmitSignal(BaseButton.SignalName.Pressed); await SettleUi();
        Check(_page == "Conventions" && _presentation.Guidance.Thread.Last().Texts.SequenceEqual(new[] { GoalCatalog.Get("convention").Tip }),
            $"How? opens the right page with Helper-Chan's tip (page {_page})");
        foreach (var (size, name, scale) in new (Vector2I, string, double)[] { (new(1920, 1080), "1080", 1), (new(1280, 720), "720-150", 1.5) })
        {
            _presentation.UiScale = scale; ApplyTextScale(); GetWindow().Size = size; Press("Goals"); await SettleUi(); await SettleUi();
            await CaptureSmokeImage($"goals-{name}");
        }
        _presentation.UiScale = 1; ApplyTextScale(); GetWindow().Size = new(1920, 1080); await SettleUi();
    }
}
```

In `godot/DebugMain.cs`, after the `--tester-b-smoke` dispatch line, add:

```csharp
        if (OS.GetCmdlineUserArgs().Contains("--goals-smoke")) CallDeferred(nameof(RunGoalsSmoke));
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL with `CS0103` for `_dashboardGoalsTitle` and `_dashboardGoalsList`.

- [ ] **Step 3: Split Helper-Chan's routing so How? can reuse it**

In `godot/DebugMain.Alpha.cs`, in `ShowGuidance()`, cut everything from `Control? focus=null;string hint;` through the end of the `if(focus is not null){...}` highlight block, and put in its place:

```csharp
        var (focus,hint)=RouteGuidance(step.Target,step.Project);
        HighlightGuidance(focus);
```

(The `CareerGuidance.Say(...)`, `RefreshGuidance();` and `Notify(...)` lines after it stay.) Then add these two methods to the class. `RouteGuidance` holds the `switch(step.Target)` you cut, unchanged except that it switches on `target` and every `step.Project` becomes `project` (the printing, series, publishing and conventions cases), plus the four new cases shown before `default:`:

```csharp
    // Opens the screen for a guidance target and says what to do there; shared by "Show me" and the goals board's How? (spec 2026-10-01).
    private (Control? Focus,string Hint) RouteGuidance(string target,int project)
    {
        Control? focus=null;string hint;
        switch(target)
        {
            // ... the existing cases from ShowGuidance, with step.Project replaced by project ...
            case "books":Navigate("Books");hint="Your books, print runs and online listings are here.";break;
            case "studios":Navigate("Studios");hint="Studios for rent are listed here, with their desks and monthly rent.";break;
            case "licenses":Navigate("Licenses");hint="Licence offers for your series appear here. Compare the terms before you accept.";break;
            case "business":Navigate("Finances");hint="Funding, loans and incorporation are in Finances.";break;
            default:Navigate("Guidance");hint="Here are all my suggestions. Pick a direction whenever you like.";break;
        }
        return (focus??_sideContent.GetChildren().OfType<Button>().FirstOrDefault(),hint);
    }
    private void HighlightGuidance(Control? focus)
    {
        if(focus is null)return;
        focus.GrabFocus();focus.Modulate=new Color(.75f,1,.8f);
        for(Node? parent=focus.GetParent();parent is not null;parent=parent.GetParent())
            if(parent is ScrollContainer scroll)scroll.CallDeferred(ScrollContainer.MethodName.EnsureControlVisible,focus);
        var target=focus;GetTree().CreateTimer(2).Timeout+=()=>{if(IsInstanceValid(target))target.Modulate=Colors.White;};
    }
```

- [ ] **Step 4: Write the Goals page and dashboard card**

`godot/DebugMain.Goals.cs`:

```csharp
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

// The career goals board (spec 2026-10-01): the current chapter's goals, their progress and Helper-Chan's How? tips.
public partial class DebugMain
{
    private Label _dashboardGoalsTitle=null!;
    private VBoxContainer _dashboardGoalsList=null!;
    private string _dashboardGoalsKey="";

    private (int Done,int Total) GoalTally()
    {
        if(_state.Goals is not {} goals||goals.Chapter>=GoalCatalog.Chapters.Length)return (0,0);
        var current=GoalCatalog.In(goals.Chapter).ToArray();
        return (current.Count(g=>_state.GoalDone(g.Id)),current.Length);
    }
    private string GoalsRailText(){var (done,total)=GoalTally();return total==0?"★  Goals":$"★  Goals  {done}/{total}";}

    private void BuildGoals()
    {
        if(_state.Goals is not {} goals||goals.Chapter>=GoalCatalog.Chapters.Length)
        {
            Words(_sideContent,"Every goal complete",28);
            QuietWords(_sideContent,"You have finished every chapter and mastery goal. The career carries on as long as you like.");
            return;
        }
        var chapter=GoalCatalog.Chapters[goals.Chapter];
        Words(_sideContent,chapter.Name,28);
        QuietWords(_sideContent,chapter.Blurb);
        LiveWords(_sideContent,()=>{var (done,total)=GoalTally();return $"{done} of {total} goals done";},15);
        foreach(var goal in GoalCatalog.In(goals.Chapter))GoalCard(_sideContent,goal);
        if(chapter.Reward.Describe() is {Length:>0} reward){var card=StudioCard(_sideContent,"CHAPTER REWARD");Words(card,reward,15);}
    }

    private void GoalCard(Control parent,GoalDefinition goal)
    {
        var card=StudioCard(parent,goal.Title,goal.Why);
        var bar=new ProgressBar{MaxValue=1,Step=.001,ShowPercentage=false,CustomMinimumSize=new(0,14)};card.AddChild(bar);
        var line=LiveWords(card,()=>GoalLine(goal),14);
        QuietWords(card,"Reward: "+goal.Reward.Describe(),13);
        var how=ActionButton(card,"How?",()=>ShowGoalTip(goal));how.Name="GoalHow-"+goal.Id;
        void Update()
        {
            var done=_state.GoalDone(goal.Id);
            bar.Value=done?1:goal.Measure(_state).Fraction;how.Visible=!done;
            line.ThemeTypeVariation=done?"SectionLabel":""; // a mint tick for finished goals
        }
        Update();_pageLiveValues.Add(Update);
    }
    private string GoalLine(GoalDefinition goal)=>_state.Goals?.Completed.FirstOrDefault(r=>r.Id==goal.Id) is {} done
        ?$"✓ Done {done.At:d MMM yyyy}":goal.Measure(_state).Text;

    private void ShowGoalTip(GoalDefinition goal)
    {
        if(_inMenu||_helperPopup.Visible||OfficeEditing){Notify("Finish or close the current dialog or furniture draft first.");return;}
        var project=_state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId).OrderBy(s=>s.Id).LastOrDefault()?.Id??0;
        if(project>0)SelectSeriesForWorkbench(project);
        ClosePhone();
        var (focus,_)=RouteGuidance(goal.Target,project);
        HighlightGuidance(focus);
        CareerGuidance.Say(_presentation.Guidance,_state.Clock.Now,goal.Tip);CareerGuidance.MarkRead(_presentation.Guidance);
        RefreshGuidance();Notify("Helper-Chan: "+goal.Tip);
    }

    private void BuildDashboardGoals(Control content)
    {
        var card=StudioCard(content,"THIS CHAPTER");
        _dashboardGoalsTitle=Words(card,"",18);
        _dashboardGoalsList=new VBoxContainer();card.AddChild(_dashboardGoalsList);
        ActionButton(card,"All goals",()=>Navigate("Goals"));
    }
    private void RefreshDashboardGoals()
    {
        if(_state.Goals is not {} goals||goals.Chapter>=GoalCatalog.Chapters.Length)
        {_dashboardGoalsTitle.Text="Every goal complete";Empty(_dashboardGoalsList);_dashboardGoalsKey="";return;}
        _dashboardGoalsTitle.Text=GoalCatalog.Chapters[goals.Chapter].Name;
        var next=GoalCatalog.In(goals.Chapter).Where(g=>!_state.GoalDone(g.Id)).Select(g=>(Goal:g,Progress:g.Measure(_state)))
            .OrderByDescending(x=>x.Progress.Fraction).ThenBy(x=>x.Goal.Id,System.StringComparer.Ordinal).Take(2).ToArray();
        var key=string.Join("|",next.Select(x=>x.Goal.Id+":"+x.Progress.Text));
        if(key==_dashboardGoalsKey)return;
        _dashboardGoalsKey=key;Empty(_dashboardGoalsList);
        foreach(var (goal,progress) in next)
        {
            Words(_dashboardGoalsList,goal.Title,14);
            _dashboardGoalsList.AddChild(new ProgressBar{MaxValue=1,Step=.001,Value=progress.Fraction,ShowPercentage=false,CustomMinimumSize=new(0,10)});
            QuietWords(_dashboardGoalsList,progress.Text,12);
        }
    }
}
```

In `godot/DebugMain.OfficeDashboard.cs`, `BuildOfficeDashboard`, directly before `actions=StudioCard(content,"QUICK ACTIONS");` add:

```csharp
        BuildDashboardGoals(content);
```

In `godot/DebugMain.Management.cs`:
- in the rail array at line 147, add `("Goals","★"),` before `("Inbox","✉"),`;
- in `RefreshManagement`, directly after `RefreshOfficeDashboard();` add:

```csharp
        RefreshDashboardGoals();
        if(_navigation.TryGetValue("Goals",out var goalsButton))goalsButton.Text=GoalsRailText();
```

In `godot/DebugMain.ManagementPanels.cs`, `BuildManagementPage`, add to the switch:

```csharp
            case "Goals":BuildGoals();break;
```

In `godot/DebugMain.ManagementMenus.cs:194`, add `"Goals",` to the restorable page array (after `"Office",`).

In `godot/DebugMain.DisplaySweepSmoke.cs`, add to the `screens` list after the office entry:

```csharp
                ("goals",()=>Navigate("Goals")),
```

- [ ] **Step 5: Build and run the check**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Flags '--goals-smoke' -Timeout 600"
```
Expected: 0 warnings; `GOALS SMOKE PASSED: 4 checks.`

---

### Task 7: Celebrations, reward furniture and the catalogue

**Files:**
- Modify: `godot/DebugMain.cs` (`ScanEvents`: goal notice, stopping rule)
- Modify: `godot/DebugMain.ManagementPanels.cs` (`ImportantEvent`, `ShowEvent`)
- Modify: `godot/DebugMain.Office.cs:67`, `:114` (catalogue)
- Modify: `godot/Office/OfficeArt.cs` (reward kinds)
- Modify: `godot/DebugMain.GoalsSmoke.cs` (add `CheckGoalCelebrations`)

**Interfaces:**
- Consumes: Tasks 1 to 6.
- Produces: `CheckGoalCelebrations()`.

- [ ] **Step 1: Write the failing smoke check**

In `godot/DebugMain.GoalsSmoke.cs`, add `await CheckGoalCelebrations();` after `await CheckGoalsBoard();`, and add:

```csharp
    private async Task CheckGoalCelebrations()
    {
        var prefs = _presentation.Guidance;
        ShowOffice(); SetSpeed(4); ChooseSpeed(QuietSpeed); _popupEvents.Clear(); _helperPopup.Hide(); CareerGuidance.MarkRead(prefs); await SettleUi();
        _state.Goals!.Completed.Add(new("convention", _state.Clock.Now, false));
        _state.Events.Add(new() { Time = _state.Clock.Now, ActivityDate = _state.Clock.Now.Date, Type = EventType.GoalCompleted, Message = "Goal complete: Attend a convention. Reward: ¥10,000." });
        Check(!ScanEvents() && _speed == QuietSpeed && prefs.Thread.Last().Step == CareerGuidance.GoalStep && _notice.Text.Contains("Attend a convention"),
            $"A finished goal texts and notifies without stopping 32x (speed {_speed})");
        _state.Events.Add(new() { Time = _state.Clock.Now, ActivityDate = _state.Clock.Now.Date, Type = EventType.GoalChapterCompleted, Message = "Chapter complete: Doujin Days! Reward: a trophy shelf." });
        Check(ScanEvents() && _speed == 0 && _popupEvents.Count > 0, "A finished chapter stops 32x and queues its celebration");
        ShowEvent(_popupEvents.Dequeue()); await SettleUi(); Press("Open relevant controls"); await SettleUi();
        Check(_page == "Goals", "The chapter celebration opens the Goals page");
        Check(_state.Furniture.Any(f => f.Kind == "print" && f.Paid == 0 && f.Owner == FurnitureOwner.Business), "A decoration reward waits in office storage");
        RefreshOffice(); await SettleUi();
        Check(Enumerable.Range(0, _officeCatalog.ItemCount).All(i => !_officeCatalog.GetItemText(i).StartsWith("Trophy")), "Goal rewards are not for sale in the catalogue");
        var desk = Array.FindIndex(OfficeCatalog.Furniture, f => f.Id == "desk-studio");
        Check(_officeCatalog.IsItemDisabled(desk) && _officeCatalog.GetItemText(desk).Contains("Unlocked by: Rookie chapter"), $"The studio desk names the chapter that unlocks it ({_officeCatalog.GetItemText(desk)})");
    }
```

- [ ] **Step 2: Build and run to verify it fails**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Flags '--goals-smoke' -Timeout 600"
```
Expected: `GOALS SMOKE FAILED: A finished goal texts and notifies without stopping 32x` (the goal text stops 32x today).

- [ ] **Step 3: Celebrations**

In `godot/DebugMain.cs`, `ScanEvents`:
- change `var threadBefore = prefs.Thread.Count;` to `var threadBefore = CareerGuidance.StoppingMessages(prefs);`
- change `if (prefs.Thread.Count > threadBefore && CareerGuidance.Unread(prefs) > 0) { shouldPause = true; stop = true; }` to:

```csharp
            // Goal texts never stop 32x; her other new texts still do (spec 2026-10-01).
            if (CareerGuidance.StoppingMessages(prefs) > threadBefore && CareerGuidance.UnreadStopping(prefs) > 0) { shouldPause = true; stop = true; }
```

- in the `foreach (var ev in fresh)` loop, after `AppendLog(ev);` add:

```csharp
            if (_managementReady && ev.Type == EventType.GoalCompleted) Notify("Helper-Chan: " + ev.Message);
```

- directly before `if (fast && prefs.Visible)` add, so goal texts also reach the phone at slower speeds this frame:

```csharp
        if (_managementReady && prefs.Visible) CareerGuidance.ObserveGoals(_state, prefs);
```

In `godot/DebugMain.ManagementPanels.cs`:
- in `ImportantEvent`, after `EventType.CareerMilestone or ` add `EventType.GoalChapterCompleted or `;
- in `ShowEvent`, change the expression argument of `HelperBody(` from `ImportantEvent(e)?"concerned":"happy"` to `ImportantEvent(e)&&e.Type!=EventType.GoalChapterCompleted?"concerned":"happy"`;
- in the "Open relevant controls" lambda, change `if(e.Type is EventType.AwardResult or EventType.AwardNomination)Navigate("Awards");` to `if(e.Type==EventType.GoalChapterCompleted)Navigate("Goals");else if(e.Type is EventType.AwardResult or EventType.AwardNomination)Navigate("Awards");`.

- [ ] **Step 4: The catalogue and the reward furniture**

In `godot/DebugMain.Office.cs:67`, change the catalogue fill to skip rewards:

```csharp
        foreach(var f in OfficeCatalog.Furniture.Where(f=>!f.RewardOnly))_officeCatalog.AddItem($"{f.Name} · ¥{f.Price:N0}");
```

and replace the line at `:114` (`for(var i=0;i<OfficeCatalog.Furniture.Length;i++)_officeCatalog.SetItemDisabled(...)`) with:

```csharp
        for(var i=0;i<_officeCatalog.ItemCount;i++)
        {
            var f=OfficeCatalog.Furniture[i];var available=state.EquipmentAvailable(f.Id);
            _officeCatalog.SetItemDisabled(i,!available);
            _officeCatalog.SetItemText(i,$"{f.Name} · ¥{f.Price:N0}"+(!available&&GoalCatalog.UnlockedBy(f.Id) is {} chapter?$" · Unlocked by: {chapter} chapter":""));
        }
```

In `godot/Office/OfficeArt.cs`, `Furniture(...)`, add these branches before the final `else` (kind names avoid "desk", "chair" and "break", which earlier branches match with `Contains`):

```csharp
        else if(kind is "trophy-shelf" or "display-cabinet")
        {
            var tall=kind=="display-cabinet";var gold=new Color(.86f,.69f,.29f);
            Box(root,new(.5f,tall?.9f:.6f,.2f),new(1f,tall?1.8f:1.2f,.4f),wood.Darkened(.15f));
            for(var i=0;i<(tall?4:3);i++)Box(root,new(.5f,.15f+i*.42f,.2f),new(.92f,.03f,.36f),wood);
            Box(root,new(.32f,.52f,.2f),new(.12f,.16f,.02f),paper);
            Box(root,new(.68f,.5f,.2f),new(.08f,.1f,.08f),gold);
            if(tall)for(var i=0;i<3;i++)Box(root,new(.25f+i*.25f,1.38f,.2f),new(.07f,.12f,.07f),gold);
        }
        else if(kind=="trophy")
        {
            var gold=new Color(.86f,.69f,.29f);
            Box(root,new(.25f,.06f,.25f),new(.22f,.12f,.22f),dark);
            Box(root,new(.25f,.2f,.25f),new(.05f,.16f,.05f),gold);
            Sphere(root,new(.25f,.36f,.25f),new(.2f,.18f,.2f),gold);
        }
        else if(kind is "framed-letter" or "ranking-chart" or "award-plaque" or "gold-frame" or "company-sign")
        {
            var frame=kind is "gold-frame" or "award-plaque"?new Color(.86f,.69f,.29f):kind=="company-sign"?dark:wood;
            var w=kind=="company-sign"?1.2f:kind=="ranking-chart"?.7f:.5f;
            Box(root,new(w/2,1.5f,.025f),new(w,.5f,.04f),frame);
            Box(root,new(w/2,1.5f,.052f),new(w-.08f,.42f,.01f),kind=="company-sign"?new Color(.94f,.78f,.47f):paper);
            if(kind=="ranking-chart")for(var i=0;i<4;i++)Box(root,new(.15f+i*.13f,1.36f+i*.05f,.061f),new(.08f,.1f+i*.1f,.005f),new Color(.31f,.75f,.69f));
        }
        else if(kind=="plant-set")
        {
            foreach(var x in new[]{.2f,.6f})
            {Box(root,new(x,.14f,.25f),new(.24f,.28f,.24f),new(.57f,.31f,.22f));Sphere(root,new(x,.5f,.25f),new(.3f,.36f,.3f),new(.25f,.43f,.28f));}
        }
```

- [ ] **Step 5: Build and run the checks**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Flags '--goals-smoke','--quiet-speed-smoke' -Timeout 900"
```
Expected: 0 warnings; `GOALS SMOKE PASSED: 10 checks.`; the quiet speed smoke still passes (32x stops for her other texts).

---

### Task 8: Goal times in the playtest harness

**Files:**
- Modify: `tests/MangakaSim.Tests/CareerPlaytest.cs` (`ReadEvents`)

**Interfaces:**
- Consumes: `EventType.GoalCompleted`, `EventType.GoalChapterCompleted`.

- [ ] **Step 1: Record goals and chapters in the report**

In `ReadEvents`, add to the switch:

```csharp
                    case EventType.GoalCompleted: Mark(e.Message.Split(". Reward")[0]); break;
                    case EventType.GoalChapterCompleted: Mark(e.Message.Split("! Reward")[0]); break;
```

- [ ] **Step 2: Run the harness and read the times**

Run:
```powershell
dotnet test tests/MangakaSim.Tests --filter "Category=Playtest"
Select-String -Path TestResults/career-playtest/seed0-Standard.md -Pattern "Goal complete|Chapter complete|goal-next"
```
Expected: the tests pass; Doujin Days completes, ideally within the first one to two in-game months (spec starting target); later chapters show their dates; after "career-settled" the guidance timeline shows `goal-next:` steps. If a chapter never completes because a goal is out of reach for the guided player, or Doujin Days takes more than about four months, change only that goal's number in `Goals.cs`, record a ruling with the before and after times, and rerun.

---

### Task 9: Full verification, rendered review and records

**Files:**
- Modify only where a check or the review finds a problem (a ruling for any change to a check's expectation or a layout value).
- Create: `docs/superpowers/career-goals-completion.md`
- Modify: `CLAUDE.md` section 5, `README.md` (add `--goals-smoke`), `docs/superpowers/session-handoff.md`

- [ ] **Step 1: Run the full suite**

Run:
```powershell
dotnet test tests/MangakaSim.Tests
dotnet build MangakaGame.sln -warnaserror
pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Flags '--smoke-test','--management-smoke','--progression-smoke','--alpha-smoke','--usability-smoke','--production-smoke','--office-life-smoke','--convenience-smoke','--series-status-smoke','--atmosphere-smoke','--family-home-smoke','--quiet-speed-smoke','--display-sweep-smoke','--journey-smoke','--music-smoke','--startup-smoke','--title-smoke','--brand-smoke','--tester-b-smoke','--goals-smoke' -Timeout 1800"
```
Expected: every unit test passes; 0 warnings; every smoke prints its PASSED line; the display sweep reports one more screen per combination (27 screens, 540 screen checks) with 0 flagged.

- [ ] **Step 2: Capture and review**

Run, with the output in `TestResults/goals`:
```powershell
pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Windowed -Flags '--goals-smoke --capture --alpha-output=C:/Users/moosh/Repos/MangakaGame/TestResults/goals','--display-sweep-smoke --capture --alpha-output=C:/Users/moosh/Repos/MangakaGame/TestResults/goals' -Timeout 2400"
```
Look at the Goals page, the office's This chapter card, the rail badge and a chapter celebration in both themes at 1920 x 1080, 3440 x 1440 and 1280 x 720 with 150% text, and the reward furniture placed in the office. Check: progress bars and numbers read clearly, nothing is cut off, finished goals show the mint tick, How? is hidden on finished goals. Fix what looks wrong and record what was seen.

- [ ] **Step 3: Records**

Write `docs/superpowers/career-goals-completion.md` in the style of `brand-ui-restyle-completion.md`: what changed (catalogue, state, rewards, older saves, Helper-Chan, the board, celebrations, furniture), the chapter times from the playtest, verification, not verified (whether goals make players want to play on: needs the user, then testers), rulings, deferred minors, next step (A3 progressive disclosure).

Add to `CLAUDE.md` section 5:

```markdown
- **Career goals board (2026-10-01):** five career chapters (Doujin Days,
  Rookie, Serialized, Studio Head, Legend) plus mastery goals, with progress
  bars, one-off rewards (cash, fans, decorations, unlocks, opportunities,
  Helper-Chan scenes) and How? tips (Q48 to Q51). Spec
  `docs/superpowers/specs/2026-10-01-career-goals-design.md`; record
  `docs/superpowers/career-goals-completion.md`.
```

In `README.md`, add `--goals-smoke` (the career goals board) to the list of other checks after `--tester-b-smoke`. Refresh `docs/superpowers/session-handoff.md`. Convert every edited doc to CRLF (except `README.md`, which stays LF) and confirm no LF-only lines remain.
