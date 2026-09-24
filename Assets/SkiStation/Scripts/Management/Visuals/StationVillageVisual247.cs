using UnityEngine;

namespace SkiStation
{
    // Decorative prototype only: no visitor spawns, finances, construction or arcade scripts changed.
    public sealed class StationVillageVisual247 : MonoBehaviour
    {
        private static StationVillageVisual247 instance;
        private GameObject village;
        private StationGatewayRuntime gateway;
        private float nextCheck;
        private Material timber, stone, roof, snow, glass, dark, warm, path;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var root = new GameObject("SkiStation_247_Village_Visuel");
            instance = root.AddComponent<StationVillageVisual247>();
        }


        // The optional gameplay gateway is not instantiated in a fresh management scene.
        // Keep visual scenery available independently, without creating visitors or changing arcade.
        private static Vector3 VisualAnchor()
        {
            var point = StationGatewayRuntime.I != null ? StationGatewayRuntime.I.GatewayArrivalPoint : null;
            if (point != null) return point.position;
            const float x = 0f, z = -2600f;
            return new Vector3(x, SkiStationGuaranteedMountainVisual.Height(x, z), z);
        }

        private void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive)
            {
                if (village != null) Destroy(village);
                Destroy(gameObject);
                return;
            }
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 2f;
            var current = StationGatewayRuntime.I;
            if (current == gateway && village != null) return;
            if (village != null) Destroy(village);
            gateway = current;
            Build(VisualAnchor());
        }

        private static Material Mat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader);
            material.color = color;
            return material;
        }

        private GameObject Cube(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            if (rotation.HasValue) go.transform.localRotation = rotation.Value;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private float Ground(float x, float z) => SkiStationGuaranteedMountainVisual.Height(x, z);

        private void Build(Vector3 arrival)
        {
            timber = Mat(new Color(.42f, .25f, .15f));
            stone = Mat(new Color(.48f, .45f, .42f));
            roof = Mat(new Color(.24f, .27f, .29f));
            snow = Mat(new Color(.91f, .94f, .97f));
            glass = Mat(new Color(.30f, .52f, .61f));
            dark = Mat(new Color(.17f, .12f, .10f));
            warm = Mat(new Color(.95f, .72f, .36f));
            path = Mat(new Color(.67f, .65f, .61f));
            village = new GameObject("Village_front_de_neige_DECORATIF");
            village.transform.SetParent(transform, false);
            // Existing gateway is the only anchor; keep a clear central access corridor.
            float x = arrival.x;
            float z = arrival.z;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 3; row++)
                {
                    float bx = x + side * (42f + row * 5f);
                    float bz = z + 36f + row * 42f;
                    Chalet("Chalet_" + side + "_" + row, bx, bz, row == 0 ? 18f : 15f, row == 0 ? 14f : 12f, side);
                }
            }
            Chalet("Restaurant_alpin", x - 100f, z + 108f, 26f, 18f, 1);
            Chalet("Location_de_skis", x + 100f, z + 108f, 25f, 18f, -1);
            Chalet("Ecole_de_ski", x - 95f, z + 160f, 22f, 15f, 1);
            // Visual-only plaza: no traffic/pathfinding logic attached.
            float py = Ground(x, z + 135f);
            Cube("Place_pietonne", village.transform, new Vector3(x, py + .12f, z + 135f), new Vector3(30f, .18f, 25f), path);
            Debug.Log("SkiStation 2.47: village decoratif cree au portail existant. Aucun visiteur ni script arcade modifie.");
        }

        private void Chalet(string label, float x, float z, float width, float depth, int facing)
        {
            var root = new GameObject(label);
            root.transform.SetParent(village.transform, false);
            root.transform.position = new Vector3(x, Ground(x, z) + .5f, z);
            float height = width > 23f ? 12f : 9f;
            Cube("Fondation_pierre", root.transform, new Vector3(0, 1f, 0), new Vector3(width + 1f, 2f, depth + 1f), stone);
            Cube("Facade_bois", root.transform, new Vector3(0, 2f + height / 2f, 0), new Vector3(width, height, depth), timber);
            Cube("Toit_pente_gauche", root.transform, new Vector3(-width * .24f, height + 3.7f, 0), new Vector3(width * .60f, .65f, depth + 3f), roof, Quaternion.Euler(0, 0, 32));
            Cube("Toit_pente_droite", root.transform, new Vector3(width * .24f, height + 3.7f, 0), new Vector3(width * .60f, .65f, depth + 3f), roof, Quaternion.Euler(0, 0, -32));
            Cube("Neige_toit_gauche", root.transform, new Vector3(-width * .24f, height + 4.12f, 0), new Vector3(width * .60f, .15f, depth + 3.2f), snow, Quaternion.Euler(0, 0, 32));
            Cube("Neige_toit_droite", root.transform, new Vector3(width * .24f, height + 4.12f, 0), new Vector3(width * .60f, .15f, depth + 3.2f), snow, Quaternion.Euler(0, 0, -32));
            for (int i = -1; i <= 1; i++)
            {
                if (i == 0) continue;
                Cube("Vitrine", root.transform, new Vector3(i * width * .27f, 5.3f, facing * (depth / 2f + .06f)), new Vector3(width * .17f, 3.3f, .12f), glass);
                Cube("Lumiere_interieure", root.transform, new Vector3(i * width * .27f, 4.2f, facing * (depth / 2f + .16f)), new Vector3(width * .12f, .28f, .12f), warm);
            }
            Cube("Porte", root.transform, new Vector3(0, 3.8f, facing * (depth / 2f + .1f)), new Vector3(2.6f, 4.8f, .2f), dark);
            Cube("Balcon", root.transform, new Vector3(0, height * .72f, facing * (depth / 2f + 1.2f)), new Vector3(width * .65f, .3f, 2.3f), stone);
            for (int i = -3; i <= 3; i++)
                Cube("Garde_corps", root.transform, new Vector3(i * width * .09f, height * .72f + .85f, facing * (depth / 2f + 2.2f)), new Vector3(.22f, 1.6f, .22f), timber);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (village != null) Destroy(village);
            foreach (var material in new[] { timber, stone, roof, snow, glass, dark, warm, path })
                if (material != null) Destroy(material);
        }
    }
}
