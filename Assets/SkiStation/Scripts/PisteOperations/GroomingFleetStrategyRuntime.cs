using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation.PisteOperations
{
    public enum GroomingPriorityMode { Automatic, ManualPriority }

    [Serializable]
    public struct GroomingFleetSnapshot
    {
        public int availableVehicles;
        public int activeVehicles;
        public int queuedOperations;
        public float estimatedQueueMinutes;
        public string prioritySlope;
    }

    /// <summary>High-level dispatch layer: multiple real groomers, garage capacity and manual priority.</summary>
    public class GroomingFleetStrategyRuntime : MonoBehaviour
    {
        public static GroomingFleetStrategyRuntime I { get; private set; }
        public GroomingPriorityMode mode = GroomingPriorityMode.Automatic;
        public int fleetSize = 2;
        public float garageCapacity = 4f;
        public float simulatedWorkMinutesPerOperation = 12f;
        public string manualPrioritySlopeId = "";
        readonly List<string> queue = new List<string>();
        float estimatedMinutes;

        int runtimeAvailable;
        int runtimeActive;
        int runtimeQueued;

        public GroomingFleetSnapshot Snapshot => new GroomingFleetSnapshot {
            availableVehicles = runtimeAvailable > 0 ? runtimeAvailable : Mathf.Max(0, fleetSize),
            activeVehicles = runtimeActive,
            queuedOperations = runtimeQueued > 0 ? runtimeQueued : queue.Count,
            estimatedQueueMinutes = estimatedMinutes,
            prioritySlope = manualPrioritySlopeId
        };

        public void SetRuntimeState(int available, int active, int queued) { runtimeAvailable = Mathf.Max(0, available); runtimeActive = Mathf.Max(0, active); runtimeQueued = Mathf.Max(0, queued); }

        void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; }
        void Update() { if (queue.Count > 0) estimatedMinutes = Mathf.Max(0f, estimatedMinutes - Time.deltaTime / 60f); }

        public void SetManualPriority(string slopeId) { mode = GroomingPriorityMode.ManualPriority; manualPrioritySlopeId = slopeId ?? ""; }
        public void ClearManualPriority() { mode = GroomingPriorityMode.Automatic; manualPrioritySlopeId = ""; }
        public void Enqueue(string slopeId) { if (string.IsNullOrEmpty(slopeId) || queue.Contains(slopeId)) return; queue.Add(slopeId); estimatedMinutes += simulatedWorkMinutesPerOperation; }
        public bool TryGetNext(out string slopeId) {
            slopeId = null;
            if (queue.Count == 0) return false;
            if (mode == GroomingPriorityMode.ManualPriority && queue.Contains(manualPrioritySlopeId)) { slopeId = manualPrioritySlopeId; queue.Remove(slopeId); return true; }
            slopeId = queue[0]; queue.RemoveAt(0); return true;
        }
    }
}
