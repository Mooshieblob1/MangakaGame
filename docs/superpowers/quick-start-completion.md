# Quick start for the opening, completion

Date: 2026-10-04. Decision: Q65 option 1, "Quick start", answered by the user
on 2026-10-04 in response to tester C's finding C1 ("starting the game feels
like reading an installation document"). Build and tests authorised by the
user on 2026-10-04.

## Before

On a first launch the player read the AI notice, then a setup screen with
three volume sliders, fullscreen or windowed, a window size and the interface
size, then the title, then a New Career form with the name, six appearance
choices, glasses and a Career rules tab, and finally three Helper-Chan texts
at once (the goals chapter introduction and two lines of one-shot advice).

## What changed

- **Setup screen ("Set your volume and size"):** Master volume and the
  interface size only. Music, Sound effects, fullscreen and window sizes
  are now only in Settings, at the same
  defaults as before (Music 50%, Sound effects 60%, fullscreen). A line under
  Continue says where they are. `AudioSliders(masterOnly)` and
  `DisplayControls(sizeOnly)` in `godot/DebugMain.Audio.cs` and
  `godot/DebugMain.Display.cs`.
- **New Career:** the 3D preview, the name, **Randomise look**, **Begin
  career** (focused, the main button) and Back. **Customise** opens the
  Appearance and Career rules tabs underneath; the defaults are unchanged (Aki's
  look, Standard difficulty, world seed 0, studio keeps rights, every screen
  not shown). Randomise look rolls a menu-only random number, never the
  career's own, so careers stay deterministic. `NewCareerMenu` in
  `godot/DebugMain.ManagementMenus.cs`; `MenuSections` gained a parent.
- **Helper-Chan's first day:** one text, "Let's make your first doujin!",
  with Show me and Later (`CareerGuidance.FirstText`). The one-shot advice
  moved to the Show me hint on the New doujin page, and the "New goals: Doujin
  Days!" introduction waits for the first doujin. A player who already made a
  title and has none in progress still gets the longer one-shot text.
  `src/MangakaSim/Guidance.cs`, `godot/DebugMain.Alpha.cs`.
- No save format, balance or simulation change; older careers keep the texts
  they already have.

## Checks

- `dotnet build MangakaGame.sln -warnaserror`: 0 warnings, 0 errors.
- `dotnet test` (excluding the playtest category): 790 passed, 0 failed,
  including `tests/MangakaSim.Tests/QuickStartTests.cs` (one opening text
  with a Show me target, the goals introduction arriving with the first
  doujin, no change to the career).
- Smoke checks updated: `--startup-smoke` (Master only, Music and Sound
  effects defaults kept, interface size without window options),
  `--display-smoke` (first-launch screen shows the interface size only),
  `--alpha-smoke` (one opening text), `--atmosphere-smoke` (New Career leads
  with name, Randomise look and Begin career; Randomise look leaves the career
  untouched; Customise shows the appearance choices), `--progression-smoke`
  and `--display-sweep-smoke` (open Customise before Career rules).

## Still to do

- Run the smoke checks above and look at the setup screen and New Career
  captures (results added below when run).
- Re-check C1 with a fresh player (round 2 or the stranger playtest).
- Coordinator note: CLAUDE.md section 5, the roadmap and the session handoff
  need one line each for this record; the thread committing the PC-only work
  owns those files today.
