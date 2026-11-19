# Audit avant modification — Gestion 3.93

Base exclusive reçue : Gestion 3.92 Hybrid Motion clôturée (2), SHA-256 de73e7ad4d8f82bfec3b903dbe3ef4ac8b0c69a2ef340ccb841d15a736855588. 4 728 fichiers ; CRC et 4 727 empreintes du manifeste contrôlés avant extraction. Aucun retour à la 3.92 initiale. Unity 6000.3.7f1 ; validation précédente NO_GO_UNVERIFIED, essais Unity encore absents.

| Propriétaire réel | Décision | Constat / extension nécessaire |
|---|---|---|
| SkierRuntime | EXTEND | Seul moteur MoveTo au sol ; vitesse fixe 6 m/s, pose parfois interpolée en 3D sans surface. Déléguer uniquement la descente active à une trajectoire légère depuis son Update. |
| SkierRouteAgent | EXTEND | GPS, file, transfert, piste courante, index et reprise. Route complète d'une piste ; conserver ses champs pour les anciens checkpoints et lui faire exécuter le tronçon GPS. |
| IntegratedSkiDomainNetworkRuntime | EXTEND | Graphe unique pistes/remontées/transferts ; liens de piste entiers. Découper aux jonctions physiques validées sans second graphe ni nouveau visiteur. |
| MultiLiftDestinationPlanner301 | EXTEND | Dijkstra avec difficulté, objectif précis, attente, budget de retour ; adapter le coût et le but aux tronçons, garder l'objectif aux jonctions. |
| SlopeRuntime | KEEP | Points, largeurs, révision, difficulté, ouverture et comptage historique disponibles. L'observation locale sera dérivée des vraies descentes. |
| LiftPassengerDisembarkRuntime / LiftPostArrivalRoutingRuntime | KEEP/EXTEND | Vidage et dégagement réels avant navigation. Écarter aussi les replis vers une difficulté interdite. |
| SkierMountainTransferRuntime / SkierAlternateRouteRuntime | EXTEND | Replis directs historiques ; refuser un accès non marchable ou hors niveau, ne pas inventer un corridor pour traverser une vallée. |
| VisitorDayDecision329 / VisitorDayPlan328 | KEEP | Repas, repos, services, départ et budget de journée restent propriétaires. |
| IntelligentSkiDayRuntime / VisitorIntentRuntime / GOAP shadow | KEEP | Intentions et GOAP expérimental existent ; ne pas ajouter un nouveau décideur. |
| VisitorPartyRuntime346 / Member346 / FamilySkiSchoolRuntime347 | KEEP | Groupes, cours et contraintes de niveau demeurent natifs ; pas de rendez-vous supplémentaire. |
| SlopeVisitorExperienceRuntime | KEEP | Difficultés autorisées par profil et satisfaction de fin de piste existantes. Aucune nouvelle formule de satisfaction. |
| ManagementSurface / altitude / relief 360° | KEEP | Surface autoritaire ; cache dérivé de profils terrain, pas de façade face caméra. |
| LiftBoardingVisualAdapter392 / VisitorVisualPool392 | EXTEND/KEEP | LOD et pool de modèles réels existants. Coordonner la posture de descente avec cet unique propriétaire de rendu, sans réinitialiser équipements/identité. |
| TrafficStateCodec391 / checkpoint schéma 9 | EXTEND | Champs privés exacts : ne pas casser les anciens acteurs. Ajouter un composant explicite pour progression ; caches non sérialisés, historiques bornés. |
| Arcade / Hybrid Motion | KEEP | Tous les fichiers de contrôle/physique/pose Arcade et les modules Hybrid doivent rester identiques à la base. |

Aucun contrôleur supprimé ou marqué DEPRECATE à ce stade. Principales lacunes observées : choix de repli sans vérification systématique du niveau, coûts de piste entière, comptage de traversée complète même aux jonctions, absence de vitesse/trajectoire locale réelle. Les limites de sauvegarde carrière 3.92 restent explicites (services engagés, cours, séjours, routes, ressources non cataloguées). Le nouveau travail ne doit pas les contourner.

Compilation C#/Unity, Play Mode, visuels, vrais fichiers de partie et performances : NON EXÉCUTÉS. Cet audit est une lecture des sources et une vérification d'archive.
