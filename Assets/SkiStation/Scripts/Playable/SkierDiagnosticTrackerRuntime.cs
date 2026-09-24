using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.11 diagnostic: follows a REAL SkierRuntime agent, with a world beacon and state trail.</summary>
    public sealed class SkierDiagnosticTrackerRuntime : MonoBehaviour
    {
        private SkierRuntime tracked;
        private SkierState lastState;
        private GameObject beacon;
        private LineRenderer trail;
        private readonly List<Vector3> trailPoints = new List<Vector3>();
        private float nextAcquire;
        private float nextTrail;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (FindFirstObjectByType<SkierDiagnosticTrackerRuntime>() == null)
                new GameObject("SkiStation_2.11_REAL_SKIER_DIAGNOSTIC").AddComponent<SkierDiagnosticTrackerRuntime>();
        }

        private void Update()
        {
            if (tracked == null)
            {
                if (Time.unscaledTime >= nextAcquire) { nextAcquire = Time.unscaledTime + .5f; Acquire(); }
                return;
            }

            if (beacon != null)
                beacon.transform.position = tracked.transform.position + Vector3.up * 7f;

            if (tracked.State != lastState)
            {
                lastState = tracked.State;
                Debug.Log("SkiStation 2.11 TRACKED SKIER STATE => " + lastState + " @ " + tracked.transform.position);
            }

            if (Time.unscaledTime >= nextTrail)
            {
                nextTrail = Time.unscaledTime + .12f;
                Vector3 p = tracked.transform.position + Vector3.up * .35f;
                if (trailPoints.Count == 0 || Vector3.Distance(trailPoints[trailPoints.Count - 1], p) > 1.5f)
                {
                    trailPoints.Add(p);
                    if (trailPoints.Count > 220) trailPoints.RemoveAt(0);
                    if (trail != null) { trail.positionCount = trailPoints.Count; trail.SetPositions(trailPoints.ToArray()); }
                }
            }
        }

        private void Acquire()
        {
            var all = FindObjectsByType<SkierRuntime>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0) return;
            // Prefer an agent already engaged in the ski loop.
            var proof = FindFirstObjectByType<VisibleSkiLoopAuthorityRuntime>();
            if (proof != null && proof.TrackedSkier != null) tracked = proof.TrackedSkier;
            else tracked = all[0];
            if (proof == null || proof.TrackedSkier == null)
                for (int i = 0; i < all.Length; i++)
                    if (all[i].State == SkierState.RidingLift || all[i].State == SkierState.Skiing || all[i].State == SkierState.WaitingForLift) { tracked = all[i]; break; }
            lastState = tracked.State;
            BuildBeacon();
            Debug.Log("SkiStation 2.11 REAL SKIER TRACKER attached to " + tracked.name + " state=" + tracked.State);
        }

        private void BuildBeacon()
        {
            beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "TRACKED_SKIER_BEACON";
            beacon.transform.localScale = Vector3.one * 5f;
            var c = beacon.GetComponent<Collider>(); if (c != null) Destroy(c);
            var r = beacon.GetComponent<Renderer>();
            if (r != null)
            {
                var shader = Shader.Find("Standard");
                if (shader != null) { var m = new Material(shader); m.color = new Color(1f, .12f, .72f); r.material = m; }
            }

            var go = new GameObject("TRACKED_SKIER_TRAIL");
            trail = go.AddComponent<LineRenderer>();
            trail.useWorldSpace = true;
            trail.startWidth = 2.2f; trail.endWidth = 2.2f;
            var s = Shader.Find("Sprites/Default");
            if (s != null) { var m = new Material(s); m.color = new Color(1f, .12f, .72f); trail.material = m; }
        }

        private string FrenchState()
        {
            if (tracked == null) return "EN ATTENTE D'UN VISITEUR";
            var arrival = tracked.GetComponent<StationVisitorRoadArrival248>();
            if (arrival != null && arrival.IsArriving) { var identity=tracked.GetComponent<ArrivalIdentity250>(); return (identity != null ? (identity.Mode==ArrivalMode250.Bus?"Bus : ":identity.Mode==ArrivalMode250.Car?"Voiture : ":"Pieton : ") : "") + arrival.ArrivalStage; }
            switch (tracked.State)
            {
                case SkierState.Arriving: return "ARRIVE";
                case SkierState.WaitingForLift: return "ATTEND LA REMONTEE";
                case SkierState.RidingLift: return "MONTE EN REMONTEE";
                case SkierState.Skiing: return "DESCEND / SKIE";
                case SkierState.Resting: return "REPOS";
                case SkierState.Leaving: return "REPART";
                default: return tracked.State.ToString().ToUpperInvariant();
            }
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
            string name = tracked != null ? tracked.name : "aucun";
            GUI.Box(new Rect(Screen.width - 390, 18, 370, 72), "SKIEUR REEL SUIVI\n" + FrenchState() + "  |  " + name, style);
            if (tracked != null && Camera.main != null)
            {
                Vector3 sp = Camera.main.WorldToScreenPoint(tracked.transform.position + Vector3.up * 10f);
                if (sp.z > 0f)
                {
                    var tag = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
                    GUI.Box(new Rect(sp.x - 95, Screen.height - sp.y - 22, 190, 34), "★ " + FrenchState(), tag);
                }
            }
        }
    }
}
