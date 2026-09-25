using UnityEngine;

namespace SkiStation
{
    /// <summary>2.60 visual-only snow/rock/vegetation transition around the station. Gameplay untouched.</summary>
    public sealed class StationMountainBlend260 : MonoBehaviour
    {
        static StationMountainBlend260 instance; GameObject root; float retry;
        Material snowA,snowB,rockA,rockB,pine,trunk;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive||instance!=null)return;var g=new GameObject("SkiStation_260_Raccord_Village_Montagne_VISUEL");instance=g.AddComponent<StationMountainBlend260>();}
        static Material Mat(Color c,float smooth=.25f){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s);m.color=c;if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);return m;}
        static float H(float x,float z)=>SkiStationGuaranteedMountainVisual.Height(x,z);
        void Update(){if(SkiStation.Arcade.ArcadeSceneIsolation.IsActive){Destroy(gameObject);return;}if(root!=null||Time.unscaledTime<retry)return;retry=Time.unscaledTime+1f;var p=StationGatewayRuntime.I!=null?StationGatewayRuntime.I.GatewayArrivalPoint:null;Build(p!=null?p.position:new Vector3(0,H(0,-2600),-2600));}
        GameObject P(string n,PrimitiveType t,Vector3 p,Vector3 sc,Material m,Quaternion? r=null){var g=GameObject.CreatePrimitive(t);g.name=n;g.transform.SetParent(root.transform,false);g.transform.position=p;g.transform.localScale=sc;if(r.HasValue)g.transform.rotation=r.Value;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);var rr=g.GetComponent<Renderer>();if(rr!=null)rr.sharedMaterial=m;return g;}
        void Build(Vector3 a){root=new GameObject("Raccords_neige_roche_foret_260_VISUELS");root.transform.SetParent(transform,false);snowA=Mat(new Color(.91f,.95f,.98f),.42f);snowB=Mat(new Color(.78f,.86f,.91f),.30f);rockA=Mat(new Color(.31f,.29f,.27f),.12f);rockB=Mat(new Color(.43f,.39f,.34f),.10f);pine=Mat(new Color(.055f,.16f,.105f),.16f);trunk=Mat(new Color(.24f,.14f,.075f),.08f);
            // Uneven snow banks: visual micro-relief without changing gameplay/colliders.
            for(int side=-1;side<=1;side+=2)for(int i=0;i<10;i++){float z=a.z+25f+i*34f;float x=a.x+side*(72f+12f*Mathf.Sin(i*1.31f));float y=H(x,z);float sx=24f+(i%3)*7f,sz=15f+(i%4)*4f;P("Congere_neige",PrimitiveType.Sphere,new Vector3(x,y+.55f,z),new Vector3(sx,1.15f+(i%2)*.35f,sz),i%2==0?snowA:snowB);}
            // Rock outcrops grow toward the mountain, breaking the white blanket.
            for(int side=-1;side<=1;side+=2)for(int i=0;i<8;i++){float z=a.z+105f+i*48f;float x=a.x+side*(118f+18f*Mathf.Sin(i*.83f));float y=H(x,z);var q=Quaternion.Euler(8f+i*5f,17f+i*29f,side*(9f+i%3*4f));P("Rocher_alpin",PrimitiveType.Cube,new Vector3(x,y+2.2f,z),new Vector3(8f+i%3*3f,4f+i%2*2f,6f+i%4),i%2==0?rockA:rockB,q);P("Neige_sur_rocher",PrimitiveType.Sphere,new Vector3(x,y+4.3f+i%2,z),new Vector3(7f+i%3*2f,.55f,5f+i%4),snowA);}
            // Irregular tree clusters, leaving a generous central construction corridor.
            for(int side=-1;side<=1;side+=2)for(int i=0;i<13;i++){float z=a.z+60f+i*31f;float x=a.x+side*(145f+22f*Mathf.Sin(i*1.77f));Tree(x,z,7f+(i%4)*1.3f);if(i%3==0)Tree(x+side*13f,z+9f,5.5f+(i%2));}
            Debug.Log("SkiStation 2.60: raccord visuel village/montagne, neige nuancee, rochers et bosquets installes; gameplay inchange.");}
        void Tree(float x,float z,float h){float y=H(x,z);P("Tronc",PrimitiveType.Cylinder,new Vector3(x,y+h*.22f,z),new Vector3(.55f,h*.22f,.55f),trunk);for(int j=0;j<3;j++){float yy=y+h*(.38f+j*.18f);float radius=h*(.27f-j*.045f);P("Sapin_alpin",PrimitiveType.Cylinder,new Vector3(x,yy,z),new Vector3(radius,h*.17f,radius),pine);}P("Neige_branche",PrimitiveType.Sphere,new Vector3(x,y+h*.72f,z),new Vector3(h*.20f,.22f,h*.20f),snowA);}
        void OnDestroy(){if(instance==this)instance=null;if(root!=null)Destroy(root);foreach(var m in new[]{snowA,snowB,rockA,rockB,pine,trunk})if(m!=null)Destroy(m);}
    }
}
