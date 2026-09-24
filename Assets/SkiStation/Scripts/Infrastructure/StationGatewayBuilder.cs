using UnityEngine;

namespace SkiStation
{
    public class StationGatewayBuilder : MonoBehaviour
    {
        public StationGatewayDefinition definition;
        public StationEntranceDefinition entranceDefinition;
        public AccessRoadDefinition roadDefinition;
        public Material roadMaterial;
        public Material sidewalkMaterial;
        public Material signMaterial;

        public StationGatewayRuntime BuildGateway(Vector3 position, Quaternion rotation)
        {
            var def = definition != null ? definition : ScriptableObject.CreateInstance<StationGatewayDefinition>();
            var root = new GameObject(string.IsNullOrEmpty(def.stationName) ? "StationGateway" : "StationGateway_" + def.stationName);
            root.transform.SetPositionAndRotation(position, rotation);

            var gateway = root.AddComponent<StationGatewayRuntime>();
            var entrance = root.AddComponent<StationEntranceRuntime>();
            root.AddComponent<StationEntranceAccessNode>();

            var external = CreatePoint(root.transform, "Spawn_Exterieur", -Vector3.forward * def.externalSpawnDistance);
            var arrival = CreatePoint(root.transform, "Portail_Arrivee", Vector3.zero);
            var dropOff = CreatePoint(root.transform, "Depose_Minute", Vector3.forward * (def.gatewayDepth + 6f));
            var parking = CreatePoint(root.transform, "Cible_Parking", Vector3.forward * (def.gatewayDepth + 18f) + Vector3.right * 10f);
            var pedestrian = CreatePoint(root.transform, "Cible_Pietons", Vector3.forward * (def.gatewayDepth + 8f) + Vector3.right * 6f);

            gateway.Configure(def, external, arrival, dropOff, parking, pedestrian);
            entrance.Configure(entranceDefinition, arrival, dropOff);

            BuildRoad(root.transform, external.position, arrival.position);
            BuildGatewayVisual(root.transform, def);
            BuildAccessNodes(root.transform, arrival.position, parking.position, pedestrian.position);

            return gateway;
        }

        private Transform CreatePoint(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private void BuildRoad(Transform root, Vector3 start, Vector3 end)
        {
            var builder = root.gameObject.AddComponent<AccessPathBuilder>();
            builder.defaultRoad = roadDefinition;
            builder.roadMaterial = roadMaterial;
            builder.sidewalkMaterial = sidewalkMaterial;
            builder.CreateSegment(new[] { start, Vector3.Lerp(start, end, .55f) + Vector3.up * 0.25f, end }, roadDefinition);
        }

        private void BuildGatewayVisual(Transform root, StationGatewayDefinition def)
        {
            if (def.visualStyle == StationGatewayVisualStyle.SignOnly) return;

            var arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arch.name = "Portique_Entree";
            arch.transform.SetParent(root, false);
            arch.transform.localPosition = Vector3.forward * (def.gatewayDepth * .45f) + Vector3.up * 4f;
            arch.transform.localScale = new Vector3(14f, 1f, 1f);

            var left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.name = "Poteau_Gauche";
            left.transform.SetParent(root, false);
            left.transform.localPosition = new Vector3(-6.5f, 2f, def.gatewayDepth * .45f);
            left.transform.localScale = new Vector3(1f, 4f, 1f);

            var right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.name = "Poteau_Droit";
            right.transform.SetParent(root, false);
            right.transform.localPosition = new Vector3(6.5f, 2f, def.gatewayDepth * .45f);
            right.transform.localScale = new Vector3(1f, 4f, 1f);

            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Enseigne_Nom_Station";
            sign.transform.SetParent(root, false);
            sign.transform.localPosition = Vector3.forward * (def.gatewayDepth * .45f) + Vector3.up * 5.6f;
            sign.transform.localScale = new Vector3(11f, 2f, .35f);
            if (signMaterial != null) sign.GetComponent<Renderer>().sharedMaterial = signMaterial;
        }

        private void BuildAccessNodes(Transform root, Vector3 arrival, Vector3 parking, Vector3 pedestrian)
        {
            CreateNode(root, "Parking_Node", parking, AccessNodeType.Parking);
            CreateNode(root, "Pedestrian_Node", pedestrian, AccessNodeType.PedestrianHub);
        }

        private void CreateNode(Transform parent, string name, Vector3 position, AccessNodeType type)
        {
            var node = new GameObject(name).AddComponent<AccessNetworkNode>();
            node.transform.SetParent(parent, true);
            node.transform.position = position;
            node.nodeType = type;
            node.nodeId = name;
            AccessNetworkRuntime.I?.RegisterNode(node);
        }
    }
}
