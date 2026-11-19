using System;
using System.Collections.Generic;
using SkiStation.AlpineEmpires;
using SkiStation.Arcade;
using UnityEngine;

namespace SkiStation
{
    [Serializable]
    internal sealed class AlpinePedestrianCheckpoint261
    {
        public string buildingId;
        public int sceneryIndex;
        public Vector3[] points;
    }

    [Serializable]
    internal sealed class AlpineMaterialCheckpoint261
    {
        public AlpineSessionSave world;
        public string roadSha256;
        public List<AlpinePedestrianCheckpoint261> accesses=new List<AlpinePedestrianCheckpoint261>();

        [Serializable]
        private sealed class RoadIdentity
        {
            public Vector3[] points;
        }

        internal static StationRoadNetwork247 RequireRoad(ManagementDomainSource261 source)
        {
            var road=StationRoadNetwork247.I;
            if (source.gameObject.scene.name!=DemoFlow263.HomeScene || road==null || !road.IsReady || !source.Contains(road))
                throw new NotSupportedException("Route permanente de la scène Alpine Empires introuvable.");
            if (road.Branches.Count!=0)
                throw new NotSupportedException("Routes secondaires présentes : leur sauvegarde n’est pas encore prise en charge.");
            return road;
        }

        internal static string RoadHash(StationRoadNetwork247 road)
            => ManagementDomainFileStore261.Hash(JsonUtility.ToJson(new RoadIdentity{points=road.GetPermanentRoadPath()}));

        internal static List<StationPedestrianNetwork249.Access> Paths(ManagementDomainSource261 source)
        {
            var result=new List<StationPedestrianNetwork249.Access>();
            var network=StationPedestrianNetwork249.I;
            if (network==null) return result;
            foreach (var path in network.Accesses)
            {
                if (path.Building==null || path.Visual==null) continue;
                var identity=path.Building.GetComponent<ManagementElementIdentity261>();
                if (identity!=null&&source.Contains(identity)) result.Add(path);
                else if (source.Contains(path.Visual.transform))
                    throw new NotSupportedException("Accès piéton dans le domaine sans propriétaire sérialisable.");
            }
            return result;
        }

        internal static HashSet<Transform> SkipScenery(StationRoadNetwork247 road,List<StationPedestrianNetwork249.Access> paths)
        {
            var result=new HashSet<Transform>{road.transform};
            foreach (var path in paths) result.Add(path.Visual.transform);
            return result;
        }

        [Serializable]
        private sealed class SavedServiceIdentity
        {
            public string serviceId;
        }

        internal static AlpineMaterialCheckpoint261 Read(ManagementSaveData261 data)
        {
            if (data==null||data.buildings==null||data.scenery==null)
                throw new ArgumentException("Collections du checkpoint indisponibles.",nameof(data));
            if (data.schema!=ManagementSaveData261.AlpineSchema)
            {
                if (!string.IsNullOrEmpty(data.alpineMaterial) || data.buildings.Exists(b=>HasOwnership(b)))
                    throw new ArgumentException("Données Alpine Empires dans un checkpoint Gestion classique.");
                return null;
            }
            if (data.sourceScene!=DemoFlow263.HomeScene || !data.hasStation || data.finance==null || data.traffic391!=null
                || string.IsNullOrEmpty(data.alpineMaterial) || data.alpineMaterial.Length>8*1024*1024)
                throw new ArgumentException("Checkpoint Alpine Empires incomplet.");
            var value=JsonUtility.FromJson<AlpineMaterialCheckpoint261>(data.alpineMaterial);
            if (value==null || value.roadSha256==null || value.roadSha256.Length!=64
                || value.accesses==null || value.accesses.Count>data.buildings.Count)
                throw new ArgumentException("Réseau Alpine Empires invalide.");
            foreach(char digit in value.roadSha256)
                if ((digit<'0'||digit>'9')&&(digit<'a'||digit>'f'))throw new ArgumentException("Empreinte de route invalide.");
            var world=AlpineSession.RestoreWorld(value.world);
            var buildingIds=new HashSet<string>(StringComparer.Ordinal);
            var alliedServices=new HashSet<string>(StringComparer.Ordinal);
            foreach (var building in data.buildings)
            {
                if (building==null||building.visual==null||!buildingIds.Add(building.id))
                    throw new ArgumentException("Bâtiment Alpine Empires invalide.");
                if (!HasOwnership(building)) continue;
                if (!world.TryGetManager(building.alpineCompany,out var company))
                    throw new ArgumentException("Entreprise du bâtiment introuvable.");
                var territory=world.Inspect(company.ManagerId,company.Id,building.alpineTerritory);
                var point=building.visual.position;
                if (territory==null || territory.OwnerId!=company.Id || !territory.Buildable
                    || string.IsNullOrEmpty(building.service) || point.x-8f<territory.Bounds.minX || point.x+8f>territory.Bounds.maxX
                    || point.z-8f<territory.Bounds.minZ || point.z+8f>territory.Bounds.maxZ)
                    throw new ArgumentException("Propriété de bâtiment Alpine Empires invalide.");
                if (!company.IsHuman)
                {
                    var serviceId=JsonUtility.FromJson<SavedServiceIdentity>(building.service)?.serviceId;
                    if (!world.MatchesServiceSite(company.Id,building.alpineTerritory,serviceId,point.x,point.z) || !alliedServices.Add(serviceId))
                        throw new ArgumentException("Chantier allié sans reçu unique.");
                }
            }
            if (alliedServices.Count!=world.BuiltServiceCount)
                throw new ArgumentException("Bâtiment allié payé absent du checkpoint.");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            var scenery=new HashSet<int>();
            foreach (var path in value.accesses)
            {
                if (path==null || !buildingIds.Contains(path.buildingId) || !seen.Add(path.buildingId)
                    || path.sceneryIndex<0 || path.sceneryIndex>=data.scenery.Count || !scenery.Add(path.sceneryIndex)
                    || path.points==null || path.points.Length!=13)
                    throw new ArgumentException("Accès piéton Alpine Empires invalide.");
                foreach (var point in path.points)
                    if (!ManagementDomainSource261.ValidPosition(point))throw new ArgumentException("Point piéton invalide.");
            }
            return value;
        }

        private static bool HasOwnership(ManagementSavedBuilding261 building)
            => building!=null&&(!string.IsNullOrEmpty(building.alpineCompany)||!string.IsNullOrEmpty(building.alpineTerritory));
    }
}
