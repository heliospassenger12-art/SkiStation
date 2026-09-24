using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Automatic piste safety infrastructure. It is deliberately non-micromanaged:
    /// building a piste automatically provides realistic boundary markers, a difficulty
    /// sign at the top and closure barriers when the piste is closed.
    /// </summary>
    public sealed class SlopeSafetySignageRuntime : MonoBehaviour
    {
        [SerializeField] private float markerSpacing = 22f;
        [SerializeField] private float sideOffset = 0.8f;
        [SerializeField] private float markerHeight = 1.25f;

        public bool HasCompleteSignage { get; private set; }
        public int MarkerCount { get; private set; }

        private SlopeRuntime slope;
        private readonly List<GameObject> spawned = new();
        private bool lastOpen = true;

        public void Build(SlopeRuntime target)
        {
            slope = target;
            Rebuild();
        }

        private void Awake()
        {
            slope = GetComponent<SlopeRuntime>();
            if (slope != null) Rebuild();
        }

        private void Update()
        {
            if (slope == null) return;
            if (slope.Open != lastOpen) RefreshClosureState();
        }

        public void Rebuild()
        {
            Clear();
            if (slope == null || slope.Points.Count < 2) return;

            CreateTopDifficultySign();
            CreateBoundaryMarkers();
            HasCompleteSignage = true;
            lastOpen = slope.Open;
            RefreshClosureState();
        }

        private void CreateTopDifficultySign()
        {
            Vector3 p = slope.TopPoint;
            Vector3 tangent = slope.Points.Count > 1 ? slope.Points[1] - slope.Points[0] : Vector3.forward;
            tangent.y = 0f;
            if (tangent.sqrMagnitude < .01f) tangent = Vector3.forward;
            tangent.Normalize();
            SpawnPole(p + Vector3.up * 0.02f, 1.8f, "DifficultySign");
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = $"PisteSign_{slope.Difficulty}";
            sign.transform.position = p + Vector3.up * 2.05f;
            sign.transform.localScale = new Vector3(.75f, .42f, .08f);
            sign.transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            spawned.Add(sign);
        }

        private void CreateBoundaryMarkers()
        {
            float accumulated = 0f;
            for (int i = 1; i < slope.Points.Count; i++)
            {
                Vector3 a = slope.Points[i - 1];
                Vector3 b = slope.Points[i];
                float segment = Vector3.Distance(a, b);
                if (segment < .01f) continue;
                Vector3 tangent = (b - a).normalized;
                tangent.y = 0f;
                if (tangent.sqrMagnitude < .01f) tangent = Vector3.forward;
                tangent.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                float cursor = markerSpacing - accumulated;
                while (cursor <= segment)
                {
                    Vector3 p = Vector3.Lerp(a, b, cursor / segment);
                    float halfWidth = Mathf.Max(1f, slope.GetWidthAt(i) * .5f);
                    SpawnMarker(p + side * (halfWidth + sideOffset));
                    SpawnMarker(p - side * (halfWidth + sideOffset));
                    cursor += markerSpacing;
                }
                accumulated = (accumulated + segment) % markerSpacing;
            }
            MarkerCount = spawned.Count;
        }

        private void SpawnMarker(Vector3 p)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "PisteBoundaryMarker";
            marker.transform.position = p + Vector3.up * (markerHeight * .5f);
            marker.transform.localScale = new Vector3(.10f, markerHeight, .10f);
            spawned.Add(marker);
        }

        private void SpawnPole(Vector3 p, float height, string name)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = name + "Pole";
            pole.transform.position = p + Vector3.up * height * .5f;
            pole.transform.localScale = new Vector3(.055f, height * .5f, .055f);
            spawned.Add(pole);
        }

        private void RefreshClosureState()
        {
            lastOpen = slope.Open;
            // Closure equipment is intentionally automatic and visual only.
            for (int i = 0; i < spawned.Count; i++)
            {
                var go = spawned[i];
                if (go != null && go.name.StartsWith("ClosureBarrier")) go.SetActive(!slope.Open);
            }
            if (!slope.Open) CreateClosureBarriers();
        }

        private void CreateClosureBarriers()
        {
            if (HasClosureBarrier()) return;
            Vector3 p = slope.BottomPoint;
            Vector3 tangent = slope.Points[^2] - slope.Points[^1];
            tangent.y = 0f;
            if (tangent.sqrMagnitude < .01f) tangent = Vector3.forward;
            tangent.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
            float halfWidth = Mathf.Max(2f, slope.GetWidthAt(slope.Points.Count - 1) * .5f);
            for (int s = -1; s <= 1; s += 2)
            {
                var barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barrier.name = "ClosureBarrier";
                barrier.transform.position = p + side * (halfWidth * .65f * s) + Vector3.up * .65f;
                barrier.transform.localScale = new Vector3(.15f, 1.3f, Mathf.Max(2f, halfWidth * .55f));
                barrier.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg, 0f);
                spawned.Add(barrier);
            }
        }

        private bool HasClosureBarrier()
        {
            for (int i = 0; i < spawned.Count; i++) if (spawned[i] != null && spawned[i].name.StartsWith("ClosureBarrier")) return true;
            return false;
        }

        private void Clear()
        {
            for (int i = spawned.Count - 1; i >= 0; i--) if (spawned[i] != null) Destroy(spawned[i]);
            spawned.Clear();
            MarkerCount = 0;
            HasCompleteSignage = false;
        }

        public void SetOpen(bool open)
        {
            if (slope != null) slope.SetOpen(open);
            RefreshClosureState();
        }
    }
}
