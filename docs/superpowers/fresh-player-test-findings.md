# T1.10 fresh-player test, findings

Design: [considerations](specs/2026-09-27-fresh-player-test-considerations.md),
Parts 5 and 6. Preparation:
[completion record](fresh-player-test-preparation-completion.md).

Testers are called A, B and C. No names or contact details go in this file.
Returned reports stay out of git in `TestResults/fresh-player/`.

Status: round 1 returned. Tester A returned 2026-09-28; tester B returned 2026-10-01.

## Round 1 (alpha.12)

### Tester A

Returned 2026-09-28: questionnaire, own career report and practice career
report (`TestResults/fresh-player/tester-A/`), plus Discord comments passed on
by the user. Not in the target audience: "I don't like the simulator genre".

- Hardware and screen: desktop, windowed at 1600x900 inside a 1080p screen,
  Steam Proton on Kubuntu (Linux). Switched to the light theme at minute 3.
- Sessions and total real time: about 5 h 38 min in one sitting (22:45 to
  04:24). Own career 5 h 23 min from New Career to 12 May 1998, a bit over two
  in-game years; then the practice career for about 6 minutes.
- Real time to each T1.1 step (from the timeline, measured from New Career):

  | Step | Real time | Game date |
  |---|---|---|
  | First doujin completed | 0:12 | 6 Apr 1996 |
  | First sale | 0:17 | 8 Apr 1996 |
  | Later release that sells | 0:42 (reprint sales from 0:26) | 5 May 1996 |
  | First pitch | 0:21 | 8 Apr 1996 |
  | Serialization accepted | 0:41 | 2 May 1996 |
  | First magazine chapter | 1:21 | 21 Aug 1996 |
  | First hire | 2:25 | 18 Mar 1997 |
  | Setback: cancellation warning | 2:48 | 2 Apr 1997 |
  | Setback: pitch rejected | 2:57 | 2 May 1997 |
  | Setback: cancellation | 3:22 | 23 Jul 1997 |

- Pauses over 5 minutes: two, 6.6 min on Series details before opening Awards
  (Jun 1996) and 12.8 min paused before an overnight (Mar 1997).
- Share of time at each speed: 32x 39%, paused 35%, 1x 12%, overnight 10%,
  2x to 8x under 4% together. Most-opened screens: Series (290), Series
  details (100), Production (89), Books (86), Staff (78), Conventions (71).
- Sessions without an end line: none apart from a restart at minute 3.
- Help or lookups: none ("blind run").
- Guidance: at 31 min the tester chose "Enter a contest" on the Guidance page.
  From then on Helper-Chan only gave contest steps (route "contest" in the
  saved career). Serialization, the debut wait, hiring and setbacks were never
  guided. Only 12 distinct guidance steps appeared in 5 hours.
- Errors shown to the player (16): "Series is Paused, only Active series can be
  paused" (3 in a row), "...only Paused series can be resumed" (3), "Finish all
  current work and choose an uncollected 16 to 64 page unpublished manuscript"
  (7, contest entry), "Release the contest manuscript before pitching this
  title", licence and contest refusals. No crashes; the tester saw no bugs.

### Tester B

- Hardware and screen (questionnaire 11):
- Sessions and total real time:
- Real time to each T1.1 step (from the timeline):

  | Step | Real time | Game date |
  |---|---|---|
  | First doujin completed | | |
  | First sale | | |
  | Later release that sells | | |
  | First pitch | | |
  | Serialization and first magazine chapter | | |
  | Setback (own career or practice save) | | |
  | First hire | | |

- Pauses over 5 minutes, and where:
- Share of time at each speed (pause, 1x to 8x, 32x):
- Sessions without an end line:
- Help or lookups (questionnaire 1):

### Questionnaire summary

Tester A (B pending):

1. Help or lookups: none.
2. Stuck or confused: "At first, everything. Too overwhelming", no
   progressive disclosure. Learned to skim one type of value at a time. Still
   unclear why hired staff are "Available for work" while active titles are not
   being worked on.
3. Pace: "no idea what I'm doing" at the start; later the protagonist felt
   strong enough to save money and experiment.
4. 32x speed: helped, but open windows and scroll positions reset every few
   seconds mid-action.
5. Money: licence offers are poorly explained (lump sum or royalties, what fit
   and reliability mean).
6. Setback: felt random; genre ratios and rating charts are too inconvenient to
   track.
7. Saving and loading: fine; the practice career loaded.
8. Crashes and errors: none.
9. Would keep playing: not their genre.
10. One change: progressive disclosure and more visual, less textual
    explanations.
11. Screens and hardware: desktop, 1600x900 window, Proton on Kubuntu.
12. Anything else: positive sign-off. Discord: "the game doesn't help me learn
    to play it", "almost all" mechanics feel unexplained; about 5 hours played.

### Findings and proposed triage

Categories from Part 6: always fixed (crashes, lost saves, dead ends, needed
help, misleading money, deadline or status information); fixed if cheap or seen
by more than one tester; deferred to Tier 2 with a reason.

| # | Finding | Seen by | Evidence | Proposed triage | User decision |
|---|---|---|---|---|---|
| A1 | Choosing "Enter a contest" on the Guidance page replaces the career guidance for good; the contest route never ends or hands back | A | Route "contest" from 31 min; no career steps for 4.5 h | Always fixed (guidance dead end) | Approved 2026-09-28; fixed |
| A2 | 32x resets open panels and scroll positions every few seconds | A | Questionnaire 4 | Always fixed (breaks a core control) | Approved 2026-09-28; fixed |
| A3 | Too much at once from the start; no progressive disclosure | A | Questionnaire 2 and 10, Discord | Fix in Tier 1 as a focused pass (design question) | Approved; fixed 2026-10-03 ([progressive disclosure](progressive-disclosure-completion.md)) |
| A4 | Staff show "Available for work" while active titles sit unworked; assignment is unclear | A | Questionnaire 2 | Investigate, then fix if a status or assignment is misleading (always fixed) | Approved; fixed (status now names what the person waits for) |
| A5 | Buttons that cannot work are still enabled and answer with errors (Pause on a paused title, Resume on an active one, contest entry) | A | 16 error lines | Fixed if cheap: disable with the reason shown | Approved; fixed |
| A6 | Licence offer terms unexplained (lump sum or royalties, fit, reliability) | A | Questionnaire 5 | Explain, or hide licensing until Tier 2 (T1.8) | Approved; not reached by B. Explained 2026-10-04 on the offer card ([closeout](tier1-closeout-completion.md)) |
| A7 | Setbacks feel random; genre and ranking information hard to follow | A | Questionnaire 6 | Fixed if cheap or seen twice | Approved; not seen again. Closed 2026-10-04 without new work: caused by A1, which kept Helper-Chan's warning text from tester A; the Tier 2 stranger playtest watches for it ([closeout](tier1-closeout-completion.md)) |
| A8 | Timeline cleaner garbles words containing a staff name ("cur[name]t") | A (report) | Timeline | Fixed (cheap, already a deferred minor) | Approved; fixed |
| A9 | The tester's own career could not be loaded ("invalid publication slots"). A revised contest manuscript left its old draft looking like a waiting magazine chapter; once that title was serialized (23 Aug 1997), every later save was refused. Found by loading the returned career, not reported by the tester | A (career) | Replay of the career's command history | Always fixed (unloadable save) | Fixed; the tester's career now loads and plays on |

### Tester B

Returned 2026-10-01: questionnaire and own career report with timeline only (no
career file, no screenshot), `TestResults/fresh-player/tester-B/`. Played
alpha.12, which predates the title screen, the UI restyle and the tester A
fixes. Not in the target audience: "Not my type of game" (would keep playing:
2 of 5).

- Hardware and screen: 1600x900 (questionnaire 11; laptop or desktop not
  stated).
- Sessions and total real time: about 1 h 26 min in two sessions (7 min, then
  1 h 19 min). New Career to 21 Jun 1996, under three in-game months. Briefly
  loaded the imported practice career at minute 4.
- Real time to each T1.1 step (playing time from New Career):

  | Step | Real time | Game date |
  |---|---|---|
  | First doujin completed | 0:19 | 6 Apr 1996 |
  | First sale | 0:23 | 8 Apr 1996 |
  | First pitch | 0:24 | 8 Apr 1996 |
  | Pitch rejected | 0:34 | 25 Apr 1996 |
  | First hire | 0:56 | 28 May 1996 |
  | Serialization, setback, growing readership | not reached | |

- From the timeline: four pitch attempts during the rejecting magazine's
  cooldown (each an error), three "only Paused series can be resumed" errors
  (A5, fixed since), four minutes on the Furniture screen early, 40 visits to
  Staff and Person after the hire, 49 to Print doujin; 58 choices of 32x.
- Questionnaire, in short:
  1. No help or lookups.
  2. Stuck: "Hired worker was always unassigned except for conventions."
  3. Pace: "at the beginning only had to wait and nothing to do."
  4. 32x: used most of the time except in menus.
  5. Money: "only way to track money was print stock and sold, others were
     just numbers."
  6. Setback: pitch rejected, "I just continued working on the masterpiece."
  7. Saving: worked, "but once continue used older save."
  8. No crash or error.
  9. Keep playing: 2, not their type of game.
  10. One change: "hard to keep track where all menus were hidden."
  11. 1600x900.
  12. Furnishing: clicking outside the furniture menu leaves several buttons
      looking selected, with duplicated text at the bottom.

### Tester B findings and proposed triage

Checked against the current code (2026-10-01), not alpha.12.

| # | Finding | Seen by | Evidence | Proposed triage | User decision |
|---|---|---|---|---|---|
| B1 | A hire sits idle. Aki starts at 95 in every stage and wins every stage unless the hire is put on a team; a one-shot has one open stage at a time; a person off duty still holds their stage, so the hire cannot even cover Aki's absences. Status says "Available for work"; Helper-Chan says the planner assigns work and that an assistant can take Backgrounds or Tones, which it never does by default | A (A4), B | Questionnaire 2, report note, 40 Staff visits; Planner.cs:98-121, Guidance.cs:77, 122 | Always fixed (paid hire with no work, misleading guidance): design question on who takes which stages || Approved 2026-10-01; fixed ([record](tester-b-fixes-completion.md)) |
| B2 | The start feels like waiting with nothing to do | B | Questionnaire 3 | Covered by the career goals board (spec 2026-10-01) || Approved 2026-10-01 |
| B3 | Money is only legible through print stock and copies sold | B | Questionnaire 5 | Observe; the goals board's money goals and progress bars address part || Approved 2026-10-01 |
| B4 | Pitch stays enabled during a magazine's cooldown and errors; Helper-Chan's rejection tip names a better magazine but opens Publishing on the rejected one | B | 4 errors; Publishing.cs:132, Alpha.cs:182 | Always fixed (A5 class): disable with the date, preselect the suggested magazine || Approved 2026-10-01; fixed ([record](tester-b-fixes-completion.md)) |
| B5 | Continue opens the newest save across all careers by real time; importing a career makes it the newest, so Continue can open the wrong career | B | Questionnaire 7; CareerStore.cs:118, 158-171 | Always fixed (save confusion): Continue opens the career played most recently, imports do not count as play || Approved 2026-10-01; fixed ([record](tester-b-fixes-completion.md)) |
| B6 | Hard to keep track of where the menus are | A (A3), B | Questionnaire 10 | Part of A3 (progressive disclosure), now seen twice | Approved 2026-10-01; fixed 2026-10-03 with A3 |
| B7 | While furnishing, clicking the rail or speed buttons leaves them looking pressed; every notice shows twice (shell and workspace) | B | Questionnaire 12; Management.cs:142-148, 201-204 | Fixed if cheap (it is): lock the rail and header while furnishing, show a notice once || Approved 2026-10-01; fixed ([record](tester-b-fixes-completion.md)) |
| B8 | Resume on an active series errors | A (A5), B | 3 errors | Already fixed after round 1 || Approved 2026-10-01 |

A3, A6 and A7 after tester B: A3 is now seen twice (B6, and B2 in part); A6
(licences) was not reached by B; A7 (setbacks feel random) was not reported by
B, who ignored the rejection rather than finding it unfair. Both were decided
in the Tier 1 closeout on 2026-10-04 (rows above).

### Tester C (alpha.12, informal feedback, reported 2026-10-03)

Played alpha.12 (packaged 2026-09-27), before the title screen, start-up
screens, restyle, goals board and progressive disclosure. No questionnaire or
report; three remarks passed on by the user ("you need a better starter guide").

| # | Finding | Evidence | Proposed triage | User decision |
|---|---|---|---|---|
| C1 | Starting the game feels like reading an installation document | Remark 1 | Re-check against the current opening (disclaimer, first-launch volume, title, New Career, Helper-Chan's first texts) after the display work; much of alpha.12's opening has changed | Q65 option 1 approved 2026-10-04: quick start ([record](quick-start-completion.md)); not yet built or re-tested |
| C2 | Text still too small on a laptop even at the largest text size | Remark 2; text scale tops out at 150% and lives only in a career's Settings; the window ignores Windows display scaling (no stretch mode, fixed 1600 x 900) | Tier 1 display sub-project | Approved; fixed 2026-10-04 ([display settings](display-settings-completion.md)) |
| C3 | No resolution or full-screen setting | Remark 3; none exists | Tier 1 display sub-project (also needed for Steam Deck in Tier 2) | Approved; fixed 2026-10-04 ([display settings](display-settings-completion.md)) |

### T1.1 completion per tester

| Tester | Every step in timeline | Setback | No help or lookups | Completes T1.1 |
|---|---|---|---|---|
| A | Yes | Yes (own career: warning, rejection, cancellation; practice warning too) | Yes | Yes, formally; but guidance failed from 31 min (A1), so T1.2 is not met |
| B | No (serialization, setback and growing readership not reached in 1 h 26 min) | No | Yes | No; stopped early, not the target audience |

## Round 2 (alpha.13)

### Fixes since round 1

### Tester D

- Hardware and screen (questionnaire 11):
- Sessions and total real time:
- Real time to each T1.1 step (from the timeline):

  | Step | Real time | Game date |
  |---|---|---|
  | First doujin completed | | |
  | First sale | | |
  | Later release that sells | | |
  | First pitch | | |
  | Serialization and first magazine chapter | | |
  | Setback (own career or practice save) | | |
  | First hire | | |

- Pauses over 5 minutes, and where:
- Share of time at each speed (pause, 1x to 8x, 32x):
- Sessions without an end line:
- Help or lookups (questionnaire 1):

### Questionnaire summary

### Findings and proposed triage

| # | Finding | Evidence | Proposed triage | User decision |
|---|---|---|---|---|
| | | | | |

### T1.1 completion

| Tester | Every step in timeline | Setback | No help or lookups | Completes T1.1 |
|---|---|---|---|---|
| D | | | | |

## Evidence for other Tier 1 items

Ticked only with linked evidence and the user's judgement (Part 6). All items
were settled on 2026-10-04 in the
[Tier 1 closeout](tier1-closeout-completion.md); round 2 (alpha.13) became the
Tier 2 stranger playtest (Q53).
