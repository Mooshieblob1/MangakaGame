# Publishing and Market Implementation Plan

**Status: approved plan, implementation in progress.** Completion notes will be written to `docs/superpowers/sub-project-2-completion.md`.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Give finished chapters somewhere to go: six magazines with generated rosters, pitching and serialization offers, an editor gate on the Name stage, chapter quality, weekly issue closes with rankings, fanbase and cultural impact, tankobon and doujin sales into a ledger, genre trends that drift and boom, personal and studio reputation, a protection score, warnings and cancellation, withdraw and end, going online, and a debug scene that exposes all of it.

**Architecture:** Option B from the design brainstorm, recorded in the spec as the chosen approach: partial classes on `GameState` (`GameState.Publishing.cs`, `GameState.Market.cs`, `GameState.Sales.cs`, `GameState.Reputation.cs`) that each add one step to `Tick()`, plus stateless rule modules under `src/MangakaSim/Rules/` that take plain inputs and return numbers and are tested with exact expected values. Static catalog data (publishers, magazines, genres, trend keyframes, internet reach, price index) ships as embedded JSON and is never saved. All randomness goes through `GameState.Rng`. `MangakaSim` still never references Godot.

**Tech Stack:** .NET 8 SDK, C# 12, System.Text.Json, xUnit 2.9, Godot 4.7.2 stable mono.

**Spec:** `docs/superpowers/specs/2026-09-22-publishing-market-design.md` (every section number below refers to it).

## Global Constraints

- Everything in the sub-project 1 plan's global constraints still holds: hourly ticks, `Advance`, `Apply`, validation before mutation, no Godot in `MangakaSim`.
- Tick order becomes: `Clock.Advance`, `WorkStep`, `EditorStep`, `IssueCloseStep`, `SalesStep`, `PitchStep`, `RiskStep`, `DayEndStep`, midnight rollover.
- Save format `Version` = 2. Version 1 saves are rejected by the existing "not supported" error.
- All catalog yen amounts are 1996 prices multiplied by `Economy.PriceIndex` at the moment of use. Ledger amounts are nominal.
- New `EventType` values are appended after `CommandApplied` in the spec's order so existing saves' enum names stay valid.
- Every commit message ends with the attribution lines given by the session.
- Tooling for this session: `dotnet` from the Ubuntu 24.04 `dotnet-sdk-8.0` package; Godot 4.7.2 mono Linux downloaded from GitHub releases into the session scratchpad for the headless smoke run. On the owner's Windows machine the paths in the sub-project 1 plan apply.

## Decisions locked in by this plan (not spelled out in the spec)

- **Decisions log.** The "decisions log" referenced when this plan was commissioned is not in the repository or any connected document store. The only recorded option choice is the spec's "Approach chosen: partial classes on `GameState` plus pure rule modules", so this plan takes that as option B and follows the spec literally elsewhere. If the log's option B meant something else, the affected tasks are the ones under "Architecture" above.
- **Plan format.** The sub-project 1 plan embedded every source file. This plan embeds the contracts (rule signatures, catalog JSON, model shapes, event order) and the exact test expectations, and specifies state-side behaviour in prose. The source tree is the record of the implementation, as the sub-project 1 completion notes already established.
- **RNG helpers.** `Rng` gains `NextDouble()` in `[0, 1)`, `NextDouble(min, max)`, and `NextInt(minInclusive, maxInclusive)`. The existing `NextInt(maxExclusive)` is unchanged. The spec's `Rng.NextInt(1, 3) years` means one, two or three years.
- **Catalog on the state.** `GameState` holds `[JsonIgnore] PublisherCatalog Publishers` and `[JsonIgnore] TrendCatalog TrendData`, both defaulting to `LoadDefault()`. `FromJson` uses the defaults; tests can swap in `FromJson` catalogs before `NewGame`.
- **Pitching replaces an untouched open chapter.** Under the planner an Active series always has an open chapter. `PitchSeries` is valid when that chapter has no stage started or skipped; the untouched chapter is removed and the one-shot takes its number. A chapter with any progress rejects the pitch ("finish the current chapter first").
- **One-shot resolution timing.** A one-shot resolves in the tick in which its magazine closes an issue and `now >= DueDate`. `MagazineState.LastIssueClose` records the most recent close so `PitchStep` can see that a close happened this tick without a transient field.
- **Offer timing.** `FirstIssueClose` is the fourth close counting the magazine's next close as the first, so `NextIssueClose + 3 × cadence` right after a close.
- **Boom bookkeeping.** `GenreTrend` gains `bool BoomFading` so the fade-start roll for `BoomFloor` happens exactly once. A new boom starting on top of a residual floor sets `BoomPeak = min(1.0, Boom + draw)` so `Boom` stays within the save invariant `0..1`.
- **Trend month.** `LastTrendUpdateMonth` starts at the game's first month (April 1996), so the first monthly update runs at the first issue close of May 1996.
- **Contract grace.** `Contract` carries `int ChaptersPublished` (chapters published under this contract). The first eight are the grace period; `Series.ChaptersPublished` stays the lifetime count.
- **Clocks are rounded.** Warning clock `round(3 + 9P)`, cancel clock `round(3 + 23P)`, strike lifetime `round(8 − 6P)` issues. A strike's age in issues is `floor(days / cadenceDays)`.
- **Surviving a roll restarts the warning clock.** `WarningIssuedAt = now` after `CancellationSurvived`; the warning stays active. Without this the roll would repeat every issue.
- **RankingPublished** fires only for magazines where a player series published this issue; six silent magazines a week would drown the log.
- **Zero-yen ledger entries are not written.** A released volume that sells no copies in a week gets no royalty entry.
- **Issue close advance exists from Task 4** so the save invariant `NextIssueClose >= Clock.Now` holds before Task 8 fills in publishing and rankings.
- **Volumes** carry `bool IsReleased` so `VolumeReleased` fires once. Doujin volumes are created released. Chapters "not in any volume" are those whose number lies outside every volume's `[FirstChapter, LastChapter]` range.
- **Million-seller influence** is tracked by `Series.MillionInfluenceGiven`.
- **Convention recap** runs before that Monday's sales so it sums the previous month only.
- **Withdraw due date.** The open chapter's new due date is `CadenceRules.NextDue(now, series.Cadence)`.
- **Recap yen.** `GameState.LedgerWindowStart` mirrors `RecapWindowStart`; `YenEarned` is the sum of ledger amounts appended since it.
- **Pitching invariant.** `Pitching ⇒ at least one one-shot chapter with a valid PitchMagazineId`. A finished one-shot waiting for its magazine's close is legal.
- **Effective reputation with one person** is `0.5 × StudioTrackRecord + 0.5 × Aki.Reputation`.
- **Economic curves step by calendar year.** The spec wants the price index to read exactly 1.00 on 1 April 1996 and internet reach 0.1 through 1996, which fractional-year interpolation cannot give, so `Economy.PriceIndex` and `Economy.InternetReach` interpolate on the integer year. Genre baselines keep the spec's fractional-year interpolation.
- **Skipped Tones quality.** The spec's example value 74 contradicts its formula: 84 − 0.10 × 100 × 0.84 = 75.6, which rounds to 76. The formula wins; the test asserts 76.
- **Filler words** live in `Rules/FillerRules.cs` as two static arrays (40 adjectives, 40 nouns). Filler ids come from a per-magazine counter (`MagazineState.NextFillerId`), matching the spec's "unique per magazine" invariant and leaving the global id counter, which sub-project 1 tests hard-code, untouched.

## File structure

| Path | Responsibility |
|---|---|
| `src/MangakaSim/Catalog/PublisherCatalog.cs` | `Publisher`, `Magazine`, `Demographic`, load and validate `publishers.json` |
| `src/MangakaSim/Catalog/TrendCatalog.cs` | genre list, keyframes, internet reach, price index, load and validate `trends.json` |
| `src/MangakaSim/Data/publishers.json`, `Data/trends.json` | embedded resources |
| `src/MangakaSim/Market.cs` | `MagazineState`, `FillerSeries`, `RankEntry`, `GenreTrend`, `LedgerEntry`, `Volume`, `VolumeFormat`, `Contract`, `SerializationOffer`, `PublishingStatus`, `EditorStatus` |
| `src/MangakaSim/Rules/Economy.cs` | price index |
| `src/MangakaSim/Rules/TrendRules.cs` | baseline, effective, crowding, normalise |
| `src/MangakaSim/Rules/QualityRules.cs` | skill factor, rush factor, contribution, quality |
| `src/MangakaSim/Rules/PitchRules.cs` | chance, weakest factor, fee |
| `src/MangakaSim/Rules/EditorRules.cs` | threshold, approval probability, review hours |
| `src/MangakaSim/Rules/RankingRules.cs` | fan score, player and filler scores, K by tier |
| `src/MangakaSim/Rules/FanbaseRules.cs` | rank factor, base readers, issue gain |
| `src/MangakaSim/Rules/SalesRules.cs` | tankobon and doujin weekly copies, cover prices, royalties |
| `src/MangakaSim/Rules/ReputationRules.cs` | effective reputation, protection, ending bonus, withdraw penalty |
| `src/MangakaSim/Rules/CancellationRules.cs` | clocks, strike expiry, cancel chance |
| `src/MangakaSim/Rules/FillerRules.cs` | word lists, popularity ranges, drift |
| `src/MangakaSim/GameState.Publishing.cs` | `EditorStep`, `PitchStep`, pitch, accept, decline, expiry |
| `src/MangakaSim/GameState.Market.cs` | `IssueCloseStep`, rankings, fillers, monthly trends, player influence |
| `src/MangakaSim/GameState.Sales.cs` | `SalesStep`, volumes, doujin, ledger, online |
| `src/MangakaSim/GameState.Reputation.cs` | reputation deltas, protection, cancellation rule, cancel, withdraw, end, iconic |
| `tests/MangakaSim.Tests/Rules/*Tests.cs` | one test file per rule module |
| `tests/MangakaSim.Tests/CatalogTests.cs` | catalog loading, validation, spread rule |
| `tests/MangakaSim.Tests/QualityStateTests.cs`, `EditorTests.cs`, `PitchTests.cs`, `MarketTests.cs`, `CancellationTests.cs`, `SalesTests.cs`, `SaveV2Tests.cs` | state tests |
| `docs/superpowers/sub-project-2-completion.md` | completion notes |

Modified: `Rng.cs`, `Model.cs`, `GameEvent.cs`, `EventType.cs`, `Settings.cs`, `Commands.cs`, `GameState.cs`, `GameState.Planner.cs`, `GameState.Work.cs`, `GameState.Commands.cs`, `GameState.Serialization.cs`, `GameState.Recap.cs`, `MangakaSim.csproj`, `godot/DebugMain.cs`, `godot/DebugMain.SmokeTest.cs`, `README.md`, `docs/superpowers/specs/2026-09-22-roadmap.md`.

---

### Task 1: RNG helpers, events, settings

**Files:**
- Modify: `src/MangakaSim/Rng.cs`, `EventType.cs`, `GameEvent.cs`, `GameState.cs` (Emit overload), `Settings.cs`
- Test: `tests/MangakaSim.Tests/RngTests.cs`, extend `ModelTests.cs`

**Interfaces:**
- `double Rng.NextDouble()` in `[0,1)` from the top 53 bits; `double NextDouble(double min, double max)`; `int NextInt(int minInclusive, int maxInclusive)`.
- `EventType` appends, in order: `PitchSubmitted, PitchRejected, SerializationOffered, OfferAccepted, OfferDeclined, OfferExpired, EditorApproved, EditorRedoRequested, ChapterPublished, IssueMissed, RankingPublished, CancellationWarning, CancellationWarningLifted, CancellationSurvived, SeriesCancelled, SeriesWithdrawn, SeriesEnded, SeriesBecameIconic, VolumeScheduled, VolumeReleased, VolumeMilestone, ConventionRecap, GenreTrendShifted, WentOnline`.
- `GameEvent` gains `string? MagazineId`, `int? VolumeId`, `int? Rank`, `long? Amount`.
- `record EventContext(int? SeriesId = null, int? ChapterNumber = null, int? PersonId = null, Stage? Stage = null, string? MagazineId = null, int? VolumeId = null, int? Rank = null, long? Amount = null)` and `Emit(EventType, string, EventContext)`.
- `DailyRecapPayload` gains `long YenEarned`, `int ChaptersPublished`, `int IssuesMissed`.
- `Settings.Default()` sets `SerializationOffered, PitchRejected, EditorRedoRequested, CancellationWarning, SeriesCancelled, VolumeMilestone, ConventionRecap, SeriesBecameIconic` to true.

- [ ] **Step 1: Write the failing tests.** `NextDouble` stays in `[0,1)` over 10,000 draws and is deterministic for a seed; `NextInt(1, 3)` only yields 1, 2, 3 and hits all three; `NextDouble(-0.02, 0.02)` stays in range. `EventType` has 35 values with `PitchSubmitted == 11` and `WentOnline == 34`. Defaults include the eight new auto-pause keys.
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run tests to verify they pass.**
- [ ] **Step 5: Commit** `feat(sim): rng helpers, publishing event types and auto-pause defaults`.

---

### Task 2: Catalog data and loaders

**Files:**
- Create: `src/MangakaSim/Catalog/PublisherCatalog.cs`, `Catalog/TrendCatalog.cs`, `Data/publishers.json`, `Data/trends.json`
- Modify: `src/MangakaSim/MangakaSim.csproj` (`<EmbeddedResource Include="Data\*.json" />`)
- Test: `tests/MangakaSim.Tests/CatalogTests.cs`

**Interfaces:**

```csharp
public enum Demographic { Shonen, Shojo, Seinen, Josei }
public sealed class Publisher { string Id; string Name; }
public sealed class Magazine {
  string Id; string PublisherId; string Name; int Tier; Cadence Cadence; Demographic Demographic;
  Dictionary<string,double> GenreAffinities; int RosterSize; int CancellationRank;
  int FeePerPageMin; int FeePerPageMax; DayOfWeek IssueCloseDay; int IssueCloseHour; int ChaptersPerVolume;
  double Affinity(string normalisedGenre);      // 1.0 when unlisted
  int CadenceDays;                              // 7, 14, 28
}
public sealed class PublisherCatalog {
  IReadOnlyList<Publisher> Publishers; IReadOnlyList<Magazine> Magazines;
  Magazine? Find(string id); Magazine Require(string id);
  static PublisherCatalog LoadDefault(); static PublisherCatalog FromJson(string json);
}
public sealed class TrendCatalog {
  IReadOnlyList<string> Genres; IReadOnlyDictionary<string, SortedDictionary<int,double>> Keyframes;
  SortedDictionary<int,double> InternetReach; SortedDictionary<int,double> PriceIndex;
  static TrendCatalog LoadDefault(); static TrendCatalog FromJson(string json);
}
```

`publishers.json` holds the six magazines of spec section 2 exactly (ids `tokiwa-jump`, `tokiwa-square`, `kaidan-magazine`, `kaidan-afternoon`, `hoshigaku-sunday`, `hoshigaku-flowers`) with the affinity table (1.2 boosts, 1.1 mild, 0.9 cool, 0.8 cold). `trends.json` holds the genre list, the baseline table of section 3, the internet reach curve of section 8, and the price index table of section 11.

Validation throws `InvalidDataException` for: duplicate ids, unknown publisher id, `RosterSize <= CancellationRank`, `FeePerPageMin >= FeePerPageMax`, affinity outside 0.75..1.25, tier outside 1..3, hour outside 0..23, keyframe years not strictly increasing, missing `other` genre, keyframes for a genre not in the list, and the spread rule (per year: at least two genres ≤ 0.70, at least two ≥ 1.20, mean excluding `other` within 0.90..1.10).

- [ ] **Step 1: Write the failing tests.** Default catalog loads six magazines and three publishers; `tokiwa-jump` is tier 1 weekly Shonen with roster 20, line 15, fees 9,000–20,000, Thursday 18:00, 9 chapters per volume; `Affinity("action") == 1.2`, `Affinity("horror") == 1.0`; every affinity in range and roster > line for all six. `trends.json` has twelve genres including `other`, the spread rule holds for every keyframe year, `PriceIndex[1996] == 1.00`, `InternetReach[2005] == 1.0`. Each validation rule is exercised with a mutated JSON copy that throws.
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement the two catalogs and data files.**
- [ ] **Step 4: Run tests to verify they pass.**
- [ ] **Step 5: Commit** `feat(sim): publisher and trend catalogs as embedded json`.

---

### Task 3: Pure rule modules

**Files:**
- Create: `src/MangakaSim/Rules/{Economy,TrendRules,QualityRules,PitchRules,EditorRules,RankingRules,FanbaseRules,SalesRules,ReputationRules,CancellationRules,FillerRules}.cs`
- Test: `tests/MangakaSim.Tests/Rules/*Tests.cs`

**Interfaces (all `public static`, no `GameState`):**

```csharp
Economy.PriceIndex(TrendCatalog c, DateTime now)          // interpolate; after last keyframe × 1.02^years
Economy.Interpolate(SortedDictionary<int,double> curve, DateTime now, bool growAfterLast)
TrendRules.Normalise(string genre, TrendCatalog c)         // trim, lower, unknown -> "other"
TrendRules.Baseline(TrendCatalog c, string genre, DateTime now)
TrendRules.Effective(GenreTrend t, TrendCatalog c, DateTime now)   // clamp 0.2..1.8
TrendRules.Crowding(int sameGenreOthers)                    // max(0.85, 1 - 0.03 × max(0, n - 2))
QualityRules.Weight(Stage)                                  // 0.35 0.30 0.15 0.10 0.10
QualityRules.SkillFactor(int skill, int redoCount = 0)      // 0->0.2, 50->0.6, 100->1.0 (+0.05/redo, cap 1.0)
QualityRules.RushFactor(double overtimeHours, double hoursRequired)  // max(0.7, 1 - 0.5 × ot/req)
QualityRules.Contribution(Stage, int skill, double overtime, double required, int redoCount)
QualityRules.Quality(IEnumerable<double> contributions)     // clamp(round(sum), 0, 100)
PitchRules.Base(int tier)                                   // 0.15 0.30 0.50
PitchRules.QualityFactor(int quality)                       // lerp 50->0.5, 100->1.6, unclamped
PitchRules.ReputationFactor(double effectiveRep)            // lerp 0->0.6, 100->1.6
PitchRules.Chance(int tier, int quality, double rep, double affinity, double trend)  // clamp 0.02..0.95
PitchRules.WeakestFactor(int quality, double rep, double affinity, double trend)      // "quality" | "reputation" | "fit" | "trend"
PitchRules.FeePerPage(Magazine m, double rep, double priceIndex)  // round(lerp × index) to nearest 100
EditorRules.Threshold(int tier, double rep)                 // T0 65/55/45, T100 50/40/30
EditorRules.ApproveChance(double nameQuality, double threshold)   // clamp(0.5 + 0.0225 × diff, 0.05, 0.95)
EditorRules.ReviewHours(int tier)                           // 48 36 24
RankingRules.K(int tier)                                    // 200000 100000 50000
RankingRules.FanScore(double fanbase, int tier)
RankingRules.PlayerScore(int quality, double fanbase, int tier, double affinity, double trend, double crowding)
RankingRules.FillerScore(double popularity, double crowding)
FanbaseRules.BaseReaders(int tier)                          // 3000 1500 800
FanbaseRules.RankFactor(int rank, int line, int roster)     // piecewise 1->3.0, line->0.5, roster->0.2
FanbaseRules.IssueGain(int tier, double rankFactor, int quality)  // base × rf × q/70
SalesRules.TankobonCopies(int week, double fanbase, double avgQuality, double trend)
SalesRules.DoujinCopies(int week, double fanbase, double avgQuality, double trend, double reach)
SalesRules.DoujinWindow(bool online)                        // 4 or 8
SalesRules.TankobonCover(double priceIndex) / DoujinCover(double priceIndex)   // 400 / 500 × index
SalesRules.Royalty(long copies, double cover) / DoujinIncome(long copies, double cover)  // 10% / 60%
ReputationRules.StaffTerm(IEnumerable<double> reputations)  // top 3, weights 0.5/0.3/0.2 renormalised
ReputationRules.Effective(double trackRecord, double staffTerm)
ReputationRules.Protection(int chaptersPublished, double fanbase, double impact)
ReputationRules.EndingBonus(int line, double avgRank, long totalCopies)   // 1 + 4 × clamp × min(1, copies/500k), 1 dp
ReputationRules.WithdrawPenalty(int chaptersPublished)      // -3 - 0.05 × n, floor -10
CancellationRules.WarningClock(double p) / CancelClock(double p) / StrikeLifetime(double p)
CancellationRules.LiveStrikes(IEnumerable<DateTime> strikes, DateTime now, int cadenceDays, int lifetime)
CancellationRules.CancelChance(double effectiveRep)         // 1 - 0.4 × rep/100
FillerRules.Adjectives / Nouns (≥ 40 each); PopularityRange(int tier); ReplacementRange = (45, 65); IconicRange = (85, 95)
```

- [ ] **Step 1: Write the failing tests** with these exact expectations:
  - `Economy`: 1996-04-01 → 1.00; 1999-07-01 → 1.015; 2030-01-01 → `1.18 × 1.02^4` (within 1e-9).
  - `TrendRules`: `slice of life` 2002-07-02 (midway 2000→2005) → 0.60; 2025 holds 1.00 for slice of life; `Crowding` for 0..8 → 1, 1, 1, 0.97, 0.94, 0.91, 0.88, 0.85, 0.85; `Normalise("  Sci-Fi ")` → `sci-fi`, `"isekai"` → `other`.
  - `QualityRules`: skill 80 everywhere → 84; Tones skipped → 76 (see decisions); Pencils with 20% overtime → Pencils contribution 30 × 0.84 × 0.9 = 22.68 and chapter quality 82; `SkillFactor(80, 4) == 1.0` (0.84 + 0.20 capped); `SkillFactor(0) == 0.2`, `(50) == 0.6`, `(100) == 1.0`.
  - `PitchRules`: tier 1, quality 84, rep 25, affinity 1.2, trend 1.3: qf 1.248, rf 0.85, raw 0.15 × 1.248 × 0.85 × 1.2 × 1.3 = 0.2482272 → chance 0.248227 (6 dp); tier 3, quality 100, rep 100, 1.25, 1.8 clamps to 0.95; quality 40 → qf 0.28 and chance clamps to 0.02 in a cold slot; `WeakestFactor(84, 25, 1.2, 1.3) == "reputation"`, `(84, 90, 0.8, 1.3) == "fit"`; fee rep 50 at `tokiwa-jump` index 1.00 → 14,500; index 1.18 → 17,100.
  - `EditorRules`: threshold tier 1 rep 0 → 65; tier 1 rep 100 → 50; tier 2 rep 50 → 47.5; chance at threshold → 0.5, at +20 → 0.95, at −20 → 0.05.
  - `RankingRules`: `FanScore(200000, 1) == 50`; player score quality 84, fanbase 0, tier 1, 1.2, 1.3, 1.0 → 50.4 × 1.56 = 78.624.
  - `FanbaseRules`: rank 1 → 3.0, line → 0.5, roster → 0.2, above roster → 0.2; rank 8 with line 15 → 2.0 (halfway); gain tier 1 rf 3.0 quality 84 → 3000 × 3 × 1.2 = 10800.
  - `SalesRules`: fanbase 10,000, quality 84, trend 1.0: week 1 → 7200; week 5 → `10000 × 0.04 × 1.2 × 0.93^3 = 386.15…` → 386; doujin week 1 fanbase 0 quality 84 no internet → 240; with reach 1.0 → 360; week 2 → 96 / 144.
  - `ReputationRules`: protection 300 chapters, 500k fans, impact 50 → 0.7; staff term of `[10]` → 10, of `[50, 30, 10, 90]` → 0.5×90 + 0.3×50 + 0.2×30 = 66; ending bonus line 15, avg rank 5, 500k copies → 1 + 4 × (10/15) = 3.7 (1 dp); withdraw penalty 20 chapters → −4, 200 chapters → −10.
  - `CancellationRules`: P = 0 → 3, 3, 8; P = 1 → 12, 26, 2; a strike 8 weeks old with lifetime 8 on a weekly is dropped, 7 weeks old kept; chance rep 50 → 0.8.
  - `FillerRules`: both word lists ≥ 40 distinct entries; ranges (40,95), (35,85), (30,75).
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement the rule modules.**
- [ ] **Step 4: Run tests to verify they pass.**
- [ ] **Step 5: Commit** `feat(sim): pure publishing, market, sales and reputation rules`.

---

### Task 4: Model additions, new-game setup, save version 2

**Files:**
- Create: `src/MangakaSim/Market.cs`
- Modify: `Model.cs`, `GameState.cs`, `GameState.Serialization.cs`, `GameState.Recap.cs`
- Test: extend `GameStateTickTests.cs`, `SerializationTests.cs`; create `MarketTests.cs` (roster part)

**Interfaces:**

```csharp
public enum PublishingStatus { Unpublished, Pitching, Offered, Serialized }
public enum EditorStatus { NotRequired, AwaitingReview, Approved, RedoRequested }
public enum VolumeFormat { Tankobon }
public class LedgerEntry { DateTime Time; long Amount; string Reason; int? SeriesId; }
public class MagazineState { string MagazineId; DateTime NextIssueClose; DateTime? LastIssueClose; List<FillerSeries> Fillers; List<RankEntry> LastRanking; int IssuesClosed; }
public class FillerSeries { int Id; string Title; string Genre; double Popularity; bool IsIconic; int IssuesBelowLine; }
public class RankEntry { int Rank; string Title; int? SeriesId; int? FillerId; double Score; }
public class GenreTrend { string Genre; double Noise; double Boom; double BoomPeak; double BoomFloor; bool BoomFading; DateTime? BoomEndsAt; DateTime? BoomFadeEndsAt; double PlayerInfluence; }
public class Volume { int Id; int Number; VolumeFormat Format; int FirstChapter; int LastChapter; DateTime ReleaseDate; bool IsReleased; long CopiesSold; int WeeksOnSale; bool IsDoujin; double AverageQuality; }
public class Contract { string MagazineId; int FeePerPage; DateTime SignedAt; int ChaptersPublished; }
public class SerializationOffer { string MagazineId; int FeePerPage; DateTime FirstIssueClose; DateTime ExpiresAt; }
```

`Series` gains `Publishing, Contract, PendingOffer, Fanbase, CulturalImpact, IsIconic, MillionInfluenceGiven, Strikes, WeeksBelowLine, WarningIssuedAt, PitchCooldowns, Volumes, ChaptersPublished, LastRank`. `Chapter` gains `Quality, Editor, EditorDecisionAt, RedoCount, IsOneShot, PitchMagazineId, PublishedAt, Rank`. `StageWork` gains `OvertimeHours, Contribution`. `Person` gains `Reputation`. `GameState` gains `Money, Ledger, StudioTrackRecord, Markets, Trends, HasInternet, LastTrendUpdateMonth, DoujinCopiesThisMonth, DoujinFansThisMonth, LedgerWindowStart` and the two `[JsonIgnore]` catalogs. `CurrentVersion = 2`.

`NewGame`: Aki `Reputation = 10`; `Money = 500,000` with no ledger entry; one `MagazineState` per catalog magazine with `NextIssueClose` = first close day/hour at or after start; `RosterSize − 1` fillers per magazine (title, weighted genre, popularity by tier, tier 1 iconic re-roll); one `GenreTrend` per genre; `LastTrendUpdateMonth = 1996-04-01`. `AddLedger(long amount, string reason, int? seriesId)` appends and updates `Money`.

- [ ] **Step 1: Write the failing tests.** New game has six markets in catalog order, `tokiwa-jump` next close `1996-04-04 18:00`, `hoshigaku-sunday` `1996-04-02 18:00`, `kaidan-magazine` `1996-04-03 18:00`; 19 fillers in Jump, 13 in Flowers; every filler popularity within its tier range; exactly one iconic filler per tier 1 magazine at 85–95 and none elsewhere; filler genres all in the catalog list; twelve trends; `Money == 500000`; Aki reputation 10; same seed gives identical rosters, different seeds differ. Save JSON contains `"Version": 2`; loading `"Version": 1` fails with "not supported"; round trip identical; missing `Markets` or `Trends` rejected.
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement.** Also bump `FromJson` required properties and `ValidateSave`: exactly one market per catalog magazine, closes on the hour and `>= Clock.Now`, unique filler ids, rank entries point at existing rows, one trend per genre with `|Noise| <= 0.15`, `0 <= Boom <= 1`, `0 <= PlayerInfluence <= 0.5`, `Money == 500000 + Σ Ledger`, reputations and track record in 0..100.
- [ ] **Step 4: Run all tests.**
- [ ] **Step 5: Commit** `feat(sim): market model, rosters, ledger and save version 2`.

---

### Task 5: Quality, overtime tracking, personal reputation, doujin volumes

**Files:**
- Modify: `GameState.Work.cs`, `GameState.Commands.cs` (skip path), `GameState.Planner.cs` (page override)
- Create: `GameState.Reputation.cs` (chapter-completion reputation), `GameState.Sales.cs` (doujin volume creation only)
- Test: `tests/MangakaSim.Tests/QualityStateTests.cs`

Behaviour: `WorkStep` adds 1 to `StageWork.OvertimeHours` for an overtime hour. When a stage completes, `OnStageFinished(chapter, work, person)` sets `Contribution` from `QualityRules` using the assignee's skill and `RedoCount` for Name; a skipped stage gets 0. `CompleteChapterIfDone` computes `Quality`, puts it in the `ChapterCompleted` message ("… finished, quality 84"), applies `(Quality − 60) / 20 × HourShare` (halved for doujin) to each contributor, skips `DeadlineMissed` for Serialized series, and calls `TryCreateDoujinVolume(series)`: an Unpublished series with ≥ 5 finished chapters outside every volume gets a `Volume { IsDoujin = true, IsReleased = true, ReleaseDate = now }`, `VolumeReleased`, and `+0.5` track record when `AverageQuality >= 75`.

- [ ] **Step 1: Write the failing tests.** Clean solo weekly chapter completes with quality 84 and the message contains "quality 84"; Aki's reputation rises by 1.2 × 0.5 = 0.6 for a doujin chapter; Tones skipped → 74; an at-risk weekly with overtime records `OvertimeHours > 0` on the stage that ran late and its contribution is below the clean value; fifth finished chapter creates a doujin volume covering chapters 1–5 with `AverageQuality == 84`, emits `VolumeReleased`, and adds 0.5 track record; the sixth chapter creates no volume.
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run all tests.**
- [ ] **Step 5: Commit** `feat(sim): chapter quality, overtime tracking, personal reputation and doujin volumes`.

---

### Task 6: Editor gate

**Files:**
- Create: `GameState.Publishing.cs` (`EditorStep`, `SubmitForReview`, redo handling)
- Modify: `GameState.Planner.cs` (`IsStartable`), `GameState.cs` (`Tick`)
- Test: `tests/MangakaSim.Tests/EditorTests.cs`

Behaviour per spec section 5. `ReviewMagazine(chapter)` is the contract magazine for a Serialized series or `PitchMagazineId` for a one-shot; `RequiresEditor(chapter)` is true only in those cases. Name completion or skip on such a chapter sets `AwaitingReview` and `EditorDecisionAt = now + ReviewHours(tier)`. `IsStartable` returns false for Pencils while `AwaitingReview`. `EditorStep` resolves due reviews: `RedoCount == 2` approves; otherwise one `Rng.NextDouble()` against `EditorRules.ApproveChance(nameQuality, threshold)`. Redo resets Name, `RedoCount++`, Name assignee −0.5 reputation, studio −0.25 track record, emits `EditorRedoRequested` with the Name quality, and runs the planner.

- [ ] **Step 1: Write the failing tests** using a series forced to Serialized in a tier 3 magazine by setting `Publishing`, `Contract` and `Cadence` directly: after Name completes Pencils is not startable and Aki's `CurrentTask` is null (or another series' work); at `EditorDecisionAt` with a seed that approves, `EditorApproved` fires and Pencils starts; with a seed that rejects, Name is back to NotStarted with `RedoCount == 1`, reputation dropped 0.5, track record dropped 0.25; a third submission is approved regardless of seed; a doujin chapter never enters review.
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run all tests.**
- [ ] **Step 5: Commit** `feat(sim): editor gate on the name stage`.

---

### Task 7: Pitching, offers, serialized due dates

**Files:**
- Modify: `Commands.cs`, `GameState.Commands.cs`, `GameState.Publishing.cs`, `GameState.Planner.cs`
- Test: `tests/MangakaSim.Tests/PitchTests.cs`

Commands `PitchSeriesCommand(int SeriesId, string MagazineId)`, `AcceptOfferCommand(int SeriesId)`, `DeclineOfferCommand(int SeriesId)`. Validation and effects per spec section 4 and the "pitching replaces an untouched open chapter" decision. `CreateNextChapter(series, oneShot: false, pagesOverride: null)` sets a Serialized chapter's due date to the first close strictly after the previous chapter's due date and a one-shot's to the first close at least 14 days out. `PitchStep` resolves one-shots and expires offers.

- [ ] **Step 1: Write the failing tests.** `PitchSeries` on a fresh series replaces the untouched chapter 1 with a 31-page one-shot (Name needs 37.2 h) due at Flowers' first close ≥ 14 days out (`1996-04-17 18:00`), sets `Pitching`, emits `PitchSubmitted`; pitching an unknown magazine, a Pitching series, or a series with a started chapter throws and changes nothing; after the one-shot completes and is approved, resolution happens at the close: with a rejecting seed `PitchRejected` names a factor, cooldown is 26 weeks, and a second pitch throws; with an accepting seed `SerializationOffered` carries a fee equal to `PitchRules.FeePerPage` and `FirstIssueClose == 4th close`; `AcceptOffer` makes the series Serialized with `Cadence == Monthly` and the open chapter due at `FirstIssueClose` with `Editor == NotRequired`; `DeclineOffer` returns to Unpublished without cooldown; an unanswered offer expires at `FirstIssueClose` with `OfferExpired`.
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run all tests.**
- [ ] **Step 5: Commit** `feat(sim): pitching, serialization offers and serialized due dates`.

---

### Task 8: Issue close, rankings, fanbase, fillers, trends

**Files:**
- Create: `GameState.Market.cs`
- Modify: `GameState.cs` (`Tick`), `GameState.Reputation.cs` (iconic check, track record deltas)
- Test: `tests/MangakaSim.Tests/MarketTests.cs`

Behaviour per spec section 6 with the decisions above. `IssueCloseStep` handles each magazine whose `NextIssueClose <= now`: publish or miss, score, record, fanbase, cultural impact, reputation and cancellation hooks (Task 9 fills the cancellation rule; this task records strikes and below-line counts), filler drift and retirement, advance, then the monthly trend update (`UpdateTrendsMonthly`) with layers 2 and 3 and `GenreTrendShifted` events. `CheckIconic(series)` runs after every fanbase or impact change.

- [ ] **Step 1: Write the failing tests.** A serialized series whose chapter is complete and approved at the close gets `ChapterPublished`, a `"chapter fee"` ledger entry of `Pages × FeePerPage`, `ChaptersPublished == 1`, a rank, `LastRanking` with `RosterSize` rows, and fanbase `> 0`; an unready chapter gets `IssueMissed`, one strike (after grace is disabled by setting `Contract.ChaptersPublished = 8`), due date moved one cadence, fanbase × 0.97, and no row in the table; fillers drift within ±3 and stay in 5..100; a filler forced to popularity 5 in a magazine with 12 closes is replaced with a new id and 45–65 popularity while a forced-low iconic filler survives; `NextIssueClose` advances by 28 days for a monthly magazine; the first close in May 1996 runs one monthly trend update even though several magazines close that month (noise changed for exactly one update, `LastTrendUpdateMonth == 1996-05-01`); a forced boom (set `BoomEndsAt` past and `BoomFading == false`) emits `GenreTrendShifted` at fade start; a top-3 finish with quality ≥ 80 adds 0.01 player influence; cultural impact rises 0.05 per publish and 0.1 per top-3; a series at impact 90 and fanbase 1,000,000 becomes Iconic once and takes no strike on a later miss.
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run all tests.**
- [ ] **Step 5: Commit** `feat(sim): issue close, rankings, fanbase, filler churn and genre trends`.

---

### Task 9: Reputation, protection, cancellation, withdraw, end

**Files:**
- Modify: `GameState.Reputation.cs`, `Commands.cs`, `GameState.Commands.cs`, `GameState.Market.cs` (hook)
- Test: `tests/MangakaSim.Tests/CancellationTests.cs`

Commands `WithdrawSeriesCommand(int SeriesId)`, `EndSeriesCommand(int SeriesId)`. Behaviour per spec section 9 plus the rounding and warning-restart decisions. `Cancel(series)`, `Withdraw(series)`, `End(series)` share `ClearContract(series)` and `ScheduleFinalVolume(series)`.

- [ ] **Step 1: Write the failing tests.** Below-line ranks accumulate `WeeksBelowLine`; at the warning clock `CancellationWarning` fires once; recovering above the line lifts it; three live strikes trigger exactly one roll: a surviving seed emits `CancellationSurvived`, halves strikes and restarts the warning clock, a cancelling seed emits `SeriesCancelled`, ends the series, clears the contract, drops the open chapter, sets a 52-week cooldown, and applies −2 to Aki and −8 to track record; a strike older than its lifetime is dropped; `WithdrawSeries` clears the contract, multiplies fanbase by 0.9, keeps the open chapter as doujin with `Editor == NotRequired`, applies the penalty; `EndSeries` with 12 published chapters gives a proper ending bonus computed from `ReputationRules.EndingBonus` and emits `SeriesEnded`, with fewer applies the withdraw penalty; a doujin series ends free; both commands validate.
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run all tests.**
- [ ] **Step 5: Commit** `feat(sim): protection, cancellation, withdraw and end`.

---

### Task 10: Sales, volumes, ledger, going online, recap counters

**Files:**
- Modify: `GameState.Sales.cs`, `GameState.Market.cs` (schedule tankobon after publish), `GameState.Recap.cs`, `Commands.cs`, `GameState.Commands.cs`, `GameState.cs`
- Test: `tests/MangakaSim.Tests/SalesTests.cs`, extend `RecapTests.cs`

Command `GetOnlineCommand()`. `SalesStep` per spec sections 7 and 8: release check every tick, Monday 00:00 sales for tankobon and doujin volumes, royalties and doujin income into the ledger, fanbase gains, `VolumeMilestone` with track record and player influence side effects, word of mouth when online, convention recap on the first Monday of a month. Tankobon scheduling after `ChapterPublished` and final volumes on end or cancel. `DailyRecapPayload` counters filled from the window.

- [ ] **Step 1: Write the failing tests.** After the ninth publish in Jump a tankobon is scheduled six weeks after the close with `AverageQuality` equal to the mean quality and `VolumeScheduled` fires; at its release date `VolumeReleased` fires once; the next Monday sells `floor(Fanbase × 0.6 × q/70 × trend)` copies and writes a `"royalties"` entry of `round(copies × 400 × index × 0.1)`; a doujin volume with fanbase 0 and quality 84 sells 240 on the next Monday and writes `"doujin sales"` of `round(240 × 500 × 0.6)` = 72,000 yen, then 96 the week after, and nothing after week 4; `GetOnline` in 1996 debits 120,000, sets `HasInternet`, and throws when repeated or with `Money < cost`; online, the doujin window is 8 weeks and word of mouth adds `Fanbase × 0.01 × reach`; the first Monday of the following month emits `ConventionRecap` with the month's copies and resets the counters; a volume forced past 100,000 copies emits `VolumeMilestone`, adds 3 track record and 0.05 influence; the daily recap after a fee shows `YenEarned` and `ChaptersPublished == 1`.
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run all tests.**
- [ ] **Step 5: Commit** `feat(sim): volume sales, doujin sales, ledger, internet and recap counters`.

---

### Task 11: Save validation and the two-year round trip

**Files:**
- Modify: `GameState.Serialization.cs`
- Test: `tests/MangakaSim.Tests/SaveV2Tests.cs`

All of spec section 11's `ValidateSave` additions. A scripted two-year game (create, pitch with a known accepting seed, accept, serialize, get online, second doujin series) round-trips byte-identically, continues identically after load, and every new invariant is exercised by a mutated `JsonNode` copy that throws `InvalidDataException`.

- [ ] **Step 1: Write the failing tests.**
- [ ] **Step 2: Run tests to verify they fail.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run all tests.**
- [ ] **Step 5: Commit** `feat(sim): save validation for publishing state`.

---

### Task 12: Debug scene, smoke test, docs

**Files:**
- Modify: `godot/DebugMain.cs`, `godot/DebugMain.SmokeTest.cs`, `README.md`, `docs/superpowers/specs/2026-09-22-roadmap.md`
- Create: `docs/superpowers/sub-project-2-completion.md`

Per spec section 12: series panel with magazine dropdown and Pitch / Accept / Decline / Withdraw / End buttons plus a status block; a Market column with the selected magazine's latest ranking (player rows marked), ledger tail with balance, price index and a Get Online button, and volumes; bottom status lines for studio reputation and trends. The smoke test runs a doujin chapter, pitches to `hoshigaku-flowers`, accepts under a seed that succeeds, runs until a chapter publishes and a volume sells, checks the ledger for a fee and a royalty entry, saves, reloads and validates, and captures one screenshot when `--capture` is passed.

- [ ] **Step 1: Extend the scene and the smoke test.**
- [ ] **Step 2: Build the solution with `-warnaserror` and run the full test suite.**
- [ ] **Step 3: Run the headless import and smoke test.**
- [ ] **Step 4: Update README, roadmap status and write the completion notes.**
- [ ] **Step 5: Commit** `feat(godot): publishing and market debug controls, smoke test and docs`.

---

## Self-review notes

- Spec coverage: section 1 (Tasks 1, 4), section 2 (Tasks 2, 4, 8), section 3 (Tasks 3, 8), section 4 (Task 7), section 5 (Tasks 5, 6), section 6 (Task 8), section 7 (Task 10), section 8 (Tasks 5, 10), section 9 (Task 9), section 10 (Tasks 1, 4), section 11 (Tasks 3, 4, 11), section 12 (Task 12), section 13 (every task's step 1).
- Every ambiguity found while planning is resolved under "Decisions locked in by this plan" rather than silently in code.
