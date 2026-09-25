# SkiStation 3.03 — Navigation anti-bouchons

Base : 3.02.

## Ajout
- Le planificateur multi-remontées tient maintenant compte de la file réelle de chaque remontée.
- La taille de file est convertie en temps d'attente estimé à partir de la capacité horaire réelle de la remontée.
- Une remontée chargée reste utilisable : elle devient simplement plus coûteuse dans le calcul d'itinéraire.
- Le visiteur peut donc choisir une chaîne légèrement plus longue si elle est estimée plus rapide.
- La pénalité d'attente est plafonnée pour éviter des comportements extrêmes.
- Aucun téléport, aucune modification des files, des remontées, des pistes ou de la construction.

## Test Unity conseillé
1. Créer au moins deux chemins possibles vers une zone du domaine.
2. Provoquer une file importante sur une des remontées.
3. Observer si de nouveaux visiteurs répartissent davantage leurs choix vers l'alternative disponible.
4. Fermer/réouvrir une remontée pour vérifier que 3.02 + 3.03 continuent à recalculer correctement.
5. Vérifier l'absence d'erreurs rouges.

Compilation et Play Mode Unity non exécutés dans l'environnement de génération.
