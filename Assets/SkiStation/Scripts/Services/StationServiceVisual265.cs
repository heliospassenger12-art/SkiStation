using UnityEngine;

namespace SkiStation
{
    /// <summary>2.65 close-up polish for service buildings. Visual-only; no gameplay/collider changes.</summary>
    public static class StationServiceVisual265
    {
        static Material Mat(Color c) => RuntimeMaterialFactory.Create(c);
        static Transform Box(Transform p,string n,Vector3 pos,Vector3 scale,Color c,Vector3 rot=default)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=n; g.transform.SetParent(p,false);
            g.transform.localPosition=pos; g.transform.localScale=scale; g.transform.localEulerAngles=rot;
            var col=g.GetComponent<Collider>(); if(col!=null) Object.Destroy(col);
            var r=g.GetComponent<Renderer>(); if(r!=null){var m=Mat(c); if(m!=null) r.sharedMaterial=m;}
            return g.transform;
        }
        static void WindowFrame(Transform t,float x,float y,float z)
        {
            Color wood=new Color(.19f,.105f,.055f), glass=new Color(.18f,.42f,.53f), warm=new Color(1f,.62f,.28f);
            Box(t,"Vitrine_verre",new Vector3(x,y,z),new Vector3(.20f,.24f,.018f),glass);
            Box(t,"Vitrine_lueur",new Vector3(x,y,z-.012f),new Vector3(.16f,.19f,.010f),warm);
            Box(t,"Cadre_H1",new Vector3(x,y+.13f,z-.018f),new Vector3(.23f,.018f,.025f),wood);
            Box(t,"Cadre_H2",new Vector3(x,y-.13f,z-.018f),new Vector3(.23f,.018f,.025f),wood);
            Box(t,"Cadre_V1",new Vector3(x-.115f,y,z-.018f),new Vector3(.018f,.28f,.025f),wood);
            Box(t,"Cadre_V2",new Vector3(x+.115f,y,z-.018f),new Vector3(.018f,.28f,.025f),wood);
            Box(t,"Croisillon",new Vector3(x,y,z-.020f),new Vector3(.018f,.26f,.026f),wood);
        }
        static void Chair(Transform t,string n,float x,float z,float yaw)
        {
            Color wood=new Color(.30f,.18f,.095f);
            var root=new GameObject(n).transform; root.SetParent(t,false); root.localPosition=new Vector3(x,-.36f,z); root.localEulerAngles=new Vector3(0,yaw,0);
            Box(root,"Assise",Vector3.zero,new Vector3(.075f,.025f,.075f),wood);
            Box(root,"Dossier",new Vector3(0,.07f,.035f),new Vector3(.075f,.13f,.018f),wood,new Vector3(8,0,0));
            foreach(float sx in new[]{-.025f,.025f}) foreach(float sz in new[]{-.025f,.025f})
                Box(root,"Pied",new Vector3(sx,-.075f,sz),new Vector3(.012f,.14f,.012f),wood);
        }
        public static void Apply(GameObject b, StationServiceType type, string displayName)
        {
            if(b==null)return;
            StationServiceVisual257.Apply(b,type,displayName);
            Transform t=b.transform; bool food=type==StationServiceType.Restaurant||type==StationServiceType.Bar;
            Color timber=new Color(.22f,.12f,.06f), stone=new Color(.36f,.35f,.33f), snow=new Color(.95f,.98f,1f), dark=new Color(.12f,.13f,.14f);

            // Stone plinth blocks break the perfectly straight procedural base.
            for(int i=-5;i<=5;i++)
            {
                float x=i*.095f; float h=.10f+((i*i)%3)*.018f;
                Box(t,"Pierre_socle_"+i,new Vector3(x,-.425f,-.522f),new Vector3(.085f,h,.055f),stone);
            }
            // Stronger facade rhythm: shutters/frames, timber braces and snow shelf.
            WindowFrame(t,-.31f,.05f,-.552f); WindowFrame(t,.31f,.05f,-.552f);
            Box(t,"Poutre_diagonale_G",new Vector3(-.36f,.33f,-.575f),new Vector3(.025f,.30f,.025f),timber,new Vector3(0,0,-38));
            Box(t,"Poutre_diagonale_D",new Vector3(.36f,.33f,-.575f),new Vector3(.025f,.30f,.025f),timber,new Vector3(0,0,38));
            Box(t,"Neige_auvent",new Vector3(0,.205f,-.61f),new Vector3(.40f,.025f,.27f),snow,new Vector3(4,0,0));
            Box(t,"Marche_entree",new Vector3(0,-.475f,-.62f),new Vector3(.28f,.045f,.20f),stone);
            Box(t,"Poignee_porte",new Vector3(.045f,-.20f,-.548f),new Vector3(.012f,.055f,.012f),new Color(.75f,.58f,.30f));

            // Roof details make the silhouette readable from the management camera.
            for(int i=-3;i<=3;i++)
                Box(t,"Arret_neige_"+i,new Vector3(i*.12f,.695f,-.27f),new Vector3(.018f,.045f,.48f),dark,new Vector3(0,0,i<0?24f:-24f));
            Box(t,"Neige_faitage",new Vector3(0,.855f,0),new Vector3(.09f,.035f,1.20f),snow);

            if(food)
            {
                // A denser terrace reads as an occupied resort rather than an empty prototype.
                foreach(float x in new[]{-.34f,0f,.34f})
                {
                    Box(t,"Table_265",new Vector3(x,-.34f,-.86f),new Vector3(.13f,.025f,.13f),timber);
                    Box(t,"Pied_table_265",new Vector3(x,-.405f,-.86f),new Vector3(.018f,.12f,.018f),dark);
                    Chair(t,"Chaise_G",x-.12f,-.86f,90); Chair(t,"Chaise_D",x+.12f,-.86f,-90);
                }
                // Low snow-covered terrace boundary.
                Box(t,"Muret_terrasse_G",new Vector3(-.54f,-.36f,-.84f),new Vector3(.055f,.20f,.46f),stone);
                Box(t,"Muret_terrasse_D",new Vector3(.54f,-.36f,-.84f),new Vector3(.055f,.20f,.46f),stone);
                Box(t,"Neige_muret_G",new Vector3(-.54f,-.245f,-.84f),new Vector3(.075f,.035f,.48f),snow);
                Box(t,"Neige_muret_D",new Vector3(.54f,-.245f,-.84f),new Vector3(.075f,.035f,.48f),snow);
            }
        }
    }
}
