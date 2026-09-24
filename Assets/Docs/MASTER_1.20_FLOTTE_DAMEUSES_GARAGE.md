# SkiStation Master 1.20 — Garage & flotte réelle de dameuses

## Objectif
La stratégie de damage devient une vraie couche opérationnelle : plusieurs dameuses réelles peuvent travailler en parallèle.

## Principes
- Les pistes et visiteurs existants restent la source de vérité.
- Aucune population ou machine fictive n'est créée pour remplir la montagne.
- Un garage possède une capacité et des emplacements physiques.
- Chaque dameuse est un GameObject indépendant avec son propre état et sa propre progression.
- Plusieurs pistes peuvent être damées simultanément selon la taille de flotte.
- La priorité manuelle de 1.19 influence la file réelle.
- Le mode instantané reste compatible : aucune attente obligatoire.
- Le mode chronométré permet le travail parallèle.
- Les opérations peuvent enregistrer une dépense de maintenance.

## Contrôles
- F5 : ancien diagnostic de file.
- F6 : diagnostic flotte/garage.

## Correction intégrée
`GroomingVehicleRuntime` utilise désormais `SlopeRuntime.Length` (et non l'ancien `TotalLength`).

## Limite honnête
Cette version est vérifiée statiquement et par intégrité ZIP ; elle n'a pas été compilée dans l'éditeur Unity ici.
