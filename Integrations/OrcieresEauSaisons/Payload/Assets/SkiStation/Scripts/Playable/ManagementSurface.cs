using UnityEngine;

namespace SkiStation
{
    // Query the rendered terrain directly; trees, piste meshes and visitors cannot mask it.
    public static class ManagementSurface
    {
        private static Collider massif;
        private static Collider apron;

        public static void InvalidateCheckpoint261() { massif = null; apron = null; }

        private static void Refresh349()
        {
            if (massif == null) massif = GameObject.Find("SkiStation_MASSIF_01")?.GetComponent<Collider>();
            if (apron == null) apron = GameObject.Find("SkiStation_FRONT_DE_NEIGE")?.GetComponent<Collider>();
        }

        public static bool TryRaycast349(Ray ray, out Vector3 surface)
        {
            surface = default;
            if(TerrainGeneration.TerrainGenerationWorld.TryRaycast(ray,out surface))return true;
            if(TerrainGeneration.TerrainGenerationWorld.HasCurrent)return false;
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return false;
            Refresh349();
            bool found = false; float closest = 30000f;
            if (massif != null && massif.enabled && massif.gameObject.activeInHierarchy && massif.Raycast(ray, out var hit, closest))
            { surface = hit.point; closest = hit.distance; found = true; }
            if (apron != null && apron.enabled && apron.gameObject.activeInHierarchy && apron.Raycast(ray, out var baseHit, closest))
            { surface = baseHit.point; found = true; }
            // Never place on a different, hidden mountain when the visible massif exists.
            if (!found && GameObject.Find("SkiStation_MASSIF_01") == null && MountainRuntime.I != null)
            {
                var legacy = MountainRuntime.I.GetComponent<Collider>();
                if (legacy != null && legacy.enabled && legacy.Raycast(ray, out var fallback, closest))
                { surface = fallback.point; found = true; }
            }
            return found;
        }

        public static bool TryGet(Vector3 point, out Vector3 surface)
        {
            surface = point;
            Ray ray = new Ray(new Vector3(point.x, 10000f, point.z), Vector3.down);
            return TryRaycast349(ray, out surface);
        }

        // Construction in an isolated Arcade scene must query its own real ground,
        // never the hidden management mountain or unrelated scenery colliders.
        public static bool TryGet(Vector3 point, out Vector3 surface, Component owner)
        {
            var arcade = owner != null ? owner.GetComponentInParent<SkiStation.Arcade.ArcadeAlpineTerrain>() : null;
            if (arcade == null) return TryGet(point, out surface);
            Ray ray = new Ray(new Vector3(point.x, 10000f, point.z), Vector3.down);
            return arcade.TryRaycastSurface(ray, out surface);
        }

        public static bool TryTrail(Vector3 point,out Vector3 surface,Component owner=null)
        {
            bool native = TryGet(point,out surface,owner);
            if (owner != null && owner.GetComponentInParent<SkiStation.Arcade.ArcadeAlpineTerrain>() != null) return native;
            var ray = new Ray(new Vector3(point.x,10000f,point.z),Vector3.down);
            if (!TerrainGeneration.WaterCrossing.TrySkiRay(ray,out var deck,out _) || native && deck.y<=surface.y) return native;
            surface=deck;
            return true;
        }
        public static Vector3 Entrance
        {
            get
            {
                if(TerrainGeneration.TerrainGenerationWorld.Current?.FrontDeNeige!=null)return TerrainGeneration.TerrainGenerationWorld.Current.FrontDeNeige.position;
                if (StationRoadNetwork247.I != null && StationRoadNetwork247.I.IsReady) return StationRoadNetwork247.I.MainRoadEnd;
                Vector3 point = new Vector3(0f, 80f, -1670f);
                if (GameObject.Find("SkiStation_MASSIF_01") == null && MountainRuntime.I != null)
                    point = MountainRuntime.I.transform.TransformPoint(new Vector3(0f, 0f, -MountainRuntime.I.Definition.size * .4f));
                return TryGet(point, out var surface) ? surface : point;
            }
        }
    }
}
