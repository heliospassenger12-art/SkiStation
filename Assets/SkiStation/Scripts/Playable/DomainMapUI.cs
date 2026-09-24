using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Readable management map for the first playable SkiStation domain.</summary>
    public sealed class DomainMapUI : MonoBehaviour
    {
        public static DomainMapUI I { get; private set; }
        private bool visible;
        private Vector2 scroll;
        private GUIStyle title;
        private readonly List<SlopeRuntime> slopes = new List<SlopeRuntime>();

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) visible = !visible;
            if (Input.GetKeyDown(KeyCode.Escape)) visible = false;
            if (!visible) return;
            slopes.Clear();
            slopes.AddRange(Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None).OrderByDescending(s => s.PopularityScore));
        }

        public void Toggle() => visible = !visible;

        private void OnGUI()
        {
            if (!visible) return;
            if (title == null) title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };

            float w = Mathf.Min(760f, Screen.width - 30f);
            float h = Mathf.Min(600f, Screen.height - 30f);
            Rect area = new Rect(15f, 15f, w, h);
            GUILayout.BeginArea(area, GUI.skin.window);
            GUILayout.Label("CARTE DU DOMAINE", title);
            GUILayout.Label("Vue gestionnaire • M pour fermer • les données viennent de la simulation réelle");

            float mapH = Mathf.Min(390f, h - 165f);
            Rect map = GUILayoutUtility.GetRect(w - 25f, mapH, GUILayout.ExpandWidth(true));
            GUI.Box(map, "");
            DrawMap(map);

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(Mathf.Max(110f, h - mapH - 60f)));
            foreach (var s in slopes.Take(10))
            {
                GUILayout.Label($"{DifficultyLabel(s.Difficulty)}  {s.name}   •   {s.Length:0} m   •   {s.AverageAngle:0.0}°   •   {s.CompletedRuns} descentes   •   popularité {s.PopularityScore:0}");
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawMap(Rect r)
        {
            var all = slopes.SelectMany(s => s.Points).ToList();
            var lifts = Object.FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None).ToList();
            all.AddRange(lifts.SelectMany(l => l.Route));
            if (all.Count == 0) return;
            float minX = all.Min(p => p.x), maxX = all.Max(p => p.x), minZ = all.Min(p => p.z), maxZ = all.Max(p => p.z);
            float spanX = Mathf.Max(1f, maxX - minX), spanZ = Mathf.Max(1f, maxZ - minZ);

            foreach (var s in slopes)
            {
                for (int i = 1; i < s.Points.Count; i++)
                {
                    Vector2 a = Map(s.Points[i - 1], r, minX, spanX, minZ, spanZ);
                    Vector2 b = Map(s.Points[i], r, minX, spanX, minZ, spanZ);
                    DrawLine(a, b, DifficultyColor(s.Difficulty), 4f);
                }
            }
            foreach (var l in lifts)
            {
                for (int i = 1; i < l.Route.Count; i++)
                {
                    Vector2 a = Map(l.Route[i - 1], r, minX, spanX, minZ, spanZ);
                    Vector2 b = Map(l.Route[i], r, minX, spanX, minZ, spanZ);
                    DrawLine(a, b, new Color(.15f,.15f,.15f), 3f);
                }
            }
            GUI.Label(new Rect(r.x + 8, r.y + 8, 250, 22), "PISTES");
            GUI.Label(new Rect(r.x + 8, r.yMax - 28, 300, 22), "Vert / Bleu / Rouge / Noir / Extrême");
        }

        private static Vector2 Map(Vector3 p, Rect r, float minX, float spanX, float minZ, float spanZ)
            => new Vector2(r.x + 15f + (p.x - minX) / spanX * (r.width - 30f), r.yMax - 15f - (p.z - minZ) / spanZ * (r.height - 30f));

        private static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Matrix4x4 old = GUI.matrix;
            Color oldColor = GUI.color;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            float len = Vector2.Distance(a, b);
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - width * .5f, len, width), Texture2D.whiteTexture);
            GUI.matrix = old;
            GUI.color = oldColor;
        }

        private static string DifficultyLabel(SlopeDifficulty d) => d switch
        {
            SlopeDifficulty.Green => "🟢",
            SlopeDifficulty.Blue => "🔵",
            SlopeDifficulty.Red => "🔴",
            SlopeDifficulty.Black => "⚫",
            _ => "◆"
        };

        private static Color DifficultyColor(SlopeDifficulty d) => d switch
        {
            SlopeDifficulty.Green => new Color(.12f,.65f,.20f),
            SlopeDifficulty.Blue => new Color(.10f,.35f,.85f),
            SlopeDifficulty.Red => new Color(.80f,.12f,.10f),
            SlopeDifficulty.Black => new Color(.05f,.05f,.05f),
            _ => new Color(.50f,.10f,.55f)
        };
    }
}
