# SkiStation Master 1.06 — Unity Foundation

## Objectif
Cette version transforme le package précédent en véritable base de projet Unity propre et pédagogique.

## Ce qui est installé
- `SkiStationGameBootstrap` : point d'entrée unique de la scène.
- `SkiStationRuntimeRegistry` : registre léger des services runtime.
- `SkiStationDiagnosticsRuntime` : diagnostic de démarrage.
- `SkiStationDiagnosticsUI` : panneau diagnostic avec F1/F2.
- `SkiStationVersion` : version et cible Unity centralisées.
- `SkiStationProjectConfig` : configuration de projet sous forme de ScriptableObject.
- scène `SkiStation_Bootstrap` dédiée au démarrage.
- `ProjectSettings/EditorBuildSettings.asset` avec la scène bootstrap.

## Règles d'architecture
1. Une seule scène bootstrap pour initialiser les services.
2. Les systèmes gameplay restent séparés de la fondation.
3. Aucun visiteur, véhicule ou skieur artificiel n'est créé par la fondation.
4. La simulation de gestion reste la source de vérité.
5. La simulation du joueur reste indépendante de la gestion.
6. Les Assembly Definitions ne sont pas ajoutées avant une première compilation réelle dans Unity : elles seront introduites à partir des dépendances effectivement validées.

## Utilisation débutant
1. Installer Unity 6.3 LTS / 6000.3.x.
2. Ouvrir le dossier contenant `Assets`, `Packages` et `ProjectSettings`.
3. Ouvrir la scène `Assets/Scenes/SkiStation_Bootstrap.unity`.
4. Lancer Play.
5. Appuyer sur F1 pour afficher le diagnostic et F2 pour le relancer.

## Limite connue
La compilation avec l'éditeur Unity n'a pas été exécutée dans cet environnement. La validation fournie ici est statique : structure, doublons de types, accolades, fichiers requis et intégrité du ZIP.
