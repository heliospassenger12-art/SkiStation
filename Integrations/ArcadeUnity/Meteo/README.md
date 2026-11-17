Le dossier contient le code météo de Grand Aigle pour Unity 6000.3.7f1 et URP 17.3. Il vise les quatre ambiances de la planche d’intention, avec des nuages plus épais quand le ciel se couvre, une lumière diffuse, des flocons entraînés par le vent et une visibilité réduite dans la brume.

La planche générée sert de référence artistique. La compilation et l’évaluation visuelle dans Unity restent à réaliser.

Installation sur Alpine Empires Avant Première

1. Depuis la racine du projet Unity, appliquer la version actuelle de `../SkiStation-Unity-Arcade-Cycle-Lumiere-Grand-Aigle.patch`. Ce patch raccorde aussi le cycle à `ArcadeTestSlope`.
2. Appliquer ensuite `../SkiStation-Unity-Arcade-Meteo-Grand-Aigle.patch`. Vérifier chaque application avec `git apply --check` avant `git apply`.
3. Si une ancienne version des patchs est déjà installée, copier les fichiers du dossier `Assets` de cette livraison aux mêmes chemins dans le projet. Garder les fichiers `.meta` préexistants des assets remplacés. Les deux nouveaux assets sont accompagnés de leurs `.meta`.
4. Ouvrir `Assets/Scenes/SkiStation_Bootstrap.unity`, attendre l’import et choisir Arcade puis Grand Aigle.

Les sources prêtes à copier comprennent `ArcadeGrandAigleDaylight.cs`, `ArcadeGrandAigleWeather.cs`, `ArcadeAlpineSky257.shader` et `GrandAigleWeatherSnow.shader`. Le patch du cycle fournit le raccord à `ArcadeTestSlope` lors d’une première installation.

Réglages et comparaison

En Play Mode, sélectionner la racine générée de Grand Aigle. Son composant `ArcadeGrandAigleWeather` expose `Preset`, `Hold Time`, `Transition Seconds` et les paramètres des quatre profils. `Hold Time` fixe l’heure et laisse évoluer les nuages et les transitions météo. Les menus contextuels `Météo/Ciel clair`, `Météo/Couvert`, `Météo/Neige` et `Météo/Brouillard` sélectionnent le profil.

Les valeurs modifiées dans l’inspecteur pendant Play Mode sont temporaires. Pour conserver un réglage de référence, reporter ses valeurs dans les champs initialisés de `ArcadeGrandAigleWeather.cs`. Après un changement des paramètres d’un profil, sélectionner un autre preset puis revenir au preset édité pour déclencher le fondu.

| Profil | Nuages | Brume | Lumière | Voile | Neige | Vent |
| --- | --- | --- | --- | --- | --- | --- |
| Clear | 0,28 | 0,00012 | 1,00 | 0,00 | 0,00 | 0,45 |
| Overcast | 0,77 | 0,00040 | 0,58 | 0,52 | 0,00 | 1,20 |
| Snow | 0,93 | 0,00085 | 0,52 | 0,65 | 1,00 | 2,25 |
| Fog | 0,69 | 0,00160 | 0,48 | 0,72 | 0,00 | 0,65 |

Exemple d’appel depuis une méthode de votre code Unity pendant une session active

```csharp
var weather = UnityEngine.Object.FindFirstObjectByType<SkiStation.Arcade.ArcadeGrandAigleWeather>();
if (weather != null)
{
    weather.HoldTime = true;
    weather.SetPreset(SkiStation.Arcade.ArcadeGrandAigleWeather.WeatherPreset.Snow);
}
```

Le mode `Forecast` suit une prévision de démonstration liée au cycle visuel. Il passe au couvert à 9 h 30, à la neige à 12 h, à la brume à 15 h 30 et au ciel clair à 17 h. Le fondu dure 25 secondes de jeu actif par défaut. Les flocons et le cycle s’arrêtent en pause.

Trois prompts prêts à utiliser avec un assistant de code ayant accès au projet Unity

1. Intégration et compilation

```text
Travaille dans SkiStation Alpine Empires Avant Première, Unity 6000.3.7f1, URP 17.3. Lis Integrations/ArcadeUnity/Meteo/README.md et les sources fournies. Identifie les patchs Grand Aigle déjà appliqués. Intègre la dernière livraison par la méthode adaptée à cet état, en conservant les GUID existants. Vérifie que Bootstrap → Arcade → Grand Aigle crée une seule instance de la météo et du cycle. Corrige les erreurs C# et de shaders réellement signalées par Unity. Contrôle le constructeur Palette et son affectation Hour = hour. Livre le diff, la version testée et le résultat observé dans l’éditeur. Mets le code et le suivi à jour sur la branche GitHub de la PR nº 4.
```

2. Réglage artistique

```text
Sur Grand Aigle, utilise la planche météo à quatre panneaux comme référence artistique. Garde la même caméra, l’heure à midi, le trajet et l’exposition pour comparer Clear, Overcast, Snow et Fog. Utilise Hold Time et SetPreset du composant ArcadeGrandAigleWeather. Recherche un ciel clair bleu avec des ombres fraîches, un couvert gris-bleu diffus, des chutes de neige lisibles et un brouillard qui masque d’abord l’arrière-plan. Ajuste les profils et le shader à partir des captures Unity observées. Garde les commandes, la glisse, le score, les seuils jaune/vert/rouge du boost et les corridors skiables fonctionnels. Relève le temps d’image dans le Profiler et joins les quatre captures réelles avec leurs paramètres. Enregistre les réglages retenus dans le code et actualise la PR nº 4.
```

3. Vérification du cycle et des transitions

```text
Vérifie la livraison météo Grand Aigle dans Unity. À midi, passe de Clear à Snow pendant le fondu, puis à Fog avant sa fin. Vérifie la continuité de la lumière et des nuages. Mets en pause pendant la neige, reprends puis sors de la session. Vérifie l’arrêt et la reprise des flocons, leur disparition au retour à l’accueil et la libération du matériau. Relance Arcade deux fois et contrôle l’absence de doublons. Compare les lumières à 0 h, 6 h 18, 12 h et 19 h 18, puis le passage de minuit et Hold Time. Note la première erreur complète, la scène, le commit et les résultats observés. Corrige les problèmes reproductibles et pousse les changements dans la PR nº 4.
```

Vérifications effectuées

Application et annulation du patch météo vérifiées sur sa base de code reconstruite. Les contrôles arithmétiques couvrent 1 441 heures échantillonnées, les bornes des transitions et leur interruption. Un contrôle ciblé repère l’omission de l’affectation de l’heure dans le constructeur de palette. La couverture nuageuse moyenne augmente entre les valeurs Clear, Overcast et Snow testées.

La compilation Unity, les variantes de shaders URP, les interactions visuelles des flocons avec le terrain, le Play Mode et les mesures GPU restent à vérifier. Les précipitations utilisent des billboards limités à 750 particules. Les nuages et les rayons sont calculés dans le ciel.
