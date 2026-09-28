# Tier 1 closeout and tester A fixes, completion

Date: 2026-09-28. Scope approved by the user on 2026-09-28: finish the Tier 1
items that do not depend on testers (T1.6, T1.7, T1.8), then fix the approved
findings from fresh-player tester A. Findings and triage:
[fresh-player-test-findings.md](fresh-player-test-findings.md). Not yet
committed. alpha.12 is unchanged for tester B.

## What changed

### Saves (T1.6)

- **alpha.11 careers vanished from the Load list** and could not be imported
  ("Invalid guidance or audio preferences"). Tier 1 fix 1 renamed Helper-Chan's
  guidance routes and the save checker refused the old names. The checker now
  accepts them and the guidance moves them onto the career path
  (`Guidance.cs`, `CareerStore.cs`).
- **Saves the game cannot read are named, not hidden.** The Load Career screen
  says how many saved files could not be read and that they stay untouched
  (`CareerStore.Unreadable`, `DebugMain.ManagementMenus.cs`).
- **A revised contest manuscript made every later save unloadable** (finding
  A9, found by loading tester A's career). Revising left the old draft looking
  like a waiting magazine chapter; once that title was serialized, loading was
  refused with "invalid publication slots", and the draft also counted as a
  chapter ready for the magazine. Old drafts are now marked when a manuscript
  is revised, kept out of the magazine logic, and repaired when an older save
  loads (`Chapter.Superseded`, `Chapter.MagazineBound`, `GameState.Awards.cs`,
  `GameState.Serialization.cs`). The tester's own career now loads and plays on.
- New fixtures: `alpha11-opening.career` (written by the packaged alpha.11
  game) and `alpha11-year1/2.mangaka` (one and two in-game years from the
  alpha.11 simulation, including serialization, a hire, an award, a licence
  offer and a studio move). The journey walk-through now loads the two-year
  career in the real screens.

### Stability (T1.7)

- **Unexpected errors reach the report.** Godot's error stream, where
  unexpected C# exceptions land, now feeds the session timeline as "error
  unexpected ...", cleaned of paths and names, at most 20 distinct lines per
  session. The player sees one notice suggesting a report
  (`godot/TimelineErrorLogger.cs`, `DebugMain.Timeline.cs`).

### Extra systems (T1.8)

- Mapped how awards, licensing, moves and branches, loans, rivals and digital or
  overseas deals reach a new player (notes in the closeout workspace). Awards,
  licences, loans and moves are acceptable as they are.
- The publisher digital and overseas proposal buttons are greyed out, with a
  reason, until those routes exist in the game's history.
- With one studio, the rail's studio picker reads "YOUR STUDIO" and is not
  clickable.
- Open: rival job offers to the mangaka arrive from mid-1996 with pausing
  notices and a one-click "Accept career offer". Folded into finding A3
  (progressive disclosure), which waits for tester B.

### Tester A findings

- **A1 guidance dead end.** A contest or employment route is now a side trip:
  career moments that need the player (an offer, the debut wait, a first hire,
  a warning, a rejection, a cancellation) come first, and a contest hands back
  to the career path once its manuscript is entered. The Guidance page says so.
- **A2 32x closed the open page.** A day that rolls into the night at 32x no
  longer returns to the office, so the open page and its scroll position stay.
- **A4 "Available for work".** Staff with assigned pages they cannot start yet
  now show what they wait for, for example "Waiting for Aki's storyboard ·
  title", "out for a pitch" or "waiting for the editor"
  (`GameState.WaitingReason`).
- **A5 buttons that could not work.** Pause and Resume offer only the change
  that applies; the pitch button is greyed out with a reason while a contest
  manuscript is out; the Awards page offers only titles and entries the
  contest rules accept, and explains why when there are none.
- **A8 timeline cleaner.** Names are replaced as whole words only.
- **Final review fix:** choosing "Enter a contest" again while an entry is
  being judged is respected and no longer repeats the "entered" text.
- No balance change. One optional save field (`Superseded`); older saves load.

## Verification

### Automated

- `dotnet build MangakaGame.sln -warnaserror`: no warnings or errors.
- `dotnet test --filter Category!=Playtest`: 626 tests pass. New:
  `OldSaveTests` (5), `ContestDraftTests` (3), `StaffWaitingTests` and
  `AdoptManuscriptOfferTests` (3), two contest detour tests in `GuidanceTests`
  and a whole-word test in `SessionTimelineTests`. Each new test was run
  failing first; the old-save, contest-draft, detour and redactor tests failed
  on the real fault, the others first failed to compile.
- `Three_year_guided_career`: 6 careers over three in-game years, no errors,
  saves and replays match.
- Headless Godot checks: `--smoke-test` (823), `--management-smoke` (99),
  `--alpha-smoke` (45), `--progression-smoke` (16), `--usability-smoke` (50),
  `--production-smoke` (21), `--series-status-smoke` (23),
  `--convenience-smoke` (25), `--quiet-speed-smoke` (1,177, with new checks for
  the open page at 32x), `--atmosphere-smoke` (893), `--journey-smoke` (53, with
  the alpha.11 career and the error catcher) and `--display-sweep-smoke` (480
  screen checks, 0 flagged).
- Old saves: a scan of 1,069 careers left in `TestResults` by earlier alpha
  checks. With the fixes, 1,055 load and play on; 14 are refused because the
  alpha smoke check edits their savings directly, which the validator rightly
  rejects.
- Tester A's own and practice careers load and play 60 more days (temporary
  check, not kept, because it reads the tester's files).

### Not verified

- No rendered captures were reviewed for this change; the display sweep checks
  layout only.
- No human has played these fixes yet. Tester B plays alpha.12, which does not
  contain them.

## Next step

Wait for tester B, then triage A3, A6 and A7 together with B's findings.
Meanwhile, option 2: the music system.
