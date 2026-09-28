# Session handoff

Last updated: 2026-09-27. Read this after `CLAUDE.md` when a session starts or
resumes after compaction. It records work in progress that is not yet in the
roadmap or a completion record. Replace its contents when the task changes.

## Current task: waiting for tester B and for Suno tracks (2026-09-28)

- Committed `f9e8ac6`: Tier 1 closeout, tester A fixes (A1, A2, A4, A5, A8,
  A9) and the music system. Records:
  `docs/superpowers/tier1-closeout-and-tester-a-completion.md`,
  `docs/superpowers/music-system-completion.md`. Deferred minors are listed in
  those records' sessions (music: failed moment file loses the plan's place,
  back-to-back transitions cut abruptly, logger try/catch, replay edge for
  pre-fix adopted manuscripts).
- Waiting: tester B's report (then triage A3 progressive disclosure incl. rival
  job offers, A6 licence explanations, A7 setbacks feel random); the user's Suno
  tracks (then `scripts/convert-music.ps1`, import, listen together; add the
  credits line with the first track).
- alpha.12 is what tester B plays; a new tester build (alpha.13) needs a
  go-ahead. Not pushed since `26057df`... push only when asked.

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
