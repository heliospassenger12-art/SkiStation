#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SkiStation.Arcade;

// No NUnit/package dependency. Runs the actual leg solver in the installed Unity.
public static class SkiStationArcadeChecks
{
    [MenuItem("SkiStation/Arcade/Verifier geometrie et glisse 2.42")]
    public static void Run()
    {
        int count=0;
        for(int c=0;c<=100;c++)
        for(int l=-45;l<=45;l++)
        for(int side=-1;side<=1;side+=2)
        {
            float crouch=c/100f;
            Vector3 hip=ArcadeHumanoidPose.RaceHipOffset(crouch,l)
                + Quaternion.Euler(0f,0f,l*.35f)*new Vector3(side*.23f,0f,0f);
            Vector3 foot=new Vector3(side*.23f,-.86f,0f);
            Vector3 knee=ArcadeHumanoidPose.SolveKnee(hip,foot);
            if(Mathf.Abs(Vector3.Distance(hip,knee)-.5f)>.0001f ||
                Mathf.Abs(Vector3.Distance(foot,knee)-.5f)>.0001f ||
                knee.z<(hip.z+foot.z)*.5f)
                throw new System.Exception("Pose incorrecte: charge="+c+" inclinaison="+l);
            count++;
        }
        foreach (float slope in new float[] { 0f, 15f, 35f })
        {
            Vector3 normal=Quaternion.Euler(slope,0f,0f)*Vector3.up;
            Vector3 trajectory=Vector3.ProjectOnPlane(Vector3.forward,normal).normalized;
            foreach (float angle in new float[] { -180f,-160f,-90f,-20.01f,-20f,0f,20f,20.01f,90f,159.99f,160f,180f,200f,200.01f,360f })
            {
                Vector3 skis=Quaternion.AngleAxis(angle,normal)*trajectory;
                float expectedError=Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(angle,0f)),Mathf.Abs(Mathf.DeltaAngle(angle,180f)));
                bool expected=expectedError<=20f+.0001f;
                if(ArcadeSkiRules.CanLand(skis,trajectory,normal)!=expected)
                    throw new System.Exception("Reception incorrecte: "+angle+" pente="+slope);
            }
        }
        Vector3 downhill=new Vector3(0f,-.3f,1f).normalized;
        if(ArcadeSkiRules.CameraYaw(0f,10f,12f,35f,1f)!=0f ||
            Mathf.Abs(ArcadeSkiRules.CameraYaw(0f,90f,12f,35f,1f)-35f)>.001f)
            throw new System.Exception("Camera trop reactive.");
        if(ArcadeSkiRules.StepSpeed(0f,Vector3.forward,29f,.1f,.02f)!=0f)
            throw new System.Exception("Acceleration sur le plat.");
        if(ArcadeSkiRules.StepSpeed(10f,-downhill,29f,.1f,.02f)>=10f)
            throw new System.Exception("Gain de vitesse en montee.");
        if(ArcadeSkiRules.StepSpeed(0f,downhill,29f,.1f,.02f)<=0f)
            throw new System.Exception("Aucune acceleration en descente.");
        Debug.Log("[Arcade 2.42] PASS : "+count+" poses, receptions face/switch +/-20 deg et acceleration gravitaire.");
    }
}
#endif
