# Session handoff

Last updated: 2026-09-27. Read this after `CLAUDE.md` when a session starts or
resumes after compaction. It records work in progress that is not yet in the
roadmap or a completion record. Replace its contents when the task changes.

## Current task: music system (plan executing, Native), after Tier 1 closeout

- Music ledger: `.superpowers/sdd/2026-09-28-music-system/progress.md`
  (plan `docs/superpowers/plans/2026-09-28-music-system.md`, spec
  `docs/superpowers/specs/2026-09-28-music-system-design.md`).
- Tier 1 closeout and tester A fixes are done but not committed; notes in
  `.superpowers/sdd/tier1-closeout/notes.md`; record
  `docs/superpowers/tier1-closeout-and-tester-a-completion.md`. T1.6 and T1.7
  ticked by the user. A3, A6, A7 wait for tester B.
- Build, test and Godot checks approved; packaging needs a separate go-ahead.
  Nothing committed since `26057df`.

## Context-saving changes made this session

- Disabled the `huggingface-skills` plugin in `~/.claude/settings.json` and
  turned off the Hugging Face claude.ai connector (not needed for Tier 1 or 2).
- The design plugin is a claude.ai account plugin; only the user can turn it
  off (claude.ai or the app's plugin settings). Its servers (asana, atlassian,
  figma, intercom, linear, notion, slack) are unauthorized and not needed.
- The `autoCompactWindow` limit is gone from `~/.claude/settings.json`. After the
  restart on 2026-09-27 the session reports a 1,000,000 token window, still on
  Opus 5.5, with auto-compact at 97%. About 122k tokens were in use at restart.
- Working habit: delegate broad file reading to Explore agents and keep only
  their summaries in the main session.

## Uncommitted work in the tree

- `CLAUDE.md` and `docs/superpowers/specs/2026-09-22-roadmap.md` (T1.10 design
  and plan notes, T1.1 rewording), the T1.10 record, the T1.10 plan and this
  file are not committed yet. Commit only when the user asks.
