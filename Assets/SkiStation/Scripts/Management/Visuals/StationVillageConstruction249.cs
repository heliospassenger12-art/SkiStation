using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    // Management-only first playable building placement prototype. No economy is charged yet.
    public sealed class StationVillageConstruction249 : MonoBehaviour
    {
        private static StationVillageConstruction249 instance;
        private readonly List<GameObject> placed = new List<GameObject>();
        private GameObject preview;
        private GameObject pathPreview;
        private Material pathPreviewMaterial;
        private Vector3 pathEntrance, pathRoadside;
        private Renderer[] previewRenderers;
        private Material previewMaterial;
        private int selection; // 0 chalet, 1 hotel, 2 shop, 3 apartment building
        private bool equipmentShopSelected = true;
        private int stars = 2, floors = 3, chaletSize = 1, amenitiesMask, manualRate = 100;
        private StationAccommodation249.PriceMode priceMode = StationAccommodation249.PriceMode.SansGestion;
        private static readonly string[] AmenityNames = { "Massage", "Spa", "Hammam", "Sauna", "Piscine interieure chauffee", "Piscine exterieure chauffee" };
        private bool IsAccommodation { get { return selection != 2; } }
        private int EffectiveFloors { get { return selection == 0 ? 1 : floors; } }
        private int EffectiveCapacity
        {
            get
            {
                if (selection == 0) return (4 + chaletSize * 4) + (stars >= 4 ? 2 : 0);
                if (selection == 1) return Mathf.Max(1, Mathf.RoundToInt(EffectiveFloors * 14f * (1.15f - stars * .08f)));
                if (selection == 3) return Mathf.Max(1, Mathf.RoundToInt(EffectiveFloors * 20f * (1.15f - stars * .08f)));
                if (selection == 4) return Mathf.Max(1, Mathf.RoundToInt(EffectiveFloors * 12f * (1.15f - stars * .08f)));
                return Capacities[selection];
            }
        }
        private int EffectiveCost
        {
            get
            {
                if (!IsAccommodation) return Prices[selection];
                float sizeFactor = selection == 0 ? (.65f + chaletSize * .35f) : (.4f + EffectiveFloors * .2f);
                int amenityCount = 0; for (int i = 0; i < 6; i++) if ((amenitiesMask & (1 << i)) != 0) amenityCount++;
                return Mathf.RoundToInt(Prices[selection] * sizeFactor * (1f + .2f * (stars - 1)) + (selection == 1 ? amenityCount * 45000 : 0));
            }
        }
        private void ChangedOptions() { awaitingConfirmation = false; RefreshPreview(); }
        private bool active, awaitingConfirmation, valid;
        public static bool IsPlacing { get { return instance != null && instance.active; } }
        private Vector3 candidate;
        private string message = "";
        private static readonly string[] Names = { "Chalet", "Hotel", "Commerce", "Immeuble_appartements", "Residence_tourisme" };
        private static readonly int[] Prices = { 85000, 450000, 120000, 720000, 350000 };
        private static readonly int[] Capacities = { 8, 60, 24, 96, 48 };
        private static readonly Vector2[] Sizes = { new Vector2(15, 12), new Vector2(28, 22), new Vector2(20, 16), new Vector2(30, 25), new Vector2(28, 22) };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            instance = new GameObject("SkiStation_249_ConstructionVillage").AddComponent<StationVillageConstruction249>();
        }

        private void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (Input.GetKeyDown(KeyCode.B)) { active = !active; awaitingConfirmation = false; RefreshPreview(); }
            if (!active) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) Choose(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Choose(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Choose(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) Choose(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) Choose(4);
            if (Input.GetKeyDown(KeyCode.Escape)) { if (awaitingConfirmation) awaitingConfirmation = false; else active = false; RefreshPreview(); }
            if (awaitingConfirmation) return;
            if (preview == null) RefreshPreview();
            Camera camera = Camera.main;
            if (camera == null) return;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            // Intersect a horizontal reference plane, then project onto the generated ground.
            float baseY = SkiStationGuaranteedMountainVisual.Height(0f, -2600f);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, baseY, 0f));
            float distance;
            if (!plane.Raycast(ray, out distance) || distance < 0f) { if (preview != null) preview.SetActive(false); return; }
            Vector3 hit = ray.GetPoint(distance);
            candidate = new Vector3(hit.x, SkiStationGuaranteedMountainVisual.Height(hit.x, hit.z), hit.z);
            valid = Validate(candidate);
            if (pathPreview != null) Destroy(pathPreview);
            pathPreview = null;
            if (valid && pathPreviewMaterial != null) pathPreview = StationPedestrianAccess249.CreateVisual(pathEntrance, pathRoadside, pathPreviewMaterial, "Apercu_chemin_pieton");
            preview.SetActive(true);
            preview.transform.position = candidate;
            if (previewMaterial != null) previewMaterial.color = valid ? new Color(.2f, .85f, .4f, .55f) : new Color(.95f, .25f, .2f, .55f);
            if (Input.GetMouseButtonDown(0) && !IsPointerOverPanel() && valid) awaitingConfirmation = true;
        }

        private bool IsPointerOverPanel() { return Input.mousePosition.x < 370f && Input.mousePosition.y > Screen.height - 230f; }
        private void Choose(int index) { selection = index; awaitingConfirmation = false; RefreshPreview(); }
        private bool Validate(Vector3 point)
        {
            var road = StationRoadNetwork247.I;
            float radius = Mathf.Max(Sizes[selection].x, Sizes[selection].y) * .72f + 4f;
            if (road == null || !road.IsReady) { message = "Route principale non initialisee"; return false; }
            if (!road.CanPlaceBuilding(point, radius)) { message = "Emplacement sur une route existante"; return false; }
            // A building must be reachable from a road, but may stand behind a pedestrian forecourt.
            if (road.DistanceToNearestRoad(point) > radius + 55f)
            { message = "Trop loin d'une route (55 m maximum depuis le bord)"; return false; }
            foreach (var building in placed)
                if (building != null && Vector2.Distance(new Vector2(point.x, point.z), new Vector2(building.transform.position.x, building.transform.position.z)) < radius + 20f)
                { message = "Trop proche d'un autre batiment"; return false; }
            string accessReason;
            if (!StationPedestrianAccess249.TryPlan(road, point, Sizes[selection].y, out pathEntrance, out pathRoadside, out accessReason))
            { message = accessReason; return false; }
            // A footpath must not pass through an already confirmed building.
            // Check the complete sampled path, not only its two endpoints.
            foreach (var building in placed)
            {
                if (building == null) continue;
                Vector3 center = building.transform.position;
                // Conservative footprint until individual building dimensions are persisted.
                const float clearance = 9f;
                for (int step = 0; step <= 24; step++)
                {
                    Vector3 p = Vector3.Lerp(pathRoadside, pathEntrance, step / 24f);
                    if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(center.x, center.z)) < clearance)
                    {
                        message = "Le chemin traverse un batiment existant";
                        return false;
                    }
                }
            }
            message = "Emplacement et chemin pieton disponibles";
            return true;
        }
        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader); material.color = color; return material;
        }
        private static GameObject Box(string label, Transform parent, Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = label;
            go.transform.SetParent(parent, false); go.transform.localPosition = localPosition; go.transform.localScale = size;
            Collider collider = go.GetComponent<Collider>(); if (collider != null) Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        private GameObject MakeBuilding(Material material)
        {
            var root = new GameObject("Batiment_" + Names[selection]);
            float width = Sizes[selection].x * (selection == 0 ? (.8f + chaletSize * .2f) : 1f), depth = Sizes[selection].y * (selection == 0 ? (.8f + chaletSize * .2f) : 1f), height = IsAccommodation ? (selection == 0 ? 7f + chaletSize * 2f : EffectiveFloors * 3.2f) : 10f;
            Box("Fondation", root.transform, new Vector3(0, .7f, 0), new Vector3(width + 1f, 1.4f, depth + 1f), material);
            Box("Facade", root.transform, new Vector3(0, height * .5f + 1.4f, 0), new Vector3(width, height, depth), material);
            Box("Toit", root.transform, new Vector3(0, height + 2.4f, 0), new Vector3(width + 2f, 1.5f, depth + 2f), material);
            return root;
        }
        private void RefreshPreview()
        {
            if (preview != null) Destroy(preview);
            if (pathPreview != null) Destroy(pathPreview);
            if (pathPreviewMaterial != null) Destroy(pathPreviewMaterial);
            pathPreview = null; pathPreviewMaterial = null;
            if (previewMaterial != null) Destroy(previewMaterial);
            preview = null; previewMaterial = null;
            if (!active) return;
            previewMaterial = CreateMaterial(new Color(.2f, .85f, .4f, .55f));
            pathPreviewMaterial = CreateMaterial(new Color(.75f, .7f, .55f, .75f));
            preview = MakeBuilding(previewMaterial); preview.name = "Apercu_construction_NON_VALIDE";
        }
        private void Confirm()
        {
            if (!Validate(candidate)) { awaitingConfirmation = false; return; }
            Material material = CreateMaterial(new Color(.51f, .33f, .20f));
            GameObject building = MakeBuilding(material);
            building.name = Names[selection] + "_joueur_" + (placed.Count + 1);
            building.transform.position = candidate;
            placed.Add(building);
            // Modify only the confirmed footprint and its soft perimeter, never the preview.
            float foundationWidth = Sizes[selection].x * (selection == 0 ? (.8f + chaletSize * .2f) : 1f);
            float foundationDepth = Sizes[selection].y * (selection == 0 ? (.8f + chaletSize * .2f) : 1f);
            ConstructionTerrainAdaptation251.Flatten(candidate, foundationWidth, foundationDepth, 5f);
            ConstructionClearance249.ClearTrees(new Vector3[] { candidate }, Mathf.Max(foundationWidth, foundationDepth) * .72f + 5f);
            building.AddComponent<StationStructureStatus249>();
            if (IsAccommodation)
            {
                StationAccommodation249 accommodation = building.AddComponent<StationAccommodation249>();
                accommodation.Configure(selection == 0 ? StationStructureCatalog249.Kind.Chalet :
                    selection == 1 ? StationStructureCatalog249.Kind.Hotel : selection == 3 ? StationStructureCatalog249.Kind.ApartmentBuilding : StationStructureCatalog249.Kind.ApartmentResidence, EffectiveCapacity);
                accommodation.ConfigureDetails(stars, EffectiveFloors, chaletSize, amenitiesMask, priceMode, manualRate, EffectiveCost);
            }
            if (selection == 2 && equipmentShopSelected)
            {
                building.name = "Magasin_sports_hiver_" + (placed.Count);
                building.AddComponent<StationEquipmentShop249>();
            }
            GameObject accessVisual = StationPedestrianAccess249.CreateVisual(pathEntrance, pathRoadside, CreateMaterial(new Color(.66f, .61f, .51f)), "Acces_pieton_" + building.name);
            StationPedestrianNetwork249.GetOrCreate().Register(building, accessVisual, pathEntrance, pathRoadside);
            awaitingConfirmation = false;
            message = "Batiment et chemin pieton construits (sans debit financier)";
        }
        private void OnGUI()
        {
            if (!active || SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            GUILayout.BeginArea(new Rect(12, 100, 380, IsAccommodation ? (selection == 1 ? 560 : 400) : 205), GUI.skin.box);
            GUILayout.Label("CONSTRUCTION DU VILLAGE  |  B : fermer");
            GUILayout.Label("1 Chalet  2 Hotel  3 Commerce  4 Immeuble  5 Residence");
            if (selection == 2) equipmentShopSelected = GUILayout.Toggle(equipmentShopSelected, "Magasin sports hiver : 5 rayons (sinon commerce general)");
            if (IsAccommodation)
            {
                GUILayout.Label("Standing : " + stars + " etoile(s)");
                int newStars = Mathf.RoundToInt(GUILayout.HorizontalSlider(stars, 1, 5));
                if (newStars != stars) { stars = newStars; ChangedOptions(); }
                if (selection == 0)
                {
                    GUILayout.Label("Taille du chalet : " + chaletSize + "/3");
                    int nextSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(chaletSize, 1, 3));
                    if (nextSize != chaletSize) { chaletSize = nextSize; ChangedOptions(); }
                }
                else
                {
                    GUILayout.Label("Etages : " + floors + " / 9");
                    int nextFloors = Mathf.RoundToInt(GUILayout.HorizontalSlider(floors, 1, 9));
                    if (nextFloors != floors) { floors = nextFloors; ChangedOptions(); }
                }
                if (selection == 1)
                {
                    GUILayout.Label("Prestations de l'hotel :");
                    for (int i = 0; i < AmenityNames.Length; i++)
                    {
                        bool enabled = (amenitiesMask & (1 << i)) != 0;
                        bool next = GUILayout.Toggle(enabled, AmenityNames[i]);
                        if (next != enabled) { amenitiesMask ^= 1 << i; ChangedOptions(); }
                    }
                }
                GUILayout.Label("Tarification :");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Sans gestion")) priceMode = StationAccommodation249.PriceMode.SansGestion;
                if (GUILayout.Button("Auto")) priceMode = StationAccommodation249.PriceMode.Automatique;
                if (GUILayout.Button("Manuel")) priceMode = StationAccommodation249.PriceMode.Manuel;
                GUILayout.EndHorizontal();
                GUILayout.Label("Mode : " + priceMode);
                if (priceMode == StationAccommodation249.PriceMode.Manuel)
                {
                    GUILayout.Label("Tarif nuit indicatif : " + manualRate + " EUR");
                    manualRate = Mathf.RoundToInt(GUILayout.HorizontalSlider(manualRate, 0, 2000));
                }
                GUILayout.Label("Capacite prevue : " + EffectiveCapacity + " personnes");
            }
            else GUILayout.Label("Capacite indicative : " + Capacities[selection] + " personnes");
            GUILayout.Label(Names[selection] + " | Cout indicatif : " + EffectiveCost + " EUR");
            GUILayout.Label(message);
            if (awaitingConfirmation)
            {
                GUILayout.Label("Batiment + chemin pieton automatique. Aucun debit.");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Confirmer")) Confirm();
                if (GUILayout.Button("Modifier")) awaitingConfirmation = false;
                if (GUILayout.Button("Annuler")) { awaitingConfirmation = false; active = false; RefreshPreview(); }
                GUILayout.EndHorizontal();
            }
            else GUILayout.Label("Survolez le sol et cliquez pour previsualiser le recapitulatif.");
            GUILayout.EndArea();
        }
        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (preview != null) Destroy(preview);
            if (pathPreview != null) Destroy(pathPreview);
            if (pathPreviewMaterial != null) Destroy(pathPreviewMaterial);
            pathPreview = null; pathPreviewMaterial = null;
            if (previewMaterial != null) Destroy(previewMaterial);
        }
    }
}
