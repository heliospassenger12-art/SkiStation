using UnityEngine;
namespace SkiStation { public static class GroomingBootstrap { [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Init(){
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
 foreach(var s in Object.FindObjectsOfType<SlopeRuntime>()){ if(s.GetComponent<SlopeMaintenanceRuntime>()==null)s.gameObject.AddComponent<SlopeMaintenanceRuntime>(); } } } }
