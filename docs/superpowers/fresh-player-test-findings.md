# T1.10 fresh-player test, findings

Design: [considerations](specs/2026-09-27-fresh-player-test-considerations.md),
Parts 5 and 6. Preparation:
[completion record](fresh-player-test-preparation-completion.md).

Testers are called A, B and C. No names or contact details go in this file.
Returned reports stay out of git in `TestResults/fresh-player/`.

Status: round 1 in progress. Tester A returned 2026-09-28; tester B pending.

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
| A3 | Too much at once from the start; no progressive disclosure | A | Questionnaire 2 and 10, Discord | Fix in Tier 1 as a focused pass (design question) | Approved; waits for tester B |
| A4 | Staff show "Available for work" while active titles sit unworked; assignment is unclear | A | Questionnaire 2 | Investigate, then fix if a status or assignment is misleading (always fixed) | Approved; fixed (status now names what the person waits for) |
| A5 | Buttons that cannot work are still enabled and answer with errors (Pause on a paused title, Resume on an active one, contest entry) | A | 16 error lines | Fixed if cheap: disable with the reason shown | Approved; fixed |
| A6 | Licence offer terms unexplained (lump sum or royalties, fit, reliability) | A | Questionnaire 5 | Explain, or hide licensing until Tier 2 (T1.8) | Approved; waits for tester B |
| A7 | Setbacks feel random; genre and ranking information hard to follow | A | Questionnaire 6 | Fixed if cheap or seen twice | Approved; waits for tester B |
| A8 | Timeline cleaner garbles words containing a staff name ("cur[name]t") | A (report) | Timeline | Fixed (cheap, already a deferred minor) | Approved; fixed |
| A9 | The tester's own career could not be loaded ("invalid publication slots"). A revised contest manuscript left its old draft looking like a waiting magazine chapter; once that title was serialized (23 Aug 1997), every later save was refused. Found by loading the returned career, not reported by the tester | A (career) | Replay of the career's command history | Always fixed (unloadable save) | Fixed; the tester's career now loads and plays on |

### T1.1 completion per tester

| Tester | Every step in timeline | Setback | No help or lookups | Completes T1.1 |
|---|---|---|---|---|
| A | Yes | Yes (own career: warning, rejection, cancellation; practice warning too) | Yes | Yes, formally; but guidance failed from 31 min (A1), so T1.2 is not met |
| B | | | | |

## Round 2 (alpha.13)

### Fixes since round 1

### Tester C

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
| C | | | | |

## Evidence for other Tier 1 items

Ticked only with linked evidence and the user's judgement (Part 6).

- T1.2 (guidance):
- T1.3 and T1.4 (money and clarity):
- T1.5 (time to first sale, target 15 to 20 minutes):
- T1.6 (saves):
- T1.7 (crashes and errors):
