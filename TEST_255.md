# SkiStation 2.55 — Fiche visiteurs

Base : archive 2.54, sources conservees. Aucun changement de logique de deplacement,
de placement des gares, de vente, de location ou de boucle de ski.

## Utilisation
- En gestion, appuyer sur V pour ouvrir/fermer la liste des visiteurs presents.
- Cliquer sur un nom dans la liste ou sur un visiteur visible dans le monde.
- La fiche affiche objectif, mode d'arrivee, equipement actuel, forfait du jour,
  satisfaction, destination et attente connue. Elle se met a jour pendant la simulation.
- Echap ou X ferme la fiche. F6 rend la place a la fiche structures.
- Les textes restent a taille ecran fixe quand la camera zoome.
- Les menus a gauche, en haut et en bas sont exclus de la selection du monde :
  utiliser V si un visiteur se trouve sous ces zones ou s'il est trop petit.

## Limites explicites
- La liste concerne les visiteurs physiquement apparus, pas les passagers encore en vehicule.
- Le materiel affiche l'equipement actuel, sans pretendre distinguer equipement personnel et location.
- Les attentes au guichet regroupent file et service ; aucun rang ou delai estime n'est invente.
- Pour un visiteur au repos, si la simulation n'expose pas de cause precise, la fiche le dit.
- Pas de relance, teleportation, vente ou changement d'etat depuis cette fiche.

## Tests a faire dans Unity
1. Ouvrir Bootstrap, lancer Play, appuyer sur V : liste visible, y compris avant les arrivees.
2. Choisir un pieton, un arrivant en voiture et un arrivant en autocar.
3. Observer les etapes location/guichet : equipement et forfait evoluent apres le service.
4. Suivre approche, file, montee et descente ; confirmer que plusieurs boucles continuent.
5. Zoomer, faire defiler la liste : le texte garde sa taille et la molette sur la fiche ne zoome pas.
6. Fermer avec X/Echap ; ouvrir une fiche structure avec F6 ; verifier l'absence de superposition.
7. Construire une piste, une remontee, un batiment ou une route : pas de selection visiteur parasite.
8. Laisser partir le visiteur selectionne : pas d'erreur, la liste reste utilisable.
9. Verifier que la scene arcade ne presente pas cette fiche.

## Verification hors Unity
405 fichiers C# analyses sans erreur de syntaxe (tree-sitter).
Comparaison des methodes de simulation avec 2.54 : aucune modification.
Arcade, scenes, placement des remontees et vehicules identiques a 2.54.
Ce controle n'est ni une compilation Unity ni un test en Play.
