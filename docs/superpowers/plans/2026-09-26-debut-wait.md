# Tier 1 fix 5: the wait before a series debuts, implementation plan

Date: 2026-09-26. Decisions: [considerations](../specs/2026-09-26-debut-wait-considerations.md)
(Q24, Q25). Status: implemented 2026-09-26; see the
[completion record](../debut-wait-completion.md).

## Goal

Between accepting a serialization and its first issue, the player knows when the
series debuts, why chapters are drawn ahead, how many are ready, and what useful
side work to do once they are.

## Tasks

1. **Ready-ahead count (simulation, read-only).** Add a helper on `GameState`
   that returns chapters ready ahead and the target for a serialized series:
   complete, unpublished, not one-shot, not doujin chapters, against
   `Math.Max(1, series.BufferLimit)`, matching the planner's rule in
   `GameState.Planner.cs`. Unit tests: 0 of 2 at acceptance, 2 of 2 when the
   planner stops, the count follows a changed buffer limit.

2. **Guidance before the debut (`src/MangakaSim/Guidance.cs`).**
   - Rewrite the `first-deadline` step while stock is not ready: debut date and
     magazine, "x of y chapters ready", why editors want them early (missed-issue
     protection) and that page fees start after publication.
   - New step `debut-wait` once stock is ready, choosing one suggestion by the
     Q25 order: early hire (runway safe; desk card if no free desk), convention
     (unsold doujin stock; next major event if before the debut, else the next
     regional one), short doujin or one-shot (no side doujin in progress),
     part-time job. A second text lists the other options, and a third mentions
     drawing up to 4 ahead in Production.
   - Step id includes the chosen suggestion (for example `debut-wait-hire`) so the
     phone sends a new text when the situation changes.
   - Keep each text within `CareerGuidance.TextLimit` (140 characters).
   - Unit tests for each branch, the text limit, and that the card ends when the
     first chapter publishes.

3. **Series status in Production (`godot/DebugMain.SeriesStatus.cs`).** For a
   serialized series, add "Chapters ready ahead: x of y" to the status line,
   both before and after the debut.

4. **Playtest harness (`tests/MangakaSim.Tests/CareerPlaytest.cs`).** Record the
   date the pre-debut stock is ready as a milestone, and have the bot act on the
   `debut-wait` suggestion the way a player would (for example start the
   one-shot or book the convention). Re-run seeds 0 and 7 on Standard and
   compare idle days and the business balance at the debut with the fix 4
   reports.

5. **Smoke check.** Extend the management smoke to reach a pre-debut
   serialization and assert the ready line and the `debut-wait` text appear.

6. **Records.** Completion record `docs/superpowers/debut-wait-completion.md`,
   a re-run section in the playtest findings, the roadmap and `CLAUDE.md`
   section 5.

## Verification (only with authorization)

- `dotnet build MangakaGame.sln -warnaserror`
- `dotnet test tests/MangakaSim.Tests --filter "Category!=Playtest"`
- `dotnet test tests/MangakaSim.Tests --filter Category=Playtest`
- Godot import, `--smoke-test`, `--management-smoke`, and rendered AVIF captures
  of the phone and series status at 1920x1080 and 1280x720 with 150% text.
- No packaging.
