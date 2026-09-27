# Tier 1: fresh-player test (T1.10), considerations

Date: 2026-09-27.
Status: design approved 2026-09-27 (Q31 to Q34); plan written, not yet
implemented.
Plan: `../plans/2026-09-27-fresh-player-test.md`. Checklist item: T1.10 in the
roadmap's Release plan section.

## Problem

T1.10 asks that someone who has never seen the game completes the core journey
(T1.1) without help, and that everything they run into is fixed or explicitly
deferred. So far all evidence comes from the automated career playtest, my own
rendered checks and the user's own feedback. Nobody new has played the game.

Several open questions can only be answered by a new player:

- whether Helper-Chan's texts are enough to find every step (T1.2),
- whether money, deadlines and status make sense (T1.3, T1.4),
- whether the pace feels right, including the 32x speed (T1.5),
- whether a player notices and understands a setback and the recovery advice,
- items left over from the playtest findings: the convention and part-time job
  texts alternating about once a week late in the debut wait, and unpaid wages.

The game currently records nothing about how a session went, so a tester's
report would rest on memory alone.

## Q31: how should the test be run?

Answer: **2**, unmoderated at home. Each tester gets the build, a one-page brief
and a questionnaire, plays on their own computer in as many sittings as they
like, and sends back the game's problem report. "Without help" is self-reported
in the questionnaire. Testers send the report even if they give up, because an
abandoned run is the most useful evidence.

## Q32: how do we see what happened in a session?

Answer: **1**, a session timeline log kept by the game, attached to the problem
report. See Part 1.

## Q33: how long does a tester play?

Answer: **1**. Testers must reach the first hire, which takes about 1 to 1.5
real hours, in any number of sittings. After that they play on as long as they
enjoy it, up to three in-game years.

- Any pitch rejection, cancellation warning or cancellation counts as the
  setback.
- If none happens, the tester loads the practice save (Part 3) and plays until
  Helper-Chan warns about cancellation.
- Testers choose Standard difficulty. Relaxed rarely produces a setback (none
  in the automated playtest), and Challenging would make the first run harder
  than most players will choose.
- T1.1 is reworded from "in order" to "reaches each of", because the first hire
  normally comes before any setback (day 143 against April 1997 on seed 0).

## Q34: how many testers and rounds?

Answer: **1**, two rounds.

- Round 1: two fresh testers play alpha.12. Their findings are triaged and fixed.
- Round 2: one new tester plays alpha.13.
- T1.10 is ticked when a round 2 tester completes T1.1 and every finding from
  both rounds is fixed or deferred.

## Where the timeline is kept (approved with the design)

Option 1: a separate text file next to the saves. Rejected alternatives:

- Inside each save. This would change the save format, make saves grow, and
  lose the history when a tester starts a new career.
- Godot's own engine log. It is noisy and contains file paths, which breaks the
  problem report's privacy allowlist.

## Design

### Part 1: session timeline log

A small engine-free class in `src/MangakaSim` writes `timeline.log` in the
game's user folder. The Godot side calls it from the places where the events
already happen. It does not touch the simulation or the save format, so play
stays deterministic and saves are unchanged.

One line per event: real local time, minutes into the session, game date and
the event. For example:

```
2026-10-02 19:41:07 | +12 | 1996-04-08 | step first-sale
```

Recorded:

- session start and end (a session with no end line points to a crash or a
  forced close),
- window size, text size and theme at session start,
- new career, with difficulty and whether Sandbox is on,
- each new Helper-Chan step, by its ID,
- journey milestones from the game's own events: first doujin completed, first
  sale, later doujin releases that sell, first pitch, pitch answer, serialization
  accepted, first magazine chapter, first deadline missed, cancellation warning,
  cancellation, first hire, missed payday,
- speed changes, including pause,
- screens opened and phone opens,
- saves, loads and imports,
- error messages shown to the player.

Never recorded: names, typed text, file paths or hardware identifiers. A career
appears as a short code (the first 6 characters of its identifier). Error text
is logged with file paths removed and any name or title from the current career
replaced by a placeholder.

The file is capped at about 1 MB. When it is full it becomes a single older file
(`timeline.old.log`) and a new one starts, so at most about 2 MB are kept.

"Report a problem" gets a third checkbox, "Attach session timeline", ticked by
default. The preview lists it with the other contents. When ticked, both files
go into the report ZIP as readable text, so a tester can open and check them.
Nothing is uploaded; the tester decides how to send the ZIP.

Unit tests cover the line format, the size cap and roll-over, the removal of
paths and names, and the report contents with the box ticked and unticked.

### Part 2: alpha.12 package

- `ProblemReport.Build` becomes `0.8.0-private-alpha.12`.
- The package adds `READ ME FIRST.txt` (the brief), `Questionnaire.txt` and the
  practice career file.
- A build verification record, `alpha-12-build-verification.md`, follows the
  earlier ones.

### Part 3: practice save

The automated playtest makes the practice career from seed 0 on Standard and
stops about two in-game weeks before the April 1997 cancellation warning. It is
exported as a portable career named "Practice: a struggling series". The file
name drops the colon, which Windows does not allow:
`Practice - a struggling series.mangaka`.

A test loads the file and plays on, and checks that the warning arrives. Testers
open it with the existing "Import career or previous save" button, so no new
screen is needed. The practice career keeps a fixed identifier, so its short
code is recognisable in a timeline.

### Part 4: my walk-through before any tester plays

A new rendered check, `--journey-smoke`, drives the real screens through every
T1.1 step on Standard, reaching the setback through the practice career. At
each step it saves, loads and confirms the career continues identically. It also loads an alpha.11 career and the oldest save
fixture (`tests/MangakaSim.Tests/Fixtures/v1-minimal.json`); each must load, or
be refused with a clear message. If no alpha.11 career file exists yet, one is
made with the alpha.11 package.

I review the captures (AVIF, in the usual `TestResults` folder). This is
evidence for T1.6, described as an automated check plus my visual review, not
human play. Anything it finds is fixed before alpha.12 is packaged.

### Part 5: tester kit and rounds

Testers are people who have not played the game or watched it being played. The
user recruits them and sends the builds; I never contact testers or share
builds. Testers need a Windows PC, laptop or desktop.

The brief (`READ ME FIRST.txt`, one page) covers:

- what the game is, in one paragraph,
- what we ask: start a new career on Standard, without Sandbox, play until the
  first hire, then keep going as long as you enjoy it, up to three in-game years,
- that about 1 to 1.5 hours reaches the first hire, in as many sittings as you
  like,
- the one rule: do not ask anyone or look anything up; if you do, just say so
  in the questionnaire,
- that giving up is fine and still useful: send the report anyway,
- if nothing went wrong by the end, import the practice career and play until
  Helper-Chan warns about cancellation,
- how to start the game, including the Windows SmartScreen prompt for an
  unsigned build,
- how to send the report: main menu, "Report a problem", keep "Attach session
  timeline" and "Attach career" ticked, export, send the ZIP and the
  questionnaire to the user,
- what the report contains, and that nothing is uploaded,
- known gaps: no music yet.

The questionnaire (`Questionnaire.txt`, about 12 questions, answered in the
file and returned with the report):

1. Did anyone help you, or did you look anything up? What about?
2. Where were you stuck or confused, and how did you get past it?
3. How did the pace feel at the start, while waiting for your series to debut,
   and later on?
4. Did the 32x speed help, and did it stop at the right moments?
5. Did money changes make sense? Was anything a surprise?
6. What went wrong for your series (rejection, warning or cancellation)? Did it
   feel fair, and did you know what to do next?
7. Did saving and loading work?
8. Did the game crash or show an error?
9. Would you keep playing? (1 to 5) Why?
10. What is the one thing you would change?
11. What screen size did you play on, and was it a laptop or a desktop?
12. Anything else?

An online form is the user's option; the text file is the default.

Returned reports are kept out of git, in `TestResults/fresh-player/`. The
findings record calls testers A, B and C; no names or contact details go into
the repository.

After round 1 I write `docs/superpowers/fresh-player-test-findings.md`:

- real time to each T1.1 step, from the timelines,
- pauses of more than 5 minutes, and where they happened,
- the share of time spent at each speed,
- questionnaire answers, summarised,
- each finding with a proposed triage (Part 6).

The user approves the triage. Fixes become Tier 1 fix 8 onward, each with its
own completion record. Then alpha.13 goes to round 2, and the findings record
gets a round 2 section.

### Part 6: triage and ticking

Always fixed:

- crashes,
- lost or unloadable saves,
- dead ends,
- anything that needed help or a lookup,
- misleading money, deadline or status information.

Fixed if cheap or seen by more than one tester:

- confusion testers worked out themselves,
- stretches of more than about 10 real minutes with nothing to do.

Deferred to Tier 2, each with a written reason: preferences, feature or content
requests, and balance beyond three in-game years.

A tester completes T1.1 when:

- their timeline shows each step: first doujin, first sale, a later release
  that sells (the growing readership), a pitch, a serialization with its first
  magazine chapter, and a first hire,
- they had a setback, on their own career or on the practice save,
- their questionnaire reports no help and no lookups.

If a round 2 tester hits an "always fixed" problem, it is fixed and a further
new tester plays. The same applies if a round 2 tester stops for reasons
outside the game before finishing T1.1.

The test also gives evidence for other items. Each is ticked only with linked
evidence, and none is ticked from this test alone without the user's judgement:

- T1.2: the questionnaire's help answer, and each step reached after the
  matching Helper-Chan text.
- T1.3 and T1.4: the money and confusion answers, and the returned careers.
- T1.5: time to first sale from the timelines (target 15 to 20 minutes). The
  three-year time is not ticked from an extrapolation without the user's
  judgement.
- T1.6: the journey check (Part 4), plus the testers' save and load lines.
- T1.7: sessions without an end line, and the crash answers.

## Routine decisions (made without a question)

- **Growing readership.** Counted as a second doujin release or print run that
  sells after the first sale, which is where Helper-Chan's "continue as a
  series" step leads.
- **Window details.** Window size, text size and theme are logged at session
  start so display problems can be matched to a screen size without relying on
  memory. None of these identify a person or a machine.
- **Old timeline files.** Only the current and one older file are kept and
  attached. Older history is not needed for a test of a few hours.

## Out of scope

- No simulation, balance or save format change in this preparation.
- No crash handler. Catching unexpected errors gracefully stays with T1.7; the
  missing end line only hints at a crash.
- No upload, telemetry or online service. The tester sends the ZIP themselves.
- No Steam playtest branch; there is no Steam app ID yet.

## Approvals needed along the way

Implementation follows the approved plan. Each of these still needs the user's
go-ahead at the time:

- building, running the journey check and packaging alpha.12,
- recruiting testers and sending them alpha.12 (the user's task, asked for when
  that step arrives),
- the round 1 triage,
- building and packaging alpha.13, and sending it to the round 2 tester.
