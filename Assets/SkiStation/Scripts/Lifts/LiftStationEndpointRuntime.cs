using UnityEngine;

namespace SkiStation
{
    public enum LiftStationEndpointKind { Bottom, Top }

    /// <summary>Runtime anchor for one end of a lift: queue/boarding at bottom, unloading at top.</summary>
    public sealed class LiftStationEndpointRuntime : MonoBehaviour
    {
        public LiftStationEndpointKind Kind { get; private set; }
        public LiftRuntime Lift { get; private set; }
        public Transform QueuePoint { get; private set; }
        public Transform BoardingPoint { get; private set; }
        public Transform UnloadingPoint { get; private set; }
        public bool IsBottom => Kind == LiftStationEndpointKind.Bottom;

        public void Configure(LiftRuntime lift, LiftStationEndpointKind kind, Vector3 worldPosition)
        {
            Lift = lift;
            Kind = kind;
            transform.position = worldPosition;
            transform.rotation = lift != null ? lift.transform.rotation : Quaternion.identity;
            QueuePoint = GetOrCreate("QueuePoint", Vector3.forward * 2.5f);
            BoardingPoint = GetOrCreate("BoardingPoint", Vector3.zero);
            UnloadingPoint = GetOrCreate("UnloadingPoint", Vector3.forward * 3.5f + Vector3.up * .1f);
        }

        private Transform GetOrCreate(string childName, Vector3 localPosition)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            child.localPosition = localPosition;
            return child;
        }
    }
}
