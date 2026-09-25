# Sub-project 4 delivery record

Implemented and verified locally on 24 September 2026. No commit or push was
requested. Prior Sub-project 3 working-tree changes were retained.

## Delivered

The **Office** tab renders the simulation's actual workplace, inventory, staff
and last resolved activities. The starting parents' house has zero rent, two
family desk/chair pairs, shared domestic fittings and two ambient residents.
The sixteen Tokyo catalog properties vary by floor area and capacity (4, 8,
16 or 32 desks); employers and later returns home use the same logical rules.
Shared commercial areas have up to six unrelated occupants. Closed private
rooms and WC doors are decorative, not another management system.

The overhead camera rotates, zooms, pans, resets and focuses the protagonist.
Staff can be selected for matching portraits, activity and needs. Quiet alerts
jump to the relevant workplace. Only the selected office is rendered, while
all businesses continue in the simulation.

Furniture is owned separately by the business, family or landlord. The paused
editor supports placement, quarter-turn rotation (R), paired chairs, locks,
storage, resale, manual desk assignment, catalog purchases, optional basic
packages, Auto-arrange and explicit Apply/Discard. Access validation reserves
0.75 m approaches, entry circulation and stock space. Fixed property capacity
is still the ceiling; a desk is usable only with its paired accessible chair.

Auto-arrange evaluates two deterministic bounded trials (up to 2,000 placement
attempts), respecting locks and ownership. Existing team order informs desk
placement, then candidates are scored for workspaces, spending, storage,
adjacency, unnecessary changes and circulation distance. It uses no remote
service or gameplay randomness. Unplaced owned furniture remains in storage.

Moving through either management or Office opens a combined lease/move/furniture
quote. The typed relocation command validates the entire proposed result on a
copy before applying it once. Empty branch leases remain available; they need
furniture before accepting staff. Family and landlord fixtures remain at their
site. Business furniture follows ordinary business moves but remains with its
business when the protagonist changes career. Existing stock, title rights,
relocation downtime, personal funds, wage/bill reserves and gross employer
spending limits are retained.

Original reusable 3D furniture, work props, room fittings and modular characters
are generated from local source. Walking and seated work animate at the selected
1x/2x/4x/8x speed. Full timelapse now blends closely overlapping, depth-tested exposure samples
along the walking path; Reduced draws fewer samples and Off none. The original
four-ghost implementation was replaced after visual feedback (see below). Pause, location changes, loads
and time jumps clear history. Rooms and interface stay sharp. Walking never
adds a production penalty. Staff use resolved break-seat reservations; ambient
people have no payroll, needs, facility reservations or selectable staff IDs.

Furniture benefits are deliberately modest: support chairs reduce productive
hour comfort loss by 0.5; upgraded break sets add 3 comfort to provisioned break
recovery; distinct plant/print decor categories each add one atmosphere point
(maximum two). Shelves do not increase book inventory and better desks do not
increase productivity. Appearance uses independent stable visual recipes and
can be customised for the starting protagonist before time advances.

## Save compatibility

New saves are version 4 (`debug-v4.json`). Required office collections, property
metadata, ownership, placement, assignments, appearance and observations are
validated. Camera bookmarks, selection and timelapse preference are saved in a
separate `.office-view.json` companion, outside deterministic gameplay state.

**Import previous save** explicitly reads `debug-v3.json` in Godot's user-data
folder. Only the completed Sub-project 3 schema is supported. Import preserves
cash, financial/title history, IDs and gameplay RNGs. Existing implicit desks
and break upgrades become furniture with zero paid value (no resale windfall).
Closed historical sites do not receive speculative inventory, and family-home
records share one physical furnishing set. The original file is never written;
the user presses Save to write the converted version-4 checkpoint.

Incomplete earlier v3, v1/v2, unknown custom properties and legacy capacities
that cannot be furnished are rejected clearly. Old commands remain history;
replay after import starts from the converted checkpoint. Fresh version-4
command replay is verified from its original seed.

## Verification

- Baseline before office work: 332 simulation tests.
- Final full simulation suite: **381 passed, 0 failed, 0 skipped**.
- Solution build with warnings treated as errors: **0 warnings, 0 errors**.
- Godot 4.7.2 .NET import: successful.
- Headless complete scene walkthrough: **120 checks passed**.
- Rendered complete scene walkthrough with captures: **182 checks passed**,
  exit code 0 and empty engine error log.
- Whitespace diff check: no errors (existing Git line-ending notices only).

Office coverage includes all sixteen full-capacity property layouts, rotation,
clearance, locks/determinism, stale/invalid previews, atomic relocation,
caller-list isolation, wage reserves, employer limits, resale, usable-desk
hiring, career asset retention, appearance RNG independence, resolved break
versus work activity, malformed saves, required legacy fields, conversion of
closed/home-return/break-upgrade history, and command replay. Existing staff,
career, branch, publishing and financial suites continue to pass.

Actual scene-control checks exercise furnishing pause ownership, invalid drafts,
cancel purity, purchases, management-to-office move preview, full furnishing,
break sets, returning home, save/load, view preferences, all four speeds and
pause trail clearing. A funded 32-person rendering fixture isolates visual
scale from years of economic progression. The same next simulation hour is
compared with Office shown, hidden and processing disabled; states agree.

Inspected local captures under ignored `TestResults/`:

- `office-family.png`, `office-editor.png`, `office-small-studio.png`.
- `office-large.png` (1600 × 900) and `office-1280.png` (1280 × 720).
- `office-motion-000.png` through `office-motion-044.png`: 45 rendered frames
  at 30 visual FPS; also assembled as `office-timelapse.gif` (1.5 seconds).
- `office-render.log` and `office-render-error.log`.

Camera clipping, portrait overflow, hidden Apply/Discard controls and seated
limb direction were corrected during rendered inspection. The sequence is
captured from the real Godot scene, not a mockup or generated concept image.

### Local frame-time sample

Windows, NVIDIA GeForce RTX 5070, Godot 4.7.2 .NET, Vulkan Forward+, 2× MSAA,
1600 × 900, 32 working staff + six ambient people, 8x speed. Full measured
150 frames after warmup; Reduced measured 120. Full includes three still-capture
frames. The later 45-frame sequence is outside the timed sample.

| Effect | Median frame time | 95th percentile |
|---|---:|---:|
| Full | 6.59 ms | 8.24 ms |
| Reduced | 6.36 ms | 7.98 ms |

This meets the approximate 60 FPS target on this development machine. These
short local samples are not a minimum-spec, integrated-GPU, long-session or
exported-build claim. Allocation is bounded to the displayed office and fixed per-actor pools; broader hardware profiling remains future release work.

## Implementation and remaining scope

Core files are `Office.cs`, `GameState.Office.cs`,
`GameState.OfficeValidation.cs`, `Rules/OfficeLayoutRules.cs` and
`Rules/OfficeAutoArrange.cs`, integrated with the existing simulation steps.
Rendering responsibilities are consolidated in `OfficeView.cs`, `OfficeArt.cs`
and `OfficeActor.cs`; `DebugMain.Office.cs` supplies the player controls.
Tests are consolidated in `OfficeTests.cs` and `DebugMain.OfficeSmoke.cs`.
The property room is built in code rather than a separate scene file for each
variant; the independent prototype scene is retained for motion experiments.

The surrounding management UI remains a debug harness. Art is an original
usable stylised first pass; character animation, historical room dressing and
commercial UI polish can improve further. Household/building occupants now travel between explicit doorways and wait
out of sight (see the doorway correction below); private room interiors and
interactions are outside scope. Furniture prices, floor areas and benefit
magnitudes are game estimates, not verified 1996 commercial listings. Existing
primary Japanese research, dated comparisons and evidence gaps remain in the
[research ledger](specs/2026-09-23-3d-office-research.md). Asset provenance and
metre/grid conventions are in [ASSETS.md](../../godot/Office/ASSETS.md).

Sub-project 5, historical timeline and rivals, has not been started.


## Hyperlapse correction — 24 September 2026

User feedback identified that the first four faint ghosts still read as fast
forward. Replaced them with a continuous long-exposure effect: spatially dense
rounded silhouettes blend with soft edges and age/distance falloff. Sampling
follows actual route segments, including corners within a single rendered
frame. The current moving body becomes translucent, without dithered mesh
transparency or a sharp moving shadow dominating the exposure. Stopped people
become sharp again as their history fades. Pause and Off clear it immediately.
Furniture, UI, selection and all simulation behaviour remain unchanged.

Full uses a fixed 96-instance pool, 0.075 m spacing, a 4.5 m distance cap and
0.10/0.14/0.18 second shutters at 2x/4x/8x. Reduced draws every third sample at
lighter total intensity. Drawing hands use a separate bounded 20-instance pool
(six in Reduced). Both pools use one instanced draw each instead of creating a
node per exposure sample. Material depth testing preserves room occlusion.

Validation for this visual correction: warning-as-error build with zero
warnings/errors, **130 headless checks** and **192 rendered checks**, exit 0,
empty shader/engine error logs. Ten new assertions cover 30 versus 144 FPS,
speed-dependent streak length, bounded sampling, softened bodies, Reduced,
Off, stop fading and pause. The existing scene checks still verify identical
simulation outcomes with rendering shown, hidden or disabled. No simulation
source was changed for this correction; the earlier 381-test domain result
remains the baseline.

Latest same-hardware 32-staff/6-ambient measurement: Full median **8.69 ms**,
p95 **12.22 ms**; Reduced median **6.36 ms**, p95 **8.03 ms**. The 60 FPS local
target is still met. These replace the initial four-ghost performance numbers
for the current effect; they do not establish performance on other hardware.

The actual revised frames were inspected in the motion prototype and full
office. `TestResults/office-hyperlapse.gif` contains the new 45-frame sequence;
`hyperlapse-render.log` and `hyperlapse-render-error.log` contain the run record.


## Ambient doorway correction — 24 September 2026

The initial back-and-forth ambient movement was a placeholder. It used arbitrary
endpoints and visible pauses, which could read as people appearing from walls.
Replaced it with `OfficeAmbientTraffic`: occupants start hidden, emerge inside
an explicit doorway, follow the shared corridor and leave through the other
door. Door frames, open leaves and thresholds survive wall cutaways, with actual
openings in the wall behind them. The single-sided recess is visible from inside
without becoming an opaque box from the dollhouse camera.

Arrival hides both actor and exposure during the same update. No idle frame,
visible destination wait, turnaround or stale trail remains. Delays between
trips happen entirely behind the doors. Nighttime, time jumps, location changes
and loads reset circulation; pausing freezes visible trips. Props were moved
out of the lane and approaches. Family and commercial variants share these
rules, using appropriate Private/Entry and 201/Stairs labels.

Verified the family home plus all 16 catalog floor plans at each of 1x/2x/4x/8x,
including threshold-only appearances, route bounds and wall crossings, immediate
exit disappearance, hidden waits, opposite-door return, pause, off-hours, and
slow frames that complete an entire trip. Warning-as-error build passes;
**742 headless checks** and **804 rendered checks** pass. The rendered family
home and large office were inspected, with captures at both supported window
sizes. The simulation is untouched and existing visual-independence checks pass.
Logs: `TestResults/office-doors.log` and `office-doors-error.log`; moving preview:
`TestResults/office-door-transitions.gif`.
