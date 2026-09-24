# SkiStation Master 1.01 — Boucle réelle du skieur

## Objectif
Faire passer la décision de route de la 1.00 dans une boucle de déplacement lisible et observable pour les vrais visiteurs de la simulation.

## États explicites
`LiftBottom → Queue → Boarding → Riding → LiftTop → SlopeAccess → Skiing → SlopeBottom → ChoosingNextLift`

Les états sont représentés par `SkierMountainJourneyRuntime`. Le système s'appuie sur les systèmes existants : file de remontée, embarquement, véhicule, débarquement, transfert montagne et agent de piste.

## Intégration
- Lorsqu'un skieur est mis en file, son état de boucle devient `Queue`.
- Lorsqu'il monte dans un véhicule réel, il passe à `Riding`.
- À la sortie du véhicule, le système conserve la remontée source et passe par `LiftTop`.
- La piste sélectionnée par la décision 1.00 est consommée en priorité lors du choix post-arrivée.
- Le transfert vers le départ de piste est exposé comme `SlopeAccess`.
- La descente sur la piste réelle reste pilotée par `SkierRouteAgent`.
- À la fin de piste, l'agent revient vers le choix d'une prochaine remontée.

## Règles conservées
- Aucun visiteur artificiel.
- Aucun véhicule artificiel.
- Aucun déplacement du joueur.
- Les pistes fermées et remontées indisponibles restent exclues.
- La simulation de gestion reste indépendante de la simulation du joueur.

## Limitation
La compilation Unity complète n'est pas exécutée dans cet environnement. Le package a été vérifié statiquement et l'intégrité du ZIP est contrôlée.
