# Modular employee kit, first implementation

## Scope

Original interchangeable chibi geometry for Aki, employees and generic visitors. Mom and Dad keep their existing fixed placeholder models until the user supplies references; Helper-Chan is unchanged. No additional Tripo request or credit use.

The current kit is a functional prototype. It does not yet match the finish of the approved generated Helper-Chan, and it is not a segmented version of the generated mangaka. It provides independently editable modules with consistent attachment points, so the art can be refined or replaced without regenerating every employee.

## Shared asset and appearance

- `godot/Assets/People/modular-person.glb`: original geometry, generated reproducibly by `scripts/build-modular-people.py`. All three hairstyles and clothing modules live in one reusable asset. Godot and the browser preview select one of each.
- Short, bob and tied-back hair; cardigan, collared shirt and sweater. Cardigan/sweater include lower sleeves; shirt exposes forearms. Glasses are a separate module.
- Four skin tones, four hair colours, six clothing colours, three body widths. Head and leg length remain consistent. Face remains mouthless.
- Palette materials are named independently of meshes. The Godot loader duplicates materials per employee, protecting other employees from recolouring and hyperlapse fading.
- `AppearanceRecipe.Wardrobe` is an optional trailing field, default zero (cardigan). Existing skin/hair/colour/style/glasses/build values keep their meaning. Existing save recipes receive the default; generated future employees choose clothing using the existing cosmetic-only hash, with the original first six choices unchanged.
- New Career and the existing starting-appearance controls select clothing and body build. The live creator viewport and office actors use the same `OfficeActor.Build(AppearanceRecipe)` path. Small staff portraits reflect the palette and clothing choice.

## Motion and compatibility

The first rig is a hierarchy of rigid chibi parts, not a weighted skeletal mesh: Body, Head, Torso, shoulder/forearm joints and hip/knee joints. It reuses the existing route following, seat height, work pose, attention/selection indicators, and hyperlapse exposure. Elbows position short hands near desk height while writing. No imported animation clips, physics or simulation changes.

The shared hierarchy is the compatibility contract for later art. Smooth skin deformation and more elaborate hair motion remain future work. The raw kit GLB includes all alternatives, so a generic external viewer must select modules instead of displaying every hairstyle/outfit at once.

## Preview and validation

- Interactive preview source: `docs/superpowers/mockups/modular-people.html`. Working copy and a small local Three.js dependency set: `TestResults/modular-people/`. Serve the latter on localhost; it loads a byte-identical copy of the game GLB.
- Browser review performed: default standing, bob + glasses + collared shirt at a desk, tied-back hair + sweater + broad build walking, skin/hair/clothing recolouring, front and back inspection. This proves the standalone preview path, not Godot import or in-game rendering.
- GLB structural validation passed: 642,344 bytes; 23,976 triangles across **all** alternative modules; 36 mesh/material batches; no external textures or required GLTF extensions. Only the selected modules are shown on each employee.
- 172 non-generated C# files passed a syntax parse. No compilation, game launch or packaged build was performed.
- Added unrun simulation checks for wardrobe persistence, legacy recipes, invalid choices and cosmetic RNG independence. Updated atmosphere smoke checks to verify clothing/build controls and the actual modular import path.

Next authorized game check should cover GLB import/material names, per-employee material isolation, all hairstyles at work and break seats, torso/chair and hair/desk clearance, walking/exposure at each speed, saved identity restoration and 720p / 16:9 / 21:9 creator-menu fit.

## Art refinement after Helper rig trial

Eyes now sit flush against the face as illustrated almond shapes. Front hair locks are flatter and tapered, with a recolourable highlight slot; clothing has softer contours and visible shirt buttons. Preview and game asset remain identical. These are incremental improvements, not a claim of equal polish to the generated Helper model.
