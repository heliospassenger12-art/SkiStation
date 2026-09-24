# SkiStation 2.50 — Piétons, voitures, bus et préparation au ski

Projet Unity COMPLET fondé sur PHASE63_FIX2. Ce fichier est la consigne actuelle ; les documents phase63 précédents sont historiques.

## Ce que tu verras
- Des visiteurs arrivant du bord de carte sur le trottoir droit, skis portés s'ils ont leur matériel.
- Des voitures avec carrosserie colorée, vitres, toit, phares et roues ; elles roulent sur la chaussée puis se garent.
- Des autocars bleus plus longs, avec porte latérale, qui stationnent dans leurs places dédiées.
- Un parking fonctionnel de 12 places voitures et 3 places bus, marquages et liaisons piétonnes. Il remplace le parking décoratif précédent.
- Les passagers apparaissent à la porte uniquement après stationnement, espacés de 1,2 seconde, puis marchent jusqu'au front de neige. Ils sont comptés dans la demande dès le départ du véhicule : aucun groupe gratuit supplémentaire.
- Un petit guichet FORFAITS et un bouton de caméra ARRIVEES.

## Parcours des visiteurs
| Possessions à l'arrivée | Parcours après route ou parking |
|---|---|
| Matériel, aucun forfait | Guichet forfaits, puis recherche d'une remontée |
| Ni matériel ni forfait | Magasin accessible, retour au front de neige, guichet, puis remontée |
| Forfait prépayé, sans matériel | Magasin accessible, puis remontée |
| Matériel et forfait prépayé | Recherche d'une remontée après arrivée à pied terminée |

Les possessions sont tirées une fois : 65 % ont leur matériel et 20 % un forfait déjà payé. Ce sont des réglages de prototype, pas des statistiques réelles.
Le forfait acheté au guichet est enregistré dans les recettes une seule fois pour la journée ; il n'est pas refacturé à l'embarquement. Le forfait déjà détenu ne produit pas de revenu supplémentaire dans cette simulation. La location reprend le magasin existant : son prix reste indicatif, sans nouvelle facturation ajoutée ici.
Sans magasin construit, ouvert et relié au réseau piéton à proximité, les visiteurs sans matériel attendent. Le guichet est fourni automatiquement avec l'accueil ; sa file est logique et un seul client est servi à la fois (3 secondes), sans emplacements individuels pour la foule.

## Installation et observation
1. Extraire dans un NOUVEAU dossier, ajouter le projet dans Unity Hub puis ouvrir Assets/Scenes/SkiStation_Bootstrap.unity.
2. Attendre la compilation puis Play. Cliquer ARRIVEES pour cadrer parking, trottoirs et guichet.
3. Observer pendant les horaires 7 h–18 h. Les arrivées suivent la demande existante : environ 8 personnes par heure de jeu avant réputation. Les groupes alternent piéton / voiture de 2 / piéton / bus de 8 / voiture de 2. Il faut cumuler la demande d'un groupe avant son départ ; un bus ne surgit donc pas dès la première seconde.
4. Pour voir arriver tous les types plus vite, utiliser temporairement 3x puis revenir à 1x au parking. Tous les déplacements utilisent le temps du jeu et s'arrêtent en pause.
5. Construire un magasin accessible si tu veux observer les quatre parcours. La caméra Visiteur permet de suivre une personne après sa sortie de véhicule.
6. Contrôler que le compteur « En véhicule » diminue à chaque passager descendu, puis observer location, guichet et départ vers la gare.

## Limites de cette première modélisation
- Véhicules et personnages procéduraux simples ; pas d'animation détaillée de marche, d'ouverture de porte ou de passager assis dans l'habitacle.
- Circulation volontairement simplifiée : un seul véhicule en mouvement à la fois sur cet accès, les autres restent garés. Il n'y a pas encore de trafic dense, d'évitement des piétons ou de moteur de collision routière. Les rotations aux virages sont lissées mais pas une simulation des roues directrices.
- Si une catégorie de parking est pleine, la demande suivante bascule vers des piétons ; aucun véhicule ne se gare sur une place réservée.
- Les voitures et bus restent présents pendant la visite de leur groupe. Quand tous ses visiteurs sont supprimés par le système de départ existant, le véhicule repart vide. Le retour des personnes au véhicule et l'embarquement de retour ne sont pas encore modélisés.
- Le tracé piéton du parking est explicite, mais l'accès aux magasins conserve le réseau actuel ; pas de navigation générale autour des bâtiments.
- Le parking et le guichet sont des infrastructures fixes du prototype, sans prix de construction ni placement interactif. La vérification routière de construction réserve leur emprise ; les autres outils de construction ne sont pas tous adaptés à cette réserve.
- Pas de sauvegarde persistante des places ou des groupes. La distribution d'arrivées reste répétitive pour faciliter ton test.
- L'entrée logique est alignée sur le terminus de la route existante (le village visuel). L'ancien point de repli à z=-1670 reste uniquement avant création de la route. Aucune gare construite n'est déplacée.
- La boucle spéciale de la phase63 et les limites du FIX2 restent présentes. Cette livraison n'affirme pas avoir résolu les derniers problèmes de remontée observés : elle ajoute les types d'arrivée demandés.

## Vérifications réellement effectuées
402 fichiers C# : analyse syntaxique sans erreur. Comparaison avec FIX2 : arcade, scènes, placement des gares, véhicules des remontées, navigation de ski et méthodes de boucle phase63 conservés. Vérifications géométriques indépendantes des 15 places et du positionnement sur route/trottoir.
Unity n'est pas disponible ici : compilation C#, rendu, achats et parcours complets en Play restent à tester sur ton ordinateur.
