using UnityEngine;
namespace SkiStation
{
    // Shared geometry rules for first approach, upper handoff and safe departure.
    public static class ManagementJourneyAccess
    {
        public const float LiftApproachRadius = 600f;
        public const float SlopeConnectionRadius = 90f;
        public static bool CanWalk(Vector3 from, Vector3 to, float radius)
        {
            float distance = Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));
            if (distance > radius) return false;
            if (!ManagementSurface.TryGet(from, out var previous)) return false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / 10f));
            for (int i = 1; i <= steps; i++)
            {
                if (!ManagementSurface.TryGet(Vector3.Lerp(from, to, i / (float)steps), out var next)) return false;
                float horizontal = Vector2.Distance(new Vector2(previous.x, previous.z), new Vector2(next.x, next.z));
                if (Mathf.Abs(next.y - previous.y) > Mathf.Max(.5f, horizontal * .25f)) return false;
                previous = next;
            }
            return true;
        }
        public static bool CanExitToVillage(Vector3 position)
        {
            Vector3 entrance = ManagementSurface.Entrance;
            return Mathf.Abs(position.y - entrance.y) <= 20f && CanWalk(position, entrance, 1200f);
        }
        public static SlopeRuntime SlopeForLift(LiftRuntime lift)
        {
            if (lift == null) return null;
            SlopeRuntime best = null;
            float gap = SlopeConnectionRadius;
            foreach (var slope in Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
            {
                if (slope == null || !slope.Open || slope.Points.Count < 2 || !slope.name.StartsWith("Piste_")) continue;
                float distance = Vector3.Distance(lift.Top, slope.TopPoint);
                if (distance > gap) continue;
                gap = distance;
                best = slope;
            }
            return best;
        }
    }
}
