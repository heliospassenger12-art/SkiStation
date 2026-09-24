using UnityEngine;

namespace SkiStation
{
    public sealed class MountainTerrainWorkUI : MonoBehaviour
    {
        [SerializeField] MountainTerrainWorkMode mode;
        public void Bind(MountainTerrainWorkMode value) { mode = value; }
        void OnGUI()
        {
            if (mode == null || !mode.IsActive) return;
            GUILayout.BeginArea(new Rect(18, 120, 390, 105), GUI.skin.box);
            GUILayout.Label("🏔️ TERRAIN WORKS — T");
            GUILayout.Label("1 Creuser   2 Remblayer   3 Aplanir");
            GUILayout.Label("Clic gauche appliquer • clic droit inverser");
            GUILayout.Label("- / + rayon • Backspace annuler les travaux");
            GUILayout.EndArea();
        }
    }
}
