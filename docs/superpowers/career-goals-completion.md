# Career goals board, completion

Date: 2026-10-02. Design: [spec](specs/2026-10-01-career-goals-design.md) (Q48
to Q51). Plan: [implementation plan](plans/2026-10-01-career-goals.md). Not yet
committed.

## What changed

- **Goal catalogue** (`src/MangakaSim/Goals.cs`, `GameState.GoalMeasures.cs`).
  Five career chapters (Doujin Days, Rookie, Serialized, Studio Head, Legend)
  plus optional Mastery goals; 4 to 6 goals each, every one with a measure, a
  one-line reason, Helper-Chan's tip and a one-off reward. A goal counts when
  reached by any route.
- **Goal state and rewards** (`GameState.Goals.cs`). The save keeps the current
  chapter, finished goals, unlocks and opportunities. The current chapter's
  goals are checked every hour; each reward is granted once: cash to the
  business account ("Goal reward: ..."), fans to the newest series, decorations
  into office storage, unlocks, a free convention table, a better chance on the
  next pitch. Finishing a chapter grants its bigger reward, queues Helper-Chan's
  scene and opens the next chapter. No permanent bonuses.
- **Reward furniture.** Nine new items drawn from simple shapes in code (trophy
  shelf, framed editor's letter, ranking chart, award plaque, housewarming
  plants, company sign, gold frame, award display cabinet, trophy). They cannot
  be bought. The professional drawing desk shows "Unlocked by: Rookie chapter"
  (the old reputation route still works, so no career loses it).
- **Older saves.** On load, every goal already met in any chapter counts as
  done, with its unlocks and decorations but no cash, fans, opportunities,
  pop-ups or texts; the replay checkpoint is rebased.
- **Helper-Chan.** She introduces each chapter, congratulates each goal and,
  after the first hire, keeps texting the next goal's tip instead of going
  quiet. Goal texts never stop 32x; her other texts still do. "Show me" and the
  board's How? share one route table.
- **The board.** A "Goals" item in the left rail with a badge ("Goals 2/5"), a
  Goals page (chapter, progress bars with real numbers, rewards, How?, mint tick
  and date when done), and a "This chapter" card on the office dashboard with
  the two closest goals.
- **Celebrations.** A goal shows a notice and her text and keeps 32x running. A
  chapter stops the game with a happy pop-up that opens the Goals page, plays
  the good-news music and queues her scene (shown when Stories are on).
- **Conventions page.** A free table shows as "Booth free (Doujin Days reward)"
  and is not counted against your cash.

## Chapter times (playtest harness, six careers over three years)

| Career | Doujin Days | Rookie | Serialized |
|---|---|---|---|
| Seed 0 Standard | 4 Aug 1996 | 16 Oct 1996 | not within 3 years |
| Seed 1 Standard | 4 Aug 1996 | 15 Nov 1996 | 16 Sep 1998 |
| Seed 42 Standard | 4 Aug 1996 | 16 Oct 1996 | 9 Feb 1999 |
| Seed 7 Standard | 4 Aug 1996 | 16 Oct 1996 | not within 3 years |
| Seed 7 Relaxed | 4 Aug 1996 | 18 Sep 1996 | 12 Nov 1997 |
| Seed 7 Challenging | 4 Aug 1996 | 18 Oct 1996 | not within 3 years |

Doujin Days waits on the first convention, which the guided test player books
late; its other goals finish in May to July. Real players following the goal can
book a local convention within weeks.

## Verification

- Unit tests: 711 pass (goal catalogue, progress, older saves, opportunities,
  guidance and the review fixes, each written to fail first, plus every
  existing test).
- Godot: new `--goals-smoke` (11 checks: rail badge, page, dashboard card,
  How?, goal text keeps 32x running, chapter stops 32x, celebration opens
  Goals, reward in storage, rewards not for sale, desk unlock label, free table
  on the Conventions page). All 20 Godot checks pass, warning-free build; the
  display sweep covers the Goals page (540 screen checks, 0 flagged).
- Playtest harness: all seven playtests pass, with goal and chapter times in
  their reports.
- Rendered review: the Goals page in both themes at 1920 x 1080, 3440 x 1440 and
  1280 x 720 with 150% text; the badge fits, bars and numbers read clearly,
  finished goals show a mint tick and date, How? is hidden on them.
- Independent review (most capable model, source only): no critical problems;
  three important ones fixed (older saves paid later chapters' goals; 32x could
  miss a stopping text once the phone thread was full; the free table was
  invisible and could block a booking), plus "Show me" losing the missed-payday
  route.

## Not verified

- Whether the goals make players want to play on. That needs the user, then
  testers.

## Rulings

- Built on `main`, uncommitted; the user commits.
- Existing tests updated for intended behaviour: the zero-quality sales identity
  counts goal rewards; three guidance tests (goal markers, guidance after the
  first hire, the per-step message count); the 32x stop list has 11 events.
- The free-table test books at least 8 days ahead, so the existing refund rule
  applies.
- "Reach the top 5" became "Reach the top 10": with top 5, four of six careers
  never finished the Serialized chapter in three years.
- The final review's "Show me" finding was raised from minor to important.

## Deferred minors

- State goals (two series, three staff, first hire) count only when true while
  their chapter is open, not if true earlier and then lost.
- While employed at another studio, goal cash and furniture go to the employer
  and the furniture stays behind; "Move out" cannot be met while employed.
- A fan reward with no titles is lost silently.
- Two chapters finished in the same moment show one pop-up.
- Chapter scenes wait while Stories are off (Stories are optional).
- The studio desk's purchase error still mentions reputation only.
- Some tests use staged events or call the bonus directly rather than the full
  path.
- Goal rewards are not in the Finances income chart (the ledger rows show them).
- The Rookie chapter's hourly event scans could be cached.
- Goal validation does not check record chapters or dates.

## Next step

Play a new career and an older save to see the board; then A3 (progressive
disclosure, seen by both testers).
