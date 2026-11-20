using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Management-only road network. The gateway road is immutable; player branches are additive.</summary>
    public sealed class StationRoadNetwork247 : MonoBehaviour
    {
        public static StationRoadNetwork247 I { get; private set; }
        public const float MainRoadHalfWidth = 12f;
        private readonly List<List<Vector3>> branches = new List<List<Vector3>>();
        private readonly List<GameObject> branchObjects = new List<GameObject>();
        private Vector3[] mainPath;
        public bool IsReady => mainPath != null && mainPath.Length > 1;
        public Vector3 MainRoadEnd => IsReady ? mainPath[mainPath.Length - 1] : Vector3.zero;
        public Vector3? FrontFootwayEnd { get; private set; }
        public Vector3[] GetPermanentRoadPath() => IsReady ? (Vector3[])mainPath.Clone() : null;
        public IReadOnlyList<List<Vector3>> Branches => branches;

        private void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; }
        private void OnDestroy() { if (I == this) I = null; }

        // Called by the permanent road visual after gateway creation. Never moves an existing road.
        public void RegisterPermanentRoad(Vector3[] path)
        {
            if (IsReady || path == null || path.Length < 2) return;
            mainPath = (Vector3[])path.Clone();
        }

        internal void RegisterFrontFootway(Vector3 end)
        {
            FrontFootwayEnd = end;
        }

        public bool CanPlaceBuilding(Vector3 center, float radius)
        {
            if (!IsReady) return false;
            if (ArrivalHub250.I != null && ArrivalHub250.I.BlocksConstruction(center, radius)) return false;
            if (IntersectsMainRoad(center, Mathf.Max(0, radius))) return false;
            foreach (var branch in branches)
                if (DistanceToPath(center, branch) < 9f + Mathf.Max(0f, radius)) return false;
            return true;
        }

        public bool IntersectsMainRoad(Vector3 center, float radius)
        {
            if (!IsReady) return false;
            Vector2 p = new Vector2(center.x, center.z);
            for (int i = 1; i < mainPath.Length; i++)
            {
                Vector2 a = new Vector2(mainPath[i - 1].x, mainPath[i - 1].z);
                Vector2 b = new Vector2(mainPath[i].x, mainPath[i].z);
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
                if ((p - (a + t * ab)).sqrMagnitude < Mathf.Pow(MainRoadHalfWidth + radius, 2f)) return true;
            }
            return false;
        }

        /// <summary>Road construction API for future construction UI. First point must connect to an existing road.
        /// A branch may connect at the permanent road's end or along either side of it.</summary>
        public bool TryAddPlayerRoad(IList<Vector3> waypoints, out string reason)
        {
            reason = "";
            if (!IsReady) { reason = "Route principale non initialisee"; return false; }
            if (waypoints == null || waypoints.Count < 2 || waypoints.Count > 128) { reason = "Trace invalide (2 a 128 points)"; return false; }
            var start = waypoints[0];
            if (!IsConnected(start, 16f)) { reason = "Le depart doit rejoindre une route existante"; return false; }
            var path = new List<Vector3>();
            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector3 p = waypoints[i];
                if (float.IsNaN(p.x) || float.IsNaN(p.z) || float.IsInfinity(p.x) || float.IsInfinity(p.z)) { reason = "Coordonnees invalides"; return false; }
                if (i > 0 && Vector2.Distance(new Vector2(p.x, p.z), new Vector2(waypoints[i-1].x, waypoints[i-1].z)) < 3f) { reason = "Points trop rapproches"; return false; }
                path.Add(new Vector3(p.x, SkiStationGuaranteedMountainVisual.Height(p.x, p.z), p.z));
            }
            if (!TerrainGeneration.RealTerrainWater.AllowsRoute(path,MainRoadHalfWidth*2,out reason,false)) return false;
            if(!Altitude.AltitudeConstructionPolicy.ValidateRoute(path,MainRoadHalfWidth,out reason,this))return false;
            GameObject root = new GameObject("Route_secondaire_" + (branches.Count + 1));
            root.transform.SetParent(transform, false);
            BuildRoad(root.transform, path);
            branches.Add(path);
            branchObjects.Add(root);
            return true;
        }

        public bool TryRemovePlayerRoad(int index)
        {
            if (index < 0 || index >= branches.Count) return false;
            branches.RemoveAt(index);
            if (branchObjects[index] != null) Destroy(branchObjects[index]);
            branchObjects.RemoveAt(index);
            return true; // Permanent road is not in the removable list.
        }

        /// <summary>Distance on the ground to any permanent or player-built road.</summary>
        public float DistanceToNearestRoad(Vector3 point)
        {
            if (!IsReady) return float.MaxValue;
            float distance = DistanceToPath(point, mainPath);
            foreach (var branch in branches)
                distance = Mathf.Min(distance, DistanceToPath(point, branch));
            return distance;
        }

        public bool IsRoadConnection(Vector3 p, float tolerance) => IsConnected(p, tolerance);

        private bool IsConnected(Vector3 p, float tolerance)
        {
            if (DistanceToPath(p, mainPath) <= tolerance) return true;
            foreach (var branch in branches) if (DistanceToPath(p, branch) <= tolerance) return true;
            return false;
        }
        private static float DistanceToPath(Vector3 p, IList<Vector3> path)
        {
            float best = float.MaxValue;
            Vector2 point = new Vector2(p.x, p.z);
            for (int i = 1; i < path.Count; i++)
            {
                Vector2 a = new Vector2(path[i-1].x, path[i-1].z), b = new Vector2(path[i].x, path[i].z);
                Vector2 d = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, d) / Mathf.Max(.0001f, d.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(point, a + d * t));
            }
            return best;
        }
        internal static void BuildRoad(Transform parent, List<Vector3> points)
        {
            // Meshes are independent from the permanent road and can be removed without affecting it.
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 a = points[i-1], b = points[i];
                Vector3 delta = b - a;
                Vector3 flat = new Vector3(delta.x, 0, delta.z);
                Quaternion rot = Quaternion.LookRotation(flat.normalized, Vector3.up);
                MakeBox(parent, "Chaussee", (a+b)*.5f + Vector3.up*.22f, rot, new Vector3(12f,.14f,flat.magnitude + .5f), new Color(.17f,.19f,.21f));
                Vector3 side = rot * Vector3.right;
                MakeBox(parent, "Trottoir_gauche", (a+b)*.5f - side*7.5f + Vector3.up*.3f, rot, new Vector3(3f,.2f,flat.magnitude+.5f), new Color(.64f,.65f,.64f));
                MakeBox(parent, "Trottoir_droit", (a+b)*.5f + side*7.5f + Vector3.up*.3f, rot, new Vector3(3f,.2f,flat.magnitude+.5f), new Color(.64f,.65f,.64f));
                for (float d = 5f; d < flat.magnitude-2f; d += 14f)
                    MakeBox(parent, "Marquage_automatique", a + flat.normalized*d + Vector3.up*.31f, rot, new Vector3(.18f,.04f,5f), new Color(.95f,.9f,.7f));
            }
        }
        private static void MakeBox(Transform parent, string label, Vector3 pos, Quaternion rot, Vector3 size, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = label; obj.transform.SetParent(parent, false);
            obj.transform.SetPositionAndRotation(pos, rot); obj.transform.localScale = size;
            var collider = obj.GetComponent<Collider>(); if (collider != null) Destroy(collider);
            // The test scene preserves the project's pipeline; no URP conversion is implied by package presence.
            var shader = SkiStation.ValAlpin.ValAlpinScene.IsActive && UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                ? Shader.Find("Standard") : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null) { var mat = new Material(shader); mat.color = color; obj.GetComponent<Renderer>().sharedMaterial = mat; }
        }
    }
}
