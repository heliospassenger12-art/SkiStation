using System;
using UnityEditor;
using UnityEngine;
namespace SkiStation.MountainLab.Editor
{
 public static class MountainLabChecks312
 {
  [MenuItem("SkiStation/Montagne/Verifier le generateur 3.12")]
  public static void CheckMenu(){try{Debug.Log(Run());}catch(Exception ex){Debug.LogException(ex);}}
  public static string Run(){int samples=0;
   foreach(int seed in new[]{0,312,-127,int.MaxValue}) foreach(float scale in new[]{.7f,1f,1.25f}){
    var st=new MountainLabSettings312{seed=seed,heightScale=scale,naturalVariation=1f}; var a=new MountainLabHeight312(st); var b=new MountainLabHeight312(st);
    for(int iz=0;iz<=11;iz++) for(int ix=0;ix<=24;ix++){float x=-3000f+ix*250f,z=-2800f+iz*110f; Require(a.Height(x,z)==MountainLabHeight312.BaseY,"Front de neige modifié");samples++;}
    for(int iz=0;iz<=40;iz++) for(int ix=0;ix<=48;ix++){float x=-3000f+ix*125f,z=-2800f+iz*200f;float y=a.Height(x,z),sl=a.SlopeDegrees(x,z);Require(!float.IsNaN(y)&&!float.IsInfinity(y)&&y>=1599f&&y<5000f,"Altitude invalide");Require(sl>=0&&sl<90,"Inclinaison invalide");Require(y==b.Height(x,z),"Génération non reproductible");samples++;}
   }
   var n=new MountainLabHeight312(new MountainLabSettings312()); var o=new MountainLabHeight312(new MountainLabSettings312{seed=312}); bool changed=false;var bands=new bool[4];
   for(int z=-1200;z<=5000;z+=200)for(int x=-3000;x<=3000;x+=150){changed|=Mathf.Abs(n.Height(x,z)-o.Height(x,z))>.01f;bands[MountainLabHeight312.SlopeBand(n.SlopeDegrees(x,z))]=true;}
   Require(changed,"La graine ne change pas le relief"); foreach(bool present in bands)Require(present,"Une bande d’inclinaison manque");
   return "PASS 3.12 : "+samples+" échantillons ; domaine 1600-4200, face arrière, relief déterministe et quatre bandes d’inclinaison.";
  }
  static void Require(bool c,string m){if(!c)throw new InvalidOperationException(m);}
 }
}
