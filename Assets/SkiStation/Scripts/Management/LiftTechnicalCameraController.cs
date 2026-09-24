using UnityEngine;
public class LiftTechnicalCameraController : MonoBehaviour {
 [SerializeField] float distance=35f,heightOffset=12f,moveSpeed=12f; Transform target; Vector3 desired;
 void Awake(){desired=transform.position;}
 void Update(){if(target==null)return;Vector3 focus=target.position+Vector3.up*heightOffset*.25f;desired=focus-target.forward*distance+Vector3.up*heightOffset;transform.position=Vector3.Lerp(transform.position,desired,Time.unscaledDeltaTime*moveSpeed);transform.LookAt(focus);}
 public void Focus(LiftPylonRuntime p){if(p!=null)target=p.transform;} public void ClearFocus(){target=null;}
}