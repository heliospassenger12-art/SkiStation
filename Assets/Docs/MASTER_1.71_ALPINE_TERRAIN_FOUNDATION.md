# SkiStation Master 1.71 — Alpine Terrain Foundation

This version turns the validated visible alpine massif into a gameplay-readable terrain foundation.

`AlpineTerrainFoundationRuntime` samples the same height function used by `SkiStationGuaranteedMountainVisual`, so gameplay queries and the visible mountain cannot silently diverge.

The API exposes altitude, normal, slope, terrain class, buildability, skiability and forest suitability. It also searches the southern valley for suitable resort/front-de-neige anchor points.

This is intentionally a foundation pass: no new heavy player-facing management system is added before the next Unity test.
