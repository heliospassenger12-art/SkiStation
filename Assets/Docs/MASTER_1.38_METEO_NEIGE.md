# MASTER 1.38 — Météo, neige et conditions du domaine

Objectif : donner au domaine une première couche de conditions montagne crédibles sans introduire de destruction artificielle.

## Ajouts
- Météo dynamique : grand soleil, nuageux, neige, fortes chutes de neige, tempête.
- Température, vent et visibilité affichés dans le HUD.
- Qualité de neige évolutive : les épisodes neigeux améliorent progressivement la qualité disponible.
- Les remontées adaptent leur vitesse aux conditions.
- Le vent peut faire bouger/ralentir le système mais ne provoque jamais de dégâts ni de casse.
- La météo est pilotable par le runtime avec `ForceWeather(...)`, utile pour les futurs scénarios carrière.

## Principe de simulation
La météo influence le confort et les performances, mais ne remplace pas les systèmes réels de domaine. Les visiteurs restent de vrais agents de gestion et les remontées restent l'autorité pour les files, l'embarquement et le transport.

## Commandes
Les commandes 1.37 restent inchangées : B piste, L remontée, M carte, F1 aide, Espace pause, 1/2/3 vitesses.
