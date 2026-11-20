# SkiStation

Le dépôt regroupe le prototype web et la reprise du projet Unity. Leurs versions et leurs validations sont suivies séparément.

| Chantier | État vérifié le 19 novembre 2026 | Point d'entrée |
|---|---|---|
| Prototype web | Derniers ajouts intégrés le 18 novembre. Publication GitHub Pages réussie sur le commit `5018af2`. | [Jouer](https://heliospassenger12-art.github.io/SkiStation/) · [Guide web](Web/README.md) |
| Unity à la racine | Base historique documentée comme livraison 3.29. Les marqueurs de version hérités divergent. | [Démarrage historique](COMMENCER_ICI_WORK_3_29.md) · [Tests historiques](TEST_329.md) |
| Alpine Empires Unity | Bloc 2 partiel. Archive Avant Première retrouvée et compatibilité du correctif Comptes alliés vérifiée. Recette Unity attendue, passage au Bloc 3 fermé. | [Audit et prochaine action](Docs/Audit_2026_11_19/REPRISE.md) · [Kit Comptes alliés](Integrations/AlpineEmpiresBloc2/README.md) |
| Intégrations Gestion et Arcade | Trois PR de reprise ouvertes. Métadonnées Gestion et applications de patchs Arcade contrôlées sur l'archive retrouvée. | [Examen des trois PR](Docs/Audit_2026_11_19/REVUE_PR.md) |

Orcières Merlette dispose d’un kit Avant Première pour l’eau saisonnière, les activités des Estaris et les franchissements. [Kit Orcières et tests Unity](Integrations/OrcieresEauSaisons/README.md). Le même système est disponible pour [Auron et la carte fusionnée des Trois Vallées](Integrations/AuronTroisValleesHydro/HYDROGRAPHIE.md).

Le kit Bloc 2 est conservé dans `Integrations/AlpineEmpiresBloc2`, avec ses sources et son installateur d'origine. Son contrôle de filiation cible le projet Avant Première contenant le complément Sauvegarde matérielle. L'arborescence Unity historique à la racine doit suivre le plan de rapprochement avant d'accueillir ces changements.

Les 16 assertions métier du kit Comptes alliés ont été compilées et exécutées hors Unity. La compilation Unity, les scènes en Play Mode, les sauvegardes réelles et le réseau restent à valider. Le [rapport d'audit](Docs/Audit_2026_11_19/REPRISE.md) précise la provenance des sources et le périmètre de chaque contrôle.

[Cahier des charges historique](CAHIER_DES_CHARGES_WORK_SKISTATION.md)
