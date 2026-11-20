# Auron dans Avant Première

Depuis **Jouer → Gestion → Reliefs de France**, choisir **Auron**. Le décor suit les emprises relevées sur la carte jouable de 8,04 × 8,4 km.

Les emplacements des arbres sont générés à l’intérieur de 227 boisements de la BD Forêt V2. Le lot utilise 25 808 emplacements avant les dégagements de construction, 36 zones rocheuses et 23 lignes de falaise. Les pistes OpenStreetMap servent à dégager visuellement les arbres. Les nouvelles constructions du joueur dégagent aussi le décor, y compris aux accès des ponts.

Le profil Pure Nature 2 est sélectionné par défaut à Auron quand il est disponible localement. Les arbres, blocs et parois réutilisent les modèles importés dans Unity. Les hauteurs et les essences d’arbres sont des représentations visuelles. Pour les mélèzes, le jeu utilise des conifères fins en attendant un modèle dédié.

## Installation

Ce lot cible **SkiStation Alpine Empires Avant Première** sous Unity 6000.3.7f1, avec l’hydrographie d’Auron installée. Fermer Unity, puis lancer `./Installer.ps1 -Projet 'chemin du projet Unity'` dans PowerShell 7 avec Git disponible. Le runtime utilise les ressources Pure Nature 2 installées dans ce projet et propose sa géométrie de secours si le pack est absent.

Pour vérifier une copie, placer `AuronNatureCheck.cs` dans `Assets/Editor` et lancer Unity avec `-batchmode -force-d3d11 -executeMethod AuronNatureCheck.Run`. Le test avec Pure Nature 2 vérifie le décor, le gel et le dégel, un pont, son dégagement visuel et la reprise via **Continuer**.

`AuronNatureCheck.RunLegacy` vérifie la lecture de la sauvegarde produite par le test du lot hydrographique, avec ses deux franchissements d’origine.

## Sources géographiques

- Les boisements et les points rocheux nommés proviennent de la BD Forêt V2 et de la BD TOPO © IGN, sous Licence Ouverte 2.0, extraites le 20 novembre 2026. [BD Forêt](https://www.data.gouv.fr/datasets/bd-foret-r) · [BD TOPO](https://www.data.gouv.fr/datasets/bd-topo-r)
- Les lignes de falaise, les affleurements, les éboulis et les tracés de pistes proviennent des contributeurs OpenStreetMap, extraits le 20 novembre 2026. Les couches JSON dérivées exploitant ces données sont disponibles sous ODbL 1.0. [Crédits OpenStreetMap](https://www.openstreetmap.org/copyright) · [Licence ODbL](https://opendatacommons.org/licenses/odbl/1-0/)
- Les dimensions des parois et leur orientation sont adaptées à la géométrie du relief jouable. Les fichiers d’eau du lot précédent fournissent les zones à dégager.
