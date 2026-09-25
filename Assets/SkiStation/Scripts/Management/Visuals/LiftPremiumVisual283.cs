using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.83 visual-only premium dressing for constructed surface lifts. Never changes lift logic, route or colliders.</summary>
    public sealed class LiftPremiumVisual283 : MonoBehaviour
    {
        static LiftPremiumVisual283 instance;
        readonly HashSet<int> decorated = new HashSet<int>();
        Material wood, stone, metal, dark, glass, snow, accent;
        float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var go = new GameObject("SkiStation_283_Remontees_Premium_VISUEL");
            instance = go.AddComponent<LiftPremiumVisual283>();
        }

        void Awake()
        {
            wood = Mat(new Color(.27f,.15f,.08f)); stone = Mat(new Color(.34f,.34f,.33f));
            metal = Mat(new Color(.31f,.34f,.36f)); dark = Mat(new Color(.08f,.10f,.12f));
            glass = Mat(new Color(.20f,.36f,.43f)); snow = Mat(new Color(.96f,.985f,1f));
            accent = Mat(new Color(.68f,.08f,.055f));
        }

        void Update()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) { Destroy(gameObject); return; }
            if (Time.unscaledTime < nextScan) return; nextScan = Time.unscaledTime + 1.5f;
            foreach (var lift in FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None))
            {
                if (lift == null || lift.Definition == null || lift.Route == null || lift.Route.Count < 2 || lift.IsUnderground) continue;
                int id = lift.GetInstanceID(); if (decorated.Contains(id)) continue;
                Decorate(lift); decorated.Add(id);
            }
        }

        void Decorate(LiftRuntime lift)
        {
            var root = new GameObject("Habillage_premium_283"); root.transform.SetParent(lift.transform, false);
            Vector3 a = lift.Bottom, b = lift.Top;
            Vector3 dir = b-a; dir.y=0f; if(dir.sqrMagnitude<.01f) dir=Vector3.forward; dir.Normalize();
            Terminal(root.transform, a, dir, "Depart", lift.Definition.type);
            Terminal(root.transform, b, -dir, "Arrivee", lift.Definition.type);
            if (IsAerial(lift.Definition.type)) LineDetails(root.transform, a, b, lift.Definition.type);
            Debug.Log("SkiStation 2.83: habillage premium ajoute a " + lift.LiftId + " (visuel uniquement).");
        }

        void Terminal(Transform parent, Vector3 p, Vector3 dir, string label, LiftType type)
        {
            Quaternion q=Quaternion.LookRotation(dir,Vector3.up); float w = (type==LiftType.CableCar||type==LiftType.ThreeS)?14f:10.5f;
            Box(parent,label+"_Soubassement",p+Vector3.up*.18f,new Vector3(w+2f,.35f,10.5f),stone,q);
            Box(parent,label+"_BandeauBois",p+Vector3.up*3.8f+q*Vector3.forward*4.25f,new Vector3(w,.75f,.18f),wood,q);
            Box(parent,label+"_Vitrage",p+Vector3.up*2.45f+q*Vector3.forward*4.38f,new Vector3(w*.70f,1.65f,.08f),glass,q);
            Box(parent,label+"_Auvent",p+Vector3.up*4.65f+q*Vector3.forward*5.25f,new Vector3(w+2.2f,.18f,3.0f),metal,q);
            Box(parent,label+"_NeigeAuvent",p+Vector3.up*4.80f+q*Vector3.forward*5.25f,new Vector3(w+2.35f,.12f,3.15f),snow,q);
            Box(parent,label+"_Enseigne",p+Vector3.up*4.05f+q*Vector3.forward*5.40f,new Vector3(3.6f,.62f,.10f),accent,q);
            for(int s=-1;s<=1;s+=2) Box(parent,label+"_Pilier",p+q*Vector3.right*(s*(w*.43f))+q*Vector3.forward*4.55f+Vector3.up*2.0f,new Vector3(.28f,4f,.28f),dark,q);
            // Snow banks visually seat the terminal in the terrain without modifying terrain geometry.
            for(int s=-1;s<=1;s+=2) Box(parent,label+"_Congere",p+q*Vector3.right*(s*(w*.55f))+q*Vector3.forward*.5f+Vector3.up*.22f,new Vector3(1.4f,.42f,7.5f),snow,q);
        }

        void LineDetails(Transform parent, Vector3 a, Vector3 b, LiftType type)
        {
            float horizontal=Vector3.Distance(new Vector3(a.x,0,a.z),new Vector3(b.x,0,b.z));
            int n=Mathf.Clamp(Mathf.RoundToInt(horizontal/85f),2,10);
            for(int i=1;i<n;i++)
            {
                float t=i/(float)n; Vector3 p=Vector3.Lerp(a,b,t);
                // compact concrete/stone footings under the visual line; no colliders and no effect on actual pylon mechanics.
                Box(parent,"SoclePylone_283",p+Vector3.up*.18f,new Vector3(2.3f,.36f,2.3f),stone,Quaternion.identity);
                Box(parent,"ProtectionPylone_283",p+Vector3.up*1.05f,new Vector3(.78f,1.75f,.78f),accent,Quaternion.identity);
                Box(parent,"NeigeSocle_283",p+Vector3.up*.40f,new Vector3(2.55f,.12f,2.55f),snow,Quaternion.identity);
            }
        }

        bool IsAerial(LiftType t) => t==LiftType.FixedChair||t==LiftType.DetachableChair||t==LiftType.BubbleChair||t==LiftType.Gondola||t==LiftType.CableCar||t==LiftType.ThreeS;
        Material Mat(Color c){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Sprites/Default");var m=new Material(s);m.color=c;return m;}
        GameObject Box(Transform parent,string n,Vector3 p,Vector3 scale,Material m,Quaternion q){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent,false);g.transform.position=p;g.transform.rotation=q;g.transform.localScale=scale;var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        void OnDestroy(){if(instance==this)instance=null;foreach(var m in new[]{wood,stone,metal,dark,glass,snow,accent})if(m!=null)Destroy(m);}
    }
}
