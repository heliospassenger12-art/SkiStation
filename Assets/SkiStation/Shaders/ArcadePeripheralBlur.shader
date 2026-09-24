Shader "Hidden/SkiStation/ArcadePeripheralBlur"
{
    Properties { _MainTex ("Screen", 2D) = "white" {} _Intensity ("Intensity", Float) = 0 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Intensity;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 centered = uv - float2(0.5, 0.53);
                float edge = smoothstep(0.24, 0.60, length(centered * float2(1.0, 1.05)));
                edge = max(edge, smoothstep(0.69, 1.0, 1.0 - uv.y) * 0.35);
                float strength = _Intensity * edge * 0.028;
                float2 direction = normalize(centered + float2(0.00001, 0.00001));
                fixed4 original = tex2D(_MainTex, uv);
                fixed4 blurred = original * 0.28;
                blurred += tex2D(_MainTex, uv + direction * strength) * 0.18;
                blurred += tex2D(_MainTex, uv - direction * strength) * 0.18;
                blurred += tex2D(_MainTex, uv + direction * strength * 2.0) * 0.12;
                blurred += tex2D(_MainTex, uv - direction * strength * 2.0) * 0.12;
                blurred += tex2D(_MainTex, uv + direction * strength * 3.0) * 0.06;
                blurred += tex2D(_MainTex, uv - direction * strength * 3.0) * 0.06;
                return lerp(original, blurred, saturate(edge * _Intensity));
            }
            ENDCG
        }
    }
    Fallback Off
}
