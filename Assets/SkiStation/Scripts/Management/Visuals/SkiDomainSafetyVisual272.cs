using UnityEngine;

namespace SkiStation
{
    /// <summary>2.72 visual-only ski-domain safety dressing. No colliders, routing or gameplay changes.</summary>
    public sealed class SkiDomainSafetyVisual272 : MonoBehaviour
    {
        static SkiDomainSafetyVisual272 instance; GameObject root; float retry;
        Material red, orange, blue, black, metal, snow, net;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_272_Securite_Domaine_VISUEL");instance=g.AddComponent<SkiDomainSafetyVisual272>();}
        static Material Mat(Color c){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s);m.color=c;return m;}
        static float H(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        GameObject Box(string n,Vector3 p,Vector3 sc,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(root.transform,false);g.transform.position=p;g.transform.localScale=sc;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(root!=null||Time.unscaledTime<retry)return;retry=Time.unscaledTime+1f;if(GameObject.Find("SkiStation_MASSIF_01")==null)return;Build();}
        void Build(){root=new GameObject("Habillage_securite_domaine_272");root.transform.SetParent(transform,false);red=Mat(new Color(.82f,.08f,.06f));orange=Mat(new Color(1f,.38f,.04f));blue=Mat(new Color(.06f,.34f,.72f));black=Mat(new Color(.08f,.09f,.10f));metal=Mat(new Color(.22f,.24f,.26f));snow=Mat(new Color(.97f,.99f,1f));net=Mat(new Color(.86f,.12f,.08f));
            // Sparse, visual-only markers kept away from the arrival hub. They do not define piste gameplay.
            Vector3[] markers={new Vector3(-42,0,150),new Vector3(-28,0,190),new Vector3(-10,0,230),new Vector3(18,0,270),new Vector3(42,0,310)};
            for(int i=0;i<markers.Length;i++)Marker(markers[i],i<2?blue:(i<4?red:black));
            NetLine(new Vector3(-58,0,205),new Vector3(-40,0,224)); NetLine(new Vector3(48,0,250),new Vector3(62,0,270));
            for(int i=0;i<4;i++)Pad(new Vector3(-18+i*12,0,335+i*8));
            Direction(new Vector3(8,0,175));
            Debug.Log("SkiStation 2.72: balises, panneaux, filets et protections visuelles du domaine installes; gameplay inchange.");}
        void Marker(Vector3 p,Material m){float y=H(p.x,p.z);Box("Balise_piste",new Vector3(p.x,y+1.35f,p.z),new Vector3(.16f,2.6f,.16f),metal);Box("Plaque_balise",new Vector3(p.x,y+2.35f,p.z),new Vector3(.65f,.65f,.12f),m);Box("Neige_balise",new Vector3(p.x,y+2.72f,p.z),new Vector3(.72f,.10f,.22f),snow);}
        void NetLine(Vector3 a,Vector3 b){float ya=H(a.x,a.z),yb=H(b.x,b.z);Vector3 aa=new Vector3(a.x,ya+.9f,a.z),bb=new Vector3(b.x,yb+.9f,b.z);for(int i=0;i<=5;i++){float t=i/5f;Vector3 p=Vector3.Lerp(aa,bb,t);Box("Piquet_filet",p,new Vector3(.10f,1.8f,.10f),metal);}Vector3 mid=(aa+bb)*.5f;float len=Vector3.Distance(aa,bb);var g=Box("Filet_securite",mid,new Vector3(len,1.35f,.06f),net);g.transform.rotation=Quaternion.LookRotation((bb-aa).normalized)*Quaternion.Euler(0,90,0);}
        void Pad(Vector3 p){float y=H(p.x,p.z);Box("Protection_pylone_orange",new Vector3(p.x,y+1.05f,p.z),new Vector3(.75f,2.1f,.75f),orange);Box("Bande_protection",new Vector3(p.x,y+1.15f,p.z-.39f),new Vector3(.55f,.22f,.05f),black);}
        void Direction(Vector3 p){float y=H(p.x,p.z);Box("Poteau_direction",new Vector3(p.x,y+2f,p.z),new Vector3(.18f,3.8f,.18f),metal);var a=Box("Panneau_pistes",new Vector3(p.x+1.3f,y+3.0f,p.z),new Vector3(2.8f,.55f,.16f),blue);a.transform.rotation=Quaternion.Euler(0,-8,0);var b=Box("Panneau_station",new Vector3(p.x-1.15f,y+2.3f,p.z),new Vector3(2.4f,.5f,.16f),red);b.transform.rotation=Quaternion.Euler(0,10,0);}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{red,orange,blue,black,metal,snow,net})if(m!=null)Destroy(m);}
    }
}
