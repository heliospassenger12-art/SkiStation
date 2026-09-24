# SkiStation Master 1.05 — Fondation Unity

## Objectif

Cette version ne cherche volontairement pas à ajouter une grosse mécanique de gameplay.
Elle transforme le contenu précédent en **projet Unity propre, lisible et importable**.

## Base retenue

- Unity 6.3 LTS / 6000.3.3f1
- PC / Standalone comme cible de départ
- C# runtime classique Unity
- uGUI 2.0.0 disponible pour les scripts utilisant `UnityEngine.UI`

## Nettoyage effectué

Les anciennes copies de travail telles que `SkiStation_Master_0.68`, les anciens dossiers `src/` et les autres arbres historiques ont été retirés du projet actif.

Ils ne doivent pas se trouver sous `Assets/`, car Unity importerait et compilerait leurs scripts également, ce qui peut provoquer des doublons de classes.

## Pourquoi cette structure

Unity compile normalement les scripts du projet dans ses assemblies de scripts. Cette version privilégie d'abord la simplicité et la compatibilité pour une personne qui découvre Unity.

Les Assembly Definitions (`.asmdef`) pourront être introduites plus tard, après la première compilation propre, lorsque les dépendances réelles entre domaines seront confirmées.

## Diagnostic Editor

Menu : `SkiStation > Validate Project`

Le diagnostic vérifie notamment :

- présence des dossiers fondamentaux ;
- nombre de scripts actifs ;
- doublons de noms de classes détectables ;
- présence de la scène de démarrage ;
- présence du manifest Packages ;
- version Unity cible.

## Important

Une validation statique n'est pas équivalente à une compilation Unity. La vraie compilation doit être faite par l'éditeur Unity.
