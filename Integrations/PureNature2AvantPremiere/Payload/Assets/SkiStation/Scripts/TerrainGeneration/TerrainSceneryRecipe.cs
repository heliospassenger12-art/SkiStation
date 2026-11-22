using System;
using System.Collections.Generic;
using UnityEngine;
using SkiStation.Altitude;
namespace SkiStation.TerrainGeneration
{
    public enum TerrainSceneryKind { TREE, ROCK, CLIFF }
    [Serializable] public sealed class TerrainSceneryAnchor
    {public string id;public TerrainSceneryKind kind;public Vector3 position;public Quaternion rotation=Quaternion.identity;public float height,radius,rank;public bool removed;}
    [Serializable] public sealed class TerrainSceneryRecipe
    {
        public int schema=1;public string version="SKISTATION_SCENERY_1";public TerrainSceneryAnchor[] anchors=new TerrainSceneryAnchor[0];
        public void Validate()
        {
            if(schema!=1||version!="SKISTATION_SCENERY_1"||anchors==null||anchors.Length>768)throw new InvalidOperationException("Catalogue décor SkiStation hors schema/budget.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var a in anchors)
            {if(a==null||!TerrainGenerationEngine.ValidId(a.id)||!ids.Add(a.id)||!Enum.IsDefined(typeof(TerrainSceneryKind),a.kind)||!AlpineAltitudeRules.Finite(a.position))throw new InvalidOperationException("Ancre décor invalide.");
             foreach(float v in new[]{a.height,a.radius,a.rank,a.rotation.x,a.rotation.y,a.rotation.z,a.rotation.w})TerrainGenerationEngine.Finite(v);
             float norm=a.rotation.x*a.rotation.x+a.rotation.y*a.rotation.y+a.rotation.z*a.rotation.z+a.rotation.w*a.rotation.w;
             if(a.height<.5f||a.height>18||a.radius<.2f||a.radius>5||a.rank<0||a.rank>1||norm<.99f||norm>1.01f)throw new InvalidOperationException("Dimensions/orientation décor incohérentes.");}
        }
    }
    public static class TerrainSceneryPlanner
    {
        const int TreeLimit = 608;
        const int RockLimit = 160;

        // Explicit new-domain creation only. Logical dimensions/collisions never depend on commercial meshes.
        public static TerrainSceneryRecipe Prepare(TerrainHeightfield field,TerrainMassifReport report)
        {
            if(field.recipe.scenery!=null)throw new InvalidOperationException("Décor déjà figé : pas de répartition automatique d'une ancienne station.");
            var r=field.recipe;var reference=r.altitudeData?.reference;
            if(reference==null||r.verticalDerived==null)throw new InvalidOperationException("Profil altitude et masques requis pour les nouveaux candidats.");
            var output=new List<TerrainSceneryAnchor>();var cells=new Dictionary<Vector2Int,List<Vector2>>();uint state=unchecked((uint)r.seed)^0xa6d20317u;int trees=0,rocks=0;
            for(int attempt=0;attempt<30000&&output.Count<TreeLimit+RockLimit;attempt++)
            {
                float x=r.originX+r.playableInset+Next(ref state)*(r.sizeX-2*r.playableInset),z=r.originZ+r.playableInset+Next(ref state)*(r.sizeZ-2*r.playableInset);
                var p=new Vector3(x,field.Height(x,z),z);float slope=TerrainMassifAnalysis.Slope(field,x,z),altitude=WorldAltitudeService.GetAltitudeMeters(p,reference);
                if(!r.verticalDerived.TrySample(r,p,out _,out var forest,out _,out var rock))continue;
                bool tree=forest!=ForestPotential.NO_TREES&&forest!=ForestPotential.CLEARING&&trees<TreeLimit;
                float probability=tree?AltitudeClimate.TreeDensity(altitude,slope,reference):rock==RockPotential.NONE?0:.45f;
                if(tree&&forest==ForestPotential.DENSE_FOREST)probability=Mathf.Min(1f,probability*1.2f);
                if(Next(ref state)>probability||!tree&&rocks>=RockLimit||Excluded(p,report,r.verticalDerived.corridors))continue;
                var key=new Vector2Int(Mathf.FloorToInt(x/12),Mathf.FloorToInt(z/12));bool near=false;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(cells.TryGetValue(key+new Vector2Int(dx,dz),out var row))foreach(var old in row)if(Vector2.Distance(old,new Vector2(x,z))<9)near=true;
                if(near)continue;if(!cells.TryGetValue(key,out var points)){points=new List<Vector2>();cells.Add(key,points);}points.Add(new Vector2(x,z));
                float height=tree?7+Next(ref state)*5:1+Next(ref state)*3,radius=tree?2+Next(ref state):.8f+Next(ref state)*1.3f;
                bool cliff=!tree&&(rock&(RockPotential.CLIFF|RockPotential.ROCK_BAND))!=0;
                if(cliff)
                {
                    height=Mathf.Lerp(3.5f,11f,Next(ref state));
                    radius=Mathf.Lerp(2.4f,4.7f,Next(ref state));
                }
                else if(!tree&&(rock&RockPotential.SCREE)!=0)
                {
                    float size=Next(ref state);
                    height=size<.3f?Mathf.Lerp(.5f,.8f,size/.3f):Mathf.Lerp(.85f,1.8f,(size-.3f)/.7f);
                    radius=Mathf.Lerp(.32f,1.35f,size);
                }
                float e=4;var normal=new Vector3(field.Height(x-e,z)-field.Height(x+e,z),2*e,field.Height(x,z-e)-field.Height(x,z+e)).normalized;
                var rotation=(tree?Quaternion.identity:Quaternion.FromToRotation(Vector3.up,normal))*Quaternion.Euler(0,Next(ref state)*360,0);
                output.Add(new TerrainSceneryAnchor{id="scenery."+output.Count,kind=tree?TerrainSceneryKind.TREE:cliff?TerrainSceneryKind.CLIFF:TerrainSceneryKind.ROCK,position=p,rotation=rotation,height=height,radius=radius,rank=Next(ref state)});
                if(tree)trees++;else rocks++;
            }
            var recipe=new TerrainSceneryRecipe{anchors=output.ToArray()};recipe.Validate();return recipe;
        }
        static float Next(ref uint state){if(state==0)state=1;unchecked{state^=state<<13;state^=state>>17;state^=state<<5;}return (state&65535)/65535f;}
        static bool Excluded(Vector3 p,TerrainMassifReport report,SkiableCorridorCandidate[] corridors)
        {
            if(report.front.found&&new Vector2(p.x-report.front.point.x,p.z-report.front.point.z).magnitude<180)return true;
            if(report.road.compatible&&Near(p,report.road.points,18))return true;
            foreach(var l in report.lifts)if(l.compatible&&Near(p,l.points,16))return true;
            foreach(var c in corridors)if(Near(p,c.points,c.minimumWidth*.5f+5))return true;return false;
        }
        static bool Near(Vector3 p,Vector3[] points,float radius){for(int i=1;i<points.Length;i++)if(ConstructionRules349.SegmentDistanceXZ(p,points[i-1],points[i])<radius)return true;return false;}
    }
}

