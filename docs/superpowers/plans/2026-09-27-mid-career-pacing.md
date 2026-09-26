# Tier 1: mid-career pacing, implementation plan

Date: 2026-09-27. Decisions: [considerations](../specs/2026-09-27-mid-career-pacing-considerations.md)
(Q26 to Q28). Status: implemented 2026-09-27; see the
[completion record](../mid-career-pacing-completion.md).

## Goal

Routine working days in the middle of a career can pass at about 11 seconds
each without clicks, while anything that needs the player still stops the game
at their usual speed.

## Tasks

1. **Speed list (`godot/DebugMain.Management.cs`, `DebugMain.Usability.cs`,
   `DebugMain.SpeedFeedback.cs`).** Add 32 to the header buttons ("32×") and
   to the 1 and 2 key steps. Remember the last daytime speed below 32 as the
   speed to drop back to. Speed feedback shows "▶▶ 32×" with a tooltip that
   says routine days skip ahead and Helper-Chan stops you when needed. The
   overnight behaviour of the buttons and keys is unchanged.

2. **Fast-day stops (`godot/DebugMain.cs` `ScanEvents`).** While the day runs
   at 32x:
   - `DailyRecap` and `ChapterCompleted` do not pause or open the recap
     dialog; they stay in the inbox, and the day end starts the overnight
     skip directly, which then resumes at 32x.
   - The stop list in Q27 (industry decisions, offers, rejections, redo
     requests, cancellation warnings and cancellations, missed deadlines and
     issues, chapters at risk, missed paydays, new Helper-Chan texts) pauses
     as today and sets the resume speed to the remembered slower speed.
   - Other events follow the existing Auto-pause setting.
   - At 8x and below nothing changes.

3. **Office playback.** Cap `OfficePlaybackSpeed` at 8 during 32x days, as
   during the overnight skip.

4. **Helper-Chan's introduction (`src/MangakaSim/Guidance.cs`).** A new
   one-time step `quiet-speed` shown after the first full working day with no
   stop event and no other pending guidance, once the career has reached its
   first sale, so it does not interrupt the opening. The text stays within
   `CareerGuidance.TextLimit`. It is marked complete when sent or when the
   player first chooses 32x. Unit tests: appears after a quiet day, not
   before the first sale, not twice, read-only.

5. **Smoke checks.** Update `AtmosphereSmoke` (32x daytime pace about
   11 seconds per working day, office capped at 8x), `ConvenienceSmoke`
   (button and keys reach 32x and step back) and `SmokeTest` speed checks.
   Add a fast-day check: at 32x a day ends without the recap dialog and the
   next morning continues at 32x; a staged serialization offer or cancellation
   warning drops the game to the remembered speed.

6. **Pacing measurement (`tests/MangakaSim.Tests/CareerPlaytest.cs`).** Report
   real time per in-game year at 8x and at 32x from the stop events the bot
   meets, and the number of stops per year at 32x. Re-run seeds 0 and 7 on
   Standard.

7. **Records.** Completion record `docs/superpowers/mid-career-pacing-completion.md`,
   a re-run section in the playtest findings, the roadmap and `CLAUDE.md`
   section 5.

## Verification (only with authorization)

- `dotnet build MangakaGame.sln -warnaserror`
- `dotnet test tests/MangakaSim.Tests --filter "Category!=Playtest"`
- `dotnet test tests/MangakaSim.Tests --filter Category=Playtest`
- Godot import, `--smoke-test`, `--management-smoke`, and rendered AVIF
  captures of the header at 32x and the Helper-Chan text at 1920x1080 and
  1280x720 with 150% text.
- No packaging.
