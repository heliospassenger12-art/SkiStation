# Portage de la gestion web vers Unity

- `gen_catalog.js` : régénère `Assets/SkiStation/Gestion/Core/Catalog.Generated.cs` depuis `Web/js/data.js`.
- `gen_rules.js` (+ `fn_sources.json`) : régénère événements, succès, scénarios et `Stats.Generated.cs`.
- `TestRelief.cs` : vérifie que le relief C# est identique au relief web (écart < 1 mm).
- `TestSim.cs` : construit un petit domaine, simule des journées, sauvegarde et recharge.

Compilation hors Unity (Mono) :

```
mcs -target:library -out:Gestion.dll Assets/SkiStation/Gestion/Core/*.cs
mcs -r:Gestion.dll -out:TestSim.exe Tools/GestionPort/TestSim.cs && mono TestSim.exe 4
```
