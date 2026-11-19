# Reprise de SkiStation Gestion 3.93

La [PR nº 1](https://github.com/heliospassenger12-art/SkiStation/pull/1) reprend la branche `codex/suivi-skistation-2026-11-16` depuis `main` au commit `112285f89e44110da1fd9b3db5afa7d0f5f68712` (Gestion 3.29). Elle contenait déjà le bilan du 16 novembre et la correction de `VERSION.txt` en 3.29, au commit `efb6ac96f2dfeedbce9be217c3e0f322f36eb4e9`.

## Paquet reçu et contrôles

- ZIP `SkiStation_GESTION_3_93_PISTES_VIVANTES_DESCENTES_IA_PREPARATION_COMPLET(5).zip`, SHA-256 `3b04bce2ef15214b76f76d46cf14c6384a5b2ccf214745651597afc3af3d1c14`. CRC et 4 797 empreintes du manifeste vérifiés. Comparaison avec `main` par chemin et empreinte Git : 949 fichiers identiques, 160 modifiés, 3 689 propres au ZIP et une archive historique propre à `main`.
- Le manifeste 3.92 inclus dans le ZIP situe le delta immédiat à 19 fichiers modifiés et 71 nouveaux fichiers, manifeste de livraison compris. Le delta 3.29 → 3.93 contient notamment des versions intermédiaires des systèmes visiteurs, remontées, opérations et sauvegarde.
- Contrôles rejoués sur le ZIP : matrice des 60 points, 12 nouveaux scripts avec leurs `.meta`, références GUID des 9 scènes et scènes des paramètres de build, et 10 100 cas du modèle numérique Python. Deux `.meta` Arcade ont un GUID de 33 caractères : `ArcadeCarvingVisual.SnowEffects.cs.meta` et `ArcadeSkiController.ContactReaction.cs.meta`. Les 1 578 autres GUID ont un format valide et sont uniques dans le paquet. Import, compilation, Play Mode, durées réelles et sauvegarde Unity restent à vérifier.
- Le paquet demande Unity `6000.3.7f1`. `ProjectSettings/ProjectSettings.asset` indique 3.93, tandis que `VERSION.txt` et `Assets/SkiStation/MASTER_VERSION.txt` dans le ZIP indiquent 3.87. Confirmer les marqueurs de version lors de l’intégration du projet.

Le [guide de démarrage](COMMENCER_ICI_GESTION_3_93.md), l’[audit des 60 points](FINAL_VALIDATION_GESTION_3_93.md) et le [rapport structurel](VALIDATION_STRUCTURE_GESTION_3_93.json) sont les notes du paquet. Le rapport décrit les contrôles de livraison et compte aussi les deux GUID trop longs. Pour l’IA visiteurs 2.0, le [contrat de navigation](VISITOR_AI_NAVIGATION_CONTRACT_3_93.md), le [contrat trafic](VISITOR_AI_TRAFFIC_INTEGRATION_3_93.md) et la [note de passation](VISITOR_AI_2_0_HANDOFF_3_93.md) décrivent les points d’extension préparés.

## Ordre de reprise

| Ordre | Lot | Estimation et critère de passage |
| --- | --- | --- |
| 1 | Notes et comparaison de la livraison | Petit. Vérifier le ZIP et ses manifestes, puis consigner les écarts observables. |
| 2 | Métadonnées déjà applicables au dépôt | Petit. Apparier par chemin et contenu identique, vérifier le format et l’unicité des GUID avec les métadonnées suivies. |
| 3 | Base Unity intermédiaire jusqu’à 3.92 | Important. Rapprocher d’abord projet, paquets, scènes et ressources, puis les dépendances des visiteurs, lifts et checkpoint. Vérifier l’import et relever la première erreur de compilation, s’il y en a. |
| 4 | Descente et GPS 3.93 | Moyen une fois la base disponible. Intégrer ensemble `SkierRuntime`, les segments du graphe, le planificateur et les replis. Tester gare haute → piste → bifurcation → arrivée. |
| 5 | Trafic, rendu et checkpoint 3.93 | Moyen. Contrôler les coûts du GPS, les métriques et la pose, puis charger deux fois un checkpoint de descente. Faire la campagne de journée et la régression Arcade. |

Pour rejouer le contrôle complet `Tools/Management/verify_pistes393.py --base …`, il faut joindre le ZIP **Gestion 3.92 Hybrid Motion clôturée**, SHA-256 `de73e7ad4d8f82bfec3b903dbe3ef4ac8b0c69a2ef340ccb841d15a736855588`, contenant `HYBRID_MOTION_CLOTURE_MANIFEST.json`. Le manifeste embarqué identifie les fichiers, tandis que l’archive 3.92 fournit les anciennes sources modifiées, nécessaires aux comparaisons de structure. L’archive 3.93 fournie suffit pour examiner le projet candidat et préparer son import Unity.

Pour rapprocher les travaux mentionnés dans le [bilan de novembre](../../ETAT_DU_PROJET_2026-11-16.md), il reste à identifier le ZIP, les scènes ou les commits qui contiennent ValAlpin, Glisse 3.0, Alpine Empires et les trois scènes physiques. L’archive reçue contient les scènes SkiStation de Gestion et les laboratoires Arcade, avec des contrats préparatoires pour l’IA visiteurs. Le lien entre ces éléments et les chantiers cités dans le bilan reste à confirmer.
