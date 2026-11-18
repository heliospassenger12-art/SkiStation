# SkiStation — version web jouable

Jeu de gestion/tycoon de station de ski en 3D, jouable directement dans un navigateur (PC ou mobile).
Il reprend le cahier des charges du projet (`CAHIER_DES_CHARGES_WORK_SKISTATION.md`) sans toucher au projet Unity.

## Lancer

Ouvrir `Web/index.html` dans un navigateur récent (Chrome, Edge, Firefox, Safari). Aucun serveur ni installation :
tout est en JavaScript, three.js est fourni dans `js/three.min.js`.

## Ce qui est en jeu

| Cahier des charges | Dans la version web |
|---|---|
| Modes | Carrière par scénarios, Bac à sable (argent illimité / catalogue libre en option) et **Alpine Empires** (compétition contre 3 stations rivales, chacune sur son massif, grande carte de 18 × 18 km) |
| Scénarios | **Premiers pas** (420 000 €, 250 visiteurs en une journée en 70 jours) · **Station en perdition** (station endettée et vieillissante à rendre incontournable en 320 jours) · **Capitale des familles** · **Station de prestige** · **La route des Jeux** (campagne en 8 étapes jusqu'aux Jeux Olympiques). Objectifs chiffrés en direct, délai, échec (faillite ou délai dépassé), victoire notée de 1 à 3 étoiles, records gardés dans le navigateur |
| Prestige et notoriété | Deux indicateurs distincts. **Prestige** (★ à ★★★★★) : le standing — domaine, remontées d'exception, hébergement de luxe, loisirs, gastronomie, spa — plafonné sans luxe, sans loisirs ou sans remontée d'exception. **Notoriété** (%) : la renommée — surtout l'humeur des visiteurs et le rapport qualité/prix ; une station simple aux clients ravis peut être très réputée. Détail dans `ALPINE_EMPIRES_REGLES.md` |
| Grands événements | Échelle : concerts, course régionale, Coupe d'Europe, **X Games** (10 critères, facultatifs à organiser), Coupe du monde, **Jeux Olympiques** (12 critères reprenant ceux des X Games avec des seuils plus élevés) |
| Jeux Olympiques | Candidature longue : dépôt (600 000 €, niveau X Games et Coupe du monde requis), 25 jours d'évaluation face à 3 villes rivales, vote du CIO, 100 jours de préparation avec inspection à mi-parcours, inspection finale. **Le jour 1 des Jeux, la partie est gagnée** |
| Montagne 360° | Quatre choix au départ (Classique, Dents Blanches, Val Perdu, Aléatoire). Massif procédural 8 × 8 km, 1 640 → 4 200 m : vallée d'accès douce au pied du village, replats étagés (alpages, cirques, glaciers) reliés par des faces skiables, aiguilles rocheuses étroites au sommet. 30 à 45 % de la carte peut accueillir une piste reliée au front de neige |
| Construction libre | Pistes (à la main ou **tracé assisté** : un clic sur l'arrivée d'une remontée et le jeu trouve la descente jusqu'au front de neige, avec difficulté visée), remontées, routes, bâtiments. Carte des zones desservables, front de neige signalé. Devis avant validation (prix, longueur, dénivelé, pente, difficulté, arbres). Arbres coupés seulement après validation, terrassement local sous les bâtiments et les gares · **Jonctions** : une piste ne chevauche jamais une autre ; elle peut partir d'une piste existante (embranchement) et s'arrête sur la première piste qu'elle rencontre, qu'elle rejoint (le réseau les relie à ce carrefour). Un tracé qui longe une piste existante est refusé. Même principe pour les routes, qui se raccordent au carrefour · **Front de neige libre** : tout le cercle pointillé est un espace ouvert ; une piste qui finit n'importe où dedans est raccordée, et les skieurs y glissent librement jusqu'à la remontée ou la piste de leur choix (pas de jonction forcée dans ce rond) |
| Difficulté des pistes | Calculée sur le relief réellement traversé (pente le long du tracé et dévers) |
| Remontées | 13 modèles sur 6 paliers de recherche, dont un **funiculaire souterrain** (tunnel invisible jusqu'à 8 km, toutes pentes, seules les gares sont visibles, aucun arbre coupé, on peut construire au-dessus). Modernisation sur la même ligne avec reprise de l'ancien modèle. Portée dépassée refusée, jamais corrigée. Téléski : pas de traversée de piste. Véhicules physiques, embarquement réel |
| Temps | Horloge unique, pause / x1 / x3 / x8, journée de 07:30 à minuit, cycle jour-nuit |
| Ski nocturne | Recherche dédiée, éclairage des pistes, remontées ouvertes de 18 h 30 à 21 h, forfait du soir, damage décalé après la nocturne |
| Visiteurs | Familles et groupes d'amis qui arrivent, skient et déjeunent ensemble (le groupe choisit pour le moins à l'aise). Budget temps, faim, énergie, profil, niveau. Décision Skier / Manger / Repos / Rentrer, fiche en lecture seule |
| Fidélité | Les visiteurs contents deviennent des **habitués** (familles, groupes, skieurs seuls, avec leurs prénoms et leur niveau) : ils reviennent plusieurs fois dans la saison, surtout le week-end et pendant les vacances, et d'une saison à l'autre ; ils retournent sur leur piste préférée, les enfants gardent leur progression. Les plus fidèles achètent le **forfait saison** au début de l'hiver (prix réglable dans Finances). Les déçus espacent leurs venues puis disparaissent. Bouche-à-oreille sur la demande et la notoriété. Panneau Visiteurs › Fidélité |
| GPS du domaine | Piste cible persistante, trajet par remontées (montée) et pistes (descente), temps de trajet + attente + retour vérifiés, aucune téléportation |
| Services | Restauration (snack à table gastronomique), bar, location, magasin, activités : gamme, personnel, horaires (midi / midi et soir), prix, note des clients, marge, agrandissement |
| Hébergements | Du gîte (50 lits) au palace et au chalet de prestige : trois segments de clientèle (économique, confort, luxe) avec demande propre, demi-pension, spa, navette, note, occupation, prix de la nuitée |
| Pisteurs | Effectif par poste de secours, km sécurisés par pisteur, pistes non sécurisées plus accidentées, délais d'intervention, motoneiges, balisage |
| Dameuses | Parc par garage (jusqu'à 6), équipe de nuit, priorité de damage par piste, capacité en km contre besoin, bilan de la nuit |
| Finances | Compte de résultat du jour et de la saison, charges fixes, seuil de rentabilité, rentabilité par bâtiment, politique salariale, trois prêts échéancés, plafond d'endettement, crédit d'urgence en cas de découvert |
| Recherche | 30 projets en 6 catégories, laboratoire à 3 niveaux (projets en parallèle, durée réduite), file d'attente |
| École / enfants | École de ski (moniteurs, cours collectifs et individuels), Jardin des Piou-Piou, médailles |
| Neige / météo / sécurité | Prévisions, vent (arrêt des remontées exposées), verglas, soupe, enneigeurs, damage du soir par dameuses visibles, pannes, accidents, pisteurs, hélicoptère, sécurisation avalanche. Option « neige simplifiée » |
| Activités | Snowpark, luge, patinoire, nordique, chiens de traîneau, parapente, spa, musée, concerts, stade de slalom, stade olympique |
| Économie | Forfait, tarifs, charges, emprunt, marketing, événements, réputation, prestige, recettes d'été |
| Événements du matin | Une fois sur quatre environ : grève, panne électrique, risque d'avalanche, influenceuse, subvention, tournage, équipe nationale, contrôle technique, chef étoilé. Deux choix, conséquences immédiates |
| Saison d'été | Après la recherche « Saison d'été » : 30 jours jouables de VTT et de randonnée sur les remontées (hors téléskis et tapis), paysage d'été, météo et forfait dédiés ; peut être abrégée |
| Succès | 18 succès à débloquer, listés dans Carrière |
| Arcade | Descente à la troisième personne sur n'importe quelle piste construite : sauts, vrilles, flips |
| Alpine Empires (prototype) | **Grande carte de 18 × 18 km à quatre massifs différents**, reliés par un plateau de cols (pas de falaise entre les territoires) : le vôtre au sud-ouest (relief choisi au menu), un grand domaine d'alpages pour Valfroide, une haute montagne glaciaire pour Pic-Royal, une moyenne montagne boisée pour Les Rochettes ; le panneau indique la montagne et le sommet de chacun. Deux liaisons à acheter (50 000 € et 100 000 €) pour s'étendre vers les voisins, col central protégé. Valfroide (bâtisseur), Pic-Royal (luxe) et Les Rochettes (populaire) construisent pour de vrai remontées, pistes et bâtiments, fixent leurs prix, se disputent la même clientèle, gravissent l'échelle des événements et candidatent aux Jeux. Mode « chacun pour soi » ou « deux équipes » (vous et Valfroide). Victoire : la première compagnie au jour 1 des Jeux Olympiques ; si c'est un rival, la partie est perdue |

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
- `js/manage.js` règles de gestion (services, hébergements, banque, recherche, pisteurs, dameuses)
- `js/scenario.js` scénarios, notoriété, candidature olympique, station préconstruite
- `js/loyalty.js` fidélité : habitués, retours, forfait saison, bouche-à-oreille
- `js/ae.js` Alpine Empires : concessions, stations rivales gérées par l'ordinateur, marché partagé, course aux Jeux
- `js/ui_scn.js` panneau Carrière
- `js/ui_mgmt.js` panneaux Finances, Recherche, Exploitation et fiche de gestion d'un bâtiment
- `js/ui.js` interface
- `js/minimap.js` mini-carte
- `js/audio.js` ambiance sonore

## Limites connues

- Graphismes procéduraux (pas d'assets), sans ombres portées ; pas de musique.
- 1 500 visiteurs simulés au maximum.
- Au village, les piétons contournent les bâtiments un par un (pas de vrai calcul d'itinéraire).
- Alpine Empires : les rivaux ont une gestion simplifiée (pas de visiteurs simulés un par un) ; multijoueur non réalisé.
- L'été réutilise les pistes comme sentiers : pas de tracés VTT dédiés ni d'équipements propres à l'été.
- La sauvegarde automatique est locale au navigateur ; Options › « Exporter un code de sauvegarde » donne un code à recoller au menu principal sur un autre appareil ; une partie rechargée reprend au matin du jour en cours.
