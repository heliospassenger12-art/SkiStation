using UnityEngine;
using UnityEngine.UI;

namespace SkiStation
{
    public class LiftTechnologyUI : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.F6;
        private Canvas canvas;
        private Text body;

        void Start()
        {
            var go = new GameObject("LiftTechnologyCanvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("TechnologyPanel");
            panel.transform.SetParent(go.transform, false);
            var img = panel.AddComponent<Image>();
            var r = panel.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(.12f,.10f); r.anchorMax = new Vector2(.88f,.90f);
            r.offsetMin = r.offsetMax = Vector2.zero;

            var text = new GameObject("Body");
            text.transform.SetParent(panel.transform, false);
            body = text.AddComponent<Text>();
            body.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            body.fontSize = 17;
            body.alignment = TextAnchor.UpperLeft;
            var tr = body.rectTransform;
            tr.anchorMin = new Vector2(.04f,.04f); tr.anchorMax = new Vector2(.96f,.96f);
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            canvas.enabled = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey)) canvas.enabled = !canvas.enabled;
            if (canvas.enabled) Refresh();
        }

        void Refresh()
        {
            if (LiftTechnologyRuntime.Instance == null) { body.text = "LABORATOIRE — REMONTÉES\nRuntime indisponible."; return; }
            body.text = "LABORATOIRE TECHNOLOGIQUE — REMONTÉES\n\n";
            foreach (var d in LiftTechnologyRuntime.Instance.technologies)
            {
                if (d == null) continue;
                body.text += $"{d.displayName} | Palier {d.researchTier} | Coût {d.researchCost:0} | Déverrouillé: {LiftTechnologyRuntime.Instance.IsUnlocked(d.technology)}\n";
            }
            body.text += "\nF6 : fermer";
        }
    }
}
