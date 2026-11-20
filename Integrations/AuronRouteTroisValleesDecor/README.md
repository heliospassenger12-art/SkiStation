# Route d’Auron et décor des Trois Vallées

## Auron

La route d’accueil contourne la retenue en conservant ses extrémités et l’accès au parking. Le jeu reconnaît l’empreinte précise de l’ancienne route pour les reprises d’Auron. Quand le joueur réenregistre une partie, la sauvegarde utilise la nouvelle empreinte.

## Les Trois Vallées

Le décor couvre la carte fusionnée de 19,56 × 31,2 km. La génération place les arbres dans 700 boisements de la BD Forêt V2. Les rochers et falaises utilisent 541 zones et 192 lignes cartographiées. Les tracés de pistes éclaircissent les boisements et le placement de constructions dégage les emprises en jeu.

Les modèles proches utilisent les ressources Pure Nature 2 accessibles dans le projet. Les modèles intermédiaires sont regroupés par secteur. Les plages de forêt restent lisibles en vue générale grâce à l’ombrage du terrain.

## Installation et tests

Ce lot cible **SkiStation Alpine Empires Avant Première** sous Unity 6000.3.7f1 après les lots hydrographie, caméra et décor d’Auron. Fermer Unity, puis lancer `./Installer.ps1 -Projet 'chemin du projet Unity'` dans PowerShell 7 avec Git disponible.

- Copier `AuronRoadCheck.cs` dans `Assets/Editor` d’une copie Unity. Placer une copie du profil de test `SkiStation_HydroFrance_Smoke` sous le nom `SkiStation_AuronRoad_Smoke`, puis lancer Unity avec `-batchmode -force-d3d11 -executeMethod AuronRoadCheck.Run`.
- Copier `TroisValleesDecorCheck.cs` dans `Assets/Editor`. Utiliser `TroisValleesDecorCheck.RunBaseline` sur la copie avec l’hydrographie, puis `TroisValleesDecorCheck.RunDecor` sur la copie équipée du décor. Ces méthodes produisent des mesures de rendu à 1440 × 900 avec transfert synchrone d’un pixel et trois points de vue.

## Mesures du rendu

Unity 6000.3.7f1, RTX 3070, Direct3D 11, 24 rendus mesurés par vue après 6 rendus d’échauffement. Les mesures couvrent le rendu et la synchronisation GPU. La simulation des visiteurs demande une mesure en jeu.

| Vue | Référence médiane / 95e centile | Décor médiane / 95e centile |
| --- | ---: | ---: |
| Carte entière | 3,00 / 3,30 ms | 3,47 / 3,69 ms |
| Vallée de Tuéda | 2,04 / 2,26 ms | 5,82 / 6,69 ms |
| Près du sol | 1,82 / 2,33 ms | 6,41 / 6,72 ms |

## Sources

- Les boisements et les points rocheux proviennent de la BD Forêt V2 et de la BD TOPO © IGN sous Licence Ouverte 2.0. [BD Forêt](https://www.data.gouv.fr/datasets/bd-foret-r) · [BD TOPO](https://www.data.gouv.fr/datasets/bd-topo-r)
- Les affleurements, éboulis, falaises et pistes proviennent d’OpenStreetMap. Les deux couches JSON dérivées utilisant ces données sont distribuées sous ODbL 1.0. [© contributeurs OpenStreetMap](https://www.openstreetmap.org/copyright) · [Licence ODbL](https://opendatacommons.org/licenses/odbl/1-0/)
