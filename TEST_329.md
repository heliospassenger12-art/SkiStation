# Tests Unity 3.29 — à exécuter

Aucun test Unity déclaré exécuté ici. Ouvrir une copie du projet complet avec la version de ProjectSettings/ProjectVersion.txt, attendre la compilation et relever toute erreur rouge.

1. Menu SkiStation > Visiteurs > Verifier les decisions 3.29 : neuf cas couvrent départ prioritaire, manque d'énergie, repas disponible/absent, trajet trop long, seuil exact et absence de chemin. Ce menu ne simule pas les déplacements.
2. Dans Bootstrap, sélectionner un visiteur ordinaire après son arrivée et ses services d'accueil. La fiche affiche faim, énergie, décision et raison. Le marqueur rose de la boucle témoin garde son fonctionnement historique.
3. Pause : les valeurs et durées ne progressent plus. Comparer une même durée simulée à x1, x3 et x8 : progression cohérente, sans multiplier deux fois les besoins. Les déplacements physiques suivent les commandes habituelles.
4. Faire terminer plusieurs pistes : fatigue et faim augmentent. Une marche vers la remontée ne constitue pas du repos. À faible énergie, au front de neige, le visiteur s'arrête pour une pause de 10 minutes de jeu puis reprend sa décision. Il ne doit pas être rappelé à pied depuis le sommet.
5. À faim élevée, ouvrir un restaurant proche, accessible à pied, avec personnel, stock et places/file disponibles. Le visiteur marche jusqu'à lui, attend si nécessaire puis mange. La faim ne doit baisser qu'après le service réel. L'état de caisse ne doit recevoir qu'un paiement pour ce service.
6. Fermer le restaurant pendant l'approche ou l'attente ; tester aussi la suppression du bâtiment et une file trop longue. La visite est annulée sans repas ni recette, la capacité est libérée et le visiteur reprend une décision. Un restaurant sur une falaise ou en altitude ne doit pas attirer un trajet direct.
7. Avec peu de temps restant (modifier l'heure de départ du composant VisitorDayPlan328 pendant Play si besoin), proposer un long itinéraire GPS à plusieurs remontées. Depuis la base, le visiteur refuse un nouveau cycle qui ne tient pas avec la réserve de retour. À égalité exacte du budget, le trajet peut être accepté.
8. Pendant une chaîne de remontées/pistes, observer la cible GPS persistante : une envie de repas ne doit pas interrompre une descente ou déplacer un passager hors de son véhicule.
9. Vérifier retour normal et retour vers un hébergement réservé, puis reprise le lendemain sans boucle de retour immédiate. Vérifier que les réserves de faim/énergie sont réinitialisées pour le nouveau jour.
10. Refaire arrivée voiture/autocar/piéton, équipement/forfait, boucle piste-remontée, puis ouvrir Arcade séparément. Tester plusieurs visiteurs en file restaurant pour vérifier le passage du suivant après fin ou annulation d'un service.

Limites : estimation du chemin vers la piste + réserve fixe de retour, pas de routage universel vers les services ; restaurants locaux seulement ; distances de marche contrôlées sur le relief sans contournement complet des bâtiments. Les pannes avec passagers restent gérées par le système existant.
