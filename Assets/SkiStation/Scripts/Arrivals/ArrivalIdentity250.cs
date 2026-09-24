using UnityEngine;
namespace SkiStation
{
    public sealed class ArrivalIdentity250 : MonoBehaviour
    {
        public ArrivalMode250 Mode;
        public bool OwnEquipment;
        public bool PrepaidPass;
        private Transform[] skis;
        private Vector3[] skiPositions;
        private SkierRuntime skier;
        private void Start()
        {
            skier=GetComponent<SkierRuntime>();
            var list=new System.Collections.Generic.List<Transform>();
            foreach(Transform child in transform)if(child.name=="Ski")list.Add(child);
            skis=list.ToArray();skiPositions=new Vector3[skis.Length];
            for(int i=0;i<skis.Length;i++)skiPositions[i]=skis[i].localPosition;
        }
        private void LateUpdate()
        {
            if(skis==null||skier==null)return;
            bool carrying=skier.State==SkierState.Arriving||skier.State==SkierState.Leaving;
            for(int i=0;i<skis.Length;i++)
            {
                if(skis[i]==null)continue;
                skis[i].gameObject.SetActive(OwnEquipment);
                skis[i].localPosition=carrying?new Vector3(.5f+i*.1f,1.25f,.1f):skiPositions[i];
                skis[i].localRotation=carrying?Quaternion.Euler(80,0,10):Quaternion.identity;
            }
        }
        public string Needs => OwnEquipment ? (PrepaidPass?"Deja equipe et muni d'un forfait":"Forfait seulement") : (PrepaidPass?"Location seulement":"Location puis forfait");
    }
}
