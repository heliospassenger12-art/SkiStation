using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SkiStation.MountainLab.Editor
{
    public sealed class MountainLabWindow312 : EditorWindow
    {
        internal const string OutputRoot = "Assets/SkiStation/Generated/MountainLab312";
        [SerializeField] MountainLabSettings312 settings = new MountainLabSettings312();
        Vector2 scroll;
        string status = "Créer une scène d’étude, puis l’observer dans l’onglet Scene.";

        [MenuItem("SkiStation/Montagne/Atelier montagne 3.12")]
        public static void Open() { GetWindow<MountainLabWindow312>("Montagne 3.12"); }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("ATELIER MONTAGNE", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Prototype visuel dans l’éditeur. Passe 3.12 : première étude verticale du grand domaine, de 1600 m jusqu’au secteur glaciaire proche de 4200 m, toujours exploitable en 360°. Le domaine de gestion reste intact.", MessageType.Info);
            settings.seed = EditorGUILayout.IntField("Graine du massif", settings.seed);
            settings.heightScale = EditorGUILayout.Slider("Hauteur générale", settings.heightScale, .6f, 1.4f);
            settings.naturalVariation = EditorGUILayout.Slider("Variations naturelles", settings.naturalVariation, 0f, 1f);
            int choice = settings.resolution == 513 ? 1 : 0;
            choice = EditorGUILayout.Popup("Résolution", choice, new[] { "257 — aperçu rapide", "513 — relief détaillé" });
            settings.resolution = choice == 1 ? 513 : 257;
            settings.trees = EditorGUILayout.Toggle("Sapins", settings.trees);
            settings.slopeColours = EditorGUILayout.Toggle("Carte des inclinaisons", settings.slopeColours);
            EditorGUILayout.LabelField("Étude 3.12 : environ 6 × 8,2 km. Station basse vers 1600 m, étages forestiers et alpins, vaste haute montagne/glacier visant ~4200 m, puis seconde face derrière la ligne de partage.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Carte : vert < 10°, bleu 10–18°, rouge 18–28°, noir ≥ 28°. Repères de relief, pas un classement automatique des pistes.", EditorStyles.wordWrappedLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Créer une nouvelle scène d’étude", GUILayout.Height(32))) Generate();
                if (GUILayout.Button("Vérifier le générateur (tests locaux)"))
                {
                    try { status = MountainLabChecks312.Run(); Debug.Log(status); }
                    catch (Exception ex) { status = "Échec du contrôle : " + ex.Message; Debug.LogException(ex); }
                }
            }
            if (GUILayout.Button("Recadrer la vue montagne")) FrameMountain();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.None);
            EditorGUILayout.LabelField("Dans Scene : Alt + clic gauche pour tourner, molette pour zoomer. En jeu : maintenir la molette et déplacer la souris pour tourner autour du massif. Sélectionner un élément puis F pour le cadrer. Revenir à SkiStation_Bootstrap pour tester le GPS.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        void Generate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string folder = null;
            try
            {
                EditorUtility.DisplayProgressBar("Montagne 3.12", "Préparation du relief…", .05f);
                Directory.CreateDirectory(OutputRoot);
                AssetDatabase.Refresh();
                folder = AssetDatabase.GenerateUniqueAssetPath(OutputRoot + "/Massif_" + settings.seed);
                AssetDatabase.CreateFolder(OutputRoot, Path.GetFileName(folder));
                // Never regenerate or overwrite an existing scene or its meshes.
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene.name = "SkiStation_Montagne_312";
                var height = new MountainLabHeight312(settings);
                var root = new GameObject("Atelier montagne 3.12 — aperçu uniquement");
                root.tag = "EditorOnly";
                Material[] mats = Materials(folder, settings.slopeColours);
                var mesh = Surface(height, settings.resolution, settings.slopeColours);
                SaveMesh(folder, "Relief", mesh, root.transform, mats, true);
                EditorUtility.DisplayProgressBar("Montagne 3.12", "Sapins et socle…", .65f);
                var skirt = Skirt(height, settings.resolution);
                SaveMesh(folder, "Socle", skirt, root.transform, new[] { mats[3] }, false);
                if (settings.trees)
                {
                    var forest = Forest(height, settings.seed);
                    var green = MakeMaterial("Sapins", new Color(.075f, .19f, .15f), null);
                    AssetDatabase.CreateAsset(green, folder + "/Sapins.mat");
                    SaveMesh(folder, "Foret", forest, root.transform, new[] { green }, false);
                }
                SetupView();
                File.WriteAllText(folder + "/Parametres.json", JsonUtility.ToJson(settings, true));
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                string path = folder + "/SkiStation_Montagne_312.unity";
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("La scène n’a pas pu être enregistrée.");
                Selection.activeGameObject = root;
                FrameMountain();
                status = "Scène créée : " + path + "\nAperçu dans Scene. Réglages sauvegardés dans Parametres.json. Une nouvelle génération produit une autre scène.";
                Debug.Log("SkiStation 3.12 : " + status);
            }
            catch (Exception ex)
            {
                status = "Génération interrompue : " + ex.Message + (folder == null ? "" : "\nDossier partiel : " + folder);
                Debug.LogException(ex);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        static Mesh Surface(MountainLabHeight312 height, int n, bool slopeColours)
        {
            var vertices = new Vector3[n * n];
            var uv = new Vector2[vertices.Length];
            var tris = new List<int>[5];
            for (int i = 0; i < tris.Length; i++) tris[i] = new List<int>();
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    int index = z * n + x;
                    float px = MountainLabHeight312.MinX + x * MountainLabHeight312.Width / (n - 1);
                    float pz = MountainLabHeight312.MinZ + z * MountainLabHeight312.Length / (n - 1);
                    vertices[index] = new Vector3(px, height.Height(px, pz), pz);
                    uv[index] = new Vector2(px, pz) / 30f;
                }
            for (int z = 0; z < n - 1; z++)
                for (int x = 0; x < n - 1; x++)
                {
                    int a = z * n + x, b = a + 1, c = a + n, d = c + 1;
                    Vector3 centre = (vertices[a] + vertices[d]) * .5f;
                    float degrees = height.SlopeDegrees(centre.x, centre.z);
                    int band = slopeColours ? MountainLabHeight312.SlopeBand(degrees) :
                        centre.z > 1520f && degrees > 32f ? 4 :
                        centre.z > 1550f && degrees > 22f ? 3 : degrees > 20f ? 2 : degrees > 10f ? 1 : 0;
                    tris[band].AddRange(new[] { a, c, b, b, c, d });
                }
            var mesh = new Mesh { name = "Massif312", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices; mesh.uv = uv; mesh.subMeshCount = 5;
            for (int i = 0; i < 5; i++) mesh.SetTriangles(tris[i], i, false);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh Skirt(MountainLabHeight312 height, int n)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int side = 0; side < 4; side++)
                for (int i = 0; i < n - 1; i++)
                {
                    Vector2 a = Boundary(side, i / (float)(n - 1));
                    Vector2 b = Boundary(side, (i + 1f) / (n - 1));
                    int start = v.Count;
                    v.Add(new Vector3(a.x, height.Height(a.x, a.y), a.y));
                    v.Add(new Vector3(b.x, height.Height(b.x, b.y), b.y));
                    v.Add(new Vector3(a.x, -80f, a.y)); v.Add(new Vector3(b.x, -80f, b.y));
                    t.AddRange(new[] { start, start + 1, start + 2, start + 1, start + 3, start + 2 });
                }
            var m = new Mesh { name = "Socle312" }; m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        static Vector2 Boundary(int side, float t)
        {
            float left = MountainLabHeight312.MinX, right = -left;
            float bottom = MountainLabHeight312.MinZ, top = bottom + MountainLabHeight312.Length;
            if (side == 0) return new Vector2(Mathf.Lerp(left, right, t), bottom);
            if (side == 1) return new Vector2(right, Mathf.Lerp(bottom, top, t));
            if (side == 2) return new Vector2(Mathf.Lerp(right, left, t), top);
            return new Vector2(left, Mathf.Lerp(top, bottom, t));
        }

        static Mesh Forest(MountainLabHeight312 height, int seed)
        {
            var rng = new System.Random(seed);
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < 2350; i++)
            {
                float x = (float)rng.NextDouble() * 5600f - 2800f;
                float z = (float)rng.NextDouble() * 4100f - 1300f;
                float y = height.Height(x, z);
                if (y > 2750f || height.SlopeDegrees(x, z) > 32f) continue;
                // 3.11: broad skiable clearings inspired by the references; forest forms edges rather than a uniform carpet.
                float corridorA = Mathf.Abs(x + 1450f + .16f * (z + 250f));
                float corridorB = Mathf.Abs(x - 850f - .08f * (z + 100f));
                float centralGlade = Mathf.Abs(x + 120f) + .28f * Mathf.Abs(z - 250f);
                if (corridorA < 260f || corridorB < 285f || centralGlade < 520f) continue;
                // Keep the beginner/front-de-neige sector airy and create irregular forest islands.
                if (z < -650f && Mathf.Abs(x) < 1950f) continue;
                float patch = Mathf.Sin(x * .0041f + z * .0023f) + Mathf.Sin(x * .0017f - z * .0052f);
                if (patch < -.15f) continue;
                float h = 13f + (float)rng.NextDouble() * 13f;
                for (int layer = 0; layer < 3; layer++)
                {
                    float baseY = y + layer * h * .22f;
                    float radius = h * (.22f - layer * .045f);
                    Cone(vertices, triangles, new Vector3(x, baseY, z), radius, h * .58f);
                }
            }
            var mesh = new Mesh { name = "Foret312", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static void Cone(List<Vector3> v, List<int> t, Vector3 centre, float radius, float height)
        {
            const int sides = 7;
            int first = v.Count; v.Add(centre + Vector3.up * height);
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                v.Add(centre + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
            }
            for (int i = 0; i < sides; i++) t.AddRange(new[] { first, first + 1 + (i + 1) % sides, first + 1 + i });
        }

        static void SaveMesh(string folder, string name, Mesh mesh, Transform parent, Material[] materials, bool collider)
        {
            AssetDatabase.CreateAsset(mesh, folder + "/" + name + ".asset");
            var obj = new GameObject(name); obj.tag = "EditorOnly"; obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterials = materials;
            if (collider) obj.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static Material[] Materials(string folder, bool diagnostic)
        {
            var snow = MakeTexture("Neige312", false); var rock = MakeTexture("Roche312", true);
            AssetDatabase.CreateAsset(snow, folder + "/Neige.asset");
            AssetDatabase.CreateAsset(rock, folder + "/Roche.asset");
            Color[] colours = diagnostic ? new[] { new Color(.2f, .7f, .36f), new Color(.15f, .48f, .92f), new Color(.86f, .19f, .16f), new Color(.08f, .085f, .1f), Color.black } :
                new[] { new Color(.93f, .96f, .99f), new Color(.81f, .88f, .94f), new Color(.67f, .77f, .84f), new Color(.38f, .25f, .17f), new Color(.23f, .145f, .10f) };
            var materials = new Material[5];
            for (int i = 0; i < 5; i++)
            {
                materials[i] = MakeMaterial("Surface_" + i, colours[i], diagnostic ? null : i < 3 ? snow : rock);
                AssetDatabase.CreateAsset(materials[i], folder + "/Surface_" + i + ".mat");
            }
            return materials;
        }

        static Material MakeMaterial(string name, Color colour, Texture texture)
        {
            Shader shader = GraphicsSettings.currentRenderPipeline == null ? Shader.Find("Standard") : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) throw new InvalidOperationException("Aucun shader compatible pour le prototype.");
            var mat = new Material(shader) { name = name, color = colour };
            if (texture != null) mat.mainTexture = texture;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .12f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .12f);
            return mat;
        }

        static Texture2D MakeTexture(string name, bool rock)
        {
            const int n = 128;
            var pixels = new Color[n * n];
            // Periodic sine layers keep both edges continuous when tiled across the mesh.
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x * 2f * Mathf.PI / n, v = y * 2f * Mathf.PI / n;
                    float grain = Mathf.Sin(17f * u + 13f * v) * Mathf.Sin(23f * v - 7f * u);
                    float strata = Mathf.Abs(Mathf.Sin(4f * v + .8f * Mathf.Sin(2f * u)));
                    float value = rock ? .57f + .3f * strata + .09f * grain : .93f + .05f * grain;
                    pixels[y * n + x] = new Color(value, value, value, 1f);
                }
            var texture = new Texture2D(n, n, TextureFormat.RGB24, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            texture.SetPixels(pixels); texture.Apply(true, false); return texture;
        }

        static void SetupView()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.61f, .72f, .84f);
            RenderSettings.ambientEquatorColor = new Color(.43f, .49f, .56f);
            RenderSettings.ambientGroundColor = new Color(.25f, .27f, .30f);
            RenderSettings.fog = false;
            var sun = new GameObject("Soleil atelier").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.15f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f); sun.gameObject.tag = "EditorOnly";
            RenderSettings.sun = sun;
            var camera = new GameObject("Camera apercu").AddComponent<Camera>();
            camera.gameObject.tag = "EditorOnly";
            camera.transform.position = new Vector3(1650f, 2200f, -4100f);
            camera.transform.LookAt(new Vector3(0, 500f, -100f));
            camera.nearClipPlane = 2f; camera.farClipPlane = 25000f;
            camera.fieldOfView = 58f; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.52f, .67f, .80f);
        }

        internal static void FrameMountain()
        {
            var view = SceneView.lastActiveSceneView ?? GetWindow<SceneView>();
            view.LookAt(new Vector3(0, 500f, -250f), Quaternion.Euler(32f, -12f, 0), 3900f, false, true);
            view.sceneLighting = true; view.Repaint();
        }
    }

    [InitializeOnLoad]
    internal static class MountainLabIsolation312
    {
        static MountainLabIsolation312()
        {
            EditorApplication.playModeStateChanged += OnPlay;
        }
        static void OnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                bool study = scene.path.StartsWith(MountainLabWindow312.OutputRoot + "/", StringComparison.Ordinal)
                    || scene.name == "SkiStation_Montagne_312";
                if (!study) continue;
                // Management has global runtime installers: this study is intentionally edit-mode only.
                EditorApplication.isPlaying = false;
                Debug.Log("Atelier montagne : aperçu dans Scene, sans Play. Ouvrez SkiStation_Bootstrap pour jouer.");
                return;
            }
        }
    }
}
