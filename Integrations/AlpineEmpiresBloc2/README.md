# Alpine Empires · Comptes alliés · kit de reprise

**Bloc 2 partiel. Recette Unity en attente. NO-GO Bloc 3.**

Ce dossier versionne le contenu intégral du ZIP `SkiStation_Installer_Comptes_Allies.zip` retrouvé sur Drive. Les fichiers du kit d'origine sont conservés à l'octet. Le présent guide et `verify.py` complètent cette livraison.

Le correctif relie un restaurant de l'entreprise B aux coûts, ventes, charges et reçus persistants de B. Le [rapport de livraison](Payload/ALPINE_EMPIRES_BLOC_2_COMPTES/REPRISE_COMPTES_ALLIES.md) décrit le périmètre fonctionnel et les limites restantes.

## Base vérifiée

La source est le projet **SkiStation Alpine Empires Avant Première**, Unity **6000.3.7f1**. Ses neuf volumes RAR ont été téléchargés à partir du dossier Drive, empreintés et extraits avec contrôle d'intégrité. Le manifeste Sauvegarde matérielle correspond au SHA-256 exigé par l'installateur. Les 11 fichiers à remplacer correspondent à leurs empreintes parentes, et les quatre fichiers à créer sont absents. Les 15 fichiers du Payload correspondent aux empreintes de sortie.

[Dossier source Drive](https://drive.google.com/drive/folders/1BPfldyEiuiEp6yQlPjEFLwVh3jG1MbiE) · [Installateur d'origine](https://drive.google.com/file/d/1wI0akQgfurGU7O_xyUjT2mdu_EjCDlZy/view)

## Vérifier un projet avant installation

Avec Python 3.11 ou ultérieur, depuis la racine de ce dépôt, utiliser une copie complète du projet Avant Première et choisir un fichier de rapport situé hors de ce projet.

```sh
python Integrations/AlpineEmpiresBloc2/verify.py --project "/chemin/Avant_Premiere" --repository . --ref main --output "/chemin/rapport-audit.json"
```

Le script lit le projet et la révision Git demandée. Il vérifie le manifeste parent, l'éditeur et les fichiers du kit, distingue une installation complète d'un état mélangé ou divergent et produit la comparaison par chemin avec GitHub. Il renvoie 0 pour un parent compatible ou un correctif entièrement appliqué, 2 pour une divergence et 1 pour une erreur de lecture ou d'argument. Son succès établit la compatibilité des empreintes. La recette en jeu reste distincte.

Après validation de la base et des anomalies recensées dans l'audit, l'installateur Windows fourni se lance sur la copie complète contenant le complément Sauvegarde matérielle. Il crée sa sauvegarde des fichiers remplacés et refuse les écarts de filiation. Le script PowerShell lui-même n'a pas été exécuté pendant l'audit du 19 novembre. L'installation modifie cette copie du projet et doit être suivie de la recette Unity décrite dans le rapport de livraison.

## Tests métier

Les fichiers `Payload/Tools/AlpineEmpiresFinanceChecks/` contiennent le banc d'origine. Après installation sur le projet complet compatible, son point d'entrée est disponible à la racine de ce projet.

```sh
dotnet run --project Tools/AlpineEmpiresFinanceChecks/Checks.csproj
```

Le 19 novembre, les sources du banc, `AlpineSession` candidate et les trois dépendances natives de l'archive ont été compilées avec Roslyn du SDK .NET 8.0.428 puis exécutées avec .NET 8.0.34. Les **16 assertions** passent. Le banc utilise les miroirs de données `StationState` et `CustomerProfile` qu'il fournit déjà. Le détail des empreintes et la limite liée à `dotnet run` dans l'environnement d'audit figurent dans [la preuve de test](../../Docs/Audit_2026_11_19/TESTS_FINANCIERS.json).

[Audit, écarts et prochaine action](../../Docs/Audit_2026_11_19/REPRISE.md)
