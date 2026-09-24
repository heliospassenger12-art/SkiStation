using UnityEngine;

namespace SkiStation
{
    public sealed class SlopeProfileToolUI : MonoBehaviour
    {
        [SerializeField] SlopeProfileTool tool;
        void OnGUI()
        {
            if (tool == null || !tool.IsActive) return;
            GUILayout.BeginArea(new Rect(18, 235, 430, 125), GUI.skin.box);
            GUILayout.Label("🎿 SLOPE PROFILE — P");
            GUILayout.Label("1 Naturel  2 Régulier  3 Bosses  4 Champ de bosses");
            GUILayout.Label("5 Compression  6 Mur");
            GUILayout.Label("Clic appliquer • - / + amplitude");
            GUILayout.EndArea();
        }
    }
}
