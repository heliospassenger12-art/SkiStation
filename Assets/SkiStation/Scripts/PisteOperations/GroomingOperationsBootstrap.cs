using UnityEngine;
namespace SkiStation { public static class GroomingOperationsBootstrap { [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Init(){
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
 if(Object.FindObjectOfType<GroomingScheduleRuntime>()==null)new GameObject("SkiStation_GroomingOperations").AddComponent<GroomingScheduleRuntime>(); if(Object.FindObjectOfType<GroomingDispatchRuntime>()==null)new GameObject("SkiStation_GroomingDispatch").AddComponent<GroomingDispatchRuntime>(); if(Object.FindObjectOfType<GroomingOperationsUI>()==null)new GameObject("SkiStation_GroomingUI").AddComponent<GroomingOperationsUI>(); } } }
