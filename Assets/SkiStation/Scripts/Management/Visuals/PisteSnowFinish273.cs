using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>
    /// 2.73 visual-only piste snow finish. Adds subtle groomer corduroy and soft edge
    /// definition to player-built pistes. No colliders and no gameplay state changes.
    /// </summary>
    public sealed class PisteSnowFinish273 : MonoBehaviour
    {
        static PisteSnowFinish273 instance;
        readonly HashSet<int> decorated = new HashSet<int>();
        Material groomMat, edgeMat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var go = new GameObject("SkiStation_273_FinitionNeigePistes_VISUEL");
            instance = go.AddComponent<PisteSnowFinish273>();
        }

        void Start()
        {
            groomMat = MakeMat("273_NeigeDamee", new Color(.84f,.91f,.95f,1f), .18f);
            edgeMat = MakeMat("273_BordNeige", new Color(.94f,.97f,.985f,1f), .28f);
            InvokeRepeating(nameof(Refresh), 1.4f, 3f);
        }

        void Refresh()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            foreach (var slope in FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
            {
                if (slope == null || !slope.name.StartsWith("Piste_") || slope.Points == null || slope.Points.Count < 2) continue;
                if (!decorated.Add(slope.GetInstanceID())) continue;
                Decorate(slope);
            }
        }

        void Decorate(SlopeRuntime slope)
        {
            var root = new GameObject("273_FinitionNeige_" + slope.name);
            root.transform.SetParent(slope.transform, true);
            int made = 0;
            for (int i=1; i<slope.Points.Count && made<180; i++)
            {
                Vector3 a=slope.Points[i-1], b=slope.Points[i];
                float len=Vector3.Distance(a,b); if(len<1f) continue;
                Vector3 dir=(b-a).normalized;
                Vector3 across=Vector3.Cross(Vector3.up,dir).normalized;
                if(across.sqrMagnitude<.1f) across=Vector3.right;
                float w=Mathf.Clamp((slope.GetWidthAt(i-1)+slope.GetWidthAt(i))*.5f,4f,28f);
                int count=Mathf.Clamp(Mathf.FloorToInt(len/7f),1,24);
                for(int j=0;j<count && made<180;j++)
                {
                    float t=(j+.5f)/count; Vector3 p=Vector3.Lerp(a,b,t)+Vector3.up*.055f;
                    // Alternating narrow grooming bands create readable snow texture from management camera.
                    var band=GameObject.CreatePrimitive(PrimitiveType.Cube); band.name="Dameuse_Sillon";
                    band.transform.SetParent(root.transform,true); band.transform.position=p;
                    band.transform.rotation=Quaternion.LookRotation(across,Vector3.up);
                    band.transform.localScale=new Vector3(.055f,.018f,w*.86f);
                    StripCollider(band); SetMat(band,groomMat); made++;
                }
                // Soft snow lips on both piste edges, intentionally low and sparse.
                if(i%2==0)
                {
                    AddEdge(root, Vector3.Lerp(a,b,.5f)+across*w*.49f+Vector3.up*.08f, dir, len, edgeMat);
                    AddEdge(root, Vector3.Lerp(a,b,.5f)-across*w*.49f+Vector3.up*.08f, dir, len, edgeMat);
                }
            }
            Debug.Log("SkiStation 2.73: finition neige ajoutee sur " + slope.name + " (damage visuel + bords doux, gameplay inchange).");
        }

        static void AddEdge(GameObject root,Vector3 p,Vector3 dir,float len,Material mat)
        {
            var e=GameObject.CreatePrimitive(PrimitiveType.Cube); e.name="BordNeigeDoux"; e.transform.SetParent(root.transform,true);
            e.transform.position=p; e.transform.rotation=Quaternion.LookRotation(dir,Vector3.up);
            e.transform.localScale=new Vector3(.22f,.09f,Mathf.Min(len*.92f,24f)); StripCollider(e); SetMat(e,mat);
        }
        static void StripCollider(GameObject g){var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);}
        static void SetMat(GameObject g,Material m){var r=g.GetComponent<Renderer>();if(r!=null)r.sharedMaterial=m;}
        static Material MakeMat(string n,Color c,float smooth){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Unlit/Color");if(s==null)return null;var m=new Material(s){name=n,color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);return m;}
        void OnDestroy(){if(instance==this)instance=null;}
    }
}
