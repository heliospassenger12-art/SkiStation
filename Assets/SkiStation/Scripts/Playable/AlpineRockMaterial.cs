using UnityEngine;
using UnityEngine.Rendering;

namespace SkiStation
{
    public static class AlpineRockMaterial
    {
        private static Material material;
        public static Material Shared
        {
            get
            {
                if (material != null) return material;
                var shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) return null;
                material = new Material(shader) { name="SkiStation_2_47_DarkBrownRock", color=Color.white };
                const int size=512;
                var texture=new Texture2D(size,size,TextureFormat.RGBA32,true);
                texture.name="StratifiedBrownStone_Procedural";
                texture.wrapMode=TextureWrapMode.Repeat; texture.filterMode=FilterMode.Trilinear; texture.anisoLevel=4;
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                {
                    float u=x/(float)size*Mathf.PI*2f, v=y/(float)size*Mathf.PI*2f;
                    float nx=x/(float)size, ny=y/(float)size;
                    float warp=(Noise(nx,ny,4)-.5f)*3f;
                    float grain=.35f*Noise(nx,ny,4)+.30f*Noise(nx,ny,17)+.22f*Noise(nx,ny,67)+.13f*Noise(nx,ny,181);
                    float strata=Mathf.Pow(.5f+.5f*Mathf.Sin(v*9f+warp),16f);
                    float fracture=Mathf.Pow(.5f+.5f*Mathf.Sin(u*7f+.55f*Mathf.Sin(v*3f)),48f);
                    float mineral=Mathf.Clamp01(.25f+.65f*grain-.20f*strata-.24f*fracture);
                    pixels[y*size+x]=Color.Lerp(new Color(.105f,.065f,.043f),new Color(.40f,.29f,.19f),mineral);
                }
                texture.SetPixels(pixels);texture.Apply(true,true);
                material.mainTexture=texture;
                if(material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap",texture);
                if(material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",.06f);
                if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.06f);
                if(material.HasProperty("_Metallic")) material.SetFloat("_Metallic",0f);
                return material;
            }
        }

        public static void ApplyUV(Mesh mesh)
        {
            var vertices=mesh.vertices;var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++) uv[i]=new Vector2(vertices[i].x/24f,(vertices[i].y+vertices[i].z*.15f)/24f);
            mesh.uv=uv;
        }
        private static float Noise(float u,float v,int cells)
        {
            float x=u*cells,y=v*cells;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);
            float tx=x-ix,ty=y-iy;tx=tx*tx*(3f-2f*tx);ty=ty*ty*(3f-2f*ty);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix%cells,iy%cells),Hash((ix+1)%cells,iy%cells),tx),
                Mathf.Lerp(Hash(ix%cells,(iy+1)%cells),Hash((ix+1)%cells,(iy+1)%cells),tx),ty);
        }
        private static float Hash(int x,int y)
        {
            unchecked { uint n=(uint)(x*374761393+y*668265263);n=(n^(n>>13))*1274126177u;return (n&65535u)/65535f; }
        }
    }
}
