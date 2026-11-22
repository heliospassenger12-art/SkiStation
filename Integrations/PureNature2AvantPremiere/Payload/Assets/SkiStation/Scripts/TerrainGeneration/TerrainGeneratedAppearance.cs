using System.Collections.Generic;
using UnityEngine;
using SkiStation.Visuals;
namespace SkiStation.TerrainGeneration
{
    // Render-only splat adapter. Never changes heightmap, holes, collision, zones or construction policy.
    public sealed class TerrainGeneratedAppearance:MonoBehaviour
    {
        TerrainGenerationWorld world;string requested=VisualProfileRegistry.DefaultId;int revision=-1;
        Material nativeMaterial,ownedMaterial,fallbackMaterial;TerrainLayer[] nativeLayers;
        Texture2D naturalSnow, powderSnow, windSnowNormal;
        bool seasonalSnow, appliedWinter;
        bool WinterSnow => seasonalSnow && RealTerrainWater.Season == AlpineSeason.Winter;
        internal void EnableSeasonalSnow() => seasonalSnow = true;
        readonly List<TerrainLayer> layers=new List<TerrainLayer>();readonly List<Texture2D> textures=new List<Texture2D>();
        public string RequestedId=>requested;
        public string ResolvedId=>VisualProfileRegistry.ResolveProfileOrFallback(requested).profileId;
        TerrainLayer Native(Color color)
        {var texture=new Texture2D(8,8,TextureFormat.RGBA32,false){name="SkiStation native terrain color"};for(int y=0;y<8;y++)for(int x=0;x<8;x++)texture.SetPixel(x,y,color*(.96f+.04f*Mathf.Sin(x*2+y*3)));texture.Apply();textures.Add(texture);var layer=new TerrainLayer{diffuseTexture=texture,tileSize=new Vector2(12,12),smoothness=.15f};layers.Add(layer);return layer;}
        public void Connect(TerrainGenerationWorld owner)
        {
            world=owner;nativeMaterial=world.Terrain.materialTemplate;fallbackMaterial=VisualPipelineFallback.CreateTerrain();
            nativeLayers=ValAlpinTestScene.IsActive
                ?new[]{Native(new Color(.72f,.79f,.85f)),Native(new Color(.25f,.26f,.28f))}
                :new[]{Native(new Color(.9f,.94f,.98f)),Native(new Color(.28f,.27f,.25f))};
            world.Terrain.terrainData.alphamapResolution=128;Select(DomainVisualPreferences.Get(world.domainId));
        }
        public void Select(string id){requested=VisualProfileRegistry.ValidId(id)?id:VisualProfileRegistry.DefaultId;Apply();}
        public void RefreshClimate(){Apply();world?.Scenery?.RefreshClimate();}
        void Update(){if(world==null||!world.IsReady)return;if(revision!=VisualProfileRegistry.Revision || appliedWinter!=WinterSnow)Apply();}
        TerrainLayer SnowLayer(float tileSize, Color tint, bool withNormal, bool powder = false)
        {
            if(naturalSnow==null)
            {
                naturalSnow=ValAlpin.ValAlpinNatureSurface.CreateSoftSnow(fineDetail:true,brightSnow:true);
                textures.Add(naturalSnow);
            }
            if(withNormal&&windSnowNormal==null)
            {
                windSnowNormal=ValAlpin.ValAlpinNatureSurface.CreateWindSnowNormal();
                textures.Add(windSnowNormal);
            }
            if(powder&&powderSnow==null)
            {
                powderSnow=ValAlpin.ValAlpinNatureSurface.CreateSoftSnow(brightSnow:true);
                powderSnow.name="SkiStation · poudreuse";
                textures.Add(powderSnow);
            }
            var layer=new TerrainLayer
            {
                diffuseTexture=powder?powderSnow:naturalSnow, normalMapTexture=withNormal?windSnowNormal:null,
                tileSize=Vector2.one*tileSize, diffuseRemapMax=tint,
                normalScale=withNormal?.32f:1f, smoothness=withNormal?.16f:.05f
            };
            layers.Add(layer);
            return layer;
        }
        void Apply()
        {
            if(world==null||!world.IsReady)return;revision=VisualProfileRegistry.Revision;var data=world.Terrain.terrainData;
            if(ownedMaterial!=null){Destroy(ownedMaterial);ownedMaterial=null;}
            foreach(var layer in layers.ToArray())if(layer!=null&&layer!=nativeLayers[0]&&layer!=nativeLayers[1]){layers.Remove(layer);Destroy(layer);}
            world.Terrain.materialTemplate=VisualSafety.Usable(nativeMaterial,VisualPipeline.Any)?nativeMaterial:fallbackMaterial;
            var profile=VisualProfileRegistry.ResolveProfileOrFallback(requested);var set=profile.terrain;
            var selected=new List<TerrainLayer>();var categories=new List<string>();
            bool naturalSnowLayers=false;
            if(!profile.inheritNativeRendering&&set!=null&&VisualSafety.PipelineMatches(set.pipeline))
            {
                naturalSnowLayers=set.useNaturalSnowLayers;
                if(VisualSafety.Usable(set.terrainMaterial,set.pipeline)){ownedMaterial=new Material(set.terrainMaterial);world.Terrain.materialTemplate=ownedMaterial;}
                foreach(var entry in set.layers??new TerrainVisualLayer[0])
                {
                    if(entry==null||string.IsNullOrEmpty(entry.category)||entry.category=="UNKNOWN")continue;
                    TerrainLayer layer=null;
                    if(entry.terrainLayer!=null&&entry.terrainLayer.diffuseTexture!=null)layer=Instantiate(entry.terrainLayer);
                    else if(entry.albedo!=null)
                    {
                        layer=new TerrainLayer{diffuseTexture=entry.albedo,normalMapTexture=entry.normal,maskMapTexture=entry.mask,tileSize=entry.tiling,metallic=entry.metallic,smoothness=entry.smoothness};
                        if(naturalSnowLayers&&entry.category=="ROCK")
                        {
                            // The BK detail texture relies on tint in its original mesh material.
                            layer.diffuseRemapMax=new Color(.42f,.41f,.40f,1f);
                            layer.normalScale=.7f;
                        }
                    }
                    if(layer==null)continue;
                    selected.Add(layer);
                    categories.Add(entry.category.ToUpperInvariant());
                    layers.Add(layer);
                    if(selected.Count>=12)break;
                }
            }
            if(selected.Count==0){selected.Add(nativeLayers[0]);selected.Add(nativeLayers[1]);categories.Add("SNOW");categories.Add("ROCK");}
            else
            {
                // Partial local sets keep native snow/rock for families not mapped, with no missing reference.
                if(!categories.Contains("SNOW"))
                {
                    selected.Add(naturalSnowLayers?SnowLayer(24f,Color.white,false):nativeLayers[0]);
                    categories.Add("SNOW");
                }
                if(!categories.Exists(c=>c.ToUpperInvariant()=="ROCK")){selected.Add(nativeLayers[1]);categories.Add("ROCK");}
            }
            if(naturalSnowLayers)
            {
                selected.Add(SnowLayer(14f,new Color(1f,.995f,.987f,1f),false,powder:true));
                categories.Add("SNOW_POWDER");
                selected.Add(SnowLayer(40f,new Color(.88f,.93f,.98f,1f),true));
                categories.Add("SNOW_WINDPACKED");
            }
            appliedWinter = WinterSnow;
            var nature = world.GetComponent<RealTerrainNature>();
            int resolution = nature != null
                ? Mathf.Max(128, nature.RockPaintResolution, nature.ForestPaintResolution) : 128;
            int rockLayer = categories.IndexOf("ROCK");
            int cliffLayer = categories.IndexOf("CLIFF");
            int forestLayer = categories.IndexOf("WOODLAND");
            int snowLayer = categories.IndexOf("SNOW");
            int powderLayer = categories.IndexOf("SNOW_POWDER");
            int windSnowLayer = categories.IndexOf("SNOW_WINDPACKED");
            bool orcieresWinterRock = world.domainId == ValAlpin.ValAlpinScene.OrcieresDomainId &&
                profile.profileId == "ORCIERES_NEIGE" && rockLayer >= 0;
            if (appliedWinter && forestLayer >= 0)
                selected[forestLayer].diffuseRemapMax = new Color(.73f, .80f, .82f, 1f);
            if (data.alphamapResolution != resolution) data.alphamapResolution = resolution;
            data.terrainLayers=selected.ToArray();var alphas=new float[resolution,resolution,selected.Count];var weights=new float[selected.Count];
            var origin=world.Terrain.transform.position;
            float span = resolution - 1;
            for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
            {
                var p=new Vector3(origin.x+data.size.x*x/span,0,origin.z+data.size.z*z/span);var sample=TerrainVisualBridge.Sample(world,p);float sum=0;
                if(naturalSnowLayers)sample.surfaceVariation=Mathf.PerlinNoise(p.x*.026f+81.1f,p.z*.026f+39.7f);
                for(int i=0;i<weights.Length;i++){weights[i]=TerrainVisualBridge.Weight(categories[i],sample);sum+=weights[i];}
                if(sum<.00001f){int ground=categories.FindIndex(c=>c=="GROUND"||c=="ALPINE_GROUND");if(ground<0)ground=categories.FindIndex(c=>c=="ROCK");if(ground<0)ground=0;weights[ground]=1;sum=1;}
                float exposed = rockLayer >= 0 && nature != null ? nature.RockCoverage(x, z) : 0f;
                if (orcieresWinterRock)
                {
                    // On exposed high faces, breaks in the snow follow the underlying slope and rock mask.
                    float cliff = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(38f, 60f, sample.slope));
                    float altitude = Mathf.InverseLerp(2100f, 2550f, sample.altitudeMeters);
                    float ledges = Mathf.PerlinNoise(p.x * .055f + 17f, p.z * .055f + 29f);
                    exposed = Mathf.Max(exposed, altitude * cliff * Mathf.Lerp(.46f, .83f, ledges));
                    float estaris = RealTerrainNature.EstarisInfluence(p.x,p.z);
                    float brokenRock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(29f,53f,sample.slope));
                    exposed = Mathf.Max(exposed, estaris*altitude*brokenRock*Mathf.Lerp(.28f,.90f,ledges));
                }
                float forest = forestLayer >= 0 && nature != null
                    ? nature.ForestCoverage(x, z) * (appliedWinter ? .12f : .56f) * (1f-exposed) : 0f;
                float remaining = 1f - exposed - forest;
                float snow = weights[snowLayer] / sum * remaining;
                // A winter snowpack covers the clearings and forest floor. The steepest cuts keep visible rock.
                float minimumSnow = appliedWinter ? Mathf.Lerp(.94f, .37f, Mathf.InverseLerp(35f, 67f, sample.slope)) : 0f;
                float snowFill = Mathf.Max(0f, minimumSnow - snow);
                float scale = snowFill > 0f ? (1f - minimumSnow) / (1f - snow) : 1f;
                float faceRock=cliffLayer<0?0f:exposed*Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(48f,68f,sample.slope));
                for(int i=0;i<weights.Length;i++)
                {
                    float weight = weights[i]/sum*remaining + (i==rockLayer ? exposed-faceRock : 0f) +
                        (i==cliffLayer ? faceRock : 0f) + (i==forestLayer ? forest : 0f);
                    alphas[z,x,i] = i == snowLayer ? snow + snowFill : weight * scale;
                }
                if(naturalSnowLayers)
                {
                    float powder=(1f-Mathf.InverseLerp(18f,48f,sample.slope))*
                        Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.28f,.78f,sample.surfaceVariation))*.72f;
                    float wind=Mathf.InverseLerp(15f,66f,sample.slope)*Mathf.Lerp(.24f,.78f,1f-sample.surfaceVariation);
                    wind*=1f-powder;
                    float totalSnow=snow+snowFill;
                    alphas[z,x,snowLayer]=totalSnow*(1f-powder-wind);
                    alphas[z,x,powderLayer]=totalSnow*powder;
                    alphas[z,x,windSnowLayer]=totalSnow*wind;
                }
            }
            data.SetAlphamaps(0,0,alphas);
        }
        void OnDestroy(){if(ownedMaterial!=null)Destroy(ownedMaterial);if(fallbackMaterial!=null)Destroy(fallbackMaterial);foreach(var layer in layers)if(layer!=null)Destroy(layer);foreach(var texture in textures)if(texture!=null)Destroy(texture);}
    }
}

