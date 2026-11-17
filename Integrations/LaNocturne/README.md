# La Nocturne dans SkiStation Arcade

Le portage Unity cible **SkiStation Alpine Empires Avant Première**, référence extraite du neuvième volume de l’archive Drive, avec Unity **6000.3.7f1**. Ce dossier d’intégration est en dehors d’`Assets` car la branche de reprise GitHub contient une génération Arcade plus ancienne.

## Effets portés

- Deux traînées partent des poignets du rider 3D et persistent brièvement au relâchement. Elles reprennent les couleurs du contrôleur natif, jaune `#FFD60D`, vert `#26FF38` et rouge `#FF140B`. Leur largeur et leur persistance augmentent avec le palier.
- Pendant la charge d’un saut, la caméra descend et se rapproche. En vol, elle recule, ouvre le champ et regarde davantage la piste sous le rider. Le cadrage revient progressivement après le contact. Les réglages de confort et la protection contre les collisions de caméra continuent de s’appliquer.
- La consommation et les paliers de boost, les points, la réception, la sauvegarde et le catalogue restent sous l’autorité des systèmes Unity existants.

Le terrain nocturne, l’éclairage et le skieur 2D restent disponibles dans le prototype web fourni dans le ZIP Drive, dossier `PrototypeWeb`. Ouvrir `index.html` dans un navigateur, ou utiliser la [version privée de La Nocturne](https://skistation-arcade-la-nocturne.quick-bead-4287.chatgpt.site). La piste 640 m, ses décors et les animations de signature ne font pas partie du portage Unity de ce lot.

## Appliquer à l’archive Unity

Fermer Unity. Depuis ce dossier, avec Python 3.8 ou ultérieur, vérifier puis appliquer sur le dossier extrait contenant `Assets` et `ProjectSettings`.

```text
python apply.py "CHEMIN_DU_PROJET_UNITY" --check
python apply.py "CHEMIN_DU_PROJET_UNITY"
```

L’outil contrôle les empreintes des deux sources et accepte une seconde exécution. Les `.meta` existants restent en place. La validation automatisée a contrôlé les empreintes et l’application sur les fichiers extraits de Drive. Vérifier ensuite la compilation et le Play Mode dans Unity, puis essayer les trois paliers, le relâchement du boost, un saut chargé avec réception, les réglages de confort et le retour au départ.
