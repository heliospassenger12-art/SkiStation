> Historique du premier correctif. Voir COMMENCER_ICI_PHASE63_FIX2.md pour les règles actuelles.

# SkiStation 2.49 — Phase 63, parcours du visiteur corrigé

Version cumulative issue exclusivement du ZIP fourni `SkiStation_2_49_ARRIVEE_CLIC_EXACT_PHASE63_TEST(1).zip`.
La phase 63 reste la base. Ce document remplace les anciennes consignes PHASE61/62 pour cette livraison.

## Installation
1. Extraire ce ZIP dans un nouveau dossier ; conserver le projet précédent.
2. Ajouter ce dossier dans Unity Hub. Utiliser la version indiquée dans ProjectSettings/ProjectVersion.txt.
3. Ouvrir `Assets/Scenes/SkiStation_Bootstrap.unity`, attendre la compilation puis lancer Play.
4. Conserver la disposition de test habituelle : gare basse près du front de neige (35 m maximum pour le choix initial, pas plus de 3 m au-dessus du visiteur), piste connectée au sommet de la remontée. Les placements ne sont pas déplacés automatiquement.

## Un test en plusieurs étapes
- Sans remontée : regarder les visiteurs suivre la route jusqu'au front de neige. Aucun forfait ne doit être vendu à la création du visiteur.
- Sans magasin : certains visiteurs ont leur matériel ; les autres attendent au front de neige. Le suivi affiche « Attend un magasin de ski ». Ils ne doivent pas être aspirés par une remontée.
- Construire un magasin ouvert avec accès piéton à moins de 80 m : les visiteurs concernés rejoignent la porte, attendent le service puis reviennent au front de neige.
- Construire une remontée et sa piste reliée : le visiteur équipé rejoint une gare accessible. Le premier embarquement de sa journée vend un forfait ; les rotations suivantes ne doivent pas le refacturer.
- Observer sortie de gare haute, descente puis retour. La trajectoire physique phase 63 est conservée.
- Fermer une remontée avec des visiteurs dans sa file : la file doit se libérer et les visiteurs doivent pouvoir chercher une autre remontée accessible. Les passagers déjà en ligne restent soumis à l'arrêt existant de la remontée.
- Supprimer un commerce pendant une visite : retour vers le front de neige, puis nouvelle décision ; pas de départ au ski depuis l'accès supprimé.
- Laisser passer 18 h 30 avec un visiteur sans équipement : il doit pouvoir terminer son attente et repartir, sans être autorisé à skier.

Pour un contrôle technique optionnel, hors Play : menu **SkiStation > Verification > Parcours phase 63 (hors Play)**. Ces tests C# sont fournis, pas exécutés ici.

## Vérification de cette livraison
- 397 fichiers C# analysés : pas d'erreur de syntaxe détectée. Ce contrôle ne remplace pas la compilation Unity.
- Comparaison intégrale avec le ZIP source : 11 scripts existants modifiés, aucune suppression.
- Fichiers de placement des gares, scènes, montagne et arcade identiques à la source.
- Méthodes de déplacement physique de la boucle phase 63 identiques à la source.
- Unity n'est pas installé dans l'environnement de préparation : compilation, rendu, FPS et test complet en Play restent à vérifier sur ton ordinateur.

Le détail des défauts, corrections et limites figure dans `AUDIT_PARCOURS_PHASE63.md`.
