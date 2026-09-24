using UnityEngine;

namespace SkiStation
{
    // Optional decorative layer for the 2.47 management scene. No gameplay or arcade scripts are changed.
    public sealed class StationPlazaDetails247 : MonoBehaviour
    {
        private static StationPlazaDetails247 instance;
        private StationGatewayRuntime gateway;
        private GameObject decor;
        private float nextCheck;
        private Material wood, stone, snow, metal, glass, lightMat, green, sign, fabric;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            instance = new GameObject("SkiStation_247_Place_Details").AddComponent<StationPlazaDetails247>();
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
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 2f;
            StationGatewayRuntime current = StationGatewayRuntime.I;
            if (current == gateway && decor != null) return;
            if (decor != null) Destroy(decor);
            gateway = current;
            Build(VisualAnchor());
        }

        private static Material Make(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private float Ground(float x, float z) { return SkiStationGuaranteedMountainVisual.Height(x, z); }

        private GameObject Box(string label, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = label;
            obj.transform.SetParent(decor.transform, false);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.localScale = scale;
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        private void Box(string label, float x, float y, float z, float sx, float sy, float sz, Material material)
        {
            Box(label, new Vector3(x,y,z), new Vector3(sx,sy,sz), material, Quaternion.identity);
        }

        private void Build(Vector3 entry)
        {
            wood = Make(new Color(.42f,.25f,.15f));
            stone = Make(new Color(.53f,.52f,.50f));
            snow = Make(new Color(.92f,.96f,.99f));
            metal = Make(new Color(.16f,.19f,.23f));
            glass = Make(new Color(.38f,.60f,.68f));
            lightMat = Make(new Color(1f,.79f,.42f));
            green = Make(new Color(.12f,.30f,.23f));
            sign = Make(new Color(.19f,.29f,.38f));
            fabric = Make(new Color(.68f,.22f,.17f));
            decor = new GameObject("Place_centrale_details_DECORATIFS");
            decor.transform.SetParent(transform, false);
            float x = entry.x, z = entry.z + 135f;
            // Keep the central road / pedestrian axis free. Details sit around the existing village plaza.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float bx = x + side * 22f;
                    float bz = z - 10f + i * 10f;
                    Bench(bx, bz);
                    Lamp(x + side * 29f, bz);
                }
                Terrace(x + side * 37f, z + 18f);
                Sign(x + side * 18f, z + 26f, side);
                Tree(x + side * 34f, z - 25f);
            }
            Debug.Log("SkiStation 2.47: mobilier de place centrale decoratif cree. Aucun spawn, economie ou script arcade modifie.");
        }

        private void Bench(float x, float z)
        {
            float y = Ground(x,z);
            Box("Banc_assise",x,y+.65f,z,3.5f,.22f,.9f,wood);
            Box("Banc_dossier",x,y+1.22f,z+.38f,3.5f,.9f,.17f,wood);
            Box("Banc_pied_gauche",x-1.3f,y+.32f,z,.16f,.64f,.7f,metal);
            Box("Banc_pied_droit",x+1.3f,y+.32f,z,.16f,.64f,.7f,metal);
        }

        private void Lamp(float x, float z)
        {
            float y = Ground(x,z);
            Box("Lampadaire_pied",x,y+2.5f,z,.18f,5f,.18f,metal);
            Box("Lampadaire_lanterne",x,y+5.1f,z,.72f,.65f,.72f,lightMat);
            Box("Lampadaire_chapeau",x,y+5.55f,z,1.05f,.18f,1.05f,metal);
        }

        private void Terrace(float x, float z)
        {
            float y = Ground(x,z);
            Box("Terrasse_plancher",x,y+.14f,z,12f,.25f,11f,wood);
            for (int i = -1; i <= 1; i++)
            {
                float tx = x + i*3.5f;
                Box("Table_terrasse",tx,y+1f,z,1.5f,.15f,1.5f,stone);
                Box("Pied_table",tx,y+.55f,z,.13f,.9f,.13f,metal);
                Box("Parasol_mat",tx,y+2.2f,z,.10f,2.4f,.10f,metal);
                Box("Parasol_toile",new Vector3(tx,y+3.4f,z),new Vector3(3.1f,.14f,3.1f),fabric,Quaternion.Euler(0,0,0));
            }
            Box("Terrasse_bordure_neige",x,y+.33f,z+5.45f,12f,.22f,.3f,snow);
        }

        private void Sign(float x, float z, int side)
        {
            float y = Ground(x,z);
            Box("Panneau_direction_poteau",x,y+1.65f,z,.18f,3.3f,.18f,wood);
            Box("Panneau_direction_front_de_neige",x+side*.75f,y+2.6f,z,1.8f,.48f,.13f,sign);
            Box("Panneau_direction_commerces",x-side*.75f,y+1.95f,z,1.8f,.48f,.13f,sign);
            // Signage is visual only; readable text can be added after in-Unity scale validation.
        }

        private void Tree(float x, float z)
        {
            float y = Ground(x,z);
            Box("Sapin_tronc",x,y+1.25f,z,.45f,2.5f,.45f,wood);
            for (int level=0;level<3;level++)
            {
                float width = 4.5f-level*1.1f;
                float cy = y+2.6f+level*1.25f;
                Box("Sapin_branches",new Vector3(x,cy,z),new Vector3(width,1.6f,width),green,Quaternion.Euler(0,45,0));
                Box("Sapin_neige",new Vector3(x,cy+.85f,z),new Vector3(width*.8f,.18f,width*.8f),snow,Quaternion.Euler(0,45,0));
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (decor != null) Destroy(decor);
            foreach (Material mat in new [] { wood, stone, snow, metal, glass, lightMat, green, sign, fabric })
                if (mat != null) Destroy(mat);
        }
    }
}
