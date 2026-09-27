# Session handoff

Last updated: 2026-09-27. Read this after `CLAUDE.md` when a session starts or
resumes after compaction. It records work in progress that is not yet in the
roadmap or a completion record. Replace its contents when the task changes.

## Current task: T1.10 preparation done, alpha.12 packaged; waiting on testers

- Ledger (rulings, run logs): `.superpowers/sdd/2026-09-27-fresh-player-test/`,
  git-ignored scratch that can be deleted once no longer useful.
- Package: `builds/MangakaStudio-0.8.0-private-alpha.12-Windows.zip`, see
  `docs/superpowers/alpha-12-build-verification.md`. START-HERE.md is left out
  by the user's decision (the guide counts as outside help).
- Committed on `main` as `5ceb97a` (not pushed). Next: the user recruits two fresh
  testers and sends alpha.12 (never contact testers or share builds myself).
  Returned reports go in `TestResults/fresh-player/`; results in
  `docs/superpowers/fresh-player-test-findings.md`.

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
