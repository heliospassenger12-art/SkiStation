using UnityEngine;

namespace SkiStation
{
    /// <summary>Visual pass 2.57: richer alpine service buildings without changing service gameplay.</summary>
    public static class StationServiceVisual257
    {
        static Material Mat(Color c) => RuntimeMaterialFactory.Create(c);
        static Transform Box(Transform p,string n,Vector3 pos,Vector3 scale,Color c,Vector3 rot=default)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=n; g.transform.SetParent(p,false);
            g.transform.localPosition=pos; g.transform.localScale=scale; g.transform.localEulerAngles=rot;
            var col=g.GetComponent<Collider>(); if(col!=null) Object.Destroy(col);
            var r=g.GetComponent<Renderer>(); if(r!=null){var m=Mat(c);if(m!=null)r.sharedMaterial=m;} return g.transform;
        }
        static void Lamp(Transform p,float x,float z)
        {
            Box(p,"Lampadaire_pied",new Vector3(x,-.10f,z),new Vector3(.025f,.72f,.025f),new Color(.12f,.14f,.15f));
            Box(p,"Lampadaire_lanterne",new Vector3(x,.27f,z),new Vector3(.09f,.13f,.09f),new Color(1f,.70f,.30f));
        }
        public static void Apply(GameObject b, StationServiceType type, string displayName)
        {
            if(b==null)return;
            // Keep 2.53 as the architectural base, then add readable close-up detail.
            StationServiceVisual253.Apply(b,type,displayName);
            var t=b.transform; bool restaurant=type==StationServiceType.Restaurant, bar=type==StationServiceType.Bar;
            Color timber=new Color(.25f,.14f,.075f), stone=new Color(.43f,.42f,.39f), warm=new Color(1f,.58f,.22f);
            Color snow=new Color(.94f,.97f,1f), metal=new Color(.13f,.15f,.16f);

            // Timber frame and stone corners give the runtime prototype a recognisable alpine silhouette.
            foreach(float x in new[]{-.47f,.47f})
            {
                Box(t,"Poteau_bois",new Vector3(x,.02f,-.535f),new Vector3(.045f,.83f,.055f),timber);
                Box(t,"Angle_pierre",new Vector3(x,-.30f,0),new Vector3(.075f,.31f,1.035f),stone);
            }
            Box(t,"Poutre_facade_haute",new Vector3(0,.38f,-.54f),new Vector3(.96f,.045f,.06f),timber);
            Box(t,"Balcon",new Vector3(0,.29f,-.62f),new Vector3(.82f,.035f,.18f),timber);
            for(int i=-3;i<=3;i++) Box(t,"Barreau_balcon",new Vector3(i*.12f,.40f,-.70f),new Vector3(.018f,.23f,.018f),timber);
            Box(t,"Main_courante",new Vector3(0,.51f,-.70f),new Vector3(.82f,.025f,.025f),timber);
            Box(t,"Cheminee",new Vector3(.29f,.79f,.18f),new Vector3(.11f,.35f,.11f),stone);
            Box(t,"Chapeau_cheminee",new Vector3(.29f,.98f,.18f),new Vector3(.15f,.045f,.15f),snow);

            // Warm window cores remain visible even without post-processing/baked lighting.
            foreach(float x in new[]{-.31f,.31f})
                Box(t,"Lueur_fenetre",new Vector3(x,.07f,-.548f),new Vector3(.18f,.27f,.012f),warm);

            if(restaurant||bar)
            {
                // More believable terrace: snow edge, tables, benches, parasols and lamps.
                Box(t,"Bord_neige_terrasse",new Vector3(0,-.445f,-1.00f),new Vector3(1.06f,.055f,.08f),snow);
                for(int i=-2;i<=2;i++)
                {
                    float x=i*.19f;
                    Box(t,"Chaise_"+i,new Vector3(x,-.38f,-.88f),new Vector3(.07f,.13f,.07f),timber);
                }
                foreach(float x in new[]{-.30f,.30f})
                {
                    Box(t,"Parasol_mat",new Vector3(x,-.16f,-.83f),new Vector3(.018f,.48f,.018f),metal);
                    var cap=Box(t,"Parasol",new Vector3(x,.08f,-.83f),new Vector3(.33f,.035f,.33f),bar?new Color(.30f,.12f,.10f):new Color(.72f,.18f,.12f));
                    cap.localEulerAngles=new Vector3(0,45f,0);
                }
                Lamp(t,-.52f,-.78f); Lamp(t,.52f,-.78f);
            }
            if(restaurant)
            {
                Box(t,"Panneau_restaurant",new Vector3(0,.48f,-.575f),new Vector3(.50f,.12f,.035f),new Color(.17f,.10f,.06f));
                // Restaurant d'altitude gets a wider panoramic rear glazing.
                Box(t,"Verriere_altitude",new Vector3(0,.10f,.53f),new Vector3(.72f,.34f,.025f),new Color(.20f,.48f,.62f));
            }
            else if(bar)
            {
                Box(t,"Bandeau_apres_ski",new Vector3(0,.47f,-.575f),new Vector3(.48f,.11f,.035f),new Color(.25f,.09f,.08f));
            }
        }
    }
}
