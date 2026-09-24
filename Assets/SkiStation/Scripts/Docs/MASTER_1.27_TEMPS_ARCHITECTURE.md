# SkiStation 1.27 — Architecture du temps au choix du joueur

## Principe fondamental
Le calendrier est une donnée de simulation. Le jour/nuit est une couche de présentation optionnelle.

`GameClock` reste la source de vérité pour la date et l'heure de simulation.
`DayNightModeRuntime` décide seulement si le joueur veut voir le cycle jour/nuit.
`TimePresentationRuntime` traduit cette décision en heure visuelle.

## Mode Tycoon — Jour continu
- Le calendrier continue normalement.
- Recherches, compétitions, événements, économie et visiteurs continuent.
- L'éclairage peut rester en journée via `DayNightLightingAdapter`.
- Le damage est résolu hors écran dans `GroomingScheduleRuntime`.
- Aucun skieur n'est bloqué par une opération de damage visible.
- Aucun temps mort artificiel n'est créé.

## Mode Immersion — Jour / Nuit
- `TimePresentationRuntime.VisualHour` suit `GameClock`.
- Les opérations de damage visibles restent soumises à la fenêtre nocturne.
- Les pistes sont fermées pendant une opération visible et rouvertes à sa fin.

## Changement à chaud
`DayNightModeRuntime.SetMode(...)` ou `SetTycoonDayOnly(...)` peut être appelé à tout moment.
Le changement ne remet pas la journée à zéro, ne touche pas aux finances et ne modifie pas la progression du calendrier.

## UI / intégration Unity
Le prototype expose F12 pour basculer le mode. Une future Settings UI doit appeler directement :
- `DayNightModeRuntime.SetTycoonDayOnly(true)`
- `DayNightModeRuntime.SetTycoonDayOnly(false)`

Les scènes peuvent ajouter `DayNightLightingAdapter` et lui assigner leur lumière principale.

## Règle de conception
> Le joueur choisit son rythme : construire sans interruption ou prendre le temps de vivre sa station.

Cette architecture doit rester découplée du Player Simulation : le mode de présentation ne doit jamais créer de visiteurs artificiels ni modifier la logique de déplacement du joueur.
