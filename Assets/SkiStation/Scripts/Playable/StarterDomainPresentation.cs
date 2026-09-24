using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// Builds a small, immediately visible starter resort in a fresh scene.
    /// It uses the real MountainRuntime, SlopeRuntime, LiftRuntime and visitor systems,
    /// so it is a presentation layer rather than a fake/demo simulation.
    /// </summary>
    public sealed class StarterDomainPresentation : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool frameCamera = true;
        [SerializeField] private bool createScenery = true;

        private readonly List<GameObject> scenery = new List<GameObject>();
        private Material snowMaterial;
        private Material rockMaterial;
        private Material treeMaterial;
        private Material trunkMaterial;
        private Material buildingMaterial;
        private Material glassMaterial;
        private Material signGreen;
        private Material signBlue;
        private Material signRed;
        private Material signBlack;

        private void Start()
        {
            if (buildOnStart) Build();
        }

        public void Build()
        {
            if (GameObject.Find("SkiStation_StarterDomain") != null) return;
            var root = new GameObject("SkiStation_StarterDomain");
            transform.SetParent(root.transform);

            CreateMaterials();
            BuildBaseVillage(root.transform);
            BuildSlopesAndLift(root.transform);
            if (createScenery) BuildScenery(root.transform);
            BuildInformationSign(root.transform);
            SetupWorldLighting();
            if (frameCamera) FrameCamera();
        }

        private void BuildSlopesAndLift(Transform root)
        {
            var mountain = MountainRuntime.I;
            if (mountain == null || !mountain.IsGenerated) return;

            Vector3 high = Surface(new Vector3(0f, 0f, 330f));
            Vector3 baseA = Surface(new Vector3(-250f, 0f, -250f));
            Vector3 baseB = Surface(new Vector3(-80f, 0f, -290f));
            Vector3 baseC = Surface(new Vector3(90f, 0f, -285f));
            Vector3 baseD = Surface(new Vector3(260f, 0f, -245f));

            CreateSlope(root, "Piste Aigle", SlopeDifficulty.Black, high,
                new[] { new Vector3(0f,0f,330f), new Vector3(-35f,0f,220f), new Vector3(-70f,0f,100f), new Vector3(-140f,0f,-40f), new Vector3(-205f,0f,-150f), new Vector3(-250f,0f,-250f) },
                new[] { 12f, 12f, 13f, 15f, 17f, 18f }, signBlack);
            CreateSlope(root, "Piste Glacier", SlopeDifficulty.Red, high,
                new[] { new Vector3(0f,0f,330f), new Vector3(45f,0f,230f), new Vector3(70f,0f,120f), new Vector3(55f,0f,10f), new Vector3(20f,0f,-130f), new Vector3(-80f,0f,-290f) },
                new[] { 11f, 12f, 14f, 16f, 18f, 20f }, signRed);
            CreateSlope(root, "Piste Mélèze", SlopeDifficulty.Blue, high,
                new[] { new Vector3(0f,0f,330f), new Vector3(80f,0f,250f), new Vector3(125f,0f,145f), new Vector3(155f,0f,40f), new Vector3(135f,0f,-110f), new Vector3(90f,0f,-285f) },
                new[] { 15f, 16f, 18f, 20f, 22f, 24f }, signBlue);
            CreateSlope(root, "Piste Famille", SlopeDifficulty.Green, high,
                new[] { new Vector3(0f,0f,330f), new Vector3(-105f,0f,250f), new Vector3(-155f,0f,160f), new Vector3(-180f,0f,60f), new Vector3(-175f,0f,-90f), new Vector3(-80f,0f,-290f) },
                new[] { 18f, 20f, 22f, 24f, 26f, 28f }, signGreen);

            BuildStarterLift(root, baseC, high);
        }

        private void CreateSlope(Transform root, string name, SlopeDifficulty difficulty, Vector3 ignoredHigh, Vector3[] xz, float[] widths, Material signMaterial)
        {
            var route = new List<Vector3>();
            for (int i = 0; i < xz.Length; i++) route.Add(Surface(xz[i]) + Vector3.up * 0.18f);
            if (route.Count < 2) return;

            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var slope = go.AddComponent<SlopeRuntime>();
            slope.Build(route, new List<float>(widths));
            slope.SetDesignedDifficulty(difficulty);
            slope.SetGroomed(difficulty != SlopeDifficulty.Black);
            var mesh = go.AddComponent<SlopeMeshGenerator>();
            mesh.Generate(slope);
            var traffic = go.AddComponent<SlopeTraffic>();
            traffic.enabled = true;
            BuildSlopeMarkers(root, route, signMaterial, name);
        }

        private void BuildStarterLift(Transform root, Vector3 bottom, Vector3 top)
        {
            if (GameObject.Find("Télésiège Panorama — Démonstration") != null) return;
            var definition = ScriptableObject.CreateInstance<LiftDefinition>();
            definition.id = "starter_panorama_6seat";
            definition.displayName = "Télésiège Panorama 6 places";
            definition.type = LiftType.DetachableChair;
            definition.buildCost = 250000f;
            definition.speedKmh = 18f;
            definition.seatsPerVehicle = 6;
            definition.vehicles = 48;
            definition.spacingSeconds = 8f;
            definition.detachable = true;
            definition.variableSpeed = true;

            var route = new List<Vector3> { bottom + Vector3.up * 1.2f, Surface(new Vector3(60f,0f,-40f)) + Vector3.up * 8f, Surface(new Vector3(30f,0f,160f)) + Vector3.up * 8f, top + Vector3.up * 5f };
            var go = new GameObject("Télésiège Panorama — Démonstration");
            go.transform.SetParent(root, false);
            var lift = go.AddComponent<LiftRuntime>();
            lift.Build(definition, route, false);
        (go.GetComponent<LiftPlayableVisibilityRuntime>() ?? go.AddComponent<LiftPlayableVisibilityRuntime>()).Build(route);
        }

        private void BuildBaseVillage(Transform root)
        {
            Vector3 baseCenter = Surface(Vector3.zero) + Vector3.up * 0.2f;
            CreateBuilding(root, "Gare principale", baseCenter + new Vector3(0f, 2.5f, -35f), new Vector3(46f, 5f, 22f), buildingMaterial);
            CreateBuilding(root, "Restaurant", baseCenter + new Vector3(-60f, 2.5f, -55f), new Vector3(30f, 5f, 22f), buildingMaterial);
            CreateBuilding(root, "Boutique", baseCenter + new Vector3(60f, 2.2f, -55f), new Vector3(24f, 4.4f, 18f), buildingMaterial);
            CreateBuilding(root, "École de ski", baseCenter + new Vector3(110f, 2.0f, -15f), new Vector3(26f, 4f, 16f), buildingMaterial);
            CreateBuilding(root, "Hôtel", baseCenter + new Vector3(-115f, 4f, -15f), new Vector3(30f, 8f, 20f), buildingMaterial);
        }

        private void BuildScenery(Transform root)
        {
            var positions = new[]
            {
                new Vector3(-360,0,80), new Vector3(-330,0,160), new Vector3(-290,0,250), new Vector3(-240,0,330),
                new Vector3(280,0,90), new Vector3(320,0,170), new Vector3(300,0,260), new Vector3(230,0,350),
                new Vector3(-310,0,-100), new Vector3(310,0,-70), new Vector3(-270,0,-210), new Vector3(280,0,-180),
                new Vector3(185,0,210), new Vector3(-170,0,220)
            };
            foreach (var p in positions) CreateTree(root, Surface(p));
            for (int i = 0; i < 14; i++)
            {
                float a = i * 0.87f;
                Vector3 p = Surface(new Vector3(Mathf.Sin(a) * (260f + i * 7f), 0f, 30f + Mathf.Cos(a) * 250f));
                CreateRock(root, p);
            }
        }

        private void BuildSlopeMarkers(Transform root, List<Vector3> route, Material mat, string slopeName)
        {
            for (int i = 1; i < route.Count; i += 2)
            {
                Vector3 p = route[i];
                Vector3 dir = (route[Mathf.Min(i + 1, route.Count - 1)] - route[Mathf.Max(0, i - 1)]).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                CreateSign(root, p + side * 10f, slopeName, mat);
                CreateSign(root, p - side * 10f, slopeName, mat);
            }
        }

        private void BuildInformationSign(Transform root)
        {
            Vector3 p = Surface(new Vector3(0f,0f,-5f)) + Vector3.up * 6f;
            CreateSign(root, p, "DOMAINE OUVERT • 4 PISTES • 1 TÉLÉSIÈGE", signBlue, 1.5f);
        }

        private void CreateTree(Transform root, Vector3 p)
        {
            var go = new GameObject("Mélèze"); go.transform.SetParent(root, false);
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder); trunk.name = "Tronc"; trunk.transform.SetParent(go.transform, false); trunk.transform.localPosition = Vector3.up * 3f; trunk.transform.localScale = new Vector3(.45f,3f,.45f); trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere); crown.name = "Houppier"; crown.transform.SetParent(go.transform, false); crown.transform.localPosition = Vector3.up * 7f; crown.transform.localScale = new Vector3(3.2f,5.2f,3.2f); crown.GetComponent<Renderer>().sharedMaterial = treeMaterial;
            go.transform.position = p;
        }

        private void CreateRock(Transform root, Vector3 p)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Bloc rocheux"; go.transform.SetParent(root, false); go.transform.position = p + Vector3.up * .8f; go.transform.localScale = new Vector3(4f,2f,3f); go.GetComponent<Renderer>().sharedMaterial = rockMaterial;
        }

        private void CreateBuilding(Transform root, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(root, false); go.transform.position = pos; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube); roof.name = name + " Toit"; roof.transform.SetParent(root, false); roof.transform.position = pos + Vector3.up * (scale.y * .5f + .5f); roof.transform.localScale = new Vector3(scale.x * 1.05f, .8f, scale.z * 1.05f); roof.GetComponent<Renderer>().sharedMaterial = rockMaterial;
        }

        private void CreateSign(Transform root, Vector3 pos, string text, Material mat, float scale = 1f)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder); post.name = "Poteau signalétique"; post.transform.SetParent(root, false); post.transform.position = pos + Vector3.down * 1.5f; post.transform.localScale = new Vector3(.12f,1.5f,.12f); post.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube); board.name = text; board.transform.SetParent(root, false); board.transform.position = pos; board.transform.localScale = new Vector3(2.8f * scale,.55f * scale,.12f); board.GetComponent<Renderer>().sharedMaterial = mat;
            var label = new GameObject("Texte " + text); label.transform.SetParent(root, false); label.transform.position = pos + Vector3.forward * -.08f + Vector3.up * .02f; label.transform.localScale = Vector3.one * (.07f * scale);
            var tm = label.AddComponent<TextMesh>(); tm.text = text; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.characterSize = 1f; tm.fontSize = 32; tm.color = Color.white; label.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private Vector3 Surface(Vector3 xz)
        {
            if (MountainRuntime.I != null && MountainRuntime.I.TryGetSurface(xz + Vector3.up * 2500f, out var p)) return p;
            return new Vector3(xz.x, 900f, xz.z);
        }

        private void FrameCamera()
        {
            if (Camera.main == null || MountainRuntime.I == null) return;
            var cam = Camera.main;
            float minY = MountainRuntime.I.MinAltitude;
            float maxY = MountainRuntime.I.MaxAltitude;
            float relief = Mathf.Max(250f, maxY - minY);
            Vector3 target = new Vector3(0f, minY + relief * .42f, 40f);
            cam.transform.position = new Vector3(760f, maxY + 430f, -1120f);
            cam.transform.LookAt(target);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 7000f;
            cam.fieldOfView = 52f;
            cam.clearFlags = CameraClearFlags.Skybox;
        }

        private void SetupWorldLighting()
        {
            RenderSettings.ambientLight = new Color(.62f, .68f, .76f);
            RenderSettings.fog = false; // 2.17: keep construction previews readable at every distance.

            var sun = FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.type = LightType.Directional;
                sun.intensity = 1.15f;
                sun.color = new Color(1f, .95f, .86f);
                sun.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            }
        }

        private void CreateMaterials()
        {
            snowMaterial = Mat(new Color(.96f,.98f,1f));
            rockMaterial = Mat(new Color(.34f,.36f,.39f));
            treeMaterial = Mat(new Color(.12f,.32f,.16f));
            trunkMaterial = Mat(new Color(.28f,.18f,.10f));
            buildingMaterial = Mat(new Color(.55f,.42f,.30f));
            glassMaterial = Mat(new Color(.45f,.70f,.82f));
            signGreen = Mat(new Color(.12f,.50f,.20f));
            signBlue = Mat(new Color(.12f,.30f,.65f));
            signRed = Mat(new Color(.72f,.12f,.10f));
            signBlack = Mat(new Color(.08f,.08f,.08f));
        }

        private Material Mat(Color c)
        {
            return RuntimeMaterialFactory.Create(c);
        }
    }
}
