# Helper-Chan animated import candidate

The Helper actor now loads this asset through `Office/CharacterModel.cs`. Integration is source-only: no engine import, compilation or in-game rendering check has been run.

`helper-chan-animated.glb` contains the approved Tripo Helper geometry with a purchased biped rig, local twintail weight corrections, two extra hair joints, a hand-attached pen, and locally authored Idle / Walk / Sit / Write clips.

Source faces +X. The common loader uses uniform 1.2 scale and +90-degree Y rotation for ordinary actors; Helper uses -90 degrees to preserve her existing +Z-facing routes and desk. The seated root offsets and writing reach are tuned to the browser preview furniture. Check actual office desk/chair alignment before adoption. The clipboard remains fused into the original mesh; avoid broad hand gestures until it is separated or its skin weights are refined.

Recreate with `scripts/animate-helper-trial.py` from the retained local vendor rig. Check with `scripts/check-helper-animation.py`. The source rig is under the ignored TestResults art experiment folder; the self-contained output GLB is retained here.

See `docs/superpowers/tripo-asset-trial.md` for task IDs, costs, validation and limitations.
