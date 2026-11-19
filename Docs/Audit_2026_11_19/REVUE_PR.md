# Examen des propositions de reprise

État observé le 19 novembre 2026 sur le dépôt `heliospassenger12-art/SkiStation`. Les trois PR sont en brouillon. La PR 1 cible main. Les PR 3 et 4 ciblent la branche de reprise Gestion de la PR 1. Leur base commune remonte au dépôt historique de septembre.

| PR | Contenu réel | Contrôle effectué | Suite proposée |
|---|---|---|---|
| [1](https://github.com/heliospassenger12-art/SkiStation/pull/1) · Gestion 3.93 | Notes de reprise, marqueur VERSION et 190 métadonnées. Les scripts Gestion 3.93 complets restent dans l'archive de livraison. | Les 190 métadonnées correspondent à l'archive Avant Première retrouvée. La proposition reste trois commits devant sa base commune avec main. | Réutiliser les métadonnées dans le plan de reprise. Revoir les notes et marqueurs lors du rapprochement du projet complet pour refléter le Bloc 2 récent. |
| [3](https://github.com/heliospassenger12-art/SkiStation/pull/3) · La Nocturne | Deux sources Arcade et installateur par empreintes sous Integrations, accompagnés de documentation. | Les empreintes parentes correspondent aux sources Avant Première. Application sur copie ciblée et répétition réussies, y compris après les patchs météo. | Garder le kit lié à sa base récente. Recetter boost, charge, vol, réception, confort et sortie dans Unity. |
| [4](https://github.com/heliospassenger12-art/SkiStation/pull/4) · Grand Aigle | Quatre patchs cumulatifs de rendu et lumière, sources météo et guides d'intégration. | Application des quatre patchs dans l'ordre, puis comparaison des six fichiers météo au contenu final. | Recetter shaders URP, cycle, transitions météo, pause, nettoyage et performances GPU dans Unity. |

## Révisions examinées

| Branche | Commit |
|---|---|
| main | `5018af206bbf754fb41f40a4db8ddd8a413862e9` |
| codex/suivi-skistation-2026-11-16 | `b7eb68b1f99d180e1e6e603b8a079dd0a83cbf3f` |
| codex/arcade-la-nocturne | `dae3750a227d795126810060afc35e6569fa6868` |
| codex/arcade-unity | `e2ea7f3a6acccc4bf1644ff03a7c7520c1b0e7c2` |

Le premier point d'intégration pour Unity est la source Avant Première vérifiée et son complément Comptes alliés. Les trois PR fournissent des lots de reprise à examiner dans ce contexte. Une fusion des notes de la PR 1 seule laisserait encore la racine Unity sur sa génération historique.

## Ordre des patchs Grand Aigle contrôlé

1. `SkiStation-Unity-Arcade-Grand-Aigle.patch`
2. `SkiStation-Unity-Arcade-Sapins-Grand-Aigle.patch`
3. `SkiStation-Unity-Arcade-Cycle-Lumiere-Grand-Aigle.patch`
4. `SkiStation-Unity-Arcade-Meteo-Grand-Aigle.patch`

Le portage La Nocturne utilise deux autres fichiers et a passé son contrôle après cet ordre. Les essais ont utilisé une copie ciblée des fichiers concernés. Ces contrôles ne chargent aucune scène et n'exécutent aucun shader Unity.

[Résultats détaillés](CONTROLES_PATCHS_ARCADE.json) · [Plan de reprise](REPRISE.md)
