# SkiStation Master 1.03 — Raccords piste → remontée

## Objectif
Permettre de relier une piste principale à une remontée située juste à côté sans créer une nouvelle piste complète.

## Utilisation
- Le constructeur `SlopeLiftConnectorBuilder` raycast la piste sous la souris.
- Appuyer sur **C** crée un petit raccord vers la remontée ouverte la plus proche.
- Le raccord est généré entre le point cliqué sur la piste et le pied de la remontée.
- Le raccord possède sa propre largeur et son propre ruban de neige.
- Il n'est **pas enregistré comme une nouvelle piste** dans `SlopeNetwork`.

## Simulation
- Les vrais skieurs de gestion peuvent détecter le raccord lorsqu'ils passent à proximité de son point d'entrée.
- Ils quittent alors la piste principale et suivent le raccord jusqu'au bas de la remontée.
- Une fois arrivés, ils rejoignent la file existante via `LiftRuntime.TryQueueSkier`.
- Aucun visiteur, skieur ou véhicule artificiel n'est créé.

## Intention de design
Le raccord est une infrastructure de circulation courte : sortie de piste, chemin skiable de retour, accès à une file de remontée.
Il évite de sur-segmenter le domaine en multipliant les micro-pistes uniquement pour représenter des accès.

## Limites volontairement conservées
- Pas de sculpture automatique du terrain.
- Pas de nouvelle logique de capacité de remontée.
- Pas de création automatique de remontées.
- Pas de changement du réseau routier.
