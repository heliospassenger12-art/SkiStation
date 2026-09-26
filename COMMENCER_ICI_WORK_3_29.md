# SkiStation 3.29 — Besoins et décisions de journée

Base : ZIP officiel SkiStation_3_28_BASE_WORK_CAHIER_DES_CHARGES fourni par Hélios. Les travaux 3.07 inachevés ne sont pas repris. Lire le cahier des charges conservé et TEST_329.md.

## Cette brique
- Réutilisation d'IntelligentSkiDayRuntime pour la faim et l'énergie : elles évoluent désormais avec les minutes de GameClock, sans horloge parallèle. Pause et accélération passent par l'horloge existante.
- Raccordement aux fins réelles de piste et de remontée. Marcher avec l'état technique Resting ne rend plus artificiellement de l'énergie.
- Décision Skier / Manger / Repos / Rentrer au pied du domaine via VisitorDayDecision329. La faim, l'énergie, le profil, le temps restant et les possibilités locales participent au choix.
- Le budget de temps est vérifié sur le chemin GPS complet vers la piste cible, marche et attente estimée comprises, avant de lancer un cycle depuis le front de neige. Une réserve de retour reste ajoutée. Le calcul n'inclut pas encore un chemin GPS de retour complet depuis toute destination.
- Pause réelle de 10 minutes de jeu. Les repas utilisent des restaurants réels proches, ouverts, dotés de capacité/file et accessibles sur le relief à moins de 250 m, au niveau du village. Ils se terminent via StationServiceRuntime ; aucune baisse de faim par simple proximité.
- Les restaurants éloignés ou en altitude ne sont pas encore reliés par le GPS de services : travail prévu en 3.30. CanWalk contrôle le terrain, pas les obstacles de bâtiments ; ce n'est pas un NavMesh piéton complet.
- Le retour réutilise VisitorLifecycleRuntime, y compris le séjour en hébergement. Aucune téléportation ajoutée. Pas d'interruption d'un trajet en altitude, d'une file de remontée ou d'une descente pour aller manger.
- Le GPS conserve sa piste cible lors d'une pause/repas. Les anciennes décisions de service ne peuvent plus donner un ordre de déplacement concurrent aux visiteurs pris en charge par 3.29.
- Fiche visiteur : décision, raison, faim, énergie, minutes disponibles. L'ancien plan journalier devient un observateur pour ces visiteurs.

## Corrections liées aux services
Suppression du repas fictif accordé en passant près d'un marqueur de destination. Suppression du double crédit d'une consommation par StationServiceRuntime et VisitorServiceConsumptionRuntime. Conservation des catégories de revenus (restauration, locations, cours…). Une visite annulée libère la capacité, la file peut reprendre ; un repas annulé ne donne ni nourriture ni recette.

## Préservation
Aucune modification du terrain, des scènes, du placement phase 63, des vitesses des remontées, de la boucle témoin ou des fichiers Arcade. Aucun GOAP ajouté. La boucle témoin rose n'est pas une validation des décisions des visiteurs ordinaires.

## Validation
Contrôle syntaxique de 512 fichiers C#, comparaison des systèmes préservés avec le ZIP 3.28 et intégrité du ZIP. Les tests C# du menu SkiStation > Visiteurs > Verifier les decisions 3.29 sont fournis mais NON exécutés ici. Compilation Unity, Play Mode, repas/file/retour physiques et profilage restent à faire sur PC.

## Suite
Tester la 3.29 avant d'élargir le système. Puis 3.30 : véritables itinéraires piétons et services distants via le domaine, estimation du retour, évolution des coûts avec les files. Ne pas modifier le massif avant son test visuel. GOAP vient seulement ensuite si utile.
