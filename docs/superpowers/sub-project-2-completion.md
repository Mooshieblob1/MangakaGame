# Sub-project 2 completion

Completed 22 September 2026. Sub-project 3 has not been started.

## Delivered

- Six fictional magazines across three publishers, shipped as embedded JSON
  with genre affinities, fee ranges, cancellation lines, close days and
  chapters per volume; generated filler rosters with hidden popularity, one
  iconic star per flagship, drift, and retirement after twelve issues below
  the line.
- Genre trends with historical baseline keyframes (spread rule enforced by a
  catalog test), monthly mean-reverting noise, random booms with a fade and an
  optional permanent floor, and player influence earned by top-3 finishes and
  volume milestones.
- Pitching: an untouched open chapter becomes a 31-page one-shot, goes through
  the editor, and is judged at the magazine's close with a tested chance
  formula; offers four issues out, accept and decline, expiry, 26-week
  cooldowns on rejection.
- The editor gate on the Name stage for serialized chapters and one-shots, with
  tier-based review times, redo requests that reset the Name with a skill
  bonus, and a guaranteed third approval.
- Chapter quality from per-stage contributions (skill, overtime rush, redo
  bonus, skipped stages), reported on completion.
- Issue closes per magazine: publish or miss, scoring with affinity, trend and
  crowding, rankings with the player's row, fanbase growth by rank, cultural
  impact, strikes, warnings, the cancellation roll, and the iconic transition.
- Tankobon scheduling and weekly sales with royalties, doujin volumes sold
  over a four-week window (eight online), milestones, the convention recap,
  going online, and word of mouth. A ledger with the running balance and a
  price index that inflates every catalog yen amount.
- Personal and studio reputation, the effective reputation used by every
  publisher decision, protection that lengthens the cancellation clocks,
  withdraw and end commands with penalties and proper-ending bonuses.
- Save format version 2 with catalog-free saves, full invariant validation,
  and a two-year scripted round trip.
- Debug scene: publishing controls, a status block, quality and rank columns,
  a market column (ranking, ledger tail with balance and price index, Get
  Online, volumes), and studio and trend status lines. The smoke test runs the
  whole doujin-to-serialization-to-royalty loop against the real controls.

## Verification

Verified in a Linux container (Ubuntu 24.04) with the .NET SDK 8.0.131 from
the distribution package and Godot 4.7.2.stable.mono.official.ed1daf0bf
downloaded from the official GitHub release.

- `dotnet test tests/MangakaSim.Tests`: **283 passed**, none failed or skipped
  (148 from sub-project 1, 135 new).
- `dotnet build MangakaGame.sln -warnaserror`: **0 warnings, 0 errors**.
- Godot headless editor import: successful.
- Godot scene walkthrough: **51 checks passed** headless; **54 passed** when
  rendered under Xvfb with software OpenGL (`--rendering-driver opengl3`),
  including three screenshot captures.
- The three screenshots (`debug-recap`, `debug-main`, `debug-market`) were
  inspected: the market screenshot shows a serialized series ranked in Monthly
  Hoshigaku Flowers, seven chapter-fee entries and a royalty entry in the
  ledger, the internet purchase, the first tankobon on sale, and the trend line
  with two booms in progress. A first rendering pass showed the log column
  pushed off the 1600 px window by the publishing button row's minimum width;
  the row now wraps and the screenshots were retaken.
- Two-year scripted game (get online, pitch, accept, serialize, second doujin
  series): identical JSON after a round trip, identical continuation after
  loading, identical replay from the command log.

No rendering on the owner's Windows machine or GPU was performed in this
session; the README documents both the Windows commands and the Linux virtual
display command.

## Corrections to the spec and plan examples

The source files are the implemented version. Where the spec's example values
disagreed with its own formulas, the formulas won:

- **Skipped Tones quality.** The spec says 74; the formula gives
  84 − 0.10 × 100 × 0.84 = 75.6, rounded to 76. The test asserts 76.
- **Pitch chance example.** 0.15 × 1.248 × 0.85 × 1.2 × 1.3 = 0.2482272, so the
  test asserts 0.248227, not the plan's first-draft 0.248208.
- **Price index at game start.** The spec wants exactly 1.00 on 1 April 1996
  and internet reach 0.1 through 1996, which fractional-year interpolation
  cannot give, so economic curves interpolate on the calendar year. Genre
  baselines keep fractional-year interpolation.
- **Doujin example copies.** The spec's 240 copies for fanbase 0 and quality
  84 assume a trend of exactly 1.00. Romance's baseline is 1.00, but the May
  trend update adds noise, so the state tests compute the expectation from the
  live trend and the pure rule test asserts 240.

## Decisions recorded during implementation

All plan-level decisions are listed at the top of the plan. The ones found
only by running the whole loop:

- **Contract start chapter.** A prodigy finishes chapters faster than a
  monthly magazine prints them, so finished doujin chapters pile up before and
  after acceptance. Without `Contract.FirstChapterNumber` the first close
  judged a pre-contract doujin chapter and kept missing on it. Chapters before
  the contract stay doujin work; the first tankobon of the smoke-test series
  covers chapters 4 to 9 for exactly this reason.
- **Cancellation rule on a miss.** A series on hiatus is never ranked, so the
  strike roll now also runs for a missed issue (with no below-line change).
- **Filler ids per magazine.** Fillers use a per-magazine counter so the
  global id counter, which sub-project 1 tests hard-code, is untouched. A
  filler retired at a close keeps its row in that issue's table, and the save
  validator accepts any id the magazine has issued.
- **Serialized due dates resequence** after every close so a chapter that
  misses and moves to the next close never collides with the chapter already
  due there.

## Balance observations (not changed)

- A solo mangaka at skill 80 cannot hold a weekly tier 1 slot: the 48-hour
  review after the Name stage plus 63 working hours per chapter exceeds a
  seven-day issue even with overtime. Monthly and biweekly magazines work.
  Sub-project 3's assistants are the intended answer; the state tests that
  need a weekly series force chapters complete instead.
- The same mangaka on a monthly magazine builds a backlog of approved chapters
  months ahead of their closes. That is harmless now, and a natural hook for
  hiatus and buffer mechanics later.

No publishing, commercial release, or deployment was performed. Later roadmap
milestones remain pending further instructions.
