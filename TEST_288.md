# SkiStation 2.88 — Finition « jeu commercial »

Base : 2.87.

## Ajouts
- Les bâtiments réellement confirmés par le joueur reçoivent automatiquement une finition visuelle supplémentaire : soubassement pierre, entrée, auvent enneigé, vitrages/éclairage chaud, volets et balcons lorsque la hauteur le permet.
- Micro-vie à l'entrée : panneau, rack à skis, skis et petits bacs alpins.
- Les micro-détails sont masqués automatiquement à grande distance (> ~520 m caméra) afin de conserver une vue générale lisible et de limiter le coût de rendu.
- Le système rescane périodiquement : les bâtiments construits après le démarrage sont aussi décorés.

## Sécurité
- Visuel uniquement : aucun collider ajouté.
- Aucun changement de construction, finances, visiteurs, pistes, remontées ou Arcade.

## Test Unity
1. Ouvrir la scène de gestion et lancer Play.
2. Construire/observer plusieurs bâtiments du village.
3. Vérifier les détails de façade et d'entrée à zoom proche/moyen.
4. Dézoomer fortement : les micro-détails doivent disparaître sans modifier les bâtiments.
5. Vérifier l'absence d'erreur rouge.

Compilation et Play Mode Unity non exécutés dans l'environnement de génération.
