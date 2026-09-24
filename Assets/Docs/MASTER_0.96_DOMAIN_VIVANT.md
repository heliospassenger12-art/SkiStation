# SkiStation — Master 0.96

## Domaine skiable vivant et lisible

Objectif : rendre la fréquentation du domaine visible sans ajouter de nouvelle simulation complexe.

### Principes
- La population réelle reste la seule source de vérité.
- Aucun visiteur, véhicule ou spectateur artificiel n'est créé.
- Les pistes et remontées existantes sont observées, pas remplacées.
- Les comportements des skieurs produisent naturellement l'affluence.
- Le système routier 0.94/0.95 n'est pas étendu.

### Ajouts
- `SkiDomainFlowSnapshot` : état synthétique du domaine.
- `SkiDomainFlowRuntime` : agrégation périodique des visiteurs, pistes, remontées et véhicules.
- `SkiDomainFlowBootstrap` : démarrage automatique.
- `SkiDomainFlowUI` : overlay diagnostic optionnel.

### Indicateurs
- visiteurs réels
- visiteurs en descente
- attente remontée
- remontée
- repos
- arrivées / départs
- pistes ouvertes
- remontées ouvertes
- véhicules routiers réels
- satisfaction moyenne

## Limite volontaire
Cette version ne crée pas encore de nouvelle IA complexe de choix de piste. Elle prépare une lecture fiable de l'affluence à partir du système existant.

## Prochaine étape proposée
0.97 — construction et visualisation des pistes : tracé, largeur variable, adaptation au terrain, panneaux et connexions.
