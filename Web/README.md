# SkiStation — version web jouable

Jeu de gestion/tycoon de station de ski en 3D, jouable directement dans un navigateur (PC ou mobile).
Il reprend le cahier des charges du projet (`CAHIER_DES_CHARGES_WORK_SKISTATION.md`) sans toucher au projet Unity.

## Lancer

Ouvrir `Web/index.html` dans un navigateur récent (Chrome, Edge, Firefox, Safari). Aucun serveur ni installation :
tout est en JavaScript, three.js est fourni dans `js/three.min.js`.

## Ce qui est en jeu

| Cahier des charges | Dans la version web |
|---|---|
| Modes | Carrière (6 étapes, du premier téléski aux Jeux Olympiques) et Bac à sable (argent illimité / catalogue libre en option) |
| Montagne 360° | Massif procédural 8 × 8 km, 1 640 → 4 200 m : alpages, forêts, bancs, crêtes rocheuses, plateau glaciaire |
| Construction libre | Pistes, remontées, routes, bâtiments. Devis avant validation (prix, longueur, dénivelé, pente, difficulté, arbres). Arbres coupés seulement après validation, terrassement local sous les bâtiments et les gares |
| Difficulté des pistes | Calculée sur le relief réellement traversé (pente le long du tracé et dévers) |
| Remontées | 13 modèles sur 6 paliers de recherche, dont un funiculaire au sol. Modernisation sur la même ligne avec reprise de l'ancien modèle. Portée dépassée refusée, jamais corrigée. Téléski : pas de traversée de piste. Véhicules physiques, embarquement réel |
| Temps | Horloge unique, pause / x1 / x3 / x8, journée de 07:30 à minuit, cycle jour-nuit |
| Ski nocturne | Recherche dédiée, éclairage des pistes, remontées ouvertes de 18 h 30 à 21 h, forfait du soir, damage décalé après la nocturne |
| Visiteurs | Budget temps, faim, énergie, profil, niveau. Décision Skier / Manger / Repos / Rentrer, fiche en lecture seule |
| GPS du domaine | Piste cible persistante, trajet par remontées (montée) et pistes (descente), temps de trajet + attente + retour vérifiés, aucune téléportation |
| Services | Restauration, bar, location, magasin, hébergements, activités : capacité, file, qualité, prix réglable, agrandissement |
| École / enfants | École de ski (moniteurs, cours collectifs et individuels), Jardin des Piou-Piou, médailles |
| Neige / météo / sécurité | Prévisions, vent (arrêt des remontées exposées), verglas, soupe, enneigeurs, damage du soir par dameuses visibles, pannes, accidents, pisteurs, hélicoptère, sécurisation avalanche. Option « neige simplifiée » |
| Activités | Snowpark, luge, patinoire, nordique, chiens de traîneau, parapente, spa, musée, concerts, stade de slalom, stade olympique |
| Économie | Forfait, tarifs, charges, emprunt, marketing, événements, réputation, prestige, recettes d'été |
| Événements du matin | Une fois sur quatre environ : grève, panne électrique, risque d'avalanche, influenceuse, subvention, tournage, équipe nationale, contrôle technique, chef étoilé. Deux choix, conséquences immédiates |
| Arcade | Descente à la troisième personne sur n'importe quelle piste construite : sauts, vrilles, flips |

## Interface

- Journal : dix derniers jours (visiteurs, satisfaction, résultat) et faits marquants.
- Cartes d'affluence et de qualité de neige sur les pistes (Options).
- Suivi caméra d'un visiteur depuis sa fiche ; renommage de la station, des pistes et des remontées.
- Mini-carte cliquable (relief, pistes, remontées, visiteurs, champ de la caméra), masquable dans Options.
- Ambiance sonore procédurale (vent selon l'altitude, foule, câbles, sons d'interface), désactivable dans Options. Elle démarre au premier clic.

## Commandes

- Souris : glisser pour déplacer, clic droit (ou Maj + glisser) pour tourner/incliner, molette pour zoomer.
- Tactile : un doigt déplace, deux doigts zooment, tournent et inclinent.
- Clavier : WASD/flèches, Q/E, Espace (pause), 1-2-3 (vitesses), R (tourner un bâtiment), Entrée (valider), Échap.

## Fichiers

- `js/core.js` utilitaires, bruit, constantes d'horloge
- `js/terrain.js` massif, forêts, terrassement
- `js/data.js` catalogues (remontées, bâtiments, recherche, carrière, événements)
- `js/game.js` état de partie, règles de construction, réseau GPS, finances, sauvegarde
- `js/weather.js` météo et neige
- `js/sim.js` visiteurs, remontées, services, secours, damage, fin de journée
- `js/models.js`, `js/render.js` modèles procéduraux et rendu
- `js/build.js` outils de construction et entrées
- `js/arcade.js` mode arcade
- `js/events.js` événements du matin
- `js/ui.js` interface
- `js/minimap.js` mini-carte
- `js/audio.js` ambiance sonore

## Limites connues

- Graphismes procéduraux (pas d'assets), sans ombres portées ; pas de musique.
- 1 500 visiteurs simulés au maximum.
- Au village, les piétons contournent les bâtiments un par un (pas de vrai calcul d'itinéraire).
- Multijoueur non réalisé.
- La sauvegarde est locale au navigateur ; une partie rechargée reprend au matin du jour en cours.
