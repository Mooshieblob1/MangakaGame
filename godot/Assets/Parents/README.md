# Fixed parent character candidates

The original user references, chibi adaptations, exact image-generation prompts, provenance and animated model candidates are kept in the `mom` and `dad` folders. Chibi adaptations used the built-in image_gen tool, not the CLI. The original PNGs and generated alpha are preserved; these are art references, not screenshots.

Both models preserve the supplied fixed appearances. Mom has a brown side braid, blue eyes, cream sweater and apron, blue jeans and beige slippers. Dad has a bald crown, brown side hair and moustache, glasses, green eyes, cream/olive argyle sweater, beige trousers and green slippers.

Each model cost 30 Tripo credits to generate and 25 to rig. Both free rig checks passed. Total this batch: 110 credits, leaving 315 available and zero frozen. No animation API calls were made.

`mom/mom-animated.glb` and `dad/dad-animated.glb` contain 23-joint Mixamo-name biped skeletons and locally authored Idle, Walk, Sit and Eat clips. Source faces +X; preview rotates +90 degrees around Y and scales 1.2. The parent actors now load these files through `Office/CharacterModel.cs`, with procedural fallback if import is unavailable. Integration awaits compilation and engine checks.

Generation/rig originals are retained under the ignored `TestResults/tripo-chibi-trial/mom` and `dad` folders. Recreate local animation with `python scripts/animate-parent-trial.py mom` (or dad). Validate with `python scripts/check-helper-animation.py TestResults/modular-people/mom-animated.glb --clips Idle,Walk,Sit,Eat`.

Preview at `http://127.0.0.1:8767/parents.html`, with Mom/Dad links and Idle/Walk/Sit controls. Page source is `docs/superpowers/mockups/parents-animation.html`.

Validation: both GLBs passed structural checks, normalized skin weights, rest-pose binding, finite animation values, quaternion normalization, increasing sample times and looping seams. Browser review covered each parent's front walk and side seated pose; Mom's relaxed idle was also reviewed. Rest skin maximum error was below 0.000001 for both. Preview files match the saved game-folder candidates exactly.

No compilation, game launch or build occurred. The replacement is wired in source. Actual home route following, doorways, scale, culling, and chair alignment still need an authorized engine check. This preview is not proof of production deformation quality across every pose.

## Occasional meals

The Eat clip is authored locally by animate-parent-trial.py. Both regenerated GLBs pass skin/bind/loop validation. At home, parents occasionally reserve one of the two rear table chairs, use the corridor opening, eat with a bowl, then leave through a door. Staff retain priority if a scheduled activity needs their seat. Closing time interrupts meals and waits for normal exits. Meals do not touch simulation state or RNG. Added lifecycle checks are pending the next authorized compile and engine run; alpha.8 is unchanged.
