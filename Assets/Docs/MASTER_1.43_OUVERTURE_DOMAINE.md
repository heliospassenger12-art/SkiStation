# SkiStation 1.43 — Ouverture du domaine

La 1.43 transforme la préparation nocturne en conséquence visible au début de la journée.

## Boucle

Météo → neige → trafic → dégradation → damage → garage/équipes → secteurs → préparation nocturne → **ouverture du domaine**.

## Ouverture du matin

À 06:00, un passage d'ouverture évalue les conditions des pistes et des remontées.
Une piste trop dégradée, trop peu damée, verglacée ou soumise à une visibilité trop faible reste fermée.
Les remontées non en panne sont ouvertes si la visibilité permet l'exploitation.

## Verglas lié à l'ombre matinale

Chaque piste possède `morningSunExposure01` (0 = ombre persistante, 1 = soleil matinal).
Par temps froid, l'ombre matinale augmente progressivement le risque de verglas entre 06:00 et 11:00.
Le soleil et le damage réduisent ce risque.

## Philosophie

Pas de microgestion de carburant. Le système reste orienté gestion de station : préparation, priorités, sécurité et qualité d'exploitation.
