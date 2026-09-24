using UnityEngine;
namespace SkiStation.PisteOperations
{
 public static class GroomingSectorManagementBootstrap
 {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Init()
  {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

   if(GroomingSectorManagementRuntime.I==null)new GameObject("SkiStation_GroomingSectorManagement").AddComponent<GroomingSectorManagementRuntime>();
   if(Object.FindObjectOfType<GroomingSectorManagementUI>()==null)new GameObject("SkiStation_GroomingSectorManagementUI").AddComponent<GroomingSectorManagementUI>();
  }
 }
}
