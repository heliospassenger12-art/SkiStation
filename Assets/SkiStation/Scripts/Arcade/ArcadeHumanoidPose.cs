using UnityEngine;
namespace SkiStation.Arcade
{
    // Procedural presentation only. Two-link legs solve towards fixed boots.
    public sealed class ArcadeHumanoidPose : MonoBehaviour
    {
        public ArcadeSkiController controller;
        public Transform visualRoot;
        [Range(0f,45f)] public float maximumRaceLean = 38f;
        Transform pelvis,chest,head;
        readonly Transform[] thigh=new Transform[2],shin=new Transform[2],
            arm=new Transform[2],forearm=new Transform[2];
        float crouch,landing,grab,lean,pitch,terrain;
        Vector3 previousNormal=Vector3.up;
        void Start()
        {
            if(visualRoot==null || controller==null) return;
            pelvis=visualRoot.Find("Pelvis"); if(pelvis==null) return;
            chest=pelvis.Find("Chest"); head=chest.Find("Head");
            for(int i=0;i<2;i++)
            {
                string s=i==0?"Left":"Right";
                thigh[i]=visualRoot.Find(s+" thigh mesh"); shin[i]=visualRoot.Find(s+" shin mesh");
                arm[i]=visualRoot.Find(s+" arm mesh"); forearm[i]=visualRoot.Find(s+" forearm mesh");
            }
            controller.Landed+=Land; controller.Retried+=ResetPose;
        }
        void OnDestroy()
        {
            if(controller==null) return;
            controller.Landed-=Land; controller.Retried-=ResetPose;
        }
        void Land(float impact) { landing=Mathf.Clamp01(impact/28f); }
        void ResetPose()
        {
            crouch=landing=grab=lean=pitch=terrain=0f;
            previousNormal=controller.GroundNormal;
        }
        void LateUpdate()
        {
            if(pelvis==null || controller==null || !controller.enabled || controller.IsCrashed) return;
            float dt=Time.deltaTime; if(dt<=0f) return;
            bool air=controller.IsAirborne;
            float speed=Mathf.Clamp01(controller.Speed/Mathf.Max(1f,controller.maximumSpeed));
            float blend=1f-Mathf.Exp(-10f*dt);
            float curvature=Vector3.Angle(previousNormal,controller.GroundNormal)/dt;
            previousNormal=controller.GroundNormal;
            terrain=Mathf.Lerp(terrain,!air?Mathf.Clamp01(curvature/100f)*speed*.2f:0f,blend);
            float target=air?.25f:.12f+speed*.25f+(controller.IsBraking?.12f:0f)+terrain;
            if(controller.IsChargingJump && !air) target=Mathf.Lerp(target,.95f,controller.JumpCharge01);
            crouch=Mathf.Lerp(crouch,Mathf.Clamp01(target+landing*.45f),blend);
            grab=Mathf.Lerp(grab,air && Input.GetKey(KeyCode.H)?1f:0f,blend);
            lean=Mathf.Lerp(lean,!air?-controller.SteeringInput*Mathf.Clamp01(controller.Speed/22f)
                *maximumRaceLean*(controller.IsSwitch?-1f:1f):0f,blend);
            pitch=Mathf.Lerp(pitch,air?10f:controller.IsBraking?5f:10f+speed*22f,blend);
            landing=Mathf.MoveTowards(landing,0f,dt*2.4f);
            pelvis.localPosition=RaceHipOffset(crouch,lean);
            pelvis.localRotation=Quaternion.Euler(0f,0f,lean*.35f);
            chest.localRotation=Quaternion.Euler(pitch+crouch*8f+grab*12f,0f,lean*.65f);
            head.localRotation=Quaternion.Euler(-Mathf.Clamp(pitch*.65f,0f,25f),0f,-lean*.3f);
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1f:1f;
                Vector3 hip=visualRoot.InverseTransformPoint(pelvis.TransformPoint(new Vector3(side*.23f,0f,0f)));
                Vector3 foot=new Vector3(side*.23f,-.86f,0f);
                Vector3 knee=SolveKnee(hip,foot);
                Segment(thigh[i],hip,knee,.17f); Segment(shin[i],knee,foot,.14f);
                Vector3 shoulder=visualRoot.InverseTransformPoint(chest.TransformPoint(new Vector3(side*.30f,.40f,0f)));
                Vector3 hand=Vector3.Lerp(shoulder+new Vector3(side*.10f,-.43f,.25f),knee+new Vector3(side*.08f,0f,.08f),grab);
                Vector3 reach=Vector3.ClampMagnitude(hand-shoulder,.62f); hand=shoulder+reach;
                Vector3 elbow=(shoulder+hand)*.5f+Vector3.ProjectOnPlane(Vector3.back,reach.normalized).normalized*
                    Mathf.Sqrt(Mathf.Max(0f,.1024f-reach.sqrMagnitude*.25f));
                Segment(arm[i],shoulder,elbow,.12f); Segment(forearm[i],elbow,hand,.10f);
            }
        }
        public static Vector3 SolveKnee(Vector3 hip,Vector3 foot)
        {
            Vector3 axis=foot-hip;
            float d=Mathf.Clamp(axis.magnitude,.15f,.99f);
            Vector3 bend=Vector3.ProjectOnPlane(Vector3.forward,axis.normalized).normalized;
            return (hip+foot)*.5f+bend*Mathf.Sqrt(Mathf.Max(0f,.25f-d*d*.25f));
        }
        public static Vector3 RaceHipOffset(float crouch,float lean)
        {
            return new Vector3(-Mathf.Sin(lean*Mathf.Deg2Rad)*.5f,
                -crouch*.32f-Mathf.Abs(lean)/45f*.12f,-crouch*.12f);
        }
        static void Segment(Transform mesh,Vector3 a,Vector3 b,float width)
        {
            if(mesh==null) return;
            mesh.localPosition=(a+b)*.5f;
            mesh.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
            mesh.localScale=new Vector3(width,Vector3.Distance(a,b)*.5f,width);
        }
    }
}
