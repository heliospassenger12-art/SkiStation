using UnityEngine;
namespace SkiStation { public class AccessNetworkBootstrap : MonoBehaviour { void Awake(){if(AccessNetworkRuntime.I==null)new GameObject("AccessNetworkRuntime").AddComponent<AccessNetworkRuntime>();} } }
