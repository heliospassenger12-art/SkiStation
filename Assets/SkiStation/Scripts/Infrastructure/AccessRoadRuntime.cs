using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public class AccessRoadRuntime : MonoBehaviour
    {
        public AccessRoadDefinition definition;
        [SerializeField] private List<Transform> controlPoints = new List<Transform>();
        public IReadOnlyList<Transform> ControlPoints => controlPoints;
        public float LengthMeters { get; private set; }

        public void SetPoints(IEnumerable<Transform> points)
        {
            controlPoints.Clear();
            if (points != null) controlPoints.AddRange(points);
            RecalculateLength();
        }

        public void RecalculateLength()
        {
            LengthMeters = 0f;
            for (int i = 1; i < controlPoints.Count; i++)
                if (controlPoints[i - 1] != null && controlPoints[i] != null)
                    LengthMeters += Vector3.Distance(controlPoints[i - 1].position, controlPoints[i].position);
        }

        public bool IsVehicleRoad => definition != null && definition.type != AccessRoadType.PedestrianPath;
        public bool IsPedestrianAccessible => !IsVehicleRoad || HasSidewalk;
        public bool HasSidewalk => definition != null && definition.hasSidewalk && definition.sidewalkWidth > 0f;
        public float TotalWidth => definition == null ? 0f : definition.width + (HasSidewalk ? definition.sidewalkWidth * 2f : 0f);
    }
}
