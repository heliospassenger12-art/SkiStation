using UnityEngine;
namespace SkiStation { public sealed class VisitorDayPlanBootstrap : MonoBehaviour { [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Boot(){
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
 if(VisitorDayPlanRuntime.I!=null)return; var go=new GameObject("SkiStation_VisitorDayPlan"); go.AddComponent<VisitorDayPlanRuntime>(); } } }
