using UnityEngine;

namespace SkiStation
{
    public static class RuntimeMaterialFactory
    {
        public static Material Create(Color color)
        {
            string[] names = {
                "Standard",
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Unlit",
                "Sprites/Default",
                "Unlit/Color",
                "Legacy Shaders/Diffuse"
            };
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader == null) continue;
                var material = new Material(shader);
                material.color = color;
                return material;
            }
            Debug.LogWarning("SkiStation: aucun shader runtime compatible trouvé; matériau ignoré sans interrompre le jeu.");
            return null;
        }
    }
}
