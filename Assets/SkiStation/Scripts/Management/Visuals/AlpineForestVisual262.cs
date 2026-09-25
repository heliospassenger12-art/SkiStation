using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation
{
    /// <summary>2.62 visual-only pass: replaces capsule crowns by layered alpine conifers with light snow loading.</summary>
    public sealed class AlpineForestVisual262 : MonoBehaviour
    {
        static AlpineForestVisual262 instance; Mesh crownMesh; Material needles, snow; float retry;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){ if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return; var g=new GameObject("SkiStation_262_Foret_Alpine_VISUEL"); instance=g.AddComponent<AlpineForestVisual262>(); }
        void Update(){ if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;} if(crownMesh!=null||Time.unscaledTime<retry)return; retry=Time.unscaledTime+1f; Apply(); }
        void Apply()
        {
            var trunks=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None); int made=0;
            crownMesh=BuildCrown(); needles=Mat("262_Aiguilles",new Color(.035f,.13f,.075f),.12f); snow=Mat("262_NeigeBranches",new Color(.93f,.965f,.985f),.28f);
            foreach(var t in trunks)
            {
                if(t==null||t.name!="PineTrunk")continue;
                Transform old=null; var all=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
                foreach(var q in all) if(q!=null&&q.name=="PineCrown"&&(new Vector2(q.position.x-t.position.x,q.position.z-t.position.z)).sqrMagnitude<.01f){old=q;break;}
                if(old==null)continue; var rr=old.GetComponent<Renderer>(); if(rr!=null)rr.enabled=false;
                float h=Mathf.Max(5f,old.localScale.y*2f); float radius=Mathf.Max(2.5f,old.localScale.x);
                var g=new GameObject("Sapin_Alpin_262"); g.transform.SetParent(t,false); g.transform.localPosition=new Vector3(0f,h*.62f,0f); g.transform.localScale=new Vector3(radius,h,radius); g.transform.localRotation=Quaternion.Euler(0f,(made*137.5f)%360f,0f);
                var mf=g.AddComponent<MeshFilter>(); mf.sharedMesh=crownMesh; var mr=g.AddComponent<MeshRenderer>(); mr.sharedMaterials=new[]{needles,snow}; mr.shadowCastingMode=ShadowCastingMode.On; mr.receiveShadows=true; made++;
            }
            Debug.Log("SkiStation 2.62: "+made+" sapins proceduraux ameliores (silhouette etagee + neige), gameplay inchange.");
        }
        static Mesh BuildCrown()
        {
            const int sides=8; var v=new List<Vector3>(); var green=new List<int>(); var white=new List<int>();
            // Three overlapping conical branch tiers; normalized mesh, scaled per existing tree.
            AddCone(v,green,sides,0f,.64f,.48f); AddCone(v,green,sides,.20f,.80f,.38f); AddCone(v,green,sides,.43f,.96f,.27f);
            // Thin snow skirts on the upper surface of each tier, offset to avoid z-fighting.
            AddCone(v,white,sides,.025f,.655f,.455f); AddCone(v,white,sides,.225f,.815f,.355f); AddCone(v,white,sides,.455f,.975f,.245f);
            var m=new Mesh{name="SkiStation_262_SapinEtage"}; m.SetVertices(v); m.subMeshCount=2; m.SetTriangles(green,0);m.SetTriangles(white,1);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static void AddCone(List<Vector3> v,List<int> tri,int sides,float bottomY,float topY,float radius)
        {
            int tip=v.Count; v.Add(new Vector3(0,topY,0)); int ring=v.Count;
            for(int i=0;i<sides;i++){float a=i*Mathf.PI*2f/sides;v.Add(new Vector3(Mathf.Cos(a)*radius,bottomY,Mathf.Sin(a)*radius));}
            for(int i=0;i<sides;i++){int a=ring+i,b=ring+(i+1)%sides;tri.Add(tip);tri.Add(b);tri.Add(a);}
        }
        static Material Mat(string n,Color c,float smooth){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Unlit/Color");var m=new Material(s){name=n,color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);return m;}
        void OnDestroy(){if(instance==this)instance=null;if(crownMesh!=null)Destroy(crownMesh);if(needles!=null)Destroy(needles);if(snow!=null)Destroy(snow);}
    }
}
