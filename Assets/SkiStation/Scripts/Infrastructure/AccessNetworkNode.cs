using System.Collections.Generic;
using UnityEngine;
namespace SkiStation {
 public enum AccessNodeType { Generic, StationEntrance, Parking, Village, LiftBase, Building, PedestrianHub, ServiceHub, RoadJunction, PedestrianJunction }
 public class AccessNetworkNode : MonoBehaviour {
  public AccessNodeType nodeType = AccessNodeType.Generic; public string nodeId;
  [SerializeField] private List<AccessRoadRuntime> connections = new List<AccessRoadRuntime>();
  public IReadOnlyList<AccessRoadRuntime> Connections => connections;
  public void Register(AccessRoadRuntime road){ if(road!=null&&!connections.Contains(road)) connections.Add(road); }
  public void Unregister(AccessRoadRuntime road){ connections.Remove(road); }
 }
}
