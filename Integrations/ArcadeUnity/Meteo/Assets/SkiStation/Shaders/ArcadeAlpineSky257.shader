Shader "SkiStation/Arcade/AlpineSky257" {
 Properties {_Zenith("Zenith",Color)=(.19,.36,.58,1) _Horizon("Horizon",Color)=(.63,.74,.82,1) _Clouds("Nuages",Range(0,1))=.25 _Clock("Temps domaine",Float)=0 _SunDir("Soleil",Vector)=(.5,.5,-.5,0) _SunTint("Couleur soleil",Color)=(1,.96,.90,1) _SunStrength("Intensite",Float)=1 _DetailQuality("Detail",Float)=1 _MoonDir("Lune",Vector)=(0,1,0,0) _Night("Nuit",Range(0,1))=0 _TwilightTint("Lueur horizon",Color)=(1,.5,.4,1) _TwilightStrength("Intensite crepuscule",Range(0,1))=0 _WeatherVeil("Voile meteo",Range(0,1))=0}
 SubShader {Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 float4 _Zenith,_Horizon,_SunDir,_SunTint,_MoonDir,_TwilightTint;float _Clouds,_Clock,_SunStrength,_DetailQuality,_Night,_TwilightStrength,_WeatherVeil;
 struct app{float4 vertex:POSITION;};struct v2f{float4 position:SV_POSITION;float3 direction:TEXCOORD0;};
 v2f vert(app v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.direction=mul((float3x3)unity_ObjectToWorld,v.vertex.xyz);return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float n(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
 float TwilightStrata(float2 azimuth,float elevation){
  float layers=n(azimuth*2.6+float2(_Clock*.0003,elevation*37));
  float filaments=n(azimuth*5.7+float2(elevation*65,_Clock*.0002));
  return smoothstep(.46,.71,layers*.72+filaments*.28);
 }

 float CloudCoverage(float3 direction)
 {
  direction = normalize(direction);
  float elevation = saturate(direction.y);
  float2 azimuth = normalize(direction.xz + float2(.00001, 0));
  float2 uv = direction.xz / max(.12, direction.y) * 1.45 + float2(_Clock * .0004, 0);
  // Domain-warped noise breaks up the outline of individual cloud banks.
  uv += float2(n(uv * .65 + 13), n(uv * .65 - 27)) * .18;
  float broad = n(uv) * .64 + n(uv * 2.1 + 17) * .25;
  if (_DetailQuality > .4) broad += n(uv * 4.3 - 19) * .11;

  float cloud = smoothstep(.72 - _Clouds * .37, .83 - _Clouds * .37, broad);
  float upperSky = smoothstep(.02, .18, direction.y);
  cloud *= upperSky * saturate(_Clouds * 2);
  float blanket = (.84 + broad * .12) * smoothstep(0, .08, direction.y);
  cloud = lerp(cloud, max(cloud, blanket), smoothstep(.58, .94, _Clouds));

  float strata = TwilightStrata(azimuth, elevation);
  strata *= smoothstep(.01, .075, elevation) * (1 - smoothstep(.3, .53, elevation));
  float twilight = _TwilightStrength * (1 - _WeatherVeil);
  return saturate(lerp(cloud, strata * .53, twilight * .88));
 }

 float SunShafts(float3 view,float3 sun,float cosine,float coverage){
  float lowSun=smoothstep(.005,.065,sun.y)*(1-smoothstep(.22,.46,sun.y));
  float strength=lowSun*_TwilightStrength*saturate(_Clouds*4)*(1-_WeatherVeil);
  if(strength<.002 || cosine<.80 || view.y<.015)return 0;

  // Compare the opening on this spoke with cloud on either side of it.
  // Every point on the same spoke samples the same gap near the sun.
  float3 radial=view-sun*cosine;
  float radius=length(radial);
  float3 spoke=radial/max(radius,.0001);
  float3 flank=cross(sun,spoke)*.050;
  float3 nearSun=sun+spoke*.16;
  float opening=CloudCoverage(nearSun);
  float neighbours=max(CloudCoverage(nearSun+flank),CloudCoverage(nearSun-flank));
  float contrast=smoothstep(.035,.34,neighbours-opening);
  float lengthFade=smoothstep(.04,.12,radius)*(1-smoothstep(.24,.57,radius));
  return strength*contrast*(1-opening)*lengthFade*smoothstep(.015,.09,view.y)*(1-coverage)*.22;
 }
 float4 frag(v2f i):SV_Target{
  float3 d=normalize(i.direction);float3 sun=normalize(_SunDir.xyz);float h=saturate(d.y);
  float3 col=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,.42));float sd=saturate(dot(d,sun));
  col+=_SunTint.rgb*pow(sd,64)*.075*_SunStrength;
  float2 azimuth=normalize(d.xz+float2(.00001,0));
  float2 sunAzimuth=normalize(sun.xz+float2(.00001,0));
  float sunSide=smoothstep(-.15,.95,dot(azimuth,sunAzimuth));
  float rim=exp2(-h*9)*(.46+.54*sunSide)*_TwilightStrength;
  col=lerp(col,_TwilightTint.rgb,rim*.88);
  float coverage = CloudCoverage(d);
  float shade = saturate(.67 + .2 * dot(d, sun));
  float sunlight = shade * (1 - _Night) * (1 - _WeatherVeil);
  float3 cloudColor = lerp(_Horizon.rgb * .85, _SunTint.rgb * .95, sunlight);
  cloudColor = lerp(cloudColor, _TwilightTint.rgb,
                   _TwilightStrength * (.12 + .48 * sunSide) * exp2(-h * 4));
  col=lerp(col,cloudColor,coverage*.75);
  col+=lerp(_SunTint.rgb,_TwilightTint.rgb,.25)*SunShafts(d,sun,sd,coverage);
  float disc=smoothstep(.99984,.99996,sd)*smoothstep(-.02,.04,sun.y);col+=_SunTint.rgb*disc*1.4*_SunStrength*(1-coverage*.75);
  if(_Night>0){
   float3 moon=normalize(_MoonDir.xyz);
   float md=saturate(dot(d,moon));
   float visibility=_Night*(1-coverage*.85)*smoothstep(0,.07,moon.y);
   float edge=max(fwidth(md),.000008);
   float moonDisk=smoothstep(.99987-edge,.99987+edge,md);
   col+=float3(.63,.76,1)*(.09*pow(md,160)+.82*moonDisk)*visibility;
  }
  col=lerp(_Horizon.rgb*.84,col,smoothstep(-.12,.01,d.y));return float4(col,1);
 }ENDCG }
 }FallBack Off
}
