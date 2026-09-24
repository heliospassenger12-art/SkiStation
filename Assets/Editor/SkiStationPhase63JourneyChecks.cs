#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SkiStation;

public static class SkiStationPhase63JourneyChecks
{
    [MenuItem("SkiStation/Verification/Parcours phase 63 (hors Play)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Arreter Play avant ce test.");
        var visitor = new GameObject("JourneyCheck_Visitor");
        var station = new GameObject("JourneyCheck_Queue");
        var vehicleObject = new GameObject("JourneyCheck_Vehicle");
        try
        {
            var skier = visitor.AddComponent<SkierRuntime>();
            visitor.transform.position = ManagementSurface.Entrance + Vector3.up * 150f;
            var lifecycle = visitor.AddComponent<VisitorLifecycleRuntime>();
            typeof(VisitorLifecycleRuntime).GetMethod("BeginDeparture", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(lifecycle, null);
            Check(!(bool)typeof(VisitorLifecycleRuntime).GetField("departing", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(lifecycle), "Pas de depart direct depuis le sommet.");
            Check(!ManagementJourneyAccess.CanExitToVillage(visitor.transform.position), "Le sommet ne constitue pas une sortie de village.");
            visitor.transform.position = Vector3.zero;
            var arrival = visitor.AddComponent<StationVisitorRoadArrival248>();
            var queue = station.AddComponent<LiftQueueRuntime>();
            Check(!queue.Enqueue(skier), "La file refuse une arrivee non terminee.");
            Check(!VisitorSkiAdmission.CanSki(skier), "La route est obligatoire.");
            Set(arrival, "roadCompleted", true);
            Set(arrival, "ownsEquipment", true);
            Set(arrival, "<IsArriving>k__BackingField", false);
            skier.SetState(SkierState.Resting);
            Check(queue.Enqueue(skier), "Un visiteur arrive et equipe peut entrer dans la file.");
            var identity = visitor.AddComponent<ArrivalIdentity250>();
            identity.OwnEquipment = true;
            Check(!VisitorSkiAdmission.CanSki(skier), "Un nouveau visiteur sans forfait ne peut pas skier.");
            VisitorSkiAdmission.GrantPrepaid(skier);
            Check(VisitorSkiAdmission.HasPass(skier) && VisitorSkiAdmission.CanSki(skier), "Le forfait et le materiel debloquent le ski apres l'arrivee.");
            VisitorSkiAdmission.GrantPrepaid(skier);
            Check(VisitorSkiAdmission.HasPass(skier), "L'enregistrement du forfait est idempotent.");
            Check(!queue.Enqueue(skier) && queue.Count == 1, "Pas de double inscription.");
            var vehicle = vehicleObject.AddComponent<LiftVehicleRuntime>();
            var blocker = new GameObject("JourneyCheck_Blocker");
            try
            {
                Check(vehicle.TryBoard(blocker.AddComponent<SkierRuntime>()), "Remplissage de la place de test.");
                Check(!vehicle.TryBoard(queue.Peek()) && queue.Count == 1, "Un siege plein laisse le visiteur dans la file.");
            }
            finally { UnityEngine.Object.DestroyImmediate(blocker); }
            UnityEngine.Object.DestroyImmediate(visitor);
            Check(queue.Peek() == null && queue.Count == 0, "Une tete de file detruite ne bloque pas les suivants.");
            Debug.Log("Phase 63 : 12 controles de garde/files reussis. Tester ensuite le parcours en Play selon COMMENCER_ICI_PHASE63_CORRIGEE.md.");
        }
        finally
        {
            if (visitor != null) UnityEngine.Object.DestroyImmediate(visitor);
            UnityEngine.Object.DestroyImmediate(station);
            UnityEngine.Object.DestroyImmediate(vehicleObject);
        }
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static void Set(object target, string field, object value)
    { target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value); }
}
#endif
