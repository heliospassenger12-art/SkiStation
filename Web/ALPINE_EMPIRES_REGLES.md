# Règles communes SkiStation / Alpine Empires — prestige, notoriété, grands événements

Ce document décrit les règles déjà en place dans la version web (`js/game.js`, `js/scenario.js`, `js/data.js`).
Il sert de référence pour le futur mode compétition **Alpine Empires**, où plusieurs compagnies se disputent les mêmes événements.

## Deux indicateurs distincts

### Prestige : le standing (0 à 100, affiché de ★ à ★★★★★)
Le prestige mesure le niveau de gamme de la station. Un grand domaine avec de bonnes remontées ne suffit pas.

| Composante | Maximum | Ce qui compte |
|---|---|---|
| Domaine skiable | 28 | km de pistes, dénivelé, les quatre couleurs, altitude du sommet |
| Remontées | 14 | technologie la plus récente, remontées d'exception (funiculaire, téléphérique, 3S, télécabines) |
| Hébergement haut de gamme | 24 | lits de luxe, prix moyen de la nuitée au-dessus de la moyenne, note des établissements de luxe |
| Loisirs, gastronomie, spa | 20 | nombre de loisirs différents, table gastronomique, spa, restaurants premium |
| Compétitions accueillies | 8 | événements sportifs réussis |
| Qualité de service | 6 | note moyenne des services et hébergements |

Plafonds : sans hébergement de luxe, le prestige ne dépasse pas 55 ; avec moins de 3 loisirs différents, 60 ; sans remontée d'exception, 72 ; sans table gastronomique ni spa, 80.

### Notoriété : la renommée (0 à 100 %)
La notoriété mesure si l'on parle de la station, en bien. Elle dépend surtout de l'humeur des visiteurs et du rapport qualité/prix. Une station modeste où les clients sont très contents et paient le juste prix peut avoir une excellente notoriété, sans luxe.

| Composante | Poids |
|---|---|
| Humeur des visiteurs (satisfaction moyenne sur 7 jours) | 50 % |
| Rapport qualité/prix (forfait comparé au juste prix, prix des services comparés à leur référence, notes) | 20 % |
| Fréquentation (record de visiteurs, échelle logarithmique) | 15 % |
| Part du prestige | 10 % |
| Événements réussis, marketing | bonus |

Elle évolue lentement (environ 5 % de l'écart par jour en hausse, 4 % en baisse) et reste freinée tant que la station a peu de clients cumulés.

## L'échelle des événements
Fête du village → Festival de concerts (notoriété 10 %) → Course régionale (18 %) → Coupe d'Europe (35 %, prestige 30) → **X Games** (critères ci-dessous) → Coupe du monde (68 %, prestige 50, une Coupe d'Europe déjà organisée) → **Jeux Olympiques** (candidature).

### Niveau X Games (10 critères)
Notoriété 60 %, prestige 45, satisfaction moyenne 72 %, snowpark et scène de concerts, 800 lits, 30 km de pistes dont 2 noires, 9 000 pers./h, 10 pisteurs sur 2 postes, 5 restaurants, une Coupe d'Europe organisée.
Organiser les X Games est **facultatif**. Atteindre leur niveau est **obligatoire** pour candidater aux Jeux.

### Cahier des charges olympique (12 critères, tous ceux des X Games avec des seuils plus élevés)
Notoriété 82 %, prestige 72, satisfaction moyenne 78 %, snowpark + scène + stade de slalom + stade olympique, 1 400 lits dont 250 de luxe, 55 km de pistes dont 4 noires avec un sommet au-dessus de 3 000 m, 16 000 pers./h avec une remontée d'exception, 18 pisteurs sur 3 postes + héliport, 8 restaurants, 6 dameuses + neige de culture, une Coupe d'Europe et une Coupe du monde organisées, dette inférieure à 1 M€.

## La candidature olympique
1. **Dépôt** (600 000 €) : niveau X Games atteint, Coupe du monde organisée, recherche « Dossier olympique », notoriété 75 %, prestige 60.
2. **Évaluation** pendant 25 jours face à 3 villes rivales (notes de 74 à 90, qui évoluent).
3. **Vote du CIO** : le meilleur dossier l'emporte. Note du dossier = 55 % de part du cahier des charges rempli, plus la notoriété, le prestige, la satisfaction et les grands événements organisés. En cas d'échec, nouvelle candidature après 60 jours.
4. **Préparation** de 100 jours, avec une inspection à mi-parcours qui liste ce qui manque.
5. **Inspection finale** : un seul critère manquant et le CIO retire les Jeux (réputation −12, notoriété −18).
6. **Jour 1 des Jeux** (cérémonie d'ouverture) : **la partie est gagnée** (`SS.Scn.VICTORY = 'olympics_day1'`).

## Reprise pour Alpine Empires (mode compétition)
- Chaque compagnie possède ses propres prestige et notoriété, calculés de la même façon.
- Les villes rivales du CIO deviennent les autres compagnies : leur note de dossier se calcule avec `SS.Scn.dossierScore()` sur leur propre station.
- Une seule compagnie peut obtenir les Jeux d'une édition. La première à atteindre le jour 1 des Jeux remporte la partie.
- Les X Games restent un événement que plusieurs compagnies peuvent organiser, sans condition de victoire.
