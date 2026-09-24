# SkiStation Master 1.22 — Réseau de damage par secteurs

Cette brique organise le damage autour de secteurs géographiques de montagne.
Les secteurs sont construits à partir des vraies pistes présentes dans la scène.

## Principes
- aucune piste ou dameuse artificielle ;
- les priorités restent compatibles avec le contrôle manuel ;
- plusieurs dameuses peuvent travailler en parallèle via la flotte 1.20/1.21 ;
- les pistes d'un même secteur sont favorisées afin de limiter les trajets ;
- une piste fortement dégradée conserve une priorité élevée ;
- F7 affiche les secteurs et leur état ;
- le mode instantané reste compatible avec la construction/maintenance instantanée.

Le système reste volontairement léger : il ne crée pas encore un nouveau réseau de chemins physiques dédié aux dameuses.
