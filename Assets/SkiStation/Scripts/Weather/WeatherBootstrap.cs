using UnityEngine; namespace SkiStation { public static class WeatherBootstrap { [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Init(){
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
 if(Object.FindObjectOfType<WeatherRuntime>()==null)new GameObject("SkiStation_Weather").AddComponent<WeatherRuntime>(); } } }
