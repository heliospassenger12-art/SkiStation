using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum ResortHubRole { ValleyGateway, Village, AlpineBase }

    [Serializable]
    public sealed class ResortHub
    {
        public string id;
        public string displayName;
        public ResortHubRole role;
        public ResortAltitudeTier tier;
        public Vector3 center;
        public float influenceRadius;
        public bool enabledForPlanning = true;
        public int serviceCount;
        public int liftAccessCount;
        public int slopeAccessCount;
        public float connectivityScore;
        public bool IsActive => enabledForPlanning && (serviceCount + liftAccessCount + slopeAccessCount > 0);
    }

    [Serializable]
    public struct ResortHubLink
    {
        public string fromId, toId;
        public float distance;
        public bool liftConnection;
        public bool pisteConnection;
        public bool internalTransportCandidate;
        public float score;
    }

    // 2.20 — turns optional terrain terraces into planning hubs and measures how the player's domain connects them.
    // No village is forced: hubs remain dormant until the player builds near them.
    public sealed class ResortHubNetworkRuntime : MonoBehaviour
    {
        public static ResortHubNetworkRuntime I { get; private set; }
        public IReadOnlyList<ResortHub> Hubs => hubs;
        public IReadOnlyList<ResortHubLink> Links => links;
        readonly List<ResortHub> hubs = new List<ResortHub>();
        readonly List<ResortHubLink> links = new List<ResortHubLink>();
        float nextRefresh;

        void Awake() { if (I != null && I != this) { Destroy(gameObject); return; } I = this; }
        void Start() { Rebuild(); }
        void Update() { if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + 2f; RefreshActivity(); } }

        public void Rebuild()
        {
            hubs.Clear(); links.Clear();
            var terraces = MultiAltitudeResortTerracesRuntime.I;
            if (terraces == null) return;
            foreach (var t in terraces.Terraces)
            {
                hubs.Add(new ResortHub {
                    id=t.id, displayName=t.displayName, tier=t.tier, center=t.center,
                    influenceRadius=Mathf.Max(160f, t.radius*1.65f),
                    role=t.tier==ResortAltitudeTier.Valley ? ResortHubRole.ValleyGateway :
                         (t.tier==ResortAltitudeTier.Alpine ? ResortHubRole.AlpineBase : ResortHubRole.Village)
                });
            }
            BuildPotentialLinks(); RefreshExistingActivity();
            Debug.Log("SkiStation 2.20: RESORT HUB NETWORK READY — optional multi-altitude centres can activate organically from player construction.");
        }

        void BuildPotentialLinks()
        {
            links.Clear();
            for (int i=0;i<hubs.Count;i++) for (int j=i+1;j<hubs.Count;j++)
            {
                float d=Vector3.Distance(hubs[i].center,hubs[j].center);
                links.Add(new ResortHubLink { fromId=hubs[i].id, toId=hubs[j].id, distance=d,
                    internalTransportCandidate=d<2600f, score=Mathf.Clamp01(1f-d/4200f) });
            }
        }

        public void RefreshActivity()
        {
            if (hubs.Count==0 && MultiAltitudeResortTerracesRuntime.I!=null) { Rebuild(); return; }
            RefreshExistingActivity();
        }

        // Deliberately never calls Rebuild: an empty terrace list is valid.
        void RefreshExistingActivity()
        {
            var lifts=FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None);
            var slopes=FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            for(int h=0;h<hubs.Count;h++)
            {
                var hub=hubs[h]; hub.serviceCount=hub.liftAccessCount=hub.slopeAccessCount=0;
                foreach(var s in StationServiceRuntime.All) if(s!=null && Vector3.Distance(s.transform.position,hub.center)<=hub.influenceRadius) hub.serviceCount++;
                for(int i=0;i<lifts.Length;i++) if(lifts[i]!=null && lifts[i].Route!=null && lifts[i].Route.Count>=2 && IsPlayerLift(lifts[i]) &&
                    (Vector3.Distance(lifts[i].Route[0],hub.center)<=hub.influenceRadius || Vector3.Distance(lifts[i].Route[lifts[i].Route.Count-1],hub.center)<=hub.influenceRadius)) hub.liftAccessCount++;
                for(int i=0;i<slopes.Length;i++) if(slopes[i]!=null && slopes[i].Points!=null && slopes[i].Points.Count>=2 && slopes[i].name.StartsWith("Piste_") &&
                    (Vector3.Distance(slopes[i].Points[0],hub.center)<=hub.influenceRadius || Vector3.Distance(slopes[i].Points[slopes[i].Points.Count-1],hub.center)<=hub.influenceRadius)) hub.slopeAccessCount++;
                hub.connectivityScore=Mathf.Clamp01((hub.liftAccessCount*2f+hub.slopeAccessCount*1.5f+hub.serviceCount*.5f)/8f);
            }
            RefreshLinks(lifts,slopes);
        }

        void RefreshLinks(LiftRuntime[] lifts, SlopeRuntime[] slopes)
        {
            for(int k=0;k<links.Count;k++)
            {
                var l=links[k]; var a=FindHub(l.fromId); var b=FindHub(l.toId); l.liftConnection=false; l.pisteConnection=false;
                if(a==null||b==null){links[k]=l;continue;}
                for(int i=0;i<lifts.Length;i++) if(lifts[i]!=null && lifts[i].Route!=null && lifts[i].Route.Count>=2 && IsPlayerLift(lifts[i]) && Connects(lifts[i].Route[0],lifts[i].Route[lifts[i].Route.Count-1],a,b)) { l.liftConnection=true; break; }
                for(int i=0;i<slopes.Length;i++) if(slopes[i]!=null && slopes[i].Points!=null && slopes[i].Points.Count>=2 && slopes[i].name.StartsWith("Piste_") && Connects(slopes[i].Points[0],slopes[i].Points[slopes[i].Points.Count-1],a,b)) { l.pisteConnection=true; break; }
                l.score=Mathf.Clamp01((l.liftConnection ? .55f : 0f)+(l.pisteConnection ? .35f : 0f)+(l.internalTransportCandidate ? .10f : 0f)); links[k]=l;
            }
        }
        bool Connects(Vector3 p0,Vector3 p1,ResortHub a,ResortHub b) =>
            (Vector3.Distance(p0,a.center)<=a.influenceRadius && Vector3.Distance(p1,b.center)<=b.influenceRadius) ||
            (Vector3.Distance(p1,a.center)<=a.influenceRadius && Vector3.Distance(p0,b.center)<=b.influenceRadius);
        static bool IsPlayerLift(LiftRuntime l) => l!=null && l.LiftId!="starter_panorama_6seat" && !l.name.Contains("Démonstration") && !l.name.Contains("Demonstration");
        ResortHub FindHub(string id){for(int i=0;i<hubs.Count;i++)if(hubs[i].id==id)return hubs[i];return null;}
        public ResortHub FindNearestHub(Vector3 p){ResortHub best=null;float d=float.MaxValue;foreach(var h in hubs){float x=(h.center-p).sqrMagnitude;if(x<d){d=x;best=h;}}return best;}
    }

    public static class ResortHubNetworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
if(UnityEngine.Object.FindFirstObjectByType<ResortHubNetworkRuntime>()!=null)return;new GameObject("SkiStation_2.20_RESORT_HUB_NETWORK").AddComponent<ResortHubNetworkRuntime>();}
    }
}
