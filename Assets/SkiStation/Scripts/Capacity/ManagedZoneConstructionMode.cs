using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Simple world-space polygon drawing tool. Z toggles the tool.</summary>
    public sealed class ManagedZoneConstructionMode : MonoBehaviour
    {
        [SerializeField] private Camera buildCamera;
        [SerializeField] private ManagedZoneType zoneType = ManagedZoneType.Beginner;
        [SerializeField] private float rayDistance = 5000f;
        private readonly List<Vector3> points = new List<Vector3>();
        private LineRenderer preview;
        public bool IsDrawing => active;
        private bool active;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z)) Toggle();
            if (!active) return;
            if (buildCamera == null) buildCamera = Camera.main;

            if (Input.GetMouseButtonDown(0) && TryGroundPoint(out var p))
            {
                points.Add(p);
                RefreshPreview();
            }
            if (Input.GetMouseButtonDown(1) && points.Count > 0)
            {
                points.RemoveAt(points.Count - 1);
                RefreshPreview();
            }
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
            {
                points.Clear(); RefreshPreview();
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Finish();
            if (Input.GetKeyDown(KeyCode.Escape)) Cancel();
        }

        private void Toggle()
        {
            active = !active;
            if (!active) Cancel();
            else EnsurePreview();
        }

        private void Finish()
        {
            if (points.Count < 3) return;
            if (ManagedZoneConfirmationRuntime.I == null) return;
            ManagedZoneConfirmationRuntime.I.Prepare(zoneType, points);
            // Keep the preview while the player reviews the recap. A new trace can still replace it.
            points.Clear(); RefreshPreview();
        }

        private void Cancel()
        {
            points.Clear();
            if (preview != null) preview.positionCount = 0;
        }

        private bool TryGroundPoint(out Vector3 point)
        {
            point = default;
            if (buildCamera == null) return false;
            Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out var hit, rayDistance, ~0, QueryTriggerInteraction.Ignore) &&
                   (point = hit.point) != default;
        }

        private void EnsurePreview()
        {
            if (preview != null) return;
            var go = new GameObject("SkiStation_ZonePreview");
            preview = go.AddComponent<LineRenderer>();
            preview.useWorldSpace = true;
            preview.widthMultiplier = .09f;
            preview.loop = false;
            preview.positionCount = 0;
        }

        private void RefreshPreview()
        {
            EnsurePreview();
            preview.positionCount = points.Count + (points.Count >= 3 ? 1 : 0);
            for (int i = 0; i < points.Count; i++) preview.SetPosition(i, points[i] + Vector3.up * .15f);
            if (points.Count >= 3) preview.SetPosition(points.Count, points[0] + Vector3.up * .15f);
        }

        private void OnGUI()
        {
            if (!active && !LegacyUIPanelCoordinator.DiagnosticsVisible) return;
            GUI.Box(new Rect(18, Screen.height - 150, 500, 125), "ZONES DE GESTION — Z");
            GUI.Label(new Rect(32, Screen.height - 122, 465, 20), active
                ? "Dessin actif : clic gauche = point • clic droit = retour"
                : "Z : dessiner une zone physique (débutants par défaut)");
            GUI.Label(new Rect(32, Screen.height - 98, 465, 20), "Entrée = créer • Échap = annuler • Suppr = effacer les points");
            GUI.Label(new Rect(32, Screen.height - 74, 465, 20), "Type actuel : " + zoneType + " — frontière matérialisée par piquets/barrière");
            if (ManagedZoneRuntime.I != null)
                GUI.Label(new Rect(32, Screen.height - 50, 465, 20), "Zones créées : " + ManagedZoneRuntime.I.Zones.Count);
        }
    }
}
