# SkiStation — gestion fusionnée dans Unity

Cette partie fait tourner sur le moteur Unity **la gestion et le relief de la version web** (dossier `Web/`),
et garde de la version ChatGPT **l'arcade, le rider et l'habillage de la montagne** (roche, sapins, palette de neige).

## Lancer

1. Ouvrir le projet dans Unity 6 (6000.3).
2. Menu **SkiStation › Gestion fusionnée › Jouer (gestion + arcade)**, ou ouvrir `Assets/Scenes/SkiStation_Gestion.unity` puis Play.
   Cette scène est désormais la première de la liste de build ; l'ancienne `SkiStation_Bootstrap` reste disponible.

La scène est vide : tout s'installe au lancement (`GestionBootstrap`) dès que le nom de la scène commence par
`SkiStation_Gestion`. Dans ces scènes, `ArcadeSceneIsolation.IsActive` est vrai : les ~110 anciens systèmes
de gestion ChatGPT (qui s'installaient automatiquement) ne démarrent pas, il n'y a donc aucun conflit.

## Ce qui vient d'où

| Élément | Source |
|---|---|
| Règles de gestion, visiteurs, remontées, pistes, finances, recherche, pisteurs, dameuses, hébergements, loisirs, météo, scénarios, prestige / notoriété, X Games, candidature olympique, sauvegarde | Version web, portée en C# pur : `Gestion/Core` (assembly `SkiStation.Gestion.Core`, sans dépendance Unity) |
| Relief du massif 8 × 8 km, 1 640 → 4 200 m, forêts (positions) | Version web (`Relief.cs`, identique au millimètre près) |
| Texture de roche stratifiée | ChatGPT : `AlpineRockMaterial.Shared` |
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
