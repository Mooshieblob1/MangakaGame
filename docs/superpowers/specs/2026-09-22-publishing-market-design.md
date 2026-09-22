# Sub-project 2: Publishing and Market — Design

Date: 2026-09-22
Status: implemented (plan `../plans/2026-09-22-publishing-market.md`, completion notes `../sub-project-2-completion.md`)
Roadmap: `2026-09-22-roadmap.md`
Builds on: `2026-09-22-sim-core-design.md` and the sub-project 1 code at commit `e9662d1`

## Goal

Give the finished chapters of sub-project 1 somewhere to go. A series starts as
a self-published doujin, pitches a one-shot to a magazine, gets an editor, is
serialized, is ranked every issue against a generated roster, earns chapter
fees and volume royalties, builds a fanbase and cultural impact, and can be
warned, cancelled, withdrawn, or ended. Genre trends follow real manga history
and bend to what the player publishes. Studio and personal reputation shape
every publisher decision as probabilities, never as hard gates.

At the end of this sub-project the owner can open the debug scene, run a
garage doujin studio, get online, pitch to any of six magazines, watch weekly
rankings and the editor's verdicts, sell tankobon, read the ledger, and lose or
keep a series under the cancellation rule. Everything is headless-testable and
deterministic for a given seed and command sequence.

## Non-goals (deferred)

- Costs, salaries, print runs, convention travel, provider fees, staff-run
  marketing (3). This sub-project has exactly one debit: getting online.
- Fatigue affecting quality (3, via the SkillFactor hook in section 5).
- Named rivals, scripted eras, poaching offers, audience aging by
  demographic (5). Hooks are left in fillers, trends, and the withdraw path.
- Anime, awards, merchandise feeding cultural impact (7).
- Real UI, charts, 3D office (4, 6).
- Product formats other than tankobon. The `Format` field exists for later.
- International interest. The stat stays absent; nothing here references it.

## Architecture

Approach chosen: partial classes on `GameState` plus pure rule modules.

- New partial files follow the sub-project 1 pattern and each adds one step
  to `Tick()`: `GameState.Publishing.cs` (pitch resolution, offers, editor
  gate), `GameState.Market.cs` (issue close, rankings, fillers, cancellation,
  trends), `GameState.Sales.cs` (volumes, doujin, ledger, online word of
  mouth), `GameState.Reputation.cs` (personal and studio reputation,
  protection, iconic transition).
- Every formula lives in a static, stateless class that takes plain inputs
  and returns numbers: `QualityRules`, `PitchRules`, `EditorRules`,
  `RankingRules`, `FanbaseRules`, `SalesRules`, `ReputationRules`,
  `CancellationRules`, `TrendRules`, `Economy` (price index). These are unit
  tested with exact expected values and never touch `GameState`.
- Static catalog data (publishers, magazines, genre list, trend keyframes,
  price index) is embedded JSON inside the `MangakaSim` assembly, loaded once
  into `PublisherCatalog` and `TrendCatalog`. Saves store only dynamic state
  and reference catalog entries by id.
- All randomness draws from the existing `GameState.Rng`, so a seed and a
  command log replay identically.
- `MangakaSim` still never references Godot.

### New files

| File | Responsibility |
|---|---|
| `src/MangakaSim/Catalog/PublisherCatalog.cs` | `Publisher`, `Magazine`, `Demographic`, load and validate `publishers.json` |
| `src/MangakaSim/Catalog/TrendCatalog.cs` | genre list, trend keyframes, internet reach curve, price index curve, load and validate `trends.json` |
| `src/MangakaSim/Data/publishers.json` | embedded resource |
| `src/MangakaSim/Data/trends.json` | embedded resource |
| `src/MangakaSim/Market.cs` | `MagazineState`, `FillerSeries`, `RankEntry`, `GenreTrend`, `LedgerEntry`, `Volume`, `VolumeFormat`, `Contract`, `SerializationOffer`, `PublishingStatus`, `EditorStatus` |
| `src/MangakaSim/Rules/*.cs` | one file per rule module listed above |
| `src/MangakaSim/GameState.Publishing.cs` | `EditorStep`, `PitchStep`, pitch and offer handling |
| `src/MangakaSim/GameState.Market.cs` | `IssueCloseStep`, rankings, fillers, cancellation rule, trend updates |
| `src/MangakaSim/GameState.Sales.cs` | `SalesStep`, volumes, doujin volumes, ledger |
| `src/MangakaSim/GameState.Reputation.cs` | reputation deltas, protection score, iconic transition |
| `tests/MangakaSim.Tests/Rules/*Tests.cs` | pure rule tests |
| `tests/MangakaSim.Tests/Publishing*Tests.cs`, `Market*Tests.cs`, `Sales*Tests.cs` | state tests |

Modified: `Model.cs`, `GameEvent.cs`, `EventType.cs`, `Settings.cs`,
`Commands.cs`, `GameState.cs`, `GameState.Planner.cs`, `GameState.Work.cs`,
`GameState.Commands.cs`, `GameState.Serialization.cs`, `GameState.Recap.cs`,
`godot/DebugMain.cs`, `godot/DebugMain.SmokeTest.cs`.

## Section 1: Data model additions

### Static catalog (never saved)

```
Publisher { string Id; string Name; }
Magazine {
  string Id; string PublisherId; string Name;
  int Tier;                       // 1 flagship .. 3 entry
  Cadence Cadence;                // existing enum
  Demographic Demographic;        // Shonen, Shojo, Seinen, Josei; descriptive only
  Dictionary<string,double> GenreAffinities;  // 0.75..1.25, default 1.0
  int RosterSize; int CancellationRank;
  int FeePerPageMin; int FeePerPageMax;       // 1996 yen
  DayOfWeek IssueCloseDay; int IssueCloseHour;
  int ChaptersPerVolume;
}
```

### GameState

```
long Money;                       // yen, nominal
List<LedgerEntry> Ledger;         // append-only
double StudioTrackRecord;         // 0..100
List<MagazineState> Markets;      // one per catalog magazine
List<GenreTrend> Trends;          // one per catalog genre
bool HasInternet;
```

```
LedgerEntry { DateTime Time; long Amount; string Reason; int? SeriesId; }
MagazineState {
  string MagazineId; DateTime NextIssueClose;
  List<FillerSeries> Fillers; List<RankEntry> LastRanking;
  int IssuesClosed;               // for "first close of the month" checks
}
FillerSeries { int Id; string Title; string Genre; double Popularity; bool IsIconic; int IssuesBelowLine; }
RankEntry { int Rank; string Title; int? SeriesId; int? FillerId; double Score; }
GenreTrend { string Genre; double Noise; double Boom; double BoomPeak; double BoomFloor; DateTime? BoomEndsAt; DateTime? BoomFadeEndsAt; double PlayerInfluence; }
```

### Series

```
PublishingStatus Publishing;      // Unpublished, Pitching, Offered, Serialized
Contract? Contract;               // MagazineId, FeePerPage, SignedAt
SerializationOffer? PendingOffer; // MagazineId, FeePerPage, FirstIssueClose, ExpiresAt
double Fanbase;                   // >= 0, unbounded
double CulturalImpact;            // 0..100, never falls
bool IsIconic;
List<DateTime> Strikes;
int WeeksBelowLine;
DateTime? WarningIssuedAt;
Dictionary<string,DateTime> PitchCooldowns;   // magazine id -> until
List<Volume> Volumes;
int ChaptersPublished;
int? LastRank;
```

`Series.Status` (Active, Paused, Ended) is untouched. `Publishing` is
orthogonal: an Ended series is always Unpublished.

### Chapter

```
int? Quality;                     // 0..100, set on completion
EditorStatus Editor;              // NotRequired, AwaitingReview, Approved, RedoRequested
DateTime? EditorDecisionAt;
int RedoCount;
bool IsOneShot;
string? PitchMagazineId;         // one-shots only
DateTime? PublishedAt;
int? Rank;
```

`Chapter.Stages` stays a list of exactly five `StageWork`. `StageWork` gains
`double OvertimeHours` and `double Contribution` (quality points, set when
the stage completes or is skipped).

### Person

`double Reputation` (0..100). `NewGame` starts Aki at 10.

### Volume

```
Volume {
  int Id; int Number; VolumeFormat Format;   // Tankobon only for now
  int FirstChapter; int LastChapter;         // chapter numbers, inclusive
  DateTime ReleaseDate; long CopiesSold; int WeeksOnSale; bool IsDoujin;
  double AverageQuality;                     // frozen at creation
}
```

## Section 2: Catalog data

### Publishers and magazines

Fictional analogues of the three big houses. One weekly flagship and one
slower magazine each. Fees are 1996 yen per page (see section 11 for the
price index).

| Magazine | Publisher | Tier | Cadence | Demographic | Roster | Line | Fee per page | Close | Ch/vol |
|---|---|---|---|---|---|---|---|---|---|
| Weekly Tokiwa Jump | Tokiwa Publishing | 1 | Weekly | Shonen | 20 | 15 | 9,000–20,000 | Thu 18:00 | 9 |
| Tokiwa Square | Tokiwa Publishing | 2 | Biweekly | Shonen | 16 | 12 | 8,000–14,000 | Fri 18:00 | 8 |
| Weekly Kaidan Magazine | Kaidan Press | 1 | Weekly | Shonen | 20 | 15 | 9,000–18,000 | Wed 18:00 | 9 |
| Monthly Kaidan Afternoon | Kaidan Press | 3 | Monthly | Seinen | 16 | 12 | 7,000–12,000 | Fri 18:00 | 6 |
| Weekly Hoshigaku Sunday | Hoshigaku Books | 2 | Weekly | Shonen | 18 | 14 | 8,000–15,000 | Tue 18:00 | 9 |
| Monthly Hoshigaku Flowers | Hoshigaku Books | 3 | Monthly | Shojo | 14 | 10 | 6,000–11,000 | Wed 18:00 | 6 |

Ids are lowercase slugs: `tokiwa-jump`, `tokiwa-square`, `kaidan-magazine`,
`kaidan-afternoon`, `hoshigaku-sunday`, `hoshigaku-flowers`.

### Genre affinities

Per magazine, 0.75 to 1.25, default 1.0 for unlisted genres. Genre is the
free-text `Series.Genre` matched case-insensitively after trimming; unknown
genres map to the `other` trend row. Shipped values:

| Magazine | Boosts (1.2) | Mild (1.1) | Cool (0.9) | Cold (0.8) |
|---|---|---|---|---|
| Tokiwa Jump | action, adventure | comedy, sports | mystery | slice of life, romance |
| Tokiwa Square | fantasy, action | mystery | sports | slice of life |
| Kaidan Magazine | sports, action | romance, comedy | horror | slice of life |
| Kaidan Afternoon | drama, slice of life | sci-fi, mystery | comedy | action |
| Hoshigaku Sunday | comedy, mystery | adventure, sports | horror | drama |
| Hoshigaku Flowers | romance, drama | slice of life, comedy | mystery | action, sports |

Affinity is soft. It scales how readers receive a series in that magazine
(pitch odds, ranking score) and never refuses a genre. A quality 85 shojo
chapter in Tokiwa Jump ranks like a native chapter at quality 68.

### Filler roster

At `NewGame` each magazine spawns `RosterSize - 1` fillers. Titles are
`Adjective Noun` drawn from two word lists of at least 40 entries each with
`Rng`. Each filler has a genre drawn from the catalog genre list weighted by
the magazine's affinities, and a hidden popularity: tier 1 draws in 40–95,
tier 2 in 35–85, tier 3 in 30–75. Each tier 1 magazine marks its highest
draw `IsIconic` and re-rolls it into 85–95.

Each issue every filler drifts by an integer step in −3..+3, clamped to
5..100. A non-iconic filler whose rank was below the cancellation line for
12 consecutive issues is retired and replaced by a fresh filler drawn in
45–65 with a new id and title. Iconic fillers never retire and have no
floor; they drift like anyone else from a high start.

The ranking for an issue has `fillers + player series in that magazine` rows.

### Issue schedule

`NextIssueClose` is initialised to the first `IssueCloseDay` at
`IssueCloseHour` at or after game start, then advances by 7, 14 or 28 days
per cadence. Monthly magazines close every 4 weeks; `CadenceRules.NextDue`
(which uses `AddMonths`) is only used for doujin schedules.

### Genre list

Shipped in `trends.json`: `action, adventure, comedy, sports, romance, drama,
slice of life, horror, mystery, fantasy, sci-fi, other`.

### Files and loading

`PublisherCatalog.LoadDefault()` and `TrendCatalog.LoadDefault()` parse the
embedded resources once and cache. `FromJson(string)` variants exist for
tests and later mods. Validation on load throws `InvalidDataException` for:
duplicate ids, unknown publisher id, `RosterSize <= CancellationRank`,
`FeePerPageMin >= FeePerPageMax`, affinity outside 0.75..1.25, tier outside
1..3, keyframes not strictly increasing by year, missing `other` genre, and
the spread rule in section 3.

## Section 3: Genre trends

Genre popularity is a per-genre multiplier that feeds pitch odds, ranking
score, and volume sales. It has three layers and one clamp:

```
Effective(genre, now) = clamp(Baseline + Noise + Boom + PlayerInfluence, 0.2, 1.8)
```

### Layer 1: Baseline keyframes (data)

Shipped in `trends.json`, linearly interpolated by fractional year, held flat
after the last keyframe. All values are placeholders for tuning; the shape
follows real manga history (slice of life rises through the 2000s, fantasy
booms in the 2010s, mystery and sci-fi fade).

| Genre | 1996 | 2000 | 2005 | 2010 | 2015 | 2020 |
|---|---|---|---|---|---|---|
| slice of life | 0.25 | 0.40 | 0.80 | 1.25 | 1.10 | 1.00 |
| action | 1.30 | 1.20 | 1.25 | 1.20 | 1.20 | 1.30 |
| adventure | 1.20 | 1.20 | 1.20 | 1.00 | 1.00 | 1.00 |
| fantasy | 0.80 | 0.90 | 0.90 | 1.10 | 1.40 | 1.50 |
| sports | 1.10 | 0.90 | 0.90 | 1.00 | 1.30 | 1.20 |
| mystery | 1.20 | 1.10 | 1.00 | 0.90 | 0.90 | 0.90 |
| horror | 0.70 | 0.70 | 0.70 | 0.70 | 0.70 | 0.70 |
| sci-fi | 1.15 | 1.00 | 0.70 | 0.70 | 0.70 | 0.70 |
| romance | 1.00 | 1.00 | 1.00 | 1.10 | 1.20 | 1.30 |
| comedy | 1.00 | 1.00 | 1.05 | 1.05 | 1.00 | 1.00 |
| drama | 0.90 | 0.95 | 1.00 | 1.00 | 1.00 | 1.05 |
| other | 0.90 | 0.90 | 0.90 | 0.90 | 0.90 | 0.90 |

**Spread rule** (enforced by a catalog test against the shipped data, so
tuning cannot silently flatten genres): for every keyframe year, at least two
genres are at or below 0.70, at least two are at or above 1.20, and the mean
across genres excluding `other` is within 0.90..1.10. The table above
satisfies it in every year (horror and sci-fi supply the low end after 2005;
booms give them their moments).

### Layer 2: Noise and booms (random, per genre)

Updated once per calendar month, on the first issue close of the month across
all magazines (the `IssueCloseStep` checks whether any magazine has already
closed in this month; see section 6).

- **Noise**: `Noise += Rng.NextDouble(-0.02, 0.02) - 0.1 × Noise`, then clamp
  to −0.15..0.15. Mean-reverting random walk.
- **Boom**: if no boom is active for the genre, a 1% roll starts one:
  `BoomPeak = Rng.NextDouble(0.3, 0.5)`, `BoomEndsAt = now + Rng.NextInt(1, 3) years`,
  `BoomFadeEndsAt = BoomEndsAt + 1 year`, `Boom = BoomPeak`. Emit
  `GenreTrendShifted` ("Fantasy is having a moment"). While `now < BoomEndsAt`
  the boom holds at peak. During fade, `Boom` decays linearly from `BoomPeak`
  toward `BoomFloor`, which is chosen by one roll at fade start: 25% chance
  `BoomFloor = BoomPeak / 2` (the genre keeps half its gain permanently),
  otherwise `BoomFloor = 0`. At `BoomFadeEndsAt`, `Boom = BoomFloor` and the
  boom fields are cleared, so a later boom can start and add on top (within
  the 1.8 clamp). Emit `GenreTrendShifted` at fade start ("the fantasy boom
  is cooling"). `GenreTrend` therefore also carries `double BoomFloor`.
- Genre `other` never booms and has no noise.

### Layer 3: Player influence (0..0.5, per genre)

The player can move a genre by publishing excellent work in it:

| Trigger | Delta |
|---|---|
| Player series in the genre finishes an issue in the top 3 with chapter quality ≥ 80 | +0.01 |
| Player volume in the genre passes 100,000 copies | +0.05 |
| Player volume in the genre passes 1,000,000 copies | +0.15 (once per series) |
| Monthly update while no player series in the genre is Serialized | −0.005 |

Clamp 0..0.5. Each time `PlayerInfluence` crosses a multiple of 0.1 upward,
emit `GenreTrendShifted` crediting the studio ("Slice of life is catching on,
and critics point at Studio Aki"). Iconic series (section 4) do not
contribute to influence.

### Crowding penalty

In ranking score only: each other series (filler or player) in the same
magazine with the same genre beyond the second costs 3%, capped at 15%.
Three same-genre rivals → ×0.97; seven or more → ×0.85. This keeps a magazine
from turning into one genre without forbidding it.

### `TrendRules` API

```
static double Baseline(TrendCatalog c, string genre, DateTime now);
static double Effective(GenreTrend t, TrendCatalog c, DateTime now);
static double Crowding(int sameGenreOthers);      // 1.0, 1.0, 0.97, 0.94 ... floor 0.85
static string Normalise(string genre, TrendCatalog c);  // trim, lower, unknown -> "other"
```

## Section 4: Pitching and offers

### Command `PitchSeries(int SeriesId, string MagazineId)`

Valid when the series exists, `Publishing == Unpublished`, `Status == Active`,
the magazine id exists, `PitchCooldowns[magazine]` is absent or in the past,
and the series has no chapter with any stage in progress or pending
(the current open chapter must be complete). Otherwise
`InvalidCommandException` with a specific reason.

Effect: create a chapter via `CreateNextChapter` with `IsOneShot = true`,
`PagesPerChapter` overridden to 31 for its hours, `Editor = NotRequired` until
Name completes (the gate in section 5 applies because `IsOneShot` is set),
and `DueDate` = the magazine's first issue close at least 14
days from now. Set `Publishing = Pitching`, store the target magazine on the
chapter as `PitchMagazineId`. Emit `PitchSubmitted`.

Chapter therefore gains `string? PitchMagazineId` (only on one-shots).

### Resolution (`PitchStep`)

When a one-shot chapter is Complete and `Editor == Approved`, and the current
time is at or past its `DueDate`, resolve at the magazine's next issue close
(a late one-shot waits for the following close, no strike, no penalty). At
resolution:

```
base   = tier 1: 0.15, tier 2: 0.30, tier 3: 0.50
qf     = lerp(quality, 50 -> 0.5, 100 -> 1.6)   // linear, unclamped below 50
rf     = lerp(effectiveRep, 0 -> 0.6, 100 -> 1.6)
chance = clamp(base × qf × rf × affinity × genreTrend, 0.02, 0.95)
```

One `Rng.NextDouble()` draw. `PitchRules.Chance(...)` is pure and tested;
`PitchRules.WeakestFactor(...)` names the smallest of `qf, rf, affinity,
genreTrend` for the rejection message.

Success: `Publishing = Offered`, `PendingOffer = { MagazineId, FeePerPage,
FirstIssueClose = 4th issue close from now, ExpiresAt = FirstIssueClose }`.
`FeePerPage = round(lerp(effectiveRep, 0 -> FeeMin, 100 -> FeeMax) × PriceIndex(now))`
rounded to the nearest 100 yen. Emit `SerializationOffered`.

Failure: `Publishing = Unpublished`, `PitchCooldowns[magazine] = now + 26 weeks`,
emit `PitchRejected` with the weakest-factor reason. If quality ≥ 70,
`StudioTrackRecord += 0.25`. The one-shot stays a finished chapter and counts
toward the next doujin volume.

### Commands `AcceptOffer(int SeriesId)` and `DeclineOffer(int SeriesId)`

Valid only when `Publishing == Offered`. Accept: `Contract = { MagazineId,
FeePerPage, SignedAt = now }`, `PendingOffer = null`, `Publishing = Serialized`,
`Series.Cadence = magazine.Cadence`, `Strikes`, `WeeksBelowLine`,
`WarningIssuedAt`, `LastRank` reset, `ChaptersPublished` kept (it is a
lifetime count). If the series has no open chapter, `CreateNextChapter` runs
with `DueDate = FirstIssueClose`; if one is open, its due date moves to
`FirstIssueClose` and `Editor` is set per section 5 based on stage progress.
Emit `OfferAccepted`. Decline: `Publishing = Unpublished`, offer cleared, no
cooldown, emit `OfferDeclined`.

### Offer expiry (`PitchStep`)

When `now >= PendingOffer.ExpiresAt`: clear offer, `Publishing = Unpublished`,
no cooldown, emit `OfferExpired`.

### Iconic series

A series becomes Iconic when `CulturalImpact >= 90` and `Fanbase >= 1,000,000`,
checked whenever either value changes. Emit `SeriesBecameIconic` once; the
flag never clears. Rules for an Iconic series:

- Fanbase never decays: no issue churn, no hiatus decay, no withdraw penalty.
- Cultural impact never falls (true for everyone) and stays at or above 90.
- Cancellation is off: no strikes recorded, no warning clock, no roll.
- No rank floor. It competes on score like any row, but its rank feeds
  nothing: the fanbase gain uses a fixed `RankFactor = 1.5`, and top-3
  results do not add track record, personal reputation, or player influence.
- Affinity and genre trend are fixed at 1.0 for its own ranking score, pitch
  odds and volume sales. It does not add to `PlayerInfluence`.
- Fees, royalties, volumes and the editor gate work normally.
- A withdrawn Iconic series keeps its fanbase; its pitch chance uses the
  normal formula, which at 1M fanbase and 90+ impact will be near the 0.95 cap
  in any magazine.

## Section 5: Editor gate and chapter quality

### Who gets an editor

Only chapters of a Serialized series and pitched one-shots. Every other
chapter (doujin) has `Editor = NotRequired` for its whole life. When a series
becomes Serialized with a chapter already open, the open chapter's `Editor`
is set from its Name stage: Name not complete → `NotRequired` until Name
completes (then it enters review); Name complete and Pencils not started →
`AwaitingReview` now; Pencils already started → `Approved`.

### Flow

1. Name stage completes (or is skipped, which submits with Name quality 0).
   `Editor = AwaitingReview`, `EditorDecisionAt = now + 24h` (tier 3), `36h`
   (tier 2), `48h` (tier 1). The one-shot's magazine is `PitchMagazineId`.
2. `IsStartable` returns false for the Pencils stage while
   `Editor == AwaitingReview`; the planner skips it and the person picks other
   work. Inks, Backgrounds and Tones still require Pencils first under the
   existing dependency rule, so the whole chapter waits.
3. `EditorStep` runs each tick: for every chapter with `Editor == AwaitingReview`
   and `now >= EditorDecisionAt`, roll `EditorRules.Approve`.
4. Approved → `Editor = Approved`, emit `EditorApproved`. Pencils becomes
   startable.
5. Redo → `Editor = RedoRequested`, `RedoCount++`, the Name `StageWork` is
   reset to `HoursDone = 0`, `Status = Pending`, `Contribution = 0`,
   `OvertimeHours = 0`, keeping its assignee. Emit `EditorRedoRequested` with
   the Name quality in the message. When Name completes again it re-enters
   step 1. The Name assignee loses 0.5 reputation, the studio 0.25 track
   record.
6. The third submission (`RedoCount == 2`) is always approved.

Review time counts against the issue close: a chapter still under review at
close is not published and misses the issue.

### `EditorRules`

```
threshold = lerp(effectiveRep, 0 -> T0[tier], 100 -> T100[tier])
  T0   = { tier1: 65, tier2: 55, tier3: 45 }
  T100 = { tier1: 50, tier2: 40, tier3: 30 }
p(approve) = clamp(0.5 + 0.0225 × (nameQuality - threshold), 0.05, 0.95)
```

So exactly at threshold 50%, at +20 → 95%, at −20 → 5%. `nameQuality` is the
Name stage's contribution scaled to 0..100 (`Contribution / 0.35`). One
`Rng.NextDouble()` draw.

### Chapter quality

Deterministic. Computed when the chapter completes, from per-stage
contributions recorded at stage completion:

```
Contribution(stage) = Weight × 100 × SkillFactor × RushFactor × RedoBonus
Weights: Name 0.35, Pencils 0.30, Inks 0.15, Backgrounds 0.10, Tones 0.10
SkillFactor = lerp(skill, 0 -> 0.2, 50 -> 0.6, 100 -> 1.0)      // piecewise linear
RushFactor  = max(0.7, 1 - 0.5 × OvertimeHours / HoursRequired)  // -5% per 10% overtime
RedoBonus   = Name only: SkillFactor += 0.05 × RedoCount, cap 1.0 (applied inside SkillFactor)
Skipped stage: Contribution = 0
Quality = clamp(round(Σ Contribution), 0, 100)
```

Skill is the assignee's skill for that stage at completion time. Aki's
default skill of 80 gives `SkillFactor 0.84` and a clean solo chapter of
quality 84. A stage worked by several people over its life uses the skill of
the person who logged the final hour; sub-project 3 may refine this.

Overtime hours are tracked per `StageWork` by `WorkStep`: any hour worked
outside the person's regular schedule window adds 1 to `OvertimeHours`.

`CompleteChapterIfDone` sets `Chapter.Quality`, includes it in the
`ChapterCompleted` message ("Chapter 3 finished, quality 84"), applies the
personal reputation rule (section 9), and checks whether a doujin volume is
due (section 8).

## Section 6: Issue close, rankings, fanbase

### Serialized due dates

`CreateNextChapter` for a Serialized series sets `DueDate` to the magazine's
first issue close strictly after the previous chapter's `DueDate`. Doujin
series keep the sub-project 1 cadence rule and `DeadlineMissed` behaviour.
`DeadlineMissed` does not fire for Serialized chapters; `IssueMissed` covers
them.

### `IssueCloseStep`

For each magazine in catalog order whose `NextIssueClose <= now`:

1. **Publish or miss.** For each Serialized series in this magazine with a
   chapter whose `DueDate == NextIssueClose`: if the chapter is Complete and
   `Editor == Approved`, publish it: `PublishedAt = now`,
   `ChaptersPublished++`, add `Pages × Contract.FeePerPage` to the ledger
   with reason `"chapter fee"`, emit `ChapterPublished`. Otherwise emit
   `IssueMissed`, add a strike (unless Iconic or within grace, section 9),
   move `DueDate` to the next close, `Fanbase ×= 0.97` (not Iconic). A
   missed series is absent from this issue's table.
2. **Score.** Player rows that published this issue:
   `Score = (0.6 × Quality + 0.4 × FanScore) × Affinity × GenreTrend × Crowding`,
   `FanScore = 100 × Fanbase / (Fanbase + K)`, `K = 200,000 / 100,000 / 50,000`
   by tier. Iconic uses Affinity and GenreTrend 1.0. Fillers:
   `Score = Popularity × Crowding`. Sort descending, ties broken by lower
   filler id then lower series id.
3. **Record.** `LastRanking`, `Chapter.Rank`, `Series.LastRank`. Emit
   `RankingPublished` once per magazine with the player's best rank in the
   message.
4. **Fanbase.** Per published player row, not Iconic:
   `Fanbase ×= 0.995` then `Fanbase += BaseReaders × RankFactor × Quality / 70`.
   `BaseReaders = 3,000 / 1,500 / 800` by tier.
   `RankFactor = lerp(rank, 1 -> 3.0, line -> 0.5, roster -> 0.2)` piecewise.
   Iconic: no churn, `RankFactor = 1.5`.
5. **Cultural impact.** `+0.05` per published chapter, `+0.1` per top-3 rank.
   Cap 100.
6. **Reputation and cancellation** (section 9).
7. **Fillers.** Drift and retirement (section 2).
8. **Advance.** `NextIssueClose += cadence`, `IssuesClosed++`.
9. **Monthly trend update.** If this is the first issue close in this
   calendar month across all magazines (tracked by `GameState.LastTrendUpdateMonth`,
   a `DateTime` at month precision), run the section 3 layer 2 and layer 3
   monthly updates once.

Only one issue close per magazine can happen per tick because ticks are one
hour and issues are at least a week apart.

## Section 7: Volumes, sales, ledger

### Tankobon scheduling

After each `ChapterPublished`, if the series has at least
`Magazine.ChaptersPerVolume` published chapters not yet in a volume, create a
`Volume { Format = Tankobon, FirstChapter, LastChapter, ReleaseDate =
lastChapterIssueClose + 6 weeks, AverageQuality = mean of its chapters' Quality }`
and emit `VolumeScheduled`. When a series ends or is cancelled (section 9), a
final volume is scheduled 6 weeks later if at least 3 published chapters are
left over.

`SalesStep` emits `VolumeReleased` when `now >= ReleaseDate` for the first
time (the volume's `WeeksOnSale` is 0 until then).

### Weekly sales (`SalesStep`, Mondays 00:00)

For each released volume with `WeeksOnSale < 52`:

```
week = WeeksOnSale + 1
qf   = AverageQuality / 70
copies = week == 1 ? Fanbase × 0.6 × qf × GenreTrend
                   : Fanbase × 0.04 × qf × 0.93^(week - 2)
```

`Fanbase` is read live from the series (so a growing series lifts its back
catalogue). Iconic uses GenreTrend 1.0. `copies` is rounded down to a long.
`CopiesSold += copies`, `WeeksOnSale++`. After week 52 the volume is frozen.

Royalties: cover price `400 × PriceIndex(ReleaseDate)` yen, royalty 10%;
`Amount = copies × cover × 0.10` rounded to whole yen, one ledger entry per
volume per Monday with reason `"royalties"`. Fanbase gains `5%` of copies
sold this week. `VolumeMilestone` fires when `CopiesSold` first passes
100,000 and 1,000,000 (with the section 3 and section 9 side effects).

Doujin volumes follow section 8 instead.

### Ledger

`Money` starts at `500,000`. Every change is a `LedgerEntry`; `Money` is the
running sum and `ValidateSave` recomputes it. Reasons used in this
sub-project: `"chapter fee"`, `"royalties"`, `"doujin sales"`, `"internet"`
(negative). Amounts are nominal yen at the time of the entry.

## Section 8: Doujin path and going online

Every Unpublished series is a doujin series. Its chapters have
`Editor = NotRequired`, no strikes, no cancellation, no fees.

### Doujin volumes

In `CompleteChapterIfDone`: if an Unpublished series has 5 or more finished
chapters not in any volume (one-shots count), create a `Volume { IsDoujin =
true, ReleaseDate = now, WeeksOnSale = 0 }` and emit `VolumeReleased`
immediately (no `VolumeScheduled`).

### Doujin sales (Mondays 00:00)

Window 4 weeks, or 8 weeks when `HasInternet`:

```
qf      = AverageQuality / 70
reach   = HasInternet ? InternetReach(now) : 0
week1   = (200 + Fanbase × 0.3) × qf × GenreTrend × (1 + 0.5 × reach)
weekN   = week1 × 0.4          // weeks 2..window, recomputed from live fanbase
```

Cover `500 × PriceIndex(ReleaseDate)` yen, studio keeps 60%; ledger reason
`"doujin sales"`. Fanbase gains `30%` of copies sold. Cultural impact is not
affected. Track record `+0.5` per doujin volume whose `AverageQuality >= 75`
(once, at release).

### Convention recap

On the first Monday of each calendar month, emit `ConventionRecap` summing
doujin copies sold and fanbase gained over the previous month (tracked in
`GameState.DoujinCopiesThisMonth`, `DoujinFansThisMonth`, reset after the
recap). Skipped when nothing was sold.

### Internet

Command `GetOnline()`: valid once, when `!HasInternet` and
`Money >= cost`, `cost = round(120,000 × PriceIndex(now))`. Adds a negative
ledger entry with reason `"internet"`, sets `HasInternet = true`, emits
`WentOnline`.

`InternetReach(now)` is a keyframe curve in `trends.json`:
1996 0.1, 1998 0.2, 2001 0.5, 2005 1.0, 2010 1.6, 2015 2.0, flat after.

Online effects: doujin week-one multiplier and 8-week window above, plus
word of mouth every Monday: each Unpublished series with at least one
released volume gains `Fanbase × 0.01 × InternetReach(now)` fans. Sub-project 3
hangs provider fees and staff marketing on `HasInternet`.

## Section 9: Reputation, protection, cancellation, leaving

### Personal reputation (0..100, no decay)

On chapter completion each contributor gets
`(Quality - 60) / 20 × HourShare`, where `HourShare` is their hours over the
chapter's total hours; doujin chapters at half weight. Top-3 issue: each
contributor `+0.5 × HourShare`. Redo: Name assignee `-0.5`. Cancellation:
every contributor to the series `-2`. Proper ending: `bonus × HourShare`
over the series' lifetime hours.

### Studio track record (0..100)

| Event | Delta |
|---|---|
| Rank 1 | +0.5 |
| Rank 2–3 | +0.3 |
| Volume passes 100k / 1M | +3 / +10 |
| Proper ending | +1..+5 (see below) |
| Doujin volume, avg quality ≥ 75 | +0.5 |
| Rejected pitch with quality ≥ 70 | +0.25 |
| Missed issue | −1 |
| Editor redo | −0.25 |
| Cancellation | −8 |
| Withdraw or early end | −3 − 0.05 × ChaptersPublished, floor −10 |

### Effective reputation

```
staffTerm = weighted mean of the top 3 people by Reputation, weights 0.5/0.3/0.2 renormalised to those present
Effective = 0.5 × StudioTrackRecord + 0.5 × staffTerm
```

Used for pitch odds, fee offers, editor thresholds, cancellation survival.

### Protection

```
P = clamp(0.4 × min(ChaptersPublished, 300) / 300
        + 0.35 × Fanbase / (Fanbase + 500,000)
        + 0.25 × CulturalImpact / 100, 0, 1)
```

The cancellation line is fixed per magazine. Protection only lengthens the
clocks:

| Clock | Weeks |
|---|---|
| Consecutive issues below line before warning | `3 + 9P` |
| Issues under warning before cancellation roll | `3 + 23P` |
| Strike lifetime | `8 − 6P` |

"Weeks" are issues for weekly magazines; for biweekly and monthly the same
issue counts apply (the clocks are in issues, the label is descriptive).
The first 8 published chapters of a contract are a grace period: no strikes
and no below-line counting.

### Cancellation rule (runs at issue close, per Serialized non-Iconic series)

- Rank at or above the line: `WeeksBelowLine = 0`; if a warning was active,
  clear it and emit `CancellationWarningLifted`.
- Rank below the line: `WeeksBelowLine++`. When it reaches the warning clock
  and no warning is active: `WarningIssuedAt = now`, emit
  `CancellationWarning`.
- Strikes older than their lifetime are dropped at every close.
- Roll when either issues since `WarningIssuedAt` reach the cancel clock, or
  3 live strikes exist: `cancelChance = 1 − 0.4 × Effective / 100`, one draw.
  Survive → `CancellationSurvived`, `WeeksBelowLine` and strike count halved
  (oldest strikes dropped), warning kept. Cancelled → below.

### Cancellation

`SeriesCancelled`; `Status = Ended`, `Publishing = Unpublished`, contract
cleared, open chapter dropped, final volume if ≥ 3 leftover chapters,
`PitchCooldowns[magazine] = now + 52 weeks`, contributors −2, track record −8.

### Command `WithdrawSeries(int SeriesId)`

Valid when Serialized. `Publishing = Unpublished`, contract cleared,
`Fanbase ×= 0.9` (not Iconic), strikes and warning cleared, the open chapter
stays and becomes doujin (`Editor = NotRequired`, due date recomputed by the
doujin cadence rule), `PitchCooldowns[magazine] = now + 52 weeks`, track
record penalty per table. Emit `SeriesWithdrawn`.

### Command `EndSeries(int SeriesId)`

Always valid for a series with `Status != Ended`. `Status = Ended`; if it was
Serialized, contract cleared and `Publishing = Unpublished`. Open chapter
dropped. With `ChaptersPublished >= 12` it is a proper ending: track record
`+1 + 4 × clamp((lineRank − avgRank) / lineRank, 0, 1) × min(1, totalCopies / 500,000)`
rounded to one decimal, contributors share it by lifetime hours. Below 12 the
withdraw penalty applies. Doujin series end free. Final volume if ≥ 3
leftover published chapters. Emit `SeriesEnded`. Pending offers and pitch
one-shots are dropped.

`PauseSeries` on a Serialized series stays legal and simply lets issues be
missed (hiatus).

## Section 10: Events and tick order

### New `EventType` values

Appended after the existing eleven, in this order:

```
PitchSubmitted, PitchRejected, SerializationOffered, OfferAccepted,
OfferDeclined, OfferExpired, EditorApproved, EditorRedoRequested,
ChapterPublished, IssueMissed, RankingPublished, CancellationWarning,
CancellationWarningLifted, CancellationSurvived, SeriesCancelled,
SeriesWithdrawn, SeriesEnded, SeriesBecameIconic, VolumeScheduled,
VolumeReleased, VolumeMilestone, ConventionRecap, GenreTrendShifted,
WentOnline
```

`GameEvent` gains nullable `MagazineId`, `VolumeId`, `Rank`, `Amount`.
`Emit` gets an overload taking an optional `EventContext` record with those
fields. `DailyRecapPayload` gains `YenEarned`, `ChaptersPublished`,
`IssuesMissed`.

### Auto-pause defaults

`Settings.Default()` adds these keys set to true: `SerializationOffered`,
`PitchRejected`, `EditorRedoRequested`, `CancellationWarning`,
`SeriesCancelled`, `VolumeMilestone`, `ConventionRecap`,
`SeriesBecameIconic`. Missing keys read as off (already the case).

### Tick order

```
Clock.Advance
WorkStep              (existing; now records OvertimeHours and Contribution)
EditorStep            (resolve reviews due)
IssueCloseStep        (magazines in catalog order; monthly trend update)
SalesStep             (Mondays 00:00: volume sales, doujin sales, royalties,
                       online word of mouth, convention recap on first Monday)
PitchStep             (resolve one-shots at their magazine's close; expire offers)
RiskStep              (existing)
DayEndStep            (existing)
midnight rollover     (existing)
```

`PitchStep` runs after `IssueCloseStep` so a one-shot due at this close is
resolved in the same tick the issue closes.

## Section 11: Commands, save format, validation, prices

### Commands

| Command | Valid when | Effect summary |
|---|---|---|
| `PitchSeries(SeriesId, MagazineId)` | Unpublished, Active, no cooldown, no open work | one-shot created, Pitching |
| `AcceptOffer(SeriesId)` | Offered | contract, Serialized |
| `DeclineOffer(SeriesId)` | Offered | Unpublished, no cooldown |
| `WithdrawSeries(SeriesId)` | Serialized | Unpublished, penalties, cooldown |
| `EndSeries(SeriesId)` | not Ended | Ended, bonus or penalty |
| `GetOnline()` | no internet, affordable | debit, `HasInternet` |

All validate before mutating and throw `InvalidCommandException` with a
reason string. They are added to the `ICommand` polymorphic set and the
switch in `Apply`, and each emits `CommandApplied` like existing commands.

### Save format

`GameState.CurrentVersion = 2`. Version 1 saves are rejected with the
existing version error. New required top-level fields: `Money`, `Ledger`,
`StudioTrackRecord`, `Markets`, `Trends`, `HasInternet`,
`LastTrendUpdateMonth`, `DoujinCopiesThisMonth`, `DoujinFansThisMonth`. The
catalog is never saved; ids are validated against the loaded catalog.

### `ValidateSave` additions

- `Money == 500,000 + Σ Ledger.Amount`.
- Exactly one `MagazineState` per catalog magazine, `NextIssueClose` on the
  hour and `>= Clock.Now`, filler ids unique per magazine, every `RankEntry`
  points at an existing filler or series.
- Per series: `Serialized ⇒ Contract != null && PendingOffer == null`;
  `Offered ⇒ PendingOffer != null && Contract == null`; `Pitching ⇒ exactly
  one unfinished one-shot`; `Unpublished ⇒ Contract == null && PendingOffer == null`;
  `Status == Ended ⇒ Unpublished`. Contract and cooldown magazine ids exist.
- Per chapter: `Editor == NotRequired` unless the series is Serialized or the
  chapter is a one-shot; `AwaitingReview ⇒ Name complete && Pencils pending`;
  `Quality` non-null iff Complete.
- Volumes: ascending `Number`, chapter ranges non-overlapping,
  `CopiesSold >= 0`, `WeeksOnSale <= 52` (or the doujin window).
- Trends: one row per catalog genre, `|Noise| <= 0.15`, `0 <= Boom <= 1.0`,
  `0 <= PlayerInfluence <= 0.5`.
- Reputations, track record and cultural impact in 0..100; `Fanbase >= 0`;
  strikes not in the future; `IsIconic ⇒ CulturalImpact >= 90`.
- Existing checks stay: one person, five stages per chapter.

### Price index (inflation)

Real Japanese CPI normalised to 1996 = 1.00, shipped in `trends.json` and
linearly interpolated:

| Year | 1996 | 1998 | 2000 | 2005 | 2010 | 2013 | 2015 | 2020 | 2022 | 2023 | 2024 | 2025 | 2026 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Index | 1.00 | 1.02 | 1.01 | 0.99 | 0.99 | 0.99 | 1.02 | 1.04 | 1.06 | 1.09 | 1.12 | 1.16 | 1.18 |

After 2026 the index grows 2% per year. All catalog yen amounts are 1996
prices multiplied by `Economy.PriceIndex(TrendCatalog, DateTime)` at the
moment of use: fee ranges at offer time (then fixed in the contract), cover
prices at release, the internet purchase at command time. Ledger amounts are
nominal. The debug scene displays the current index.

## Section 12: Debug scene

`godot/DebugMain.cs` grows:

- Series panel: magazine dropdown, buttons Pitch / Accept / Decline /
  Withdraw / End, and a status block showing publishing state, contract
  magazine and fee, editor state of the open chapter, last quality, last rank
  with the magazine line, fanbase, cultural impact, live strikes, warning.
- New right-hand "Market" column: latest ranking for the dropdown's magazine
  with player rows highlighted; ledger tail with balance, price index and a
  Get Online button; volumes with copies sold and weeks on sale.
- Bottom status: "Studio" line (track record, staff term, effective) and a
  trends line per genre (effective multiplier and player influence).

Smoke test (`DebugMain.SmokeTest.cs`, headless): new game, run until the
first chapter completes (doujin), pitch to `hoshigaku-flowers`, force the
pitch roll via a seed known to succeed, accept, run until a chapter publishes
and a volume sells at least one copy, assert the ledger has a fee and a
royalty entry, save and reload, validate. One rendered screenshot as in
sub-project 1.

## Section 13: Testing

### Pure rule tests (exact numbers)

- `QualityRules`: Aki solo clean chapter = 84; skipped Tones = 74; 20%
  overtime on Pencils removes 10% of that stage; redo bonus caps at 1.0.
- `PitchRules`: tier 1, quality 84, rep 25, affinity 1.2, trend 1.3 →
  chance computed and clamped; weakest factor picks the right name.
- `EditorRules`: threshold at tier 1 rep 0 is 65; probability 0.95 at +20.
- `ReputationRules`: fee for rep 50 at Tokiwa Jump in 1996 = 14,500;
  protection at 300 chapters, 500k fans, impact 50 = 0.4 + 0.175 + 0.125.
- `CancellationRules`: clocks at P = 0 and P = 1; strike expiry.
- `FanbaseRules`: rank factor at rank 1, line, bottom; K by tier.
- `SalesRules`: week 1 and week 5 copies for fanbase 10,000, quality 84,
  trend 1.0; doujin with and without internet.
- `TrendRules`: interpolation between keyframes; hold after 2020; crowding
  table; normalise unknown genre to `other`.
- `Economy`: price index at 1996-04-01 = 1.00, at 2030 = 1.18 × 1.02^4.
- Catalog tests: `publishers.json` loads, six magazines, affinities in
  range, roster > line; `trends.json` spread rule for every keyframe year.

### State tests (`NewGame(seed)` + `Advance` + commands)

- Editor gate: after Name completes on a Serialized chapter, Pencils is not
  startable until `EditorDecisionAt`; approval unblocks it.
- Missed issue: chapter not ready at close → `IssueMissed`, one strike, due
  date moves to next close, fanbase ×0.97.
- Three strikes → cancellation roll fires exactly once; seeded outcome
  asserted both ways by choosing seeds.
- Filler churn: after 12 issues a bottom filler with forced low popularity is
  replaced; iconic filler never is.
- Doujin volume created on fifth finished chapter; sold on the next Monday;
  ledger entry with reason `"doujin sales"`.
- Pitch flow: `PitchSeries` creates a 31-page one-shot; rejection sets a
  26-week cooldown and a second pitch throws; success leads to
  `SerializationOffered`; expiry after four issues.
- Iconic transition fires once when impact and fanbase cross; no strike is
  added on a later miss.
- `GetOnline` debits `120,000` in 1996, throws when repeated or unaffordable.
- Monthly trend update runs once even with two magazines closing in the same
  month; `GenreTrendShifted` emitted on a forced boom.
- Save round trip after 2 in-game years of scripted play: serialise,
  deserialise, `ValidateSave` passes, and every new invariant is exercised by
  a mutated copy that fails.

### Godot

Headless smoke run and one rendered screenshot, as in sub-project 1.

## Hooks into sub-project 1 code

| Existing member | Change |
|---|---|
| `GameState.Tick()` | insert `EditorStep`, `IssueCloseStep`, `SalesStep`, `PitchStep` in the order above |
| `IsStartable(QueueRef)` | return false for Pencils while `Editor == AwaitingReview` |
| `CompleteChapterIfDone(Chapter)` | compute quality, personal reputation, doujin volume check |
| `CreateNextChapter(Series)` | issue-close due dates for Serialized; one-shot page override |
| `WorkStep` | record `OvertimeHours`; set `Contribution` on stage completion or skip |
| `Apply(ICommand)` | six new cases |
| `ValidateSave` | section 11 additions |
| `Settings.Default()` | eight new auto-pause keys |
| `NewGame(seed)` | catalog load, `Markets`, `Trends`, `Money`, Aki reputation 10 |
| `DailyRecapPayload` | three new counters |
