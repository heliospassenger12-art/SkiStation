using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Management-only visual prototype: pedestrians walk along confirmed building access paths.
    /// Does not spawn skiers or change the existing visitor economy/lifecycle.</summary>
    public sealed class StationBuildingPedestrians249 : MonoBehaviour
    {
        private sealed class Walker
        {
            public GameObject Visual;
            public Vector3[] Path;
            public int Segment;
            public float Progress;
            public bool Returning;
            public float Wait;
        }

        private readonly List<Walker> walkers = new List<Walker>();
        private readonly Dictionary<GameObject, float> nextVisit = new Dictionary<GameObject, float>();
        private Material pedestrianMaterial;
        private const float Speed = 1.8f;
        private const int MaxWalkers = 18;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            // Phase 8: no synthetic pedestrians; real arriving visitors use confirmed access paths.
            // The prototype component remains available for explicit debugging if needed.
        }

        private void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            var network = StationPedestrianNetwork249.I;
            if (network != null)
            {
                network.PruneDestroyed();
                foreach (var access in network.Accesses)
                {
                    if (walkers.Count >= MaxWalkers) break;
                    if (access.Building == null || access.RoadToDoor == null || access.RoadToDoor.Length < 2) continue;
                    float due;
                    if (nextVisit.TryGetValue(access.Building, out due) && Time.time < due) continue;
                    nextVisit[access.Building] = Time.time + Random.Range(9f, 18f);
                    Spawn(access.RoadToDoor);
                }
            }
            float remaining = Time.deltaTime;
            for (int i = walkers.Count - 1; i >= 0; i--)
            {
                Walker w = walkers[i];
                if (w.Visual == null) { walkers.RemoveAt(i); continue; }
                if (w.Wait > 0f) { w.Wait -= remaining; continue; }
                float distance = Speed * remaining;
                while (distance > 0f && w.Segment < w.Path.Length - 1)
                {
                    Vector3 a = w.Path[w.Segment], b = w.Path[w.Segment + 1];
                    float length = Vector3.Distance(a, b);
                    if (length < .001f) { w.Segment++; w.Progress = 0f; continue; }
                    float available = (1f - w.Progress) * length;
                    float move = Mathf.Min(distance, available);
                    w.Progress += move / length;
                    distance -= move;
                    w.Visual.transform.position = Vector3.Lerp(a, b, w.Progress) + Vector3.up * .9f;
                    if (w.Progress >= .9999f) { w.Segment++; w.Progress = 0f; }
                }
                if (w.Segment < w.Path.Length - 1) continue;
                if (!w.Returning)
                {
                    System.Array.Reverse(w.Path);
                    w.Segment = 0; w.Progress = 0f; w.Returning = true; w.Wait = Random.Range(3f, 7f);
                }
                else { Destroy(w.Visual); walkers.RemoveAt(i); }
            }
        }

        private void Spawn(Vector3[] path)
        {
            if (pedestrianMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) return;
                pedestrianMaterial = new Material(shader);
                pedestrianMaterial.color = new Color(.17f, .37f, .65f);
            }
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Pieton_visuel_acces_batiment";
            visual.transform.localScale = new Vector3(.65f, .9f, .65f);
            visual.transform.position = path[0] + Vector3.up * .9f;
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            visual.GetComponent<Renderer>().sharedMaterial = pedestrianMaterial;
            walkers.Add(new Walker { Visual = visual, Path = (Vector3[])path.Clone() });
        }

        private void OnDestroy()
        {
            foreach (var walker in walkers) if (walker.Visual != null) Destroy(walker.Visual);
            if (pedestrianMaterial != null) Destroy(pedestrianMaterial);
        }
    }
}
