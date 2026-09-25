# SkiStation 3.06 — Atelier montagne

## Base
Projet complet cumulatif à partir de SkiStation 3.05, elle-même issue de la base officielle 3.04. Les 1 000 fichiers de la 3.05 sont conservés sans modification. Le GPS reste à tester selon TEST_305.md.

## Ouvrir le prototype
1. Décompresser ce ZIP dans un nouveau dossier et ajouter ce dossier à Unity Hub. Version du projet : Unity 6000.3.7f1.
2. Après import et compilation, ouvrir le menu **SkiStation > Montagne > Atelier montagne 3.06**.
3. Garder les réglages par défaut et cliquer **Créer une nouvelle scène d’étude**. Si Unity propose de sauvegarder la scène précédente, choisir normalement selon les modifications à conserver.
4. Observer l'onglet **Scene**, pas Play : Alt + clic gauche pour tourner, molette pour zoomer. Le bouton **Recadrer la vue montagne** remet la vue générale.
5. Pour comparer, activer **Carte des inclinaisons** et générer une deuxième scène avec la même graine. Chaque génération crée un nouveau dossier, sans écraser la précédente.

Le dossier créé est Assets/SkiStation/Generated/MountainLab306/Massif_<graine> (suffixe ajouté si nécessaire). Il contient la scène .unity, les maillages, matériaux, textures et Parametres.json. Les scènes générées sont des résultats locaux ; aucune scène pré-générée n'est prétendue validée dans cette archive.

## Contenu
- Massif de 8 km de large sur 5,5 km de long.
- Front de neige de 8 × 2 km, plat à 82 m dans les coordonnées du projet.
- Quatre grands secteurs avec dénivelé progressif, ondulations FastNoiseLite et couronne rocheuse au sommet.
- Graine déterministe ; réglages de hauteur générale, variations naturelles, résolution 257 ou 513, sapins et carte des inclinaisons.
- Neige nuancée, roches brunes à texture procédurale simple, sapins stylisés réunis dans un maillage, socle de fermeture et éclairage d'étude.
- Carte indicative : vert < 10°, bleu 10–18°, rouge 18–28°, noir ≥ 28°. La difficulté réelle d'une piste dépend aussi de sa largeur, de son tracé et des conditions.

## Isolation
Cet atelier fonctionne uniquement dans l'éditeur, avant Play. La gestion existante utilise de nombreux installateurs globaux au lancement : Play est donc bloqué lorsqu'une scène de cet atelier est chargée, avec un message Console explicatif. Fermer la scène d'étude et ouvrir Assets/Scenes/SkiStation_Bootstrap.unity pour jouer et tester le GPS.

Le générateur de gestion, les scènes existantes, les routes, bâtiments, gares, pistes, visiteurs, GPS et scripts Arcade ne sont pas modifiés. Les objets de l'atelier sont marqués EditorOnly. Le prototype ne doit pas être ajouté aux scènes du jeu compilé. Il sert à choisir un relief avant une future intégration contrôlée.

## Validation
Effectué ici : analyse syntaxique des 451 fichiers C#, identité des 1 000 fichiers de base, absence de GUID dupliqué, vérification des sources FastNoiseLite par empreinte, intégrité du ZIP.

Non effectué : compilation Unity, exécution des tests numériques dans Unity, création effective de la scène par l'éditeur, inspection du rendu et performances. Lire TEST_306.md. Les matériaux restent une première passe procédurale, pas un rendu final validé.

## Suite
Tester/choisir les paramètres de la montagne. Ensuite seulement envisager la migration du relief jouable, avec raccordement route/front de neige et vérification des constructions et du GPS. GOAP n'a pas été ajouté.
