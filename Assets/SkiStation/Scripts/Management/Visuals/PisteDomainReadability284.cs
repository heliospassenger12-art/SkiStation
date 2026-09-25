using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>2.84 visual-only: makes real player-built pistes readable from medium/long management-camera distances.</summary>
    public sealed class PisteDomainReadability284 : MonoBehaviour
    {
        static PisteDomainReadability284 instance;
        readonly HashSet<int> decorated = new HashSet<int>();
        Material snowMat, greenMat, blueMat, redMat, blackMat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive || instance != null) return;
            var go = new GameObject("SkiStation_284_LisibiliteDomainePistes_VISUEL");
            instance = go.AddComponent<PisteDomainReadability284>();
        }

        void Start()
        {
            snowMat = MakeMat("284_NeigePisteLisible", new Color(.78f,.88f,.93f,1f), .16f);
            greenMat = MakeMat("284_Vert", new Color(.12f,.48f,.20f,1f), .22f);
            blueMat = MakeMat("284_Bleu", new Color(.08f,.30f,.68f,1f), .22f);
            redMat = MakeMat("284_Rouge", new Color(.70f,.08f,.06f,1f), .22f);
            blackMat = MakeMat("284_Noir", new Color(.035f,.04f,.05f,1f), .18f);
            InvokeRepeating(nameof(Refresh), 1.8f, 3f);
        }

        void Refresh()
        {
            if (SkiStation.Arcade.ArcadeSceneIsolation.IsActive) return;
            foreach (var slope in FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
            {
                if (slope == null || slope.Points == null || slope.Points.Count < 2) continue;
                if (!slope.name.StartsWith("Piste_") || !decorated.Add(slope.GetInstanceID())) continue;
                Decorate(slope);
            }
        }

        void Decorate(SlopeRuntime slope)
        {
            var root = new GameObject("284_Lisibilite_" + slope.name);
            root.transform.SetParent(slope.transform, true);
            Material difficulty = DifficultyMat(slope.Difficulty);
            float travelled = 0f;
            float nextBeacon = 18f;
            int snowSegments = 0, beacons = 0;

            for (int i = 1; i < slope.Points.Count; i++)
            {
                Vector3 a = slope.Points[i-1], b = slope.Points[i];
                float len = Vector3.Distance(a,b); if (len < .8f) continue;
                Vector3 dir = (b-a).normalized;
                Vector3 across = Vector3.Cross(Vector3.up, dir).normalized;
                if (across.sqrMagnitude < .1f) across = Vector3.right;
                float width = Mathf.Clamp((slope.GetWidthAt(i-1)+slope.GetWidthAt(i))*.5f, 4f, 30f);

                // Broad, extremely shallow snow panel: reinforces piste silhouette from afar without changing terrain/gameplay.
                if (snowSegments < 90)
                {
                    var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    s.name = "NeigePisteDistance"; s.transform.SetParent(root.transform,true);
                    s.transform.position = (a+b)*.5f + Vector3.up*.025f;
                    s.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    s.transform.localScale = new Vector3(width*.78f,.012f,len*.96f);
                    StripCollider(s); SetMat(s,snowMat); snowSegments++;
                }

                float segmentStart = travelled, segmentEnd = travelled + len;
                while (nextBeacon <= segmentEnd && beacons < 70)
                {
                    float t = Mathf.Clamp01((nextBeacon-segmentStart)/len);
                    Vector3 p = Vector3.Lerp(a,b,t);
                    AddBeacon(root,p + across*width*.48f, difficulty, slope.Difficulty);
                    // Alternate sides for a natural piste-edge rhythm.
                    if ((beacons & 1) == 1) AddBeacon(root,p - across*width*.48f, difficulty, slope.Difficulty);
                    beacons++; nextBeacon += 24f;
                }
                travelled = segmentEnd;
            }
            Debug.Log("SkiStation 2.84: piste " + slope.name + " renforcee pour lecture moyenne/longue distance (visuel uniquement).");
        }

        void AddBeacon(GameObject root, Vector3 p, Material mat, SlopeDifficulty difficulty)
        {
            var pole=GameObject.CreatePrimitive(PrimitiveType.Cylinder); pole.name="JalonPiste_"+difficulty;
            pole.transform.SetParent(root.transform,true); pole.transform.position=p+Vector3.up*.72f;
            pole.transform.localScale=new Vector3(.055f,.72f,.055f); StripCollider(pole); SetMat(pole,mat);
            var cap=GameObject.CreatePrimitive(PrimitiveType.Sphere); cap.name="RepereCouleur";
            cap.transform.SetParent(root.transform,true); cap.transform.position=p+Vector3.up*1.48f;
            cap.transform.localScale=Vector3.one*.22f; StripCollider(cap); SetMat(cap,mat);
        }

        Material DifficultyMat(SlopeDifficulty d)
        {
            switch(d){case SlopeDifficulty.Green:return greenMat;case SlopeDifficulty.Blue:return blueMat;case SlopeDifficulty.Red:return redMat;default:return blackMat;}
        }
        static void StripCollider(GameObject g){var c=g.GetComponent<Collider>();if(c!=null)Destroy(c);}
        static void SetMat(GameObject g,Material m){var r=g.GetComponent<Renderer>();if(r!=null&&m!=null)r.sharedMaterial=m;}
        static Material MakeMat(string n,Color c,float smooth){var s=Shader.Find("Universal Render Pipeline/Lit");if(s==null)s=Shader.Find("Standard");if(s==null)s=Shader.Find("Unlit/Color");if(s==null)return null;var m=new Material(s){name=n,color=c};if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);return m;}
        void OnDestroy(){if(instance==this)instance=null; foreach(var m in new[]{snowMat,greenMat,blueMat,redMat,blackMat}) if(m!=null)Destroy(m);}
    }
}
