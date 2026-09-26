using System;
using UnityEditor;
using UnityEngine;
namespace SkiStation.MountainLab.Editor
{
 public static class MountainLabChecks311
 {
  [MenuItem("SkiStation/Montagne/Verifier le generateur 3.11")]
  public static void CheckMenu(){try{Debug.Log(Run());}catch(Exception ex){Debug.LogException(ex);}}
  public static string Run(){int samples=0;
   foreach(int seed in new[]{0,311,-127,int.MaxValue}) foreach(float scale in new[]{.7f,1f,1.25f}){
    var st=new MountainLabSettings311{seed=seed,heightScale=scale,naturalVariation=1f}; var a=new MountainLabHeight311(st); var b=new MountainLabHeight311(st);
    for(int iz=0;iz<=11;iz++) for(int ix=0;ix<=24;ix++){float x=-2400f+ix*200f,z=-2300f+iz*100f; Require(a.Height(x,z)==MountainLabHeight311.BaseY,"Front de neige modifié");samples++;}
    for(int iz=0;iz<=40;iz++) for(int ix=0;ix<=48;ix++){float x=-2400f+ix*100f,z=-2300f+iz*100f;float y=a.Height(x,z),sl=a.SlopeDegrees(x,z);Require(!float.IsNaN(y)&&!float.IsInfinity(y)&&y>=81f&&y<2600f,"Altitude invalide");Require(sl>=0&&sl<90,"Inclinaison invalide");Require(y==b.Height(x,z),"Génération non reproductible");samples++;}
   }
   var n=new MountainLabHeight311(new MountainLabSettings311()); var o=new MountainLabHeight311(new MountainLabSettings311{seed=311}); bool changed=false;var bands=new bool[4];
   for(int z=-1000;z<=1600;z+=100)for(int x=-2200;x<=2200;x+=100){changed|=Mathf.Abs(n.Height(x,z)-o.Height(x,z))>.01f;bands[MountainLabHeight311.SlopeBand(n.SlopeDegrees(x,z))]=true;}
   Require(changed,"La graine ne change pas le relief"); foreach(bool present in bands)Require(present,"Une bande d’inclinaison manque");
   return "PASS 3.11 : "+samples+" échantillons ; massif compact, front plat, relief déterministe et quatre bandes d’inclinaison.";
  }
  static void Require(bool c,string m){if(!c)throw new InvalidOperationException(m);}
 }
}
