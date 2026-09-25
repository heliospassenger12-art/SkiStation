# Test Unity 3.05 — GPS ordonné

Statut : protocole à exécuter, aucun résultat Play Mode revendiqué.

1. Ouvrir le projet complet, attendre la compilation et vérifier l'absence d'erreurs rouges. Ouvrir Bootstrap puis lancer en vitesse 1x.
2. Vérifier d'abord la boucle témoin existante : arrivée, vraie remontée, descente. Le marqueur rose seul ne valide pas le GPS ; choisir ensuite un visiteur ordinaire dans la fiche visiteurs, dont SkierRouteAgent est actif.
3. Construire une chaîne A (remontée), P (piste intermédiaire), B (remontée), Q (piste cible). Ouvrir les installations. Les gares/pistes doivent se raccorder réellement : proximité des extrémités et marche possible sur le relief. Pour un essai déterministe, choisir un visiteur dont la fiche affiche Q comme piste souhaitée. Observer l'ordre A → P → B → Q. Il ne doit pas rejoindre B à pied à travers la montagne en sautant P.
4. Construire deux remontées consécutives avec une correspondance marchable de moins de 28 m entre le sommet de la première et le départ de la seconde. Vérifier marche, file réelle et deuxième embarquement sans piste obligatoire entre les deux.
5. Ajouter un second chemin vers Q. Charger la file de la première option ; observer de nouveaux choix aux points de décision, même cible Q tant qu'elle reste accessible. Les visiteurs déjà en file/assis ne doivent pas être téléportés sur l'autre chemin.
6. Fermer une remontée avant que le visiteur y entre. Si une autre chaîne atteint Q, la cible reste Q ; sinon une autre cible accessible est choisie, ou le visiteur attend. Réouvrir et vérifier la reprise après reconstruction du réseau. Tester aussi une panne avant embarquement. Le comportement d'un véhicule en panne avec passagers reste celui de la 3.04.
7. Pour un débutant, rendre Q accessible uniquement par une piste noire intermédiaire : cette chaîne doit être refusée même si Q est facile. Refaire avec un profil autorisé à parcourir les pistes noires.
8. Fermer la cible, puis toutes les installations. La fiche ne doit pas conserver une ancienne destination fermée ; le visiteur attend si aucun trajet n'existe. Réouvrir une chaîne puis observer la reprise. Tester également la suppression d'une remontée en cours d'approche et pendant l'attente.
9. Contrôler les arrivées voiture/autocar/piéton, les locations/forfaits, la fin de journée, puis ouvrir séparément la scène Arcade pour vérifier l'absence de régression.
10. Avec plusieurs dizaines de visiteurs, observer la Console et le Profiler. Noter la fréquence des recalculs et les éventuels pics CPU avant d'augmenter la fréquentation.

À rapporter : capture de la fiche (piste souhaitée et GPS), profil du visiteur, état des installations, étape bloquée et texte complet de toute erreur Console.
