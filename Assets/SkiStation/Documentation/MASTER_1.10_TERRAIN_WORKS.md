# SkiStation Master 1.10 — Terrain Works

## Objectif
Première couche de modification du relief, non destructive par rapport à la hauteur procédurale de base.

## Outils
- T : active/désactive les travaux de terrain.
- 1 : creuser.
- 2 : remblayer.
- 3 : aplanir autour d'une hauteur capturée.
- clic gauche : appliquer.
- clic droit : inverser l'opération.
- - / + : rayon de brosse.
- Backspace : remettre toutes les modifications à zéro.

Les modifications sont stockées comme deltas de hauteur et le MeshCollider est reconstruit après chaque coup de brosse.

## Limites volontairement conservées
- pas encore de système de déblais/volumes de terre ;
- pas encore de tunnels ;
- pas encore de bosses/champs de bosses ;
- pas de destruction de la montagne procédurale de référence ;
- les futures pistes pourront demander des travaux ciblés automatiquement.

## Suite logique
1.11 pourra relier automatiquement les travaux de terrain à une piste : plateforme douce, creusement contrôlé et seuils de pente, avant d'introduire tunnels et ouvrages spéciaux.
