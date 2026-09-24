using UnityEngine;

namespace SkiStation
{
    /// <summary>Management-only two-point secondary-road construction. No financial debit or persistence yet.</summary>
    public sealed class StationRoadConstruction249 : MonoBehaviour
    {
        private static StationRoadConstruction249 instance;
        private bool active, hasStart, confirming;
        private Vector3 start, end;
        private string status = "Cliquez sur une route existante pour commencer.";
        private LineRenderer guide;
        private const float MaxLength = 350f;
        private const float CostPerMeter = 450f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            instance = new GameObject("SkiStation_249_ConstructionRoutes").AddComponent<StationRoadConstruction249>();
        }

        private void Awake()
        {
            var go = new GameObject("Apercu_route_secondaire");
            go.transform.SetParent(transform, false);
            guide = go.AddComponent<LineRenderer>();
            guide.useWorldSpace = true;
            guide.positionCount = 2;
            guide.startWidth = guide.endWidth = 3f;
            guide.material = new Material(Shader.Find("Sprites/Default"));
            guide.enabled = false;
        }

        private void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (Input.GetKeyDown(KeyCode.R))
            {
                active = !active; hasStart = confirming = false;
                status = "Cliquez sur une route existante pour commencer.";
                if (guide != null) guide.enabled = false;
            }
            if (!active) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (confirming) confirming = false;
                else if (hasStart) hasStart = false;
                else active = false;
                if (guide != null) guide.enabled = false;
            }
            if (!active || confirming) return;
            Vector3 point;
            if (!TryGroundPoint(out point)) { if (guide != null) guide.enabled = false; return; }
            if (hasStart)
            {
                end = point;
                guide.enabled = true;
                guide.SetPosition(0, start + Vector3.up * 1.3f);
                guide.SetPosition(1, end + Vector3.up * 1.3f);
                bool valid = ValidateEnd(out status);
                guide.startColor = guide.endColor = valid ? Color.green : Color.red;
            }
            if (Input.GetMouseButtonDown(0) && !OverPanel())
            {
                var network = StationRoadNetwork247.I;
                if (network == null || !network.IsReady) { status = "Route principale non initialisee."; return; }
                if (!hasStart)
                {
                    if (!network.IsRoadConnection(point, 12f)) { status = "Depart : cliquez sur une route existante."; return; }
                    start = point; hasStart = true; status = "Choisissez l'arrivee de la route.";
                }
                else if (ValidateEnd(out status)) confirming = true;
            }
        }

        private bool ValidateEnd(out string reason)
        {
            float distance = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(end.x, end.z));
            if (distance < 12f) { reason = "Route trop courte (12 m minimum)."; return false; }
            if (distance > MaxLength) { reason = "Route trop longue (350 m maximum)."; return false; }
            const int samples = 12;
            float last = SkiStationGuaranteedMountainVisual.Height(start.x, start.z);
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                float x = Mathf.Lerp(start.x, end.x, t), z = Mathf.Lerp(start.z, end.z, t);
                float height = SkiStationGuaranteedMountainVisual.Height(x, z);
                if (Mathf.Abs(height - last) / (distance / samples) > .25f)
                { reason = "Pente trop forte : modifiez le trace (25 % maximum)."; return false; }
                last = height;
            }
            reason = "Trace valide : cliquez pour afficher le recapitulatif.";
            return true;
        }

        private static bool TryGroundPoint(out Vector3 point)
        {
            point = Vector3.zero;
            Camera camera = Camera.main;
            if (camera == null) return false;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, SkiStationGuaranteedMountainVisual.Height(0f, -2600f), 0f));
            float distance;
            if (!plane.Raycast(ray, out distance) || distance < 0f) return false;
            Vector3 hit = ray.GetPoint(distance);
            point = new Vector3(hit.x, SkiStationGuaranteedMountainVisual.Height(hit.x, hit.z), hit.z);
            return true;
        }

        private static bool OverPanel() => Input.mousePosition.x < 390f && Input.mousePosition.y > Screen.height - 285f;

        private void Confirm()
        {
            string reason;
            if (!ValidateEnd(out reason)) { status = reason; confirming = false; return; }
            var network = StationRoadNetwork247.I;
            if (network == null || !network.TryAddPlayerRoad(new[] { start, end }, out reason))
            { status = reason; confirming = false; return; }
            status = "Route secondaire construite (prototype : aucun debit).";
            hasStart = confirming = false;
            guide.enabled = false;
        }

        private void OnGUI()
        {
            if (!active || SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            GUILayout.BeginArea(new Rect(12, 305, 375, confirming ? 205 : 135), GUI.skin.box);
            GUILayout.Label("ROUTES SECONDAIRES  |  R : fermer");
            GUILayout.Label(status);
            if (confirming)
            {
                float distance = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(end.x, end.z));
                GUILayout.Label("Longueur : " + distance.ToString("F0") + " m  |  Largeur : 12 m");
                GUILayout.Label("Cout indicatif : " + Mathf.RoundToInt(distance * CostPerMeter) + " EUR (non debite)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Confirmer")) Confirm();
                if (GUILayout.Button("Modifier")) confirming = false;
                if (GUILayout.Button("Annuler")) { confirming = hasStart = false; guide.enabled = false; }
                GUILayout.EndHorizontal();
            }
            else GUILayout.Label("Clic 1 : depart sur route  |  Clic 2 : arrivee");
            GUILayout.EndArea();
        }
        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (guide != null && guide.material != null) Destroy(guide.material);
        }
    }
}
