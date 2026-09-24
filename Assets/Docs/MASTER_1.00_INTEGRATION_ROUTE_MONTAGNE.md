# SkiStation Master 1.00 — Intégration réelle du parcours montagne

## Objectif
Relier enfin les trois couches réelles :
- `SkierRuntime` = visiteurs de la simulation de gestion
- `LiftRuntime` / `ResortRouteNetwork` = réseau de remontées
- `SlopeRuntime` / graphe des pistes = domaine skiable

## Ce qui est intégré
- Récupération des briques cœur historiques nécessaires à cette intégration.
- `SkiDomainRouteDecisionRuntime` : observe les vrais skieurs et produit une décision de route.
- Depuis une piste : choix d'une remontée compatible, puis d'une prochaine piste selon le profil visiteur.
- Depuis une remontée : choix d'une piste compatible selon le profil visiteur.
- Prise en compte indirecte des préférences, de la congestion et des caractéristiques réelles des infrastructures via les runtimes existants.
- Nettoyage automatique des décisions des visiteurs disparus.
- Bootstrap automatique, sans création de population artificielle.
- Debug F12 optionnel.

## Règle absolue
Ce système ne crée aucun visiteur, ne crée aucun véhicule et ne déplace aucun skieur. Il décide/observe uniquement. Les mouvements restent pilotés par les systèmes de simulation existants.

## Joueur
La simulation du joueur reste totalement indépendante : aucun score, collision ou décision de l'avatar n'est injecté dans la simulation de gestion.

## Limitation volontaire
La compilation Unity complète n'a pas été exécutée dans cet environnement. Le package est préparé et les fichiers ont été vérifiés statiquement ; un passage dans l'éditeur Unity reste nécessaire pour valider les références/assemblies selon la version Unity utilisée.

## Suite logique 1.01
Faire consommer cette décision par l'agent de déplacement réel des skieurs, avec états explicites :
`LiftBottom → Queue → Boarding → Riding → LiftTop → SlopeAccess → Skiing → SlopeBottom`.
Toujours sans population artificielle et sans toucher à la simulation du joueur.
