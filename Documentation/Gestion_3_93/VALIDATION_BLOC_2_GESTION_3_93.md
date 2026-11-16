# Bloc 2 — campagne à exécuter

Gestion 3.93 Bloc 2 préparé structurellement. Compilation Unity, Play Mode et validation réelle du GPS multi-pistes, des bifurcations et des enchaînements de remontées restent à effectuer dans Unity.

- 16. Audit du graphe : même graphe intégré, segmentation. État Unity : NOT_RUN.
- 17. Destination précise : DestinationObjective3782 et planner natif. État Unity : NOT_RUN.
- 18. Choix après remontée : première arête réellement accessible. État Unity : NOT_RUN.
- 19. Bifurcations : coupures aux jonctions validées. État Unity : NOT_RUN.
- 20. Convergences : tronçon commun réel. État Unity : NOT_RUN.
- 21. Pistes successives : tronçon terminé, même objectif. État Unity : NOT_RUN.
- 22. Difficultés : filtrage natif plus replis sécurisés. État Unity : NOT_RUN.
- 23. Secteurs : coordonnées monde 360°. État Unity : NOT_RUN.
- 24. Chaînes de remontées : files et transport 3.92 intacts. État Unity : NOT_RUN.
- 25. Temps complet : coûts tronçons + marche + attente + lift. État Unity : NOT_RUN.
- 26. Temps restant : VisitorDayPlan328 et retour natifs. État Unity : NOT_RUN.
- 27. Fermetures : recovery natif et arêtes invalidées. État Unity : NOT_RUN.
- 28. Choix stables : Stabilize393 et seuil de gain. État Unity : NOT_RUN.
- 29. Contrat IA/GPS : EstimateNavigation393 en lecture. État Unity : NOT_RUN.
- 30. Parcours multi-pistes : scénario préparé sur domaine réel. État Unity : NOT_RUN.

Contrôles préparés : SkiDescentChecks393 (Editor), SkiDescentCampaign393 (Play), SkiDescentRoundTrip393, LiftTrafficRoundTrip391 et SkiDescentBench393. Les menus C# n’ont pas été exécutés ici. Cas : verte/bleue/rouge/noire, novice/expert, piste étroite, courbe/bosse, arbres/bâtiments, intersection, trois convergences, plusieurs versants, familles/amis/équipe, piste et remontée fermées, repas/repos, pause/accéléré, hors caméra et rechargements.

Le checkpoint demeure matériel + trafic compatible, pas une sauvegarde Carrière universelle. Services engagés, cours actifs, séjours, réseau routier logique, certains chantiers et ressources/avatars non catalogués peuvent toujours faire refuser une sauvegarde. Aucun refus n’est contourné. La réflexion des états et IL2CPP restent non vérifiés. Une surface/corridor invalide arrête prudemment la progression avec diagnostic ; cela ne garantit pas encore un parcours sans blocage dans toutes les scènes. Les animations procédurales sont simples ; les rigs et équipements existants ne sont pas remplacés. La précision de contact sur rochers/bosses et les croisements doivent être observés. Le GPS reste Dijkstra synchrone : les connexions sont cachées, mais un ordonnanceur global de demandes n’est pas ajouté. Les très grands graphes, allocations de recherche, paramètres de connexion inhabituels et limites de requêtes physiques restent à profiler.
