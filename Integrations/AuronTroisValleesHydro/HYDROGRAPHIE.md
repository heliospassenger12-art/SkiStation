# Hydrographie jouable d’Auron et des Trois Vallées

Les couches d’eau sont alignées sur les reliefs jouables du catalogue France. Les Trois Vallées utilisent la carte fusionnée de 19,56 × 31,2 km. Auron utilise la carte de 8,04 × 8,4 km.

| Domaine | Surfaces de plans d’eau | Surfaces de cours d’eau | Repère dans le jeu |
| --- | ---: | ---: | --- |
| Auron | 13 | 221 | Retenue d’altitude |
| Les Trois Vallées | 429 | 1 029 | Lac de Tuéda |

Les chiffres comptent des surfaces importées, qui peuvent représenter plusieurs parties d’un même plan ou cours d’eau. Les relevés ponctuels ou souterrains sont écartés. Pour les cours d’eau représentés par un axe, le jeu estime la largeur à partir de la classe cartographiée lorsqu’elle est disponible.

Les saisons du jeu déterminent le gel, l’enneigement et le dégel. La construction au sol reste interdite sur les emprises d’eau toute l’année. Le menu **Construire → Montagne** permet de poser un pont de piste ou une passerelle VTT entre deux berges compatibles. La baignade et la plongée sous glace restent disponibles au site déjà configuré d’Orcières.

Données hydrologiques © IGN, BD TOPO, Licence Ouverte 2.0. [BD TOPO](https://www.data.gouv.fr/datasets/bd-topo-r) · [Licence Ouverte](https://www.etalab.gouv.fr/licence-ouverte-open-licence/)

## Installation dans Avant Première

Ce lot s’applique au projet Unity Avant Première qui contient le catalogue France jouable et l’intégration saisonnière d’Orcières. Dans PowerShell 7, lancer `./install.ps1 -Project 'chemin du projet Unity'`. L’installateur vérifie la présence des deux reliefs et les modifications de code attendues avant l’écriture. `HydroFranceCheck.cs` est un contrôle Unity Editor à placer dans une copie du projet pour vérifier la sauvegarde et la reprise sur les deux cartes.
