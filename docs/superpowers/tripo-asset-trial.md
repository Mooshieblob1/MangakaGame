# Tripo chibi asset trial

This is a standalone art experiment. No game assets have been replaced, no game compilation performed, and no animation has been purchased. The approved Helper rigging pass is recorded below.

## Pipeline

- API key: read from Windows user environment `TRIPO_API_KEY`; never stored in the repository or printed.
- API: [Tripo v3](https://developers.tripo3d.ai/en/docs/quick-start).
- Runner: `scripts/tripo-chibi-trial.ps1`; `-Character Mangaka` for the first text-only trial, `-Character Helper` for the requested image-based comparison. Saves generation IDs and rejects duplicate submissions. Free riggability checks use `RigCheck` / `RigStatus`.
- Model validation: `scripts/inspect-tripo-glb.py` checks GLB chunk/buffer structure and reports mesh, texture, skeleton and animation counts. A structural check is not proof of correct rigging or runtime performance.
- Local interactive preview: `TestResults/tripo-chibi-trial/index.html`, served from that directory. Query `?character=helper` selects Helper-Chan. Front, side, back, game-angle and tiny-display controls support visual review. Uses Google's model-viewer library, loaded locally. No API key enters the browser.

## First mangaka

- Text-to-model, H3 `v3.1-20260211`, standard textures, 8,000-face request.
- Task `8abe55c3-7c66-4a54-99fb-1c5161fd596f`, successful; 20 credits consumed.
- `TestResults/tripo-chibi-trial/chibi-mangaka.glb`: 2,139,596 bytes; 7,902 triangles; one mesh and material; three embedded texture images; no external resources, skeleton or animation.
- Browser loaded and rendered the downloaded GLB. Compact proportions and short legs are present. The generated clothing is closer to a teal pullover than the requested cardigan; hands read as dark mittens. Treat as a comparison candidate, not final character art.

## Helper-Chan

- Source: existing transparent `godot/Assets/Helper/neutral.png`, the user's supplied design.
- Prepared a chibi derivative with the built-in image-generation tool (not the CLI). Retains enormous long curled blonde twintails, blue eyes, glasses, blouse, pencil skirt, tights, shoes, lanyard and clipboard, with a large head and shortened legs.
- Saved reference: `TestResults/tripo-chibi-trial/helper-chan/chibi-reference.png`. Exact image prompt is beside it in `reference-prompt.txt`. The original game artwork is untouched.
- Uploaded this derivative to Tripo for H3 image-to-model, 12,000-face request, standard textures, original-image texture alignment and no autofix or paid add-ons.
- Task `1fdbd449-d017-4211-ad28-9d01d01e8e6a`; successful, 30 credits consumed.
- `TestResults/tripo-chibi-trial/helper-chan/helper-chan.glb`: 2,648,948 bytes; 11,152 triangles; 8,611 vertices; one mesh and material, three embedded texture images. No required GLTF extensions or external resource dependencies. No skeleton or animations yet.
- Visually reviewed the downloaded model in the browser from front, side, back and an elevated game-like angle, including a 180x200 preview. Recognizable glasses, eyes, short legs, outfit and oversized twintails survive generation. Hair curls are visibly coarse ribbon-like strips in profile/back views; they need cleanup and deformation review before production use. The clipboard is part of the single generated mesh.
- Free riggability check `259a5b80-d615-4785-8087-37390b21debb` succeeded: `riggable=true`, recommended `biped`, zero credits consumed. This indicates auto-rig eligibility, not a tested skeleton or seated/walking deformation.
- Account balance verified after both trials: 450 available, zero frozen. Total generation cost 50 credits (USD 0.50).

## Remaining game work

Any accepted model still needs scale/pivot alignment, skeleton and deformation review, seated/standing/walking poses, hair and clipboard attachment decisions, desk/chair clearance, and engine performance checks. A static generation is not a drop-in replacement for the current procedural actors.

## Approved Helper rig and locally authored motion

- Rig task `5765ddee-5240-4deb-b715-16f006f2704b`: successful; 25 credits consumed, balance 425 available / zero frozen. Total Tripo cost across both generation trials and this rig: 75 credits ($0.75).
- Native biped auto-rig `v1.0-20240301`, Mixamo bone names, GLB output. Original result remains untouched in `TestResults/tripo-chibi-trial/helper-chan/helper-chan-rigged.glb`: 23 joints, one skin, zero supplied animation clips.
- `scripts/animate-helper-trial.py` authors Idle, Walk, Sit and Write locally. No animation-retarget API calls or fees. Output candidate: `godot/Assets/Helper/Model/helper-chan-animated.glb`; byte-identical preview copy at `TestResults/modular-people/helper-animated.glb`.
- Found and corrected long hair weighted to limbs: 5,460 outer/blonde strand vertices reassigned to two added twintail joints. Gentle hair sway is hand-authored, not physics. Bind matrices normalized to the mesh coordinate space. Added a rigid pen attached to the right hand.
- Candidate: 25 joints, 4 clips, 2 meshes, 11,184 triangles, 3,095,228 bytes. Original embedded textures retained. PNG/JPEG inside the GLB are model textures, not screenshots.
- Preview: `http://127.0.0.1:8767/helper.html`; reproducible page source `docs/superpowers/mockups/helper-animation.html`. Includes pose, camera, skeleton, desk visibility, pause and tiny-view controls. Asset faces +X in source coordinates; preview rotates +90 degrees around Y and scales 1.2 to match employee height.
- `scripts/check-helper-animation.py` passed skin weight, rest bind position, quaternion normalization, finite/time-ordered track and loop-seam checks. Rest binding maximum positional error below 0.000001. Structural GLB checks passed. These are asset checks, not a Godot compilation or in-game test.
- Remaining limits: original clipboard is still fused into the skinned mesh, so keep gestures restrained pending separation/weight cleanup. Pencil-skirt deformation and the coarse hair ribbons need further art review. Desk pose is aligned to the preview furniture, not every game desk. Imported Helper is an asset candidate and is not yet wired into the game actor.

Final browser review included walking from the front and writing from the side after tightening the hair/skin colour separation, which removed stretched hand-to-hair triangles. The revised employee kit was also reviewed with silver bob hair, glasses and a collared shirt at the desk.

## Parent reference batch

User supplied Mom and Dad references; both were adapted to chibi using built-in image generation. Models, original/chibi references, exact prompts and task provenance are retained in `godot/Assets/Parents/`.

- Mom generation: `e0019b3f-a9f6-4d7e-9693-a91c9e2bd5d9`; rig: `0d5aee57-f1d3-4a3f-ad29-80e9a03a9d5e`.
- Dad generation: `00edf771-5d74-40db-9bc3-c430f357fb76`; rig: `dab7db20-050f-43be-9b18-cc4c816b5f0a`.
- Both free rig checks passed. Each generation cost 30 and each rig 25; this batch used 110 credits. Latest verified balance: 315 available / zero frozen. Total all trials to date: 185 credits ($1.85).
- Three local animation clips per parent: Idle, Walk, Sit. No paid animation requests. Mom: 11,875 triangles, 3,471,404 bytes animated. Dad: 11,298 triangles, 4,243,712 bytes animated.
- Browser preview: `http://127.0.0.1:8767/parents.html`. Structural and skin/animation numerical checks passed; front walking and side seated poses reviewed for both. Preserved original vendor assets, no additional retopology/segmentation purchases.
- Fixed parent game actors remain unchanged. Imported models are candidates awaiting engine validation. See `godot/Assets/Parents/README.md` for exact reproduction and validation boundaries.
