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

## Alignement avec le Bloc 2 de la version Unity ChatGPT (Drive, 16/11/2026)
Référence copiée dans `Docs/AlpineEmpires_Bloc2/` (kit « Comptes alliés », statut PARTIEL, tests Unity en attente).
- **4 compagnies** (`company-1` à `company-4`), chacune avec sa station, son gestionnaire et son grand livre ; de 1 à 4 humains, les autres places sont des alliés ou adversaires.
- **Modes d'équipe** : Individuel (4 équipes), Deux équipes (1-2 contre 3-4), Empire partagé (une seule équipe).
- **Concessions** : une concession de départ gratuite par compagnie, deux extensions achetables (50 000 € et 100 000 €), une zone protégée non constructible (haute altitude). Altitudes autorisées 1 300 → 4 050 m.
- **Comptes** : chaque dépense et recette (construction, ventes d'un service, charges) est attribuée à la compagnie propriétaire, au centime, avec un journal rejouable (opérations idempotentes) pour la sauvegarde.
- Dans notre modèle : chaque compagnie aura son propre `GameState` (argent, prestige, notoriété, prêts, recherche) sur un massif commun ; le réseau de pistes et les visiteurs restent partagés, les recettes d'un service vont à son propriétaire. La victoire reste le **jour 1 des Jeux Olympiques** : une seule compagnie par édition.

## Prototype jouable (site web, version 1.8)
- **Grande carte à quatre montagnes** : 22 × 22 km (7,5 fois la carte classique). Chaque compagnie a sa propre montagne, de caractère différent, et toutes dépassent 3 300 m :
  - vous (sud-ouest) : *massif aux mille versants*, environ 4 450 m (la graine choisie au menu varie sa forme) ;
  - Valfroide (sud-est) : *grand domaine aux larges alpages*, reliefs doux, nombreux replats, environ 3 750 m ;
  - Pic-Royal (nord-est) : *haute montagne et glaciers*, reliefs raides, environ 4 650 m ;
  - Les Rochettes (nord-ouest) : *moyenne montagne boisée*, nombreuses petites montagnes arrondies, environ 3 550 m.
- **Relief en étages, plusieurs versants** : une montagne n'est plus une seule pente. Elle assemble un socle, un sommet principal, 2 à 4 sous-sommets allongés (des arêtes), des montagnes à sommet plat devant le village (on monte jusque vers 2 400-2 750 m, plateau, puis ça redescend vers un col), des montagnes latérales et arrière. Entre elles : cols et combes, chacun avec ses versants d'orientations différentes.
- **Un seul relief continu et naturel** : tous les reliefs de toutes les montagnes sont fondus ensemble (aucun cadre ni bord découpé, aucun bloc posé), sur un terrain fortement déformé ; arêtes et ravines à toutes les échelles ; puis une érosion par ruissellement (des centaines de milliers de gouttes) creuse talwegs et vallons et dépose au pied des pentes. Rugosité et recalage des sommets sont fondus entre montagnes voisines : aucune couture. Chaque station garde son caractère (hauteurs, raideur, nombre de reliefs).
- **Replats** : 9 à 18 par montagne, de 2 000 à 3 300 m, de 220 à 620 m de rayon, inclinés d'environ 6 % pour garder de la vitesse (mode arcade), entourés de pentes plus raides. Ils sont listés dans `SS.Terrain.plateaus` (position, rayon, altitude, montagne) pour y placer plus tard gares, restaurants d'altitude, bars, chiens de traîneau…
- **Répartition des pentes au-dessus de 1 900 m** : environ 19 % de vertes, 11 % de bleues, 15 % de rouges, 36 % de noires, 16 % de rocher (carte classique : 11 / 9 / 24 / 46 / 8). Une rampe douce (≈ 6 %) relie chaque village à sa montagne : les pistes redescendent jusqu'au front de neige.
- **Entre les stations, de quoi bâtir de grandes stations** : un *massif central* (environ 3 900 m, quatre arêtes vers les quatre stations, une douzaine de replats) et quatre *montagnes de liaison* (environ 3 150-3 300 m) à cheval sur les frontières entre voisins. Les quatre cols qui les séparent ne sont jamais plats et ne se ressemblent pas : col des bosses, col de la petite crête, col des mamelons, col de la combe (disposition tirée au sort). Tout le centre de la carte ondule légèrement.
- **Massifs reliés** : depuis votre village, on atteint les trois villages rivaux sans dépasser une pente de 0,45 ; pas de falaise en limite de territoire.
- **Concessions** : votre massif ; le **massif central** à acheter (300 000 €, le premier acheteur l'emporte ; Valfroide le convoite après le jour 180), son aiguille sommitale reste protégée ; deux extensions à acheter, la liaison Sud vers Valfroide (50 000 €) et la liaison Ouest vers Les Rochettes (100 000 €) ; les trois autres massifs appartiennent aux rivaux. Tout devis du joueur hors de ses terrains est refusé avec le motif. Les anciennes sauvegardes gardent la carte d'un seul massif (secteurs angulaires autour du sommet).
- **Rythme** : sur la grande carte, un rival ne dépose sa candidature olympique qu'avec un dossier complet à 92 % ; en Normal, un rival ouvre les Jeux vers les jours 485 à 515 (le plus souvent Valfroide, dont le grand domaine s'équipe vite).
- **Rivaux** : Valfroide (bâtisseur, prix au juste prix), Pic-Royal (luxe, +30 %), Les Rochettes (populaire, −18 %). Chaque jour : fréquentation, recettes (forfaits, dépenses sur place, nuitées limitées par la clientèle réelle), charges, satisfaction, réputation, notoriété et prestige calculés comme pour le joueur ; une construction par jour au plus (remontée + piste réelles, ou un bâtiment) ; montée en palier technologique ; événements quand leurs prérequis sont remplis ; candidature olympique aux mêmes conditions que le joueur.
- **Marché partagé** : chaque compagnie a un score d'attractivité (domaine, bâtiments, notoriété, prestige, prix) ; sa fréquentation est multipliée par 0,55 à 1,35 selon sa part face à la moyenne des autres.
- **Une seule édition des Jeux** : dès qu'une compagnie obtient les Jeux (vote gagné), les autres candidatures sont closes. Les villes rivales du vote du joueur incluent les compagnies.
- **Équipes** : en mode « deux équipes », Valfroide est votre allié ; son ouverture des Jeux vaut victoire.
