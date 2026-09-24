SkiStation 2.51 — prototype de terrassement sur le massif de gestion actuel

Modifications :
- Confirmation bâtiment : plateforme de son emprise et raccordement de 5 m, arbres retirés sur l'emprise + marge.
- Confirmation remontée : plateformes locales 13 x 13 m aux deux gares, arbres retirés autour des gares et sur le tracé.
- Le mesh visible et son MeshCollider sont mis à jour ensemble ; les requêtes de hauteur du massif consultent le mesh modifié.
- Les pistes conservent leur suppression d'arbres existante.

Limites connues :
- Ce n'est pas encore un système de murs de soutènement ou de déneigement visuel des fenêtres.
- Les plateformes des gares sont appliquées après création de la remontée : vérifier visuellement l'ancrage de la gare.
- Les emprises sont alignées sur les axes du monde et non sur une rotation de bâtiment.
- Aucun test de compilation ou d'exécution Unity n'a été effectué ici.

Test conseillé : construire un chalet sur une légère pente puis une remontée ; vérifier le sol, les arbres, les gares et les déplacements.
