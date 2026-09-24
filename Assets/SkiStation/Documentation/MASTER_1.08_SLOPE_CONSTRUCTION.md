# SkiStation — Master 1.08 — Slope Construction

## Objectif
Premier outil de construction réellement utilisable dans Unity : tracer une piste directement sur le terrain de la montagne.

## Utilisation
1. Lancer la scène Bootstrap.
2. Appuyer sur `B` pour activer le tracé.
3. Déplacer la souris sur le relief et cliquer pour accumuler des points.
4. `[` / `]` modifient la largeur des prochains points.
5. `Entrée` valide la piste.
6. `Échap` annule.

## Données produites
Chaque piste reçoit un `SlopeRuntime` avec une route de points et une largeur propre à chaque point. La géométrie neige est générée par `SlopeMeshGenerator`.

## Limites volontairement conservées
- Pas de sculpture destructive du terrain.
- Pas encore de tunnels, bosses ou champs de bosses.
- Pas encore de validation avancée des pentes trop fortes.
- Pas encore de raccord automatique aux remontées.
- Le mesh suit les points échantillonnés du terrain ; l'amélioration terrain/piste arrive en 1.09.

## Architecture
`SlopeConstructionMode` → `SlopeRuntime` → `SlopeMeshGenerator` + `SlopeSignRuntime`.
Le système reste compatible avec le futur réseau de pistes et les connexions de remontées.
