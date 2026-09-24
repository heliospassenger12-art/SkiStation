Phase 61 — correction ciblée de la zone morte centrale
Cause identifiée dans LiftConstructionPlanner.Update : le rectangle 500x310 du sélecteur de modèle était exclu des clics même après fermeture du sélecteur. Les clics au centre du massif étaient donc ignorés pour les deux gares. Suppression de cette exclusion permanente ; le sélecteur bloque déjà les clics via `if (choosingModel) return;`.
Conservés : sélecteur de modèle, autres protections HUD, limite de longueur et trait violet plafonné.
Vérification statique du code et de l'archive uniquement ; test de compilation et d'exécution dans Unity non réalisé.
