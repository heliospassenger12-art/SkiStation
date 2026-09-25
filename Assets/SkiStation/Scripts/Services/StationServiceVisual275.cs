using UnityEngine;

namespace SkiStation
{
    /// <summary>2.75 premium material/detail pass for runtime service buildings. Visual only.</summary>
    public static class StationServiceVisual275
    {
        static Material Mat(Color c) => RuntimeMaterialFactory.Create(c);
        static Transform Box(Transform p,string n,Vector3 pos,Vector3 scale,Color c,Vector3 rot=default)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=n; g.transform.SetParent(p,false);
            g.transform.localPosition=pos; g.transform.localScale=scale; g.transform.localEulerAngles=rot;
            var col=g.GetComponent<Collider>(); if(col!=null) Object.Destroy(col);
            var r=g.GetComponent<Renderer>(); if(r!=null){var m=Mat(c); if(m!=null) r.sharedMaterial=m;} return g.transform;
        }
        static void WoodSlats(Transform t,float z)
        {
            Color a=new Color(.28f,.145f,.065f), b=new Color(.20f,.095f,.04f);
            for(int i=-8;i<=8;i++) Box(t,"Lame_bois_"+i,new Vector3(i*.058f,.02f,z),new Vector3(.048f,.72f,.018f),(i&1)==0?a:b);
        }
        static void StoneCourse(Transform t,float z)
        {
            Color s1=new Color(.39f,.385f,.37f), s2=new Color(.31f,.305f,.295f);
            for(int row=0;row<3;row++) for(int i=-5;i<=5;i++)
            {
                float x=i*.095f+(row%2==0?0:.045f);
                Box(t,"Pierre_facade",new Vector3(x,-.39f+row*.065f,z),new Vector3(.085f,.052f,.026f),((i+row)&1)==0?s1:s2);
            }
        }
        static void RoofSnow(Transform t)
        {
            Color snow=new Color(.965f,.985f,1f), ice=new Color(.76f,.89f,.96f);
            Box(t,"Neige_toit_G",new Vector3(-.255f,.735f,0),new Vector3(.57f,.035f,1.18f),snow,new Vector3(0,0,-25));
            Box(t,"Neige_toit_D",new Vector3(.255f,.735f,0),new Vector3(.57f,.035f,1.18f),snow,new Vector3(0,0,25));
            for(int i=-4;i<=4;i++) Box(t,"Glacon_"+i,new Vector3(i*.105f,.49f,-.625f),new Vector3(.014f,.08f+(i%3)*.018f,.014f),ice,new Vector3(0,0,6));
        }
        static void Glass(Transform t,float x,float y,float z,float w,float h)
        {
            Color glass=new Color(.12f,.33f,.43f), glow=new Color(1f,.58f,.24f), frame=new Color(.08f,.095f,.10f);
            Box(t,"Verre_premium",new Vector3(x,y,z),new Vector3(w,h,.012f),glass);
            Box(t,"Lueur_interieure",new Vector3(x,y,z-.008f),new Vector3(w*.82f,h*.78f,.006f),glow);
            Box(t,"Cadre_verre_H",new Vector3(x,y+h*.52f,z-.012f),new Vector3(w*1.08f,.018f,.02f),frame);
            Box(t,"Cadre_verre_B",new Vector3(x,y-h*.52f,z-.012f),new Vector3(w*1.08f,.018f,.02f),frame);
            Box(t,"Cadre_verre_G",new Vector3(x-w*.52f,y,z-.012f),new Vector3(.018f,h*1.08f,.02f),frame);
            Box(t,"Cadre_verre_D",new Vector3(x+w*.52f,y,z-.012f),new Vector3(.018f,h*1.08f,.02f),frame);
        }
        public static void Apply(GameObject b, StationServiceType type, string displayName)
        {
            if(b==null)return;
            StationServiceVisual265.Apply(b,type,displayName);
            Transform t=b.transform;
            WoodSlats(t,-.588f); StoneCourse(t,-.606f); RoofSnow(t);
            Glass(t,-.30f,.055f,-.620f,.19f,.25f); Glass(t,.30f,.055f,-.620f,.19f,.25f);
            Color metal=new Color(.09f,.105f,.115f), brass=new Color(.62f,.47f,.23f), snow=new Color(.97f,.99f,1f);
            Box(t,"Gouttiere",new Vector3(0,.49f,-.645f),new Vector3(1.08f,.025f,.025f),metal);
            foreach(float x in new[]{-.47f,.47f}) Box(t,"Descente_gouttiere",new Vector3(x,.03f,-.635f),new Vector3(.022f,.90f,.022f),metal);
            Box(t,"Plaque_entree",new Vector3(0,.33f,-.655f),new Vector3(.38f,.105f,.022f),new Color(.13f,.075f,.035f));
            Box(t,"Liseret_plaque",new Vector3(0,.33f,-.670f),new Vector3(.34f,.012f,.010f),brass);
            Box(t,"Seuil_neige",new Vector3(0,-.49f,-.70f),new Vector3(.38f,.035f,.22f),snow);
        }
    }
}
