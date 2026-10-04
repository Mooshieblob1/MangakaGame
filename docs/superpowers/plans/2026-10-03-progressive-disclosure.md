# Progressive Disclosure (A3) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A new career shows only what Doujin Days needs; each other part of the interface opens at its own moment (or its chapter), is announced with a "New" tag and one Helper-Chan line, and never closes again, with a "show every screen" choice for experienced players.

**Architecture:** An engine-free part catalogue (`src/MangakaSim/Disclosure.cs`) names each part, its moment, its backstop chapter, its announcement and the pages it owns. `GameState` keeps a small saved `DisclosureState` (parts opened, show every screen) checked every hour and after every command; opening a part from navigation is a logged command, so replays stay exact. Rival job offers to Aki wait for the Industry part. The Godot layer hides rail items, the 32x button and in-page sections, opens a part whenever anything navigates to its page, and shows "New" tags.

**Tech Stack:** C# (.NET 8), Godot 4.7.2 .NET, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-02-progressive-disclosure-design.md`

## Global Constraints

- Always present: Office, Goals, Inbox, Series, Help, the menu, the header's date, money and speeds up to 8x, Finances with the accounts and the part-time job.
- Parts, moments and backstop chapters (Goals chapter index: Doujin Days 0, Rookie 1, Serialized 2, Studio Head 3):
  books (first finished book; 1), quiet-speed (first sale; 1), publishing (first ongoing series; 1), contests (routing only; 1), staff (routing, or any staff; 2), studios (first hire; 3), industry (serialization or a contest result; 2), money (missed payday, a loan or incorporation; 3).
- Nothing points at a hidden part: any navigation to a page opens its part first (Helper-Chan's "Show me", How?, inbox and pop-up routes, page links).
- Nothing closes once opened. Hidden means absent: no greyed-out buttons for hidden parts.
- Announcements: a "New" tag on the rail item (or section button) until visited, and one Helper-Chan line; these texts never stop 32x (step `goal`, like goal texts). quiet-speed has no tag or text (Helper-Chan's existing 32x introduction covers it).
- Show every screen: on shows everything with no tags or texts; off returns to the parts already opened; recorded as a player action.
- Older saves open every reached part silently (no tags, no texts, no events); replay checkpoint rebased.
- Rival job offers to Aki wait for the industry part; offers to staff unchanged. Balance and economy unchanged; every difficulty and Sandbox alike.
- Clarification: the spec's "Helper-Chan's 32x introduction after the first sale" is measured in the simulation as the first sale.
- Docs CRLF, code LF, metric, no em dashes. Do not commit: the user commits. Packaging is not approved.
- Commands (PowerShell, repository root):
  ```powershell
  dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~<Name>"
  dotnet build MangakaGame.sln -warnaserror
  pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Flags '--disclosure-smoke' -Timeout 600"
  ```

## Review Focus

1. An older mid-career save: every part already reached opens silently and the rail looks as before (Task 2 tests).
2. Every inbox or pop-up route to a hidden part opens it rather than leading nowhere; every page a part owns maps to it (Task 1 page-map test; Task 5 smoke route check).
3. The 32x keyboard shortcut before the first sale does nothing and breaks nothing (Task 5 smoke).
4. Turning show every screen off mid-career never hides a part already opened (Task 1 test).
5. Career moves still work once Industry is open: rival offers to Aki arrive then, never before (Task 3 tests both ways).

---

### Task 1: Part catalogue, state, hourly checks and commands (engine-free)

**Files:**
- Create: `src/MangakaSim/Disclosure.cs`, `src/MangakaSim/GameState.Disclosure.cs`
- Modify: `src/MangakaSim/Commands.cs` (two commands and their registrations), `src/MangakaSim/EventType.cs` (append `PartOpened`)
- Modify: `src/MangakaSim/GameState.Commands.cs` (switch cases; `EvaluateParts()` after `RiskStep();` at the end of `Apply`)
- Modify: `src/MangakaSim/GameState.cs` (`Tick` after `EvaluateGoals();`; `NewGame` sets `Disclosure`)
- Modify: `src/MangakaSim/GameState.Serialization.cs` (`ValidateSave` calls `ValidateDisclosure`)
- Test: `tests/MangakaSim.Tests/DisclosureTests.cs`

**Interfaces:**
- Consumes: `GameState.Goals` (chapter), goal measures `GoalDoujinFinished`, `GoalCopiesSold`, `GoalTitles`, `GoalStaff`, `GoalSerializedOrPlaced`, `GoalTitle(int?)` (career goals board).
- Produces: `sealed record PartDefinition(string Id, string Name, int Backstop, string? Announcement, Func<GameState, bool> Moment)`; `static class DisclosureCatalog { PartDefinition[] Parts; PartDefinition Get(string id); bool Known(string id); string? PartFor(string page); }`; `sealed record PartRecord(string Id, DateTime At, bool Backfilled)`; `sealed class DisclosureState { List<PartRecord> Opened; bool ShowAll; }`; `GameState.Disclosure` (`DisclosureState?`); `bool GameState.PartShown(string id)`; `bool GameState.PartOpened(string id)`; `internal void GameState.EvaluateParts(bool backfill = false)`; `sealed record OpenPartCommand(string Part)`; `sealed record ShowEveryScreenCommand(bool On)`; `EventType.PartOpened`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/DisclosureTests.cs`:

```csharp
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
        Assert.Equal("money", DisclosureCatalog.PartFor("Business actions"));
        Assert.Equal("contests", DisclosureCatalog.PartFor("Awards"));
        foreach (var page in new[] { "Office", "Goals", "Inbox", "Series", "Series details", "Finances", "Help", "Guidance", "Production", "New doujin", "New series", "Showcase" })
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
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~DisclosureTests"`
Expected: build FAILS with `CS1061: 'GameState' does not contain a definition for 'PartShown'`.

- [ ] **Step 3: Write the catalogue**

`src/MangakaSim/Disclosure.cs`:

```csharp
namespace MangakaSim;

/// <summary>A part of the interface that stays hidden until the career reaches it (progressive disclosure spec 2026-10-02).
/// Backstop is the goals chapter index that opens it at the latest; Announcement null means no tag and no text.</summary>
public sealed record PartDefinition(string Id, string Name, int Backstop, string? Announcement, Func<GameState, bool> Moment);

public static class DisclosureCatalog
{
    public static readonly PartDefinition[] Parts =
    [
        new("books", "Books", 1, "Your first book is done! Books is in the menu now: printing, online sales and conventions.",
            s => s.GoalDoujinFinished),
        new("quiet-speed", "32x speed", 1, null, s => s.GoalCopiesSold > 0),
        new("publishing", "Publishing", 1, "An ongoing series can go to a magazine. Publishing is open now: pitches and contracts.",
            s => s.GoalTitles.Any(t => !t.StandaloneDoujin)),
        new("contests", "Contests", 1, "Newcomer contests are open in Awards. A placing impresses editors.", s => false),
        new("staff", "Staff", 2, "Staff is in the menu now: recruitment, teams and overtime.", s => s.GoalStaff > 0),
        new("studios", "Studios", 3, "Studios is in the menu now: desks and furniture, and later bigger premises.", s => s.GoalStaff > 0),
        new("industry", "Industry", 2, "You're in the industry now! Industry shows rankings, rival studios and licence offers.",
            s => s.GoalSerializedOrPlaced || s.Progression.Awards.Any(a => a.ManuscriptId > 0 && a.ResolvedAt is not null && s.GoalTitle(a.SeriesId))),
        new("money", "Business money", 3, "Loans, card advances and incorporation are in Finances, under Funding, loans & incorporation.",
            s => s.WageArrears > 0 || s.Loans.Count > 0 || s.ControlledBusiness.Incorporated),
    ];

    public static PartDefinition Get(string id) => Parts.First(p => p.Id == id);
    public static bool Known(string id) => Parts.Any(p => p.Id == id);

    /// <summary>The part a page or workspace belongs to, or null for pages that are always there.</summary>
    public static string? PartFor(string page) => page switch
    {
        "Books" or "Print doujin" or "Sell online" or "Conventions" or "Distribution settings" => "books",
        "Publishing" => "publishing",
        "Awards" => "contests",
        "Staff" or "Person" or "Recruitment" or "Team settings" => "staff",
        "Studios" or "Studio actions" or "Tokyo map" or "Furniture" or "Career moves" or "Properties" => "studios",
        "Industry" or "Industry contacts" or "Licenses" or "Legacy" => "industry",
        "Business actions" => "money",
        _ => null,
    };
}
```

- [ ] **Step 4: Write the state, the checks and the commands**

`src/MangakaSim/GameState.Disclosure.cs`:

```csharp
namespace MangakaSim;

public sealed record PartRecord(string Id, DateTime At, bool Backfilled);

/// <summary>Which parts of the interface a career has opened (spec 2026-10-02). Absent from older saves (see EnsureDisclosure).</summary>
public sealed class DisclosureState
{
    public List<PartRecord> Opened { get; set; } = new();
    public bool ShowAll { get; set; }
}

public partial class GameState
{
    /// <summary>Null only in saves written before progressive disclosure, until EnsureDisclosure backfills it on load.</summary>
    public DisclosureState? Disclosure { get; set; }

    /// <summary>Whether a part is visible: opened, or show every screen. A state with no disclosure shows everything.</summary>
    public bool PartShown(string id) => Disclosure is not { } d || d.ShowAll || d.Opened.Any(r => r.Id == id);
    public bool PartOpened(string id) => Disclosure?.Opened.Any(r => r.Id == id) == true;

    /// <summary>Opens every part whose moment has happened or whose backstop chapter has begun. A backfill (older saves) emits nothing.</summary>
    internal void EvaluateParts(bool backfill = false)
    {
        if (Disclosure is not { } d) return;
        var chapter = Goals?.Chapter ?? 0;
        foreach (var part in DisclosureCatalog.Parts)
            if (!d.Opened.Any(r => r.Id == part.Id) && (chapter >= part.Backstop || part.Moment(this))) OpenPart(part, backfill);
    }

    private void OpenPart(PartDefinition part, bool backfill)
    {
        Disclosure!.Opened.Add(new(part.Id, Clock.Now, backfill));
        if (!backfill) Emit(EventType.PartOpened, $"Opened: {part.Name}.", personId: ProtagonistPersonId);
    }

    private void ApplyOpenPart(OpenPartCommand c)
    {
        if (!DisclosureCatalog.Known(c.Part)) throw new InvalidCommandException("That part of the studio does not exist.");
        if (Disclosure is { } d && !d.Opened.Any(r => r.Id == c.Part)) OpenPart(DisclosureCatalog.Get(c.Part), false);
    }

    private void ValidateDisclosure()
    {
        if (Disclosure is not { } d) return;
        if (d.Opened is null || d.Opened.Any(r => r is null || !DisclosureCatalog.Known(r.Id)) || d.Opened.Select(r => r.Id).Distinct().Count() != d.Opened.Count)
            throw new InvalidDataException("Save file has invalid disclosure parts.");
    }
}
```

In `src/MangakaSim/Commands.cs`, add after `[JsonDerivedType(typeof(RelocateOfficeCommand), "RelocateOffice")]`:

```csharp
[JsonDerivedType(typeof(OpenPartCommand), "OpenPart")]
[JsonDerivedType(typeof(ShowEveryScreenCommand), "ShowEveryScreen")]
```

and at the end of the file:

```csharp
/// <summary>Opens a part of the interface because the player went to one of its pages (progressive disclosure spec 2026-10-02).</summary>
public sealed record OpenPartCommand(string Part) : ICommand;
/// <summary>The "Experienced player: show every screen" choice.</summary>
public sealed record ShowEveryScreenCommand(bool On) : ICommand;
```

In `src/MangakaSim/EventType.cs`, change `GoalCompleted, GoalChapterCompleted,` to `GoalCompleted, GoalChapterCompleted, PartOpened,`.

In `src/MangakaSim/GameState.Commands.cs`, in the main `switch (command)` (the one with `case SetAppearanceCommand c:`), add:

```csharp
            case OpenPartCommand c: ApplyOpenPart(c); break;
            case ShowEveryScreenCommand c: if (Disclosure is not null) Disclosure.ShowAll = c.On; break;
```

and at the end of `Apply`, after `RiskStep();`, add `EvaluateParts();`.

In `src/MangakaSim/GameState.cs`: in `Tick()` after `EvaluateGoals();` add `EvaluateParts();`; in `NewGame` after `state.Goals = new();` add `state.Disclosure = new();`.

In `src/MangakaSim/GameState.Serialization.cs`, in `ValidateSave()`, after `ValidateGoals();` add `ValidateDisclosure();`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~DisclosureTests"`
Expected: PASS, 7 tests.

- [ ] **Step 6: Run the whole unit suite**

Run: `dotnet test tests/MangakaSim.Tests`
Expected: all pass. A failure from a test whose career relies on a rival job offer to Aki belongs to Task 3; note it and continue.

---

### Task 2: Older saves

**Files:**
- Modify: `src/MangakaSim/GameState.Disclosure.cs` (add `EnsureDisclosure`)
- Modify: `src/MangakaSim/GameState.Serialization.cs` (`FromJson`), `src/MangakaSim/GameState.Career.cs` (`ImportSupported`)
- Test: `tests/MangakaSim.Tests/OldSaveDisclosureTests.cs`

**Interfaces:**
- Consumes: `EvaluateParts(bool backfill)`, `EnsureGoals()`.
- Produces: `internal void GameState.EnsureDisclosure()`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/OldSaveDisclosureTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~OldSaveDisclosureTests"`
Expected: FAIL; the loaded state has `Disclosure == null`, so `PartOpened` is false.

- [ ] **Step 3: Backfill on load**

In `src/MangakaSim/GameState.Disclosure.cs`, add inside the `GameState` partial class:

```csharp
    /// <summary>Older saves have no disclosure: every part already reached opens silently (spec 2026-10-02), and the replay
    /// checkpoint is rebased so replays match. Runs after EnsureGoals so chapter backstops apply.</summary>
    internal void EnsureDisclosure()
    {
        if (Disclosure is not null) return;
        Disclosure = new();
        EvaluateParts(backfill: true);
        World.ReplayCheckpoint = null; World.ReplayLogStart = CommandLog.Count;
        ValidateSave(); World.ReplayCheckpoint = ToJson();
    }
```

In `src/MangakaSim/GameState.Serialization.cs`, change `try { state.ValidateSave(); state.EnsureGoals(); }` to `try { state.ValidateSave(); state.EnsureGoals(); state.EnsureDisclosure(); }`.

In `src/MangakaSim/GameState.Career.cs`, `ImportSupported`, after `imported.EnsureGoals();` add `imported.EnsureDisclosure();`.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~OldSaveDisclosureTests|FullyQualifiedName~OldSave"`
Expected: PASS, including the existing old-save tests.

---

### Task 3: Rival job offers to Aki wait for Industry

**Files:**
- Modify: `src/MangakaSim/GameState.Rivals.cs:118-131` (`GenerateRivalOffer`)
- Test: `tests/MangakaSim.Tests/RivalOfferDisclosureTests.cs`

**Interfaces:**
- Consumes: `PartShown(string)` (Task 1).

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/RivalOfferDisclosureTests.cs`:

```csharp
using Xunit;
namespace MangakaSim.Tests;

// Progressive disclosure, Q57: rival studios approach Aki only once Industry has opened.
public class RivalOfferDisclosureTests
{
    static bool OfferedToAki(GameState s) => s.World.Offers.Any(o => o.PersonId == s.ProtagonistPersonId);

    [Fact] public void A_solo_doujin_artist_gets_no_rival_job_offer()
    {
        var s = GameState.NewGame(0);
        s.Advance(24 * 365 * 2);
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
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~RivalOfferDisclosureTests"`
Expected: `A_solo_doujin_artist_gets_no_rival_job_offer` FAILS (an offer to Aki arrives within two years); the second passes. If the second fails because no rival studio has a free desk in this seed, try seeds 1, 7 and 42 and use the first that produces an offer, recording a ruling.

- [ ] **Step 3: Gate the offers**

In `GenerateRivalOffer`, change the `foreach` filter to add the industry condition:

```csharp
        foreach(var person in ControlledStaff.Where(p=>!IsHistoricalAssistant(p.Id)&&p.Employment is {NoticeEndsAt:null} job&&job.StartsAt.AddDays(90)<=Clock.Now&&
            (p.Id!=ProtagonistPersonId||PartShown("industry"))).OrderBy(p=>p.Id)) // Q57: Aki is approached once Industry is open
```

- [ ] **Step 4: Run the tests to verify they pass, then the whole suite**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~RivalOfferDisclosureTests"`, then `dotnet test tests/MangakaSim.Tests`.
Expected: PASS. An existing test whose career expects a rival offer to Aki before Industry opens is updated to open Industry first (`s.Apply(new OpenPartCommand("industry"))`), with a ruling naming it.

---

### Task 4: Helper-Chan's announcements and the "New" tags (engine-free)

**Files:**
- Modify: `src/MangakaSim/Guidance.cs` (`GuidancePreferences.NewParts`, `ObserveParts`, `Observe` calls it)
- Test: `tests/MangakaSim.Tests/DisclosureGuidanceTests.cs`

**Interfaces:**
- Consumes: `DisclosureState`, `DisclosureCatalog`, `CareerGuidance.GoalStep`.
- Produces: `HashSet<string> GuidancePreferences.NewParts`; `static void CareerGuidance.ObserveParts(GameState, GuidancePreferences)`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/DisclosureGuidanceTests.cs`:

```csharp
using Xunit;
namespace MangakaSim.Tests;

// Progressive disclosure (spec 2026-10-02): each opened part is announced once, with a "New" tag, and never stops 32x.
public class DisclosureGuidanceTests
{
    [Fact] public void An_opened_part_is_announced_once_and_tagged_new()
    {
        var s = GoalProgressTests.FirstSale(); var prefs = new GuidancePreferences();
        CareerGuidance.ObserveParts(s, prefs); CareerGuidance.ObserveParts(s, prefs);
        Assert.Single(prefs.Thread, m => m.Texts.Single() == DisclosureCatalog.Get("books").Announcement);
        Assert.Contains("books", prefs.NewParts);
        Assert.DoesNotContain("quiet-speed", prefs.NewParts);
        Assert.All(prefs.Thread, m => Assert.Equal(CareerGuidance.GoalStep, m.Step));
        Assert.Equal(0, CareerGuidance.UnreadStopping(prefs));
    }

    [Fact] public void Parts_from_an_older_save_and_under_show_every_screen_pass_silently()
    {
        var old = GameState.NewGame(0); old.Disclosure = null;
        old.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && old.Series[0].Volumes.Count == 0; d++) old.Advance(24);
        var prefs = new GuidancePreferences();
        CareerGuidance.ObserveParts(GameState.FromJson(old.ToJson()), prefs);
        Assert.Empty(prefs.Thread); Assert.Empty(prefs.NewParts);
        var every = GameState.NewGame(0); every.Apply(new ShowEveryScreenCommand(true)); every.Apply(new OpenPartCommand("staff"));
        CareerGuidance.ObserveParts(every, prefs);
        Assert.Empty(prefs.Thread); Assert.Empty(prefs.NewParts);
    }

    [Fact] public void Every_announcement_fits_a_phone_bubble()
    {
        foreach (var p in DisclosureCatalog.Parts.Where(p => p.Announcement is not null))
            Assert.True(p.Announcement!.Length <= CareerGuidance.TextLimit, p.Id);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~DisclosureGuidanceTests"`
Expected: build FAILS with `CS0117: 'CareerGuidance' does not contain a definition for 'ObserveParts'`.

- [ ] **Step 3: Implement the announcements**

In `src/MangakaSim/Guidance.cs`, in `GuidancePreferences`, add after `Thread`:

```csharp
    /// <summary>Parts opened but not yet visited, shown with a "New" tag. Presentation data only; older saves start empty.</summary>
    public HashSet<string> NewParts { get; set; } = new();
```

In `CareerGuidance`, add:

```csharp
    /// <summary>Announces each opened part once with one line and a "New" tag (spec 2026-10-02). Parts from an older save, or
    /// opened while show every screen is on, pass silently. Uses the goal step, so these texts never stop 32x.</summary>
    public static void ObserveParts(GameState state, GuidancePreferences preferences)
    {
        if (state.Disclosure is not { } disclosure) return;
        foreach (var record in disclosure.Opened)
        {
            if (!preferences.Completed.Add("part:" + record.Id) || record.Backfilled || disclosure.ShowAll) continue;
            if (DisclosureCatalog.Get(record.Id).Announcement is not { } text) continue;
            preferences.NewParts.Add(record.Id);
            Append(preferences, new() { Step = GoalStep, Time = record.At, Texts = [text] });
        }
    }
```

In `Observe`, after the `ObserveGoals(state, preferences);` line add `ObserveParts(state, preferences);`.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~DisclosureGuidanceTests|FullyQualifiedName~Guidance"`
Expected: PASS, including the existing guidance tests. If a guidance test counts thread messages on a career that opens a part, run it with `s.Disclosure = null` (as the goals work did with `Goals`), recording a ruling.

---

### Task 5: Hidden rail items, the 32x button, opening on navigation and "New" tags

**Files:**
- Create: `godot/DebugMain.Disclosure.cs`, `godot/DebugMain.DisclosureSmoke.cs`
- Modify: `godot/DebugMain.Management.cs` (`Navigate` opens the page's part; `RefreshManagement` calls `RefreshDisclosure`)
- Modify: `godot/DebugMain.SpeedFeedback.cs` (`ChooseSpeed` ignores a hidden 32x)
- Modify: `godot/DebugMain.Gui.cs:71` (workspace links skip hidden pages)
- Modify: `godot/DebugMain.cs` (dispatch `--disclosure-smoke`; `ScanEvents` observes parts with goals)

**Interfaces:**
- Consumes: Tasks 1 and 4.
- Produces: `bool PartShown(string part)`, `bool PageShown(string page)`, `bool PartIsNew(string part)`, `void RevealPage(string page)`, `void RefreshDisclosure()`; smoke `RunDisclosureSmoke`, `CheckDayOne`.

- [ ] **Step 1: Write the failing smoke check**

`godot/DebugMain.DisclosureSmoke.cs`:

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
    // Progressive disclosure (spec 2026-10-02).
    private async void RunDisclosureSmoke()
    {
        SetProcess(false);
        try
        {
            GetWindow().Size = new(1920, 1080); Directory.CreateDirectory(SmokeOutput);
            _careers = new CareerStore(Path.Combine(SmokeOutput, "disclosure-" + Guid.NewGuid().ToString("N")));
            NewCareerMenu(); Press("Begin career"); await SettleUi(); _helperPopup.Hide();
            await CheckDayOne();
            GD.Print($"DISCLOSURE SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => tree.Quit(); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"DISCLOSURE SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }

    private async Task CheckDayOne()
    {
        string[] dayOne = ["Office", "Goals", "Inbox", "Series", "Finances", "Help"], later = ["Books", "Staff", "Studios", "Industry"];
        Check(dayOne.All(p => _navigation[p].Visible) && later.All(p => !_navigation[p].Visible) && !_speedButtons[32].Visible,
            "A new career shows only the day-one rail and no 32x");
        var speed = _speed; ChooseSpeed(QuietSpeed); Check(_speed == speed, "The 32x shortcut does nothing before the first sale");
        _state.Apply(new CreateDoujinCommand("First pages", "adventure"));
        for (var d = 0; d < 180 && _state.Series[0].Volumes.Count == 0; d++) _state.Advance(24);
        RefreshManagement(); await SettleUi();
        Check(_navigation["Books"].Visible && _navigation["Books"].Text.EndsWith("· New")
            && _presentation.Guidance.Thread.Any(m => m.Texts.Contains(DisclosureCatalog.Get("books").Announcement!)),
            $"Books appears with a New tag and Helper-Chan's line after the first book ({_navigation["Books"].Text})");
        Press("Books"); await SettleUi(); RefreshManagement();
        Check(!_navigation["Books"].Text.EndsWith("· New"), "Visiting Books clears its New tag");
        _state.Events.Add(new() { Time = _state.Clock.Now, ActivityDate = _state.Clock.Now.Date, Type = EventType.LicenseOffered, Message = "Staged licence offer" });
        ShowEvent(_state.Events.Count - 1); await SettleUi(); Press("Open relevant controls"); await SettleUi(); RefreshManagement();
        Check(_navigation["Industry"].Visible && _state.PartOpened("industry"), "A pop-up's route opens the hidden part it leads to");
        await CaptureSmokeImage("disclosure-day-one");
    }
}
```

In `godot/DebugMain.cs`, after the `--goals-smoke` dispatch line, add:

```csharp
        if (OS.GetCmdlineUserArgs().Contains("--disclosure-smoke")) CallDeferred(nameof(RunDisclosureSmoke));
```

- [ ] **Step 2: Build and run to verify it fails**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Flags '--disclosure-smoke' -Timeout 600"
```
Expected: `DISCLOSURE SMOKE FAILED: A new career shows only the day-one rail and no 32x`.

- [ ] **Step 3: Hide, reveal and tag**

`godot/DebugMain.Disclosure.cs`:

```csharp
using System.Collections.Generic;
using MangakaSim;

namespace MangakaGame;

// Progressive disclosure (spec 2026-10-02): hidden parts, opening on navigation, and "New" tags.
public partial class DebugMain
{
    private static readonly (string Page, string Part)[] RailParts = [("Books", "books"), ("Staff", "staff"), ("Studios", "studios"), ("Industry", "industry")];
    private readonly Dictionary<string, string> _railText = new();

    private bool PartShown(string part) => _state.PartShown(part);
    private bool PageShown(string page) => DisclosureCatalog.PartFor(page) is not { } part || _state.PartShown(part);
    private bool PartIsNew(string part) => _presentation.Guidance.NewParts.Contains(part);

    /// <summary>Nothing points at a hidden part: going to a page opens its part first, then clears its New tag.</summary>
    private void RevealPage(string page)
    {
        if (!_managementReady || DisclosureCatalog.PartFor(page) is not { } part) return;
        if (!_state.PartOpened(part)) _state.Apply(new OpenPartCommand(part));
        CareerGuidance.ObserveParts(_state, _presentation.Guidance);
        _presentation.Guidance.NewParts.Remove(part);
    }

    private void RefreshDisclosure()
    {
        foreach (var (page, part) in RailParts)
            if (_navigation.TryGetValue(page, out var button))
            {
                _railText.TryAdd(page, button.Text);
                button.Visible = PartShown(part);
                button.Text = _railText[page] + (PartIsNew(part) ? "  · New" : "");
            }
        if (_speedButtons.TryGetValue(32, out var quiet)) quiet.Visible = PartShown("quiet-speed");
    }
}
```

In `godot/DebugMain.Management.cs`:
- in `Navigate`, after `if(OfficeEditing){Notify(...);return;}` add `RevealPage(page);`
- in `RefreshManagement`, after the `if(_navigation.TryGetValue("Goals",out var goalsButton))goalsButton.Text=GoalsRailText();` line add `RefreshDisclosure();`.

In `godot/DebugMain.SpeedFeedback.cs`, `ChooseSpeed`, add as the first line:

```csharp
        if(speed>=QuietSpeed&&_managementReady&&!_state.PartShown("quiet-speed"))return; // 32x opens with the first sale (spec 2026-10-02)
```

In `godot/DebugMain.Gui.cs:71`, change the workspace link loop header to:

```csharp
        foreach(var (label,destination) in WorkspaceLinks(route).Where(link=>PageShown(link.Item2)))
```

In `godot/DebugMain.cs`, `ScanEvents`, change `if (_managementReady && prefs.Visible) CareerGuidance.ObserveGoals(_state, prefs);` to:

```csharp
        if (_managementReady && prefs.Visible) { CareerGuidance.ObserveGoals(_state, prefs); CareerGuidance.ObserveParts(_state, prefs); }
```

- [ ] **Step 4: Build and run the check**

Run the commands in Step 2.
Expected: 0 warnings; `DISCLOSURE SMOKE PASSED: 5 checks.`

---

### Task 6: In-page parts, the New Career choice and the Settings switch

**Files:**
- Modify: `godot/DebugMain.OfficeDashboard.cs` (book buttons follow Books)
- Modify: `godot/DebugMain.ManagementPanels.cs:82-83, 94-98` (Series details), `:157` (Finances funding button)
- Modify: `godot/DebugMain.GuiPages.cs:73` (series card "Send to convention")
- Modify: `godot/DebugMain.ManagementMenus.cs` (New Career tick box; Settings switch)
- Modify: `godot/DebugMain.Disclosure.cs` (`_dashboardBooks` field), `godot/DebugMain.DisclosureSmoke.cs` (add `CheckInPageAndChoices`)

**Interfaces:**
- Consumes: Task 5 helpers; `ShowEveryScreenCommand`.

- [ ] **Step 1: Write the failing smoke check**

In `godot/DebugMain.DisclosureSmoke.cs`, add `await CheckInPageAndChoices();` after `await CheckDayOne();`, and add:

```csharp
    private async Task CheckInPageAndChoices()
    {
        Navigate("Finances"); await SettleUi();
        Check(!_sideContent.FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Visible && b.Text.StartsWith("Funding, loans")),
            "Finances hides loans and incorporation until they open");
        Navigate("Series details", _state.Series[0].Id); await SettleUi();
        Check(!_sideContent.FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Visible && b.Text.StartsWith("Magazine pitch")),
            "Series details hides the magazine section before an ongoing series");
        // The New Career choice shows every screen with no tags.
        _presentation = new() { Page = "Office" }; OpenTitle(); NewCareerMenu(); await SettleUi();
        var every = (CheckBox)_menuContent.FindChild("ShowEveryScreen", true, false)!; every.ButtonPressed = true;
        Press("Begin career"); await SettleUi(); _helperPopup.Hide(); RefreshManagement(); await SettleUi();
        Check(new[] { "Books", "Staff", "Studios", "Industry" }.All(p => _navigation[p].Visible && !_navigation[p].Text.EndsWith("· New")) && _speedButtons[32].Visible,
            "Experienced player: every screen shows from the start, with no New tags");
        // The Settings switch turns it off again, back to the parts already opened.
        ShowMenu(); Press("Settings"); await SettleUi();
        var toggle = (CheckBox)_menuContent.FindChild("ShowEveryScreen", true, false)!; toggle.ButtonPressed = false; await SettleUi();
        _menu.Hide(); _inMenu = false; RefreshManagement(); await SettleUi();
        Check(!_navigation["Industry"].Visible && !_state.Disclosure!.ShowAll, "Turning it off in Settings returns to the parts already opened");
    }
```

- [ ] **Step 2: Build and run to verify it fails**

Run the Task 5 commands. Expected: `DISCLOSURE SMOKE FAILED: Finances hides loans and incorporation until they open`.

- [ ] **Step 3: Hide the in-page parts**

In `godot/DebugMain.Disclosure.cs`, add the field `private Godot.Button _dashboardBooks = null!;`.

In `godot/DebugMain.OfficeDashboard.cs`, change `ActionButton(actions,"Books, printing & online sales",()=>Navigate("Books"));` to `_dashboardBooks=ActionButton(actions,"Books, printing & online sales",()=>Navigate("Books"));`, and add as the first line of `RefreshOfficeDashboard()`'s body:

```csharp
        _dashboardBooks.Visible=_dashboardConvention.Visible=PartShown("books");
```

In `godot/DebugMain.ManagementPanels.cs`, Series details:
- change the two lines `ActionButton(buttons,"Magazine pitch & contract",...);` and `ActionButton(buttons,"Print & sell",()=>OpenPrinting(s.Id));` to:

```csharp
        var pitch=ActionButton(buttons,"Magazine pitch & contract"+(PartIsNew("publishing")?"  · New":""),()=>{SelectSeriesForWorkbench(s.Id);OpenWorkspace("Publishing");});pitch.Visible=PartShown("publishing");
        ActionButton(buttons,"Print & sell",()=>OpenPrinting(s.Id)).Visible=PartShown("books");
```

- change the extras block (`var extras=Disclosure(...)` through the `Adaptations & merchandise` button) to:

```csharp
        var extras=Disclosure(_sideContent,"Promotion, showcase & adaptations");
        var extraActions=new HFlowContainer();extras.AddChild(extraActions);
        ActionButton(extraActions,"Send to convention",()=>Navigate("Conventions",s.Id)).Visible=PartShown("books");
        ActionButton(extraActions,"Digital & overseas",()=>{SelectSeriesForWorkbench(s.Id);OpenWorkspace("Industry contacts");}).Visible=PartShown("industry");
        ActionButton(extraActions,"Showcase",()=>Navigate("Showcase",s.Id));
        ActionButton(extraActions,"Awards & contests",()=>Navigate("Awards")).Visible=PartShown("contests");
        ActionButton(extraActions,"Adaptations & merchandise",()=>Navigate("Licenses")).Visible=PartShown("industry");
```

- in Finances, change `ActionButton(_sideContent,"Funding, loans & incorporation",()=>OpenWorkspace("Business actions"));` to:

```csharp
        var funding=ActionButton(_sideContent,"Funding, loans & incorporation"+(PartIsNew("money")?"  · New":""),()=>OpenWorkspace("Business actions"));funding.Visible=PartShown("money");
```

In `godot/DebugMain.GuiPages.cs:73`, change `ActionButton(actions,"Send to convention",()=>Navigate("Conventions",s.Id));` to `ActionButton(actions,"Send to convention",()=>Navigate("Conventions",s.Id)).Visible=PartShown("books");`.

- [ ] **Step 4: The New Career choice and the Settings switch**

In `godot/DebugMain.ManagementMenus.cs`, New Career: after the `rights` line (`Words(world,"When other lead creators leave",14);...world.AddChild(rights);`) add:

```csharp
        var experienced=new CheckBox{Name="ShowEveryScreen",Text="Experienced player: show every screen"};world.AddChild(experienced);
```

and in the `Begin career` action, after `_state.Apply(new SetAppearanceCommand(SelectedLook()));` add:

```csharp
            if(experienced.ButtonPressed)_state.Apply(new ShowEveryScreenCommand(true));
```

In Settings, after the `stories` check box line, add:

```csharp
        if(!TitleOpen)
        {
            var every=new CheckBox{Name="ShowEveryScreen",Text="Experienced player: show every screen",ButtonPressed=_state.Disclosure?.ShowAll==true};help.AddChild(every);
            every.Toggled+=v=>{_state.Apply(new ShowEveryScreenCommand(v));RefreshManagement();};
        }
```

- [ ] **Step 5: Build and run the checks**

Run the Task 5 commands. Expected: 0 warnings; `DISCLOSURE SMOKE PASSED: 9 checks.`

---

### Task 7: Display sweep, playtest timings and the other checks

**Files:**
- Modify: `godot/DebugMain.DisplaySweepSmoke.cs` (fixture shows every screen; a day-one screen)
- Modify: `tests/MangakaSim.Tests/CareerPlaytest.cs` (`ReadEvents`)

- [ ] **Step 1: Show every screen in the sweep, plus a day-one view**

In `SweepCareer`, after `Try(new CreateDoujinCommand("First pages","adventure"));` add:

```csharp
        Try(new ShowEveryScreenCommand(true)); // every screen is checked; the day-one view is its own screen
```

In `RunDisplaySweepSmoke`, after `_state=SweepCareer(out var serial,out var doujin,out var fixture);` add `var sweepState=_state;`. In `Reset()`, add as its first line:

```csharp
                if(_state!=sweepState){_state=sweepState;ResetManagementSession();}
```

and add to the `screens` list, after the `goals` entry:

```csharp
                ("day-one",()=>{_state=GameState.NewGame(0);ResetManagementSession();ShowOffice();}),
```

- [ ] **Step 2: Record part openings in the playtest report**

In `CareerPlaytest.ReadEvents`, add to the switch:

```csharp
                    case EventType.PartOpened: Mark(e.Message); break;
```

- [ ] **Step 3: Run the sweep, the playtests and the smokes that walk new careers**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
dotnet test tests/MangakaSim.Tests --filter "Category=Playtest"
pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Flags '--display-sweep-smoke','--disclosure-smoke','--goals-smoke','--journey-smoke','--alpha-smoke' -Timeout 1800"
Select-String -Path TestResults/career-playtest/seed0-Standard.md -Pattern "Opened:"
```
Expected: playtests pass and each report lists the parts in a natural order (Books and 32x in April 1996, Publishing soon after, Industry at serialization, Staff and Studios around the first hire); the display sweep reports 28 screens per combination (560 screen checks) with 0 flagged; the other smokes pass. A smoke that presses a rail item before its part opens still works (pressing navigates, which opens the part); a check that asserts a hidden button is visible is updated with a ruling.

---

### Task 8: Full verification, rendered review and records

**Files:**
- Modify only where a check or the review finds a problem (a ruling for any change to a check's expectation or a layout value).
- Create: `docs/superpowers/progressive-disclosure-completion.md`
- Modify: `CLAUDE.md` section 5, `README.md` (add `--disclosure-smoke`), `docs/superpowers/fresh-player-test-findings.md` (A3 and B6 decisions), `docs/superpowers/session-handoff.md`

- [ ] **Step 1: Run the full suite**

Run:
```powershell
dotnet test tests/MangakaSim.Tests
dotnet build MangakaGame.sln -warnaserror
pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Flags '--smoke-test','--management-smoke','--progression-smoke','--alpha-smoke','--usability-smoke','--production-smoke','--office-life-smoke','--convenience-smoke','--series-status-smoke','--atmosphere-smoke','--family-home-smoke','--quiet-speed-smoke','--display-sweep-smoke','--journey-smoke','--music-smoke','--startup-smoke','--title-smoke','--brand-smoke','--tester-b-smoke','--goals-smoke','--disclosure-smoke' -Timeout 1800"
```
Expected: every unit test passes; 0 warnings; all 21 smokes print their PASSED line; display sweep 560 screen checks, 0 flagged.

- [ ] **Step 2: Capture and review**

Run with output in `TestResults/disclosure`:
```powershell
pwsh -NoProfile -Command "& '.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1' -Windowed -Flags '--disclosure-smoke --capture --alpha-output=C:/Users/moosh/Repos/MangakaGame/TestResults/disclosure','--display-sweep-smoke --capture --alpha-output=C:/Users/moosh/Repos/MangakaGame/TestResults/disclosure' -Timeout 2400"
```
Look at the day-one view and a fully opened career in both themes at 1920 x 1080, 3440 x 1440 and 1280 x 720 with 150% text: the shorter rail reads well, the "New" tag fits the rail, nothing is cut off, the header without 32x still lines up. Fix what looks wrong and record what was seen.

- [ ] **Step 3: Records**

Write `docs/superpowers/progressive-disclosure-completion.md` in the style of `career-goals-completion.md`: what changed, the order parts opened in the playtests, verification, not verified (whether new players now find the start calm: Tier 2 testers), rulings, deferred minors, next step (the Tier 1 closeout, Q53).

Add to `CLAUDE.md` section 5:

```markdown
- **Progressive disclosure, A3 (2026-10-03):** a new career shows only what
  Doujin Days needs; Books, Publishing, Contests, Staff, Studios, Industry,
  business money and 32x open at their own moment or chapter, with a "New" tag
  and one Helper-Chan line; "Experienced player: show every screen"; rival job
  offers to Aki wait for Industry (Q53 to Q57). Spec
  `docs/superpowers/specs/2026-10-02-progressive-disclosure-design.md`; record
  `docs/superpowers/progressive-disclosure-completion.md`. Next: Tier 1 closeout.
```

In `README.md`, add `--disclosure-smoke` (progressive disclosure) after `--goals-smoke`. In the findings file, set A3 and B6's decision to "Fixed 2026-10-03 (progressive disclosure)". Refresh the session handoff. Convert every edited doc to CRLF (except `README.md`, which stays LF) and confirm no LF-only lines remain.
