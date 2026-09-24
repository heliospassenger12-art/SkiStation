using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Management-only registry of confirmed building access paths.
    /// Consumers can follow the sampled points from the roadside to the entrance, or in reverse.
    /// Preview paths are deliberately never registered.</summary>
    public sealed class StationPedestrianNetwork249 : MonoBehaviour
    {
        public sealed class Access
        {
            public readonly GameObject Building;
            public readonly GameObject Visual;
            public readonly Vector3[] RoadToDoor;
            internal Access(GameObject building, GameObject visual, Vector3[] points)
            { Building = building; Visual = visual; RoadToDoor = points; }
        }

        public static StationPedestrianNetwork249 I { get; private set; }
        private readonly List<Access> accesses = new List<Access>();
        public IReadOnlyList<Access> Accesses => accesses;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }
        private void OnDestroy() { if (I == this) I = null; }

        public static StationPedestrianNetwork249 GetOrCreate()
        {
            if (I != null) return I;
            return new GameObject("SkiStation_249_ReseauPieton").AddComponent<StationPedestrianNetwork249>();
        }

        public void Register(GameObject building, GameObject visual, Vector3 entrance, Vector3 roadside)
        {
            if (building == null || visual == null) return;
            // Reconfirming an access must replace the old path rather than create duplicates.
            for (int i = accesses.Count - 1; i >= 0; i--)
                if (accesses[i].Building == building) accesses.RemoveAt(i);
            const int steps = 12;
            var points = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(roadside, entrance, i / (float)steps);
                p.y = SkiStationGuaranteedMountainVisual.Height(p.x, p.z) + .2f;
                points[i] = p;
            }
            accesses.Add(new Access(building, visual, points));
        }

        public bool TryGetAccess(GameObject building, out Vector3[] roadToDoor)
        {
            PruneDestroyed();
            foreach (var access in accesses)
                if (access.Building == building)
                { roadToDoor = (Vector3[])access.RoadToDoor.Clone(); return true; }
            roadToDoor = null;
            return false;
        }

        public void PruneDestroyed()
        {
            for (int i = accesses.Count - 1; i >= 0; i--)
                if (accesses[i].Building == null || accesses[i].Visual == null) accesses.RemoveAt(i);
        }
    }
}
