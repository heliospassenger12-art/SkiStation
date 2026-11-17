Shader "SkiStation/Arcade/WeatherSnow"
{
    Properties { _WeatherTint("Lumière ambiante", Color) = (1, 1, 1, 1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "SnowBillboard"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _WeatherTint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float2 depthAndFog : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.uv = input.uv;
                output.color = input.color;
                output.depthAndFog = float2(-position.positionVS.z, ComputeFogFactor(position.positionCS.z));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                half flake = 1.0 - smoothstep(.08, 1.0, dot(centered, centered));
                half fadeNear = smoothstep(.25, 1.7, input.depthAndFog.x);
                half fadeFar = 1.0 - smoothstep(18.0, 35.0, input.depthAndFog.x);
                return half4(MixFog(input.color.rgb * _WeatherTint.rgb, input.depthAndFog.y), input.color.a * flake * fadeNear * fadeFar);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
