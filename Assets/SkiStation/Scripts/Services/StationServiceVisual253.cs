using UnityEngine;

namespace SkiStation
{
    /// <summary>Procedural, pipeline-independent service-building prototypes.</summary>
    public static class StationServiceVisual253
    {
        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            if (shader == null) return null;
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private static Transform Part(Transform parent, string name, Vector3 position, Vector3 scale, Color color, float rotationZ = 0f)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            var renderer = part.GetComponent<Renderer>();
            if (renderer != null) { var mat = MakeMaterial(color); if (mat != null) renderer.sharedMaterial = mat; }
            return part.transform;
        }

        public static void Apply(GameObject building, StationServiceType type, string name)
        {
            if (building == null) return;
            bool bar = type == StationServiceType.Bar;
            bool restaurant = type == StationServiceType.Restaurant;
            string label = (name ?? "").ToLowerInvariant();
            bool alpine = label.Contains("alpin") || label.Contains("chalet") || label.Contains("savoy") || label.Contains("auberge");
            bool panoramic = label.Contains("panoram") || label.Contains("altitude");
            bool modern = label.Contains("modern") || label.Contains("lounge");
            Color facade = alpine ? new Color(.48f,.30f,.18f) : modern ? new Color(.44f,.47f,.49f)
                : bar ? new Color(.43f,.29f,.21f) : new Color(.67f,.44f,.29f);
            var body = building.GetComponent<Renderer>();
            if (body != null) { var mat = MakeMaterial(facade); if (mat != null) body.sharedMaterial = mat; }

            // All dimensions are relative to the 12 x 6 x 12 metre building root.
            // Two real pitched roof panels, with a ridge and snow caps, replace the flat roof.
            Color roof = alpine ? new Color(.28f,.19f,.15f) : new Color(.23f,.25f,.28f);
            Part(building.transform, "Toit_pente_gauche", new Vector3(-.28f,.64f,0),
                new Vector3(.76f,.085f,1.24f), roof, 24f);
            Part(building.transform, "Toit_pente_droite", new Vector3(.28f,.64f,0),
                new Vector3(.76f,.085f,1.24f), roof, -24f);
            Part(building.transform, "Neige_toit_gauche", new Vector3(-.28f,.69f,0),
                new Vector3(.73f,.025f,1.25f), new Color(.89f,.94f,.97f), 24f);
            Part(building.transform, "Neige_toit_droite", new Vector3(.28f,.69f,0),
                new Vector3(.73f,.025f,1.25f), new Color(.89f,.94f,.97f), -24f);
            Part(building.transform, "Faitage", new Vector3(0,.82f,0),
                new Vector3(.07f,.06f,1.26f), roof);
            Part(building.transform, "Soubassement_pierre", new Vector3(0,-.45f,0),
                new Vector3(1.025f,.13f,1.025f), new Color(.40f,.40f,.39f));
            Part(building.transform, "Auvent_entree", new Vector3(0,.17f,-.59f),
                new Vector3(.36f,.045f,.25f), roof);
            Part(building.transform, "Porte", new Vector3(0,-.23f,-.511f),
                new Vector3(.16f,.47f,.026f), new Color(.20f,.15f,.12f));
            Color glass = panoramic ? new Color(.30f,.65f,.76f) : new Color(.38f,.57f,.66f);
            Color trim = new Color(.20f,.16f,.12f);
            for (int i = -1; i <= 1; i += 2)
            {
                float x = i * .31f;
                Part(building.transform, "Fenetre_facade_" + i, new Vector3(x,.07f,-.512f),
                    new Vector3(.23f,.34f,.028f), glass);
                Part(building.transform, "Encadrement_fenetre_" + i, new Vector3(x,.07f,-.531f),
                    new Vector3(.26f,.025f,.035f), trim);
                Part(building.transform, "Fenetre_arriere_" + i, new Vector3(x,.07f,.512f),
                    new Vector3(.23f,.34f,.028f), glass);
                Part(building.transform, "Fenetre_cote_" + i, new Vector3(-.512f,.07f,i*.28f),
                    new Vector3(.028f,.34f,.23f), glass);
            }
            Part(building.transform, "Enseigne_" + (bar ? "BAR" : restaurant ? "RESTAURANT" : "SERVICE"),
                new Vector3(0,.35f,-.538f), new Vector3(.43f,.10f,.04f),
                bar ? new Color(.86f,.62f,.24f) : new Color(.83f,.75f,.53f));
            if (bar || restaurant)
            {
                Part(building.transform, "Terrasse", new Vector3(0,-.48f,-.76f),
                    new Vector3(.95f,.045f,.45f), new Color(.46f,.33f,.22f));
                for (int i = -1; i <= 1; i++)
                {
                    float x = i * .28f;
                    Part(building.transform, "Table_terrasse_" + i, new Vector3(x,-.37f,-.78f),
                        new Vector3(.12f,.035f,.12f), new Color(.28f,.22f,.17f));
                    Part(building.transform, "Pied_table_" + i, new Vector3(x,-.43f,-.78f),
                        new Vector3(.025f,.10f,.025f), new Color(.20f,.19f,.18f));
                }
                for (int i = -1; i <= 1; i += 2)
                    Part(building.transform, "Garde_corps_terrasse_" + i, new Vector3(i*.46f,-.35f,-.76f),
                        new Vector3(.022f,.25f,.44f), new Color(.35f,.25f,.17f));
            }
            if (alpine)
                Part(building.transform, "Poutre_bois", new Vector3(0,.30f,-.54f),
                    new Vector3(.97f,.055f,.07f), new Color(.30f,.18f,.11f));
            if (panoramic)
                Part(building.transform, "Baie_panoramique", new Vector3(0,.02f,.513f),
                    new Vector3(.48f,.42f,.028f), glass);
        }
    }
}
