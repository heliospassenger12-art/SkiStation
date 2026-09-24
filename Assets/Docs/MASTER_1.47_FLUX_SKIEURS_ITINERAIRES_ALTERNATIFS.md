# SkiStation Master 1.47 — Flux skieurs & itinéraires alternatifs

## Objectif
Rendre le domaine plus vivant lorsque les pistes ferment ou deviennent saturées, sans créer de micro-management.

## Reroutage
Le skieur cherche une piste ouverte dont l'entrée est proche de sa position.
Le score favorise :
- faible distance ;
- faible congestion ;
- popularité ;
- adéquation avec l'intention du visiteur.

## Fermeture en cours de descente
Si une piste se ferme pendant qu'un skieur l'emprunte, il quitte le tracé courant et cherche une alternative physique. S'il n'en existe pas dans le rayon prévu, il rejoint une remontée ouverte.

## Fin de piste
À la sortie d'une piste, une piste voisine crédible peut prolonger naturellement le parcours. Sinon, le comportement historique reste actif : services/loisirs, puis remontée.

## Philosophie
Aucune téléportation, aucun réseau routier artificiel, aucune gestion manuelle de chaque skieur. Le système prépare le terrain pour des flux plus naturels et des zones de report dans les futures versions.
