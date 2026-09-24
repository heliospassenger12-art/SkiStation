using UnityEngine;
using UnityEngine.UI;

namespace SkiStation
{
    /// <summary>
    /// First real specialist management screen: lifts. F4 opens it directly;
    /// the command center Transport button also opens it through the command bus.
    /// </summary>
    public sealed class LiftManagementUI : MonoBehaviour
    {
        public static LiftManagementUI I { get; private set; }
        [SerializeField] private bool visibleAtStart;
        [SerializeField] private KeyCode toggleKey = KeyCode.F4;

        private GameObject root;
        private Text title;
        private Text summary;
        private Text detail;
        private Transform listRoot;
        private LiftRuntime selected;
        private float repaintTimer;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            if (LiftManagementRuntime.I == null)
            {
                var go = new GameObject("SkiStation_LiftManagementRuntime");
                go.AddComponent<LiftManagementRuntime>();
            }
            if (ManagementCommandActionRegistry.I == null)
            {
                var go = new GameObject("SkiStation_ManagementCommandRegistry");
                go.AddComponent<ManagementCommandActionRegistry>();
            }
            ManagementCommandActionRegistry.I.ActionRequested += OnCommand;
            Build();
        }

        private void Start()
        {
            SetVisible(visibleAtStart);
            Refresh();
        }

        private void OnDestroy()
        {
            if (ManagementCommandActionRegistry.I != null)
                ManagementCommandActionRegistry.I.ActionRequested -= OnCommand;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) SetVisible(!root.activeSelf);
            if (!root.activeSelf) return;
            repaintTimer += Time.unscaledDeltaTime;
            if (repaintTimer >= .75f) { repaintTimer = 0f; Refresh(); }
        }

        private void OnCommand(ManagementCommandAction action)
        {
            if (action == ManagementCommandAction.Transport) SetVisible(true);
        }

        public void SetVisible(bool value) => root.SetActive(value);

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1150;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            root = new GameObject("LiftManagementScreen", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            var r = root.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(.04f,.06f); r.anchorMax = new Vector2(.96f,.94f);
            root.GetComponent<Image>().color = new Color(.025f,.035f,.045f,.985f);

            title = MakeText("GESTION DES REMONTÉES MÉCANIQUES", 30, FontStyle.Bold);
            Place(title, 28, -22, -28, -70);
            var close = MakeButton("FERMER  [F4]", root.transform);
            var cr = close.GetComponent<RectTransform>(); cr.anchorMin=new Vector2(1,1);cr.anchorMax=new Vector2(1,1);cr.pivot=new Vector2(1,1);cr.sizeDelta=new Vector2(170,42);cr.anchoredPosition=new Vector2(-24,-18);
            close.onClick.AddListener(()=>SetVisible(false));

            var listPanel = new GameObject("LiftList", typeof(RectTransform), typeof(Image));
            listPanel.transform.SetParent(root.transform,false);
            var lp=listPanel.GetComponent<RectTransform>(); lp.anchorMin=new Vector2(0,0);lp.anchorMax=new Vector2(0.38f,1);lp.offsetMin=new Vector2(24,24);lp.offsetMax=new Vector2(-8,-86);
            listPanel.GetComponent<Image>().color=new Color(.055f,.07f,.085f,1f);
            listRoot=listPanel.transform;

            summary=MakeText("",18,FontStyle.Normal); Place(summary,.42f, -100,-28,-110);
            detail=MakeText("",17,FontStyle.Normal); Place(detail,.42f,-360,-28,-110);
        }

        private void Refresh()
        {
            var runtime=LiftManagementRuntime.I;
            if(runtime==null) return;
            runtime.Rebuild();
            ClearList();
            var network=ResortRouteNetwork.I;
            if(network!=null)
            {
                foreach(var lift in network.Lifts)
                {
                    if(lift==null || lift.Definition==null) continue;
                    var b=MakeButton(BuildRow(lift),listRoot);
                    var rt=b.GetComponent<RectTransform>(); rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(1,1);rt.offsetMin=new Vector2(10,-62-(CountButtons()*56));rt.offsetMax=new Vector2(-10,-CountButtons()*56);
                    var captured=lift; b.onClick.AddListener(()=>Select(captured));
                }
            }
            if(selected==null && network!=null && network.Lifts.Count>0) selected=network.Lifts[0];
            if(selected!=null) ShowSelected(selected);
            else {summary.text="Aucune remontée enregistrée.";detail.text="Construisez une remontée pour l'inspecter ici.";}
        }

        private int CountButtons(){ int n=0; for(int i=0;i<listRoot.childCount;i++) if(listRoot.GetChild(i).name.StartsWith("CommandButton")) n++; return n; }
        private string BuildRow(LiftRuntime l){ return (l.Open&&!l.Faulted?"● ":"○ ") + l.Definition.displayName + "\n" + l.Definition.type + "  •  " + l.CapacityPerHour.ToString("0") + " pers/h"; }
        private void Select(LiftRuntime l){selected=l;ShowSelected(l);}

        private void ShowSelected(LiftRuntime l)
        {
            if(l==null||l.Definition==null)return;
            var q=l.StationLogic!=null?l.StationLogic.QueueSize:0;
            var cong=InfrastructureCongestionRuntime.I;
            float p=cong!=null?cong.GetLiftPressure01(l):0f;
            var status=l.Faulted?"PANNE":(l.Open?"OUVERTE":"FERMÉE");
            summary.text=$"{l.Definition.displayName}\n\nÉTAT\n{status}\n\nTYPE\n{l.Definition.type}\n\nDÉBIT NOMINAL\n{l.CapacityPerHour:0} pers/h\n\nVITESSE\n{l.CurrentSpeedKmh:0.0} km/h\n\nFILE\n{q} personnes\n\nPRESSION\n{p*100f:0}%";
            detail.text=$"LONGUEUR\n{l.Length:0} m\n\nDÉNIVELÉ\n{l.VerticalDrop:0} m\n\nTRAJET\n{(float.IsInfinity(l.TravelSeconds)?"—":(l.TravelSeconds/60f).ToString("0.0")+" min")}\n\nVÉHICULES\n{l.Definition.vehicles} × {l.Definition.seatsPerVehicle} places\n\nCONSTRUCTION\n{(l.IsUnderground?"Souterraine":"Surface")}\n\nMAINTENANCE / JOUR\n{l.Definition.maintenancePerDay:0} €";
            AddActionButtons(l);
        }

        private void AddActionButtons(LiftRuntime l)
        {
            for(int i=root.transform.childCount-1;i>=0;i--){var c=root.transform.GetChild(i);if(c.name.StartsWith("LiftAction_"))Destroy(c.gameObject);}
            var open=MakeButton(l.Open?"FERMER LA REMONTÉE":"OUVRIR LA REMONTÉE",root.transform); open.name="LiftAction_Open";
            var or=open.GetComponent<RectTransform>();or.anchorMin=new Vector2(.42f,0);or.anchorMax=new Vector2(.42f,0);or.pivot=new Vector2(0,0);or.sizeDelta=new Vector2(260,46);or.anchoredPosition=new Vector2(0,28);open.onClick.AddListener(()=>{LiftManagementRuntime.I.SetSelectedOpen(!l.Open);Refresh();});
            var center=MakeButton("RECENTRER / ACTUALISER",root.transform);center.name="LiftAction_Refresh";var rr=center.GetComponent<RectTransform>();rr.anchorMin=new Vector2(.42f,0);rr.anchorMax=new Vector2(.42f,0);rr.pivot=new Vector2(0,0);rr.sizeDelta=new Vector2(260,46);rr.anchoredPosition=new Vector2(275,28);center.onClick.AddListener(Refresh);
        }

        private void ClearList(){for(int i=listRoot.childCount-1;i>=0;i--)Destroy(listRoot.GetChild(i).gameObject);}
        private Text MakeText(string value,int size,FontStyle style){var go=new GameObject("Text",typeof(RectTransform),typeof(Text));go.transform.SetParent(root.transform,false);var t=go.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("Arial.ttf");t.fontSize=size;t.fontStyle=style;t.color=Color.white;t.alignment=TextAnchor.UpperLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.text=value;return t;}
        private Button MakeButton(string label,Transform parent){var go=new GameObject("CommandButton",typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=new Color(.10f,.13f,.16f,1f);var b=go.GetComponent<Button>();var tx=new GameObject("Label",typeof(RectTransform),typeof(Text));tx.transform.SetParent(go.transform,false);var tr=tx.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(12,2);tr.offsetMax=new Vector2(-8,-2);var t=tx.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("Arial.ttf");t.fontSize=14;t.color=Color.white;t.alignment=TextAnchor.MiddleLeft;t.text=label;return b;}
        private void Place(Component c,float left,float top,float right,float bottom){var rt=c.GetComponent<RectTransform>();rt.anchorMin=new Vector2(left,1);rt.anchorMax=new Vector2(1+right*0,1);rt.pivot=new Vector2(0,1);rt.offsetMin=new Vector2(0,bottom);rt.offsetMax=new Vector2(right,top);if(left<1){rt.anchorMin=new Vector2(left,1);rt.anchorMax=new Vector2(1,1);rt.offsetMin=new Vector2(0,bottom);rt.offsetMax=new Vector2(right,top);}}
    }
}
