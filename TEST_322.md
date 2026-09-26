# SkiStation 3.22 — Micro-relief naturel 360°

Base directe : 3.21.

## Objectif
Supprimer progressivement l’aspect « grande nappe de terrain généré » sans sacrifier la liberté de construction.

## Ajouts
- micro-relief multi-échelle sur toute la map : bosses, creux et ondulations légères ;
- ondulations croisées pour éviter une lecture avant/arrière ;
- convexités et dépressions locales sur les quatre quadrants et la face arrière ;
- petites ruptures non continues, sans marches artificielles ;
- amplitudes volontairement faibles pour préserver pistes, remontées et zones constructibles ;
- règle 360° appliquée à absolument toute la map.

## Vérification locale
`SkiStation > Montagne > Verifier le generateur 3.22`

Le contrôle de variation de graine compare désormais la graine 322 à 323.

## Test visuel
`SkiStation > Montagne > Atelier montagne 3.22 > Créer une nouvelle scène d’étude`

Tourner autour du massif et contrôler notamment les côtés et la face arrière.

⚠️ Compilation Unity et Play Mode non exécutés dans cet environnement.
