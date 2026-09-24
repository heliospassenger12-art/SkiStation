using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>F3: draws a dedicated Piou-Piou physical zone. Minimal management, no manual child access control.</summary>
    public sealed class PiouPiouConstructionMode : MonoBehaviour
    {
        private readonly List<Vector3> points = new();
        private bool drawing;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3)) { drawing = !drawing; points.Clear(); }
            if (!drawing) return;
            if (Input.GetMouseButtonDown(0) && TryGround(Input.mousePosition, out var p)) points.Add(p);
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)) if (points.Count > 0) points.RemoveAt(points.Count - 1);
            if (Input.GetKeyDown(KeyCode.Return)) { if (points.Count >= 3 && PiouPiouZoneRuntime.I != null) PiouPiouZoneRuntime.I.AddZone(points); points.Clear(); drawing = false; }
            if (Input.GetKeyDown(KeyCode.Escape)) { points.Clear(); drawing = false; }
        }
        private bool TryGround(Vector3 screen, out Vector3 point)
        {
            point = default; var cam = Camera.main; if (cam == null) return false;
            Ray ray = cam.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out var hit, 2000f, ~0, QueryTriggerInteraction.Ignore)) { point = hit.point; return true; }
            return false;
        }
        private void OnGUI()
        {
            if (!drawing) return;
            GUI.Box(new Rect(10, Screen.height - 115, 360, 95), "Jardin des Piou-Piou — construction");
            GUI.Label(new Rect(20, Screen.height - 88, 340, 22), "Clic gauche : points | Entrée : créer");
            GUI.Label(new Rect(20, Screen.height - 66, 340, 22), "Suppr/Retour arrière : dernier point | Échap : annuler");
            GUI.Label(new Rect(20, Screen.height - 44, 340, 22), "F3 : activer/désactiver");
        }
    }
}
