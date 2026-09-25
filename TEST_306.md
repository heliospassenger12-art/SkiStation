# Vérification de l'atelier montagne 3.06

À exécuter dans Unity ; ces étapes ne sont pas déclarées réussies dans l'environnement de préparation.

1. Importer le projet complet et contrôler la Console (aucune erreur rouge).
2. Menu SkiStation > Montagne > Verifier le generateur 3.06. Le contrôle exécute le véritable code C# de génération, sans modifier la scène : 12 combinaisons de graines/hauteurs, front de neige plat, déterminisme, valeurs finies, raccord du front de neige et présence des quatre bandes d'inclinaison. Relever le message PASS ou le détail de l'exception.
3. Atelier : générer les paramètres par défaut en résolution 257. Vérifier une surface continue, un front plat, des secteurs variés, une couronne rocheuse et des sapins posés sur le terrain. Zoomer sur neige et roches ; absence de matériau rose/violet et de faces retournées.
4. Répéter la même graine : même relief et même implantation des sapins. Changer la graine : les ondulations/crêtes doivent varier, le front de neige reste plat. Comparer les échelles .6 et 1.4 et les variations 0 et 1.
5. Générer la carte des inclinaisons : contrôler les zones de couleurs et la légende. Ce diagnostic n'est pas une classification officielle des pistes.
6. Passer à 513 et observer le temps de génération, la mémoire et la fluidité de la vue Scene. Revenir à 257 si nécessaire. Aucun calcul continu du relief n'est prévu une fois la scène créée.
7. Fermer/réouvrir la scène générée. Les meshes, textures et matériaux doivent persister. Deux générations doivent posséder des dossiers distincts ; aucune modification du Bootstrap.
8. Appuyer sur Play dans la scène d'étude : le lancement doit être annulé, avec un message demandant d'utiliser Scene. Ouvrir Bootstrap puis Play : ce lancement doit rester autorisé. Reprendre TEST_305.md pour le GPS.

Limites connues : génération éditeur uniquement, pas d'érosion hydraulique, pas d'import de données réelles, sapins stylisés, pas d'intégration au domaine jouable. Supprimer un dossier d'étude via la fenêtre Project permet de supprimer seulement cette génération ; fermer d'abord sa scène.
