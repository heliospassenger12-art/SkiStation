using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    // Management-only visual footpaths. Does not change the road or arcade navigation.
    public static class StationPedestrianAccess249
    {
        public static bool TryPlan(StationRoadNetwork247 road, Vector3 building, float buildingDepth,
            out Vector3 entrance, out Vector3 roadside, out string reason)
        {
            entrance = roadside = Vector3.zero;
            reason = "";
            if (road == null || !road.IsReady) { reason = "Reseau routier indisponible"; return false; }
            Vector3 closest = Vector3.zero;
            float best = float.MaxValue;
            CheckPath(road.GetPermanentRoadPath(), building, ref closest, ref best);
            foreach (var branch in road.Branches) CheckPath(branch, building, ref closest, ref best);
            if (best > buildingDepth + 55f) { reason = "Aucun trottoir a proximite"; return false; }
            Vector3 direction = new Vector3(closest.x - building.x, 0f, closest.z - building.z);
            if (direction.sqrMagnitude < .01f) { reason = "Entree du batiment sur la route"; return false; }
            direction.Normalize();
            entrance = building + direction * (buildingDepth * .5f + 1.5f);
            roadside = closest - direction * 8.5f; // Roadside sidewalk: main and branch roads.
            entrance.y = SkiStationGuaranteedMountainVisual.Height(entrance.x, entrance.z);
            roadside.y = SkiStationGuaranteedMountainVisual.Height(roadside.x, roadside.z);
            if (Vector3.Distance(entrance, roadside) > 65f) { reason = "Chemin pieton trop long"; return false; }
            for (int i = 0; i <= 12; i++)
            {
                Vector3 p = Vector3.Lerp(entrance, roadside, i / 12f);
                float h = SkiStationGuaranteedMountainVisual.Height(p.x, p.z);
                if (i > 0)
                {
                    Vector3 prev = Vector3.Lerp(entrance, roadside, (i - 1) / 12f);
                    float prevH = SkiStationGuaranteedMountainVisual.Height(prev.x, prev.z);
                    float horizontal = Vector2.Distance(new Vector2(p.x,p.z), new Vector2(prev.x,prev.z));
                    if (Mathf.Abs(h - prevH) > horizontal * .35f) { reason = "Terrain trop pentu pour un chemin pieton"; return false; }
                }
            }
            return true;
        }
        private static void CheckPath(IList<Vector3> path, Vector3 point, ref Vector3 closest, ref float best)
        {
            if (path == null) return;
            Vector2 p = new Vector2(point.x, point.z);
            for (int i = 1; i < path.Count; i++)
            {
                Vector2 a = new Vector2(path[i-1].x, path[i-1].z);
                Vector2 b = new Vector2(path[i].x, path[i].z);
                Vector2 ab = b-a;
                float t = Mathf.Clamp01(Vector2.Dot(p-a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
                Vector2 q = a + ab*t;
                float distance = Vector2.Distance(p,q);
                if (distance < best) { best = distance; closest = new Vector3(q.x, 0f, q.y); }
            }
        }
        public static GameObject CreateVisual(Vector3 entrance, Vector3 roadside, Material material, string label)
        {
            var root = new GameObject(label);
            const int steps = 12;
            for (int i = 0; i < steps; i++)
            {
                Vector3 a = Vector3.Lerp(entrance, roadside, i/(float)steps);
                Vector3 b = Vector3.Lerp(entrance, roadside, (i+1)/(float)steps);
                a.y = SkiStationGuaranteedMountainVisual.Height(a.x,a.z) + .2f;
                b.y = SkiStationGuaranteedMountainVisual.Height(b.x,b.z) + .2f;
                Vector3 flat = new Vector3(b.x-a.x,0f,b.z-a.z);
                if (flat.sqrMagnitude < .0001f) continue;
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = "Chemin_pieton";
                segment.transform.SetParent(root.transform, false);
                segment.transform.position = (a+b)*.5f;
                segment.transform.rotation = Quaternion.LookRotation(flat.normalized,Vector3.up);
                segment.transform.localScale = new Vector3(2.4f,.12f,flat.magnitude+.12f);
                var collider = segment.GetComponent<Collider>(); if (collider != null) Object.Destroy(collider);
                segment.GetComponent<Renderer>().sharedMaterial = material;
            }
            return root;
        }
    }
}
