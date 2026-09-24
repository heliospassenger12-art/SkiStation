# SkiStation — MASTER 0.99 — Circulation montagne

## Objectif
Faire évoluer le réseau de pistes 0.98 vers un véritable graphe de circulation exploitable par la simulation de gestion.

## Ajouts
- `SkiDomainRoutePlanner` : recherche de route entre pistes réellement connectées.
- Coût de parcours combinant longueur, distance de raccord, trafic observé et changement de direction.
- `SkiDomainCirculationRuntime` : snapshot du domaine skiable réel.
- `SkiDomainCirculationDebugUI` : diagnostic F11.

## Règles conservées
- Aucun skieur artificiel.
- Le trafic provient uniquement des visiteurs réels déjà simulés.
- Pas de modification du réseau routier.
- Pas de NavMesh imposé.
- Pas de terrain sculpté automatiquement.
- Le joueur reste indépendant de la simulation de gestion.

## Suite logique 1.00
Brancher le planificateur au choix de parcours des `SkierRuntime` et au réseau des remontées, avec des destinations cohérentes : piste, remontée, village ou zone de fin de parcours.
