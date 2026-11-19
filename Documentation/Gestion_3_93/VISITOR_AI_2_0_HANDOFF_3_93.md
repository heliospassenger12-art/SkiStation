# Préparation IA visiteurs 2.0 — aucun remplacement en 3.93

| Responsabilité | Propriétaire réel | Déclencheurs / limites |
|---|---|---|
| Skier, repas, repos, service, retour | VisitorDayDecision329 / IntelligentSkiDayRuntime / VisitorDayPlan328 | Choix au front accessible, besoins, budget, repas terminé ; conserve OwnsMovement. |
| Destination de piste | DestinationObjectiveRuntime3782 / VisitorIntentRuntime / groupe 346 | Objectif précis, terminé ou invalidé ; pas à chaque virage. |
| Compétence / équipement | CustomerProfile / MaxDifficulty / VisitorEquipmentNeeds332 | Ne pas confondre matériel et autorisation de difficulté. |
| Groupe et cours | VisitorPartyRuntime346 / FamilySkiSchoolRuntime347 | Contraintes natives, niveau individuel, pas de rendez-vous nouveau. |
| Propositions GOAP | VisitorGoapShadowRuntime334 / Guardrails335 / mécanismes natifs déjà autorisés | Comparer aux décisions réelles ; aucun deuxième arbitre ajouté. |
| Itinéraire | MultiLiftDestinationPlanner301 / IntegratedSkiDomainNetworkRuntime | Réseau changé, nouvelle étape, objectif demandé ; capacités, attente, niveau, retour. |
| Exécution | SkierRouteAgent / SkierRuntime / transports / services | Une autorité physique selon l’activité ; la descente est déléguée par le moteur natif. |

Doublons identifiés : anciens choix locaux ResortRouteNetwork, IntegratedSkierDomainNavigator et FindPlayerBuiltSlope peuvent concurrencer un objectif si utilisés sans planner. Le planner existant reste prioritaire ; les replis sont filtrés, pas supprimés arbitrairement. Le modèle de journée, GOAP shadow et groupe peuvent proposer des objectifs : conserver leurs contrats et télémétrie avant toute future arbitrage.

Point d’extension livré : VisitorNavigationRequest393 → EstimateNavigation393 renvoie faisabilité et temps sans décider de faim/repos. Le snapshot de descente et les mesures de piste apportent progression, niveau, densité, attente et état réseau. Les causes de décision sont écrites au calcul dans la fiche existante.

Étapes futures recommandées pour étudier la 3.94, sans la commencer : valider d’abord cette boucle dans Unity ; inventorier les priorités réellement actives et leurs conflits ; comparer Utility/GOAP/arbres sur les mêmes observations ; essayer un mode ombre explicable ; n’autoriser un seul arbitre qu’après tests de journée et de retour. Aucun nouveau planner GOAP, arbre ou Utility AI installé dans cette version.
