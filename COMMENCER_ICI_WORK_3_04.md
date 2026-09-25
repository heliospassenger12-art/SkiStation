# SkiStation — Relais vers ChatGPT Work

## Version de référence
**SkiStation 3.04 — Objectif piste persistant et itinéraire sûr**

Ce dossier est le projet Unity complet à utiliser comme **nouvelle base de travail**. Ne pas repartir d'une ancienne version 2.x ou 3.00–3.03.

## Priorité actuelle
Après une longue passe de finition visuelle (2.57 → 3.00), le développement est revenu sur le gameplay visiteurs et la navigation d'un grand domaine skiable.

### 3.01 — Navigation multi-remontées
Les visiteurs peuvent planifier un trajet vers une piste éloignée en utilisant plusieurs remontées/pistes/connexions. Le planificateur principal est :
`Assets/SkiStation/Scripts/Slopes/MultiLiftDestinationPlanner301.cs`

### 3.02 — Domaine vivant
Le réseau doit pouvoir tenir compte des changements du domaine (construction, ouverture/fermeture, panne/réparation) afin que les itinéraires restent cohérents pendant la partie.

### 3.03 — Anti-bouchons
Le choix d'itinéraire tient compte de la congestion/files d'attente afin qu'un visiteur puisse préférer un autre chemin vers sa destination.

### 3.04 — Objectif piste persistant
Une piste choisie comme objectif reste la destination du visiteur jusqu'à ce qu'elle soit réellement terminée. Le chemin peut changer sans perdre cet objectif. Si la destination devient inaccessible, elle est abandonnée proprement. Toutes les pistes intermédiaires doivent être compatibles avec le niveau du skieur.

Voir `TEST_301.md`, `TEST_302.md`, `TEST_303.md` et `TEST_304.md`.

## Règles gameplay importantes à préserver
- Aucun téléport magique pour remonter la montagne.
- Les visiteurs doivent réellement utiliser les remontées.
- Le placement des remontées a été longuement stabilisé (phase 63) : éviter de le modifier sans nécessité.
- Une piste peut passer sous un télésiège/télécabine, mais pas traverser un téléski.
- Les arbres recouverts par une construction doivent être supprimés uniquement dans l'emprise nécessaire et après confirmation.
- Avant validation finale d'une construction : récapitulatif puis confirmer/modifier/annuler.
- Arcade et gestion sont deux flux distincts : ne pas casser le mode Arcade lors des modifications de gestion.

## Vision IA visiteurs
Le but est qu'un visiteur puisse se dire : « je veux absolument faire cette piste », comprendre comment l'atteindre à travers un grand domaine, changer de chemin si une remontée est saturée ou indisponible, mais conserver sa destination tant qu'elle reste accessible.

## Prochaine étape conseillée
Tester et renforcer la chaîne 3.01–3.04 dans Unity avec plusieurs itinéraires concurrents, fermetures et niveaux de skieurs. Ensuite, brancher cette intelligence de trajet de façon plus complète au comportement physique des visiteurs, sans téléportation ni contournement des files/remontées existantes.

## État de validation
Les archives ont été contrôlées structurellement, mais **la compilation et le Play Mode Unity de 3.04 n'ont pas été exécutés dans l'environnement de génération**. Le prochain opérateur doit donc commencer par vérifier la Console Unity et le scénario `TEST_304.md`.

## Préférences projet
- PC / Unity 6.
- Direction visuelle : station alpine réaliste, très légèrement cartoon, sobre et lisible.
- Priorité : boucle réellement jouable avant multiplication des systèmes secondaires.
- UI progressive/collapsible et lisible, sans superpositions.
