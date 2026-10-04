# Studio island and studio corner, completion

Date: 2026-10-03. Bounded change (Q59, option 1: studio island), plus the
user's follow-up to fill the freed alcove. Not yet committed.

## What changed

- **Studio island in the parents' home.** Helper-Chan's desk leaves her alcove
  and joins the room, facing the spare desk across a desk island; Aki's desk
  heads the island and looks down it, like a real manga studio. Helper-Chan
  faces into the room, so she is visible from the default camera.
  - `StudioLocation.StudioIsland` (optional in saves) turns on
    `FloorPlanDefinition.Island`: Helper-Chan's desk and chair cells count as
    protected for furniture and as solid for walking routes, and the two desk
    slots carry rotations (Aki at (3,7) facing the island, the spare desk at
    (5,2) a quarter turn round). `src/MangakaSim/Office.cs`,
    `Rules/OfficeLayoutRules.cs`, `Rules/OfficeAutoArrange.cs`.
  - New careers use it; Auto-arrange keeps it.
  - The old misalignment is gone: the second desk no longer falls back to a
    spot 25 cm out of line because its slot clashed with the stock corner.
  - **Older saves** (`GameState.StudioIsland.cs`): a parents' home still in
    the exact old default arrangement moves to the island on load (the replay
    checkpoint is rebased, as other upgrades do). A home the player rearranged
    keeps its layout, and Helper-Chan keeps her alcove there.
  - Godot draws her island desk with the grid furniture art, seats her behind
    it and turns her to face the spare desk (`HelperChan.DeskFacing`).
- **Studio corner** (`godot/Office/OfficeView.StudioCorner.cs`, decoration only):
  the freed alcove holds a reference bookcase of manga volumes, a plan chest
  for manuscript pages with paper, ink bottles, pens and a ruler, a cork board
  of Name storyboards, a glowing light table with lamp, a screentone rack,
  boxes of doujin stock (one open), a stack of weekly magazines and a plant.

## Verification

- Unit tests: 741 pass, including 5 new `StudioIslandTests` written to fail
  first (new career seating, kept-clear cells and routes, Auto-arrange, an
  untouched older home moving over, a rearranged older home kept).
- Godot: `--work-feedback-smoke` gained a studio island check (Helper-Chan
  seated at her island seat facing the spare desk; 22 checks with captures).
  All 22 Godot checks passed on the island before the corner; the display
  sweep had 560 screen checks, 0 flagged. Office life and atmosphere report
  fewer checks (730 and 877) because those checks repeat per frame while
  people leave for the night, and Helper-Chan's walk out is now shorter.
  After the corner: family home, office life, atmosphere, display sweep (560,
  0 flagged), main and management checks pass again; warning-free build.
- Rendered review at 1920 x 1080: Helper-Chan visible at her island desk,
  Aki at the head, the spare desk and chair opposite her; the corner's rack,
  light table, plan chest, boxes, magazines and plant visible. The bookcase
  and cork board sit under the header in the default view and show when the
  camera is rotated or panned.

## Not verified

- An older save in the wild that was rearranged then put back exactly to the
  old default: it would move to the island (intended).
- How the corner looks at 21:9 and with the camera rotated (only the default
  1920 x 1080 view was reviewed).
