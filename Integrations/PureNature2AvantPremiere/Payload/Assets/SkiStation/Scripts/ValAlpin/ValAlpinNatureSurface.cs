
using System;
using System.Collections.Generic;

using UnityEngine;
using SkiStation.TerrainGeneration;
using SkiStation.Visuals;

namespace SkiStation.ValAlpin
{
    // Explicit local art trial. Source assets and gameplay geometry remain untouched.
    public sealed class ValAlpinNatureSurface
    {
        readonly ValAlpinNatureAssets assets;
        public ValAlpinNatureSurface(ValAlpinNatureAssets data){assets=data;}
        Light savedSun;
        float savedIntensity;
        Color savedColor;
        Quaternion savedRotation;
        bool captured;
        Material terrainMaterial;
        Texture2D snowSurface;
        Texture2D matteMask;

        Texture2D MatteMask()
        {
            if (matteMask != null) return matteMask;
            matteMask = new Texture2D(2,2,TextureFormat.RGBA32,false,true)
            {name="ValAlpin matte surface mask",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Repeat};
            var value = new Color(0,1,.5f,.08f);
            matteMask.SetPixels(new[]{value,value,value,value}); matteMask.Apply(false,true);
            return matteMask;
        }

        Texture2D SoftSnow() => snowSurface != null ? snowSurface : snowSurface = CreateSoftSnow();

        internal static Texture2D CreateSoftSnow(bool fineDetail = false, bool brightSnow = false)
        {
            const int size = 256;
            var snow = new Texture2D(size,size,TextureFormat.RGBA32,true)
            {
                name = "ValAlpin neige douce",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            var pixels = new Color[size*size];
            for (int y=0;y<size;y++) for (int x=0;x<size;x++)
            {
                float u=x/(float)size, v=y/(float)size;
                // Periodic blend keeps tile boundaries continuous; low contrast avoids stripes.
                float a=Mathf.PerlinNoise(u*5+13,v*5+29);
                float b=Mathf.PerlinNoise((u-1)*5+13,v*5+29);
                float c=Mathf.PerlinNoise(u*5+13,(v-1)*5+29);
                float d=Mathf.PerlinNoise((u-1)*5+13,(v-1)*5+29);
                float n=Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v);
                float shade=brightSnow ? .825f+n*.047f : .76f+n*.06f;
                if (fineDetail)
                {
                    const float frequency = 48f;
                    float g = Mathf.Lerp(
                        Mathf.Lerp(Mathf.PerlinNoise(u*frequency+43,v*frequency+71),Mathf.PerlinNoise((u-1)*frequency+43,v*frequency+71),u),
                        Mathf.Lerp(Mathf.PerlinNoise(u*frequency+43,(v-1)*frequency+71),Mathf.PerlinNoise((u-1)*frequency+43,(v-1)*frequency+71),u),v);
                    shade += (g-.5f)*.065f;
                }
                pixels[y*size+x]=new Color(shade,shade+(brightSnow ? .014f : .025f),shade+(brightSnow ? .033f : .045f),1);
            }
            snow.SetPixels(pixels); snow.Apply(true,true);
            return snow;
        }

        internal static Texture2D CreateWindSnowNormal()
        {
            const int size = 256;
            var heights = new float[size * size];
            var pixels = new Color32[heights.Length];
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = "Orcières · relief fin de la neige",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                heights[y * size + x] = TileNoise(u, v, 22f, 7f) * .75f +
                    TileNoise(u, v, 55f, 51f) * .25f;
            }
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = heights[y * size + (x + 1) % size] - heights[y * size + (x + size - 1) % size];
                float dy = heights[((y + 1) % size) * size + x] - heights[((y + size - 1) % size) * size + x];
                Vector3 n = new Vector3(-dx * .50f, -dy * .50f, 1f).normalized;
                pixels[y * size + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1f);
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        static float TileNoise(float u, float v, float frequencyX, float frequencyY)
        {
            float x = u * frequencyX + 43f, y = v * frequencyY + 71f;
            return Mathf.Lerp(
                Mathf.Lerp(Mathf.PerlinNoise(x, y), Mathf.PerlinNoise(x - frequencyX, y), u),
                Mathf.Lerp(Mathf.PerlinNoise(x, y - frequencyY), Mathf.PerlinNoise(x - frequencyX, y - frequencyY), u), v);
        }

        public void Apply(TerrainGenerationWorld world)
        {
            if (world == null || world.domainId != "lab.gestion.valalpin" || !world.IsReady)
                throw new InvalidOperationException("Essai réservé au terrain ValAlpin prêt.");
            var profile = assets.profile;
            if (profile == null || profile.terrain == null)
                throw new InvalidOperationException("Profil terrain Pure Nature absent. Aucun essai appliqué.");
            if (profile.terrain.useNaturalSnowLayers)
            {
                world.Terrain.GetComponent<TerrainGeneratedAppearance>().EnableSeasonalSnow();
                world.SelectVisual(profile.profileId, false);
                ApplyLighting();
                return;
            }

            var snow = SoftSnow();
            var rock = assets.rockAlbedo;
            var rockNormal = assets.rockNormal;
            var entries = new List<TerrainVisualLayer>();
            foreach (var entry in profile.terrain.layers)
            {
                if (entry == null) continue;
                string category = (entry.category ?? "").ToUpperInvariant();
                if (category != "SNOW" && category != "ROCK" && category != "CLIFF") entries.Add(entry);
            }
            entries.Insert(0, new TerrainVisualLayer { category = "SNOW", albedo = snow, tiling = new Vector2(64,64), smoothness = .08f });
            entries.Insert(1, new TerrainVisualLayer { category = "ROCK", albedo = rock, normal = rockNormal, tiling = new Vector2(28,28), smoothness = .08f });
            entries.Insert(2, new TerrainVisualLayer { category = "CLIFF", albedo = rock, normal = rockNormal, tiling = new Vector2(42,42), smoothness = .05f });
            profile.terrain.layers = entries.ToArray();

            if(terrainMaterial==null)terrainMaterial=new Material(assets.terrainShader){name="ValAlpin macro and triplanar terrain",hideFlags=HideFlags.HideAndDontSave};
            profile.terrain.terrainMaterial=terrainMaterial;
            world.SelectVisual(assets.profile.profileId, false);
            world.Terrain.basemapDistance=40000;

            // Paint presentation weights only; no height/holes/collider writes.
            var data = world.Terrain.terrainData;
            int count = data.terrainLayers.Length;
            if (count < 3) throw new InvalidOperationException("Les trois couches de surface ne sont pas installées.");
            var terrainLayers = data.terrainLayers;
            // BK's detail albedo is intentionally near-white: its mesh shader normally supplies tint.
            terrainLayers[0].diffuseRemapMin = Vector4.zero;
            terrainLayers[0].diffuseRemapMax = new Vector4(.88f,.90f,.93f,1);
            terrainLayers[1].diffuseRemapMin = Vector4.zero;
            terrainLayers[1].diffuseRemapMax = new Vector4(.26f,.28f,.31f,1);
            terrainLayers[2].diffuseRemapMin = Vector4.zero;
            terrainLayers[2].diffuseRemapMax = new Vector4(.20f,.22f,.25f,1);
            terrainLayers[1].normalScale = .65f;
            terrainLayers[2].normalScale = .8f;
            for(int i=0;i<3;i++)
            {
                terrainLayers[i].maskMapTexture = MatteMask();
                terrainLayers[i].maskMapRemapMin = Vector4.zero;
                terrainLayers[i].maskMapRemapMax = Vector4.one;
                terrainLayers[i].metallic = 0;
            }
            data.terrainLayers = terrainLayers;
            const int resolution = 512;
            data.alphamapResolution = resolution;
            var alpha = new float[resolution,resolution,count];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (resolution - 1f), v = z / (resolution - 1f);
                float slope = data.GetSteepness(u,v);
                float variation = Mathf.PerlinNoise(u*21+17.3f,v*21+9.1f);
                float exposed = Mathf.SmoothStep(0,1,Mathf.InverseLerp(29,57,slope+(variation-.5f)*12));
                float cliff = Mathf.SmoothStep(0,1,Mathf.InverseLerp(54,73,slope));
                alpha[z,x,0] = 1-exposed;
                alpha[z,x,1] = exposed*(1-cliff);
                alpha[z,x,2] = exposed*cliff;
            }
            data.SetAlphamaps(0,0,alpha);
            data.SetBaseMapDirty();
            world.Terrain.Flush();
            ApplyLighting();
            Debug.Log("[ValAlpin surface] Rendu runtime appliqué : neige douce sans texture striée ni normale de neige, roche Pure Nature selon pente, masque 512². Relief/collisions inchangés. Retour : SkiStation > Visuals > ValAlpin > Revenir aux visuels natifs. Validation visuelle en Game encore nécessaire.",world);
        }

        void ApplyLighting()
        {
            var sun = RenderSettings.sun;
            if (sun == null || sun.type != LightType.Directional) return;
            if (!captured)
            {
                savedSun = sun; savedIntensity = sun.intensity; savedColor = sun.color;
                savedRotation = sun.transform.rotation; captured = true;
            }
            sun.intensity = .85f;
            sun.color = new Color(1f,.97f,.93f);
            sun.transform.rotation = Quaternion.Euler(32,-48,0);
        }


        public void Dispose()
        {
            RestoreLighting();
            if(terrainMaterial!=null)UnityEngine.Object.Destroy(terrainMaterial);
            terrainMaterial=null;
            if(snowSurface!=null)UnityEngine.Object.Destroy(snowSurface);
            if(matteMask!=null)UnityEngine.Object.Destroy(matteMask);
            snowSurface=null;matteMask=null;
        }
        public void RestoreLighting()
        {
            if (captured && savedSun != null)
            {
                savedSun.intensity = savedIntensity; savedSun.color = savedColor;
                savedSun.transform.rotation = savedRotation;
            }
            captured = false; savedSun = null;
        }
    }
}

