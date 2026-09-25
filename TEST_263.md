# SkiStation 2.63 — Falaises alpines visuelles

Base : 2.62.

## Ajout
- Les barres rocheuses existantes reçoivent un maillage visuel plus irrégulier et fracturé.
- Plusieurs tons de roche cassent l'effet bloc uniforme.
- Les faces orientées vers le haut reçoivent de petites corniches de neige.
- Ombres et réception des ombres conservées.

## Sécurité gameplay
- Aucun collider n'est remplacé ou déplacé.
- Aucun changement des pistes, remontées, visiteurs, routes ou règles de construction.
- Le maillage d'origine reste la référence physique ; seul son Renderer est masqué quand le nouveau visuel est prêt.

## Test Unity
1. Ouvrir la scène de gestion habituelle et lancer Play.
2. Observer les barres rocheuses en altitude : silhouette moins régulière, nuances sombres et neige sur les replats.
3. Vérifier qu'aucune erreur rouge n'apparaît.

Archive contrôlée hors Unity. Compilation et rendu Play Mode à confirmer dans Unity.
