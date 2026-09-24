using UnityEngine;
namespace SkiStation.Arcade
{
    // Unit-scale pivots; scale only mesh children.
    public static class ArcadeSkierRig
    {
        static Transform Pivot(string name, Transform parent, Vector3 position)
        {
            Transform t=new GameObject(name).transform;
            t.SetParent(parent,false); t.localPosition=position; return t;
        }
        static Transform Part(string name,Transform parent,Vector3 position,Vector3 scale,
            Material material,PrimitiveType type=PrimitiveType.Capsule)
        {
            GameObject go=GameObject.CreatePrimitive(type);
            go.name=name; go.transform.SetParent(parent,false);
            go.transform.localPosition=position; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;
            Collider c=go.GetComponent<Collider>(); c.enabled=false; Object.Destroy(c);
            return go.transform;
        }
        public static void Build(Transform avatar,ArcadeSkiController controller,Material jacket,Material dark,Material snow)
        {
            Transform root=Pivot("Arcade body visual",avatar,Vector3.up);
            Transform pelvis=Pivot("Pelvis",root,Vector3.zero);
            Part("Hips",pelvis,Vector3.zero,new Vector3(.48f,.17f,.28f),dark);
            Transform chest=Pivot("Chest",pelvis,new Vector3(0f,.12f,0f));
            Part("Jacket",chest,new Vector3(0f,.24f,0f),new Vector3(.56f,.30f,.34f),jacket);
            Transform head=Pivot("Head",chest,new Vector3(0f,.65f,0f));
            Part("Helmet",head,Vector3.zero,new Vector3(.37f,.4f,.4f),dark,PrimitiveType.Sphere);
            Part("Goggles",head,new Vector3(0f,0f,.18f),new Vector3(.3f,.10f,.05f),snow,PrimitiveType.Cube);
            for(int i=0;i<2;i++)
            {
                string side=i==0?"Left":"Right"; float x=i==0?-.23f:.23f;
                Part(side+" ski",root,new Vector3(x,-1.04f,.18f),new Vector3(.18f,.07f,2.1f),dark,PrimitiveType.Cube);
                Part(side+" boot",root,new Vector3(x,-.92f,0f),new Vector3(.18f,.21f,.32f),jacket,PrimitiveType.Cube);
                Part(side+" thigh mesh",root,Vector3.zero,Vector3.one,dark);
                Part(side+" shin mesh",root,Vector3.zero,Vector3.one,dark);
                Part(side+" arm mesh",root,Vector3.zero,Vector3.one,jacket);
                Part(side+" forearm mesh",root,Vector3.zero,Vector3.one,snow);
            }
            ArcadeHumanoidPose pose=avatar.gameObject.AddComponent<ArcadeHumanoidPose>();
            pose.controller=controller; pose.visualRoot=root;
        }
    }
}
