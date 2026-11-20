# Rendu hivernal des Trois Vallées

Le profil hivernal couvre les boisements et les pentes praticables, puis laisse davantage de roche apparente sur les fortes pentes. La neige reçoit un grain plus fin à proximité. Le givre du lac de Tuéda et des cours d’eau varie avec leur état, et les arbres s’appuient sur le terrain natif.

Ce correctif cible **Avant Première**, sous Unity 6000.3.7f1, après le lot **Route d’Auron et décor des Trois Vallées**. Avec le projet hors mode Play, lancer `./Installer.ps1 -Projet 'chemin du projet Unity'` dans PowerShell 7 avec Git disponible.

Pour reproduire la validation, placer `NeigeTuedeCheck.cs` dans `Assets/Editor` d’une copie Unity et lancer `-batchmode -force-d3d11 -executeMethod NeigeTuedeCheck.RunDecor`. Le test mesure la couverture neigeuse, le givre et les racines près de Tuéda. Il vérifie aussi le dégel et la sauvegarde d’un pont après redémarrage.
