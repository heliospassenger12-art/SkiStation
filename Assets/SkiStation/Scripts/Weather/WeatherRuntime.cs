using System; using UnityEngine;
namespace SkiStation {
 public enum WeatherCondition { Clear,Snowfall,FreshSnow,Thaw,Rain,Freeze,Windy }
 [Serializable] public struct WeatherSnapshot { public WeatherCondition condition; public float temperatureC,snowfallCm,windKmh,visibility,snowQualityModifier; }
 public class WeatherRuntime:MonoBehaviour { public static WeatherRuntime I{get;private set;} public WeatherSnapshot Current{get;private set;} public float temperatureC=-3f,windKmh=10f,snowfallRate=.1f; public float interval=10f; float t;
  void Awake(){I=this;Evaluate();} void Update(){t+=Time.deltaTime;if(t>=interval){t=0;Evaluate();}}
  public void Evaluate(){WeatherCondition c=temperatureC>3?WeatherCondition.Rain:temperatureC>1.5f?WeatherCondition.Thaw:temperatureC<-8?WeatherCondition.Freeze:snowfallRate>.7f?WeatherCondition.FreshSnow:snowfallRate>.15f?WeatherCondition.Snowfall:windKmh>65?WeatherCondition.Windy:WeatherCondition.Clear; float v=c==WeatherCondition.FreshSnow?.5f:c==WeatherCondition.Snowfall?.65f:c==WeatherCondition.Rain?.8f:1f; float q=c==WeatherCondition.FreshSnow?1.08f:(c==WeatherCondition.Thaw||c==WeatherCondition.Rain)?.72f:c==WeatherCondition.Freeze?.82f:1f; Current=new WeatherSnapshot{condition=c,temperatureC=temperatureC,snowfallCm=snowfallRate*2f,windKmh=windKmh,visibility=v,snowQualityModifier=q};}
 }
}
