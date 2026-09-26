# SkiStation 3.27 — Horloge & remontées dynamiques

Base directe : 3.26.

## Brique temps
- Horloge centrale conservée comme source unique.
- Nouvelle échelle x1 : 06:00 -> 24:00 en 4 heures réelles.
- Migration automatique des anciennes scènes qui avaient encore une journée de 180 secondes.
- Pause / x1 / x3 / x8 restent pilotés par GameSpeedController et Time.timeScale.
- Une accélération accélère donc à la fois le monde physique et l’horloge : pas de désynchronisation.
- Helpers GameClock pour convertir un trajet réel en minutes de jeu.

## Remontées de départ
- Téléski classique : 12,6 km/h = 3,5 m/s.
- Télésiège fixe 2 places : 14,4 km/h = 4,0 m/s.
- Sur 1500 m, le télésiège fixe met ~6 min 15 s à x1, soit ~28 min de journée SkiStation.
- À x3 : ~2 min 05 s réelles.
- À x8 : ~47 s réelles.
- Les modèles/recherches supérieurs conservent leur intérêt de vitesse/débit.

## Cohérence
LiftVehicleRuntime utilise déjà Time.deltaTime et la vitesse effective du LiftRuntime : les commandes x1/x3/x8 accélèrent réellement les véhicules et leurs passagers. Aucun téléport de montée n’est ajouté.

## Test Unity conseillé
1. Construire un télésiège fixe 2 places proche de 1500 m.
2. Vérifier ~14,4 km/h dans la fiche.
3. Chronométrer la montée à x1, x3 puis x8.
4. Vérifier que l’heure, les visiteurs et le véhicule restent synchronisés.
5. Tester Pause pendant une montée puis reprise.

⚠️ Compilation Unity / Play Mode non exécutés dans cette livraison.
