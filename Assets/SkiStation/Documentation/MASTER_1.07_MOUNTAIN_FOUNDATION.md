# SkiStation Master 1.07 — Mountain Foundation

## Objectif
Première vraie couche de terrain jouable : une montagne procédurale cohérente servant de support aux futures pistes, remontées, bâtiments et zones naturelles.

## Principes
- montagne générée de façon déterministe à partir d'une seed ;
- grande zone basse relativement plate pour le village/station ;
- relief central avec vallées, crêtes et sommets ;
- classification Base / Meadow / Forest / Alpine / Rock / Summit ;
- échantillonnage altitude + pente + zone + constructibilité + skiabilité ;
- MeshCollider pour permettre les futurs raycasts de construction ;
- aucune piste, remontée ou population artificielle créée par cette brique.

## Utilisation
1. Ouvrir `Assets/Scenes/SkiStation_Bootstrap.unity`.
2. Lancer Play.
3. `F3` affiche les diagnostics montagne.
4. Le GameRoot crée automatiquement la montagne si elle n'existe pas.

## Suite
1.08 : outil de construction de pistes directement sur ce relief.
1.09 : adaptation réelle de la géométrie des pistes au terrain.
1.10 : première remontée construite sur le relief.

## Limite importante
La compilation Unity réelle n'est pas exécutée dans cet environnement. Les contrôles réalisés ici sont statiques et structurels ; le premier import dans Unity 6.3 LTS reste la validation finale.
