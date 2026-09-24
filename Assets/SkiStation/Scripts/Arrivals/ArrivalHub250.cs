using System.Collections.Generic;
using UnityEngine;
namespace SkiStation
{
    public enum ArrivalMode250 { Walk, Car, Bus }
    public sealed class ArrivalHub250 : MonoBehaviour
    {
        public static ArrivalHub250 I { get; private set; }
        public bool Ready { get; private set; }
        public int PendingVisitors { get; private set; }
        public Transform Root => transform;
        public Vector3 Entrance { get; private set; }
        public ArrivalTicketOffice250 Office { get; private set; }
        private Vector3[] road;
        private Vector3 right, forward;
        private readonly bool[] cars = new bool[12];
        private readonly bool[] buses = new bool[3];
        private readonly Dictionary<Color,Material> materials = new Dictionary<Color,Material>();
        private ArrivalVehicle250 movingVehicle;
        private float nextBuild;
        private float nextDispatch;
        public int ParkedCars { get; private set; }
        public int ParkedBuses { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || I != null) return;
            new GameObject("SkiStation_250_Accueil_multimodal").AddComponent<ArrivalHub250>();
        }
        private void Awake() { I = this; }
        private void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (Ready || Time.unscaledTime < nextBuild) return;
            nextBuild = Time.unscaledTime + 1f;
            var network = StationRoadNetwork247.I;
            if (network == null || !network.IsReady || !ManagementSurface.TryGet(network.MainRoadEnd, out var ground)) return;
            road = network.GetPermanentRoadPath();
            Entrance = ground;
            forward = road[road.Length - 1] - road[road.Length - 2]; forward.y = 0; forward.Normalize();
            right = Vector3.Cross(Vector3.up, forward);
            Build(); Ready = true;
        }
        public Vector3 Point(float x, float z, float elevation = .3f)
        {
            Vector3 p = Entrance + right * x + forward * z;
            return Ground(p, elevation);
        }
        public static Vector3 Ground(Vector3 p, float elevation = .3f)
        {
            if (ManagementSurface.TryGet(p, out var ground)) return ground + Vector3.up * elevation;
            return new Vector3(p.x, SkiStationGuaranteedMountainVisual.Height(p.x,p.z) + elevation, p.z);
        }
        public Vector3[] Lane(float offset)
        {
            var path = new Vector3[road.Length];
            for (int i=0;i<road.Length;i++)
            {
                Vector3 d=road[Mathf.Min(i+1,road.Length-1)]-road[Mathf.Max(0,i-1)]; d.y=0;
                path[i]=Ground(road[i]+Vector3.Cross(Vector3.up,d.normalized)*offset, offset>8f?.36f:.26f);
            }
            return path;
        }
        public Vector3 Bay(bool bus,int slot) => bus ? Point(42+slot*15,-37) : Point(38+slot*5.5f,12);
        public bool HasSpace(ArrivalMode250 mode) => mode == ArrivalMode250.Walk || System.Array.Exists(mode == ArrivalMode250.Bus ? buses : cars, x => !x);
        public bool TryDispatch(ArrivalMode250 mode, int passengers)
        {
            if (!Ready || StationVisitorDemandRuntime.I == null || Time.time < nextDispatch) return false;
            if (mode == ArrivalMode250.Walk)
            {
                var path=new List<Vector3>(Lane(10f)); path.Add(Point(10,4)); path.Add(Entrance);
                StationVisitorDemandRuntime.I.CreateArrivedVisitor(path.ToArray(),mode);
                nextDispatch=Time.time+1.2f; return true;
            }
            bool bus=mode==ArrivalMode250.Bus;
            var slots=bus?buses:cars;
            int slot=System.Array.FindIndex(slots,x=>!x);
            if(slot<0 || movingVehicle!=null) return false;
            slots[slot]=true; PendingVisitors+=passengers;
            var go=new GameObject(bus?"Autocar_arrivee":"Voiture_arrivee"); go.transform.SetParent(transform,true);
            var vehicle=go.AddComponent<ArrivalVehicle250>();
            movingVehicle=vehicle;
            vehicle.Configure(this,bus,slot,passengers);
            nextDispatch=Time.time+3f; return true;
        }
        public Vector3[] DrivePath(bool bus,int slot)
        {
            // Vehicles turn at the road end onto the shared parking aisle.
            var p=new List<Vector3>(Lane(3.8f));
            p.Add(Point(24,0)); p.Add(Point(24,-12));
            Vector3 bay=Bay(bus,slot);
            float x=Vector3.Dot(bay-Entrance,right);
            p.Add(Point(x,-12));p.Add(bay);return p.ToArray();
        }
        public Vector3[] ExitPath(bool bus,int slot)
        {
            var p=new List<Vector3>();var bay=Bay(bus,slot);float x=Vector3.Dot(bay-Entrance,right);
            p.Add(bay);p.Add(Point(x,-12));p.Add(Point(24,-12));p.Add(Point(24,0));
            var lane=Lane(-3.8f);for(int i=lane.Length-1;i>=0;i--)p.Add(lane[i]);return p.ToArray();
        }
        public Vector3[] WalkFromBay(bool bus,int slot)
        {
            Vector3 bay=Bay(bus,slot);float x=Vector3.Dot(bay-Entrance,right);
            // Walk behind the parking row on its marked footway, not through the central aisle.
            float z=bus?-47f:22f;
            return new[]{Ground(bay+right*(bus?-1.7f:1.5f)),Point(x+(bus?-1.7f:1.5f),z),Point(18,z),Point(18,4),Point(10,4),Entrance};
        }
        public void Parked(ArrivalVehicle250 v,bool bus) { if(movingVehicle==v)movingVehicle=null; if(bus)ParkedBuses++;else ParkedCars++; }
        public void PassengerReleased() { PendingVisitors=Mathf.Max(0,PendingVisitors-1); }
        public bool TryExit(ArrivalVehicle250 v) { if(movingVehicle!=null&&movingVehicle!=v)return false;movingVehicle=v;return true; }
        public void Release(ArrivalVehicle250 v,bool bus,int slot,bool parked,int unreleased)
        {
            (bus?buses:cars)[slot]=false;
            if(movingVehicle==v)movingVehicle=null;
            if(parked){if(bus)ParkedBuses=Mathf.Max(0,ParkedBuses-1);else ParkedCars=Mathf.Max(0,ParkedCars-1);}
            PendingVisitors=Mathf.Max(0,PendingVisitors-unreleased);
        }
        public bool BlocksConstruction(Vector3 p,float radius)
        {
            if(!Ready)return false;
            Vector3 d=p-Entrance;float x=Vector3.Dot(d,right),z=Vector3.Dot(d,forward);
            return x>14-radius&&x<110+radius&&z>-55-radius&&z<30+radius;
        }
        public Material Mat(Color color)
        {if(materials.TryGetValue(color,out var cached))return cached;var m=RuntimeMaterialFactory.Create(color);if(m!=null)materials[color]=m;return m;}
        public GameObject Box(string name,Transform parent,Vector3 local,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=local;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
            var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);return go;
        }
        private void Slab(string name,float x,float z,float width,float length,Material mat)
        {
            var go=Box(name,transform,Vector3.zero,new Vector3(width,.18f,length),mat);
            go.transform.SetPositionAndRotation(Point(x,z,.2f),Quaternion.LookRotation(forward));
        }
        private void Label(string text,Vector3 pos)
        {
            var go=new GameObject(text);go.transform.SetParent(transform);go.transform.position=pos;
            go.transform.rotation=Quaternion.LookRotation(-forward);
            var label=go.AddComponent<TextMesh>();label.text=text;label.anchor=TextAnchor.MiddleCenter;label.characterSize=.5f;label.fontSize=48;label.color=Color.white;
        }
        private void Build()
        {
            var asphalt=Mat(new Color(.17f,.19f,.22f));var line=Mat(new Color(.93f,.9f,.74f));var pavement=Mat(new Color(.58f,.61f,.62f));
            Slab("Parking_voitures_et_bus",62,-12,96,82,asphalt);
            Slab("Acces_parking",14,0,28,12,asphalt);
            Slab("Trottoir_parking_nord",62,23,96,4,pavement);
            Slab("Quai_bus",62,-47,96,4,pavement);
            Slab("Liaison_pietonne",18,-12,4,74,pavement);
            Slab("Liaison_trottoir_route",14,4,12,4,pavement);
            for(int i=0;i<=12;i++)Slab("Ligne_place_voiture",35.25f+i*5.5f,12,.15f,12,line);
            for(int i=0;i<=3;i++)Slab("Ligne_place_bus",34.5f+i*15,-37,.18f,16,line);
            for(int i=0;i<5;i++)Slab("Passage_pieton",18,-8-i*1.6f,6,.8f,line);
            var kiosk=new GameObject("Guichet_FORFAITS");kiosk.transform.SetParent(transform);kiosk.transform.SetPositionAndRotation(Point(25,28),Quaternion.LookRotation(forward));
            Box("Chalet",kiosk.transform,new Vector3(0,1.6f,0),new Vector3(6,3.2f,4),Mat(new Color(.36f,.21f,.12f)));
            Box("Toit",kiosk.transform,new Vector3(0,3.4f,0),new Vector3(7,.4f,5),line);
            Box("Guichet",kiosk.transform,new Vector3(0,1.8f,-2.05f),new Vector3(3,1.2f,.12f),Mat(new Color(.24f,.5f,.62f)));
            Office=kiosk.AddComponent<ArrivalTicketOffice250>();Office.Access=new[]{Entrance,Point(10,4),Point(18,4),Point(18,23),Point(25,24)};
            Label("FORFAITS",Point(25,28,4.2f));Label("P  VOITURES",Point(72,24,3));Label("P  AUTOCARS",Point(65,-49,3));
        }
        private void OnDestroy(){if(I==this)I=null;foreach(var m in materials.Values)if(m!=null)Destroy(m);}
    }
}
