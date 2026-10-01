# Session handoff

Last updated: 2026-10-01. Read this after `CLAUDE.md` when a session starts or
resumes after compaction. It records work in progress that is not yet in the
roadmap or a completion record. Replace its contents when the task changes.

## Current state (2026-09-29)

- Last commit `3fd30d5`. About 90 files are uncommitted, all verified on this
  computer (678 xUnit, all 18 Godot checks, display sweep 520 / 0 flagged):
  - start-up disclaimer and volume (`startup-flow-and-volume-completion.md`);
  - 13 Suno music tracks and the music credits;
  - title screen and pause menu, rename to Mangaka Days, NovelAI logo and title
    art (`title-screen-and-pause-menu-completion.md`);
  - in-game UI restyle around the logo (`brand-ui-restyle-completion.md`);
  - night skip on days with no work (`quiet-day-night-skip-fix.md`);
  - tester B fixes B1, B4, B5, B7 (`tester-b-fixes-completion.md`).
- Commit only when the user asks. Push only when asked.
- Tester B returned 2026-10-01 (findings B1 to B8, triage approved). Next:
  the career goals board, spec `specs/2026-10-01-career-goals-design.md`
  awaiting the user's review (notes `.superpowers/sdd/goals/notes.md`), then A3
  progressive disclosure (seen by both testers). A6, A7 not confirmed by B.
  A new tester build (alpha.13) needs the user's go-ahead.
- Promised before any store use of NovelAI art: check NovelAI's commercial
  terms.
- Deferred minors: see the title screen and restyle completion records.
- Workspaces kept until the user commits: `.superpowers/sdd/2026-09-28-title-screen-and-pause-menu/`,
  `.superpowers/sdd/2026-09-29-brand-ui-restyle/` (ledgers, rulings, run logs,
  `smoke.ps1` runner with timeouts).

## Working habits

- Delegate broad file reading to Explore agents and keep only their summaries.
- Run Godot through `.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1`
  (Start-Process with a timeout) so a hung run cannot block the session.
