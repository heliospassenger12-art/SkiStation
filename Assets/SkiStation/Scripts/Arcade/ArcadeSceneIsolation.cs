using UnityEngine;
using UnityEngine.SceneManagement;
namespace SkiStation.Arcade
{
    public static class ArcadeSceneIsolation
    {
        // Evaluated after scene load, before an installer creates management objects.
        // No cached flag: works when domain reload is disabled in the Editor.
        public static bool IsActive
        {
            get
            {
                var scene = SceneManager.GetActiveScene();
                return scene.name.StartsWith("SkiStation_Arcade_Test_", System.StringComparison.Ordinal)
                    || Object.FindFirstObjectByType<ArcadeTestSlope>() != null;
            }
        }
    }
}
