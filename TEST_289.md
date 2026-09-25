# SkiStation 2.89 — Vie du domaine multi-échelle

Base : 2.88.

## Ajouts
- Les pistes réellement construites reçoivent une entrée visuelle identifiable au sommet : portique discret et couleur de difficulté.
- Les gares réelles de remontées reçoivent un habillage léger de zone fréquentée : jalons, fanions, neige tassée visuelle, mobilier de guidage et protection orange.
- Deux niveaux de détail automatiques selon la distance caméra : éléments structurants visibles plus loin (~900 m), micro-détails proches masqués au-delà d'environ 360 m.
- Les nouvelles pistes/remontées sont détectées périodiquement après construction.

## Sécurité
- Visuel uniquement, colliders supprimés.
- Aucun changement des files réelles, du déplacement visiteurs, du fonctionnement des remontées, des pistes, de la construction, des finances ou de l'Arcade.

## Test Unity
1. Lancer la scène de gestion en Play.
2. Construire/observer une piste et une remontée.
3. Vérifier le portique en tête de piste et l'habillage des deux gares.
4. Zoomer/dézoomer : les micro-détails proches doivent disparaître avant les éléments structurants.
5. Vérifier l'absence d'erreur rouge.

Compilation et Play Mode Unity non exécutés dans l'environnement de génération.
