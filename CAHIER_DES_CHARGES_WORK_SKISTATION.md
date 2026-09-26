# Cahier des charges — SkiStation / Base Work 3.28

## 1. Identité
Jeu PC de gestion/tycoon d'une station de ski sous Unity. Style visuel réaliste avec une très légère touche cartoon. Deux grands modes visés : Bac à sable et Carrière.

## 2. Carrière
Objectifs possibles : développer une station familiale, attirer des sportifs de haut niveau, augmenter prestige/popularité et pouvoir accueillir de grands événements jusqu'aux Jeux Olympiques. Recherche pour débloquer remontées, améliorations et équipements.

## 3. Monde et montagne
Carte entièrement 360°, sans face décorative. Domaine multi-altitudes, actuellement pensé d'environ 1600 à 4200 m, avec reliefs variés : front de neige, forêts, vallons, combes, cols, crêtes rocheuses, plateaux naturels, haute montagne et glaciers skiables. Les difficultés des pistes découlent du relief local, pas simplement de l'altitude.

## 4. Construction
Le joueur trace et construit librement pistes, remontées, routes, bâtiments et services. Validation finale précédée d'un récapitulatif. Le terrain et la végétation s'adaptent localement aux constructions sans transformer arbitrairement le choix du joueur.

## 5. Remontées
Progression technologique. Départ de carrière : téléski classique et télésiège fixe 2 places. Les modèles modernes améliorent notamment vitesse et débit. Les premières remontées doivent déjà rester dynamiques et agréables à observer.

## 6. Temps
Horloge centrale unique. Pause et vitesses x1/x3/x8. Le temps de simulation doit permettre une vraie journée de station sans imposer au joueur des attentes interminables. Les déplacements restent physiques et leurs durées alimentent la planification des visiteurs.

## 7. Visiteurs
Chaque visiteur possède un plan de journée et des besoins. Il peut notamment skier, manger, se reposer, utiliser des services et rentrer. Ses choix doivent progressivement intégrer heure, temps restant, besoins, profil, files, capacités, prix/qualité et accessibilité réelle via le domaine.

## 8. Navigation
GPS de domaine persistant : planifier une destination puis emprunter les pistes et remontées intermédiaires nécessaires. Les remontées servent à monter, les pistes à descendre. Éviter les téléportations correctives.

## 9. Services
Magasins, restaurants, bars, location de matériel, écoles de ski, activités et hébergements. Capacité, files d'attente, qualité, gamme, prix, personnel et satisfaction doivent pouvoir influencer les décisions sans tomber dans une micro-gestion pénible.

## 10. Enfants / ski école
Jardin des Piou-Piou distinct des zones adultes débutants. Cours individuels et collectifs, niveaux progressifs jusqu'aux niveaux avancés type Flèche/Chamois.

## 11. Sécurité / neige / météo
Météo et neige compréhensibles et simplifiables pour joueurs casual. Verglas, neige soupe, canons à neige simplifiés, incidents/blessures, protections de pylônes et éventuel secours héliporté pour accidents importants.

## 12. Activités futures
Snowpark/compétition, ski de fond, luge, chiens de traîneau, refuges, randonnée, ski de randonnée, parapente, activités culturelles/familiales, concerts, cycle jour/nuit et mode estival.

## 13. Arcade futur
Un seul mode de descente open world arcade sur le domaine construit, fun mais avec corps humain plausible. Figures, boost, reprise rapide après figure ratée et possibilité future de compétitions/visites en ligne.

## 14. Principes de design
FUN avant micro-réalisme. Liberté du joueur. Systèmes visibles et compréhensibles. Simulation crédible mais accélérable. Pas de logique cachée qui déplace arbitrairement les constructions. Architecture du code modulaire et source de vérité unique pour les systèmes centraux.

## 15. Priorité actuelle
Transformer l'horloge 3.27 et la journée dynamique 3.28 en véritable intelligence de visiteurs. Prochaine brique recommandée : 3.29, arbitrage Ski / Manger / Repos / Rentrer, puis connexion aux services et satisfaction avant GOAP complet.
