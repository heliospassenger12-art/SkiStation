# SkiStation 2.61 — Montagne neige / roche

Base : 2.60.

## Ajout
- Surface du massif répartie visuellement en 5 familles : neige fraîche, neige compacte, neige soufflée, roche et roche sombre.
- Répartition calculée à partir de la pente, de l'altitude et d'une variation d'exposition pour casser l'aspect blanc uniforme.
- Les faces raides et hautes laissent davantage apparaître la roche ; les zones douces restent skiables visuellement et enneigées.
- Ombres et réception des ombres conservées.

## Sécurité
Passe strictement visuelle : aucun sommet du mesh n'est déplacé, collider inchangé, hauteurs gameplay inchangées. Aucun changement aux pistes, visiteurs, remontées, routes ou placement des constructions.

## Test Unity
1. Ouvrir la scène de gestion habituelle et Play.
2. Observer le massif depuis une vue assez large : les ruptures neige/roche doivent suivre naturellement les pentes.
3. Vérifier le front de neige : il doit rester majoritairement clair et lisible.
4. Tester une remontée/piste déjà fonctionnelle pour confirmer l'absence de régression.

Compilation et rendu Unity non exécutés dans l'environnement de génération.
