# Tier 1: display sweep (T1.9), implementation plan

Date: 2026-09-27. Decisions: [considerations](../specs/2026-09-27-display-sweep-considerations.md)
(Q29, Q30). Status: implemented 2026-09-27; see the
[completion record](../display-sweep-completion.md).

## Goal

Every core screen is fully usable at 16:9, 16:10 and 21:9, at 100% and 150%
text, in dark and light themes, with nothing clipped, overlapping or out of
reach, and there is an automated check that keeps it that way.

## Tasks

1. **Layout checker (`godot/DisplaySweep.cs`, new).** A small helper that walks
   the visible control tree and reports:
   - controls whose global rectangle leaves the window,
   - visible siblings in the same container whose rectangles overlap,
   - labels and buttons whose text needs more width or height than they have
     and that do not wrap, clip on purpose or sit in a scroll container.
   Known intentional overlays (phone, Helper-Chan card, pop-ups, speed flash,
   money badges, nametags) are excluded by node, not by position.

2. **Sweep smoke (`godot/DebugMain.DisplaySweepSmoke.cs`, new, flag
   `--display-sweep-smoke`).** Builds one fixture career that has something on
   every core screen (a finished doujin with stock, a serialized series in its
   debut wait, a convention booking, one hire, unread inbox items and a
   decision card). For each of the 5 sizes, 2 text scales and 2 themes it opens
   each core screen, runs the checker and records the result. It captures AVIF
   screenshots for the review set in the considerations (about 40 images) and
   for every flagged combination. The smoke fails if the checker flags anything
   on a core screen.

3. **Layout fixes.** Fix whatever the sweep finds, in the layout code
   (`DebugMain.Management*.cs`, `DebugMain.Gui*.cs`, `HelperPhone`, dialogs):
   wrapping, scroll containers, compact variants and minimum sizes. No new
   features.

4. **Non-core screens.** One capture each at 1280x720 with 150% text; list any
   problems in the completion record, fixing only cheap ones.

5. **Records.** Completion record `docs/superpowers/display-sweep-completion.md`
   with the flagged list before and after, the reviewed captures and what I saw
   in them; tick T1.9 in the roadmap with links to the evidence; update
   `CLAUDE.md` section 5.

## Verification (only with authorization)

- `dotnet build MangakaGame.sln -warnaserror`
- `dotnet test tests/MangakaSim.Tests --filter "Category!=Playtest"`
- Godot import, `--smoke-test`, `--management-smoke`, the new
  `--display-sweep-smoke --capture`, and the existing smokes whose screens the
  fixes touch.
- My own visual review of the captures, reported separately from the
  automated result.
- No packaging.
