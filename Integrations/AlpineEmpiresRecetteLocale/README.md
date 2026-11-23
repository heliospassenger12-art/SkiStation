# Correctifs de recette locale Alpine Empires

Ce correctif cible la copie Unity **Avant Première** utilisée pour la recette du 23 novembre 2026, avec Unity 6000.3.7f1 et URP 17.3.0. Il contient les corrections de garde au sol des télésièges et d’arrêt après vidage d’une ligne fermée, deux ajustements des tests de grabs et le réglage Standalone de compatibilité URP.

La version Unity historique à la racine du dépôt remonte à une autre base. Utiliser le correctif uniquement sur une copie compatible et garder les sauvegardes originales. La copie de recette locale porte déjà les correctifs.

## Contrôle et application

Depuis PowerShell, utiliser les chemins de la copie Unity et du correctif téléchargé. Fermer Unity sur la copie à modifier.

```powershell
$projetUnity = 'D:/chemin/vers/Avant Premiere/Projet_Unity'
$correctif = 'D:/chemin/vers/recette-unity.patch'
git -c core.autocrlf=false -C "$projetUnity" apply --reverse --check -- "$correctif"
```

Si la vérification inverse réussit, les changements sont déjà présents. Sinon, vérifier les fichiers source avec les empreintes de référence du manifeste, puis vérifier l’applicabilité. En cas d’empreinte différente, arrêter et examiner la divergence.

```powershell
git -c core.autocrlf=false -C "$projetUnity" apply --check -- "$correctif"
if ($LASTEXITCODE -eq 0) {
    git -c core.autocrlf=false -C "$projetUnity" apply -- "$correctif"
}
```

Le manifeste contient les SHA-256 des cinq fichiers en UTF-8 avec fins de ligne LF, avant et après application. Les empreintes « avant » correspondent à la référence reconstruite à partir des changements de recette. L’application et son inverse ont été contrôlés sur une copie des fichiers, puis les fichiers résultants comparés à ceux de la copie Unity testée. Pour revenir en arrière après avoir vérifié les empreintes « après », reprendre la commande de contrôle inverse en retirant `--check`.

## Résultats

- Unity Edit Mode sur la copie Avant Première avec **154 tests réussis sur 154**.
- Build Windows de cinq scènes compilé et démarré sur la machine de recette, avec **0 erreur et 141 avertissements**.
- Deux scénarios d’acceptation du Bloc 15 réussis en local, cinq partiels, douze bloqués et un non exécuté. Aucun essai multiclient.

Le projet Unity complet, les assets commerciaux, les sauvegardes et l’exécutable restent locaux. [Rapport de recette](RESULTATS.md) · [Extraits des journaux](PREUVES.txt) · [Manifeste](MANIFEST.json)
