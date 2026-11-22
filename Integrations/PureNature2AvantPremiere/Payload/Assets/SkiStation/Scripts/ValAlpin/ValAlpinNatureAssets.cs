
using UnityEngine;
using SkiStation.Visuals;
namespace SkiStation.ValAlpin
{
    public sealed class ValAlpinNatureAssets : ScriptableObject
    {
        public VisualProfile profile;
        public GameObject[] trees,cliffs,boulders;
        public GameObject[] stones,pebbles;
        public Texture2D rockAlbedo,rockNormal;
        public Shader leavesShader,rockShader,impostorShader,litShader,skyShader,terrainShader,exteriorShader;
        public bool IsReady=>profile!=null&&profile.terrain!=null&&trees!=null&&trees.Length>0&&cliffs!=null&&cliffs.Length>0
            &&boulders!=null&&boulders.Length>0&&impostorShader!=null&&rockAlbedo!=null&&rockNormal!=null&&leavesShader!=null&&rockShader!=null&&litShader!=null&&skyShader!=null&&terrainShader!=null&&exteriorShader!=null;
    }
}

