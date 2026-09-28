# Music System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the in-game music system (rotation with quiet gaps, big-moment cues, title music, volume, focus fades) so Suno tracks dropped into `godot/Assets/Music/` just work, with the game unchanged while no tracks exist.

**Architecture:** An engine-free `MusicPlan` in `src/MangakaSim` decides what plays and when; `MusicMoments` turns game events into big moments. A thin Godot `MusicPlayer` node plays what the plan decides with two crossfading players. `DebugMain` feeds context and events and adds a Music slider.

**Tech Stack:** C# (.NET 8), Godot 4.7.2 .NET, xUnit, ffmpeg (conversion script), PowerShell.

**Spec:** `docs/superpowers/specs/2026-09-28-music-system-design.md`

## Global Constraints

- Music is presentation only: never touch `GameState` randomness, the simulation or the save format, apart from the optional `CareerPresentation.MusicVolume` setting.
- `MusicPlan` uses its own `System.Random`; tests pass a seed.
- Rotation gaps are 60 to 120 seconds; big-moment crossfades are about 3 seconds; focus fades about 1 second.
- Day is 06:00 to 18:00 game time; the time of day is read only when a rotation track starts.
- Pool prefixes: `title-`, `day-`, `night-`, `studio-`, `convention-`, `deadline-`, `good-news-`, `setback-`.
- Big-moment priority: setback, good news, deadline, convention. Each kind at most once per game day, never during the overnight skip or in menus.
- Missing or failing files mean silence for that cue, never an error that stops play; a load failure is written once to the session timeline.
- Music volume default 0.5, range 0 to 1, saved with the career like the ambience and effects volumes.
- Docs: CRLF, metric units, no em dashes. Commit only when the user asks.
- Builds, tests and Godot checks are authorized for this work (2026-09-28); packaging is not.

## Review Focus

1. Natural end of a track in the Godot player (`Playing` false while not paused) is never exercised headless, where the dummy audio driver may not advance playback: check `MusicPlayer.Update`'s `finished` logic by reading it, and that a paused or faded-out player is not reported as finished.
2. A window left unfocused for a long time must not burn through the quiet gap or start tracks while paused: `MusicPlayer.Update` passes 0 seconds to the plan while unfocused (Task 3), and the smoke checks "Losing window focus fades and pauses the music" and "Focus brings the same track back" pin the player side.
3. Exported builds list `day-01.ogg.import` instead of `day-01.ogg`: pinned by `Discover_strips_export_suffixes_and_ignores_other_files` (Task 1).
4. Every track in a pool failing to load must leave quiet, not a busy retry loop: pinned by `Removing_every_track_leaves_quiet_without_retrying_every_frame` (Task 1).
5. Starting a session with volume 0 and raising it later must make the current track audible: pinned by the smoke check "Raising the volume from 0 makes the track audible" (Task 3).

---

### Task 1: MusicPlan (engine-free rules)

**Files:**
- Create: `src/MangakaSim/MusicPlan.cs`
- Test: `tests/MangakaSim.Tests/MusicPlanTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `enum MusicPool { Title, Day, Night, Studio, Convention, Deadline, GoodNews, Setback }`
  - `enum MusicMoment { Convention, Deadline, GoodNews, Setback }` (ascending priority)
  - `enum MusicTransition { None, FadeIn, Crossfade, FadeOut }`
  - `readonly record struct MusicCommand(MusicTransition Transition, string? TrackId)`
  - `readonly record struct MusicContext(bool InMenu, DateTime GameTime, bool MovedOut, bool Overnight)`
  - `sealed class MusicPlan(IEnumerable<string> trackIds, int seed, double gapMin = 60, double gapMax = 120)` with `string? Current`, `static MusicPool? PoolOf(string trackId)`, `static IReadOnlyDictionary<string,string> Discover(IEnumerable<string> fileNames, string folder)`, `void Remove(string trackId)`, `void Notice(IEnumerable<MusicMoment> moments, MusicContext context)`, `MusicCommand Update(double seconds, MusicContext context, bool trackFinished)`, `const double FirstTrackDelay = 5`.

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/MusicPlanTests.cs`:

```csharp
using Xunit;
namespace MangakaSim.Tests;

public class MusicPlanTests
{
    static MusicContext At(int hour, int day = 1, bool menu = false, bool moved = false, bool overnight = false) =>
        new(menu, new DateTime(1996, 4, day, hour, 0, 0), moved, overnight);

    /// <summary>Runs quiet time until the plan starts a track, returning the command and the seconds waited.</summary>
    static (MusicCommand Command, double Waited) UntilStart(MusicPlan plan, MusicContext context, double step = 1, double limit = 1000)
    {
        for (double t = step; t <= limit; t += step)
            if (plan.Update(step, context, false) is { Transition: not MusicTransition.None } command) return (command, t);
        throw new Xunit.Sdk.XunitException("No track started.");
    }
    static MusicCommand Finish(MusicPlan plan, MusicContext context) => plan.Update(0, context, true);

    [Fact] public void Career_starts_with_a_day_track_after_a_short_delay()
    {
        var plan = new MusicPlan(["day-01", "night-01"], 1);
        Assert.Equal(MusicTransition.None, plan.Update(1, At(10), false).Transition);
        var (start, waited) = UntilStart(plan, At(10));
        Assert.Equal(new MusicCommand(MusicTransition.FadeIn, "day-01"), start);
        Assert.InRange(waited, MusicPlan.FirstTrackDelay - 1, MusicPlan.FirstTrackDelay + 1);
    }

    [Fact] public void Night_hours_use_the_night_pool()
    {
        var plan = new MusicPlan(["day-01", "night-01"], 1);
        Assert.Equal("night-01", UntilStart(plan, At(22)).Command.TrackId);
    }

    [Fact] public void Studio_tracks_join_the_day_pool_after_moving_out()
    {
        var home = new MusicPlan(["day-01", "day-02", "studio-01"], 3);
        var moved = new MusicPlan(["day-01", "day-02", "studio-01"], 3);
        var atHome = new HashSet<string>(); var afterMove = new HashSet<string>();
        for (int i = 0; i < 30; i++)
        {
            atHome.Add(UntilStart(home, At(10)).Command.TrackId!); Finish(home, At(10));
            afterMove.Add(UntilStart(moved, At(10, moved: true)).Command.TrackId!); Finish(moved, At(10, moved: true));
        }
        Assert.DoesNotContain("studio-01", atHome);
        Assert.Contains("studio-01", afterMove);
    }

    [Fact] public void The_same_track_never_plays_twice_in_a_row()
    {
        var plan = new MusicPlan(["day-01", "day-02"], 5);
        string? last = null;
        for (int i = 0; i < 20; i++)
        {
            var track = UntilStart(plan, At(10)).Command.TrackId;
            Assert.NotEqual(last, track); last = track; Finish(plan, At(10));
        }
    }

    [Fact] public void Quiet_gaps_last_60_to_120_seconds()
    {
        var plan = new MusicPlan(["day-01", "day-02"], 7);
        UntilStart(plan, At(10));
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(MusicTransition.None, Finish(plan, At(10)).Transition);
            Assert.InRange(UntilStart(plan, At(10), 0.5).Waited, 60, 120.5);
        }
    }

    [Fact] public void Time_of_day_is_read_only_when_a_track_starts()
    {
        var plan = new MusicPlan(["day-01", "night-01"], 1);
        Assert.Equal("day-01", UntilStart(plan, At(10)).Command.TrackId);
        // At 32x the game flips to night within one track; nothing switches.
        for (int i = 0; i < 50; i++) Assert.Equal(MusicTransition.None, plan.Update(0.2, At(i % 2 == 0 ? 22 : 10), false).Transition);
        Finish(plan, At(22));
        Assert.Equal("night-01", UntilStart(plan, At(22)).Command.TrackId);
    }

    [Fact] public void A_big_moment_crossfades_in_once_per_game_day()
    {
        var plan = new MusicPlan(["day-01", "good-news-01"], 1);
        UntilStart(plan, At(10));
        plan.Notice([MusicMoment.GoodNews], At(11));
        Assert.Equal(new MusicCommand(MusicTransition.Crossfade, "good-news-01"), plan.Update(0.1, At(11), false));
        Assert.Equal(MusicTransition.None, Finish(plan, At(12)).Transition);
        plan.Notice([MusicMoment.GoodNews], At(13));
        Assert.Equal(MusicTransition.None, plan.Update(0.1, At(13), false).Transition);
        plan.Notice([MusicMoment.GoodNews], At(9, day: 2));
        Assert.Equal("good-news-01", plan.Update(0.1, At(9, day: 2), false).TrackId);
    }

    [Fact] public void A_setback_wins_when_moments_arrive_together()
    {
        var plan = new MusicPlan(["day-01", "good-news-01", "setback-01", "deadline-01"], 1);
        UntilStart(plan, At(10));
        plan.Notice([MusicMoment.GoodNews, MusicMoment.Deadline, MusicMoment.Setback], At(10));
        Assert.Equal("setback-01", plan.Update(0.1, At(10), false).TrackId);
    }

    [Fact] public void A_lower_moment_does_not_interrupt_a_higher_one()
    {
        var plan = new MusicPlan(["day-01", "good-news-01", "setback-01"], 1);
        UntilStart(plan, At(10));
        plan.Notice([MusicMoment.Setback], At(10)); plan.Update(0.1, At(10), false);
        plan.Notice([MusicMoment.GoodNews], At(11));
        Assert.Equal(MusicTransition.None, plan.Update(0.1, At(11), false).Transition);
        Assert.Equal("setback-01", plan.Current);
    }

    [Fact] public void Nothing_interrupts_during_the_overnight_skip()
    {
        var plan = new MusicPlan(["night-01", "setback-01"], 1);
        UntilStart(plan, At(22));
        plan.Notice([MusicMoment.Setback], At(23, overnight: true));
        Assert.Equal(MusicTransition.None, plan.Update(0.1, At(23, overnight: true), false).Transition);
    }

    [Fact] public void Menus_play_the_title_and_leaving_returns_to_the_rotation()
    {
        var plan = new MusicPlan(["title-01", "day-01"], 1);
        Assert.Equal(new MusicCommand(MusicTransition.FadeIn, "title-01"), plan.Update(0.1, At(10, menu: true), false));
        Assert.Equal(MusicTransition.None, plan.Update(0.1, At(10, menu: true), false).Transition);
        Assert.Equal(new MusicCommand(MusicTransition.FadeIn, "title-01"), Finish(plan, At(10, menu: true)));
        Assert.Equal(new MusicCommand(MusicTransition.Crossfade, "day-01"), plan.Update(0.1, At(10), false));
    }

    [Fact] public void No_tracks_means_quiet_everywhere()
    {
        var plan = new MusicPlan([], 1);
        plan.Notice([MusicMoment.Setback], At(10));
        for (int i = 0; i < 500; i++) Assert.Equal(MusicTransition.None, plan.Update(1, At(i % 24, menu: i % 7 == 0), false).Transition);
        Assert.Null(plan.Current);
    }

    [Fact] public void A_moment_without_tracks_does_not_interrupt()
    {
        var plan = new MusicPlan(["day-01"], 1);
        UntilStart(plan, At(10));
        plan.Notice([MusicMoment.GoodNews], At(10));
        Assert.Equal(MusicTransition.None, plan.Update(0.1, At(10), false).Transition);
        Assert.Equal("day-01", plan.Current);
    }

    [Fact] public void Removing_every_track_leaves_quiet_without_retrying_every_frame()
    {
        var plan = new MusicPlan(["day-01"], 1);
        UntilStart(plan, At(10));
        plan.Remove("day-01");
        Assert.Null(plan.Current);
        for (int i = 0; i < 300; i++) Assert.Equal(MusicTransition.None, plan.Update(1, At(10), false).Transition);
    }

    [Fact] public void Unknown_names_are_not_music()
    {
        Assert.Null(MusicPlan.PoolOf("README"));
        Assert.Null(MusicPlan.PoolOf("boss-01"));
        Assert.Equal(MusicPool.GoodNews, MusicPlan.PoolOf("good-news-02"));
    }

    [Fact] public void Discover_strips_export_suffixes_and_ignores_other_files()
    {
        var found = MusicPlan.Discover(["day-01.ogg.import", "night-01.ogg", "README.md", "boss-01.ogg", "title-01.mp3.import", "day-01.ogg"], "res://Assets/Music");
        Assert.Equal(3, found.Count);
        Assert.Equal("res://Assets/Music/day-01.ogg", found["day-01"]);
        Assert.Equal("res://Assets/Music/title-01.mp3", found["title-01"]);
        Assert.True(found.ContainsKey("night-01"));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~MusicPlanTests"`
Expected: build error, `MusicPlan` does not exist.

- [ ] **Step 3: Implement `MusicPlan`**

`src/MangakaSim/MusicPlan.cs`:

```csharp
namespace MangakaSim;

public enum MusicPool { Title, Day, Night, Studio, Convention, Deadline, GoodNews, Setback }
/// <summary>Big moments in ascending priority: a setback outranks good news, which outranks a deadline, then a convention.</summary>
public enum MusicMoment { Convention, Deadline, GoodNews, Setback }
public enum MusicTransition { None, FadeIn, Crossfade, FadeOut }
public readonly record struct MusicCommand(MusicTransition Transition, string? TrackId);
public readonly record struct MusicContext(bool InMenu, DateTime GameTime, bool MovedOut, bool Overnight);

/// <summary>
/// Decides what music plays and when (spec 2026-09-28, Q35 gentle, Q36 quiet stretches). Presentation only: it has its
/// own random generator and never touches the simulation, so music cannot change play or save replays.
/// </summary>
public sealed class MusicPlan
{
    public const double FirstTrackDelay = 5;
    private static readonly (string Prefix, MusicPool Pool)[] Prefixes =
    [
        ("title-", MusicPool.Title), ("day-", MusicPool.Day), ("night-", MusicPool.Night), ("studio-", MusicPool.Studio),
        ("convention-", MusicPool.Convention), ("deadline-", MusicPool.Deadline), ("good-news-", MusicPool.GoodNews), ("setback-", MusicPool.Setback),
    ];
    private static readonly string[] Extensions = [".ogg", ".mp3", ".wav"];
    private readonly Dictionary<MusicPool, List<string>> _pools = new();
    private readonly Dictionary<MusicMoment, DateTime> _lastMomentDay = new();
    private readonly Random _random;
    private readonly double _gapMin, _gapMax;
    private MusicMoment? _pending, _playingMoment;
    private bool _inMenu;
    private double _gapLeft = FirstTrackDelay;
    private string? _last;

    public string? Current { get; private set; }

    public MusicPlan(IEnumerable<string> trackIds, int seed, double gapMin = 60, double gapMax = 120)
    {
        _random = new Random(seed); _gapMin = gapMin; _gapMax = gapMax;
        foreach (var id in trackIds.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(i => i, StringComparer.Ordinal))
            if (PoolOf(id) is { } pool)
            {
                if (!_pools.TryGetValue(pool, out var list)) _pools[pool] = list = new();
                list.Add(id);
            }
    }

    public static MusicPool? PoolOf(string trackId)
    {
        foreach (var (prefix, pool) in Prefixes)
            if (trackId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return pool;
        return null;
    }

    /// <summary>Music files by track name. Exported Godot builds list imported files with ".import" or ".remap" added.</summary>
    public static IReadOnlyDictionary<string, string> Discover(IEnumerable<string> fileNames, string folder) =>
        fileNames.Select(f => f.EndsWith(".import", StringComparison.Ordinal) ? f[..^7] : f.EndsWith(".remap", StringComparison.Ordinal) ? f[..^6] : f)
            .Where(f => Extensions.Any(e => f.EndsWith(e, StringComparison.OrdinalIgnoreCase)) && PoolOf(Path.GetFileNameWithoutExtension(f)) is not null)
            .GroupBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => folder + "/" + g.OrderBy(x => x, StringComparer.Ordinal).First(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Drops a track for the session, for example when its file fails to load.</summary>
    public void Remove(string trackId)
    {
        foreach (var list in _pools.Values) list.RemoveAll(t => string.Equals(t, trackId, StringComparison.OrdinalIgnoreCase));
        if (string.Equals(Current, trackId, StringComparison.OrdinalIgnoreCase)) { Current = null; _playingMoment = null; _gapLeft = Gap(); }
    }

    public void Notice(IEnumerable<MusicMoment> moments, MusicContext context)
    {
        if (context.InMenu || context.Overnight) return;
        foreach (var moment in moments.Distinct().OrderByDescending(m => m))
        {
            if (!Has(PoolFor(moment))) continue;
            if (_lastMomentDay.TryGetValue(moment, out var day) && day == context.GameTime.Date) continue;
            _lastMomentDay[moment] = context.GameTime.Date;
            if (_pending is null || moment > _pending) _pending = moment;
        }
    }

    public MusicCommand Update(double seconds, MusicContext context, bool trackFinished)
    {
        var playing = Current is not null && !trackFinished;
        if (context.InMenu)
        {
            if (_inMenu && playing) return default;
            _inMenu = true; _pending = null; _playingMoment = null;
            if (Pick(MusicPool.Title) is { } title) { Current = title; return new(playing ? MusicTransition.Crossfade : MusicTransition.FadeIn, title); }
            Current = null;
            return playing ? new(MusicTransition.FadeOut, null) : default;
        }
        if (_inMenu)
        {
            _inMenu = false;
            if (PickRotation(context) is { } next) { Current = next; return new(playing ? MusicTransition.Crossfade : MusicTransition.FadeIn, next); }
            Current = null; _gapLeft = Gap();
            return playing ? new(MusicTransition.FadeOut, null) : default;
        }
        if (_pending is { } moment)
        {
            _pending = null;
            if (!context.Overnight && (_playingMoment is null || moment > _playingMoment) && Pick(PoolFor(moment)) is { } track)
            {
                Current = track; _playingMoment = moment;
                return new(playing ? MusicTransition.Crossfade : MusicTransition.FadeIn, track);
            }
        }
        if (Current is not null)
        {
            if (!trackFinished) return default;
            Current = null; _playingMoment = null; _gapLeft = Gap();
            return default;
        }
        _gapLeft -= seconds;
        if (_gapLeft > 0) return default;
        if (PickRotation(context) is not { } rotation) { _gapLeft = Gap(); return default; }
        Current = rotation;
        return new(MusicTransition.FadeIn, rotation);
    }

    private string? PickRotation(MusicContext context) =>
        context.GameTime.Hour is >= 6 and < 18
            ? context.MovedOut ? Pick(MusicPool.Day, MusicPool.Studio) : Pick(MusicPool.Day)
            : Pick(MusicPool.Night);

    private string? Pick(params MusicPool[] pools)
    {
        var options = pools.SelectMany(p => _pools.TryGetValue(p, out var list) ? list : []).ToList();
        if (options.Count == 0) return null;
        if (options.Count > 1 && _last is not null) options.Remove(_last);
        var pick = options[_random.Next(options.Count)];
        _last = pick;
        return pick;
    }

    private bool Has(MusicPool pool) => _pools.TryGetValue(pool, out var list) && list.Count > 0;
    private double Gap() => _gapMin + _random.NextDouble() * (_gapMax - _gapMin);
    private static MusicPool PoolFor(MusicMoment moment) => moment switch
    {
        MusicMoment.Setback => MusicPool.Setback,
        MusicMoment.GoodNews => MusicPool.GoodNews,
        MusicMoment.Deadline => MusicPool.Deadline,
        _ => MusicPool.Convention,
    };
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~MusicPlanTests"`
Expected: PASS, 16 tests.

- [ ] **Step 5: Commit** (only if the user asked for commits)

```bash
git add src/MangakaSim/MusicPlan.cs tests/MangakaSim.Tests/MusicPlanTests.cs
git commit -m "Music: engine-free music plan"
```

---

### Task 2: Big moments from game events, and the music volume setting

**Files:**
- Create: `src/MangakaSim/MusicMoments.cs`
- Modify: `src/MangakaSim/CareerStore.cs` (the `CareerPresentation` class near line 7 and the validation near line 136)
- Test: `tests/MangakaSim.Tests/MusicMomentsTests.cs`

**Interfaces:**
- Consumes: `MusicMoment` (Task 1); `GameState.FindSeries`, `ControlledBusinessId`, `ProtagonistPersonId`, `Progression.Awards` (`AwardEntry.ResolvedAt`, `Prize`, `SeriesId`; `ResolvedAt` is set to `Clock.Now` in `GameState.Awards.cs:129`, the same time the `AwardResult` event is emitted), `Bookings` (`ConventionBooking.Date`, `PersonId`, `BusinessId`, `Cancelled`), `Series.Contract`, `EventType.SerializationOffered`, `OfferAccepted`, `AwardResult`, `SeriesCancelled`, `PitchRejected`, `ChapterAtRisk`.
- Produces: `static class MusicMoments` with `static IReadOnlyList<MusicMoment> Classify(GameState state, IEnumerable<GameEvent> fresh, IEnumerable<string> milestones)`; `CareerPresentation.MusicVolume` (double, default 0.5).

- [ ] **Step 1: Write the failing tests**

`tests/MangakaSim.Tests/MusicMomentsTests.cs`:

```csharp
using System.Text.Json;
using Xunit;
namespace MangakaSim.Tests;

public class MusicMomentsTests
{
    static (GameState State, Series Owned) Career()
    {
        var s = GameState.NewGame(0);
        s.Apply(new CreateSeriesCommand("Owned", "adventure", Cadence.Monthly, 16));
        return (s, s.Series.Last());
    }
    static GameEvent E(GameState s, EventType type, int? series, int? person = null) =>
        new() { Type = type, SeriesId = series, PersonId = person, Time = s.Clock.Now, ActivityDate = s.Clock.Now.Date };

    [Fact] public void Offers_and_acceptances_are_good_news()
    {
        var (s, owned) = Career();
        Assert.Equal([MusicMoment.GoodNews], MusicMoments.Classify(s, [E(s, EventType.SerializationOffered, owned.Id)], []));
        Assert.Equal([MusicMoment.GoodNews], MusicMoments.Classify(s, [E(s, EventType.OfferAccepted, owned.Id)], []));
    }

    [Fact] public void Cancellations_and_rejections_are_setbacks()
    {
        var (s, owned) = Career();
        Assert.Equal([MusicMoment.Setback], MusicMoments.Classify(s, [E(s, EventType.SeriesCancelled, owned.Id)], []));
        Assert.Equal([MusicMoment.Setback], MusicMoments.Classify(s, [E(s, EventType.PitchRejected, owned.Id)], []));
    }

    [Fact] public void Other_studios_events_do_not_count()
    {
        var (s, owned) = Career();
        owned.BusinessId = s.ControlledBusinessId + 1000;
        Assert.Empty(MusicMoments.Classify(s, [E(s, EventType.SeriesCancelled, owned.Id)], []));
    }

    [Fact] public void Only_an_award_with_a_prize_is_good_news()
    {
        var (s, owned) = Career();
        s.Progression.Awards.Add(new() { Id = 9001, SeriesId = owned.Id, ResolvedAt = s.Clock.Now, Prize = 300_000 });
        Assert.Equal([MusicMoment.GoodNews], MusicMoments.Classify(s, [E(s, EventType.AwardResult, owned.Id)], []));
        s.Progression.Awards[^1].Prize = 0;
        Assert.Empty(MusicMoments.Classify(s, [E(s, EventType.AwardResult, owned.Id)], []));
    }

    [Fact] public void The_first_sale_milestone_is_good_news()
    {
        var (s, _) = Career();
        Assert.Equal([MusicMoment.GoodNews], MusicMoments.Classify(s, [], ["first-sale"]));
        Assert.Empty(MusicMoments.Classify(s, [], ["later-sale"]));
    }

    [Fact] public void A_chapter_at_risk_matters_only_under_a_magazine_contract()
    {
        var (s, owned) = Career();
        Assert.Empty(MusicMoments.Classify(s, [E(s, EventType.ChapterAtRisk, owned.Id)], []));
        owned.Contract = new(9002, s.PublisherCatalog.Magazines[0].Id, 5000, s.Clock.Now, s.Clock.Now.AddDays(30));
        Assert.Equal([MusicMoment.Deadline], MusicMoments.Classify(s, [E(s, EventType.ChapterAtRisk, owned.Id)], []));
    }

    [Fact] public void Attending_a_convention_today_is_a_convention_moment()
    {
        var (s, _) = Career();
        s.Bookings.Add(new() { Id = 9003, BusinessId = s.ControlledBusinessId, PersonId = s.ProtagonistPersonId, Date = s.Clock.Now.Date });
        Assert.Equal([MusicMoment.Convention], MusicMoments.Classify(s, [], []));
        s.Bookings[^1].Cancelled = true;
        Assert.Empty(MusicMoments.Classify(s, [], []));
    }

    [Fact] public void Music_volume_defaults_to_half_and_older_settings_get_the_default()
    {
        Assert.Equal(0.5, new CareerPresentation().MusicVolume);
        Assert.Equal(0.5, JsonSerializer.Deserialize<CareerPresentation>("{}")!.MusicVolume);
    }

    [Fact] public void A_music_volume_outside_0_to_1_is_refused()
    {
        var root = Path.Combine(Path.GetTempPath(), "music-volume-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new CareerStore(root);
            store.Save(Guid.NewGuid().ToString("N"), "Loud", GameState.NewGame(0), new CareerPresentation { MusicVolume = 2 });
            Assert.Empty(store.List());
            Assert.Equal(1, store.Unreadable);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
```

Before running, confirm the `SeriesContract` constructor order used above with `grep -n "record SeriesContract\|class SeriesContract" src/MangakaSim/*.cs` (it is constructed as `new(AllocateId(), magazine.Id, offer.FeePerPage, Clock.Now, offer.FirstIssueClose)` in `GameState.Publishing.cs`), and adjust the test's arguments to match if needed.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~MusicMomentsTests"`
Expected: build errors, `MusicMoments` and `CareerPresentation.MusicVolume` do not exist.

- [ ] **Step 3: Implement**

`src/MangakaSim/MusicMoments.cs`:

```csharp
namespace MangakaSim;

/// <summary>Turns new game events into the big moments that may cut into the music (spec 2026-09-28).</summary>
public static class MusicMoments
{
    public static IReadOnlyList<MusicMoment> Classify(GameState state, IEnumerable<GameEvent> fresh, IEnumerable<string> milestones)
    {
        var moments = new HashSet<MusicMoment>();
        Series? Owned(GameEvent e) => e.SeriesId is { } id && state.FindSeries(id) is { } s && s.BusinessId == state.ControlledBusinessId ? s : null;
        foreach (var e in fresh)
            switch (e.Type)
            {
                case EventType.SerializationOffered or EventType.OfferAccepted when Owned(e) is not null:
                    moments.Add(MusicMoment.GoodNews); break;
                case EventType.AwardResult when (Owned(e) is not null || e.PersonId == state.ProtagonistPersonId) &&
                    state.Progression.Awards.Any(a => a.SeriesId == e.SeriesId && a.ResolvedAt == e.Time && a.Prize > 0):
                    moments.Add(MusicMoment.GoodNews); break;
                case EventType.SeriesCancelled or EventType.PitchRejected when Owned(e) is not null:
                    moments.Add(MusicMoment.Setback); break;
                case EventType.ChapterAtRisk when Owned(e)?.Contract is not null:
                    moments.Add(MusicMoment.Deadline); break;
            }
        if (milestones.Contains("first-sale")) moments.Add(MusicMoment.GoodNews);
        if (state.Bookings.Any(b => !b.Cancelled && b.BusinessId == state.ControlledBusinessId &&
                b.PersonId == state.ProtagonistPersonId && b.Date == state.Clock.Now.Date))
            moments.Add(MusicMoment.Convention);
        return moments.OrderByDescending(m => m).ToList();
    }
}
```

`src/MangakaSim/CareerStore.cs`, in `CareerPresentation` after `EffectsVolume`:

```csharp
    public double MusicVolume { get; set; } = .5;
```

In the validation that throws "Invalid guidance or audio preferences.", add to the condition, beside the `EffectsVolume` checks:

```csharp
            || !double.IsFinite(data.View.MusicVolume) || data.View.MusicVolume is <0 or >1
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~MusicMomentsTests|FullyQualifiedName~MusicPlanTests|FullyQualifiedName~OldSaveTests|FullyQualifiedName~AlphaTests"`
Expected: PASS (old saves without `MusicVolume` still load).

- [ ] **Step 5: Commit** (only if the user asked for commits)

```bash
git add src/MangakaSim/MusicMoments.cs src/MangakaSim/CareerStore.cs tests/MangakaSim.Tests/MusicMomentsTests.cs
git commit -m "Music: big moments from events, music volume setting"
```

---

### Task 3: MusicPlayer node, game wiring, Music slider and the music smoke

**Files:**
- Create: `godot/Office/MusicPlayer.cs`
- Create: `godot/DebugMain.MusicSmoke.cs`
- Create: `godot/Assets/Music/README.md`
- Modify: `godot/DebugMain.Alpha.cs` (field near line 14, `BuildAlpha` near line 30, the volume sliders near line 248)
- Modify: `godot/DebugMain.cs` (`_Process` near line 111, `ScanEvents` near line 150, smoke dispatch near line 105)

**Interfaces:**
- Consumes: Task 1 and Task 2 types; `DebugMain` members `_state`, `_inMenu`, `_overnightTarget`, `_managementReady`, `_presentation`, `_milestones`, `LogTimeline`, `Check`, `_smokeChecks`, `SettleUi`.
- Produces: `MusicPlayer` with `UseFolder(int seed)`, `UseTracks(IReadOnlyDictionary<string, Func<AudioStream?>> tracks, int seed, double gapMin = 60, double gapMax = 120)`, `Notice(IEnumerable<MusicMoment>, MusicContext)`, `Update(double delta, MusicContext context, double volume, bool focused)`, `Action<string>? Failed`, read-only `Current`, `ActiveGain`, `InactiveGain`, `ActiveVolumeDb`, `Paused`, `Audible`; `DebugMain.MusicNow()`; the `--music-smoke` flag.

- [ ] **Step 1: Write the smoke check first**

`godot/DebugMain.MusicSmoke.cs`:

```csharp
using System;
using System.Collections.Generic;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // Music system (spec 2026-09-28): generated tones stand in for Suno tracks. Headless audio may not advance playback,
    // so natural track ends are covered by MusicPlanTests, not here.
    private async void RunMusicSmoke()
    {
        SetProcess(false);
        try
        {
            await SettleUi();
            var failures = new List<string>(); _music.Failed = failures.Add;
            var day = new MusicContext(false, new DateTime(1996, 4, 1, 10, 0, 0), false, false);

            _music.UseTracks(new Dictionary<string, Func<AudioStream?>>(), 1);
            for (var i = 0; i < 200; i++) _music.Update(1, day, .5, true);
            Check(_music.Current is null && !_music.Audible && failures.Count == 0, "With no music files the game stays silent");

            static AudioStream Tone(float hz)
            {
                const int rate = 22050, seconds = 30; var data = new byte[rate * seconds * 2];
                for (var i = 0; i < rate * seconds; i++) { var v = (short)(Math.Sin(2 * Math.PI * hz * i / rate) * 8000); data[i * 2] = (byte)v; data[i * 2 + 1] = (byte)(v >> 8); }
                return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Stereo = false, Data = data };
            }
            _music.UseTracks(new Dictionary<string, Func<AudioStream?>>
            {
                ["day-01"] = () => Tone(440), ["day-02"] = () => Tone(494), ["night-01"] = () => Tone(330),
                ["good-news-01"] = () => Tone(660), ["title-01"] = () => Tone(262), ["setback-01"] = () => null,
            }, 2, .2, .3);
            _music.Update(MusicPlan.FirstTrackDelay + 1, day, 0, true);
            Check(_music.Current?.StartsWith("day-") == true && _music.Audible, "A day track starts after the short opening delay");
            Check(_music.ActiveVolumeDb < -60, "Volume 0 keeps it silent");
            for (var i = 0; i < 30; i++) _music.Update(.1, day, .5, true);
            Check(_music.ActiveVolumeDb > -20, "Raising the volume from 0 makes the track audible");

            _music.Notice([MusicMoment.GoodNews], day); _music.Update(.1, day, .5, true);
            Check(_music.Current == "good-news-01", "Good news crossfades in");
            for (var i = 0; i < 40; i++) _music.Update(.1, day, .5, true);
            Check(_music.ActiveGain > .99f && _music.InactiveGain < .01f, "The crossfade completes in about three seconds");

            for (var i = 0; i < 15; i++) _music.Update(.1, day, .5, false);
            Check(_music.Paused, "Losing window focus fades and pauses the music");
            for (var i = 0; i < 15; i++) _music.Update(.1, day, .5, true);
            Check(!_music.Paused && _music.Current == "good-news-01", "Focus brings the same track back");

            var nextDay = day with { GameTime = day.GameTime.AddDays(1) };
            _music.Notice([MusicMoment.Setback], nextDay); _music.Update(.1, nextDay, .5, true);
            Check(failures.Count == 1 && failures[0] == "setback-01" && _music.Current != "setback-01", "A track that fails to load is skipped and reported once");

            _music.Update(.1, day with { InMenu = true }, .5, true);
            Check(_music.Current == "title-01", "The main menu plays the title track");

            GD.Print($"MUSIC SMOKE PASSED: {_smokeChecks} checks.");
            var tree = GetTree(); tree.CreateTimer(.1).Timeout += () => tree.Quit(); QueueFree();
        }
        catch (Exception ex) { GD.PushError($"MUSIC SMOKE FAILED: {ex.Message}\n{ex.StackTrace}"); GetTree().Quit(1); }
    }
}
```

In `godot/DebugMain.cs`, beside the other smoke dispatch lines (after the `--journey-smoke` line near 105):

```csharp
        if (OS.GetCmdlineUserArgs().Contains("--music-smoke")) CallDeferred(nameof(RunMusicSmoke));
```

- [ ] **Step 2: Build to verify it fails**

Run: `dotnet build MangakaGame.sln -warnaserror`
Expected: FAIL, `_music` does not exist.

- [ ] **Step 3: Implement `MusicPlayer`**

`godot/Office/MusicPlayer.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

// Plays what MusicPlan decides (spec 2026-09-28): two players for crossfades, volume, and a fade on window focus loss.
public partial class MusicPlayer : Node
{
    public const string Folder = "res://Assets/Music";
    private const float CrossfadeSeconds = 3, FadeInSeconds = 2, FadeOutSeconds = 1.5f, FocusSeconds = 1;
    private readonly AudioStreamPlayer[] _players = [new(), new()];
    private readonly float[] _gain = [0, 0], _target = [0, 0];
    private IReadOnlyDictionary<string, Func<AudioStream?>> _tracks = new Dictionary<string, Func<AudioStream?>>();
    private MusicPlan _plan = new([], 0);
    private int _active;
    private float _rate = 1 / FadeInSeconds, _focus = 1;
    private bool _started;

    public Action<string>? Failed { get; set; }
    public string? Current => _plan.Current;
    public float ActiveGain => _gain[_active];
    public float InactiveGain => _gain[1 - _active];
    public float ActiveVolumeDb => _players[_active].VolumeDb;
    public bool Paused => _players[_active].StreamPaused;
    public bool Audible => _players.Any(p => p.Playing && !p.StreamPaused);

    public override void _Ready() { foreach (var player in _players) AddChild(player); }

    public void UseFolder(int seed)
    {
        var files = DirAccess.DirExistsAbsolute(Folder) ? DirAccess.GetFilesAt(Folder) : [];
        UseTracks(MusicPlan.Discover(files, Folder).ToDictionary(p => p.Key,
            p => (Func<AudioStream?>)(() => ResourceLoader.Exists(p.Value) ? ResourceLoader.Load<AudioStream>(p.Value) : null)), seed);
    }

    public void UseTracks(IReadOnlyDictionary<string, Func<AudioStream?>> tracks, int seed, double gapMin = 60, double gapMax = 120)
    {
        _tracks = tracks; _plan = new MusicPlan(tracks.Keys, seed, gapMin, gapMax);
        foreach (var player in _players) player.Stop();
        _gain[0] = _gain[1] = _target[0] = _target[1] = 0; _started = false;
    }

    public void Notice(IEnumerable<MusicMoment> moments, MusicContext context) => _plan.Notice(moments, context);

    public void Update(double delta, MusicContext context, double volume, bool focused)
    {
        var active = _players[_active];
        // A track has ended only when it stopped by itself: not paused, not faded out by us.
        var finished = _started && !active.Playing && !active.StreamPaused;
        if (finished) _started = false;
        // Unfocused time does not count toward the quiet gap.
        Apply(_plan.Update(focused ? delta : 0, context, finished));

        _focus = Mathf.MoveToward(_focus, focused ? 1 : 0, (float)delta / FocusSeconds);
        for (var i = 0; i < 2; i++)
        {
            _gain[i] = Mathf.MoveToward(_gain[i], _target[i], (float)delta * _rate);
            if (_gain[i] <= 0 && _target[i] <= 0 && _players[i].Playing) { _players[i].Stop(); if (i == _active) _started = false; }
            var pause = _focus <= 0 && !focused;
            if (_players[i].StreamPaused != pause && (_players[i].Playing || _players[i].StreamPaused)) _players[i].StreamPaused = pause;
            _players[i].VolumeDb = Mathf.LinearToDb(Math.Max(1e-5f, _gain[i] * _focus * (float)Math.Clamp(volume, 0, 1)));
        }
    }

    private void Apply(MusicCommand command)
    {
        switch (command.Transition)
        {
            case MusicTransition.FadeIn: Start(command.TrackId!, 1 / FadeInSeconds); break;
            case MusicTransition.Crossfade: Start(command.TrackId!, 1 / CrossfadeSeconds); break;
            case MusicTransition.FadeOut: _target[_active] = 0; _rate = 1 / FadeOutSeconds; _started = false; break;
        }
    }

    private void Start(string trackId, float rate)
    {
        var stream = _tracks.TryGetValue(trackId, out var load) ? load() : null;
        if (stream is null) { _plan.Remove(trackId); Failed?.Invoke(trackId); return; }
        var next = 1 - _active;
        _target[_active] = 0;
        _players[next].Stream = stream; _players[next].StreamPaused = false; _players[next].Play();
        _gain[next] = 0; _target[next] = 1; _active = next; _rate = rate; _started = true;
    }
}
```

`godot/Assets/Music/README.md`:

```markdown
# Music tracks

Drop converted OGG files here (see `scripts/convert-music.ps1`). The name's
prefix picks when a track plays: `title-`, `day-`, `night-`, `studio-`,
`convention-`, `deadline-`, `good-news-`, `setback-`. Any other name is ignored.
Run the Godot import step after adding files. Record each track's Suno plan,
date and prompt in `docs/superpowers/music-suno-prompts.md`.
```

- [ ] **Step 4: Wire it into the game**

`godot/DebugMain.Alpha.cs`: add the field beside `_audio` (near line 14):

```csharp
    private MusicPlayer _music=null!;
```

In `BuildAlpha`, after `_audio=new OfficeAudio();AddChild(_audio);`:

```csharp
        _music=new MusicPlayer();AddChild(_music);_music.UseFolder(Environment.TickCount);
        _music.Failed=id=>LogTimeline("error music "+id+" could not be loaded");
```

Replace the two-slider loop in the settings (the `foreach(var ambience in new[]{true,false})` block and the sentence after it, near lines 248 to 254) with:

```csharp
        foreach(var (label,read,write) in new (string,Func<double>,Action<double>)[]{
            ("Music · 0 mutes",()=>_presentation.MusicVolume,v=>_presentation.MusicVolume=v),
            ("Office ambience · 0 mutes",()=>_presentation.AmbienceVolume,v=>_presentation.AmbienceVolume=v),
            ("Sound effects · 0 mutes",()=>_presentation.EffectsVolume,v=>_presentation.EffectsVolume=v)})
        {
            Words(parent,label);
            var slider=new HSlider{MinValue=0,MaxValue=1,Step=.05,Value=read()};parent.AddChild(slider);
            slider.ValueChanged+=v=>write(v);
        }
        Words(parent,"Sounds stay natural at every speed. Office ambience pauses in menus, where the title music plays. Everything pauses when the window is unfocused.",14);
```

`godot/DebugMain.cs`: add, near `_Process`:

```csharp
    private MusicContext MusicNow()=>new(_inMenu,_state.Clock.Now,
        _state.Protagonist.Employment is {} job&&_state.Locations.FirstOrDefault(l=>l.Id==job.LocationId) is {IsFamilyHome:false},
        _overnightTarget is not null);
```

In `_Process`, after the `_audio.Update(...)` line:

```csharp
        if(_managementReady)_music.Update(delta,MusicNow(),_presentation.MusicVolume,GetWindow().HasFocus());
```

In `ScanEvents`, replace the milestone line (near line 150):

```csharp
        if(_milestones is not null)foreach(var id in _milestones.Observe(_state,fresh))LogTimeline("milestone "+id);
```

with:

```csharp
        var reached=_milestones?.Observe(_state,fresh).ToList()??[];
        foreach(var id in reached)LogTimeline("milestone "+id);
        if(_managementReady)_music.Notice(MusicMoments.Classify(_state,fresh,reached),MusicNow());
```

- [ ] **Step 5: Build, run the smoke and the checks that touch sound, settings and events**

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
$godot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
& $godot --headless --path godot -- --music-smoke
& $godot --headless --path godot -- --alpha-smoke
& $godot --headless --path godot -- --atmosphere-smoke
& $godot --headless --path godot -- --journey-smoke
& $godot --headless --path godot -- --display-sweep-smoke
```
Expected: build clean; `MUSIC SMOKE PASSED: 10 checks.`; the other checks pass as before (the settings check counts at least three sliders, now four).

- [ ] **Step 6: Commit** (only if the user asked for commits)

```bash
git add godot/Office/MusicPlayer.cs godot/DebugMain.MusicSmoke.cs godot/Assets/Music/README.md godot/DebugMain.Alpha.cs godot/DebugMain.cs
git commit -m "Music: player node, game wiring, Music slider and smoke check"
```

---

### Task 4: Conversion script and records

**Files:**
- Create: `scripts/convert-music.ps1`
- Modify: `docs/superpowers/music-suno-prompts.md` (a short "Adding tracks to the game" section)
- Create: `docs/superpowers/music-system-completion.md`
- Modify: `docs/superpowers/specs/2026-09-22-roadmap.md` (Tier 2 music line), `CLAUDE.md` (section 5)

**Interfaces:**
- Consumes: the prefixes from Task 1.
- Produces: `scripts/convert-music.ps1 -Source <folder> [-OutputDirectory <folder>]`.

- [ ] **Step 1: Write the script**

`scripts/convert-music.ps1`:

```powershell
param(
    [Parameter(Mandatory=$true)][string]$Source,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\godot\Assets\Music')
)
# Converts Suno downloads (WAV or MP3) into loudness-matched OGG files for the game (spec 2026-09-28).
$ErrorActionPreference = 'Stop'
$prefixes = 'title-','day-','night-','studio-','convention-','deadline-','good-news-','setback-'
$ffmpeg = (Get-Command ffmpeg -ErrorAction SilentlyContinue)?.Source
if(-not $ffmpeg) { throw 'ffmpeg was not found. Install it with: winget install Gyan.FFmpeg' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$converted = 0
foreach($file in Get-ChildItem -LiteralPath $Source -File | Where-Object { $_.Extension -in '.wav','.mp3' }) {
    $name = $file.BaseName.ToLowerInvariant()
    if(-not ($prefixes | Where-Object { $name.StartsWith($_) })) { Write-Warning "Skipped $($file.Name): the name must start with one of $($prefixes -join ', ')"; continue }
    $out = Join-Path $OutputDirectory ($name + '.ogg')
    & $ffmpeg -hide_banner -loglevel error -y -i $file.FullName -af 'loudnorm=I=-16:TP=-1.5:LRA=11' -ar 44100 -c:a libvorbis -q:a 5 $out
    if($LASTEXITCODE -ne 0) { throw "ffmpeg failed on $($file.Name)" }
    $converted++
    Write-Output "Converted $($file.Name) -> $out"
}
Write-Output "$converted file(s) converted."
```

- [ ] **Step 2: Try it on generated tones (not kept)**

Run:
```powershell
$src = Join-Path $env:TEMP 'music-convert-src'; $out = Join-Path $env:TEMP 'music-convert-out'
Remove-Item -Recurse -Force $src,$out -ErrorAction SilentlyContinue; New-Item -ItemType Directory $src | Out-Null
ffmpeg -hide_banner -loglevel error -f lavfi -i "sine=frequency=440:duration=3" (Join-Path $src 'day-01.wav')
ffmpeg -hide_banner -loglevel error -f lavfi -i "sine=frequency=440:duration=3" (Join-Path $src 'boss-01.wav')
scripts/convert-music.ps1 -Source $src -OutputDirectory $out
Get-ChildItem $out | Select-Object Name
Remove-Item -Recurse -Force $src,$out
```
Expected: `Converted day-01.wav`, a warning for `boss-01.wav`, "1 file(s) converted.", and only `day-01.ogg` listed.

- [ ] **Step 3: Document adding tracks**

Append to `docs/superpowers/music-suno-prompts.md` (CRLF):

```markdown
## Adding tracks to the game

1. Name each download after its cue (for example `day-01.wav`).
2. Run `scripts/convert-music.ps1 -Source <download folder>`. It writes
   loudness-matched OGG files to `godot/Assets/Music/`. Only those OGG files are
   committed; keep the downloads outside the repository.
3. Run the Godot import step, then start the game. Tracks are found by name.
4. Fill in the provenance table above, and add the credits line to
   `docs/superpowers/private-alpha-credits.txt` with the first real track:
   "Music: AI-generated with Suno (see docs/superpowers/music-suno-prompts.md)."
```

Ruling recorded here: the credits line is added with the first real track, not now, because the current build has no music.

- [ ] **Step 4: Write the completion record and update the roadmap and CLAUDE.md**

`docs/superpowers/music-system-completion.md` in the style of `tier1-closeout-and-tester-a-completion.md`: what changed (`MusicPlan`, `MusicMoments`, `MusicPlayer`, slider, conversion script), what was verified (unit tests, smoke checks, the tone conversion, stated separately), what was not (no real tracks yet; nobody has listened), and the next step (the user generates tracks in Suno when ready). Roadmap Tier 2 music line: add "music system built 2026-09-28, waiting for tracks; see ../music-system-completion.md". CLAUDE.md section 5: one bullet with the same. Convert all changed Markdown to CRLF with `unix2dos`.

- [ ] **Step 5: Full verification**

Run:
```powershell
dotnet test tests/MangakaSim.Tests --filter "Category!=Playtest"
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --smoke-test
& $godot --headless --path godot -- --management-smoke
& $godot --headless --path godot -- --music-smoke
```
Expected: all pass.

- [ ] **Step 6: Commit** (only if the user asked for commits)

```bash
git add scripts/convert-music.ps1 docs/superpowers CLAUDE.md
git commit -m "Music: conversion script and records"
```
