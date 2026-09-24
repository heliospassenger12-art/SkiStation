# SkiStation Master 0.90 — Chaîne d'arrivée complète

## Objectif
Relier la simulation des visiteurs réels à une chaîne d'arrivée cohérente :

**monde extérieur → portail/gateway → dépose-minute → parking → cheminement piéton → station.**

## Fonctionnement
- Aucun visiteur artificiel n'est créé.
- Le runtime observe uniquement les `SkierRuntime` existants.
- Après détection au gateway, le visiteur est guidé vers le point de dépose.
- Le système tente une place `DayVisitor` dans `StationParkingRuntime`.
- En cas de place disponible, le visiteur passe dans le transfert piéton.
- Le transfert utilise `SkierAccessPathRuntime` et son routage pondéré ; sinon un trajet direct de secours est utilisé.
- Si le parking est plein, l'état `ParkingFull` est enregistré puis le visiteur peut poursuivre vers le cheminement piéton au lieu'être créé ou supprimé artificiellement.
- Le runtime ne modifie pas la simulation du joueur.

## UI
- F11 : chaîne d'arrivée et état de chaque visiteur suivi.

## Suite naturelle
0.91 peut introduire une vraie logique de dépose-minute : capacité, files d'attente, véhicules/bus, temps d'arrêt et libération automatique des places.
