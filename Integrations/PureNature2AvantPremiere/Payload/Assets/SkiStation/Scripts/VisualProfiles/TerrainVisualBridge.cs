using UnityEngine;
using SkiStation.TerrainGeneration;
using SkiStation.Altitude;
namespace SkiStation.Visuals
{
    public struct TerrainVisualSample
    {
        public float altitudeMeters,slope,snowCoverage,forestDensity;
        public float surfaceVariation;
        public bool permanentSnow,glacier,extreme;
        public ForestPotential forest;public RockPotential rock;
    }
    // One-way adapter: reads gameplay geometry/zones; never generates or authorizes infrastructure.
    public static class TerrainVisualBridge
    {
        public static TerrainVisualSample Sample(TerrainGenerationWorld world,Vector3 p)
        {
            var data=world.Terrain.terrainData;var origin=world.Terrain.transform.position;var runtime=world.GetComponent<DomainAltitudeRuntime>();
            p.y=world.ClampedHeight(p.x,p.z);float u=Mathf.Clamp01((p.x-origin.x)/data.size.x),v=Mathf.Clamp01((p.z-origin.z)/data.size.z);
            float slope=data.GetSteepness(u,v);var reference=runtime?.Data?.reference??WorldAltitudeService.Reference(world);
            float altitude=WorldAltitudeService.GetAltitudeMeters(p,reference);bool permanent=runtime!=null&&runtime.IsPermanentSnowZone(p),glacier=false;
            if(runtime?.Data!=null)foreach(var zone in runtime.Data.zones)if(zone.kind==AlpineZoneKind.GLACIER&&zone.Contains(p,altitude,slope)){glacier=true;break;}
            float density=AltitudeClimate.TreeDensity(altitude,slope,reference);ForestPotential forest=density<=.02f?ForestPotential.NO_TREES:altitude>reference.treeLineAltitudeMeters-reference.treeLineBlendMeters?ForestPotential.TREE_LINE_TRANSITION:density>.65f?ForestPotential.DENSE_FOREST:ForestPotential.OPEN_FOREST;
            RockPotential rock=slope>=62?RockPotential.CLIFF:slope>=45?RockPotential.ROCK_FACE:altitude>reference.treeLineAltitudeMeters&&slope>20?RockPotential.ALPINE_ROCK:RockPotential.NONE;
            var derived=world.Recipe.verticalDerived;
            if(derived!=null&&derived.TrySample(world.Recipe,p,out _,out var savedForest,out _,out var savedRock)){forest=savedForest;rock=savedRock;if(forest==ForestPotential.CLEARING||forest==ForestPotential.NO_TREES)density=0;}
            return new TerrainVisualSample{altitudeMeters=altitude,slope=slope,permanentSnow=permanent,glacier=glacier,extreme=altitude>AlpineAltitudeRules.AbsoluteConstructionMaxAltitude,snowCoverage=AltitudeClimate.SnowCoverage(altitude,slope,reference,permanent),forestDensity=density,forest=forest,rock=rock};
        }
        public static float Weight(string category,TerrainVisualSample sample)
        {
            switch((category??"").ToUpperInvariant())
            {
                case "GLACIER":return sample.glacier?sample.snowCoverage:0;
                case "SNOW":case "SNOW_FRESH":case "SEASONAL_SNOW":return sample.snowCoverage*(sample.glacier?.35f:1);
                case "PERMANENT_SNOW":return sample.permanentSnow?sample.snowCoverage:0;
                case "ROCK":return Mathf.Max((1-sample.snowCoverage)*.5f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(30,65,sample.slope)));
                case "CLIFF":return (sample.rock&RockPotential.CLIFF)!=0?1:0;
                case "SCREE":
                    float scree=(sample.rock&RockPotential.SCREE)!=0?.8f:
                        Mathf.InverseLerp(13f,43f,sample.slope)*Mathf.InverseLerp(.8f,.35f,sample.surfaceVariation)*.75f;
                    return (1-sample.snowCoverage)*scree;
                case "SCREE_COARSE":return (1-sample.snowCoverage)*Mathf.InverseLerp(18f,45f,sample.slope)*
                    Mathf.InverseLerp(.28f,.8f,sample.surfaceVariation);
                case "SOIL":return (1-sample.snowCoverage)*(1-Mathf.InverseLerp(14f,38f,sample.slope))*
                    Mathf.InverseLerp(.53f,.84f,sample.surfaceVariation)*.65f;
                case "SOIL_DAMP":return (1-sample.snowCoverage)*(1-Mathf.InverseLerp(6f,24f,sample.slope))*
                    Mathf.InverseLerp(.44f,.15f,sample.surfaceVariation)*.8f;
                case "ALPINE_GROUND":case "GROUND":case "GRASS":return (1-sample.snowCoverage)*(sample.slope<40?1:.15f);
                case "VEGETATION":return (1-sample.snowCoverage)*sample.forestDensity;
                default:return 0; // UNKNOWN never becomes a ground family arbitrarily.
            }
        }
    }
    public static class VisualPipelineFallback
    {
        public static VisualPipeline Current=>UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline==null?VisualPipeline.BuiltIn:VisualSafety.PipelineMatches(VisualPipeline.Universal)?VisualPipeline.Universal:VisualPipeline.Any;
        public static Material CreateTerrain()
        {
            string path=Current==VisualPipeline.Universal?"Universal Render Pipeline/Terrain/Lit":Current==VisualPipeline.BuiltIn?"Nature/Terrain/Standard":null;
            var shader=path==null?null:Shader.Find(path);return shader!=null&&shader.isSupported?new Material(shader){name="SkiStation owned Terrain fallback"}:null;
        }
        public static Material CreateMesh(Color color)
        {
            string path=Current==VisualPipeline.Universal?"Universal Render Pipeline/Lit":Current==VisualPipeline.BuiltIn?"Standard":null;
            var shader=path==null?null:Shader.Find(path);if(shader==null||!shader.isSupported)return null;
            var material=new Material(shader){name="SkiStation owned scenery",enableInstancing=true};
            if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);if(material.HasProperty("_Color"))material.SetColor("_Color",color);
            if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.15f);return material;
        }
    }
}

