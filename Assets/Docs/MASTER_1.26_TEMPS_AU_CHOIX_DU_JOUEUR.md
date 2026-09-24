# SkiStation Master 1.26 — Temps au choix du joueur

## Principe
Le temps de jeu ne doit jamais imposer un rythme artificiel. Le joueur peut soit enchaîner la construction en conservant une présentation de jour permanente, soit profiter d’une station vivante avec une vraie présentation jour/nuit.

## Deux modes réversibles à tout moment
- **Tycoon — Jour continu** : l’éclairage/la présentation reste diurne. Le calendrier interne continue d’avancer normalement : jours, saisons, recherches, compétitions et événements ne sont pas bloqués.
- **Immersion — Jour/Nuit** : la présentation suit le temps interne, avec vie nocturne et opérations nocturnes visibles.

Le changement est réversible à tout moment. Le temps interne n’est jamais remis à zéro et GameClock reste la source de vérité.

## Damage / entretien
En mode Tycoon, le damage n’impose pas une phase d’attente visible. Les demandes de damage sont résolues hors écran à la frontière de journée, ce qui conserve la cohérence de gestion sans créer de période plate. En mode Immersion, le comportement 1.25 reste actif : préférence nocturne, piste fermée pendant le damage, aucun skieur présent sur la piste lors de l’opération visible.

## Philosophie
Le joueur peut donc :
1. **foncer** : construire, accélérer le temps et enchaîner les décisions ;
2. **prendre son temps** : observer les clients, tester les pistes, essayer les activités et profiter de la station ;
3. passer de l’un à l’autre sans recommencer la partie.

Le système ne crée aucune population artificielle pour donner l’impression que la station est vivante. La vie visible reste toujours issue de la simulation de gestion réelle.
