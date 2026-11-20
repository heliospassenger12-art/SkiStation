using System.Collections.Generic;
using UnityEngine;
namespace SkiStation
{
    // Geometry cache only. Never registers links or chooses a branch.
    public sealed class VisitorSkiDescentPath393
    {
        public struct Sample {public Vector3 center,left,right,tangent,normal;public float distance,halfWidth,bend;public bool valid;public string invalidReason;}
        public readonly List<Sample> samples=new List<Sample>();
        public float Length=>samples.Count==0?0:samples[samples.Count-1].distance;
        readonly SlopeRuntime slope;readonly int revision;
        readonly Arcade.ArcadeDomainContext domain;readonly int terrainRevision;
        static readonly Dictionary<SlopeRuntime,VisitorSkiDescentPath393> cache=new Dictionary<SlopeRuntime,VisitorSkiDescentPath393>();
        static readonly Collider[] obstacles=new Collider[32];
        public static VisitorSkiDescentPath393 Get(SlopeRuntime p)
        {if(p==null)return null;if(!cache.TryGetValue(p,out var c)||c.revision!=p.GeometryRevision261||c.domain!=null&&c.terrainRevision!=c.domain.GeometryRevision){c=new VisitorSkiDescentPath393(p);cache[p]=c;}return c;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){cache.Clear();}
        public static void Invalidate(){cache.Clear();}
        VisitorSkiDescentPath393(SlopeRuntime p)
        {
            slope=p;revision=p.GeometryRevision261;domain=p.GetComponentInParent<Arcade.ArcadeDomainContext>();terrainRevision=domain!=null?domain.GeometryRevision:0;float distance=0;
            if(p.Points.Count>8192)return;
            int total=0;for(int i=1;i<p.Points.Count;i++)total+=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(p.Points[i-1],p.Points[i])/3));
            float stepScale=Mathf.Max(1,total/8190f);
            for(int i=1;i<p.Points.Count;i++)
            {
                var a=p.Points[i-1];var b=p.Points[i];float length=Vector3.Distance(a,b);if(length<.001f)continue;
                int count=Mathf.Max(1,Mathf.CeilToInt(length/(3*stepScale)));
                for(int j=i==1?0:1;j<=count;j++)
                {
                    float t=j/(float)count;var point=Vector3.Lerp(a,b,t);var tangent=(b-a).normalized;
                    var right=Vector3.Cross(Vector3.up,tangent).normalized;
                    float width=Mathf.Lerp(p.GetWidthAt(i-1),p.GetWidthAt(i),t);
                    float half=Mathf.Max(0,width*.5f-.65f);
                    bool found=Surface(point,out var center);
                    string invalidReason=found?null:"Terrain natif absent";
                    bool valid=found&&Free(center,out invalidReason);
                    var left=center;var r=center;
                    // Shrink the usable strip conservatively; no obstacle crossing for prettier turns.
                    for(int attempt=0;attempt<5;attempt++)
                    {
                        bool l=Surface(point-right*half,out left)&&Free(left,out _);
                        bool rr=Surface(point+right*half,out r)&&Free(r,out _);
                        if(l&&rr&&Mathf.Abs(left.y-center.y)<3&&Mathf.Abs(r.y-center.y)<3)break;
                        half*=.5f;if(attempt==4){half=0;left=r=center;}
                    }
                    var normal=Vector3.Cross(tangent,r-left);if(normal.sqrMagnitude<.01f)normal=Vector3.up;else{normal.Normalize();if(normal.y<0)normal=-normal;}
                    if(valid&&!(width>=1.3f)){valid=false;invalidReason="Largeur inférieure à 1,3 m";}
                    if(valid&&!(Mathf.Abs(center.y-point.y)<8)){valid=false;invalidReason="Tracé éloigné de plus de 8 m du terrain";}
                    samples.Add(new Sample{center=center,left=left,right=r,tangent=tangent,normal=normal,distance=distance+t*length,halfWidth=half,valid=valid,invalidReason=invalidReason});
                }
                distance+=length;
            }
            for(int i=0;i<samples.Count;i++)
            {var s=samples[i];var tangent=i+1<samples.Count?samples[i+1].center-s.center:i>0?s.center-samples[i-1].center:s.tangent;if(tangent.sqrMagnitude>.001f)s.tangent=tangent.normalized;var normal=Vector3.Cross(s.tangent,s.right-s.left);if(normal.sqrMagnitude>.001f){normal.Normalize();s.normal=normal.y<0?-normal:normal;}samples[i]=s;}
            for(int i=0;i<samples.Count;i++){var s=samples[i];s.bend=Vector3.Angle(s.tangent,samples[Mathf.Min(i+2,samples.Count-1)].tangent);samples[i]=s;}
            if(samples.Count>16384)samples.Clear();
        }
        bool Surface(Vector3 p,out Vector3 ground)=>ManagementSurface.TryTrail(p,out ground,slope);
        static bool Free(Vector3 ground,out string reason)
        {
            reason=null;
            int n=Physics.OverlapCapsuleNonAlloc(ground+Vector3.up*.45f,ground+Vector3.up*1.3f,.35f,obstacles,~0,QueryTriggerInteraction.Ignore);
            if(n==obstacles.Length){reason="Contrôle saturé (32 colliders proches)";return false;}
            for(int i=0;i<n;i++)
            {
                var c=obstacles[i];if(c==null||c is TerrainCollider||c.GetComponentInParent<SkierRuntime>()!=null||c.bounds.max.y<ground.y+.4f)continue;
                var ribbon=c.GetComponent<SlopeMeshGenerator>();
                if(ribbon!=null&&ribbon.SupportsDescent393(c,ground))continue;
                if(c.Raycast(new Ray(ground+Vector3.up*2,Vector3.down),out var hit,3)&&Mathf.Abs(hit.point.y-ground.y)<.08f)continue;
                if(c.bounds.Contains(ground+Vector3.up*.8f))
                {
                    reason="Collider "+c.GetType().Name+" : "+ColliderPath(c.transform)+(ribbon!=null?" · "+ribbon.DescentSupportDiagnostic393():"");
                    return false;
                }
            }
            return true;
        }
        static string ColliderPath(Transform t)
        {
            string path=t.name;
            for(int depth=0;t.parent!=null&&depth<6;depth++){t=t.parent;path=t.name+"/"+path;}
            return path;
        }
        public string ExplainInvalidSegment(float from,float to)
        {
            if(samples.Count<2)return "Surface de piste indisponible";
            for(int i=Index(from);i<=Mathf.Min(samples.Count-1,Index(to)+1);i++)
                if(!samples[i].valid)return "À "+samples[i].distance.ToString("F1")+" m, "+samples[i].center.ToString("F2")+" : "+samples[i].invalidReason;
            return "Aucun échantillon invalide dans le tronçon demandé";
        }
        public int Index(float distance)
        {int a=0,b=Mathf.Max(0,samples.Count-1);while(a<b){int m=(a+b+1)/2;if(samples[m].distance<=distance)a=m;else b=m-1;}return Mathf.Min(a,Mathf.Max(0,samples.Count-2));}
        public bool Evaluate(float distance,float offset,out Vector3 position,out Sample sample)
        {
            position=Vector3.zero;sample=default;if(samples.Count<2)return false;
            int i=Index(distance);var a=samples[i];var b=samples[i+1];if(!a.valid||!b.valid)return false;
            float t=Mathf.InverseLerp(a.distance,b.distance,distance);sample=a;sample.center=Vector3.Lerp(a.center,b.center,t);sample.tangent=(b.center-a.center).normalized;
            sample.halfWidth=Mathf.Min(a.halfWidth,b.halfWidth);sample.normal=Vector3.Slerp(a.normal,b.normal,t);sample.bend=Mathf.Max(a.bend,b.bend);
            // Turn amplitude vanishes at sharp route vertices, preventing a shortcut across the elbow.
            offset=Mathf.Clamp(offset,-sample.halfWidth,sample.halfWidth)*Mathf.Lerp(1,.15f,Mathf.Clamp01(sample.bend/80));
            var side=offset<0?Vector3.Lerp(a.left,b.left,t):Vector3.Lerp(a.right,b.right,t);
            position=Vector3.Lerp(sample.center,side,sample.halfWidth>.01f?Mathf.Abs(offset)/sample.halfWidth:0)+Vector3.up*.06f;
            return true;
        }
        public static float Project(SlopeRuntime p,Vector3 point,out float gap)
        {
            float d=0,best=float.PositiveInfinity,result=0;for(int i=1;i<p.Points.Count;i++)
            {var a=p.Points[i-1];var delta=p.Points[i]-a;float t=Mathf.Clamp01(Vector3.Dot(point-a,delta)/Mathf.Max(.0001f,delta.sqrMagnitude));float sq=(point-a-delta*t).sqrMagnitude;if(sq<best){best=sq;result=d+delta.magnitude*t;}d+=delta.magnitude;}
            gap=Mathf.Sqrt(best);return result;
        }
        public static Vector3 Center(SlopeRuntime p,float distance)
        {for(int i=1;i<p.Points.Count;i++){float length=Vector3.Distance(p.Points[i-1],p.Points[i]);if(distance<=length)return Vector3.Lerp(p.Points[i-1],p.Points[i],length>.001f?distance/length:0);distance-=length;}return p.BottomPoint;}
    }
}
