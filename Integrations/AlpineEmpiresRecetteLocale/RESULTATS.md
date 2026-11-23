# SkiStation · Recette locale Alpine Empires

**Statut — validation locale partielle.** Aucun projet qualifié du Bloc 14 ne sert de parent à cette campagne. Les résultats portent sur la version locale Avant Première et une copie isolée réservée à la recette. Aucun client réseau réel n’a été lancé. Les [extraits des journaux et leurs empreintes](PREUVES.txt) sont conservés avec ce rapport.

## Référence et exécution

- Unity 6000.3.7f1 sous Windows, URP 17.3.0 et Input System 1.20.1. Scènes de référence Bootstrap et Gestion ValAlpin. La copie de test utilise son propre nom de produit et ses propres sauvegardes.
- Première passe Edit Mode, **152 réussis et 2 en échec sur 154**. Échecs confirmés d’un grab maintenu et de la géométrie d’un télésiège sur terrain plat.
- Après correction de la géométrie et alignement des assertions de grab, **154 réussis sur 154** en Edit Mode. La même suite repasse intégralement après le correctif de vitesse et le réglage URP final.
- Le scénario Classique exerce l’achat d’une concession, les droits d’emprise, la construction et les comptes du restaurant B, la météo à graine **842916**, la sauvegarde, la réouverture et l’aller-retour local Gestion–Arcade. Le test de service utilise un visiteur contrôlé.
- Le test d’autorité foncière dans Unity exerce une concurrence entre deux commandes et l’idempotence après restitution du journal. Il tourne dans un seul processus.
- Hors Unity, le banc de comptabilité a confirmé les centimes, les comptes séparés et le rejeu après rechargement. Un essai du modèle avec **10 000 mouvements** confirme le refus du mouvement suivant et l’absence de doublon lors du rejeu. Aucune simulation longue en jeu n’a servi à ce test.
- Le scénario Express lance le vrai profil, vérifie le refus d’entrée en Arcade et suspend la Gestion à l’échéance. L’échéance de **7 200 secondes** est forcée dans le banc. Aucune partie de 120 minutes ni attribution d’une victoire n’est observée.
- ValAlpin, Orcières et Auron passent la séquence file → embarquement → trajet → débarquement → sortie amont. Quatre visiteurs d’essai identifiables sont admis via la file native dans chaque scène. Le banc ne reproduit pas un séjour inter-stations.
- La fermeture a été rejouée sur ValAlpin par le registre natif des fermetures. Le calcul d’itinéraire exclut la ligne dès la fermeture, les quatre voyageurs engagés débarquent, la vitesse retombe à zéro et le calcul réintègre la ligne après réouverture. Les premiers essais de bascule directe échouaient au contrôle de l’arrêt final.
- La première tentative de build Windows s’arrête au contrôle de compatibilité URP 17.3.0. Le symbole Standalone `URP_COMPATIBILITY_MODE` déclare le mode déjà choisi par le projet. La copie a ensuite repris les réglages source de lumière linéaire et d’anticrénelage. Une seconde tentative révèle un compilateur Burst manquant dans le cache de test, restauré depuis le package local de même empreinte. Le build final des **cinq scènes** aboutit avec **zéro erreur et 141 avertissements**. L’exécutable de recette a démarré et son journal montre le moteur et la première journée. Il a été arrêté après le contrôle de démarrage. Ce contrôle ne mesure ni la fluidité ni une partie complète.

## Corrections

| Fichier dans la référence locale | Changement |
|---|---|
| `Assets/SkiStation/Scripts/Lifts/LiftLineGeometry.cs` | Les pylônes générés sont ajustés selon les mêmes points d’échantillonnage et la même enveloppe que le contrôle final de garde au sol. Les seuils, pylônes imposés et gares sont conservés. |
| `Assets/SkiStation/Scripts/Lifts/LiftVehicleRuntime.cs` | Après débarquement, recalcul de la vitesse d’une ligne fermée selon ses passagers restants. |
| `Assets/Editor/HybridAirChecks26315.cs` et `Assets/Editor/SkiStationArcadeFreestyleChecks254.cs` | Les assertions couvrent la progression du score à la tenue et le vrai plafonnement du modèle. Aucune modification de la formule de score. |
| `ProjectSettings/ProjectSettings.asset` | Activation explicite de la compatibilité URP pour Standalone, sans changement de version du package. |

## Matrice du Bloc 15

| ID | État | Preuve ou prérequis restant |
|---|---|---|
| T01 | Partiel | Quatre comptes et stations au niveau du modèle. A et B observés sur un parcours réel. Trois entreprises IA opérationnelles à vérifier. |
| T02 | Bloqué | Transport de jeu et état partagé entre quatre clients à intégrer et vérifier. |
| T03 | Bloqué | Validations locales du modèle d’équipes. Match réseau 2 VS 2 à établir. |
| T04 | Partiel | Profil Express, refus Arcade et échéance forcée réussis. Partie complète et résolution de victoire à vérifier. |
| T05 | Bloqué | Horloge, synchronisation et classement en ligne non exécutables sur cette base. |
| T06 | Bloqué | Configuration des indépendants ON/OFF et contrôle des conditions de victoire à intégrer. |
| T07 | Bloqué | Choix du nombre de neutres et calcul conditionné par la carte à intégrer. |
| T08 | Réussi en local | Arbitrage et déduplication exercés dans un seul processus Unity. |
| T09 | Réussi en local | Le constructeur natif refuse à A un restaurant sur un terrain allié découvert qui passe le contrôle générique du site. Les quatre soldes et le nombre de bâtiments restent inchangés. |
| T10 | Bloqué | Projet collectif et consentement des quatre contributeurs à intégrer. |
| T11 | Bloqué | Voyage régional d’une même personne et agrégat régional à intégrer. |
| T12 | Partiel | File et transport natifs vérifiés sur trois cartes. Fermeture, vidage, arrêt et retour dans le GPS local réussis sur ValAlpin. Parcours spontané complet et reprise en trajet à éprouver. |
| T13 | Bloqué | Indépendants, contrats et gouvernance régionale à intégrer. |
| T14 | Partiel | Hiver, été et reprise du bulletin vérifiés. Fermeture déclenchée automatiquement par redoux ou tempête à éprouver. |
| T15 | Bloqué | Recherche collective et candidature avec critères réels à intégrer. |
| T16 | Bloqué | Logistique événementielle multisite et règlement par prestataire à intégrer. |
| T17 | Partiel | Reprise de l’identité du restaurant, des comptes et du climat. Contrats et visiteurs régionaux non couverts. |
| T18 | Bloqué | Rejeu local testé. Déconnexion/reconnexion sur autorité réseau à intégrer. |
| T19 | Bloqué | Arcade locale vérifiée. Glisse multirider et compétition synchronisée à intégrer. |
| T20 | À éprouver | Pas de profilage Beta sur grande carte à quatre gestionnaires et neutres. |

La matrice distingue les essais de composants des critères d’acceptation complets. Les campagnes des Blocs 10 à 15 restent conditionnées par leurs fonctionnalités manquantes. Cette recette n’établit aucun statut de Beta Candidate.
