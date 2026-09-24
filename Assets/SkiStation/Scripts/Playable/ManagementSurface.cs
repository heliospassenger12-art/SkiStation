using UnityEngine;

namespace SkiStation
{
    // Query the rendered terrain directly; trees, piste meshes and visitors cannot mask it.
    public static class ManagementSurface
    {
        private static Collider massif;
        private static Collider apron;

        public static bool TryGet(Vector3 point, out Vector3 surface)
        {
            surface = point;
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return false;
            if (massif == null) massif = GameObject.Find("SkiStation_MASSIF_01")?.GetComponent<Collider>();
            if (apron == null) apron = GameObject.Find("SkiStation_FRONT_DE_NEIGE")?.GetComponent<Collider>();
            Ray ray = new Ray(new Vector3(point.x, 10000f, point.z), Vector3.down);
            bool found = false;
            if (massif != null && massif.Raycast(ray, out var hit, 20000f)) { surface = hit.point; found = true; }
            if (apron != null && apron.Raycast(ray, out var baseHit, 20000f) && (!found || baseHit.point.y > surface.y))
            { surface = baseHit.point; found = true; }
            if (!found && MountainRuntime.I != null)
            {
                var terrain = MountainRuntime.I.GetComponent<Collider>();
                if (terrain != null && terrain.Raycast(ray, out var fallback, 20000f)) { surface = fallback.point; found = true; }
            }
            return found;
        }

        public static Vector3 Entrance
        {
            get
            {
                if (StationRoadNetwork247.I != null && StationRoadNetwork247.I.IsReady) return StationRoadNetwork247.I.MainRoadEnd;
                Vector3 point = new Vector3(0f, 80f, -1670f);
                if (GameObject.Find("SkiStation_MASSIF_01") == null && MountainRuntime.I != null)
                    point = MountainRuntime.I.transform.TransformPoint(new Vector3(0f, 0f, -MountainRuntime.I.Definition.size * .4f));
                return TryGet(point, out var surface) ? surface : point;
            }
        }
    }
}
