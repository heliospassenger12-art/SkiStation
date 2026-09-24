# SkiStation Master 1.51 — Zones de gestion physiques

## Objectif
Permettre au joueur de délimiter une zone précise du domaine, notamment une vraie zone débutants.

## Fonctionnement
Le joueur active l'outil avec `Z`, pose au minimum trois points sur le terrain, puis valide avec `Entrée`.
La forme est convertie en polygone XZ et reprojetée sur le terrain.

La frontière est visible et matérialisée par des piquets et une barrière avec colliders. Elle constitue donc une vraie construction dans le monde, et pas seulement une zone invisible de gestion.

## Zone débutants
Les pistes qui touchent la zone sont détectées. Le système de comportement donne alors un bonus de choix aux profils Débutant, Famille et Jeune, avec un bonus supplémentaire pour les pistes vertes.

La zone n'interdit pas physiquement les autres visiteurs et ne verrouille pas les pistes. Elle représente une infrastructure de station : le joueur construit une zone sécurisée/lisible, tandis que les visiteurs adaptent naturellement leurs choix.

## Suite prévue
Le même système pourra accueillir des zones familiales, sportives, snowpark ou nature, avec des effets spécifiques et éventuellement des équipements dédiés.
