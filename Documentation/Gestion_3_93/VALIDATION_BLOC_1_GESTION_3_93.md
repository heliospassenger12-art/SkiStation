# Bloc 1 — campagne à exécuter

Gestion 3.93 Bloc 1 préparé structurellement. Compilation Unity, Play Mode et validation réelle des trajectoires de descente, des animations, des transitions GPS et de la compatibilité Arcade restent à effectuer dans Unity.

- 1. Audit préalable : AUDIT_SKI_VISITOR_MOVEMENT_3_93.md. État Unity : NOT_RUN.
- 2. État unifié : VisitorSkiDescentSnapshot393. État Unity : NOT_RUN.
- 3. Gare vers piste : dégagement natif puis première étape GPS. État Unity : NOT_RUN.
- 4. Trajectoire : VisitorSkiDescentPath393. État Unity : NOT_RUN.
- 5. Vitesse/pente : TargetSpeed et AdvanceCore. État Unity : NOT_RUN.
- 6. Virages : Offset déterministe. État Unity : NOT_RUN.
- 7. Corridor : bande utile et validation des échantillons. État Unity : NOT_RUN.
- 8. Terrain : surface autoritaire et normales cachées. État Unity : NOT_RUN.
- 9. Niveaux : MaxDifficulty natif. État Unity : NOT_RUN.
- 10. Anticipation : courbe, largeur, voisin et bout du tronçon. État Unity : NOT_RUN.
- 11. Fin de piste : FinishSlope et objectif conservé. État Unity : NOT_RUN.
- 12. Variations : VisitorId stable. État Unity : NOT_RUN.
- 13. Visuel : pose légère après le propriétaire 3.92. État Unity : NOT_RUN.
- 14. IA préparatoire : audit des décideurs existants. État Unity : NOT_RUN.
- 15. Premier parcours : SkiDescentCampaign393 fourni. État Unity : NOT_RUN.

Contrôles préparés : SkiDescentChecks393 (Editor), SkiDescentCampaign393 (Play), SkiDescentRoundTrip393, LiftTrafficRoundTrip391 et SkiDescentBench393. Les menus C# n’ont pas été exécutés ici. Cas : verte/bleue/rouge/noire, novice/expert, piste étroite, courbe/bosse, arbres/bâtiments, intersection, trois convergences, plusieurs versants, familles/amis/équipe, piste et remontée fermées, repas/repos, pause/accéléré, hors caméra et rechargements.

Le checkpoint demeure matériel + trafic compatible, pas une sauvegarde Carrière universelle. Services engagés, cours actifs, séjours, réseau routier logique, certains chantiers et ressources/avatars non catalogués peuvent toujours faire refuser une sauvegarde. Aucun refus n’est contourné. La réflexion des états et IL2CPP restent non vérifiés. Une surface/corridor invalide arrête prudemment la progression avec diagnostic ; cela ne garantit pas encore un parcours sans blocage dans toutes les scènes. Les animations procédurales sont simples ; les rigs et équipements existants ne sont pas remplacés. La précision de contact sur rochers/bosses et les croisements doivent être observés. Le GPS reste Dijkstra synchrone : les connexions sont cachées, mais un ordonnanceur global de demandes n’est pas ajouté. Les très grands graphes, allocations de recherche, paramètres de connexion inhabituels et limites de requêtes physiques restent à profiler.
