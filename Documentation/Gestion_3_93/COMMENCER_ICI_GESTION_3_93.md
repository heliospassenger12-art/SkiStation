# SkiStation Gestion 3.93 — Pistes vivantes

Gestion 3.93 terminée structurellement. Compilation Unity, Play Mode et validation réelle des pistes vivantes, des itinéraires, des performances, des animations et de la compatibilité Arcade restent à effectuer dans Unity.

Base : dernière 3.92 Hybrid Motion clôturée, reçue et vérifiée avant toute modification. Ouvrir le dossier SkiStation_GESTION_3_93 dans Unity 6000.3.7f1, URP conservée. Tous les fichiers du projet et modules Hybrid sont présents. Les contrôles/physique/poses Arcade et Hybrid restent byte-identiques ; le pilote expérimental Hybrid garde son activation native, OFF par défaut.

## Première séance de test
1. Importer et compiler ; corriger la première erreur éventuelle avant le Play.
2. Menu SkiStation / Gestion / 3.93 / Contrôles des profils et migrations (hors Play), puis anciens contrôles 3.90/3.92 et Hybrid dans leur contexte. Les assertions de schéma ont été actualisées vers CurrentSchema ; les anciens manifestes d’intégrité ne sont pas applicables au nouveau projet modifié.
3. Dans une scène jetable, construire une vraie remontée et des pistes nommées Piste_…, avec verte accessible, bleue et rouge, embranchements aux vraies extrémités, trois convergences et un trajet vers une autre remontée. Ajouter SkiDescentCampaign393 sous la racine du domaine sauvegardable, assigner les trois objectifs et départ, puis créer les trois vrais visiteurs. Ils commencent en gare aval, pas au sommet. Lancer la surveillance.
4. Observer le vrai trajet, dégagement, GPS, accès, descente, jonctions et fin. Vérifier les niveaux, le contact terrain et le diagnostic (V ou sélection visiteur). Sélectionner une piste pour ses métriques contextuelles. Tester une autre orientation du massif et la caméra éloignée.
5. Sur copie de test compatible, lancer SkiDescentRoundTrip393 au milieu d’une descente ; puis LiftTrafficRoundTrip391 avec file et véhicule occupé. Tester aussi un redémarrage réel de l’application, non couvert par le banc.
6. Ajouter SkiDescentBench393 ; mesurer 10/100/500 visiteurs réellement présents sur plusieurs versants, si le matériel le permet, avec Profiler et comparaisons proches/lointaines. Le banc ne crée pas de foule fictive et n’annonce aucun gain.
7. Tester fermeture, GPS de retour, besoins, groupes, services et journée complète, puis toute la boucle Arcade sur le même massif.

## Limites et statut
Le checkpoint demeure matériel + trafic compatible, pas une sauvegarde Carrière universelle. Services engagés, cours actifs, séjours, réseau routier logique, certains chantiers et ressources/avatars non catalogués peuvent toujours faire refuser une sauvegarde. Aucun refus n’est contourné. La réflexion des états et IL2CPP restent non vérifiés. Une surface/corridor invalide arrête prudemment la progression avec diagnostic ; cela ne garantit pas encore un parcours sans blocage dans toutes les scènes. Les animations procédurales sont simples ; les rigs et équipements existants ne sont pas remplacés. La précision de contact sur rochers/bosses et les croisements doivent être observés. Le GPS reste Dijkstra synchrone : les connexions sont cachées, mais un ordonnanceur global de demandes n’est pas ajouté. Les très grands graphes, allocations de recherche, paramètres de connexion inhabituels et limites de requêtes physiques restent à profiler.

Les 60 points sont audités dans FINAL_VALIDATION_GESTION_3_93.md. Les scénarios sont préparés et les sources raccordées ; aucune compilation Unity, réussite de Play Mode, journée réelle ou mesure de performance n’est revendiquée. La prochaine correction doit repartir de ce projet complet, sans réinstaller d’anciens contrôleurs. Aucune version intermédiaire et aucune 3.94.
