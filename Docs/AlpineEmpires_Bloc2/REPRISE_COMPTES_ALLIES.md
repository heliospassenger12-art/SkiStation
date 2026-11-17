# Bloc 2 · Comptes et construction d’une entreprise alliée

**PARTIEL · UNITY_TESTS_PENDING · NO-GO Bloc 3.**

Parent complet vérifié avant modification. `SkiStation_ALPINE_EMPIRES_BLOC_02_SAUVEGARDE_MATERIELLE_PARTIEL.zip`, SHA-256 `5f3741b4712ee46f9575b8b57e749ca945cc80a65a74e52c17cd81fd1ad7a591`. Le socle livré est Gestion 3.94 et Arcade A2.64, Unity 6000.3.7f1 et URP 17.3.0.

## Réalisation

L’essai du Bloc 2 permet à l’entreprise B de financer un restaurant de gamme standard sur une emprise plane de sa concession, si la route permanente permet le raccordement piéton. Il utilise le constructeur et le service natifs, ainsi que leurs règles de terrain, d’altitude, de placement et d’accès. Pour garantir l’absence de mutations foncières avant la validation du paiement dans cette tranche, l’essai exige un emplacement déjà plan et la politique de construction instantanée. L’achat est refusé si l’entreprise, l’emprise ou le budget ne concordent pas. Une tentative réitérée pour la même opération ne reconstruit et ne redébite pas. En cas d’échec de matérialisation, le bâtiment préparé est retiré avant toute écriture.

Les consommations réellement terminées au restaurant et ses charges de service sont attribuées à B. Les deux voies natives de perception distinguent le fournisseur. Le compte de la station humaine exclut ce restaurant de ses charges quotidiennes. Les recettes et dépenses sont conservées au centime dans l’état de la société alliée. Le journal applique des bornes dont le dimensionnement de longue carrière reste à traiter. Le panneau d’état et les fonctions de suppression et configuration consultées protègent le restaurant des actions manuelles de l’entreprise du joueur.

La sauvegarde matérielle encode les transactions alliées dans le schéma 3 de session et conserve la lecture du schéma 2. La reprise rejoue le journal sans repasser par le constructeur et restaure le bâtiment par le checkpoint natif. Le reçu de construction doit être unique et correspondre à l’entreprise, au secteur, au service et à son emplacement. Les garde-fous précédents du checkpoint restent applicables, dont l’arrêt des visiteurs et chantiers actifs et le refus des routes secondaires.

La recette Unity de bout en bout reste différée. Trois limites techniques empêchent encore de déclarer complet le Bloc 2. Le routage ne couvre qu’un premier service et non tous les types de construction et de finance. Le monde ne simule pas encore quatre domaines actifs et ne dispose pas d’une persistance de carrière entière. Les commandes foncières et les droits ne couvrent pas encore toutes les modifications et suppressions natives. L’essai B est une commande explicite de validation, sans comportement autonome d’IA.

## Matrice des 15 missions

| ID | État et preuve |
|---|---|
| AE-02.01 | SHA-256, CRC et manifeste du dernier parent contrôlés. |
| AE-02.02 | Activation limitée au slot Alpine Empires. Commande de démonstration visible dans l’éditeur. |
| AE-02.03 | Registre existant et compte de service allié codé. |
| AE-02.04 | Restaurant B matérialisé par le constructeur en code. Autres actifs et stations actives à relier. |
| AE-02.05 | Compositions structurelles héritées. Aucun comportement d’IA stratégique revendiqué. |
| AE-02.06 | Coût, ventes et charges d’un restaurant B routés en code. Emprunts et autres flux à couvrir. |
| AE-02.07 | Garde d’entreprise et d’emprise du restaurant. Contrôles généraux pour les autres actifs en attente. |
| AE-02.08 | Rejeu du chantier et reçus d’exploitation idempotents en code. Cas C# fournis, exécution en attente. |
| AE-02.09 | Parcelle B rapprochée de la route sur les nouvelles sessions compatibles. Anciennes emprises chargées à l’identique. |
| AE-02.10 | Validation native de l’emprise et du chemin piéton réutilisée. Capacité régionale de quatre domaines à établir. |
| AE-02.11 | Découverte et propriétés héritées. |
| AE-02.12 | Achats d’extension hérités. Le restaurant s’installe sur le terrain initial de B. |
| AE-02.13 | Agrégation complète des actifs et de la fréquentation à faire. |
| AE-02.14 | Journal allié et bâtiment liés par reçu au checkpoint matériel. Sauvegarde complète et test Unity à faire. |
| AE-02.15 | Projet Unity entier, SHA-256 et rapport. Verdict de sortie NO-GO. |

## Vérifications

Exécutés hors Unity. Filiation SHA-256 et CRC du parent, lecture statique des appels natifs et des branches de paiement, analyse lexicale et délimiteurs des fichiers C# modifiés, validation des liens du banc de test et comparaison d’inventaire de l’archive entière. Compilation C#, Play Mode et réseau non exécutés. Les anomalies historiques de PNJ immobiles et de disparitions en gare amont restent à reproduire.

Le banc `Tools/AlpineEmpiresFinanceChecks/Checks.csproj` compile les vraies classes métier et teste le refus d’identité, l’emprise, le rollback avant débit, le rejeu, l’isolement et la persistance. Il est fourni pour exécution sous .NET 8 avec `dotnet run --project Tools/AlpineEmpiresFinanceChecks/Checks.csproj`. .NET et Unity étaient absents de l’environnement de livraison.

Recette à effectuer sous Unity 6000.3.7f1. Ouvrir `SkiStation_Bootstrap`, créer une nouvelle partie Alpine Empires et ouvrir la station 3D. Utiliser la construction instantanée. Revenir à l’accueil et ouvrir le panneau Alpine Empires, puis lancer **Essai Bloc 2 · chantier de l’entreprise alliée B**. Vérifier la création d’un restaurant, le débit sur B seul, la recette sur B après une prestation réelle et les charges de service après changement de jour. Réitérer l’essai et contrôler l’absence de nouveau débit. À un point calme, sauver, fermer, reprendre et comparer soldes, emplacement, identité et accès piéton. Contrôler la Gestion et l’Arcade historiques dans une autre session.

Pour revenir en arrière, conserver et ouvrir le ZIP parent. Les reprises qui contiennent un restaurant allié demandent cette version ou une version compatible ultérieure.
