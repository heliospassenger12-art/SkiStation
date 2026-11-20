using System.Collections.Generic;
using UnityEngine;

namespace SkiStation.TerrainGeneration
{
    public sealed class WaterCrossing : MonoBehaviour
    {
        static readonly HashSet<WaterCrossing> active = new HashSet<WaterCrossing>();
        [SerializeField] Vector3 start, end;
        [SerializeField] float width;
        [SerializeField] bool bike;
        Collider[] decking;
        Material ownedMaterial;
        internal const float SkiWidth=18f, BikeWidth=4f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => active.Clear();
        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);
        void OnDestroy() { if(ownedMaterial!=null) Destroy(ownedMaterial); }

        static bool InDomain(WaterCrossing crossing)
        {
            var world = TerrainGenerationWorld.Current;
            return world != null && crossing != null && crossing.gameObject.scene == world.gameObject.scene;
        }

        internal static bool CoversTrail(Vector3 a,Vector3 b,float radius)
        {
            foreach(var bridge in active)
                if(InDomain(bridge)&&!bridge.bike&&bridge.Encloses(a,radius)&&bridge.Encloses(b,radius))return true;
            return false;
        }

        bool Encloses(Vector3 point,float radius)
        {
            var across=end-start;across.y=0;
            float length=across.magnitude;
            if(length<.01f)return false;
            var direction=across/length;
            var p=point-start;p.y=0;
            float along=Vector3.Dot(p,direction);
            float lateral=Mathf.Abs(Vector3.Dot(p,Vector3.Cross(Vector3.up,direction)));
            // Retain the full trail width on the deck. The two ends taper onto dry banks.
            return along>=0&&along<=length&&lateral+radius<=width*.5f+.01f;
        }

        internal static bool TrySkiRay(Ray ray,out Vector3 surface,out float distance)
        {
            surface=default;
            distance=float.PositiveInfinity;
            foreach(var bridge in active)
            {
                if(!InDomain(bridge)||bridge.bike)continue;
                if(bridge.decking==null)bridge.decking=bridge.GetComponentsInChildren<Collider>();
                foreach(var collider in bridge.decking)
                {
                    if(collider==null||!collider.enabled||!collider.Raycast(ray,out var hit,30000f)||
                        hit.normal.y<.3f||hit.distance>=distance) continue;
                    surface=hit.point;
                    distance=hit.distance;
                }
            }
            return !float.IsInfinity(distance);
        }

        internal static bool TryPlan(RealTerrainWater water,Vector3 a,Vector3 b,bool bike,
            out Vector3 deckA,out Vector3 deckB,out string notice)
        {
            deckA=default;deckB=default;
            notice="";
            if(water==null){notice="Données hydrographiques indisponibles.";return false;}
            if(!ManagementSurface.TryGet(a,out a)||!ManagementSurface.TryGet(b,out b))
            {notice="Visez deux berges sur le terrain.";return false;}
            var ab=b-a;ab.y=0;
            float length=ab.magnitude;
            if(length<20f||length>100f){notice="Choisissez un franchissement de 20 à 100 m.";return false;}
            float width=bike?BikeWidth:SkiWidth;
            var direction=ab/length;
            float ramp=Mathf.Min(10f,length*.27f);
            var lo=a+direction*ramp;
            var hi=b-direction*ramp;
            if(water.Intersects(a,lo,width*.5f)||water.Intersects(hi,b,width*.5f))
            {notice="Les deux accès et leurs appuis doivent rester sur les berges.";return false;}
            if(!water.Intersects(lo,hi,0))
            {notice="Le tablier doit franchir un cours d’eau.";return false;}
            if(water.Intersects(a,b,width*.5f,true))
            {notice="Choisissez un franchissement du cours d’eau en dehors d’un lac.";return false;}
            if(!Altitude.AltitudeConstructionPolicy.ValidateRoute(new[]{a,b},width*.5f,out notice,null,false))return false;
            if(!ConstructionPlacement349.RouteClearOfBuildings(new[]{a,b},width*.5f,out notice))return false;
            var side=Vector3.Cross(Vector3.up,direction);
            int steps=Mathf.CeilToInt(Vector3.Distance(lo,hi)/2f);
            float lift=0f;
            for(int i=0;i<=steps;i++)
            {
                float t=i/(float)steps;
                var p=Vector3.Lerp(lo,hi,t);
                p.y=Mathf.LerpUnclamped(a.y,b.y,Vector3.Dot(p-a,direction)/length);
                for(int edge=-1;edge<=1;edge++)
                {
                    if(!ManagementSurface.TryGet(p+side*(edge*width*.5f),out var ground))
                    {notice="Tablier hors du terrain.";return false;}
                    lift=Mathf.Max(lift,ground.y+.9f-p.y);
                }
            }
            if(lift>4f||Mathf.Abs(b.y-a.y)/length+lift/ramp>.32f)
            {notice="Berges trop pentues ou dégagement insuffisant pour ce pont.";return false;}
            foreach(var crossing in active)
                if(InDomain(crossing)&&
                    (ConstructionRules349.SegmentDistanceXZ(crossing.start,a,b)<crossing.width*.5f+width*.5f||
                     ConstructionRules349.SegmentDistanceXZ(crossing.end,a,b)<crossing.width*.5f+width*.5f||
                     crossing.Encloses((a+b)*.5f,0)))
                {notice="Un pont occupe déjà ce franchissement.";return false;}
            lo.y=Mathf.Lerp(a.y,b.y,ramp/length)+lift;
            hi.y=Mathf.Lerp(a.y,b.y,1-ramp/length)+lift;
            deckA=lo;deckB=hi;
            return true;
        }

        internal static WaterCrossing Build(Vector3 a,Vector3 b,Vector3 deckA,Vector3 deckB,bool bike)
        {
            var root=new GameObject(bike?"Passerelle VTT":"Pont de piste");
            var crossing=root.AddComponent<WaterCrossing>();
            crossing.start=a;
            crossing.end=b;
            crossing.bike=bike;
            crossing.width=bike?BikeWidth:SkiWidth;
            var material=RuntimeMaterialFactory.Create(bike?new Color(.48f,.34f,.22f):new Color(.61f,.69f,.77f));
            crossing.ownedMaterial=material;
            AddPanel("Rampe de berge",a+Vector3.up*.08f,deckA);
            AddPanel("Tablier",deckA,deckB);
            AddPanel("Rampe de berge",deckB,b+Vector3.up*.08f);
            Physics.SyncTransforms();
            return crossing;

            void AddPanel(string name,Vector3 from,Vector3 to)
            {
                var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name=name;
                panel.transform.SetParent(root.transform,false);
                var rotation=Quaternion.LookRotation(to-from,Vector3.up);
                panel.transform.SetPositionAndRotation((from+to)*.5f-rotation*Vector3.up*.25f,rotation);
                panel.transform.localScale=new Vector3(crossing.width,.5f,Vector3.Distance(from,to)+.05f);
                panel.GetComponent<MeshRenderer>().sharedMaterial=material;
            }
        }
    }

    internal sealed class WaterCrossingTool : MonoBehaviour
    {
        RealTerrainWater water;
        bool drawing,bike,hasStart,confirm;
        Vector3 start,end,deckA,deckB;
        string feedback;
        float cost;
        internal bool Active => drawing;

        internal void Cancel()
        {
            drawing=hasStart=confirm=false;
        }

        internal void Begin(RealTerrainWater owner,bool useBike)
        {
            ConstructionInput349.Activate(ConstructionTool349.WaterCrossing);
            water=owner;
            bike=useBike;
            Cancel();
            drawing=true;
            feedback="";
        }

        void Update()
        {
            if(!drawing)return;
            if(Input.GetKeyDown(KeyCode.Escape)){Cancel();return;}
            if(confirm||ConstructionInput349.PointerBlocked||!Input.GetMouseButtonDown(0)||
                !ConstructionInput349.TryGround(Camera.main,out var point)) return;
            ConstructionInput349.ConsumeClick();
            if(!hasStart)
            {
                if(water.Intersects(point,point,bike?2f:9f)){feedback="Choisissez un appui sur la berge.";return;}
                start=point;
                hasStart=true;
                feedback="";
                return;
            }
            end=point;
            confirm=WaterCrossing.TryPlan(water,start,end,bike,out deckA,out deckB,out feedback);
            if(confirm)cost=(bike?2400f:7200f)+Vector3.Distance(start,end)*(bike?55f:165f);
        }

        void OnGUI()
        {
            if(!drawing||ConstructionInput349.ModalOpen)return;
            var box=new Rect(Screen.width*.5f-240,165,480,112);
            ConstructionInput349.PanelArea(this,"water-crossing",box);
            GUILayout.BeginArea(box,GUI.skin.box);
            GUILayout.Label(bike?"Passerelle VTT":"Pont de piste");
            GUILayout.Label(confirm?$"Longueur {Vector3.Distance(start,end):0} m · {cost:N0} €"
                :hasStart?"Cliquez sur la berge opposée.":"Cliquez sur la première berge.");
            if(!string.IsNullOrEmpty(feedback))GUILayout.Label(feedback);
            GUILayout.BeginHorizontal();
            if(confirm&&GUILayout.Button("Construire")) Commit();
            if(GUILayout.Button("Annuler")) Cancel();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void Commit()
        {
            if(!WaterCrossing.TryPlan(water,start,end,bike,out deckA,out deckB,out feedback))return;
            if(!ConstructionPlacement349.CanAfford(cost)){feedback=ConstructionPlacement349.BudgetReason(cost);return;}
            if(!StationRuntime.I.Spend(cost,FinanceCategory.Construction,bike?"water_bike_bridge":"water_ski_bridge",
                bike?"Passerelle VTT":"Pont de piste"))return;
            WaterCrossing.Build(start,end,deckA,deckB,bike);
            Cancel();
            PlayableFeedbackRuntime.I?.Show(bike?"Passerelle VTT construite":"Pont de piste construit");
        }
    }
}
