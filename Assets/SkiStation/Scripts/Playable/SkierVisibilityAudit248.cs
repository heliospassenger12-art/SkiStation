using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Phase 7: read-only management-mode audit. F9 toggles a compact on-screen report (F8 is reserved for lift flows).
    /// It never spawns, teleports, or steers a skier and does not affect arcade mode.
    /// </summary>
    public sealed class SkierVisibilityAudit248 : MonoBehaviour
    {
        private bool show;
        private float nextRefresh;
        private int total, arriving, waiting, riding, skiing, resting, leaving;
        private int rendered, inCamera, offCorridor, noSlope, noRenderer, inactiveObjects;
        private float largestCorridorDeviation;
        private const float CorridorTolerance = 3f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (FindFirstObjectByType<SkierVisibilityAudit248>() == null)
                new GameObject("SkiStation_2.48_SKIER_VISIBILITY_AUDIT").AddComponent<SkierVisibilityAudit248>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9)) show = !show;
            if (Input.GetKeyDown(KeyCode.F10)) LogSnapshot();
            if (!show || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.5f;
            Refresh();
        }

        private void Refresh()
        {
            total = arriving = waiting = riding = skiing = resting = leaving = 0;
            rendered = inCamera = offCorridor = noSlope = noRenderer = inactiveObjects = 0;
            largestCorridorDeviation = 0f;
            var camera = Camera.main;
            var planes = camera != null ? GeometryUtility.CalculateFrustumPlanes(camera) : null;
            foreach (var skier in FindObjectsByType<SkierRuntime>(FindObjectsSortMode.None))
            {
                if (skier == null) continue;
                total++;
                if (!skier.gameObject.activeInHierarchy) inactiveObjects++;
                switch (skier.State)
                {
                    case SkierState.Arriving: arriving++; break;
                    case SkierState.WaitingForLift: waiting++; break;
                    case SkierState.RidingLift: riding++; break;
                    case SkierState.Skiing: skiing++; break;
                    case SkierState.Resting: resting++; break;
                    case SkierState.Leaving: leaving++; break;
                }
                bool visibleRenderer = false;
                bool insideCamera = false;
                foreach (var renderer in skier.GetComponentsInChildren<Renderer>())
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    visibleRenderer = true;
                    if (planes != null && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) insideCamera = true;
                }
                if (visibleRenderer) rendered++;
                else noRenderer++;
                if (insideCamera) inCamera++;
                if (skier.State != SkierState.Skiing) continue;
                var agent = skier.GetComponent<SkierRouteAgent>();
                var slope = agent != null ? agent.CurrentSlope : null;
                if (slope == null || slope.Points == null || slope.Points.Count < 2) { noSlope++; continue; }
                float nearest = float.MaxValue;
                float width = 0f;
                Vector2 p = new Vector2(skier.transform.position.x, skier.transform.position.z);
                for (int i = 0; i < slope.Points.Count - 1; i++)
                {
                    Vector3 a3 = slope.Points[i], b3 = slope.Points[i + 1];
                    Vector2 a = new Vector2(a3.x, a3.z), b = new Vector2(b3.x, b3.z);
                    Vector2 segment = b - a;
                    float t = segment.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - a, segment) / segment.sqrMagnitude) : 0f;
                    float distance = Vector2.Distance(p, a + segment * t);
                    if (distance < nearest) { nearest = distance; width = slope.GetWidthAt(i); }
                }
                float deviation = Mathf.Max(0f, nearest - width * 0.5f);
                largestCorridorDeviation = Mathf.Max(largestCorridorDeviation, deviation);
                if (nearest > Mathf.Max(5f, width * 0.5f) + CorridorTolerance) offCorridor++;
            }
        }

        // Manual, bounded snapshot: useful when a skier disappears before a screenshot can be taken.
        // No gameplay state is modified and no log is produced unless F10 is pressed.
        private void LogSnapshot()
        {
            var camera = Camera.main;
            var planes = camera != null ? GeometryUtility.CalculateFrustumPlanes(camera) : null;
            var skiers = FindObjectsByType<SkierRuntime>(FindObjectsSortMode.None);
            Debug.Log("[SkiStation 2.48 F10] Skier snapshot: " + skiers.Length +
                      " agents; main camera=" + (camera != null ? camera.name : "NONE"));
            int logged = 0;
            foreach (var skier in skiers)
            {
                if (skier == null) continue;
                bool hasRenderer = false, inFrustum = false;
                foreach (var renderer in skier.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    hasRenderer = true;
                    if (planes != null && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) inFrustum = true;
                }
                var agent = skier.GetComponent<SkierRouteAgent>();
                bool missingSlope = skier.State == SkierState.Skiing &&
                                    (agent == null || agent.CurrentSlope == null);
                // Prioritize anomalies, then include a few ordinary agents as a comparison.
                if (hasRenderer && inFrustum && !missingSlope && logged >= 4) continue;
                Debug.Log("[SkiStation 2.48 F10] " + skier.name + " state=" + skier.State +
                          " position=" + skier.transform.position.ToString("F1") +
                          " active=" + skier.gameObject.activeInHierarchy +
                          " renderer=" + hasRenderer + " inFrustum=" + inFrustum +
                          " skiingWithoutSlope=" + missingSlope);
                if (++logged >= 16) break;
            }
            Debug.Log("[SkiStation 2.48 F10] Snapshot complete (max 16 agents). " +
                      "InFrustum is not an occlusion test.");
        }

        private void OnGUI()
        {
            if (!show) return;
            GUILayout.BeginArea(new Rect(12, 135, 440, 240), GUI.skin.box);
            GUILayout.Label("SKISTATION 2.48 | DIAGNOSTIC (F9 / F10 log)");
            GUILayout.Label("Total : " + total + " | Avec modele : " + rendered + " | Dans camera : " + inCamera);
            GUILayout.Label("Sans modele actif : " + noRenderer + " | Objet inactif : " + inactiveObjects);
            GUILayout.Label("Route : " + arriving + " | File : " + waiting + " | Remontee : " + riding);
            GUILayout.Label("Piste : " + skiing + " | Repos : " + resting + " | Depart : " + leaving);
            GUILayout.Label("Hors couloir piste (estimation) : " + offCorridor);
            GUILayout.Label("Ecart maximal estime : " + largestCorridorDeviation.ToString("0.0") + " m");
            GUILayout.Label("Etat ski sans piste associee : " + noSlope);
            GUILayout.Label("Dans camera = frustum, pas visibilite sans obstacle.");
            GUILayout.Label("Lecture seule : aucune correction automatique.");
            GUILayout.EndArea();
        }
    }
}
