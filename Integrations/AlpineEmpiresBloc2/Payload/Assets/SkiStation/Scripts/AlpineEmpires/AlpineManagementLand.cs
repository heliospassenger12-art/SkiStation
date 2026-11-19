using SkiStation.Altitude;
using SkiStation.Arcade;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkiStation.AlpineEmpires
{
    internal static class AlpineManagementLand
    {
        private const string RegionId = "bootstrap-front-de-neige";

        internal static bool TryCreateSession(out AlpineSession session, out string reason)
        {
            session = null;
            reason = "Le front de neige n’est pas prêt. Relancez depuis SkiStation_Bootstrap.";
            if (SceneManager.GetActiveScene().name != DemoFlow263.HomeScene ||
                StationRoadNetwork247.I == null || !StationRoadNetwork247.I.IsReady)
                return false;

            Vector3 entrance = ManagementSurface.Entrance;
            var road = StationRoadNetwork247.I.GetPermanentRoadPath();
            float alliedMinZ = Mathf.Max(-800f, road[0].z - entrance.z + 20f);
            bool alliedRoadPlot = alliedMinZ <= -380f;
            if (!ManagementSurface.TryGet(entrance, out var ground)) return false;
            var reference = WorldAltitudeService.Reference();
            float altitude = reference.ToMeters(ground.y);
            if (!AlpineAltitudeRules.Finite(altitude)) return false;

            // Fixed test concessions around the real road end. Distant slots reserve land only.
            var sectors = new[]
            {
                Sector("start-1", -320f, 320f, -300f, 760f, 0f, true, "company-1"),
                alliedRoadPlot
                    ? Sector("start-2", -320f, 320f, alliedMinZ, -300f, 0f, true, "company-2")
                    : Sector("start-2", -1660f, -1020f, -420f, 760f, 0f, true, "company-2"),
                Sector("start-3", 1020f, 1660f, -420f, 760f, 0f, true, "company-3"),
                Sector("start-4", 1800f, 2440f, -420f, 760f, 0f, true, "company-4"),
                Sector("expansion-1", -670f, -320f, -420f, 760f, 50000f, true),
                Sector("expansion-2", 320f, 670f, -420f, 760f, 100000f, true),
                Sector("protected-1", -320f, 320f, 760f, 1120f, 0f, false),
            };
            foreach (var sector in sectors)
            {
                var box = sector.Bounds;
                if (!box.Valid || box.minX < -AlpineMassifGeneratorV2.HalfWidth ||
                    box.maxX > AlpineMassifGeneratorV2.HalfWidth ||
                    box.minZ < AlpineMassifGeneratorV2.MinZ || box.maxZ > AlpineMassifGeneratorV2.HalfSize)
                {
                    reason = "Les concessions de cette avant-première dépassent les limites du massif.";
                    return false;
                }
            }

            session = new AlpineSession(1, AlpineTeamMode.EmpirePartage, sectors);
            reason = "";
            return true;

            AlpineTerritory Sector(string id, float minX, float maxX, float minZ, float maxZ,
                float price, bool buildable, string owner = null)
            {
                var footprint = new AlpineFootprint(entrance.x + minX, entrance.x + maxX,
                    entrance.z + minZ, entrance.z + maxZ, reference.constructionMinAltitude,
                    Mathf.Min(reference.constructionMaxAltitude, AlpineAltitudeRules.AbsoluteConstructionMaxAltitude));
                return new AlpineTerritory(id, footprint, price, buildable, owner, RegionId);
            }
        }

        internal static AlpineFootprint ServiceFootprint(Vector3 point, Vector2 size)
        {
            // The native site validator checks the altitude of all terrain samples. Here only the
            // horizontal concession boundary and the altitude of the supplied origin are added.
            float altitude = WorldAltitudeService.GetAltitudeMeters(point);
            return new AlpineFootprint(point.x - size.x / 2f, point.x + size.x / 2f,
                point.z - size.y / 2f, point.z + size.y / 2f, altitude, altitude);
        }
    }
}
