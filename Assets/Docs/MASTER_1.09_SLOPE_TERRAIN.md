# SkiStation Master 1.09 — Slope ↔ Terrain

## Objectif
Les pistes construites sont désormais explicitement conformées au relief réel de `MountainRuntime`. Le centre de la piste est resynchronisé avec la surface du massif et les deux bords de la bande neigeuse sont eux aussi projetés sur le terrain.

## Résultat
- moins de pistes flottantes ;
- moins d'intersections visuelles avec le relief ;
- largeur variable conservée ;
- construction 1.08 conservée (B, échantillonnage, largeur `[ ]`, Entrée/Echap) ;
- le système ne sculpte pas encore le terrain ;
- tunnels, bosses et terrassements restent une brique ultérieure ;
- aucune population artificielle n'est créée.

## Architecture
- `SlopeTerrainConformityRuntime` : resynchronise les points avec `MountainRuntime`;
- `SlopeMeshGenerator` : projette chaque bord sur la surface du massif avant de générer la bande neigeuse ;
- `SlopeConstructionMode` et `SlopeBuilderPlayable` : appliquent automatiquement la conformité avant génération visuelle.

## Important
Le terrain reste non destructif dans cette version. La piste épouse le relief existant, mais ne le creuse pas encore.
