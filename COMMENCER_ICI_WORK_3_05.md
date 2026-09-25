# SkiStation 3.05 — GPS du domaine : étapes ordonnées

## Base officielle
Cette version part intégralement du ZIP SkiStation 3.04 fourni par Hélios. Le point de reprise initial reste archivé dans COMMENCER_ICI_WORK_3_04.md. Les fichiers 2.x présents sont historiques et ne constituent pas la base active.

La 3.04 a été enregistrée séparément sur GitHub : 5f546ad4c16ae475416fd9f293b39c6047b1359e.

## Changements 3.05
- Le planificateur expose la première étape réelle du trajet, y compris une piste intermédiaire ou une liaison à pied. Il ne renvoie plus une remontée située plus loin dans la chaîne comme action immédiate.
- Après le débarquement réel et à la fin de chaque piste, SkierRouteAgent reprend le GPS avant les anciens choix locaux. Les déplacements, files et véhicules existants exécutent chaque étape.
- Ajout des correspondances courtes entre deux remontées consécutives. Les liaisons et les raccords sont contrôlés par ManagementJourneyAccess.CanWalk (distance et pente du terrain).
- Les coûts du GPS sont exprimés en secondes estimées : marche, descente, montée et attente. Les restrictions et les coûts sont calculés une fois par lien et par recherche.
- La difficulté maximale du profil est appliquée strictement à toutes les pistes du trajet, destination comprise.
- Une cible inaccessible/fermée est supprimée proprement, même si aucune alternative n'existe. Sans trajet accessible, le visiteur attend et réessaie au lieu de partir vers une piste arbitraire.
- La fiche visiteur affiche la piste souhaitée et le dernier itinéraire calculé. L'itinéraire est rafraîchi aux points de décision ; ce n'est pas un suivi graphique en temps réel.

## Ce qui est conservé
Les scripts Arcade, scènes, construction des gares, véhicules des remontées, arrivée routière et services d'accueil n'ont pas été modifiés. Le script VisibleSkiLoopAuthorityRuntime de la boucle témoin est identique à la 3.04. Le GPS concerne les visiteurs dont SkierRouteAgent est actif ; le marqueur rose peut rester piloté par la boucle témoin.

GOAP n'est pas ajouté. Les décisions générales (manger, repos, retour) feront l'objet d'une étape ultérieure après validation du GPS.

## Validation et limites
Analyse syntaxique des 447 fichiers C# réussie. Comparaison des fichiers avec la base et intégrité ZIP contrôlées. Cela ne vaut pas compilation Unity ni test du gameplay. Unity n'est pas disponible dans l'environnement de préparation : la compilation et les scénarios de TEST_305.md restent à effectuer sur PC.

CanWalk contrôle le relief mais ne constitue pas un NavMesh d'évitement des bâtiments. Le coût de descente utilise une vitesse de référence de 12 m/s et celui de marche 6 m/s : ce sont des estimations de décision. Les performances sur un grand domaine peuplé restent à mesurer. Les recalculs se font aux changements d'étape et aux échecs d'accès, sans retirer un visiteur d'un véhicule ni réordonner une file déjà rejointe.

## Installation
Décompresser cette archive complète dans un nouveau dossier. Ajouter ce dossier comme projet dans Unity Hub et utiliser la version indiquée dans ProjectSettings/ProjectVersion.txt. Laisser Unity importer, vérifier la Console puis suivre TEST_305.md. Ne pas écraser la copie 3.04 validée pendant le premier essai.
