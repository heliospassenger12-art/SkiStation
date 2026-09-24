# SkiStation Master 1.12 — Profils de piste

## Objectif
Donner une forme sportive à la surface skiable sans rendre le terrain de montagne destructif.

## Profils
- Naturel : aucune déformation ajoutée.
- Régulier : lissage léger du profil.
- Bosses : ondulations modérées.
- Champ de bosses : ondulations rapprochées et plus fortes.
- Compression : zone de compression progressive.
- Mur : relevé progressif d'une section terminale.

## Utilisation
- P : activer/désactiver l'outil.
- 1 à 6 : choisir le profil.
- Clic gauche : appliquer au tracé de piste le plus proche.
- - / + : régler l'amplitude.

## Architecture
Le profil est une couche de géométrie sur `SlopeRuntime`. Il ne détruit pas `MountainRuntime` et ne modifie pas la couche de terrassement.
Les points modifiés restent les points de référence du skieur, donc la pente physique observe également le nouveau profil.

## Suite prévue
1.13 pourra transformer ces profils en vrais travaux de piste : coûts, engins, neige déplacée, limites de pente et prévisualisation avant validation.
