# Cahier des charges · SkiStation Arcade Unity

État au 17 novembre 2026. Base de travail observée : **SkiStation Alpine Empires Avant Première**, Unity `6000.3.7f1`. Entrée de référence : menu **Arcade** → **Grand Aigle**. La scène Unity s’appelle `SkiStation_Arcade_Test_A2_46` pour des raisons historiques. Le [cahier des charges général](../../CAHIER_DES_CHARGES_WORK_SKISTATION.md) fixe l’objectif de descente libre sur le domaine construit.

## Expérience attendue et état

| Lot | Exigence | État vérifié |
| --- | --- | --- |
| Lancement et boucle de jeu | Choisir un rider, descendre et utiliser Retry ou remonter physiquement pour repartir. Garder des pistes dégagées. | Grand Aigle et le départ Arcade sont présents dans les sources de l’archive. Retry, rails et remontée avec débarquement ont été essayés sur des versions historiques. À rejouer sur le candidat actuel. |
| Glisse, corps et caméra | Carver, freiner, charger un saut, tenir une figure et absorber la réception avec des positions plausibles. Caméra lisible pendant la descente et le vol. | Contact, figures et réceptions essayés historiquement. Hybrid Motion et le candidat Glisse 3.0 sont présents dans le code et les laboratoires. Leur comportement reste à vérifier sur la version actuelle. Le portage des traînées et de la caméra est en [PR nº 3](https://github.com/heliospassenger12-art/SkiStation/pull/3), encore ouverte. |
| Score et boost | Récompenser une figure reçue une seule fois. Les ondes du boost partent des mains et la couleur suit le palier de réserve. | Les trois phases et les multiplicateurs sont présents dans les sources Unity du candidat. Les effets aux poignets restent à valider en Play Mode. |
| Direction visuelle | Se rapprocher de la lisibilité et de la sensation de vitesse de SSX sur PS3, avec un rider détaillé, neige damée et poudreuse crédibles, sapins lisibles, éclairage, reflets et flares cohérents. | Le [correctif neige Grand Aigle](../../Integrations/ArcadeUnity/SkiStation-Unity-Arcade-Grand-Aigle.patch) ajoute un rendu URP à la neige damée et au terrain rocheux. Application à l’archive vérifiée. Visuel réel, rider, forêt, éclairage et coût GPU à évaluer dans Unity. |
| Terrain partagé avec la Gestion | Skier dans le domaine, utiliser les pistes et les remontées de la station, puis reprendre la descente. | Pont Gestion/Arcade et entrée ValAlpin présents dans les sources. Le parcours complet sur une base commune et la reprise de session restent à exercer dans Unity. |
| Extensions | Prévoir les compétitions et visites en ligne. Ajouter le snowscoot quand son pilotage dédié est prêt. | Le candidat contient des données et des règles de progression du snowscoot. Le contrôleur dédié et le multijoueur restent à livrer et à valider. |

## Barème officiel du boost

| Palier | Réserve | Croisière max. | Vitesse max. sous boost | Accélération du boost | Résistance de la neige | Figures |
| --- | --- | --- | --- | --- | --- | --- |
| Jaune | 0 à moins de 100 % | ×1,00 | ×1,00 | ×1,00 | ×1,00 | Classiques |
| Vert | 100 à moins de 200 % | ×1,08 | ×1,16 | ×1,35 | ×0,94 | Avancées selon les animations disponibles |
| Rouge | À 200 % | ×1,18 | ×1,48 | ×2,30 | ×0,86 | Signatures selon leurs conditions |

La jauge comprend deux segments de 100 points. Une tentative réussie et réceptionnée rapporte au plus 75 points. La commande consomme la réserve en jaune et en vert. À 200 %, le rouge démarre automatiquement pour 20 secondes de jeu actif. Durant cette phase, la commande doit être maintenue pour obtenir la poussée et la réserve est illimitée. Une chute ou l’expiration ramène la réserve à 120 %. Les plafonds du moteur s’appliquent aux multiplicateurs.

L’affichage de la figure en cours utilise son propre barème. Blanc de 0 à 1 999 points, jaune de 2 000 à 3 999, orange de 4 000 à 5 999, rouge à partir de 6 000.

## Prochaine validation

1. Ouvrir une copie du [candidat Unity Alpine Empires Avant Première](https://drive.google.com/file/d/1hezjqm5UQTBw4u6NRRHZV_WkHPYd6w7o/view?usp=drivesdk) dans Unity `6000.3.7f1`. Vérifier l’import et la Console. Lancer Grand Aigle depuis le menu Arcade.
2. Rejouer une boucle complète : descente, figure reçue, boost, chute, Retry, remontée et débarquement. Vérifier les seuils 100 % et 200 %, les 20 secondes actives, le retour à 120 % et l’ancrage des ondes en mouvement.
3. Comparer Grand Aigle avant et après le correctif sur le même trajet et avec les mêmes réglages. Capturer neige, sapins et rider au départ, en virage et à la réception. Relever le temps d’image et les erreurs de shader sur le PC cible.
4. Tester le parcours Gestion → Arcade → Gestion sur la version commune. Rattacher chaque résultat à une scène, un commit ou une archive et un résultat Play Mode.

## Suivi GitHub

Chaque lot de modifications effectué sur Arcade Unity est poussé sur GitHub sur une branche de travail. La PR correspondante indique le changement, la version Unity visée et les essais réellement effectués. Mettre cette page à jour quand une exigence, son état ou sa preuve change.

Le dépôt de reprise est encore basé sur le code 3.29. Les sources récentes d’Alpine Empires Avant Première sont dans l’archive ci-dessus. Le correctif Grand Aigle est donc livré sous forme de patch tant que le projet Unity correspondant n’est pas intégré au dépôt. Depuis la racine du projet extrait (dossier qui contient `Assets` et `ProjectSettings`), utiliser `git apply --check <chemin-du-patch>` puis `git apply <chemin-du-patch>`. La [reprise Gestion nº 1](https://github.com/heliospassenger12-art/SkiStation/pull/1) et les [essais historiques](../../ETAT_DU_PROJET_2026-11-16.md) donnent le contexte de version.
