# Session handoff

Last updated: 2026-10-04. Read this after `CLAUDE.md` when a session starts or
resumes after compaction. It records work in progress that is not yet in the
roadmap or a completion record. Replace its contents when the task changes.

## Current state (2026-10-04)

- The work from 29 Sep to 4 Oct is committed on `main` in feature commits on
  top of `274741a`: career goals board, progressive disclosure (A3), work
  feedback with right-click back and shorter music gaps, studio island,
  streaming sales and the selling tutorial, display settings (C2, C3), and the
  NovelAI commercial terms research. Records: `career-goals-completion.md`,
  `progressive-disclosure-completion.md`,
  `work-feedback-and-right-click-completion.md`, `studio-island-completion.md`,
  `streaming-sales-completion.md`, `display-settings-completion.md`.
- These were split by file, not by hunk, so a file edited by two features sits
  in the later commit and the middle commits may not build on their own.
- Still uncommitted, waiting for the user's go-ahead to build and test:
  - **Quick start for the opening (Q65 option 1, tester C's C1).** Shorter
    setup screen, a simpler New Career form and one Helper-Chan text on day
    one. Record `quick-start-completion.md`. Its files also carry the last of
    the display settings work (`godot/DebugMain.Display.cs`,
    `DebugMain.DisplaySmoke.cs`, `DebugMain.Startup.cs`,
    `DebugMain.StartupSmoke.cs`, `DebugMain.ManagementMenus.cs`,
    `DebugMain.Audio.cs`, `display-settings-completion.md`) and older changes
    to `fresh-player-test-findings.md`, so commit them together. Until then
    `main` does not build, because `DebugMain.Management.cs` already calls
    the display code.
  - **Tier 1 closeout.** T1.1, T1.3, T1.4 and T1.8 ticked with evidence in the
    roadmap; T1.10 moved to Tier 2 as the stranger playtest; T1.2 and T1.5 wait
    for the user's sign-off (T1.5 also for the quick start). A6 is explained on
    the licence offer card (`godot/DebugMain.Progression.cs`, presentation only,
    not built); A7 closed with no new work because A1 caused it. Record
    `tier1-closeout-completion.md`.
- Last full verification on this computer was for streaming sales (768 xUnit,
  all 23 Godot checks, display sweep 560 / 0 flagged) and display settings
  (display sweep 616 / 0 flagged). Nothing after that has been built.
- Next: the user's go-ahead to build and test the quick start and A6, then
  their sign-off on T1.2 and T1.5 to finish Tier 1. A new tester build
  (alpha.13) also needs their go-ahead.
- NovelAI terms are researched
  (`specs/2026-10-04-novelai-commercial-terms-research.md`); read it before
  any store use of NovelAI art.
- Deferred minors: see the title screen and restyle completion records.
- Workspaces kept: `.superpowers/sdd/2026-09-28-title-screen-and-pause-menu/`,
  `.superpowers/sdd/2026-09-29-brand-ui-restyle/` (ledgers, rulings, run logs,
  `smoke.ps1` runner with timeouts).

## Working habits

- Several threads can share this checkout. Before committing, check which
  files another thread is still editing and leave those out.
- Delegate broad file reading to Explore agents and keep only their summaries.
- Run Godot through `.superpowers/sdd/2026-09-29-brand-ui-restyle/smoke.ps1`
  (Start-Process with a timeout) so a hung run cannot block the session.
