# SkiStation Master 1.04 — Plateaux et circulation naturelle

## Intention
Quand une piste arrive sur un vrai plat, elle ne doit pas forcer chaque visiteur à suivre un petit chemin artificiel.

Le jeu crée une zone logique invisible au niveau d'un endpoint suffisamment plat. Le visiteur peut alors se disperser naturellement :
- reprendre une remontée proche ;
- rejoindre directement une activité ou un service proche ;
- quitter le domaine depuis la base ;
- rester brièvement en zone avant sa prochaine décision.

## Principes
- Aucun ruban de neige n'est créé pour la zone libre.
- Aucun NavMesh ou réseau routier n'est ajouté.
- Aucun visiteur artificiel n'est créé.
- La simulation de gestion reste la source de vérité.
- Les raccords piste -> remontée de la 1.03 restent disponibles quand un raccord explicite est réellement nécessaire.

## Technique
- `SlopeTerminalAreaRuntime` : zone logique d'un plateau.
- `SlopeTerminalAreaBuilder` : détection automatique d'un endpoint peu incliné.
- `SkierTerminalDecisionRuntime` : décision lift / service / activité / départ pour un vrai skieur.
- `SkierRouteAgent` : consultation de la zone terminale avant la recherche forcée d'une remontée.
