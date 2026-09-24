using UnityEngine; namespace SkiStation { public static class WeatherDebugBootstrap { [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Init(){
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
if(Object.FindObjectOfType<WeatherDebugUI>()==null)new GameObject("SkiStation_WeatherUI").AddComponent<WeatherDebugUI>();} } }
