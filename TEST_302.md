# SkiStation 3.02 — Domaine vivant / recalcul dynamique

Base : 3.01 NAVIGATION MULTI-REMONTEES.

## Ajout
- Le graphe du domaine skiable est maintenant surveille en Play Mode.
- Une nouvelle piste ou remontee est integree sans redemarrer la partie.
- Une piste fermee est retiree des choix de navigation au prochain recalcul.
- Une remontee fermee ou en panne est retiree des itineraires.
- Une reouverture/reparation la rend de nouveau disponible.
- Recalcul uniquement lorsqu'un changement d'infrastructure est detecte (controle toutes les ~3 s).
- Aucun deplacement magique et aucune modification de l'etat des infrastructures.

## Test Unity conseille
1. Lancer une scene de gestion et laisser des visiteurs skier.
2. Construire une nouvelle piste/remontee pendant le Play Mode.
3. Verifier qu'elle peut ensuite entrer dans les choix d'itineraire.
4. Fermer une piste ou provoquer/observer une panne de remontee.
5. Verifier que les nouveaux itineraires l'evitent apres quelques secondes.
6. Reouvrir/reparer et verifier qu'elle redevient utilisable.
7. Verifier la Console : aucune erreur rouge.

Compilation et Play Mode non executes dans l'environnement de generation.
