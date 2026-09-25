# SkiStation 2.85 — Cohésion visuelle domaine

Base : 2.84.

## Ajout
- Détection des vraies pistes et vraies remontées construites.
- Lorsqu'une extrémité de piste est réellement proche d'une gare (< 34 m), ajout d'un raccord visuel neigeux discret.
- Bordures neigeuses et petite signalétique de jonction pour mieux lire le domaine en vue panoramique.
- Aucun raccord artificiel n'est créé entre éléments éloignés.

## Sécurité
- Visuel uniquement : tous les colliders des primitives ajoutées sont supprimés.
- Aucun changement de logique des pistes, remontées, visiteurs, constructions ou Arcade.
- La caméra 2.77 est conservée.

## Test Unity
1. Ouvrir la scène de gestion et lancer Play.
2. Construire/observer une piste dont une extrémité arrive à moins de ~34 m d'une gare.
3. Vérifier le raccord neige + petite signalétique.
4. Dézoomer et vérifier que piste et remontée forment un ensemble plus cohérent.
5. Vérifier l'absence d'erreurs rouges.

Compilation et Play Mode non exécutés dans cet environnement.
