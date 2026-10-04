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
- Also on `main`: the opening quick start (Q65 option 1, tester C's C1;
  record `quick-start-completion.md`, commits `2bb6d66`, `f54f994`, `dfecf87`)
  and the Tier 1 closeout (`1bab533`, `4830b78`, `00aa8d3`; record
  `tier1-closeout-completion.md`). Tier 1 is complete: every item ticked, T1.2
  and T1.5 signed off by the user (Q66), T1.10 moved to Tier 2 as the stranger
  playtest. A6 is explained on the licence offer card; A7 closed with no new
  work because A1 caused it.
- Last verification (2026-10-04, after the quick start): warning-free build,
  790 xUnit tests, the affected smoke checks and the display sweep (616
  screens, 0 flagged). The A6 offer card compiled but has not been looked at:
  run `--progression-smoke --capture` at the next authorized build.
- 2026-10-05: alpha.13 packaged (`builds/MangakaDays-0.8.0-private-alpha.13-Windows.zip`,
  record `alpha-13-build-verification.md`) with the rail subtitle fix, the
  renamed tester kit and the A6 card rendered. Next: the user sends it to one
  new tester for C1 and does the "Mangaka Days" trademark search; then
  Steamworks and achievements (10 of about 25 exist), controller and Steam
  Deck, ten-year balance, sound effects, store page and Steam Playtest.
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
