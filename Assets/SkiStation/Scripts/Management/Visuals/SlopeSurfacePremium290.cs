using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.90 visual-only piste surface finish: groomed lanes and subtle ski traces following real built slopes.</summary>
    public sealed class SlopeSurfacePremium290 : MonoBehaviour
    {
        static SlopeSurfacePremium290 instance;
        readonly HashSet<int> done = new HashSet<int>();
        readonly List<LodGroup> groups = new List<LodGroup>();
        Material groom, trace;
        float nextScan, nextLod;
        sealed class LodGroup { public GameObject medium, close; public Vector3 anchor; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var go = new GameObject("SkiStation_290_Surface_Pistes_Premium_VISUEL");
            instance = go.AddComponent<SlopeSurfacePremium290>();
        }

        void Start()
        {
            groom = Mat(new Color(.82f,.89f,.94f,1f), .12f);
            trace = Mat(new Color(.67f,.77f,.84f,1f), .08f);
        }

        static Material Mat(Color c, float smooth)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh) { color = c };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            return m;
        }

        void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + 3f; Scan(); }
            if (Time.unscaledTime >= nextLod) { nextLod = Time.unscaledTime + .45f; UpdateLod(); }
        }

        void Scan()
        {
            foreach (var s in FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
            {
                if (s == null || s.Points == null || s.Points.Count < 2 || !s.name.StartsWith("Piste_") || !done.Add(s.GetInstanceID())) continue;
                Decorate(s);
            }
        }

        void Decorate(SlopeRuntime s)
        {
            var medium = new GameObject("290_Damage_Dameuse_" + s.name); medium.transform.SetParent(transform, true);
            var close = new GameObject("290_Traces_Skis_" + s.name); close.transform.SetParent(transform, true);
            int step = Mathf.Max(1, s.Points.Count / 42);
            float half = Mathf.Clamp(s.AverageWidth * .24f, 1.4f, 3.5f);

            for (int i = 0; i < s.Points.Count - step; i += step)
            {
                int j = Mathf.Min(i + step, s.Points.Count - 1);
                Vector3 a = s.Points[i], b = s.Points[j];
                Vector3 flat = Vector3.ProjectOnPlane(b - a, Vector3.up);
                if (flat.sqrMagnitude < .05f) continue;
                Vector3 dir = flat.normalized, side = Vector3.Cross(Vector3.up, dir).normalized;
                Vector3 mid = (a + b) * .5f + Vector3.up * .035f;
                float len = Vector3.Distance(a, b) * .94f;

                // Sparse parallel groom bands read as a prepared piste from management-camera distance.
                for (int k = -2; k <= 2; k++)
                    Box(medium.transform, "Dameuse", mid + side * (half * k / 2f), new Vector3(.12f, .025f, len), groom, Quaternion.LookRotation(dir, Vector3.up));

                // Close-range paired ski traces, deliberately sparse and visual only.
                if (((i / step) & 1) == 0)
                {
                    float wiggle = Mathf.Sin(i * .73f) * Mathf.Min(half * .55f, 1.25f);
                    for (int k = -1; k <= 1; k += 2)
                        Box(close.transform, "TraceSki", mid + side * (wiggle + k * .13f) + Vector3.up * .012f,
                            new Vector3(.035f, .018f, len * .78f), trace, Quaternion.LookRotation(dir, Vector3.up));
                }
            }
            groups.Add(new LodGroup { medium = medium, close = close, anchor = s.Points[s.Points.Count / 2] });
        }

        GameObject Box(Transform parent, string n, Vector3 pos, Vector3 scale, Material mat, Quaternion rot)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = n; g.transform.SetParent(parent, true);
            g.transform.position = pos; g.transform.rotation = rot; g.transform.localScale = scale;
            var c = g.GetComponent<Collider>(); if (c != null) Destroy(c);
            var r = g.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = mat;
            return g;
        }

        void UpdateLod()
        {
            var cam = Camera.main; if (cam == null) return;
            foreach (var g in groups)
            {
                float d = Vector3.Distance(cam.transform.position, g.anchor);
                if (g.medium != null && g.medium.activeSelf != (d < 760f)) g.medium.SetActive(d < 760f);
                if (g.close != null && g.close.activeSelf != (d < 260f)) g.close.SetActive(d < 260f);
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (groom != null) Destroy(groom); if (trace != null) Destroy(trace);
        }
    }
}
