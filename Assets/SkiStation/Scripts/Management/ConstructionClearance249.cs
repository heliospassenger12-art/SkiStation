using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    // Trees are removed only after the player confirms a construction.
    public static class ConstructionClearance249
    {
        public static void ClearTrees(IReadOnlyList<Vector3> route, float halfWidth)
        {
            if (route == null || route.Count == 0) return;
            // The project uses procedural GameObject trees. Avoid UnityEngine.Terrain types:
            // the optional Terrain module is not installed in this project's manifest.
            // Procedural trees may be ordinary GameObjects rather than Terrain tree instances.
            var trees = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var pinePositions = new List<Vector3>();
            foreach (Transform t in trees)
            {
                if (t == null || t.name != "PineTrunk") continue;
                Vector3 basePosition = t.position;
                basePosition.y = 0f;
                if (IsInsideRoute(basePosition, route, halfWidth)) pinePositions.Add(basePosition);
            }
            foreach (Transform t in trees)
            {
                if (t == null || t.parent == null) continue;
                string name = t.name.ToLowerInvariant();
                // StarterDomainPresentation creates one parent per tree ("Mélèze").
                bool wholeTree = name == "mélèze" || name.StartsWith("tree_") ||
                                 name.StartsWith("arbre_") || name.StartsWith("sapin_");
                if (wholeTree && IsInsideRoute(t.position, route, halfWidth))
                {
                    Object.Destroy(t.gameObject);
                    continue;
                }
                // SkiStationGuaranteedMountainVisual creates separate trunk and crown
                // objects. Remove both at the same horizontal position.
                if (name != "pinetrunk" && name != "pinecrown") continue;
                Vector3 p = t.position;
                p.y = 0f;
                for (int i = 0; i < pinePositions.Count; i++)
                {
                    if ((p - pinePositions[i]).sqrMagnitude > 0.01f) continue;
                    Object.Destroy(t.gameObject);
                    break;
                }
            }
        }

        private static bool IsInsideRoute(Vector3 point, IReadOnlyList<Vector3> route, float halfWidth)
        {
            point.y = 0f;
            for (int i = 0; i < route.Count; i++)
            {
                Vector3 a = route[i]; a.y = 0f;
                Vector3 b = route[Mathf.Min(i + 1, route.Count - 1)]; b.y = 0f;
                Vector3 ab = b - a;
                float f = ab.sqrMagnitude < .0001f ? 0f : Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude);
                if ((point - (a + f * ab)).sqrMagnitude <= halfWidth * halfWidth) return true;
            }
            return false;
        }

        public static bool ConflictsWithSurfaceLift(IReadOnlyList<Vector3> piste, float halfWidth, out string reason)
        {
            reason = null;
            if (piste == null || piste.Count < 2) return false;
            foreach (LiftRuntime lift in Object.FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None))
            {
                if (lift == null || lift.Definition == null || lift.IsUnderground || lift.Route == null || lift.Route.Count < 2) continue;
                LiftType type = lift.Definition.type;
                if (type != LiftType.Platter && type != LiftType.TBar && type != LiftType.Fork && type != LiftType.MagicCarpet) continue;
                for (int i = 1; i < piste.Count; i++)
                    for (int j = 1; j < lift.Route.Count; j++)
                        if (SegmentsNearXZ(piste[i-1], piste[i], lift.Route[j-1], lift.Route[j], halfWidth + 2f))
                        { reason = "Croisement interdit avec un teleski ou tapis au sol"; return true; }
            }
            return false;
        }

        private static bool SegmentsNearXZ(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float distance)
        {
            Vector2 p = new Vector2(a.x, a.z), q = new Vector2(b.x, b.z);
            Vector2 r = new Vector2(c.x, c.z), s = new Vector2(d.x, d.z);
            Vector2 u = q - p, v = s - r, w = p - r;
            float aa = Vector2.Dot(u,u), bb = Vector2.Dot(u,v), cc = Vector2.Dot(v,v);
            float dd = Vector2.Dot(u,w), ee = Vector2.Dot(v,w), denom = aa * cc - bb * bb;
            float x = denom < .0001f ? 0f : Mathf.Clamp01((bb * ee - cc * dd) / denom);
            float y = cc < .0001f ? 0f : Mathf.Clamp01((bb * x + ee) / cc);
            x = aa < .0001f ? 0f : Mathf.Clamp01((bb * y - dd) / aa);
            return ((p + x * u) - (r + y * v)).sqrMagnitude <= distance * distance;
        }
    }
}
