
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SkiStation.TerrainGeneration;

namespace SkiStation.ValAlpin
{
    // Runtime landscape: renderer-only copies, native gameplay geometry stays authoritative.
    // Copies render parts only, never commercial behaviours or colliders.
    public sealed class ValAlpinNatureLandscape
    {
        const string Pack="Assets/BK/PureNature_Mountains/Prefabs/";
        readonly ValAlpinNatureAssets assets;
        public ValAlpinNatureLandscape(ValAlpinNatureAssets data){assets=data;}
        readonly List<Tuple<GameObject,Vector3,float>> vegetation=new List<Tuple<GameObject,Vector3,float>>();

        readonly List<Tuple<GameObject,Vector3,float>> rockDecor=new List<Tuple<GameObject,Vector3,float>>();
        public Vector3 RockSector {get;private set;}
        GameObject root;
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly Dictionary<Material,Material> materials=new Dictionary<Material,Material>();
        readonly Dictionary<Camera,Tuple<CameraClearFlags,float>> cameraStates=new Dictionary<Camera,Tuple<CameraClearFlags,float>>();
        readonly Dictionary<UniversalAdditionalCameraData,bool> postStates=new Dictionary<UniversalAdditionalCameraData,bool>();
        RenderPipelineAsset oldPipeline;
        bool pipelineCaptured;
        Material oldSky;
        bool oldFog,captured;
        FogMode oldFogMode;
        AmbientMode oldAmbientMode;
        Color oldFogColor,oldSkyColor,oldEquatorColor,oldGroundColor;
        float oldFogStart,oldFogEnd;
        SphericalHarmonicsL2 oldProbe;
        public Vector3 Sector {get;private set;}

        float Rand(System.Random r,float a,float b)=>(float)(a+r.NextDouble()*(b-a));
        Material MaterialFor(Material source)
        {
            if(source==null)return null;
            if(materials.TryGetValue(source,out var found))return found;
            Material m;
            string shader=source.shader!=null?source.shader.name:"";
            if(shader.StartsWith("BK/")||shader.StartsWith("Universal Render Pipeline/")) m=new Material(source);
            else
            {
                var lit=assets.litShader;
                if(lit==null)throw new InvalidOperationException("URP Lit absent");
                m=new Material(lit);
                if(source.HasProperty("_MainTex"))m.SetTexture("_BaseMap",source.GetTexture("_MainTex"));
                if(source.HasProperty("_BumpMap")&&source.GetTexture("_BumpMap")!=null)
                {m.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));m.EnableKeyword("_NORMALMAP");}
                m.SetColor("_BaseColor",new Color(.72f,.77f,.82f));m.SetFloat("_Smoothness",.07f);
            }

            m.name="ValAlpin art / "+source.name;m.hideFlags=HideFlags.HideAndDontSave;m.enableInstancing=true;
            ValAlpinNatureMaterials.Configure(m,source,assets.leavesShader,assets.rockShader,assets.impostorShader);
            owned.Add(m);materials[source]=m;return m;
        }

        GameObject RenderCopy(GameObject prefab,Transform parent,Vector3 position,Quaternion rotation,Vector3 targetSize,int lod,string label)
        {
            if(prefab==null)throw new InvalidOperationException("Prefab manquant : "+label);
            var group=prefab.GetComponentInChildren<LODGroup>(true);
            Renderer[] renderers;
            if(group!=null&&group.GetLODs().Length>0)
            {var levels=group.GetLODs();renderers=levels[Mathf.Min(lod,levels.Length-1)].renderers;}
            else renderers=prefab.GetComponentsInChildren<MeshRenderer>(true);
            var container=new GameObject(label);container.transform.SetParent(parent,false);
            Bounds bounds=new Bounds();bool first=true;
            foreach(var source in renderers)
            {
                if(source==null)continue;
                var filter=source.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
                var matrix=prefab.transform.worldToLocalMatrix*source.transform.localToWorldMatrix;
                var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var corner=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(first){bounds=new Bounds(corner,Vector3.zero);first=false;}else bounds.Encapsulate(corner);
                }
                var part=new GameObject("Render part");part.transform.SetParent(container.transform,false);
                part.transform.localPosition=matrix.GetColumn(3);part.transform.localRotation=matrix.rotation;part.transform.localScale=matrix.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var renderer=part.AddComponent<MeshRenderer>();
                var mapped=new Material[source.sharedMaterials.Length];for(int j=0;j<mapped.Length;j++)mapped[j]=MaterialFor(source.sharedMaterials[j]);
                renderer.sharedMaterials=mapped;renderer.shadowCastingMode=ShadowCastingMode.On;
            }
            if(first)throw new InvalidOperationException("Aucun mesh : "+label);
            Vector3 scale=new Vector3(targetSize.x/Mathf.Max(.01f,bounds.size.x),targetSize.y/Mathf.Max(.01f,bounds.size.y),targetSize.z/Mathf.Max(.01f,bounds.size.z));
            container.transform.localScale=scale;container.transform.rotation=rotation;
            container.transform.position=position-rotation*Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),scale);
            return container;
        }

        public void Apply(TerrainGenerationWorld world)
        {
            Clear();
            if(world==null||world.domainId!="lab.gestion.valalpin"||!world.IsReady)throw new InvalidOperationException("ValAlpin prêt requis");
            root=new GameObject("ValAlpin — Pure Nature");root.transform.SetParent(world.transform,false);
            var data=world.Terrain.terrainData;var origin=world.Terrain.transform.position;
            var center=origin+new Vector3(data.size.x*.5f,0,data.size.z*.5f);
            float extent=Mathf.Max(data.size.x,data.size.z);
            float low=world.ClampedHeight(center.x,origin.z+data.size.z*.08f);
            Atmosphere(extent);
            if(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                oldPipeline=QualitySettings.renderPipeline;pipelineCaptured=true;
                var copy=UnityEngine.Object.Instantiate(pipeline);copy.hideFlags=HideFlags.HideAndDontSave;
                copy.shadowDistance=320;copy.shadowCascadeCount=4;
                // Runtime preserves the active pipeline soft-shadow support.
                QualitySettings.renderPipeline=copy;owned.Add(copy);
            }
            var volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.priority=90;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.hideFlags=HideFlags.HideAndDontSave;owned.Add(profile);volume.sharedProfile=profile;
            profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;
            var grading=profile.Add<ColorAdjustments>(true);grading.postExposure.value=.45f;grading.contrast.value=4;grading.saturation.value=-2;
            foreach(var component in profile.components)owned.Add(component);
            if(RenderSettings.sun!=null){RenderSettings.sun.intensity=1.05f;RenderSettings.sun.color=new Color(1f,.94f,.85f);}
            foreach(var camera in Camera.allCameras)
            {
                if(camera.gameObject.scene!=world.gameObject.scene)continue;
                ConfigureCamera(camera,extent);
            }

            Apron(world,extent);
            var peakMaterial=new Material(assets.exteriorShader){name="ValAlpin dolomite peaks",hideFlags=HideFlags.HideAndDontSave};
            peakMaterial.SetTexture("_RockTex",assets.rockAlbedo);
            peakMaterial.SetColor("_RockTint",new Color(.44f,.42f,.39f,1));peakMaterial.SetTexture("_RockNormal",assets.rockNormal);peakMaterial.SetFloat("_StrataStrength",.06f);

            foreach(var sourceRenderer in assets.cliffs[0].GetComponentsInChildren<MeshRenderer>(true))
            {
                bool mapped=false;
                foreach(var source in sourceRenderer.sharedMaterials)
                {
                    if(source==null||!source.HasProperty("_MainTex")||source.GetTexture("_MainTex")==null)continue;
                    peakMaterial.SetTexture("_RockTex",source.GetTexture("_MainTex"));
                    if(source.HasProperty("_BumpMap"))peakMaterial.SetTexture("_RockNormal",source.GetTexture("_BumpMap"));
                    mapped=true;break;
                }
                if(mapped)break;
            }
            peakMaterial.SetFloat("_RockScale",220);peakMaterial.SetColor("_RockTint",new Color(.76f,.57f,.42f,1));
            peakMaterial.SetFloat("_GeologyStrength",1);peakMaterial.SetFloat("_StrataStrength",0);
            owned.Add(peakMaterial);owned.Add(ValAlpinRockPeaks.Build(world,root.transform,peakMaterial));
            var random=new System.Random(631);
            const int mountains=0; // Continuous exterior replaces detached mountain prefabs.

            int cliffs=0,boulders=0;
            var rockCenters=new List<Vector3>();
            for(int attempt=0;attempt<5000&&rockCenters.Count<18;attempt++)
            {
                float x=origin.x+Rand(random,.12f,.88f)*data.size.x,z=origin.z+Rand(random,.22f,.88f)*data.size.z;
                float u=(x-origin.x)/data.size.x,v=(z-origin.z)/data.size.z;
                float slope=data.GetSteepness(u,v),y=world.ClampedHeight(x,z);
                if(slope<40||slope>73||y<900||y>2500)continue;
                var point=new Vector3(x,y,z);bool close=false;
                foreach(var other in rockCenters)if(Vector3.Distance(other,point)<650){close=true;break;}
                if(close||Vector3.Distance(point,world.FrontDeNeige.position)<650)continue;
                rockCenters.Add(point);

                var normal=data.GetInterpolatedNormal(u,v);
                var downhill=new Vector3(normal.x,0,normal.z).normalized;
                if(downhill.sqrMagnitude<.1f)continue;
                var along=Vector3.Cross(Vector3.up,downhill);

                float bedding=Rand(random,.78f,1.22f);
                float ledgeScale=Rand(random,.7f,1.3f);
                for(int row=-1;row<=1;row++)
                for(int k=-2;k<=2;k++)
                {
                    if(row==1&&Mathf.Abs(k)==2)continue;
                    var p=point+along*(k*110*bedding+Rand(random,-18,18))+
                        downhill*(-row*105+Rand(random,-26,26));

                    float ru=(p.x-origin.x)/data.size.x,rv=(p.z-origin.z)/data.size.z;
                    float localSlope=data.GetSteepness(ru,rv);
                    var localNormal=data.GetInterpolatedNormal(ru,rv);
                    var localDownhill=new Vector3(localNormal.x,0,localNormal.z).normalized;
                    float ground=world.ClampedHeight(p.x,p.z);
                    float uphill=world.ClampedHeight(p.x-localDownhill.x*180,p.z-localDownhill.z*180);
                    // Never plant a cliff pillar on a rounded crest or let it redefine the summit.
                    if(localSlope<38||localSlope>73||uphill-ground<60)continue;
                    float h=Rand(random,90,169)*ledgeScale;if(k==0)h*=1.1f;
                    var size=new Vector3(h*Rand(random,1.2f,2.1f),h,h*Rand(random,.44f,.82f));
                    var face=Quaternion.AngleAxis(Rand(random,-18,18),Vector3.up)*Quaternion.LookRotation(localDownhill);
                    PlaceRock(world,assets.cliffs[cliffs%assets.cliffs.Length],p,face,
                        size,Rand(random,.38f,.53f),"Falaise / "+cliffs);
                    if(RockSector==Vector3.zero)RockSector=new Vector3(p.x,ground+45,p.z);
                    cliffs++;
                }
                int localBoulders=0;
                for(int attemptRock=0;attemptRock<70&&localBoulders<12;attemptRock++)
                {
                    var p=point+along*Rand(random,-360,360)+downhill*Rand(random,180,600);
                    float pu=(p.x-origin.x)/data.size.x,pv=(p.z-origin.z)/data.size.z;
                    if(pu<.05f||pu>.95f||pv<.05f||pv>.95f||data.GetSteepness(pu,pv)>36)continue;
                    GameObject[] palette;
                    float h;
                    if(localBoulders%4==0)
                    {
                        palette=assets.boulders;
                        h=Rand(random,8,22);
                    }
                    else if(localBoulders%4==3&&assets.pebbles!=null&&assets.pebbles.Length>0)
                    {
                        palette=assets.pebbles;
                        h=Rand(random,.35f,1.25f);
                    }
                    else
                    {
                        palette=assets.stones!=null&&assets.stones.Length>0?assets.stones:assets.boulders;
                        h=Rand(random,1.5f,6f);
                    }
                    PlaceRock(world,palette[boulders%palette.Length],p,Quaternion.Euler(0,Rand(random,0,360),0),
                        new Vector3(h*Rand(random,1.2f,2.15f),h,h*Rand(random,1.05f,1.65f)),.28f,"Éboulis / "+boulders);
                    boulders++;localBoulders++;
                }
            }
            // Forest study is restricted to lower slopes and excludes the arrival corridor.
            int trees=0;var centers=new List<Vector3>();
            var occupied = new HashSet<Vector2Int>();
            random = new System.Random(world.Recipe.seed ^ 438919);

            for(int attempt=0;attempt<1200&&centers.Count<55;attempt++)
            {
                float x=origin.x+Rand(random,.05f,.95f)*data.size.x,z=origin.z+Rand(random,.05f,.95f)*data.size.z;
                float u=(x-origin.x)/data.size.x,v=(z-origin.z)/data.size.z,y=world.ClampedHeight(x,z);
                if(data.GetSteepness(u,v)>30||y>2050)continue;
                var sample=SkiStation.Visuals.TerrainVisualBridge.Sample(world,new Vector3(x,y,z));
                if(sample.permanentSnow||sample.glacier||sample.forestDensity<=.05f)continue;
                var pos=new Vector3(x,y,z);bool close=false;
                foreach(var other in centers)if(Vector3.Distance(pos,other)<420){close=true;break;}
                if(!close)centers.Add(pos);
            }
            foreach(var cluster in centers)
            for(int j=0;j<128&&trees<3800;j++)
            {
                float x=cluster.x+Rand(random,-210,210),z=cluster.z+Rand(random,-210,210);
                float u=(x-origin.x)/data.size.x,v=(z-origin.z)/data.size.z;
                if(u<.03f||u>.97f||v<.03f||v>.97f||data.GetSteepness(u,v)>30)continue;
                float y=world.ClampedHeight(x,z);
                var sample=SkiStation.Visuals.TerrainVisualBridge.Sample(world,new Vector3(x,y,z));
                if(sample.forestDensity<=.05f||sample.permanentSnow||sample.glacier)continue;
                if(Mathf.Abs(x)<180&&z<world.FrontDeNeige.position.z+450)continue;
                if(Vector3.Distance(new Vector3(x,y,z),world.FrontDeNeige.position)<450)continue;
                if (!occupied.Add(new Vector2Int(Mathf.FloorToInt(x/6f), Mathf.FloorToInt(z/6f)))) continue;
                float h=Rand(random,18,30);
                var prefab=assets.trees[trees%assets.trees.Length];

                var position=new Vector3(x,y-2.5f,z);var rotation=Quaternion.Euler(0,Rand(random,0,360),0);
                var tree=new GameObject(prefab.name + " / "+trees);tree.transform.SetParent(root.transform,false);
                float width = h * Rand(random,.31f,.41f);
                var size=new Vector3(width,h,width);
                var lod=tree.AddComponent<LODGroup>();
                lod.fadeMode=LODFadeMode.CrossFade;
                var prefabGroup = prefab.GetComponentInChildren<LODGroup>(true);
                int levels = Mathf.Clamp(prefabGroup != null ? prefabGroup.lodCount : 1, 1, 3);
                var sceneLods = new LOD[levels];
                for (int level = 0; level < levels; level++)
                {
                    var skin = RenderCopy(prefab,tree.transform,position,rotation,size,level,"LOD "+level);
                    var renderers = skin.GetComponentsInChildren<Renderer>();
                    if (level > 0)
                        foreach (var renderer in renderers) renderer.shadowCastingMode = ShadowCastingMode.Off;
                    float height = level == levels - 1 ? .008f : level == 0 ? .075f : .034f;
                    sceneLods[level] = new LOD(height, renderers) {fadeTransitionWidth=.15f};
                }
                lod.SetLODs(sceneLods);
                lod.RecalculateBounds();
                vegetation.Add(Tuple.Create(tree,new Vector3(x,y,z),h*.2f));
                if(trees==0)Sector=cluster;
                trees++;
            }
            Debug.Log("[ValAlpin composition] "+mountains+" massifs d'horizon, "+cliffs+" falaises, "+boulders+" blocs rocheux, "+trees+" conifères en bosquets. Rendu en jeu : aucune collision ajoutée. LOD actifs, exclusions dynamiques.",world);
        }


        GameObject PlaceRock(TerrainGenerationWorld world,GameObject prefab,Vector3 point,Quaternion rotation,Vector3 size,float buried,string label)
        {

            point.y=world.ClampedHeight(point.x,point.z);
            // Place the rock base below sampled ground across its footprint, while retaining
            // the intended top height. This avoids unsupported blocks on a steep hillside.
            float minimum=point.y;
            for(int corner=0;corner<4;corner++)
            {
                var offset=rotation*new Vector3((corner&1)==0?-size.x*.38f:size.x*.38f,0,(corner&2)==0?-size.z*.38f:size.z*.38f);
                minimum=Mathf.Min(minimum,world.ClampedHeight(point.x+offset.x,point.z+offset.z));
            }
            
float top=point.y+size.y*(1-buried),bottom=minimum-size.y*.08f;
            if(label.StartsWith("Falaise / "))
            {
                var terrain=world.Terrain;var origin=terrain.transform.position;var terrainSize=terrain.terrainData.size;
                var normal=terrain.terrainData.GetInterpolatedNormal((point.x-origin.x)/terrainSize.x,(point.z-origin.z)/terrainSize.z);
                var downhill=new Vector3(normal.x,0,normal.z).normalized;
                float crest=world.ClampedHeight(point.x-downhill.x*180,point.z-downhill.z*180);
                top=Mathf.Min(top,crest-15);
            }
            size.y=Mathf.Max(size.y*.5f,top-bottom);
            var go=RenderCopy(prefab,root.transform,new Vector3(point.x,bottom,point.z),rotation,size,0,label);
            rockDecor.Add(Tuple.Create(go,point,Mathf.Max(size.x,size.z)*.42f));
            return go;
        }

        void Apron(TerrainGenerationWorld world,float extent)
        {
            const int segments=1024,rings=65;
            var data=world.Terrain.terrainData;var p=world.Terrain.transform.position;
            var center=p+new Vector3(data.size.x*.5f,0,data.size.z*.5f);
            var vertices=new Vector3[segments*rings];var uv=new Vector2[vertices.Length];var triangles=new List<int>();
            for(int ring=0;ring<rings;ring++)for(int i=0;i<segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;var dir=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float ray=Mathf.Min(data.size.x*.5f/Mathf.Max(.0001f,Mathf.Abs(dir.x)),data.size.z*.5f/Mathf.Max(.0001f,Mathf.Abs(dir.z)));
                var edge=center+dir*ray;float t=ring/(rings-1f);var point=center+dir*(ray+t*extent*1.6f);
                float edgeY=world.ClampedHeight(edge.x,edge.z);
                var inner=edge-dir*80;
                float grade=Mathf.Clamp((edgeY-world.ClampedHeight(inner.x,inner.z))/80,-.65f,.65f);
                float distance=t*extent*1.6f;
                float continuation=edgeY+grade*320*(1-Mathf.Exp(-distance/320));
                float ridge=Mathf.PerlinNoise(point.x*.00035f+11,point.z*.00035f+7);
                float detail=Mathf.PerlinNoise(point.x*.0012f+31,point.z*.0012f+19);
                float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/1800));
                point.y=continuation+blend*((ridge-.48f)*2100+(detail-.5f)*360)-Mathf.SmoothStep(0,1,t)*450;
                vertices[ring*segments+i]=point;uv[ring*segments+i]=new Vector2(point.x/150,point.z/150);
                if(ring==rings-1)continue;int next=(i+1)%segments,a=ring*segments+i,b=ring*segments+next,c=a+segments,d=b+segments;
                triangles.AddRange(new[]{a,b,c,b,d,c});
            }
            var mesh=new Mesh{name="ValAlpin continuous boundary landscape",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
            var go=new GameObject("Paysage hors domaine jouable");go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var mat=new Material(assets.exteriorShader){hideFlags=HideFlags.HideAndDontSave};mat.SetTexture("_RockTex",assets.rockAlbedo);owned.Add(mat);go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        void Atmosphere(float extent)
        {
            oldSky=RenderSettings.skybox;oldFog=RenderSettings.fog;oldFogMode=RenderSettings.fogMode;oldFogStart=RenderSettings.fogStartDistance;oldFogEnd=RenderSettings.fogEndDistance;oldFogColor=RenderSettings.fogColor;
            oldAmbientMode=RenderSettings.ambientMode;oldSkyColor=RenderSettings.ambientSkyColor;oldEquatorColor=RenderSettings.ambientEquatorColor;oldGroundColor=RenderSettings.ambientGroundColor;captured=true;
            oldProbe=RenderSettings.ambientProbe;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=extent*.9f;RenderSettings.fogEndDistance=extent*4.5f;RenderSettings.fogColor=new Color(.64f,.73f,.82f);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.6f,.68f,.78f);RenderSettings.ambientEquatorColor=new Color(.34f,.4f,.47f);RenderSettings.ambientGroundColor=new Color(.22f,.26f,.30f);
            var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.42f,.49f,.56f));RenderSettings.ambientProbe=probe;
            var shader=assets.skyShader;
            if(shader!=null){var sky=new Material(shader){hideFlags=HideFlags.HideAndDontSave};sky.SetColor("_SkyTint",new Color(.57f,.62f,.69f));sky.SetColor("_GroundColor",new Color(.54f,.60f,.68f));sky.SetFloat("_AtmosphereThickness",.9f);sky.SetFloat("_Exposure",.85f);RenderSettings.skybox=sky;owned.Add(sky);}
        }



        public void ConfigureCamera(Camera camera,float extent)
        {
            if(camera==null)return;
            if(!cameraStates.ContainsKey(camera))cameraStates[camera]=Tuple.Create(camera.clearFlags,camera.farClipPlane);
            camera.clearFlags=CameraClearFlags.Skybox;camera.farClipPlane=Mathf.Max(camera.farClipPlane,extent*5);
            var data=camera.GetUniversalAdditionalCameraData();
            if(!postStates.ContainsKey(data))postStates[data]=data.renderPostProcessing;
            data.renderPostProcessing=true;
        }
        public void RefreshExclusions(TerrainGenerationWorld world)
        {
            var slopes=UnityEngine.Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None);
            var lifts=UnityEngine.Object.FindObjectsByType<LiftRuntime>(FindObjectsSortMode.None);
            foreach(var collection in new[]{vegetation,rockDecor})
            foreach(var item in collection)
            {
                if(item.Item1==null)continue;bool blocked=false;var p=item.Item2;
                foreach(var slope in slopes)
                {
                    if(slope.gameObject.scene!=world.gameObject.scene)continue;
                    for(int i=0;i<slope.Points.Count-1&&!blocked;i++)
                        blocked=ConstructionRules349.SegmentDistanceXZ(p,slope.Points[i],slope.Points[i+1])<=Mathf.Max(slope.GetWidthAt(i),slope.GetWidthAt(i+1))*.5f+item.Item3+5;
                    if(blocked)break;
                }
                foreach(var lift in lifts)
                {
                    if(blocked)break;if(lift.gameObject.scene!=world.gameObject.scene)continue;
                    blocked=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(lift.Bottom.x,lift.Bottom.z))<24+item.Item3
                         ||Vector2.Distance(new Vector2(p.x,p.z),new Vector2(lift.Top.x,lift.Top.z))<24+item.Item3;
                    for(int i=0;i<lift.Route.Count-1&&!blocked;i++)
                        blocked=ConstructionRules349.SegmentDistanceXZ(p,lift.Route[i],lift.Route[i+1])<=8+item.Item3;
                }
                item.Item1.SetActive(!blocked);
            }
        }
        public void Clear()
        {
            vegetation.Clear();rockDecor.Clear();Sector=Vector3.zero;RockSector=Vector3.zero;
            foreach(var pair in postStates)if(pair.Key!=null)pair.Key.renderPostProcessing=pair.Value;postStates.Clear();
            if(pipelineCaptured){QualitySettings.renderPipeline=oldPipeline;pipelineCaptured=false;oldPipeline=null;}
            foreach(var pair in cameraStates)if(pair.Key!=null){pair.Key.clearFlags=pair.Value.Item1;pair.Key.farClipPlane=pair.Value.Item2;}cameraStates.Clear();
            if(root!=null){root.SetActive(false);UnityEngine.Object.DestroyImmediate(root);root=null;}
            if(captured)
            {
                RenderSettings.skybox=oldSky;RenderSettings.fog=oldFog;RenderSettings.fogMode=oldFogMode;RenderSettings.fogStartDistance=oldFogStart;RenderSettings.fogEndDistance=oldFogEnd;RenderSettings.fogColor=oldFogColor;
                RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientSkyColor=oldSkyColor;RenderSettings.ambientEquatorColor=oldEquatorColor;RenderSettings.ambientGroundColor=oldGroundColor;captured=false;
                RenderSettings.ambientProbe=oldProbe;
            }
            foreach(var item in owned)if(item!=null)UnityEngine.Object.DestroyImmediate(item);owned.Clear();materials.Clear();
        }
    }
}

