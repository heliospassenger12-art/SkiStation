using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum ManagedZoneType
    {
        Beginner,
        Family,
        Sport,
        Snowpark,
        Nature
    }

    [Serializable]
    public sealed class ManagedZoneSnapshot
    {
        public string name;
        public ManagedZoneType type;
        public int points;
        public int slopesInside;
        public int liftsInside;
        public int visitorsInside;
        public bool physicalBoundary;
        public float specializationScore01;
        public string specializationLabel;
        public string dominantVisitorProfile;
        public int beginnerVisitors;
        public int familyVisitors;
        public int sportVisitors;
    }

    /// <summary>
    /// Player-defined physical management zones. The polygon is drawn in the world,
    /// its boundary is materialized with posts/rails/colliders, and the polygon can
    /// be queried by visitor/slope systems without requiring a dedicated terrain layer.
    /// </summary>
    public sealed class ManagedZoneRuntime : MonoBehaviour
    {
        public static ManagedZoneRuntime I { get; private set; }
        public IReadOnlyList<ManagedZoneSnapshot> Zones => snapshots;
        public event Action Changed;

        [SerializeField] private float fenceHeight = 1.15f;
        [SerializeField] private float postSpacing = 12f;
        [SerializeField] private float fenceThickness = .12f;
        private readonly List<ManagedZoneSnapshot> snapshots = new List<ManagedZoneSnapshot>();
        private readonly List<ZoneData> zones = new List<ZoneData>();

        [Serializable]
        private sealed class ZoneData
        {
            public string name;
            public ManagedZoneType type;
            public List<Vector3> points = new List<Vector3>();
            public GameObject root;
        }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool AddZone(ManagedZoneType type, IReadOnlyList<Vector3> worldPoints)
        {
            if (worldPoints == null || worldPoints.Count < 3) return false;
            var zone = new ZoneData { name = GetDefaultName(type), type = type };
            foreach (var p in worldPoints) zone.points.Add(ProjectToGround(p));
            if (zone.points.Count >= 3 && Vector3.Distance(zone.points[0], zone.points[zone.points.Count - 1]) < 2f)
                zone.points.RemoveAt(zone.points.Count - 1);
            if (zone.points.Count < 3) return false;
            zone.root = BuildPhysicalBoundary(zone);
            zones.Add(zone);
            Recalculate();
            return true;
        }

        public void ClearZones()
        {
            foreach (var zone in zones) if (zone.root != null) Destroy(zone.root);
            zones.Clear();
            Recalculate();
        }

        public bool IsPointInside(Vector3 worldPoint, ManagedZoneType type)
        {
            foreach (var zone in zones)
                if (zone.type == type && PointInPolygonXZ(worldPoint, zone.points)) return true;
            return false;
        }

        public bool IsPointInsideBeginnerZone(Vector3 worldPoint) => IsPointInside(worldPoint, ManagedZoneType.Beginner);

        public bool TryGetZoneData(string zoneName, out ManagedZoneType type, out List<Vector3> points)
        {
            foreach (var zone in zones)
            {
                if (zone.name != zoneName) continue;
                type = zone.type;
                points = zone.points;
                return true;
            }
            type = ManagedZoneType.Beginner; points = null; return false;
        }

        public bool SlopeTouchesZone(SlopeRuntime slope, ManagedZoneType type)
        {
            if (slope == null) return false;
            foreach (var p in slope.Points)
                if (IsPointInside(p, type)) return true;
            return false;
        }

        public float BeginnerSlopeBonus(SlopeRuntime slope, CustomerProfile profile)
        {
            if (slope == null || profile == CustomerProfile.Expert || profile == CustomerProfile.Athlete) return 0f;
            if (!SlopeTouchesZone(slope, ManagedZoneType.Beginner)) return 0f;
            float bonus = profile == CustomerProfile.Beginner ? .95f :
                          profile == CustomerProfile.Family ? .70f :
                          profile == CustomerProfile.Youth ? .65f : .18f;
            if (slope.Difficulty == SlopeDifficulty.Green) bonus += .55f;
            if (slope.Difficulty >= SlopeDifficulty.Red) bonus -= .45f;
            return bonus;
        }

        public void Recalculate()
        {
            snapshots.Clear();
            foreach (var zone in zones)
            {
                int slopes = 0, lifts = 0, visitors = 0;
                foreach (var slope in FindObjectsOfType<SlopeRuntime>())
                    if (slope != null && SlopeTouchesZone(slope, zone.type)) slopes++;
                foreach (var lift in FindObjectsOfType<LiftRuntime>())
                    if (lift != null && (PointInPolygonXZ(lift.Top, zone.points) || PointInPolygonXZ(lift.Bottom, zone.points))) lifts++;
                foreach (var skier in FindObjectsOfType<SkierRuntime>())
                    if (skier != null && skier.State != SkierState.Leaving && PointInPolygonXZ(skier.transform.position, zone.points)) visitors++;
                int beginner = 0, family = 0, sport = 0;
                foreach (var skier in FindObjectsOfType<SkierRuntime>())
                {
                    if (skier == null || skier.State == SkierState.Leaving || !PointInPolygonXZ(skier.transform.position, zone.points)) continue;
                    var profile = skier.PopulationProfile != null ? skier.PopulationProfile.ToCustomerProfile() : CustomerProfile.Tourist;
                    if (profile == CustomerProfile.Beginner || profile == CustomerProfile.Youth) beginner++;
                    if (profile == CustomerProfile.Family) family++;
                    if (profile == CustomerProfile.Athlete || profile == CustomerProfile.Expert || profile == CustomerProfile.Advanced) sport++;
                }
                string dominant = beginner >= family && beginner >= sport ? "Débutants" : family >= sport ? "Familles" : "Sportifs";
                float total = Mathf.Max(1, beginner + family + sport);
                float profileFit = Mathf.Max(beginner, Mathf.Max(family, sport)) / total;
                float equipmentBonus = 0f;
                if (ManagedZoneEquipmentRuntime.I != null)
                    foreach (var e in ManagedZoneEquipmentRuntime.I.Equipment)
                        if (e.zoneName == zone.type.ToString()) equipmentBonus += e.qualityBonus;
                float score = Mathf.Clamp01(profileFit * .65f + Mathf.Clamp01(equipmentBonus) * .35f);
                string label = zone.type == ManagedZoneType.Beginner ? "Débutants" :
                               zone.type == ManagedZoneType.Family ? "Familial" :
                               zone.type == ManagedZoneType.Sport ? "Sportif" :
                               zone.type == ManagedZoneType.Snowpark ? "Snowpark" : "Nature";
                if (score >= .75f) label += " — spécialisé";
                else if (score >= .48f) label += " — affirmé";
                snapshots.Add(new ManagedZoneSnapshot {
                    name = zone.name, type = zone.type, points = zone.points.Count,
                    slopesInside = slopes, liftsInside = lifts, visitorsInside = visitors, physicalBoundary = zone.root != null,
                    specializationScore01 = score, specializationLabel = label, dominantVisitorProfile = dominant,
                    beginnerVisitors = beginner, familyVisitors = family, sportVisitors = sport
                });
            }
            Changed?.Invoke();
        }

        private GameObject BuildPhysicalBoundary(ZoneData zone)
        {
            var root = new GameObject(zone.name + "_LimitesPhysiques");
            for (int i = 0; i < zone.points.Count; i++)
            {
                Vector3 a = zone.points[i];
                Vector3 b = zone.points[(i + 1) % zone.points.Count];
                float distance = Vector3.Distance(a, b);
                int segments = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(2f, postSpacing)));
                for (int s = 0; s < segments; s++)
                {
                    float t = s / (float)segments;
                    Vector3 p = Vector3.Lerp(a, b, t);
                    CreatePost(root.transform, p, zone.type);
                }
                CreateRail(root.transform, a, b, zone.type);
            }
            return root;
        }

        private void CreatePost(Transform parent, Vector3 ground, ManagedZoneType type)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Piquet_Zone_" + type;
            post.transform.SetParent(parent);
            post.transform.position = ground + Vector3.up * (fenceHeight * .5f);
            post.transform.localScale = new Vector3(.09f, fenceHeight * .5f, .09f);
            var collider = post.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = false;
        }

        private void CreateRail(Transform parent, Vector3 a, Vector3 b, ManagedZoneType type)
        {
            Vector3 mid = (a + b) * .5f + Vector3.up * (fenceHeight * .62f);
            Vector3 delta = b - a;
            delta.y = 0f;
            float length = delta.magnitude;
            if (length < .5f) return;
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Barriere_Zone_" + type;
            rail.transform.SetParent(parent);
            rail.transform.position = mid;
            rail.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            rail.transform.localScale = new Vector3(fenceThickness, .10f, length * .5f);
            var collider = rail.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = false;
        }

        private static Vector3 ProjectToGround(Vector3 p)
        {
            if (Physics.Raycast(p + Vector3.up * 250f, Vector3.down, out var hit, 600f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * .03f;
            return p;
        }

        private static bool PointInPolygonXZ(Vector3 p, List<Vector3> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector3 a = polygon[i], b = polygon[j];
                bool intersect = ((a.z > p.z) != (b.z > p.z)) &&
                    (p.x < (b.x - a.x) * (p.z - a.z) / Mathf.Max(.0001f, b.z - a.z) + a.x);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        private string GetDefaultName(ManagedZoneType type)
        {
            int n = 1;
            string prefix = type == ManagedZoneType.Beginner ? "Zone débutants" : "Zone " + type;
            foreach (var z in zones) if (z.type == type) n++;
            return prefix + " " + n;
        }
    }
}
