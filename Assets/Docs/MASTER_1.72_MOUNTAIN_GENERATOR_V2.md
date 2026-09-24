# SkiStation Master 1.72 — Mountain Generator V2

This pass replaces the rounded-peak heightfield as the terrain authority while preserving the rendering/camera chain proven in Unity.

## Morphology
- Main broken watershed made from ridge segments.
- Secondary spurs that frame a central ski domain.
- Three summit horns used only as accents, not as the base terrain shape.
- Central U-shaped glacial valley and four secondary combes/valleys.
- Deliberate cols cut into the main crest.
- Directional multi-scale erosion and gullies.
- Broad southern resort basin/front-de-neige.
- Boundary blending to reduce the visible terrain slab effect.

## Architecture
`AlpineMassifGeneratorV2.Height(x,z)` is now the deterministic terrain source. `SkiStationGuaranteedMountainVisual.Height` delegates to it, so the visible mesh and the 1.71 gameplay terrain queries remain synchronized.

Unity visual/compile validation is still required before this becomes the next validated baseline.
