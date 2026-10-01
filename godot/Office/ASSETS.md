# Office asset kit

Furniture, room geometry and modular employees are original code-authored assets.
Helper-Chan and the fixed parents now use the approved Tripo-generated and rigged
models adapted from user-provided references. Their local animation clips,
reference prompts and provenance are recorded in `../Assets/Helper/Model/`,
`../Assets/Parents/`, and `docs/superpowers/tripo-asset-trial.md`. No service or API
key is needed at runtime. This manifest does not grant a new asset licence.

## Sources and conventions

- `OfficeArt.cs`: reusable furniture and room props, rough flat materials,
  simple paper/ruler/lamp/cup/book/plant meshes. `Furniture(kind, variant)`
  returns a local `Node3D`; the scene is generated from these reusable models.
- `OfficeActor.cs`: chibi proportions with modular hair, skin, clothing, glasses
  and build, articulated arms and two-part legs and standing/walking/seated work poses.
- `OfficeActor.Exposure.cs` and `Hyperlapse.gdshader`: pooled long-exposure
  human silhouettes, soft edges, skin/clothing bands and drawing-hand samples. Feet use a local ground origin; forward is negative Z.
- `OfficeAmbientTraffic.cs`: hidden waiting and immediate door-threshold arrival/exit
  for visual-only household/building occupants.
- `OfficeView.cs`: property shell, domestic/shared building fittings, window
  frames, door signs, floor seams, stock boxes, shoes and umbrella stand.
- `DebugMain.Office.cs`: matching original flat 2D portrait and palette.
- `Prototype/OfficePrototype.tscn`: independent motion-development scene.

World units are metres. Simulation furniture footprints use a 0.25 m grid.
Furniture pivots are the back-left floor corner; `PlacementOrigin` compensates
for quarter-turns so rendered and logical footprints agree. Characters use
approximately 1.2 world units standing height with heads roughly two-fifths of
their height. Helper-Chan uses the same chibi height range with her distinctive
wide twintails; her portrait and asset provenance are in `../Assets/README.md`.
These are stylized character dimensions, not literal human scale. Logical clearance is authoritative and
uses a 0.75 m approach, separate from ornamental mesh detail.

Legs stay short in standing, walking and seated poses. Seated feet dangle rather
than elongating to reach the floor. Desk/break cushion surfaces are shared with
the character pose code; pelvis/skirt placement clears the cushion and backrest.
The fixed characters use their imported skeletons and authored seated clips.
Employees use the shared modular kit. Integration remains uncompiled at user request.

| Model | Logical footprint | Variants and gameplay |
|---|---|---|
| Desk | 1.25 × 0.75 m | Basic/better appearance; same production |
| Work chair | 0.75 × 0.75 m | Basic/support; support reduces hourly comfort loss by 0.5 |
| Break set | 1.5 × 1.5 m | Two stools; comfort variant adds 3 recovery comfort |
| Tea cabinet | 0.75 × 0.5 m | Service/cosmetic object |
| Book shelf | 1.0 × 0.5 m | Decorative; no extra book inventory capacity |
| Plant | 0.5 × 0.5 m | One atmosphere point per distinct decor category |
| Wall print | 1.0 × 0.25 m logical | Original abstract three-panel graphic; back wall only |

Prices and property floor areas are game estimates, not claimed 1996 listings.
Research sources and historical/proxy distinctions remain in
`docs/superpowers/specs/2026-09-23-3d-office-research.md`. The shared spaces are
stylised plans; private rooms and WC interiors stay behind closed doors.

The rendering uses standard Godot primitives and materials, no external model
loader or runtime asset service. Full timelapse uses a fixed 96-sample instanced pool per actor, sampled every
0.075 m along the actual path, with a 4.5 m maximum streak. The shutter grows
from 0.10 to 0.18 seconds at 2x/4x/8x. Reduced draws every third sample; Off
draws none. Seated work has a separate pool of up to 20 hand samples. Moving
bodies blend smoothly into the exposure rather than staying fully opaque. It affects characters only, leaving the
room and UI sharp. This kit is a usable initial art pass, not final commercial
character animation or a catalogue of authored historical interiors.


Ambient circulation uses two framed, hinged doors in actual outer-wall
openings. Door thresholds, rigid leaves and a single-sided dark recess keep the
openings readable with wall cutaways and camera rotation. Door positions also
supply the route endpoints. Occupants emerge at one threshold, follow the clear
shared corridor, and disappear on the update that reaches the opposite threshold.
All waiting is hidden; exit clears the body and its exposure history together.
The 1.2 m leaves rotate into their recesses without changing dimensions. Door
requests and proximity hold them open while occupants pass. A separate wall and
doorway divide the furnished break room from the traffic lane. The fridge and
counter stay inside the break room; the outer corridor is reserved for walking.

Staff use the same entry threshold when leaving and returning. Helper-Chan's
walking, desk gestures and following are presentation only. Decorative break
visits likewise leave production and needs unchanged. The latest room/door/
companion changes still await compiled and rendered verification; see
`docs/superpowers/private-alpha-feedback-3.md`.

## Imported character runtime

`CharacterModel.cs` caches packed scenes, instantiates the approved GLBs, duplicates
animation libraries and surface materials per actor, and manually advances clips.
`OfficeActor.BuildParent` selects Mom/Dad; `HelperChan.Model.cs` keeps the existing
companion routes and selects Walk, Write, Sit and Idle. Selection, parent exposure,
door thresholds, visual breaks and simulation roles remain on their existing paths.
The modular employee kit is still selected by saved appearance recipes and also
powers the New Career viewport. Procedural geometry is an import-failure fallback.

## Title screen branding

`../Assets/Branding/logo.png` (1712 x 581, "MANGAKA DAYS" lettering with a chibi
Helper-Chan) was generated by the project owner with NovelAI Diffusion V5 on
2026-09-28 (prompt 6 in `docs/superpowers/novelai-prompts.md`), upscaled in NovelAI,
then cleaned of faint alpha noise and cropped locally.

`../Assets/Branding/title-background.png` (2688 x 1536, a dusk manga workroom with
a tilted drafting board, upscaled 2x in NovelAI) was generated by the project owner with NovelAI Diffusion V5 on
2026-09-28 (prompt 1, revised, in `docs/superpowers/novelai-prompts.md`); checked
at full size for readable text or real manga covers, none found. It replaced an
earlier Gemini placeholder.

## Interface font

`../Assets/Fonts/LilitaOne-Regular.ttf` (Lilita One) is used for buttons and
headings. Downloaded 2026-09-29 from Google Fonts' repository
(`google/fonts`, `ofl/lilitaone`); SIL Open Font License 1.1, licence text in
`../Assets/Fonts/OFL-LilitaOne.txt`, shipped beside the game by
`scripts/package-alpha.ps1`.
