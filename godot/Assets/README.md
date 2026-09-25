# Bundled visual assets

These files ship with the game; no runtime AI account or service is needed.
Generation prompts, source output locations and runtime paths are recorded in
`generation-manifest.json`.

## Audio

`Office/OfficeAudio.cs` generates original low room tone, pencil strokes and soft
interface effects as PCM at runtime. No external samples, music, voice recordings
or audio service are used. Playback budgets use real elapsed seconds; simulation
speed never changes pitch or increases the activity-cue rate. Ambience/effects
volumes are saved with the career and can each be muted.

## Helper-Chan

`Helper/neutral.png` is the user-supplied reference copied from
`docs/superpowers/references/helper-chan.png`, with permission to use it in the
game. The source remains intact. `happy.png`, `concerned.png` and `determined.png`
are expression edits made with the built-in image-generation tool from that
reference. Four portrait states are delivered; six had been a proposed design
default, not a user requirement. The original proportions and enormous hair
remain visible in the portrait layout.

`Helper/desk-conversation.png` is a second user-supplied illustration, copied
unchanged from `codex-clipboard-48c415b6-173b-469a-90df-844c97be5311.png` and
verified against the source hash. The user selected it for an early, optional
everyday desk conversation in Sub-project 7. The natural scene `desk_moment`
shows the full unchanged image above separate, scrollable dialogue, with two
remembered answers, defer/skip and read-only illustrated journal replay.

`Office/HelperChan.Model.cs` loads the approved rigged chibi GLB from
`Helper/Model/helper-chan-animated.glb`, with locally authored walking, writing,
seated and idle clips. `Office/HelperChan.cs` retains procedural fallback geometry. It is not a generic employee palette or a sprite in
the office. Features include long curling blonde twintails, blue eyes, glasses,
collared blouse, pencil skirt, stockings, shoes, lanyard and clipboard. Her desk,
chair, lamp, stationery and paper are built by `OfficeArt` in a non-economic
alcove. Ordinary saved furniture, capacity, payroll and pathfinding are untouched.
When an assigned office is closed, its visual room can still be viewed; this
does not reopen its lease or create a business location.

The entire 3D cast uses chibi proportions: roughly 2.5 heads tall, with shorter
torsos and limbs and larger faces. Helper-Chan shares the staff's height range;
her long twintails remain deliberately wider. Employees, the protagonist and
ambient residents use the same proportions, including their hyperlapse silhouettes.
Seated poses keep heads above desks, and her clipboard is raised above the worktop.
The supplied 2D portraits retain their original proportions.

## Manga showcases

Twelve original genre atlases cover action, adventure, comedy, sports, romance,
drama, slice-of-life, horror, mystery, fantasy, sci-fi and the generic fallback.
Each is a 2-column, 3-row grid: two color cover compositions followed by four
monochrome panels. The showcase uses one deterministic cover and three panels;
the fourth panel is available in the source atlas. Atlas regions are selected
by Godot without modifying the generated originals. Twenty-four cover variants
are available across the library. These are reusable illustrative premises,
not renderings of every chapter or simulation of its plot.

Custom artwork overrides each slot independently. PNG/JPEG inputs are limited
to 20 MiB and 4096 pixels per side before decoding. The preview preserves aspect
ratio unless the player chooses a center crop. Accepted images are stored as
PNG by content hash inside their career and included in portable exports.

## Fixed parents and modular staff

`Parents/mom/mom-animated.glb` and `Parents/dad/dad-animated.glb` are the approved
fixed household models, with locally authored ambient clips. Each folder preserves
the user's source reference, built-in image-generation adaptation and prompt,
and Tripo task provenance. `People/modular-person.glb` is the original interchangeable
employee kit and remains driven by saved appearance choices. Source integration
uses these models by default; compilation and game-rendering checks remain pending.
