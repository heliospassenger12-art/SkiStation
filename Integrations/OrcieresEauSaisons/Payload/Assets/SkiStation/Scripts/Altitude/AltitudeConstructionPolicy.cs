using System.Collections.Generic;
using UnityEngine;
namespace SkiStation.Altitude
{
    public enum AltitudeConstructionFailure { NONE, INVALID_COORDINATES, OUTSIDE_TERRAIN, BELOW_CONSTRUCTION_ALTITUDE_LIMIT, ABOVE_CONSTRUCTION_ALTITUDE_LIMIT }
    public static class AltitudeConstructionPolicy
    {
        public static bool AllowsAltitude(float meters,DomainAltitudeReference reference,out AltitudeConstructionFailure failure)
        {
            failure=AltitudeConstructionFailure.NONE;
            if(!AlpineAltitudeRules.Finite(meters)||reference==null){failure=AltitudeConstructionFailure.INVALID_COORDINATES;return false;}
            if(meters>AlpineAltitudeRules.AbsoluteConstructionMaxAltitude||meters>reference.constructionMaxAltitude){failure=AltitudeConstructionFailure.ABOVE_CONSTRUCTION_ALTITUDE_LIMIT;return false;}
            if(meters<reference.constructionMinAltitude){failure=AltitudeConstructionFailure.BELOW_CONSTRUCTION_ALTITUDE_LIMIT;return false;}
            return true;
        }
        public static string Explain(AltitudeConstructionFailure failure,DomainAltitudeReference reference)
        {
            if(failure==AltitudeConstructionFailure.ABOVE_CONSTRUCTION_ALTITUDE_LIMIT)return "Construction interdite au-dessus de "+Mathf.Min(reference.constructionMaxAltitude,AlpineAltitudeRules.AbsoluteConstructionMaxAltitude).ToString("0")+" m (plafond absolu 4050 m). Cette montagne peut rester rideable en Arcade.";
            if(failure==AltitudeConstructionFailure.BELOW_CONSTRUCTION_ALTITUDE_LIMIT)return "Construction possible à partir de "+reference.constructionMinAltitude.ToString("0")+" m sur ce domaine.";
            return "Emplacement invalide ou hors du terrain : repositionnez le projet.";
        }
        public static bool ValidatePoint(Vector3 point,out string reason,Component owner=null,bool terrainSupport=false)
        {
            var reference=WorldAltitudeService.Reference(owner);reason="";
            if(!AlpineAltitudeRules.Finite(point)){reason=Explain(AltitudeConstructionFailure.INVALID_COORDINATES,reference);return false;}
            if(terrainSupport){if(!ManagementSurface.TryGet(point,out point,owner)){reason=Explain(AltitudeConstructionFailure.OUTSIDE_TERRAIN,reference);return false;}}
            if(!AllowsAltitude(reference.ToMeters(point.y),reference,out var failure)){reason=Explain(failure,reference);return false;}return true;
        }
        // Dense samples use the authoritative collider AND proposed geometry, so a cable, tunnel,
        // support footprint or long piste cannot jump over an altitude-protected ridge.
        public static bool ValidateRoute(IReadOnlyList<Vector3> route,float halfWidth,out string reason,Component owner=null,bool sampleTerrain=true)
        {
            reason="Tracé incomplet";if(route==null||route.Count<1||route.Count>4096||!AlpineAltitudeRules.Finite(halfWidth)||halfWidth<0||halfWidth>500)return false;
            int budget=0;
            for(int i=0;i<route.Count;i++)
            {
                var a=i==0?route[0]:route[i-1];var b=route[i];
                if(!AlpineAltitudeRules.Finite(a)||!AlpineAltitudeRules.Finite(b)){reason="Coordonnées non finies";return false;}
                int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/5f));budget+=steps*3;
                if(budget>50000){reason="Tracé trop dense / long : validation interrompue avant construction.";return false;}
                var tangent=Vector3.ProjectOnPlane(b-a,Vector3.up).normalized;var side=Vector3.Cross(Vector3.up,tangent);if(side.sqrMagnitude<.1f)side=Vector3.right;
                for(int j=0;j<=steps;j++)
                {
                    var p=Vector3.Lerp(a,b,j/(float)steps);
                    if(!ValidatePoint(p,out reason,owner))return false;
                    if(sampleTerrain)for(int sign=-1;sign<=1;sign++)
                        if(!ValidatePoint(p+side*(halfWidth*sign),out reason,owner,true))return false;
                }
            }
            reason="";return true;
        }
        public static bool ValidateFootprint(Vector3 center,Vector2 size,out string reason,Component owner=null)
        {
            if(!ValidatePoint(center,out reason,owner))return false;
            if(!AlpineAltitudeRules.Finite(size.x)||!AlpineAltitudeRules.Finite(size.y)||size.x<=0||size.y<=0||size.x>1000||size.y>1000){reason="Emprise invalide";return false;}
            if(!TerrainGeneration.RealTerrainWater.AllowsFootprint(center,size,out reason))return false;
            int nx=Mathf.Max(1,Mathf.CeilToInt(size.x/5)),nz=Mathf.Max(1,Mathf.CeilToInt(size.y/5));
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)if(!ValidatePoint(center+new Vector3(size.x*(x/(float)nx-.5f),0,size.y*(z/(float)nz-.5f)),out reason,owner,true))return false;
            return true;
        }
    }
}
