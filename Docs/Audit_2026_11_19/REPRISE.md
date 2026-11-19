# Alpine Empires · reprise vérifiée du 19 novembre 2026

**Bloc 2 partiel. Les contrôles métier hors Unity passent. Le passage au Bloc 3 reste NO-GO.**

## Résultats établis

| Contrôle | Résultat | Preuve |
|---|---|---|
| Source Unity | Projet Avant Première extrait avec succès des neuf volumes RAR. Unity 6000.3.7f1. | [Empreintes des neuf volumes](ARCHIVES_RAR_SHA256.json) |
| Correctif Comptes alliés | SHA-256 du manifeste parent conforme. Onze fichiers de base conformes, quatre créations absentes, quinze Payload conformes. | [Comparaison et compatibilité](COMPARAISON_MAIN_SOURCE.json) |
| Tests financiers | Compilation Roslyn et exécution .NET réussies. Seize assertions du banc d'origine passent. | [Provenance](TESTS_FINANCIERS.json) · [Sortie d'exécution](RESULTAT_TESTS_FINANCIERS.txt) |
| Métadonnées de la PR 1 | Les 190 fichiers `.meta` de cette proposition correspondent à ceux de l'archive Avant Première. | [Comparaison des métadonnées](CONTROLE_METADONNEES_PR1.json) |
| Grand Aigle | Les quatre patchs s'appliquent dans l'ordre sur une copie ciblée des sources. Les six fichiers météo finaux correspondent aux sources prêtes à copier de la PR 4. | [Résultats des patchs](CONTROLES_PATCHS_ARCADE.json) |
| La Nocturne | Contrôle, application et deuxième application réussis sur la copie ciblée après les patchs Grand Aigle. La seconde application effectue zéro remplacement. | [Résultats des patchs](CONTROLES_PATCHS_ARCADE.json) |

L'extraction initiale avec libarchive a échoué sur une frontière de volume. Une extraction complète avec UnRAR 7.23 a ensuite terminé avec code 0 et validation des fichiers. Les fichiers comparés proviennent de cette extraction réussie. Le ZIP complet Drive de 2,5 Go dépasse la limite de téléchargement du connecteur, les neuf volumes de moins de 256 Mio fournissent l'accès exploitable au projet. L'empreinte du ZIP d'origine n'a pas été recalculée à partir d'un ZIP complet pendant cet audit.

## Écart avec la racine GitHub

La comparaison est figée au commit `5018af206bbf754fb41f40a4db8ddd8a413862e9`, intégré le 18 novembre à 21 h 49, heure de Paris. Elle porte sur 1 182 blobs suivis par Git et l'archive Avant Première avant application du correctif Comptes alliés.

| Catégorie | Nombre |
|---|---|
| Fichiers suivis identiques à l'archive | 896 |
| Fichiers suivis présents mais différents | 213 |
| Fichiers suivis propres au dépôt | 73 |
| Fichiers Unity dans l'archive, sous Assets / Packages / ProjectSettings | 4 854 |
| Fichiers Unity de l'archive absents du dépôt | 3 844 |

Les fichiers propres au dépôt comprennent les travaux web et les références copiées sous Docs. Le rapport compare la source avant le correctif, puis contrôle séparément les empreintes du correctif. Un même chemin présent dans les deux projets peut contenir des générations différentes de code.

Les marqueurs de version de la racine divergent. Le README antérieur annonce 3.29, `VERSION.txt` annonce 3.04, `SkiStationVersion.Current` annonce 1.85 et `MASTER_VERSION.txt` affiche 2.58. La filiation des livraisons et les empreintes doivent déterminer la base de travail. Les notes du Bloc 2 déclarent Gestion 3.94 et Arcade A2.64 pour le projet récent.

## Obstacles à lever avant la reprise complète

- Deux `.meta` Arcade de l'archive comportent un GUID de 33 caractères. Ils concernent `ArcadeSkiController.ContactReaction.cs` et `ArcadeCarvingVisual.SnowEffects.cs`. Auditer leurs références dans les scènes et ressources avant correction, puis vérifier l'import. Aucun doublon de GUID n'a été trouvé parmi les 2 509 valeurs recensées. [État des GUID](STRUCTURE_SOURCE_UNITY.json)
- L'archive contient environ 4 142 Mio sous Assets. Une scène d'étude atteint 119 346 333 octets et dépasse le seuil ordinaire de 100 Mio par fichier GitHub. Le plan de dépôt doit prévoir ce fichier volumineux et vérifier les conditions de redistribution des ressources tierces avant leur publication. Le kit Comptes alliés versionné ici contient les propres sources du correctif.
- Les anomalies de skieurs immobiles et de disparition après certaines gares amont restent à reproduire. La reprise réelle d'une sauvegarde et les autres catégories d'actifs et de flux économiques du Bloc 2 restent à recetter ou à compléter selon leur statut de livraison.

## Ordre de travail

1. Identifier la copie PC qui correspond à Avant Première et ses éventuelles modifications ultérieures. Utiliser le vérificateur de kit pour comparer les empreintes avant installation.
2. Traiter les deux GUID en conservant leurs références, puis vérifier l'import et la compilation sur une copie complète dans Unity 6000.3.7f1.
3. Installer Comptes alliés sur le parent compatible et effectuer la recette restaurant B, coûts, ventes, charges, rejeu et sauvegarde/reprise. Consigner les cas des visiteurs et remontées.
4. Préparer la reprise du projet Unity complet à partir de cet état vérifié, avec une stratégie explicite pour les gros fichiers et ressources tierces. Garder les livraisons web et Unity identifiables.
5. Intégrer les lots Arcade sur cette base suivant leur ordre vérifié et recetter leurs effets dans Unity. Leurs applications hors moteur sont déjà contrôlées.
6. Réévaluer les quinze points du Bloc 2. Ouvrir le Bloc 3 après validation du périmètre et de la recette.

Le vérificateur [verify.py](../../Integrations/AlpineEmpiresBloc2/verify.py) régénère un inventaire détaillé à partir du projet complet et d'une révision Git. Les contrôles de cette livraison documentent le code et les données examinés. Compilation Unity, Play Mode, rendu, durée réelle, sauvegarde disque en jeu et réseau restent **non exécutés**.

[Examen des trois PR existantes](REVUE_PR.md) · [Kit Comptes alliés](../../Integrations/AlpineEmpiresBloc2/README.md)
