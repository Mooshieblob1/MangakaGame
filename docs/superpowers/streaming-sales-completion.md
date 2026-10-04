# Streaming sales and the selling tutorial, completion

Date: 2026-10-03. Spec `docs/superpowers/specs/2026-10-03-streaming-sales-design.md`
(Q60 shop hours, Q61 show it happening), plan
`docs/superpowers/plans/2026-10-03-streaming-sales.md`. Built task by task with a
review after each. Not yet committed.

## What changed

- **Simulation.** Each week's book sales are still planned on Monday at 00:00
  with the same formulas, but they now become sales plans released through shop
  hours, 10:00 to 20:00 every day (70 shop hours a week), instead of one lump.
  `SalesPlan` and `SalesRules` (`IsShopHour`, `ShopHoursUntil`, `NextShopHour`)
  hold the model; `GameState.Sales.cs` releases each plan's share every shop
  hour. Local shops sell from delivered stock as before (oldest print run
  first, presentation penalty with its fraction carried across the week,
  reserved convention copies untouched, copies due while there is no stock are
  missed customers). Online downloads and publisher digital and overseas deals
  are sold and paid as released; their weekly receipt is created at planning
  time and fills hour by hour. Publisher volumes update copies sold, reprint
  and 100,000 and 1,000,000 milestones and fan growth hour by hour. Income
  merges into one ledger line per title, reason and day.
- **A book on sale midweek sells at once.** A release or a delivery plans the
  rest of its first week (at least 30 shop hours); listing an already released
  book online midweek plans its first download week at listing time. The
  download week age now counts from the first planned receipt week, which fixed
  a doubled first week of downloads found by the playtest comparison.
- **Saves.** Optional `SalesPlans`, `LastShopHourAt` and recap-income fields
  load as empty in older saves; validation of plans and receipts was added
  (week one may be dated Monday 00:00 before the 08:00 start).
- **Tutorial and texts.** Helper-Chan says when the first copies go on sale
  (`sell`, `sell-online`), then a `sell-more` text offers "Sell online" and
  "Conventions" with the one-time first-copy message handled even when the first
  copy sells on the arrival tick. The first-copy goal tip, the online listing
  news ("Downloads sell through the day"), the local distribution status ("Shops
  sell through the day, 10:00 to 20:00", "Sales weeks remaining") and the
  daily recap ("Income since the last recap") were reworded. The recap now counts
  income since the previous recap, so evening shop sales are not lost.
- **Presentation.** The header Sold count pulses when the first copies reach the
  shops, a gold "First copy sold!" bubble drops below it (both off with reduced
  motion), the Sold count takes the focus from the `sold` route, and the header
  money flashes for income merged into today's ledger line. New `--selling-smoke`.

## Verification

Run on 2026-10-03 on this computer, with `.superpowers/sdd/work-feedback/suite.ps1`
(now including `--selling-smoke`), logs in `.superpowers/sdd/work-feedback/suite.txt`
and `.superpowers/sdd/2026-10-03-streaming-sales/runs/`.

- Unit tests: 768 pass, 0 failed (761 without the playtest category, plus the 7
  playtests). Warning-free build (`dotnet build MangakaGame.sln -warnaserror`:
  0 warnings, 0 errors).
- Godot: all 23 checks print their PASS line with no fatal. In the suite run
  `--goals-smoke` then exited -1073741795 after its PASS line (the shutdown
  crash under Rulings); it was re-verified after the fix and exited 0. The
  other 22 exited 0. Counts: smoke-test 823,
  management 99, progression 16, alpha 47, usability 50, production 21,
  office-life 730, convenience 25, series-status 23, atmosphere 877,
  family-home 75, quiet-speed 1519, display-sweep 26, journey 53 (end to end),
  music 14, startup 16, title 74, brand 29, tester-b 6, goals 11, disclosure 13,
  work-feedback 19, selling 27 (28 with captures).
- Display sweep: 560 screen checks, 0 flagged.
- Playtests (`--filter Category=Playtest`, run on their own): 7 of 7 pass (six
  three-year guided careers and the practice-career fixture).
- Rendered review at 1920 x 1080 (`TestResults/selling-first-sale.avif`): the
  header "Sold 53 · Stock 47" is readable with the focus frame, the "First copy
  sold!" bubble sits whole below it, the header money change and Helper-Chan's
  notice line read cleanly, and the phone shows the new reader text with "Sell
  online", "Conventions" and "Later". A first capture showed the phone's bottom
  row cut off at the window edge; a second capture after a 0.6 second wait (a
  temporary edit, reverted) showed the phone settled and fully inside the
  window, so the cut-off was the existing 0.25 second open slide caught after
  two frames, not a layout fault. Only the first-sale moment was captured;
  there is no mid-week office capture.

### Playtest comparison (baseline `ca8ff41` against the finished feature)

Taken by Task 5 with the guided career harness. Money columns are personal /
business yen at the start of each month shown. Doujin sales and digital receipts
are totals over the career with the ledger line count in brackets.

| Playtest | Run | First sale | First hire | Copies sold (all series) | 1996-07 | 1997-04 | 1998-04 | End (1999-04) | Doujin sales ¥ (lines) | Digital receipts ¥ (lines) |
|---|---|---|---|---|---|---|---|---|---|---|
| seed 0 Standard | base | 1996-04-08 | 1996-08-22 | 7,386 | 204,482 / 296,438 | 1,028,014 / 1,349,107 | 1,988,968 / 40,573 | 3,177,411 / 0 | 79,740 (17) | 311,010 (367) |
| | new | 1996-04-08 | 1996-08-22 | 7,559 (+2.3%) | 205,931 / 302,024 | 1,029,412 / 1,354,699 | 1,988,685 / 36,206 | 3,176,922 / 0 | 119,460 (84) | 321,720 (1,665) |
| seed 1 Standard | base | 1996-04-08 | 1996-08-05 | 20,765 | 212,077 / 337,102 | 1,025,817 / 1,314,338 | 1,730,282 / 0 | 2,997,013 / 534,900 | 131,528 (27) | 728,560 (519) |
| | new | 1996-04-08 | 1996-08-05 | 18,241 (-12.2%) | 214,332 / 345,628 | 1,027,194 / 1,327,707 | 1,734,755 / 0 | 2,953,007 / 356,017 | 122,620 (85) | 687,855 (1,885) |
| seed 42 Standard | base | 1996-04-08 | 1996-08-22 | 137,862 | 204,524 / 296,606 | 1,028,470 / 1,401,963 | 2,436,061 / 1,205,915 | 10,146,578 / 25,076,836 | 1,214,916 (105) | 24,110,065 (2,051) |
| | new | 1996-04-08 | 1996-08-22 | 141,661 (+2.8%) | 206,078 / 302,612 | 1,030,138 / 1,408,635 | 2,487,211 / 1,416,539 | 10,299,894 / 25,664,983 | 1,217,724 (213) | 24,868,445 (1,783) |
| seed 7 Standard | base | 1996-04-08 | 1996-08-22 | 7,779 | 204,398 / 296,102 | 1,027,955 / 1,399,903 | 1,988,676 / 202,489 | 3,190,908 / 566,422 | 144,729 (25) | 357,735 (379) |
| | new | 1996-04-08 | 1996-08-22 | 7,818 (+0.5%) | 205,700 / 301,100 | 1,029,389 / 1,405,639 | 1,988,927 / 206,513 | 3,193,339 / 576,142 | 155,900 (107) | 358,715 (1,699) |
| seed 7 Relaxed | base | 1996-04-08 | 1996-05-22 | 64,958 | 404,398 / 434,633 | 1,229,902 / 1,331,791 | 2,582,910 / 1,385,081 | 4,384,122 / 2,315,642 | 51,780 (11) | 954,030 (298) |
| | new | 1996-04-08 | 1996-05-22 | 64,454 (-0.8%) | 405,700 / 439,631 | 1,230,316 / 1,333,447 | 2,582,757 / 1,384,469 | 4,382,751 / 2,256,967 | 54,900 (43) | 944,055 (1,476) |
| seed 7 Challenging | base | 1996-04-08 | 1996-09-21 | 6,825 | 111,784 / 185,926 | 927,977 / 1,367,378 | 1,359,960 / 0 | 547,950 / 113,060 | 141,790 (29) | 564,445 (570) |
| | new | 1996-04-08 | 1996-09-21 | 6,718 (-1.6%) | 113,996 / 194,284 | 929,127 / 1,381,983 | 1,413,132 / 0 | 552,541 / 139,589 | 127,480 (84) | 561,925 (1,851) |

Reading: first sale and first hire dates are unchanged in all six careers, copies
are within -1.6% to +2.8% in five of six, and money is slightly higher early on
(sales arrive sooner) and within about 2% later. Seed 1 (-12% copies) is a
decision cascade: stock now sells through the shops, so the guided player held
16 unsold copies at the debut-wait advice instead of 62 and booked six fewer
conventions, which meant fewer fans and a lower ranking climb. The ledger has
about 4 to 5 times as many digital receipt lines (one per title, reason and day),
as designed.

## Rulings

Each is a decision made during the build, with its cost if wrong.

- Ruling: work on main, uncommitted (user rule: commit only when asked; prior plans same), reviews use working-tree snapshots via snap.sh (commit objects on no branch, refs under refs/sdd/streaming-sales/, deleted at finish), cost if wrong: refs to delete by hand.
- Ruling: plan "Commit" steps are skipped; implementers must NOT git commit; controller snapshots after each task, cost if wrong: none (user commits later).
- Pre-flight, T2 and T3: Ruling: T2 implementer also adds T3's two methods (code given in T3) so T2 compiles; T3 then adds its tests and receipt validation only, cost if wrong: T3 review sees less diff
- Pre-flight, T3: Ruling: test's first assertion (receipt.Units==0 right after planning) must be taken at the planning tick, not a week later, implementer adjusts: assert at planning tick then after the week, cost if wrong: test rework
- Task 2: Ruling: per-hour Math.Floor of the presentation penalty in SellStock loses up to 1 copy/hour (copy shop sells 0 when due=1), plan-mandated, spec wins ("weekly totals per book stay the same", "exactly as today ... presentation penalty"): streamed shop sales must carry the penalty's fraction across the week's hours so the week adds up to the old single-call result (within 1 copy); store the carry on the plan as an optional field, cost if wrong: small save field to remove.
- Task 2: Ruling: ledger-merge test asserting Count<=1 passes vacuously (plan-mandated), replace with exactly-one-line equals day's streamed revenue across >=2 shop hours, plus a creator-share bill merge test, cost if wrong: none.
- Task 2: Ruling: per-hour yen rounding (<=1 yen per hour per channel) accepted as within "weekly totals" intent, cost if wrong: a few hundred yen a year per title.
- Task 3: implemented (snapshot a1f20d94ebfc707f50d337c4c5fc9d972c81a501). Ruling: a midweek online listing of an already-released book plans its first download week at listing time (remaining shop hours, min 30), per spec 'a book that goes on sale midweek starts selling at once', added to Task 4 scope, cost if wrong: downloads start up to a week earlier than old rules.
- Task 4: Ruling: plan and receipt weeks may be Monday(GameClock.Start) (1 Apr 1996 00:00, before the 08:00 start); both validators relaxed to Week >= Monday(GameClock.Start), otherwise first-week releases write unloadable saves, cost if wrong: validation slightly looser for week one only.
- Task 4: Ruling: PrintedBook() fixture now delivers on the Monday tick (midweek delivery sold its 100 copies before the tests' Monday), accepted, intent unchanged, cost if wrong: none.
- Task 5: Ruling: listing a doujin online before its print arrives starts its shop week with no stock, so shop copies due before delivery are missed (139 -> 82 in a controlled case), kept, as the spec's "copies due while there is no stock are missed customers"; old rules lost the whole week whenever the print arrived after the next Monday, cost if wrong: players who list online first lose some early shop sales; revisit if testers hit it.
- Task 5: Ruling: the daily recap must not leave evening shop sales (after the recap fires, before 20:00) out of every recap, they appear in the next day's recap; added to Task 6 scope, cost if wrong: recap code change touches recap tests.
- Task 8: the `--goals-smoke` check printed its PASS line but exited with a
  managed-object shutdown crash (exit -1073741795, "Leaked unsafe reference"),
  first at Task 4's snapshot and not at Task 3's or the baseline. Cause (inferred
  from the fix working): Godot mono's shutdown race, which depends on how much garbage
  the run produced (Task 4's earlier sales change the allocation pattern), not a game defect;
  `GC.Collect(); GC.WaitForPendingFinalizers();` before quitting fixes it
  (`godot/DebugMain.GoalsSmoke.cs`, exit 0 on two reruns). Cost if wrong: another
  smoke may show the same exit crash after a PASS line; the game's own quit path
  was not shown to be affected. The final review fixes replaced this one-off
  call with a shared quit helper used by the game's own quit paths too.

## Deferred minors

- Task 1: minor (deferred): test 3 name overclaims; SalesPlan validation branches untested; DoesNotContain("SalesPlans\":[") weak.
- Task 1: minor (deferred): SalesClosed clause dereferences p.Kind before null check (corrupt saves only).
- Task 1: minor (deferred): AgreementId rule allows negative ids / unchecked agreement existence (T3 adds receipt check).
- Task 1: minor (deferred): ShopHoursUntil loops hourly; do not call per tick.
- Task 2: minor (deferred): missed-copies test does not assert zero sales while out of stock / no catch-up after reprint.
- Task 2: minor (deferred): word-of-mouth now uses Fanbase including the week's streamed gains (negligible shift).
- Task 2: minor (deferred): RecordSales FindIndex/Receipts scans per volume-hour are O(history).
- Task 2: minor (deferred): carry chains across runs within an hour (total conserved, attribution differs).
- Task 2: minor (deferred): older-save test tuned to Total 60; bill merge test lacks different-month case; uses reflection on AddBill.
- Task 3: minor (deferred): download receipt test lacks a mid-week partial assertion; milestone test would not catch a Monday-tick milestone (plan-mandated); receipt check scans all receipts per plan.
- Task 4: minor (deferred, carried into Task 5 dispatch): TimelineValidation receipt week lost TimeOfDay==0 check.
- Task 4: minor (deferred): Sunday overlap (two plans, WeeksOnSale 2, within demand) tested by inspection only; midweek test assertion weak; listing plans 0-unit receipts for other active channels.
- Task 5: minor (deferred): daily recap's yen earned can miss evening shop sales made after the recap fires.
- Task 5: minor (deferred): ProductionClarity unselected title's protection looser (stock+sold conserved, not unchanged); StudioOperations and PublishingConvenience bounds could be tighter.
- Task 6: minor (deferred, flag to final review): sell-more / one-time sell decided from thread history only; trimmed threads or older saves past first sale (not serialized, never listed) see "has its first readers!" again for 3 days, suggest a lasting marker in preferences.Completed.
- Task 6: minor (deferred): pre-sale "sell" path at Guidance.cs:86 not pinned by a dedicated test; recap figure can miscount once after a business switch; recap income now spans days without a recap (Godot label wording to Task 7).
- Task 7: minor (deferred, flag to final review): Sold watcher not reset when ControlledBusinessId changes, a career move into an own studio with a transferred title can fake a pulse / "First copy sold!".
- Task 7: minor (deferred): smoke does not cover reset wiring on state replacement; pulse repeats on restock and for non-header series; phone replies fixed at build time and skip ShowGuidance guard/hint; FocusMode.All adds a Tab stop; bubble check relies on node name; README line 265 still says Mondays (fixed in Task 8).

Task 4's receipt `TimeOfDay` minor was fixed in Task 5. The two items flagged for
the final review (the lasting first-sale marker for trimmed threads and older
saves, and the Sold watcher not resetting when `ControlledBusinessId` changes)
and the Task 2 missed-copies assertions were fixed in the final review fixes
below.

## Final review fixes

The whole-feature review (2026-10-03) found three important and several minor
problems. Fixed, each with a test that failed first:

1. **First-week spillover drained the next week's demand.** A book delivered
   late in the week plans at least 30 shop hours, so part of its first week sells
   after the next Monday, where it used up the new week's demand and left a
   convention that week almost nothing (318 copies of demand down to 42 in the
   test). Shop sales from a plan dated to an earlier week now sell up to what is
   due and leave the current week's demand alone (`GameState.Printing.cs`
   `SellStock`). Test: `StreamingSalesTests.A_first_week_spilling_past_monday_sells_without_draining_the_next_weeks_demand`
   (Sunday 19:00 delivery: the spillover still sells, the next week's demand
   falls only by its own shop sales, a Saturday convention sells as much as
   without the spillover). The missed-copies test now also asserts no sales
   while out of stock and no catch-up after the reprint.
2. **The Sold watcher scanned sales history every frame.** `ObserveSales` now
   looks again only when the clock or the office revision moved, and skips the
   first-sale scan once a sale is known. `--selling-smoke` checks that three
   frames with a still clock do no recomputation and one moved hour does one.
3. **Quitting could crash at Godot shutdown.** One helper, `QuitGame` /
   `QuitTree` (`DebugMain.Transitions.cs`), collects managed garbage before
   quitting. The title Quit button, the window close and the 18 smoke checks
   that quit through the shared timer pattern use it; the goals smoke's one-off
   call is gone. Five smokes that quit directly (`smoke-test`, management,
   progression, atmosphere, family home) were left unchanged. `--selling-smoke`
   now ends after its sales by Quit to title and the title's Quit button, and
   exits 0.
4. **The selling tutorial came back for older saves and trimmed threads.** The
   `sell-more` step and the one-time `sell` step after a sale now also need the
   title's first sale to be recent in the career sales history (its week began
   under 14 days ago, so 7 to 14 days after the sale), beside the three-day
   thread rule. Tests: `SellingTutorialTests.An_older_save_long_past_its_first_sale_does_not_hear_about_first_readers_again`,
   `SellingTutorialTests.A_trimmed_thread_does_not_bring_the_sell_more_suggestion_back`.
5. **A career move could fake a Sold pulse or "First copy sold!".** The watcher
   treats a change of controlled business as a first look, and once the career
   had a sale it never shows the bubble again. `--selling-smoke` moves the
   watcher into a business that already sells a title and checks that nothing
   is announced (this check failed with the reset turned off).
6. `README.md` no longer says sales happen on Mondays.
7. This record: wording fixes above.

Not fixed: conventions in the week after a partial release week still see
second-week demand. That is a balance decision for the user (Q62).

Verification after the fixes: build 0 warnings, 0 errors; 764 unit tests pass
without the playtest category (761 before, plus three new tests); Godot
`--selling-smoke` 31 (was 27; it now ends through the title Quit), work-feedback 19,
alpha 47, journey 53, goals 11, title 74, management 99 and startup 16, all exit
0 with no fatal; `--goals-smoke` run three times, exit 0 each time.

## Not verified

- How the trickle feels to a new player: whether the Sold count ticking up, the
  money flashes and the first-sale bubble are enough to make selling
  discoverable, and whether 32x and the overnight skip hide too much of it. Only
  stills, smokes and simulated careers were checked; this needs a real tester.
- A 1280 x 720 capture at 150% text of the first-sale moment: the selling smoke
  captures only at 1920 x 1080, and no existing flag gives that combination for
  this moment (the display sweep covers those sizes for the core screens, with
  0 flagged, but not the Sold pulse or bubble).
- A mid-week office capture with the new feedback at other window sizes.
- Clean-machine and Steam Deck behaviour, as before.
- The window-close quit path on its own: it now uses the same `QuitGame`
  helper as the title Quit button, which `--selling-smoke` exercises, but no
  check sends a real close request.
- Tuned balance: the playtest comparison is three in-game years of guided
  careers, not human play.

- Final full run after the fix wave: 771 unit tests pass (including the seven playtests), all 23 Godot checks exit 0 (selling 31, goals 11), display sweep 560 screen checks, 0 flagged.
- Q62 (2026-10-03, user chose 1): conventions in the week after a midweek release keep week-two demand; no code change.
