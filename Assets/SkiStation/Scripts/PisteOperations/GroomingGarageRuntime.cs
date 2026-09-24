using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Physical home for the grooming fleet. No fuel simulation: operations stay management-focused.</summary>
    public class GroomingGarageRuntime : MonoBehaviour
    {
        public static GroomingGarageRuntime I { get; private set; }
        public static readonly List<GroomingGarageRuntime> All = new List<GroomingGarageRuntime>();

        [Min(1)] public int vehicleCapacity = 4;
        public Transform spawnPoint;
        public bool playerPlaced = true;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); if (I == this) I = null; }

        public Vector3 ExitPoint => spawnPoint != null ? spawnPoint.position : transform.position;
        public int Capacity => Mathf.Max(1, vehicleCapacity);

        public void SetCapacity(int value) => vehicleCapacity = Mathf.Max(1, value);

        public Vector3 GetParkingPoint(int index)
        {
            int safe = Mathf.Max(0, index);
            int row = safe / 4;
            int col = safe % 4;
            return ExitPoint + transform.right * ((col - 1.5f) * 3.5f) + transform.forward * (-3f - row * 3f);
        }

        public float ScoreForSlope(SlopeRuntime slope)
        {
            return slope == null ? float.MaxValue : Vector3.Distance(ExitPoint, slope.BottomPoint);
        }

        public static GroomingGarageRuntime FindBestFor(SlopeRuntime slope)
        {
            GroomingGarageRuntime best = null;
            float distance = float.MaxValue;
            foreach (var garage in All)
            {
                if (garage == null) continue;
                float candidate = garage.ScoreForSlope(slope);
                if (candidate < distance) { distance = candidate; best = garage; }
            }
            return best;
        }
    }
}
