using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Visual-only management map modes. Never changes terrain, colliders or route simulation.</summary>
    public sealed class ReliefViewModesRuntime : MonoBehaviour
    {
        public bool IsDomainStatusMode => mode == ViewMode.DomainStatus;
        private enum ViewMode { Real, Grid, ColoredSlopes, DomainStatus }
        private ViewMode mode;
        private GameObject gridObject;
        private GameObject cachedMassif;
        private readonly Dictionary<Renderer, Material[]> originalSlopeMaterials = new Dictionary<Renderer, Material[]>();
        private readonly Dictionary<SlopeDifficulty, Material> coloredMaterials = new Dictionary<SlopeDifficulty, Material>();
        private readonly Dictionary<Renderer, Material[]> originalLiftMaterials = new Dictionary<Renderer, Material[]>();
        private Material statusHealthyMaterial, statusWarningMaterial, statusClosedMaterial;
        private GUIStyle buttonStyle;
        private float nextRefresh;
        private readonly int[] gridDensities = { 25, 45, 65 };
        private int gridDensityIndex = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (FindFirstObjectByType<ReliefViewModesRuntime>() == null)
                new GameObject("SkiStation_ReliefViewModes").AddComponent<ReliefViewModesRuntime>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 1.5f;
            if (mode == ViewMode.Grid) EnsureGrid();
            RefreshSlopes();
        }

        private void SetMode(ViewMode next)
        {
            if (SkiStation.ValAlpin.ValAlpinScene.IsActive && next == ViewMode.Grid) ClearGrid();
            mode = next;
            if (gridObject != null) gridObject.SetActive(mode == ViewMode.Grid);
            if (mode == ViewMode.Grid) EnsureGrid();
            RefreshSlopes();
        }

        private void OnGUI()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (SkiStationStartMenu249.HomeOpen263) return;
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }
            float width = Mathf.Min(485f, Screen.width - 20f);
            if (width < 220f) return;
            float boxHeight = mode == ViewMode.DomainStatus ? 172f : (mode == ViewMode.Grid || mode == ViewMode.ColoredSlopes ? 104f : 65f);
            var world = TerrainGeneration.TerrainGenerationWorld.Current;
            var nature = world != null ? world.GetComponent<TerrainGeneration.RealTerrainNature>() : null;
            bool showNature = nature != null && (mode == ViewMode.Real || mode == ViewMode.Grid);
            var hydro = showNature ? world.GetComponent<TerrainGeneration.RealTerrainWater>() : null;
            if (showNature) boxHeight += hydro != null ? 66f : 26f;
            var panel = new Rect(10f, 80f, width, boxHeight);
            ConstructionInput349.PanelArea(this, "relief-view", panel);
            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.BeginHorizontal();
            Button("Vue réelle", ViewMode.Real);
            Button("Vue quadrillée", ViewMode.Grid);
            Button("Pistes colorées", ViewMode.ColoredSlopes);
            Button("État domaine", ViewMode.DomainStatus);
            GUILayout.EndHorizontal();
            if (mode == ViewMode.Grid)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Relief : le quadrillage suit la hauteur du terrain", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Densité : " + (gridDensityIndex == 0 ? "faible" : gridDensityIndex == 1 ? "moyenne" : "forte"), GUILayout.Width(155f), GUILayout.Height(25f)))
                {
                    gridDensityIndex = (gridDensityIndex + 1) % gridDensities.Length;
                    ClearGrid();
                    EnsureGrid();
                }
                GUILayout.EndHorizontal();
            }
            if (mode == ViewMode.ColoredSlopes)
                GUILayout.Label("Difficulté :  vert = facile   |   bleu = intermédiaire   |   rouge = difficile   |   noir = très difficile");
            if (mode == ViewMode.DomainStatus)
            {
                var status = DomainStatusMapRuntime366.I;
                GUILayout.Label("État :  vert = fluide/bon   |   orange = à surveiller   |   rouge = fermé/critique");
                if (status != null)
                {
                    GUILayout.Label(string.Format("Pistes  ✓ {0}   ⚠ {1}   ✕ {2}     |     Remontées  ✓ {3}   ⚠ {4}   ✕ {5}",
                        status.HealthySlopes, status.WarningSlopes, status.ClosedSlopes, status.HealthyLifts, status.WarningLifts, status.ClosedLifts));
                    GUILayout.Label("Point sensible : " + status.MainAlert);
                    GUILayout.Label("Inspection 3.67 : cliquez directement sur une piste ou une remontée.");
                }
            }
            if (showNature)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Lac des Estaris", GUILayout.Height(23f)) &&
                    nature.TryGetWater("Grand Lac des Estaris", out var lake))
                    FindFirstObjectByType<PlayableCameraController>()?.FocusDomainElement(lake, 490);
                if (GUILayout.Button("© OpenStreetMap · IGN", GUILayout.Height(23f)))
                    Application.OpenURL("https://www.openstreetmap.org/copyright");
                GUILayout.EndHorizontal();
                if (hydro != null)
                {
                    GUILayout.Label(hydro.Summary);
                    GUILayout.Label(hydro.Activities);
                }
            }
            GUILayout.EndArea();
        }

        private void Button(string label, ViewMode target)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = mode == target ? new Color(.48f, .76f, .91f) : Color.white;
            if (GUILayout.Button(label, buttonStyle, GUILayout.Height(39f))) SetMode(target);
            GUI.backgroundColor = previous;
        }

        private void EnsureGrid()
        {
            if (SkiStation.ValAlpin.ValAlpinScene.IsActive && TerrainGeneration.TerrainGenerationWorld.Current != null)
            { EnsureGeneratedGridValAlpin(); return; }
            var massif = GameObject.Find("SkiStation_MASSIF_01");
            if (massif == null) return;
            if (cachedMassif == massif && gridObject != null) { gridObject.SetActive(true); return; }
            ClearGrid();
            cachedMassif = massif;
            var source = massif.GetComponent<MeshFilter>();
            if (source == null || source.sharedMesh == null) return;
            Bounds bounds = source.sharedMesh.bounds;
            Vector3 min = massif.transform.TransformPoint(bounds.min);
            Vector3 max = massif.transform.TransformPoint(bounds.max);
            float xMin = Mathf.Min(min.x, max.x), xMax = Mathf.Max(min.x, max.x);
            float zMin = Mathf.Min(min.z, max.z), zMax = Mathf.Max(min.z, max.z);
            // The grid follows the existing height surface. No new collision geometry.
            int cells = gridDensities[gridDensityIndex];
            float dx = (xMax - xMin) / cells, dz = (zMax - zMin) / cells;
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            for (int row = 0; row <= cells; row++)
                for (int col = 0; col <= cells; col++)
                {
                    Vector3 p = new Vector3(xMin + col * dx, 0f, zMin + row * dz);
                    Vector3 surface;
                    if (!ManagementSurface.TryGet(p, out surface)) surface = new Vector3(p.x, massif.transform.position.y, p.z);
                    vertices.Add(surface + Vector3.up * .65f);
                }
            for (int row = 0; row <= cells; row++)
                for (int col = 0; col <= cells; col++)
                {
                    int a = row * (cells + 1) + col;
                    if (col < cells) { indices.Add(a); indices.Add(a + 1); }
                    if (row < cells) { indices.Add(a); indices.Add(a + cells + 1); }
                }
            var mesh = new Mesh { name = "ReliefGrid_VisualOnly" };
            mesh.vertices = vertices.ToArray();
            mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
            gridObject = new GameObject("SkiStation_Quadrillage_Relief");
            gridObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gridObject.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null) renderer.sharedMaterial = new Material(shader) { color = new Color(.08f, .23f, .34f, 1f) };
        }

        // Existing view mode, same authoritative collider. Dense vertices avoid long chords through terrain.
        private void EnsureGeneratedGridValAlpin()
        {
            var world = TerrainGeneration.TerrainGenerationWorld.Current;
            if (world == null || !world.IsReady) return;
            var owner = world.Terrain.gameObject;
            if (cachedMassif == owner && gridObject != null) { gridObject.SetActive(true); return; }
            ClearGrid(); cachedMassif = owner;
            var r = world.Recipe;
            float extent = Mathf.Max(r.sizeX, r.sizeZ);
            int lines = Mathf.Clamp(Mathf.RoundToInt(gridDensities[gridDensityIndex] * Mathf.Max(1, extent / 8400f)), 25, 140);
            int steps = Mathf.Clamp(Mathf.CeilToInt(extent / 30f), 280, 720);
            var vertices = new List<Vector3>();var indices = new List<int>();
            for (int direction=0; direction<2; direction++) for (int line=0; line<=lines; line++)
            {
                int start=vertices.Count;
                for (int j=0;j<=steps;j++)
                {
                    float u=direction==0 ? line/(float)lines : j/(float)steps;
                    float v=direction==0 ? j/(float)steps : line/(float)lines;
                    float x=r.originX+u*r.sizeX,z=r.originZ+v*r.sizeZ;
                    vertices.Add(new Vector3(x,world.ClampedHeight(x,z)+.9f,z));
                    if(j>0){indices.Add(start+j-1);indices.Add(start+j);}
                }
            }
            var mesh=new Mesh{name="ValAlpin native terrain grid — visual only"};
            mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetIndices(indices.ToArray(),MeshTopology.Lines,0);mesh.RecalculateBounds();
            gridObject=new GameObject("ValAlpin — quadrillage visuel natif");
            gridObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=gridObject.AddComponent<MeshRenderer>();
            var shader=Shader.Find("Unlit/Color");if(shader==null)shader=Shader.Find("Sprites/Default");
            if(shader!=null)renderer.sharedMaterial=new Material(shader){color=new Color(.08f,.23f,.34f)};
        }

        internal static bool IsGridVisual(Transform candidate)
        {
            var view = FindFirstObjectByType<ReliefViewModesRuntime>();
            return view != null && view.gridObject != null && candidate.IsChildOf(view.gridObject.transform);
        }

        private void ClearGrid()
        {
            if (gridObject == null) return;
            var filter = gridObject.GetComponent<MeshFilter>();
            var renderer = gridObject.GetComponent<MeshRenderer>();
            if (filter != null && filter.sharedMesh != null) Destroy(filter.sharedMesh);
            if (renderer != null && renderer.sharedMaterial != null) Destroy(renderer.sharedMaterial);
            Destroy(gridObject);
            gridObject = null;
        }

        private void OnDestroy()
        {
            ClearGrid();
            foreach (var pair in originalSlopeMaterials)
                if (pair.Key != null && pair.Value != null && pair.Value.Length > 0)
                    pair.Key.sharedMaterials = pair.Value;
            foreach (var material in coloredMaterials.Values)
                if (material != null) Destroy(material);
            RestoreLiftMaterials();
            if (statusHealthyMaterial != null) Destroy(statusHealthyMaterial);
            if (statusWarningMaterial != null) Destroy(statusWarningMaterial);
            if (statusClosedMaterial != null) Destroy(statusClosedMaterial);
            coloredMaterials.Clear();
            originalSlopeMaterials.Clear();
            originalLiftMaterials.Clear();
        }

        private void RefreshSlopes()
        {
            var slopes = FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            var active = new HashSet<Renderer>();
            foreach (var slope in slopes)
            {
                var renderer = slope.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                active.Add(renderer);
                if (!originalSlopeMaterials.ContainsKey(renderer)) originalSlopeMaterials[renderer] = renderer.sharedMaterials;
                if (mode == ViewMode.ColoredSlopes)
                {
                    Material material;
                    if (!coloredMaterials.TryGetValue(slope.Difficulty, out material))
                    {
                        Color color;
                        switch (slope.Difficulty)
                        {
                            case SlopeDifficulty.Green: color = new Color(.08f, .66f, .22f); break;
                            case SlopeDifficulty.Blue: color = new Color(.08f, .36f, .94f); break;
                            case SlopeDifficulty.Red: color = new Color(.91f, .12f, .12f); break;
                            case SlopeDifficulty.Black: color = new Color(.09f, .10f, .13f); break;
                            default: color = new Color(.57f, .23f, .69f); break;
                        }
                        material = RuntimeMaterialFactory.Create(color);
                        coloredMaterials[slope.Difficulty] = material;
                    }
                    if (material != null && renderer.sharedMaterial != material) renderer.sharedMaterial = material;
                }
                else if (mode == ViewMode.DomainStatus)
                {
                    var map = DomainStatusMapRuntime366.I;
                    Material material = GetStatusMaterial(map != null ? map.GetSlopeStatus(slope) : DomainMapStatus366.Healthy);
                    if (material != null && renderer.sharedMaterial != material) renderer.sharedMaterial = material;
                }
                else
                {
                    Material[] originals = originalSlopeMaterials[renderer];
                    if (originals != null && originals.Length > 0 && renderer.sharedMaterial != originals[0]) renderer.sharedMaterials = originals;
                }
            }
            var stale = new List<Renderer>();
            foreach (var pair in originalSlopeMaterials) if (pair.Key == null) stale.Add(pair.Key);
            foreach (var renderer in stale) originalSlopeMaterials.Remove(renderer);
            RefreshLifts();
        }

        private Material GetStatusMaterial(DomainMapStatus366 status)
        {
            if (statusHealthyMaterial == null) statusHealthyMaterial = RuntimeMaterialFactory.Create(new Color(.10f, .72f, .28f));
            if (statusWarningMaterial == null) statusWarningMaterial = RuntimeMaterialFactory.Create(new Color(1f, .48f, .08f));
            if (statusClosedMaterial == null) statusClosedMaterial = RuntimeMaterialFactory.Create(new Color(.88f, .10f, .10f));
            if (status == DomainMapStatus366.Closed) return statusClosedMaterial;
            if (status == DomainMapStatus366.Warning) return statusWarningMaterial;
            return statusHealthyMaterial;
        }

        private void RefreshLifts()
        {
            var lifts = FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None);
            var alive = new HashSet<Renderer>();
            foreach (var lift in lifts)
            {
                if (lift == null) continue;
                Material statusMaterial = null;
                if (mode == ViewMode.DomainStatus)
                {
                    var map = DomainStatusMapRuntime366.I;
                    statusMaterial = GetStatusMaterial(map != null ? map.GetLiftStatus(lift) : DomainMapStatus366.Healthy);
                }
                foreach (var renderer in lift.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    alive.Add(renderer);
                    if (!originalLiftMaterials.ContainsKey(renderer)) originalLiftMaterials[renderer] = renderer.sharedMaterials;
                    if (mode == ViewMode.DomainStatus)
                    {
                        if (statusMaterial != null && renderer.sharedMaterial != statusMaterial) renderer.sharedMaterial = statusMaterial;
                    }
                    else
                    {
                        Material[] originals = originalLiftMaterials[renderer];
                        if (originals != null && originals.Length > 0 && renderer.sharedMaterial != originals[0]) renderer.sharedMaterials = originals;
                    }
                }
            }
            var stale = new List<Renderer>();
            foreach (var pair in originalLiftMaterials) if (pair.Key == null) stale.Add(pair.Key);
            foreach (var renderer in stale) originalLiftMaterials.Remove(renderer);
        }

        private void RestoreLiftMaterials()
        {
            foreach (var pair in originalLiftMaterials)
                if (pair.Key != null && pair.Value != null && pair.Value.Length > 0)
                    pair.Key.sharedMaterials = pair.Value;
        }
    }
}
