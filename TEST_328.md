# SkiStation 3.28 — journée dynamique des visiteurs

Base directe : 3.27.

## Ajouts
- `VisitorDayPlan328` donne à chaque visiteur une heure de retour légèrement différente autour de 17h30.
- Le budget utilise la vraie horloge 3.27.
- Avant un nouveau cycle depuis le bas du domaine : estimation montée + descente + marge de décision + réserve de retour.
- Si le cycle ne tient plus dans la journée, le visiteur choisit de rentrer au lieu de reprendre une remontée.
- Aux zones terminales, la fenêtre de retour devient un vrai critère de décision.
- Un visiteur déjà en altitude n'est jamais téléporté ni bloqué : les contrôleurs existants gardent l'autorité pour le ramener physiquement vers le bas.
- Les durées de pistes sont converties avec la même échelle temporelle centrale que les remontées.

## Test Unity conseillé
1. Ouvrir une scène de gestion avec au moins une remontée et une piste fonctionnelles.
2. Observer plusieurs visiteurs pendant la journée.
3. Vérifier qu'ils continuent normalement le matin / milieu de journée.
4. Approcher 16h30–18h30 avec l'accélération temporelle.
5. Vérifier qu'au front de neige un visiteur ne repart pas sur un cycle trop long et passe en retour/départ.
6. Vérifier qu'un visiteur déjà en altitude finit physiquement son trajet sans téléportation.
7. Vérifier Pause / x1 / x3 / x8.

## Important
Compilation Unity et Play Mode non exécutés dans cet environnement.
