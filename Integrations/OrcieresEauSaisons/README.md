# Orcières Merlette — Eau et franchissements

Ce kit cible **SkiStation Alpine Empires Avant Première**, sous Unity **6000.3.7f1**, avec Orcières Merlette déjà accessible en Gestion. Il suit l’organisation des intégrations Unity de ce dépôt. La version historique à la racine ne contient pas le moteur de terrain et les composants de sauvegarde requis.

## Installation

Fermer Unity, puis lancer PowerShell 7 depuis ce dossier.

```powershell
./Installer.ps1 -Projet 'X:/chemin/vers/SkiStation_Alpine_Empires_Avant_Premiere'
```

Le script vérifie tous les fichiers et l’identité du relief avant de les copier. Il affiche les chemins à rapprocher si le projet contient une version différente des sources.

## En jeu

- L’eau libre, la glace et la neige suivent la saison et la météo. Les nouvelles constructions au sol sont refusées sur l’eau, même sous la neige.
- Aux Estaris, les visiteurs peuvent accéder à la plongée sous glace en hiver et à la baignade en été selon l’état du lac.
- **Construire → Montagne** propose un pont de piste ou une passerelle VTT. Sélectionner deux berges pour obtenir un devis.
- Les commandes de nage, de plongée et de VTT restent à développer.

## Tests Unity

Les essais du 20 novembre 2026 ont couvert les quatre saisons, le refus de construire sur le lac et les cours d’eau, le refus d’un pont en double, et la construction d’une piste par l’outil natif sur le tablier. La sauvegarde a restauré les deux franchissements, la piste et l’été après sortie et retour en Play Mode. Une sauvegarde créée avant l’ajout des saisons a aussi restauré sa piste et son téléski.

Le test `Checks/OrcieresWaterCheck.cs` peut être copié dans `Assets/Editor` d’une copie Unity. Lancer Unity en batch avec `-executeMethod OrcieresWaterCheck.Run`. Le point d’entrée `RunLegacy` utilise une sauvegarde de test d’Orcières antérieure ayant une piste et une remontée, préparée dans le profil isolé `SkiStation_OrcieresWaterLegacy_Smoke`.

Les lacs et cours d’eau sont issus de la BD TOPO © IGN, extraite le 20 novembre 2026 sous Licence Ouverte 2.0. Les largeurs de certains cours d’eau sont estimées pour le jeu. [BD TOPO](https://www.data.gouv.fr/datasets/bd-topo-r) · [Licence Ouverte 2.0](https://www.etalab.gouv.fr/licence-ouverte-open-licence/)

Le kit fournit le code et la couche d’eau. Il conserve la version locale reconnue du décor complet si elle est déjà installée.
