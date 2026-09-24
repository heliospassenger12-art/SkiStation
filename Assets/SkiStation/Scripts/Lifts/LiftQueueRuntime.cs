using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public class LiftQueueRuntime : MonoBehaviour
    {
        [SerializeField] private int maxVisibleQueue = 40;
        private readonly Queue<SkierRuntime> queue = new Queue<SkierRuntime>();

        private void PruneHead() { while (queue.Count > 0 && queue.Peek() == null) queue.Dequeue(); }
        public int Count { get { PruneHead(); return queue.Count; } }

        public bool Enqueue(SkierRuntime skier)
        {
            PruneHead();
            if (!VisitorSkiAdmission.CanSki(skier) || queue.Count >= maxVisibleQueue) return false;
            // The road-arrival controller owns the visitor until equipment and arrival are complete.
            var arrival = skier.GetComponent<StationVisitorRoadArrival248>();
            if (arrival != null && arrival.IsArriving) return false;
            if (skier.State == SkierState.RidingLift || skier.State == SkierState.WaitingForLift) return false;
            foreach (var queued in queue) if (queued == skier) return false;
            var station = GetComponent<LiftRuntime>();
            if (station != null && (!station.Open || station.Faulted || Vector3.Distance(skier.transform.position, station.Bottom) > 4f)) return false;
            queue.Enqueue(skier);
            skier.SetState(SkierState.WaitingForLift);
            return true;
        }

        public SkierRuntime Peek()
        {
            PruneHead();
            return queue.Count > 0 ? queue.Peek() : null;
        }

        public SkierRuntime Dequeue()
        {
            PruneHead();
            return queue.Count > 0 ? queue.Dequeue() : null;
        }

        public SkierRuntime GetAt(int index)
        {
            if (index < 0 || index >= queue.Count) return null;
            int i = 0;
            foreach (var skier in queue)
            {
                if (i++ == index) return skier;
            }
            return null;
        }
    }
}
