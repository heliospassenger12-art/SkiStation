# SkiStation 1.30 — Services & commerces intelligents

## Objectif
Faire passer les services de la station d’un simple enregistrement de consommation à de véritables destinations de journée. Un visiteur choisit un service parce qu’il en a besoin ou parce que son plan de journée lui donne une raison d’y aller.

## Familles couvertes
- Magasins
- Restaurants
- Bars / après-ski
- Location de matériel
- Écoles de ski / leçons
- Activités
- Hébergements

## Variables de gestion
Chaque `StationServiceRuntime` expose : capacité, file, temps de service, prix de base, gamme, qualité, personnel, spécialisation et stock. Une distance de marche et des horaires d’ouverture sont également pris en compte.

## Décision visiteur
`VisitorServicePlannerRuntime` évalue les services disponibles pour chaque visiteur réel. Le score combine :
1. besoin du moment ;
2. intention / plan de journée ;
3. adéquation au profil ;
4. qualité et gamme ;
5. prix ;
6. attente et capacité ;
7. distance ;
8. spécialisation ;
9. météo / horaire de manière préparatoire.

Le système ne crée aucun visiteur et ne téléporte personne. Il demande au `SkierRuntime` de se déplacer vers la destination. Une consommation n’est enregistrée qu’à l’arrivée puis après le temps de service.

## File d’attente
La file est une vraie liste de visiteurs. Une fois la capacité de service atteinte, le visiteur attend. Le temps d’attente dégrade progressivement sa satisfaction. Le service refuse les nouvelles entrées lorsque sa file maximale est atteinte.

## Vérité financière
La vente terminée passe par `StationRuntime.Earn` / `FinanceRuntime` avec la catégorie correspondante. Le prix courant vient de `PricingRuntime`. La consommation est aussi enregistrée dans `VisitorServiceConsumptionRuntime` et les statistiques existantes.

## Statistiques 1.30
Le runtime expose les indicateurs : clients servis, revenu, attente moyenne, satisfaction moyenne, occupation, taux de refus, clients par profil et demande par heure.

## Limite volontaire
Les déplacements continuent d’utiliser le mouvement/routage existant. Cette version ne remplace pas le pathfinding montagne : elle fournit les destinations et l’état de service auxquels le système de déplacement peut se raccorder.


## Phase 2 — Construction & gestion

Le joueur peut désormais construire un service concret et le rendre exploitable par les visiteurs réels. Chaque bâtiment possède une définition runtime avec type, gamme, capacité, file maximale, durée de service, prix, qualité, personnel, stock, horaires et spécialisations.

Le plan de journée 1.29 est consulté par le planner 1.30 : Shop, Restaurant, Leisure et Rest influencent directement le type de service recherché. Une transaction terminée est réinjectée dans le plan afin que la durée du service fasse évoluer la journée du visiteur.

L'ancien système de choix optionnel aléatoire reste présent pour compatibilité mais est désactivé par défaut : le planner intelligent devient l'autorité des décisions de services.

La gestion expose aussi le nombre de servis/refusés, le chiffre d'affaires, la file maximale, l'attente cumulée, la satisfaction et la répartition par profil.
