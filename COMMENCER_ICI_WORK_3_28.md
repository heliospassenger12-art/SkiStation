# SkiStation — COMMENCER ICI — WORK 3.28

## Base officielle
Ce ZIP est la base de travail à utiliser dans ChatGPT Work. Il reprend intégralement SkiStation 3.28.
Ne pas repartir d'une version antérieure sauf demande explicite.

## Vision du jeu
SkiStation est un jeu PC Unity de gestion de station de ski, légèrement cartoon mais réaliste, avec une grande liberté de construction et un domaine vivant. Priorité actuelle : obtenir une boucle de gestion cohérente et jouable avant d'empiler les systèmes secondaires.

## Règle absolue du terrain
TOUTE LA MAP EST 360°. Aucun massif ne doit être conçu comme une façade ou un décor à sens unique. Les faces avant, arrière et latérales doivent rester cohérentes et potentiellement exploitables selon le relief local.

Le massif actuel est une étude d'environ 6 x 8,2 km, de ~1600 m jusqu'à ~4200 m, avec crêtes, vallons, cols, plateaux naturels, roches et glaciers. La 3.26 a renforcé sa cohérence globale. Ne pas bouleverser sa géographie avant un test visuel dans Unity : privilégier désormais les ajouts réversibles et les systèmes gameplay.

## État temporel — 3.27
Une horloge centrale sert de référence commune.
- journée cible : 06:00 -> 24:00
- à x1 : environ 4 heures réelles pour cette journée simulée
- commandes : Pause / x1 / x3 / x8
- les déplacements physiques suivent l'accélération temporelle
- télésiège fixe 2 places de départ : ~4 m/s
- téléski classique de départ : ~3,5 m/s
- ne pas ralentir artificiellement les remontées pour faire correspondre l'horloge : le jeu doit rester dynamique
- les technologies futures pourront augmenter vitesse/débit

Principe fondamental : séparer la durée physique agréable à regarder de la durée représentée dans la journée simulée, tout en utilisant une seule horloge cohérente.

## Journée visiteurs — 3.28
Les visiteurs ont désormais un budget de temps personnel. Avant de relancer un cycle de ski, ils doivent tenir compte du temps nécessaire à la montée, à la descente et d'une marge de retour. Un visiteur ne doit pas démarrer un trajet incompatible avec son heure de départ. S'il est déjà engagé en altitude, il termine physiquement son parcours : pas de téléportation magique.

## Orientation immédiate — 3.29+
Construire progressivement la décision de journée :
SKIER / MANGER / SE REPOSER / RENTRER.

La décision doit dépendre au minimum de :
- heure et temps restant ;
- faim / énergie / besoin de repos ;
- disponibilité et ouverture des services ;
- temps de trajet réel via le réseau du domaine ;
- files d'attente et capacité quand les données existent ;
- profil du visiteur ;
- objectif persistant de piste quand il est en déplacement.

IMPORTANT : ne pas introduire un GOAP lourd immédiatement. Consolider d'abord les besoins, contraintes temporelles et coûts de déplacement. Le GOAP pourra ensuite orchestrer les décisions de haut niveau sur des données fiables.

## GPS / déplacements
Conserver le principe du GPS du domaine déjà travaillé : un visiteur choisit un objectif et emprunte les pistes/remontées nécessaires au lieu de choisir arbitrairement chaque segment. Ne pas casser les déplacements qui fonctionnent déjà.

Règles :
- montée uniquement via remontée mécanique ;
- descente physiquement sur piste ;
- pas de téléportation pour corriger un trajet ;
- conserver l'objectif pendant les étapes intermédiaires ;
- considérer temps de trajet, attente et contraintes de fermeture dans les futurs coûts de routage.

## Remontées — à préserver
Le placement départ/arrivée a demandé de nombreux correctifs et la phase 63 était jugée correcte par l'utilisateur. Ne pas réécrire ce système sans nécessité.
Le joueur choisit le point exact ; une portée maximale doit être signalée/refusée, jamais corrigée silencieusement en déplaçant la station.

## Construction et terrain
- arbres supprimés uniquement dans l'emprise réelle après confirmation ; annulation = aucun arbre supprimé ;
- bâtiments : terrain adapté sous l'emprise avec raccord naturel autour ;
- stations de remontées : terrassement minimum nécessaire ;
- pistes sous télésiège/télécabine autorisées ; traversée de téléski interdite ;
- toujours afficher un récapitulatif avant validation finale (prix, longueur/distance, dénivelé/pente/difficulté ou équivalent).

## UI/UX
Interface sobre, jolie, lisible, menus progressifs/collapsibles. Éviter superpositions et micro-gestion inutile. Les textes doivent rester lisibles indépendamment du zoom caméra.

## Priorités de développement proposées
1. 3.29 : besoins simples + décision Ski / Manger / Repos / Rentrer.
2. 3.30 : connecter restaurants/services réels et temps de trajet au choix.
3. 3.31 : satisfaction et conséquences d'une mauvaise journée (attente, faim, fatigue, retour tardif).
4. Ensuite seulement : évaluer l'intérêt d'un GOAP de haut niveau.
5. Dès qu'un PC Unity est disponible : test visuel massif 3.26+ et test gameplay horloge 3.27 / visiteurs 3.28 avant modifications profondes.

## Méthode de travail obligatoire pour Work
- Lire ce fichier en premier.
- Inspecter le code réel avant toute modification : ne jamais se fier uniquement aux anciens récapitulatifs.
- Travailler depuis cette base 3.28.
- Faire des changements petits, identifiables et réversibles.
- Ne pas toucher aux systèmes stables sans nécessité.
- Conserver la compatibilité Unity 6 utilisée par le projet.
- Après chaque brique : créer TEST_XXX.md expliquant changements et protocole de test.
- Produire un ZIP COMPLET du projet, pas seulement un patch.
- Vérifier l'intégrité du ZIP.
- Ne jamais annoncer « testé/fonctionnel dans Unity » sans compilation/Play Mode réellement effectué.
- Si un doute existe sur un ancien système, l'auditer dans le code avant de le modifier.

## Ce que Work doit optimiser
Work peut prendre davantage de temps pour auditer plusieurs scripts liés avant de coder. Chercher d'abord les dépendances réelles entre horloge, VisitorDayPlan, navigation, remontées et services. Éviter les doublons de managers et les horloges concurrentes. Une seule source de vérité temporelle doit piloter la simulation.

## Objectif produit
Une station que le joueur peut regarder vivre : les visiteurs arrivent, s'équipent si nécessaire, marchent, prennent réellement les remontées, skient, choisissent leurs activités selon leurs besoins et le temps disponible, puis rentrent de façon crédible. Le tout doit rester FUN et lisible avant d'être ultra-réaliste.
