# Bloc 3 — campagne à exécuter

Gestion 3.93 Bloc 3 préparé structurellement. Compilation Unity, Play Mode et validation réelle de la fréquentation, de la congestion et des itinéraires alternatifs restent à effectuer dans Unity.

- 31. Présence réelle : observations dérivées des acteurs réels. État Unity : NOT_RUN.
- 32. Densité : cellules et unités explicites. État Unity : NOT_RUN.
- 33. Répartition : variantes de même trajet. État Unity : NOT_RUN.
- 34. Espacement : voisin longitudinal, vitesse progressive. État Unity : NOT_RUN.
- 35. Convergences chargées : population du tronçon commun. État Unity : NOT_RUN.
- 36. Passages étroits : largeur utile et réduction des virages. État Unity : NOT_RUN.
- 37. Familles : règles natives, priorité au niveau individuel. État Unity : NOT_RUN.
- 38. Amis/sportifs : profils réels sans promotion. État Unity : NOT_RUN.
- 39. Saturation : seuils configurables et durée. État Unity : NOT_RUN.
- 40. Durées : parcours complets et mesures libres distincts. État Unity : NOT_RUN.
- 41. Alternatives : coût local dans le GPS existant. État Unity : NOT_RUN.
- 42. Stabilité GPS : engagement tant que valide et avantage limité. État Unity : NOT_RUN.
- 43. Diagnostics : signal persistant au moteur 3.86. État Unity : NOT_RUN.
- 44. Contrat futur : VISITOR_AI_TRAFFIC_INTEGRATION_3_93.md. État Unity : NOT_RUN.
- 45. Forte fréquentation : campagne réelle préparée. État Unity : NOT_RUN.

Contrôles préparés : SkiDescentChecks393 (Editor), SkiDescentCampaign393 (Play), SkiDescentRoundTrip393, LiftTrafficRoundTrip391 et SkiDescentBench393. Les menus C# n’ont pas été exécutés ici. Cas : verte/bleue/rouge/noire, novice/expert, piste étroite, courbe/bosse, arbres/bâtiments, intersection, trois convergences, plusieurs versants, familles/amis/équipe, piste et remontée fermées, repas/repos, pause/accéléré, hors caméra et rechargements.

Le checkpoint demeure matériel + trafic compatible, pas une sauvegarde Carrière universelle. Services engagés, cours actifs, séjours, réseau routier logique, certains chantiers et ressources/avatars non catalogués peuvent toujours faire refuser une sauvegarde. Aucun refus n’est contourné. La réflexion des états et IL2CPP restent non vérifiés. Une surface/corridor invalide arrête prudemment la progression avec diagnostic ; cela ne garantit pas encore un parcours sans blocage dans toutes les scènes. Les animations procédurales sont simples ; les rigs et équipements existants ne sont pas remplacés. La précision de contact sur rochers/bosses et les croisements doivent être observés. Le GPS reste Dijkstra synchrone : les connexions sont cachées, mais un ordonnanceur global de demandes n’est pas ajouté. Les très grands graphes, allocations de recherche, paramètres de connexion inhabituels et limites de requêtes physiques restent à profiler.
