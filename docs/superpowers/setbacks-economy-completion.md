# Tier 1 fix 3: setbacks and economy balance, completion

Date: 2026-09-26. Design: [considerations](specs/2026-09-26-setbacks-economy-considerations.md)
(Q19 to Q21). Plan: [implementation plan](plans/2026-09-26-setbacks-economy.md).
Committed as `8d71521`.

## What changed

- **Pitch odds.** Base chances by tier are now 12%, 24% and 40% (were 15%,
  30% and 50%). A debut pitch to a tier 3 monthly shows about 48%, so roughly
  half of careers meet a first rejection.
- **Cancellation pressure.** The protected opening is 6 published chapters
  (was 8), the warning clock starts sooner, and tier 2 and 3 magazines open
  with stronger competing series. Reader surveys now weigh readership at 60%
  and craft at 40%, so a skilled unknown still has to win readers.
- **Sales by magazine size.** Collected edition demand scales with the
  magazine's tier and page count (weekly 19, biweekly 24, monthly 32 pages per
  issue). Volume sales grow readership more slowly.
- **Print-run royalties.** The first print run is paid at release and each
  reprint when sales pass it, instead of a royalty per copy each week. Digital
  and overseas income stays per copy. Nothing is stored; the paid amount is
  worked out from copies sold, so the save format (version 10) is unchanged.
- **Newcomer page fees.** Page fees are offered as if reputation were at least
  50, so the months after the first hire hold level.
- **Guidance.** `cancellation-warning` explains the rank against the line, how
  many issues remain, the weakest factor with a concrete fix, and the option to
  end the series. `series-cancelled` explains what is kept, the 52-week wait at
  that magazine and where to go next.
- **Phone layout.** On screens wide enough to keep a page readable, the open
  phone keeps its own column beside the page. On cramped screens (for example
  1280x720 with 150% text) opening a page folds the phone to its icon, new
  texts wait on the icon instead of opening the phone over the page, and a
  phone the player opens on purpose floats over the page. The header now
  triggers a relayout when its height changes, which fixed a stale layout
  after resizing the window.

## Verification

Automated and rendered checks only; no human playtesting.

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 568 tests pass, including new
  cases for the pitch odds, the grace period, tier-scaled sales, the
  print-run ladder, the fee floor and both guidance steps.
- Headless Godot checks: `--smoke-test` 823, `--management-smoke` 99,
  `--alpha-smoke` 45, usability 48, production 21, series status 18 and
  progression 15 checks pass. The alpha smoke now runs at the project window
  size and checks, at 1280x720 with 150% text, that the phone folds to its
  icon, that new texts wait there, and that the runway line is readable.
- Rendered runs with `--capture`: management 122, alpha 62 and usability 53
  checks pass. Captures in `TestResults/setbacks-economy/`:
  `alpha-hiring-runway-720-150`, `alpha-hiring-runway-1080`,
  `alpha-hiring-runway-warning` and `alpha-wage-arrears` (AVIF). Viewed: at
  1280x720 with 150% text the phone is folded and the runway line is fully
  readable; at 1920x1080 the phone has its own column beside the page.
- Career playtest re-run on all five seeds and presets; see the
  [findings](full-career-playtest-findings.md).

## Known issues

- **Hit series still grow large.** Seed 1 found a strong series and ends at
  about ¥10.6 million in the business by April 1999, mostly from reprints.
  This is the intended "better series clearly ahead" case, but the size of
  reprint income should be reviewed in the Tier 2 balance pass over 5 to 10
  years.
- **Relaxed growth hire.** The playtest bot's scripted third hire and studio
  move in late 1997 empty the business on seed 7 Relaxed, with three missed
  paydays in 1998 that personal savings cover. Guided players are not sent to
  that hire; the bot script is aggressive on purpose.
- **Tight page at 1280x720 with 150% text.** The header wraps to three rows,
  leaving a short scroll area on pages such as Recruitment. It works and is
  readable, but a more compact header at that size would help (T1.9).
- **Difficulty.** Challenging and Relaxed differ only in money, not in
  setbacks: seed 7 is cancelled on the same day on both. Covered by the
  planned "difficulty that matters" fix.
