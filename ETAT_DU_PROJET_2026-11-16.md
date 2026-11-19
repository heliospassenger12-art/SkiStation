# État du projet SkiStation au 16 novembre 2026

> Le ZIP Gestion 3.93 a été reçu après ce bilan. Voir [la comparaison et le plan de reprise](Documentation/Gestion_3_93/REPRISE.md).

Ce document rapproche le contenu du dépôt du [guide de démarrage 3.29](COMMENCER_ICI_WORK_3_29.md) et du bilan SkiStation du 15 novembre. Les essais historiques et le candidat de novembre concernent d'autres versions : ils ne valident pas la branche principale.

## Ce qui est dans le dépôt

La base de code est **SkiStation 3.29**, au commit [`112285f8`](https://github.com/heliospassenger12-art/SkiStation/commit/112285f89e44110da1fd9b3db5afa7d0f5f68712) du 26 septembre 2026. Elle contient les scripts de décision des visiteurs à partir de la faim, de l'énergie et du temps restant, ainsi que de repos et de repas près du front de neige. Le guide 3.29 documente les contrôles syntaxiques et structurels effectués à la livraison. La compilation dans Unity et le mode Play restent à vérifier pour cette version ; voir [les tests 3.29](TEST_329.md).

## Travaux récents à rapprocher du dépôt

| Brique | Preuve disponible | État du rapprochement |
| --- | --- | --- |
| Arcade, essais historiques A2.64 | Le bilan du 15 novembre relève des essais en mode Play : contact au sol, figures et réceptions, score et boost, rails, bouton `Retry` et montée suivie d'un débarquement avec reprise du contrôle. Les contrôles de l'outillage et ses exports CSV/JSON sont documentés séparément. | Ces résultats concernent les versions essayées. Ils restent à reproduire sur l'assemblage récent. |
| ValAlpin et IA Visiteurs 2.0, bloc 1 | L'ouverture de ValAlpin a été observée. Des visiteurs restent immobiles ; sur certaines remontées, d'autres disparaissent en gare haute. Le bilan fait état de corrections dans le candidat. | Corrections à retester sur le candidat ; elles n'ont pas été importées par cette mise à jour. |
| Glisse 3.0 | Le candidat est décrit dans le bilan. Aucun essai de conduite n'y est confirmé. | Sources récentes à récupérer, puis faire deux descentes sur la même piste pour comparer les commandes, le freinage et le contact au sol. |
| Alpine Empires, bloc 2 partiel | Le bilan décrit des structures pour quatre compagnies, leurs comptes, les concessions et la sauvegarde de session. | Aucun test confirmé reliant une construction, des revenus visiteurs ou une dette au solde. Le candidat n'est pas importé ici. |
| Massif et trois scènes de test | La décision est de tester les physiques dans des scènes séparées avant de les réunir. Un relief pixelisé ou lointain et des matériaux roses ont été signalés. | Contenu et rendu des scènes récentes à vérifier. La fusion du massif, le snowscoot jouable, la sauvegarde globale, le multijoueur et la diplomatie restent à confirmer ou à développer. |

Le ZIP ou le dossier Unity de la version commune n'était pas disponible pour ce rapprochement. Cette mise à jour documente donc les travaux récents sans enregistrer de nouvelles scènes, de nouveaux scripts ou de nouveaux résultats de test.

## Reprise dans Unity

1. **Vérifier la base GitHub.** Dans Unity Hub, ouvrir une copie de la 3.29 avec la version indiquée dans [`ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt). Attendre la fin de l'import, ouvrir la Console et vérifier qu'il n'y a aucune erreur rouge. S'il y en a, copier le texte de la première avant de poursuivre.
2. **Récupérer le candidat récent.** Utiliser le dernier ZIP complet ou le dossier qui contient `Assets/`, `Packages/` et `ProjectSettings/`. Comparer avec la 3.29 sur une branche d'intégration. Garder les fichiers `.meta` qui accompagnent les scènes et les assets ; laisser de côté les dossiers générés par Unity, comme `Library/` et `Temp/`.
3. **Valider une scène de ski.** Faire apparaître le skieur, tourner, freiner, terminer une courte descente puis relancer avec `Retry` ou reprendre une remontée. Refaire la descente sans traverser le terrain et sans nouvelle erreur rouge. Comparer ensuite les physiques dans leurs scènes de test avant de les réunir.
4. **Tester les deux blocages de gestion séparément.** Suivre un visiteur jusqu'à la gare haute et vérifier qu'il repart. Dans une session Alpine Empires, tester une action payante si elle est disponible : vérifier le solde avant et après, puis après rechargement. Si aucune action n'est raccordée, commencer par en relier une.

Pour chaque essai, noter la version du paquet, le nom de la scène et le résultat. En cas de problème, garder une capture et le texte de la première erreur rouge.
