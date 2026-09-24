# SkiStation — Master 0.88 — Intersections & circulation

## Objective
Turn the 0.87 access-road traffic signal into a usable circulation layer: junction pressure, bottlenecks and management hotspots.

## Added
- AccessNetworkRuntime and AccessNetworkNode are now present in the active Unity Assets tree.
- AccessIntersectionTrafficRuntime measures pressure around every constructed network node using only real `SkierRuntime` visitors and real road traffic.
- AccessTrafficHotspotRuntime exposes watch/critical access bottlenecks and a management recommendation.
- AccessTrafficHotspotUI opens with **F9**.

## Principles
- No artificial visitors.
- No mutation of the management simulation.
- Player simulation remains independent.
- Pedestrian routing continues to prefer lower-congestion accessible roads.
- A hotspot is an observation/decision aid, not an automatic construction action.

## 0.89 direction
Road directionality and lane/sidewalk flow, followed by parking/drop-off queue pressure and stronger integration into the management command center.
