# Sauvegardes

## SkiStation_Unity_Fusion_2026-11-17.zip

Copie complète du projet Unity au 17/11/2026, après la fusion « gestion web + arcade ChatGPT »
(commit `7c2e8f4` de la branche `claude/optimistic-noether-btruob`).

Contenu : `Assets/`, `Packages/`, `ProjectSettings/` et les outils de portage `Tools/GestionPort/`.

Pour reprendre le projet Unity : décompresser l'archive dans un dossier vide, ouvrir ce dossier avec Unity 6 (6000.3),
puis menu **SkiStation › Gestion fusionnée › Jouer (gestion + arcade)**. Voir
`Assets/SkiStation/Gestion/README_GESTION_UNITY.md` dans l'archive.

Le développement principal continue sur le **site web** (dossier `Web/`). Le noyau C# de l'archive reste à jour
tant que les règles web ne changent pas ; pour le resynchroniser plus tard, régénérer les catalogues avec
`Tools/GestionPort/gen_catalog.js` et `gen_rules.js`, puis reporter les changements de règles dans `Assets/SkiStation/Gestion/Core`.
