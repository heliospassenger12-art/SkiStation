using System.Collections.Generic;
using UnityEngine;
namespace SkiStation
{
    /// <summary>2.15 readable piste borders without the old giant blue tube.</summary>
    public sealed class SlopePlayableVisibilityRuntime : MonoBehaviour
    {
        readonly List<GameObject> made = new List<GameObject>();
        Material edgeMat;
        public void Build(SlopeRuntime slope)
        {
            if (slope == null || slope.Points.Count < 2) return;
            edgeMat = RuntimeMaterialFactory.Create(new Color(.08f,.22f,.55f,1f));
            int step=Mathf.Max(1,slope.Points.Count/22);
            for(int i=0;i<slope.Points.Count;i+=step)
            {
                Vector3 tangent=i==slope.Points.Count-1?slope.Points[i]-slope.Points[i-1]:slope.Points[i+1]-slope.Points[i];
                tangent.y=0; if(tangent.sqrMagnitude<.01f)tangent=Vector3.forward; tangent.Normalize();
                Vector3 side=Vector3.Cross(Vector3.up,tangent).normalized; float half=Mathf.Max(5f,slope.GetWidthAt(i)*.5f);
                MakeStake(slope.Points[i]+side*half,"Piste_Bord_G"); MakeStake(slope.Points[i]-side*half,"Piste_Bord_D");
            }
        }
        void MakeStake(Vector3 p,string n){if(ManagementSurface.TryGet(p,out var surface))p=surface;var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(transform);g.transform.position=p+Vector3.up*2.2f;g.transform.localScale=new Vector3(.28f,2.2f,.28f);Set(g,edgeMat);}
        void Set(GameObject g,Material m){var c=g.GetComponent<Collider>();if(c)c.enabled=false;var r=g.GetComponent<Renderer>();if(r&&m)r.sharedMaterial=m;made.Add(g);}
    }
}
