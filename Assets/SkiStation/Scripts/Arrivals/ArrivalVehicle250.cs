using System.Collections.Generic;
using UnityEngine;
namespace SkiStation
{
    public sealed class ArrivalVehicle250 : MonoBehaviour
    {
        private ArrivalHub250 hub;
        private bool bus,parked,exiting;
        private int slot,remaining,index;
        private Vector3[] path;
        private float unloadAt;
        private readonly List<SkierRuntime> passengers=new List<SkierRuntime>();
        public void Configure(ArrivalHub250 owner,bool isBus,int bay,int people)
        {
            hub=owner;bus=isBus;slot=bay;remaining=people;
            path=hub.DrivePath(bus,slot);index=1;transform.position=path[0];
            var paint=hub.Mat(bus?new Color(.13f,.38f,.64f):Color.HSVToRGB(Random.Range(0,6)/6f,.65f,.7f));
            var glass=hub.Mat(new Color(.17f,.29f,.35f));var rubber=hub.Mat(new Color(.06f,.06f,.07f));
            float length=bus?11f:4.8f;float width=bus?2.7f:2.1f;
            hub.Box("Carrosserie",transform,new Vector3(0,bus?1.6f:.85f,0),new Vector3(width,bus?2.7f:1.05f,length),paint);
            hub.Box("Habitacle_vitre",transform,new Vector3(0,bus?2.1f:1.6f,0),new Vector3(width+.025f,bus?1.1f:.75f,bus?9f:2.7f),glass);
            hub.Box("Toit",transform,new Vector3(0,bus?3.05f:2.03f,0),new Vector3(width+.1f,.18f,bus?11.2f:3f),paint);
            for(int side=-1;side<=1;side+=2)for(int axle=-1;axle<=1;axle+=2)
            {
                var wheel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wheel.name="Roue";wheel.transform.SetParent(transform,false);
                wheel.transform.localPosition=new Vector3(side*width*.5f,.55f,axle*length*.32f);
                wheel.transform.localRotation=Quaternion.Euler(0,0,90);wheel.transform.localScale=new Vector3(1.05f,.18f,1.05f);
                wheel.GetComponent<Renderer>().sharedMaterial=rubber;var col=wheel.GetComponent<Collider>();col.enabled=false;Destroy(col);
            }
            var lights=hub.Mat(new Color(1f,.95f,.7f));
            for(int side=-1;side<=1;side+=2)hub.Box("Phare",transform,new Vector3(side*width*.32f,.9f,length*.5f+.04f),new Vector3(.4f,.25f,.08f),lights);
            if(bus)hub.Box("Porte_passagers",transform,new Vector3(width*.5f+.02f,1.6f,3.3f),new Vector3(.08f,2.2f,1.2f),glass);
        }
        private void Update()
        {
            if(hub==null){Destroy(gameObject);return;}
            if(!parked||exiting)
            {
                if(index>=path.Length)
                {
                    if(exiting){Destroy(gameObject);return;}
                    parked=true;hub.Parked(this,bus);unloadAt=Time.time+2f;return;
                }
                Vector3 direction=path[index]-transform.position;
                transform.position=Vector3.MoveTowards(transform.position,path[index],(index<65?16f:4f)*Time.deltaTime);
                direction.y=0;if(direction.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),100f*Time.deltaTime);
                if(Vector3.Distance(transform.position,path[index])<.1f)index++;
                return;
            }
            if(remaining>0 && Time.time>=unloadAt)
            {
                if(StationVisitorDemandRuntime.I==null)return;
                var person=StationVisitorDemandRuntime.I.CreateArrivedVisitor(hub.WalkFromBay(bus,slot),bus?ArrivalMode250.Bus:ArrivalMode250.Car);
                if(person==null)return;
                passengers.Add(person);remaining--;hub.PassengerReleased();unloadAt=Time.time+1.2f;return;
            }
            if(remaining>0)return;
            passengers.RemoveAll(p=>p==null);
            if(passengers.Count==0&&hub.TryExit(this)){exiting=true;path=hub.ExitPath(bus,slot);index=1;}
        }
        private void OnDestroy(){if(hub!=null)hub.Release(this,bus,slot,parked,remaining);}
    }
}
