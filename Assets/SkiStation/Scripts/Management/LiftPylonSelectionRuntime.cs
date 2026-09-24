using System; using UnityEngine;
public class LiftPylonSelectionRuntime : MonoBehaviour {
 public static LiftPylonSelectionRuntime Instance{get;private set;} [SerializeField] Camera targetCamera; [SerializeField] LayerMask pylonMask=~0; [SerializeField] float maxRayDistance=1000f;
 public LiftPylonRuntime Selected{get;private set;} public event Action<LiftPylonRuntime> SelectionChanged;
 void Awake(){Instance=this;if(targetCamera==null)targetCamera=Camera.main;}
 void Update(){if(!Input.GetMouseButtonDown(0))return;if(targetCamera==null)targetCamera=Camera.main;if(targetCamera==null)return;Ray r=targetCamera.ScreenPointToRay(Input.mousePosition);if(Physics.Raycast(r,out RaycastHit h,maxRayDistance,pylonMask)){var p=h.collider.GetComponentInParent<LiftPylonRuntime>();if(p!=null)Select(p);}}
 public void Select(LiftPylonRuntime p){Selected=p;SelectionChanged?.Invoke(p);} public void Clear(){Selected=null;SelectionChanged?.Invoke(null);}
}