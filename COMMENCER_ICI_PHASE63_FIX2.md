# Phase 63 — Correctif 2 : accès aux remontées et départ depuis la montagne

Ce ZIP complet remplace le précédent correctif. Extraire dans un nouveau dossier puis ouvrir Assets/Scenes/SkiStation_Bootstrap.unity.

## Défauts identifiés dans le code
Le premier correctif avait ajouté une condition de connexion à 28 m alors que le système de sortie de gare utilise 90 m. L'accès au premier lift et la récupération du skieur par la boucle visible étaient limités à 35 m et 3 m de montée. Enfin, le nouvel appel de départ pour l'état Leaving pouvait donner directement la route comme destination depuis la montagne.
Les photos montrent REPART mais ne prouvent pas quel enchaînement exact a eu lieu : aucun journal de session ni reproduction Unity disponible ici.

## Changements
- Approche de la gare basse jusqu'à 600 m. Le segment au sol est échantillonné tous les 10 m ; sol absent ou pente locale supérieure à environ 25 % : accès refusé. Ce n'est pas une navigation autour des bâtiments.
- Connexion sommet de remontée / début de piste harmonisée à 90 m dans la recherche initiale, la boucle visible et la sortie de gare. Aucun déplacement des gares.
- Marche vers la gare maintenue au sol.
- Départ vers la route autorisé uniquement dans le secteur bas : écart vertical de 20 m au maximum par rapport à l'entrée, distance maximale de 1 200 m et trajet au sol compatible.
- Un ordre Leaving reçu en altitude devient une demande en attente. La descente ou le transfert de sortie de gare garde la priorité ; si aucune piste n'est accessible, le visiteur peut rester bloqué en haut, mais n'est pas envoyé directement à la route.
- Les décisions aléatoires de départ au terminal nécessitent aussi une position réellement proche du fond de vallée.
- La recherche ordinaire d'une remontée respecte la décision en cours à la gare haute.

## Test conseillé (vitesse 1x, en journée)
1. Construire comme dans ton test une gare basse près du village et une piste dont le départ est près de la gare haute.
2. Suivre un visiteur équipé : route, front de neige, marche vers gare, montée, piste bleue, retour.
3. Faire deux rotations. Le visiteur ne doit pas afficher REPART au sommet ni couper directement vers la route.
4. Vérifier ensuite un départ normal dans le secteur bas en fin de journée.
Si cela bloque, fournir l'heure du jeu, le libellé du visiteur et les dernières lignes de Console. Ces éléments distinguent attente de matériel, accès refusé, correspondance piste et départ.

## Validation et limites
Analyse de syntaxe C# et comparaison des fichiers effectuées hors Unity. Les scripts de placement des gares, les scènes, le relief, l'arcade et les méthodes de déplacement de la boucle phase 63 sont conservés. Unity non disponible : compilation et parcours en Play NON exécutés ici.
La boucle phase 63 continue de déplacer spécialement UN skieur sur la ligne ; les autres passent par les files et véhicules. Aucun nouveau système de voitures, de navigation avec obstacles ou d'évacuation ajouté.
