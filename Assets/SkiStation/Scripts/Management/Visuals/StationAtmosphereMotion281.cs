using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.81 visual-only atmospheric life: chimney smoke and gently animated alpine banners.</summary>
    public sealed class StationAtmosphereMotion281 : MonoBehaviour
    {
        static StationAtmosphereMotion281 instance;
        GameObject root; float retry;
        Material smokeMat, redMat, blueMat, poleMat, snowMat;
        readonly List<Transform> flags = new List<Transform>();
        readonly List<float> flagPhase = new List<float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var g = new GameObject("SkiStation_281_Vie_Atmospherique_VISUEL");
            instance = g.AddComponent<StationAtmosphereMotion281>();
        }

        static Material Mat(Color c)
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            var m = new Material(s); m.color = c; return m;
        }

        static float H(float x, float z) => SkiStationGuaranteedMountainVisual.Height(x, z);

        GameObject Box(string n, Vector3 p, Vector3 sc, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = n; g.transform.SetParent(root.transform, false);
            g.transform.position = p; g.transform.localScale = sc;
            var c = g.GetComponent<Collider>(); if (c != null) Destroy(c);
            var r = g.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = m;
            return g;
        }

        void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (root == null)
            {
                if (Time.unscaledTime < retry) return; retry = Time.unscaledTime + 1f;
                var hub = ArrivalHub250.I; if (hub == null || !hub.Ready) return; Build(hub);
                return;
            }
            float t = Time.unscaledTime;
            for (int i = 0; i < flags.Count; i++)
            {
                var f = flags[i]; if (f == null) continue;
                float wave = Mathf.Sin(t * 1.35f + flagPhase[i]);
                f.localRotation = Quaternion.Euler(0f, wave * 5f, wave * 2.2f);
                var s = f.localScale; s.z = 1f + wave * .035f; f.localScale = s;
            }
        }

        void Build(ArrivalHub250 hub)
        {
            root = new GameObject("Ambiance_vivante_station_281_VISUEL"); root.transform.SetParent(transform, false);
            smokeMat = Mat(new Color(.72f, .76f, .78f, .38f));
            redMat = Mat(new Color(.66f, .08f, .07f, 1f)); blueMat = Mat(new Color(.08f, .25f, .48f, 1f));
            poleMat = Mat(new Color(.16f, .17f, .18f, 1f)); snowMat = Mat(new Color(.96f, .985f, 1f, 1f));

            // Sparse animated banners around the central arrival area: visible from medium zoom, no collision.
            AddFlag(hub.Point(-31f, 61f), redMat, 0f); AddFlag(hub.Point(31f, 63f), blueMat, 1.7f);
            AddFlag(hub.Point(-43f, 88f), blueMat, 3.1f); AddFlag(hub.Point(44f, 91f), redMat, 4.4f);

            // A few restrained smoke plumes suggest heated chalets/restaurants without touching building logic.
            AddSmoke(hub.Point(-27f, 76f), 8.2f, .2f);
            AddSmoke(hub.Point(25f, 82f), 9.0f, 1.8f);
            AddSmoke(hub.Point(-12f, 104f), 8.5f, 3.4f);

            Debug.Log("SkiStation 2.81: fumee de cheminees et bannieres alpines animees ajoutees; visuel uniquement, gameplay inchange.");
        }

        void AddFlag(Vector3 p, Material cloth, float phase)
        {
            float y = H(p.x, p.z);
            Box("Mat_banniere", new Vector3(p.x, y + 3.4f, p.z), new Vector3(.14f, 6.6f, .14f), poleMat);
            Box("Neige_mat", new Vector3(p.x, y + 6.78f, p.z), new Vector3(.42f, .10f, .42f), snowMat);
            var f = Box("Banniere_alpine", new Vector3(p.x + 1.15f, y + 5.45f, p.z), new Vector3(2.2f, 1.15f, .10f), cloth);
            flags.Add(f.transform); flagPhase.Add(phase);
        }

        void AddSmoke(Vector3 p, float baseHeight, float phase)
        {
            float y = H(p.x, p.z);
            var holder = new GameObject("Fumee_cheminee"); holder.transform.SetParent(root.transform, false);
            holder.transform.position = new Vector3(p.x, y + baseHeight, p.z);
            var ps = holder.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.startLifetime = 5.8f; main.startSpeed = .65f; main.startSize = .9f; main.maxParticles = 26; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.rateOverTime = 2.4f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 8f; shape.radius = .18f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.x = new ParticleSystem.MinMaxCurve(.18f + phase * .015f); vel.y = new ParticleSystem.MinMaxCurve(.18f);
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .45f, 1f, 1.7f));
            var col = ps.colorOverLifetime; col.enabled = true; var grad = new Gradient(); grad.SetKeys(new[]{new GradientColorKey(new Color(.78f,.80f,.81f),0f),new GradientColorKey(new Color(.88f,.90f,.91f),1f)},new[]{new GradientAlphaKey(.34f,0f),new GradientAlphaKey(0f,1f)}); col.color = grad;
            var r = holder.GetComponent<ParticleSystemRenderer>(); if (r != null) { r.sharedMaterial = smokeMat; r.renderMode = ParticleSystemRenderMode.Billboard; }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null; if (root != null) Destroy(root);
            foreach (var m in new[] { smokeMat, redMat, blueMat, poleMat, snowMat }) if (m != null) Destroy(m);
        }
    }
}
