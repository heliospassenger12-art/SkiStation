using UnityEngine;
using UnityEngine.UI;

namespace SkiStation
{
    public class LiftMaintenanceUI : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.F5;
        private Canvas canvas;
        private Text body;

        private void Start()
        {
            Build();
            canvas.enabled = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                canvas.enabled = !canvas.enabled;

            if (canvas.enabled)
                Refresh();
        }

        private void Build()
        {
            var go = new GameObject("LiftMaintenanceCanvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            var image = panel.AddComponent<Image>();
            image.raycastTarget = false;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.08f,.08f);
            rect.anchorMax = new Vector2(.92f,.92f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var textGo = new GameObject("Body");
            textGo.transform.SetParent(panel.transform, false);
            body = textGo.AddComponent<Text>();
            body.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            body.fontSize = 18;
            body.alignment = TextAnchor.UpperLeft;
            var tr = body.rectTransform;
            tr.anchorMin = new Vector2(.03f,.03f);
            tr.anchorMax = new Vector2(.97f,.97f);
            tr.offsetMin = tr.offsetMax = Vector2.zero;
        }

        private void Refresh()
        {
            if (LiftMaintenanceRuntime.Instance == null)
            {
                body.text = "MAINTENANCE DES REMONTÉES\nRuntime indisponible.";
                return;
            }

            body.text = "MAINTENANCE DES REMONTÉES\n\n";
            foreach (var s in LiftMaintenanceRuntime.Instance.GetAll())
            {
                body.text += string.Format(
                    "{0} | {1} | État: {2} | État technique: {3:0}% | Risque panne: {4:0}%\n",
                    s.displayName, s.liftId, s.status,
                    s.condition01 * 100f, s.breakdownRisk01 * 100f);
            }
        }
    }
}
