using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Phase 36: passive movement audit. Does not change visitor routing or arcade mode.</summary>
    public sealed class SkierMovementAudit249 : MonoBehaviour
    {
        private struct Sample { public Vector3 position; public float time; public SkierState state; }
        private readonly Dictionary<int, Sample> previous = new Dictionary<int, Sample>();
        private readonly List<string> recent = new List<string>();
        private float nextSample;
        private bool visible;
        private int jumps;
        private int liftTransitions;
        private int skiTransitions;
        private int walkingCount, ridingCount, skiingCount, otherCount;
        private GUIStyle labelStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            if (FindFirstObjectByType<SkierMovementAudit249>() == null)
                new GameObject("SkiStation_2.49_PHASE36_MOVEMENT_AUDIT").AddComponent<SkierMovementAudit249>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12)) visible = !visible;
            if (Time.unscaledTime < nextSample) return;
            nextSample = Time.unscaledTime + .2f;
            var agents = FindObjectsByType<SkierRuntime>(FindObjectsSortMode.None);
            var active = new HashSet<int>();
            walkingCount = ridingCount = skiingCount = otherCount = 0;
            foreach (var agent in agents)
            {
                if (agent == null) continue;
                if (agent.State == SkierState.RidingLift) ridingCount++;
                else if (agent.State == SkierState.Skiing) skiingCount++;
                else otherCount++;
                int id = agent.GetInstanceID();
                active.Add(id);
                var now = new Sample { position = agent.transform.position, time = Time.unscaledTime, state = agent.State };
                Sample before;
                if (previous.TryGetValue(id, out before))
                {
                    float distance = Vector3.Distance(before.position, now.position);
                    float elapsed = Mathf.Max(.01f, now.time - before.time);
                    // Conservative threshold: flag only very large jumps, not ordinary fast skiing.
                    if (distance > 45f && distance / elapsed > 110f)
                    {
                        jumps++;
                        Add("SAUT " + agent.name + " : " + distance.ToString("F0") + " m | " + before.state + " -> " + now.state);
                    }
                    if (before.state != now.state)
                    {
                        if (now.state == SkierState.RidingLift) liftTransitions++;
                        if (now.state == SkierState.Skiing) skiTransitions++;
                    }
                }
                previous[id] = now;
            }
            var removed = new List<int>();
            foreach (var id in previous.Keys) if (!active.Contains(id)) removed.Add(id);
            foreach (var id in removed) previous.Remove(id);
        }

        private void Add(string message)
        {
            recent.Insert(0, message);
            if (recent.Count > 5) recent.RemoveAt(recent.Count - 1);
            Debug.LogWarning("SkiStation AUDIT DEPLACEMENTS : " + message);
        }

        private void OnGUI()
        {
            if (!visible) return;
            if (labelStyle == null) { labelStyle = new GUIStyle(GUI.skin.label); labelStyle.wordWrap = true; labelStyle.normal.textColor = Color.white; }
            GUILayout.BeginArea(new Rect(12, 115, 470, 215), GUI.skin.box);
            GUILayout.Label("DIAGNOSTIC VISITEURS — F12 pour fermer", labelStyle);
            GUILayout.Label("Agents suivis : " + previous.Count + " | Montees detectees : " + liftTransitions + " | Descentes detectees : " + skiTransitions, labelStyle);
            GUILayout.Label("En remontee : " + ridingCount + " | En descente : " + skiingCount + " | Autres etats : " + otherCount, labelStyle);
            GUILayout.Label("Deplacements brusques suspects : " + jumps + " (seuil >45 m et >110 m/s)", labelStyle);
            foreach (var message in recent) GUILayout.Label(message, labelStyle);
            GUILayout.EndArea();
        }
    }
}
