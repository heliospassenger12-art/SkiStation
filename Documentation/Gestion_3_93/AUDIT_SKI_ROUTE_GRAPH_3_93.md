# Audit du graphe réel

KEEP : SlopeRuntime, identités, difficultés, LiftRuntime, destinations et règles de retour. EXTEND : IntegratedSkiDomainNetworkRuntime, mêmes SkiDomainLink et même Dijkstra. Les nouvelles coupures sont des tronçons de pistes existantes, pas un second graphe. EXTEND : caches de connexions/projections, coût local et exécution des bornes from/to. Aucun champ ajouté à SkiDomainLink afin de conserver les anciens champs privés de checkpoint. Les croisements sans jonction réelle restent non connectés. DEPRECATE : aucun système supprimé.

Les anciens consommateurs visuels du graphe peuvent maintenant observer plusieurs liens portant la même piste : ne pas assimiler le nombre d’arêtes au nombre d’infrastructures ou de visiteurs. Les gardes de fermeture, recherche de retour et métriques doivent être testées sur ces tronçons.
