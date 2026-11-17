# SkiStation — gestion fusionnée dans Unity

Cette partie fait tourner sur le moteur Unity **la gestion et le relief de la version web** (dossier `Web/`),
et garde de la version ChatGPT **l'arcade, le rider et l'habillage de la montagne** (roche, sapins, palette de neige).

## Lancer

1. Ouvrir le projet dans Unity 6 (6000.3).
2. Menu **SkiStation › Gestion fusionnée › Jouer (gestion + arcade)**, ou ouvrir `Assets/Scenes/SkiStation_Arcade_Test_Gestion.unity` puis Play.

La scène est vide : tout s'installe au lancement (`GestionBootstrap`). Son nom commence par `SkiStation_Arcade_Test_`,
préfixe reconnu par `ArcadeSceneIsolation` de la version ChatGPT : les anciens systèmes de gestion ne s'y installent pas,
**sans modifier aucun fichier ChatGPT**. Les rares objets créés sans cette vérification sont retirés au démarrage.

## Installer dans la dernière version du projet (3.94 / A2.64, URP)

Kit `SkiStation_Installer_Gestion_Web.zip` (Drive, dossier « Alpine Empires Diviser ») : fermer Unity, lancer
`Installer_Gestion_Web.cmd`. Il **ajoute** seulement `Assets/SkiStation/Gestion/`, la scène et `Tools/GestionPort/`.
L'arcade, le rider, la roche et les sapins sont retrouvés par leur nom à l'exécution (`ChatGPTAssets.cs`) :
pas d'erreur de compilation si leur code a évolué. Shaders choisis selon le pipeline (URP ou intégré).

## Ce qui vient d'où

| Élément | Source |
|---|---|
| Règles de gestion, visiteurs, remontées, pistes, finances, recherche, pisteurs, dameuses, hébergements, loisirs, météo, scénarios, prestige / notoriété, X Games, candidature olympique, sauvegarde | Version web, portée en C# pur : `Gestion/Core` (assembly `SkiStation.Gestion.Core`, sans dépendance Unity) |
| Relief du massif 8 × 8 km, 1 640 → 4 200 m, forêts (positions) | Version web (`Relief.cs`, identique au millimètre près) |
| Texture de roche stratifiée | ChatGPT : `AlpineRockMaterial.Shared` (trouvé à l'exécution) |
| Palette neige fraîche / compacte / ventée / roche / roche ombrée | ChatGPT : `AlpineMountainSurface261` (seuils d'altitude adaptés au nouveau massif) |
| Sapin étagé enneigé | ChatGPT : `AlpineForestVisual262.BuildCrown` (dessiné par instanciation) |
| Rider, glisse, sauts, figures, combos, boost, HUD, caméra 3e personne | ChatGPT : `Scripts/Arcade` (inchangé) |

## Architecture

- `Core/` : noyau C# testable hors Unity. `Game` (état, devis avant validation, réseau GPS), `Sim` (horloge unique,
  visiteurs), `Weather`, `Mgmt`, `Scenarios`, `MorningEvents`, `Actions` (commandes du joueur), `SaveLoad`.
  Les catalogues sont **générés** depuis `Web/js/data.js` (`Tools/GestionPort/gen_*.js`) : ne pas les modifier à la main.
- `Unity/` : affichage et saisie uniquement.
  - `GestionBootstrap` : installation, horloge, sauvegarde (`persistentDataPath/skistation_gestion.json`).
  - `GestionWorld` : relief en 16 blocs avec collisions (utilisés aussi par l'arcade), surface neige/roche, forêts.
  - `GestionRender` : pistes balisées, remontées (gares, pylônes, câbles, véhicules réels), bâtiments, routes, visiteurs, dameuses, secours, jour/nuit.
  - `GestionTools` : construction avec aperçu et devis, tracé assisté, sélection.
  - `GestionUI` : menu, barre du haut, panneaux de gestion, fiches, bilans, événements du matin (IMGUI).
  - `GestionArcade` : pose le rider ChatGPT au départ d'une piste construite (fiche de piste › « Descendre en arcade »).

Coordonnées : Unity `(x, altitude, −z)` du noyau (repère main gauche), mètres réels.

## Commandes

- Caméra : ZQSD/WASD ou flèches, Q/E pour tourner, clic droit glissé pour orbiter, molette pour zoomer, clic milieu (ou Maj + clic) pour déplacer.
- Temps : Espace (pause), 1 / 2 / 3 (x1, x3, x8), ⏭ pour passer au lendemain quand la station est vide.
- Construction : clic pour poser, Retour arrière pour retirer un point, Entrée pour valider le devis, R pour tourner un bâtiment, Échap pour annuler.
- Arcade : commandes de la version ChatGPT ; Échap revient à la gestion.

## Vérifications faites hors Unity

- Relief : écart maximal 0,7 mm avec la version web (graines 1337 et 4242).
- Devis (remontées, pistes, bâtiments) : identiques au centime ; réseau du domaine identique.
- Simulation de plusieurs journées : fréquentation, recettes et retours des visiteurs équivalents à la version web.
- Sauvegarde / rechargement : aller-retour complet.
- La couche Unity compile contre une API Unity minimale (`Tools/GestionPort/UnityStubs.cs.txt`) ; elle n'a pas encore été lancée dans l'éditeur.
