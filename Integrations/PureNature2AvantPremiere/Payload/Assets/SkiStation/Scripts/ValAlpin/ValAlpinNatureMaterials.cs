
using UnityEngine;
using UnityEngine.Rendering;
namespace SkiStation.ValAlpin
{
    // One policy for native scenery skins and supplemental landscape, including every LOD.
    public static class ValAlpinNatureMaterials
    {
        public static bool Supports(Material source)
        {
            if(source==null||source.shader==null)return false;
            string name=source.shader.name;
            return name=="BK/Vegetation Leaves"||name=="BK/Vegetation Trunk"||name=="BK/Standard Layered"||name=="BK/Impostor"||name=="BK/Grass";
        }
        public static void Configure(Material target,Material source,Shader leaves,Shader rock,Shader impostor)
        {
            string name=source.shader.name;
            if(name=="BK/Vegetation Leaves")
            {
                target.shader=leaves;target.enableInstancing=false;
                target.SetColor("_MainColor",new Color(.37f,.49f,.46f,1));
                target.SetColor("_GradientColor",new Color(.64f,.70f,.67f,1));
                target.SetFloat("_TransStrength",0);target.SetFloat("_ColorVariationPower",.08f);
            }
            else if(name=="BK/Impostor")
            {
                target.shader=impostor;target.enableInstancing=false;
                target.SetColor("_MainColor",new Color(.37f,.49f,.46f,1));
                target.SetColor("_GradientColor",new Color(.64f,.70f,.67f,1));
                target.SetFloat("_GradientFalloff",1);target.SetFloat("_ColorVariationPower",.08f);
            }
            else if(name=="BK/Vegetation Trunk"||name=="BK/Grass")
            {
                var lit=Shader.Find("Universal Render Pipeline/Lit");
                if(lit==null||!lit.isSupported)return;
                target.shader=lit;
                target.enableInstancing=true;
                target.SetTexture("_BaseMap",source.GetTexture("_MainTex"));
                target.SetTextureScale("_BaseMap",source.GetTextureScale("_MainTex"));
                bool grass=name=="BK/Grass";
                target.SetColor("_BaseColor",source.GetColor(grass?"_Color01":"_Color"));
                var normal=source.GetTexture(grass?"_Normal":"_BumpMap");
                if(normal!=null)
                {
                    target.SetTexture("_BumpMap",normal);
                    target.SetFloat("_BumpScale",source.GetFloat("_NormalPower"));
                    target.EnableKeyword("_NORMALMAP");
                }
                target.SetFloat("_Metallic",0);
                target.SetFloat("_Smoothness",grass?source.GetFloat("_Smoothness"):.12f);
                if(grass)
                {
                    target.SetFloat("_AlphaClip",1);
                    target.SetFloat("_Cutoff",source.GetFloat("_Cutoff"));
                    target.SetFloat("_Cull",0);
                    target.SetOverrideTag("RenderType","TransparentCutout");
                    target.EnableKeyword("_ALPHATEST_ON");
                    target.renderQueue=(int)RenderQueue.AlphaTest;
                }
            }
            else if(name=="BK/Standard Layered")
            {
                target.shader=rock;target.enableInstancing=false;
                target.SetColor("_Color",new Color(.58f,.60f,.63f,1));
                target.SetTexture("_LayerAlbedo",Texture2D.whiteTexture);
                target.SetColor("_2ndColor",new Color(.85f,.89f,.93f,1));
                target.SetFloat("_UseVertexColor",0);target.SetFloat("_LayerPower",.12f);
                target.SetFloat("_LayerThreshold",3);target.SetFloat("_SmoothnessPower",.06f);target.SetFloat("_MetallicPower",0);
            }
        }
    }
}

