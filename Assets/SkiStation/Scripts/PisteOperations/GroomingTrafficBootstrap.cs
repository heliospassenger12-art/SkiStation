using UnityEngine;
namespace SkiStation.PisteOperations {
 public static class GroomingTrafficBootstrap {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

   if(GroomingTrafficRuntime.I==null){var go=new GameObject("SkiStation_GroomingTraffic");go.AddComponent<GroomingTrafficRuntime>();}
  }
 }
}
