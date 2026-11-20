# Caméra de Gestion

Le kit cible SkiStation Alpine Empires Avant Première sous Unity 6000.3.7f1, avec l’hydrographie saisonnière installée. Fermer Unity, vérifier que Git est disponible, puis lancer `./Installer.ps1 -Projet 'chemin vers le projet Unity'` dans PowerShell 7. Le script vérifie toutes les versions locales avant de copier les scripts.

| Commande | Action |
| --- | --- |
| Flèches, ZQSD ou WASD | Déplacement. Maj accélère. |
| Clic molette maintenu | Glisser le relief. |
| Molette | Zoomer et dézoomer autour du point sous le curseur. |
| Maj + clic molette maintenu | Tourner et incliner, y compris en construction. |
| Clic droit maintenu | Tourner et incliner pendant la navigation. |
| Début / Fin | Vue de toute la carte / retour au front de neige. |

Les sensibilités de déplacement, de rotation et de zoom restent réglables dans **Options → Caméras**.

`CameraGestionCheck.cs` s’exécute dans une copie Unity. Placer le fichier dans `Assets/Editor`, puis lancer l’éditeur avec `-batchmode -force-d3d11 -executeMethod CameraGestionCheck.Run`. Le test charge Les Trois Vallées et Orcières depuis l’accueil, vérifie les commandes et le cadrage, et vise les deux berges d’un pont dans Les Trois Vallées.

Références de commandes de caméra stratégique. [Parallel Cascades](https://github.com/Parallel-Cascades/camera-controllers) · [Unity RTS Camera](https://github.com/PanMig/Unity-RTS-Camera)
