using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public sealed class ManagedZoneConfirmationRuntime : MonoBehaviour
    {
        public static ManagedZoneConfirmationRuntime I { get; private set; }
        public bool Pending { get; private set; }
        public ManagedZoneType ZoneType { get; private set; }
        public float AreaM2 { get; private set; }
        public float PerimeterM { get; private set; }
        public int PointCount { get; private set; }
        private readonly List<Vector3> pendingPoints = new List<Vector3>();

        private void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; }

        public void Prepare(ManagedZoneType type, IList<Vector3> points)
        {
            pendingPoints.Clear();
            if (points != null) for (int i = 0; i < points.Count; i++) pendingPoints.Add(points[i]);
            ZoneType = type; PointCount = pendingPoints.Count;
            PerimeterM = 0f; AreaM2 = 0f;
            for (int i = 0; i < pendingPoints.Count; i++)
            {
                Vector3 a = pendingPoints[i], b = pendingPoints[(i + 1) % pendingPoints.Count];
                PerimeterM += Vector3.Distance(a, b);
                AreaM2 += a.x * b.z - b.x * a.z;
            }
            AreaM2 = Mathf.Abs(AreaM2) * .5f;
            Pending = pendingPoints.Count >= 3;
        }

        public bool Confirm()
        {
            if (!Pending || ManagedZoneRuntime.I == null) return false;
            ManagedZoneRuntime.I.AddZone(ZoneType, pendingPoints);
            Clear(); return true;
        }
        public void Cancel() { Clear(); }
        private void Clear() { Pending = false; pendingPoints.Clear(); }

        private void OnGUI()
        {
            if (!Pending) return;
            float w = 430f, h = 245f, x = (Screen.width-w)*.5f, y = (Screen.height-h)*.5f;
            GUI.Box(new Rect(x,y,w,h), "RECAPITULATIF — ZONE");
            GUI.Label(new Rect(x+22,y+38,w-44,24), "Type : " + ZoneType);
            GUI.Label(new Rect(x+22,y+68,w-44,24), "Surface : " + Mathf.RoundToInt(AreaM2).ToString("N0") + " m²");
            GUI.Label(new Rect(x+22,y+98,w-44,24), "Perimetre : " + Mathf.RoundToInt(PerimeterM).ToString("N0") + " m");
            GUI.Label(new Rect(x+22,y+128,w-44,24), "Points de contour : " + PointCount);
            if (GUI.Button(new Rect(x+22,y+178,120,38), "CONFIRMER")) Confirm();
            if (GUI.Button(new Rect(x+155,y+178,120,38), "MODIFIER")) { Pending = false; }
            if (GUI.Button(new Rect(x+288,y+178,120,38), "ANNULER")) Cancel();
        }
    }

    public static class ManagedZoneConfirmationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;

            if (Object.FindFirstObjectByType<ManagedZoneConfirmationRuntime>() != null) return;
            new GameObject("Managed Zone Confirmation 2.02").AddComponent<ManagedZoneConfirmationRuntime>();
        }
    }
}
