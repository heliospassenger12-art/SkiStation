using UnityEngine;

namespace SkiStation
{
    // Optional visual layer. No visitor, vehicle AI, economy or arcade behavior is changed.
    public sealed class StationParkingVisual247 : MonoBehaviour
    {
        private static StationParkingVisual247 instance;
        private StationGatewayRuntime gateway;
        private GameObject decor;
        private float nextCheck;
        private Material asphalt, marking, pavement, wood, glass, snow, dark, blue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            // Replaced by ArrivalHub250: physical parking linked to actual visitors.
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
                if (decor != null) Destroy(decor);
                Destroy(gameObject);
                return;
            }
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 2f;
            var current = StationGatewayRuntime.I;
            if (current == gateway && decor != null) return;
            if (decor != null) Destroy(decor);
            gateway = current;
            Build(VisualAnchor());
        }

        private static Material Make(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private float Ground(float x, float z) { return SkiStationGuaranteedMountainVisual.Height(x, z); }

        private GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material mat)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = size;
            var col = obj.GetComponent<Collider>();
            if (col != null) Destroy(col);
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        private void Tile(string name, float x, float z, float width, float depth, Material material)
        {
            // Short sections keep paved areas readable on gently varying terrain.
            for (float dx = -width / 2f + 4f; dx < width / 2f; dx += 8f)
                for (float dz = -depth / 2f + 4f; dz < depth / 2f; dz += 8f)
                {
                    float cx = x + dx, cz = z + dz;
                    Box(name, decor.transform, new Vector3(cx, Ground(cx, cz) + .12f, cz),
                        new Vector3(Mathf.Min(8f, width), .16f, Mathf.Min(8f, depth)), material);
                }
        }

        private void Build(Vector3 entry)
        {
            asphalt = Make(new Color(.19f, .21f, .24f));
            marking = Make(new Color(.93f, .92f, .83f));
            pavement = Make(new Color(.58f, .56f, .52f));
            wood = Make(new Color(.39f, .24f, .15f));
            glass = Make(new Color(.33f, .54f, .62f));
            snow = Make(new Color(.91f, .95f, .98f));
            dark = Make(new Color(.12f, .15f, .19f));
            blue = Make(new Color(.19f, .32f, .52f));
            decor = new GameObject("Parking_et_arret_navette_DECORATIFS");
            decor.transform.SetParent(transform, false);
            // Keep the existing central road and village corridor unobstructed.
            float px = entry.x + 155f, pz = entry.z + 58f;
            Tile("Parking_goudron", px, pz, 64f, 64f, asphalt);
            for (int row = 0; row < 2; row++)
                for (int slot = 0; slot < 5; slot++)
                {
                    float x = px - 24f + slot * 12f;
                    float z = pz - 16f + row * 30f;
                    float y = Ground(x, z) + .25f;
                    Box("Marquage_place", decor.transform, new Vector3(x - 5f, y, z), new Vector3(.18f, .08f, 12f), marking);
                    if ((slot + row) % 3 == 0) Car(x, z, (slot + row) % 2 == 0 ? blue : snow);
                }
            float stopX = entry.x + 34f, stopZ = entry.z + 20f;
            Tile("Quai_navette", stopX, stopZ, 12f, 20f, pavement);
            float sy = Ground(stopX, stopZ);
            Box("Abri_montants", decor.transform, new Vector3(stopX, sy + 2.1f, stopZ), new Vector3(8f, 4f, .3f), glass);
            Box("Abri_toit", decor.transform, new Vector3(stopX, sy + 4.2f, stopZ), new Vector3(10f, .35f, 5f), wood);
            Box("Banc", decor.transform, new Vector3(stopX, sy + .9f, stopZ - 1f), new Vector3(5f, .35f, 1f), wood);
            // Decorative pedestrian link beside the village's central corridor.
            Tile("Chemin_pieton_vers_village", entry.x + 24f, entry.z + 66f, 6f, 65f, pavement);
            Debug.Log("SkiStation 2.47: parking et arret navette decoratifs crees; aucun spawn visiteur modifie.");
        }

        private void Car(float x, float z, Material paint)
        {
            float y = Ground(x, z);
            Box("Voiture_carrosserie", decor.transform, new Vector3(x, y + 1.0f, z), new Vector3(3.1f, 1.1f, 5.5f), paint);
            Box("Voiture_vitres", decor.transform, new Vector3(x, y + 1.85f, z + .15f), new Vector3(2.7f, .8f, 2.8f), glass);
            for (int side = -1; side <= 1; side += 2)
                for (int axle = -1; axle <= 1; axle += 2)
                    Box("Roue", decor.transform, new Vector3(x + side * 1.5f, y + .45f, z + axle * 1.8f), new Vector3(.35f, .9f, .9f), dark);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (decor != null) Destroy(decor);
            foreach (var mat in new[] { asphalt, marking, pavement, wood, glass, snow, dark, blue })
                if (mat != null) Destroy(mat);
        }
    }
}
