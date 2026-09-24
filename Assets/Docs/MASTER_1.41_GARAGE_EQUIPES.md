# SkiStation 1.41 — Garage & équipes de piste

Cette brique prolonge 1.38 météo, 1.39 état des pistes et 1.40 opérations/damage.

## Principes
- Les dameuses sont des actifs de station hébergés dans un garage.
- Les conducteurs sont regroupés en équipes: on évite la microgestion individuelle.
- Les équipes peuvent fonctionner en journée, de nuit ou en horaires flexibles.
- L'efficacité de l'équipe influence la vitesse des opérations.
- Aucun carburant n'est simulé.

## Boucle
Météo -> état de neige -> trafic -> dégradation -> priorités -> équipes -> dameuses -> préparation du domaine -> qualité du matin.

## Suite logique
1. Affectation des équipes aux secteurs.
2. Préparation du domaine avant ouverture.
3. Patrouilleurs/sécurité et fermeture de pistes.
4. Réaction des visiteurs aux conditions et aux ouvertures.
