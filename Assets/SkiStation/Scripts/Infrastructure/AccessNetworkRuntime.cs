using System; using System.Collections.Generic; using UnityEngine;
namespace SkiStation {
 public class AccessNetworkRuntime : MonoBehaviour {
  public static AccessNetworkRuntime I {get;private set;} [SerializeField] float connectionTolerance=8f;
  readonly List<AccessRoadRuntime> roads=new List<AccessRoadRuntime>(); readonly List<AccessNetworkNode> nodes=new List<AccessNetworkNode>();
  public IReadOnlyList<AccessRoadRuntime> Roads=>roads; public IReadOnlyList<AccessNetworkNode> Nodes=>nodes; public event Action Changed;
  void Awake(){ if(I!=null&&I!=this){Destroy(gameObject);return;} I=this; }
  public void RegisterRoad(AccessRoadRuntime road){if(road==null||roads.Contains(road))return;roads.Add(road);RebuildConnections();}
  public void RegisterNode(AccessNetworkNode node){if(node==null||nodes.Contains(node))return;nodes.Add(node);RebuildConnections();}
  public void RebuildConnections(){foreach(var n in nodes)if(n!=null)foreach(var r in roads)n.Unregister(r);foreach(var r in roads){if(r==null)continue;foreach(var n in nodes)if(n!=null&&NearAnyPoint(r,n.transform.position))n.Register(r);}Changed?.Invoke();}
  public bool IsConnected(Vector3 from,Vector3 to,bool pedestrianOnly=false){var a=FindNearestNode(from); var b=FindNearestNode(to);if(a==null||b==null)return Vector3.Distance(from,to)<=connectionTolerance;if(a==b)return true;var seen=new HashSet<AccessNetworkNode>{a};var q=new Queue<AccessNetworkNode>();q.Enqueue(a);while(q.Count>0){var cur=q.Dequeue();foreach(var road in cur.Connections){if(road==null||(pedestrianOnly&&road.IsVehicleRoad))continue;foreach(var n in nodes){if(n==null||seen.Contains(n)||n==cur)continue;if(NearAnyPoint(road,n.transform.position)){if(n==b)return true;seen.Add(n);q.Enqueue(n);}}}}return false;}
  public AccessNetworkNode FindNearestNode(Vector3 p){AccessNetworkNode best=null;float bestSq=float.MaxValue;foreach(var n in nodes){if(n==null)continue;float sq=(n.transform.position-p).sqrMagnitude;if(sq<bestSq){bestSq=sq;best=n;}}return bestSq<=connectionTolerance*connectionTolerance?best:null;}
  bool NearAnyPoint(AccessRoadRuntime road,Vector3 p){float sq=connectionTolerance*connectionTolerance;foreach(var t in road.ControlPoints)if(t!=null&&(t.position-p).sqrMagnitude<=sq)return true;return false;}
 }
}
