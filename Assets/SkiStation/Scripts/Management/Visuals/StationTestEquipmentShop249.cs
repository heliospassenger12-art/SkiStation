using UnityEngine;

namespace SkiStation
{
    // One functional equipment shop for the management test scene, not a visitor spawn.
    // Does not alter the permanent road or create shops in arcade scenes.
    public sealed class StationTestEquipmentShop249 : MonoBehaviour
    {
        private static StationTestEquipmentShop249 instance;
        private GameObject shop;
        private float nextAttempt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            instance = new GameObject("SkiStation_249_Magasin_Test").AddComponent<StationTestEquipmentShop249>();
        }

        private void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (shop != null || Time.unscaledTime < nextAttempt) return;
            nextAttempt = Time.unscaledTime + 2f;
            StationRoadNetwork247 road = StationRoadNetwork247.I;
            if (road == null || !road.IsReady) return;
            Vector3[] path = road.GetPermanentRoadPath();
            Vector3 end = road.MainRoadEnd;
            Vector3 forward = path[path.Length - 1] - path[path.Length - 2];
            forward.y = 0f;
            if (forward.sqrMagnitude < .01f) return;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            // Search close to the front de neige, on either side of the road.
            float[] distances = { 36f, 44f, 52f, 60f };
            float[] along = { -28f, -48f, -8f, -68f, 12f };
            for (int a = 0; a < along.Length; a++)
            for (int d = 0; d < distances.Length; d++)
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 center = end + forward * along[a] + right * (side * distances[d]);
                center.y = SkiStationGuaranteedMountainVisual.Height(center.x, center.z);
                if (!road.CanPlaceBuilding(center, 20f)) continue;
                Vector3 entrance, roadside;
                string reason;
                if (!StationPedestrianAccess249.TryPlan(road, center, 16f, out entrance, out roadside, out reason)) continue;
                Build(center, entrance, roadside);
                return;
            }
            Debug.LogWarning("SkiStation 2.49 : aucun emplacement accessible pour le magasin de test. Nouvelle tentative dans 2 s.");
        }

        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.color = color;
            return material;
        }

        private static void Box(Transform parent, string name, Vector3 offset, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = offset;
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void Build(Vector3 center, Vector3 entrance, Vector3 roadside)
        {
            shop = new GameObject("Magasin_sports_hiver_TEST_unique");
            shop.transform.position = center;
            Material wall = MakeMaterial(new Color(.49f, .31f, .19f));
            Material roof = MakeMaterial(new Color(.23f, .28f, .32f));
            Box(shop.transform, "Facade", new Vector3(0f, 5.5f, 0f), new Vector3(20f, 11f, 16f), wall);
            Box(shop.transform, "Toit", new Vector3(0f, 11.8f, 0f), new Vector3(22f, 1.6f, 18f), roof);
            shop.AddComponent<StationStructureStatus249>();
            shop.AddComponent<StationEquipmentShop249>(); // Five departments, unlimited stock, unmanaged prices.
            GameObject access = StationPedestrianAccess249.CreateVisual(entrance, roadside,
                MakeMaterial(new Color(.67f, .63f, .55f)), "Chemin_magasin_test");
            StationPedestrianNetwork249.GetOrCreate().Register(shop, access, entrance, roadside);
            Debug.Log("SkiStation 2.49 : magasin de test unique ouvert et raccorde au chemin pieton.");
        }

        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
