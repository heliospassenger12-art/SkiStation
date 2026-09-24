using UnityEngine; using UnityEngine.UI;
public class LiftPylonInspectionUI : MonoBehaviour {
 [SerializeField] GameObject panel; [SerializeField] Text titleText,detailsText; [SerializeField] Button closeButton; LiftPylonSelectionRuntime selector;
 void Start(){selector=LiftPylonSelectionRuntime.Instance;if(selector!=null)selector.SelectionChanged+=OnSelectionChanged;if(closeButton!=null)closeButton.onClick.AddListener(Close);OnSelectionChanged(selector!=null?selector.Selected:null);}
 void OnDestroy(){if(selector!=null)selector.SelectionChanged-=OnSelectionChanged;}
 public void Close(){if(selector!=null)selector.Clear();if(panel!=null)panel.SetActive(false);}
 void OnSelectionChanged(LiftPylonRuntime p){if(p==null){if(titleText!=null)titleText.text="Pylône";if(detailsText!=null)detailsText.text="Aucun pylône sélectionné.";return;}if(panel!=null)panel.SetActive(true);var s=p.BuildSnapshot(0,0,true);if(titleText!=null)titleText.text=s.displayName;if(detailsText!=null)detailsText.text=$"ID : {s.id}\nAltitude : {s.altitude:0.0} m\nHauteur : {s.height:0.0} m\nCharge estimée : {s.estimatedLoad:0.0}\nÉtat technique : {s.condition01*100f:0}%\n\nImplantation : sélectionnée\nDéplacement : aperçu uniquement\nRemplacement : future version"; }
}