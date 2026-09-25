using UnityEngine;

namespace SkiStation
{
    /// <summary>2.78 - architectural silhouette/detail pass for service buildings. Visual only, no colliders.</summary>
    public static class StationServiceVisual278
    {
        static Material Mat(Color c) => RuntimeMaterialFactory.Create(c);
        static Transform Box(Transform p,string n,Vector3 pos,Vector3 scale,Color c,Vector3 rot=default)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=n; g.transform.SetParent(p,false);
            g.transform.localPosition=pos; g.transform.localScale=scale; g.transform.localEulerAngles=rot;
            var col=g.GetComponent<Collider>(); if(col!=null) Object.Destroy(col);
            var r=g.GetComponent<Renderer>(); if(r!=null){var m=Mat(c); if(m!=null) r.sharedMaterial=m;} return g.transform;
        }
        static void Balcony(Transform t)
        {
            Color wood=new Color(.20f,.095f,.035f), snow=new Color(.97f,.99f,1f);
            Box(t,"Balcon_bois",new Vector3(0,.29f,-.69f),new Vector3(.76f,.045f,.20f),wood);
            Box(t,"Neige_balcon",new Vector3(0,.325f,-.69f),new Vector3(.78f,.022f,.21f),snow);
            foreach(float x in new[]{-.36f,-.24f,-.12f,0f,.12f,.24f,.36f})
                Box(t,"Barreau_balcon",new Vector3(x,.45f,-.79f),new Vector3(.018f,.27f,.018f),wood);
            Box(t,"Main_courante",new Vector3(0,.59f,-.79f),new Vector3(.78f,.025f,.025f),wood);
        }
        static void Shutters(Transform t)
        {
            Color shutter=new Color(.16f,.29f,.20f), trim=new Color(.08f,.055f,.03f);
            foreach(float x in new[]{-.43f,-.17f,.17f,.43f})
            {
                Box(t,"Volet_alpin",new Vector3(x,.055f,-.65f),new Vector3(.07f,.27f,.025f),shutter);
                Box(t,"Traverse_volet",new Vector3(x,.055f,-.667f),new Vector3(.075f,.025f,.012f),trim,new Vector3(0,0,x<0?-22:22));
            }
        }
        static void DormerAndChimney(Transform t)
        {
            Color wood=new Color(.25f,.12f,.05f), dark=new Color(.12f,.13f,.13f), snow=new Color(.97f,.99f,1f), glass=new Color(.14f,.34f,.43f);
            Box(t,"Lucarne_corps",new Vector3(-.24f,.73f,-.22f),new Vector3(.23f,.24f,.22f),wood);
            Box(t,"Lucarne_verre",new Vector3(-.24f,.73f,-.345f),new Vector3(.13f,.12f,.012f),glass);
            Box(t,"Lucarne_toit_G",new Vector3(-.30f,.875f,-.22f),new Vector3(.16f,.025f,.27f),snow,new Vector3(0,0,-28));
            Box(t,"Lucarne_toit_D",new Vector3(-.18f,.875f,-.22f),new Vector3(.16f,.025f,.27f),snow,new Vector3(0,0,28));
            Box(t,"Cheminee",new Vector3(.33f,.84f,.20f),new Vector3(.12f,.34f,.14f),dark);
            Box(t,"Neige_cheminee",new Vector3(.33f,1.025f,.20f),new Vector3(.15f,.035f,.17f),snow);
        }
        static void EntranceCanopy(Transform t)
        {
            Color wood=new Color(.22f,.105f,.04f), snow=new Color(.97f,.99f,1f), warm=new Color(1f,.63f,.28f);
            Box(t,"Marquise_entree",new Vector3(0,.18f,-.79f),new Vector3(.34f,.035f,.27f),wood,new Vector3(8,0,0));
            Box(t,"Neige_marquise",new Vector3(0,.215f,-.79f),new Vector3(.36f,.022f,.28f),snow,new Vector3(8,0,0));
            Box(t,"Lampe_entree",new Vector3(0,.34f,-.72f),new Vector3(.055f,.08f,.045f),warm);
        }
        public static void Apply(GameObject b, StationServiceType type, string displayName)
        {
            if(b==null)return;
            StationServiceVisual275.Apply(b,type,displayName);
            var t=b.transform;
            Balcony(t); Shutters(t); DormerAndChimney(t); EntranceCanopy(t);
        }
    }
}
