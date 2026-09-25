# SkiStation 3.01 — Navigation multi-remontées

Base : 3.00.

## Ajout
- Un visiteur peut désormais conserver une destination de piste plus lointaine sur le domaine.
- Le planificateur exploite le graphe réel `IntegratedSkiDomainNetworkRuntime` : pistes, remontées et transferts existants.
- Recherche de chemin à coût minimal (temps de remontée, longueur/affluence des pistes, transferts).
- Le prochain choix de remontée et de piste est injecté dans `SkierRouteAgent` sans remplacer les files, trajets de remontée ni mouvements existants.
- Filtre de difficulté conservé selon le profil du visiteur.
- Si aucun chemin multi-remontées valable n'existe, le comportement historique reprend automatiquement la main.
- Une destination atteinte est libérée afin que le visiteur puisse choisir un nouvel objectif.

## Test Unity conseillé
1. Créer au moins 2 remontées en chaîne et plusieurs pistes reliées.
2. Lancer Play et laisser plusieurs visiteurs skier.
3. Vérifier qu'un visiteur peut enchaîner plusieurs remontées/pistes pour progresser dans le domaine au lieu de toujours reboucler localement.
4. Fermer une piste ou mettre une remontée en panne : vérifier que le système retombe sur une route utilisable/comportement existant.
5. Vérifier qu'aucun visiteur n'est envoyé sur une difficulté manifestement incompatible.
6. Vérifier la Console : aucune erreur rouge.

⚠️ Compilation et Play Mode Unity non exécutés dans l'environnement de génération.
