using UnityEngine;
namespace SkiStation.Visuals
{
    [CreateAssetMenu(menuName="SkiStation/Visual Profiles/TerrainVisualSet")]
    public sealed class TerrainVisualSet:ScriptableObject
    {
        public VisualPipeline pipeline = VisualPipeline.BuiltIn;
        public Material meshMaterial;
        [Tooltip("Explicit Unity Terrain-compatible material; meshMaterial remains the existing mesh adapter.")]
        public Material terrainMaterial;
        public bool useNaturalSnowLayers;
        public TerrainVisualLayer[] layers = new TerrainVisualLayer[0];
    }
}

