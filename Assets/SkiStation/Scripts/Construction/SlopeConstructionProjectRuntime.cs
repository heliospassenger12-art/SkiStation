using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum SlopeConstructionProjectState { Planned, Building, Completed, Cancelled }

    /// <summary>Real piste construction project. Cost is paid once at launch; instant mode completes immediately.</summary>
    public sealed class SlopeConstructionProjectRuntime : MonoBehaviour
    {
        public SlopeRuntime Slope { get; private set; }
        public SlopeConstructionProjectState State { get; private set; } = SlopeConstructionProjectState.Planned;
        public float Cost { get; private set; }
        public float DurationGameMinutes { get; private set; }
        public float Progress01 { get; private set; }
        public bool IsFinished => State == SlopeConstructionProjectState.Completed;
        public string ProjectId { get; private set; }

        LineRenderer workLine;
        readonly List<GameObject> equipment = new();
        float elapsedGameMinutes;
        float lastGameHour = -1f;
        int lastGameDay = -1;

        public static SlopeConstructionProjectRuntime StartProject(SlopeRuntime slope, float cost, float durationMinutes)
        {
            if (slope == null) return null;
            var go = new GameObject("PisteConstruction_" + slope.name);
            var p = go.AddComponent<SlopeConstructionProjectRuntime>();
            p.Begin(slope, cost, durationMinutes);
            return p;
        }

        public void Begin(SlopeRuntime slope, float cost, float durationMinutes)
        {
            Slope = slope;
            Cost = Mathf.Max(0f, cost);
            DurationGameMinutes = Mathf.Max(1f, durationMinutes);
            ProjectId = slope.name + "_Construction";
            State = SlopeConstructionProjectState.Building;
            Progress01 = 0f;
            slope.SetOpen(false);
            BuildVisuals();

            if (ConstructionPolicyRuntime.I != null && ConstructionPolicyRuntime.I.InstantConstruction)
                Complete();
        }

        void Update()
        {
            if (State != SlopeConstructionProjectState.Building) return;
            if (ConstructionPolicyRuntime.I != null && ConstructionPolicyRuntime.I.InstantConstruction)
            {
                Complete();
                return;
            }

            var clock = GameClock.I;
            if (clock == null)
            {
                elapsedGameMinutes += Time.deltaTime;
            }
            else
            {
                float hour = clock.CurrentHour;
                int day = clock.CurrentDay;
                if (lastGameDay < 0) { lastGameDay = day; lastGameHour = hour; }
                float delta = (day - lastGameDay) * 24f * 60f + (hour - lastGameHour) * 60f;
                if (delta < 0f) delta = 0f;
                elapsedGameMinutes += Mathf.Min(delta, 120f);
                lastGameDay = day; lastGameHour = hour;
            }

            Progress01 = Mathf.Clamp01(elapsedGameMinutes / DurationGameMinutes);
            UpdateVisuals();
            if (Progress01 >= 1f) Complete();
        }

        public void Complete()
        {
            if (State == SlopeConstructionProjectState.Completed) return;
            State = SlopeConstructionProjectState.Completed;
            Progress01 = 1f;
            if (Slope != null)
            {
                Slope.SetOpen(true);
                Slope.SetGroomed(false);
            }
            UpdateVisuals();
            ClearEquipment();
        }

        public void Cancel()
        {
            if (State == SlopeConstructionProjectState.Completed) return;
            State = SlopeConstructionProjectState.Cancelled;
            if (Slope != null) Slope.SetOpen(false);
            ClearEquipment();
        }

        void BuildVisuals()
        {
            if (Slope == null || Slope.Points.Count < 2) return;
            workLine = gameObject.AddComponent<LineRenderer>();
            workLine.useWorldSpace = true;
            workLine.widthMultiplier = 0.45f;
            workLine.material = new Material(Shader.Find("Sprites/Default"));
            workLine.positionCount = Slope.Points.Count;
            for (int i = 0; i < Slope.Points.Count; i++) workLine.SetPosition(i, Slope.Points[i] + Vector3.up * 0.35f);

            CreateMachine("Bulldozer", 0.2f);
            CreateMachine("SnowGroomer", 0.55f);
            CreateMachine("Dumper", 0.82f);
            UpdateVisuals();
        }

        void CreateMachine(string label, float t)
        {
            if (Slope == null || Slope.Points.Count < 2) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Chantier_" + label;
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(2.2f, 1.1f, 3.2f);
            var marker = go.AddComponent<ConstructionMachineMarker>();
            marker.progressPosition = t;
            equipment.Add(go);
        }

        void UpdateVisuals()
        {
            if (Slope == null || Slope.Points.Count < 2) return;
            for (int i = 0; i < equipment.Count; i++)
            {
                var marker = equipment[i].GetComponent<ConstructionMachineMarker>();
                float t = Mathf.Clamp01(marker.progressPosition + Progress01 * 0.15f);
                int idx = Mathf.Clamp(Mathf.RoundToInt(t * (Slope.Points.Count - 1)), 0, Slope.Points.Count - 1);
                equipment[i].transform.position = Slope.Points[idx] + Vector3.up * 1.0f;
                if (idx + 1 < Slope.Points.Count) equipment[i].transform.forward = (Slope.Points[idx + 1] - Slope.Points[idx]).normalized;
            }
            if (workLine != null) workLine.enabled = State == SlopeConstructionProjectState.Building;
        }

        void ClearEquipment()
        {
            foreach (var go in equipment) if (go) Destroy(go);
            equipment.Clear();
            if (workLine) workLine.enabled = false;
        }
    }

    public sealed class ConstructionMachineMarker : MonoBehaviour
    {
        [HideInInspector] public float progressPosition;
    }
}
