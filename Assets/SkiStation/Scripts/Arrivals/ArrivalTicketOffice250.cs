using System.Collections.Generic;
using UnityEngine;
namespace SkiStation
{
    public sealed class ArrivalTicketOffice250 : MonoBehaviour
    {
        public Vector3[] Access;
        private readonly Queue<StationVisitorRoadArrival248> queue=new Queue<StationVisitorRoadArrival248>();
        private StationVisitorRoadArrival248 serving;
        private float readyAt;
        public bool TryServe(StationVisitorRoadArrival248 visitor)
        {
            if(visitor==null||!visitor.IsArriving||Access==null||Access.Length<2)return false;
            Vector3 door=Access[Access.Length-1];
            if(Vector2.Distance(new Vector2(visitor.transform.position.x,visitor.transform.position.z),new Vector2(door.x,door.z))>2.5f)return false;
            while(queue.Count>0&&(queue.Peek()==null||!queue.Peek().IsArriving))queue.Dequeue();
            if(!queue.Contains(visitor))queue.Enqueue(visitor);
            if(queue.Peek()!=visitor)return false;
            if(serving!=visitor){serving=visitor;readyAt=Time.time+3f;}
            if(Time.time<readyAt)return false;
            if(!VisitorSkiAdmission.BuyAtOffice(visitor.GetComponent<SkierRuntime>()))return false;
            queue.Dequeue();serving=null;return true;
        }
    }
}
