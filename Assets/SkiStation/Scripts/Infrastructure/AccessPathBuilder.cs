using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public class AccessPathBuilder : MonoBehaviour
    {
        public AccessRoadDefinition defaultRoad;
        public Material roadMaterial;
        public Material sidewalkMaterial;

        public AccessRoadRuntime CreateSegment(IReadOnlyList<Vector3> points, AccessRoadDefinition definition = null)
        {
            if (points == null || points.Count < 2) return null;
            definition = definition != null ? definition : defaultRoad;
            if (definition == null) return null;

            var go = new GameObject(definition.type == AccessRoadType.PedestrianPath ? "Sentier_Pieton" : "Route_Acces");
            go.transform.SetParent(transform, true);
            var runtime = go.AddComponent<AccessRoadRuntime>();
            runtime.definition = definition;

            var pointObjects = new List<Transform>();
            for (int i = 0; i < points.Count; i++)
            {
                var p = new GameObject("Point_" + i).transform;
                p.SetParent(go.transform, true); p.position = points[i]; pointObjects.Add(p);
            }
            runtime.SetPoints(pointObjects);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = AccessRoadMeshBuilder.Build(points, definition.width, definition.sidewalkWidth, definition.type != AccessRoadType.PedestrianPath && definition.hasSidewalk);
            mr.sharedMaterial = definition.type == AccessRoadType.PedestrianPath ? sidewalkMaterial : roadMaterial;
            var startNode = new GameObject("Node_Debut").AddComponent<AccessNetworkNode>();
            startNode.transform.SetParent(go.transform, true); startNode.transform.position = points[0];
            startNode.nodeType = definition.type == AccessRoadType.PedestrianPath ? AccessNodeType.PedestrianHub : AccessNodeType.Generic;
            var endNode = new GameObject("Node_Fin").AddComponent<AccessNetworkNode>();
            endNode.transform.SetParent(go.transform, true); endNode.transform.position = points[points.Count - 1];
            endNode.nodeType = definition.type == AccessRoadType.PedestrianPath ? AccessNodeType.PedestrianHub : AccessNodeType.Generic;
            AccessNetworkRuntime.I?.RegisterRoad(runtime); AccessNetworkRuntime.I?.RegisterNode(startNode); AccessNetworkRuntime.I?.RegisterNode(endNode);
            return runtime;
        }
    }
}
