# SkiStation Master 1.59 — PLAYABLE FIRST

## But
La 1.59 ne cherche pas à empiler des fonctionnalités. Elle prépare le premier test réel dans Unity.

## Démarrage
1. Décompresser l'archive.
2. Dans Unity Hub, ajouter le dossier contenant `Assets`, `Packages` et `ProjectSettings`.
3. Utiliser Unity `6000.3.3f1`.
4. Ouvrir `Assets/Scenes/SkiStation_Bootstrap.unity`.
5. Appuyer sur **Play**.
6. Observer le bandeau `SkiStation 1.59 • READY — domaine jouable`.

## Première boucle à tester
- Déplacer la caméra.
- Laisser le temps avancer.
- Observer les visiteurs arriver.
- Observer l'utilisation des remontées et pistes.
- Vérifier l'économie et les statistiques.
- Tester la pause et les vitesses 3x/8x.
- Tester B pour une piste et L pour une remontée.
- Ouvrir les interfaces F1/F4/F5/F6/F7/F8/F10 selon les systèmes présents.

## Ce que la 1.59 ajoute
- `PlayableStartupGuard` : diagnostic visuel minimal et non bloquant.
- Nettoyage d'un double `Ensure` dans `PlayableCoreBootstrap`.
- Documentation de démarrage.

## Règle de suite
Si Unity compile et que la boucle est jouable, les prochaines versions doivent être guidées par le test réel et non par l'ajout automatique de fonctionnalités.
