# SkiStation Master 1.11 — Piste intelligente & terrassement

## Objectif
Relier directement la construction d'une piste aux travaux de terrassement nécessaires sur la montagne.

## Fonctionnement
- Tracer une piste normalement avec le mode B.
- Survoler une piste existante et appuyer sur P.
- Le système analyse le relief sous le corridor de piste et calcule un profil de terrassement doux entre le haut et le bas.
- Une ligne de prévisualisation et un résumé des zones de travail sont affichés.
- Entrée applique le plan sur la couche non destructive `MountainTerrainWorkRuntime`.
- La piste est ensuite recalée sur le nouveau terrain et son mesh est régénéré.
- Échap annule uniquement la proposition en cours.

## Principes
- Aucun visiteur artificiel.
- Aucun nouveau système routier.
- Aucun tunnel ni bosse procédurale à ce stade.
- Les travaux restent une couche de delta au-dessus du relief procédural.
- Le système prépare une future estimation économique et de volume de terrassement.

## Commandes
- B : construire une piste
- P : analyser la piste sous la souris
- Entrée : appliquer le terrassement analysé
- Échap : annuler la proposition
