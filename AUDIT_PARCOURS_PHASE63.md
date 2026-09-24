> Historique du premier correctif. Voir COMMENCER_ICI_PHASE63_FIX2.md pour les règles actuelles.

# Audit du parcours — base phase 63

Audit de code du ZIP fourni, sans exécution Unity. Les lignes suivantes décrivent des défauts observés dans le code et les corrections apportées, pas des résultats de jeu mesurés.

| Étape | Défaut constaté | Correction |
|---|---|---|
| Apparition | Lecture de roadPath[0] sans vérifier la longueur ; configuration invalide susceptible de finir l'arrivée | Route de moins de deux points refusée, aucun autre point d'apparition de secours |
| Route | La boucle VisibleSkiLoopAuthorityRuntime sélectionnait un skieur dans l'état Arriving et arrêtait son mouvement | Sélection après arrivée terminée et matériel acquis, au repos, près de la gare ; aucun contrôle de cette boucle sur un visiteur encore sur la route |
| Première décision | L'attente sans équipement (phase 6) passait dans le détecteur de blocage toutes les 12 secondes | Phase d'attente exclue ; recherche de magasin conservée toutes les 5 secondes |
| Commerce | Suppression ou blocage pouvait terminer la visite à l'endroit courant | Retour vers le terminus de la route avant autorisation de skier ; bâtiments non accueillants exclus des visites facultatives |
| Fin de journée | L'attente sans matériel empêchait le cycle de départ, car IsArriving restait vrai indéfiniment | À partir de 18 h 30, après parcours routier terminé, libération vers le cycle de départ et interdiction de skier |
| Économie | Forfait vendu dès la création, même sans remontée ni matériel | Facturation au premier embarquement, au plus une fois par jour de jeu ; même garde dans la boucle de démonstration |
| Première remontée | Une remontée inaccessible choisie par le score pouvait empêcher l'examen d'une alternative ; secours possible sans piste desservie | Validation avant sélection, recherche de secours parmi les remontées accessibles et desservant une piste ouverte |
| File | Le visiteur était retiré avant TryBoard ; réinscription refusée car déjà WaitingForLift | Essai depuis la tête sans retrait, retrait seulement après embarquement réussi |
| File | Tête détruite et places réservées obsolètes pouvaient bloquer ou désaligner les suivants | Nettoyage des têtes détruites, redistribution des réservations de file |
| Fermeture | File conservée alors que le contrôleur quittait Update dès fermeture/panne | Libération de la file, des réservations et retour au repos pour nouvelle décision |
| Descente / transfert | Réutilisation d'un transfert désactivé sans réactivation explicite dans Begin | Réactivation à chaque nouveau transfert |
| Bas de piste | La recherche périodique de remontée pouvait concurrencer une décision de terminal | Respect de la décision active avant toute reprise automatique de recherche |
| Départ | L'état Leaving choisi au terminal n'activait pas forcément le parcours de départ | Prise en charge explicite par VisitorLifecycleRuntime |
| Diagnostic | Tous les visiteurs sur route, en boutique ou sans équipement affichaient simplement Arriving | Libellé du sous-état d'arrivée dans le panneau de suivi existant |

## Ce qui est conservé
Le calcul de placement des gares n'est pas modifié. Les méthodes MoveToBoarding, MoveLinear et MoveWaypointByWaypoint de la boucle phase 63 sont conservées octet pour octet. Leur sélection initiale est corrigée : paire remontée/piste connectée, accès local et visiteur prêt. Les scripts arcade, le relief, les scènes et les outils de construction ne sont pas modifiés.

## Limites identifiées et non remplacées dans cette livraison
- La boucle visible phase 63 reste une autorité spéciale pour UN skieur : elle suit la ligne de remontée sans utiliser les sièges et la file normale. Son succès ne prouve pas à lui seul le fonctionnement de tous les autres visiteurs. La facturation intervient à son entrée dans la phase de montée. Les autres visiteurs utilisent la file et les véhicules.
- Le réseau de choix reste fondé sur des distances (connexion piste/remontée : 28 m ; approche de gare : 35 m et montée de 3 m maximum), pas sur un véritable moteur de navigation avec obstacles. La marche vers la gare et certains commerces peut encore couper le terrain. Cette version ne prétend pas créer un réseau piéton complet.
- L'arrivée actuelle est une marche le long du chemin routier enregistré. Aucun nouveau système de voitures ou de dépose n'est ajouté dans ce correctif.
- La visite facultative d'un commerce est encore une décision simple et aléatoire ; elle ne constitue pas un plan de journée complet selon les besoins.
- La réservation d'hébergement demeure attribuée à la fin du parcours routier par le système existant.
- Le départ existant supprime le visiteur au point de sortie du front de neige ; le trajet inverse complet jusqu'au bord de carte n'est pas ajouté.
- Une remontée arrêtée avec un passager en ligne conserve son fonctionnement existant : arrêt du véhicule. Pas de système d'évacuation ajouté.
- Les forfaits sont enregistrés sur le visiteur en mémoire pour la journée de jeu. Pas de nouvelle persistance des sauvegardes.
- Les contrôles n'établissent ni compatibilité de difficulté avec chaque niveau de skieur ni absence de collision sur tous les tracés possibles.

## Contrôles
`Checks/verify_phase63.py` analyse la syntaxe C# et compare au ZIP source. Résultats joints dans `Checks/RESULTATS_PHASE63.txt`.
Le menu Editor ajouté vérifie les gardes route/matériel, la non-duplication d'une file, le maintien en file si un siège est plein et le nettoyage d'une tête détruite. Il doit être exécuté dans Unity ; il n'a pas été lancé ici.
Le scénario du document COMMENCER_ICI teste le parcours visible et les interruptions qui ne peuvent pas être validés par une simple inspection statique.
