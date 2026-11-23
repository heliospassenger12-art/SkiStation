# SkiStation

Le dépôt regroupe le prototype web et la reprise du projet Unity. Leurs versions et leurs validations sont suivies séparément.

| Chantier | Dernier état vérifié | Point d'entrée |
|---|---|---|
| Prototype web | Derniers ajouts intégrés le 18 novembre. Publication GitHub Pages réussie sur le commit `5018af2`. | [Jouer](https://heliospassenger12-art.github.io/SkiStation/) · [Guide web](Web/README.md) |
| Unity à la racine | Base historique documentée comme livraison 3.29. Les marqueurs de version hérités divergent. | [Démarrage historique](COMMENCER_ICI_WORK_3_29.md) · [Tests historiques](TEST_329.md) |
| Alpine Empires Unity | Recette locale partielle du 23 novembre sur Avant Première. 154 tests Edit Mode réussis, build Windows démarré. Réseau non testé. | [Correctifs et preuves](Integrations/AlpineEmpiresRecetteLocale/README.md) · [Reprise du Bloc 2](Docs/Audit_2026_11_19/REPRISE.md) |
| Intégrations Gestion et Arcade | Trois PR de reprise ouvertes. Métadonnées Gestion et applications de patchs Arcade contrôlées sur l'archive retrouvée. | [Examen des trois PR](Docs/Audit_2026_11_19/REVUE_PR.md) |

Le kit Bloc 2 est conservé dans `Integrations/AlpineEmpiresBloc2`, avec ses sources et son installateur d'origine. Son contrôle de filiation cible le projet Avant Première contenant le complément Sauvegarde matérielle. L'arborescence Unity historique à la racine doit suivre le plan de rapprochement avant d'accueillir ces changements.

L’audit du 19 novembre a exécuté 16 assertions métier du kit Comptes alliés hors Unity. La recette suivante, sur la copie locale Avant Première corrigée, couvre aussi un parcours Gestion–Arcade, des remontées et un démarrage Windows. La qualification Beta et les essais réseau restent ouverts. [Audit du 19 novembre](Docs/Audit_2026_11_19/REPRISE.md) · [Matrice d’acceptation locale](Integrations/AlpineEmpiresRecetteLocale/RESULTATS.md)

[Cahier des charges historique](CAHIER_DES_CHARGES_WORK_SKISTATION.md)
