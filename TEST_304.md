# SkiStation 3.04 — Objectif piste persistant et itinéraire sûr

Base : 3.03.

## Ajouts
- Une piste destination choisie reste l'objectif du visiteur jusqu'à ce qu'elle soit réellement terminée.
- Les changements de files d'attente peuvent modifier le chemin vers cette piste sans faire abandonner l'objectif.
- Si fermeture/panne rend la destination inaccessible, l'objectif est abandonné proprement et une autre piste accessible est choisie.
- La validation d'un trajet tient désormais compte du niveau réel du skieur sur **toutes** les pistes intermédiaires, pas uniquement sur la destination.
- Aucun téléport : le système ne déplace personne, il ne fait que fournir la prochaine remontée/piste aux systèmes existants.

## Test Unity conseillé
1. Construire deux chaînes de remontées/pistes permettant d'atteindre une piste éloignée.
2. Laisser un visiteur choisir cette destination puis augmenter la file sur une des remontées : il doit pouvoir changer de chemin sans changer de destination.
3. Fermer une infrastructure indispensable : il doit renoncer à l'objectif devenu inaccessible et en choisir un autre.
4. Vérifier qu'un débutant ne planifie pas une chaîne imposant une piste intermédiaire trop difficile.
5. Vérifier l'absence d'erreurs rouges.

Compilation et Play Mode Unity non exécutés dans l'environnement de génération.
