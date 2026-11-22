using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SkiStation.TerrainGeneration;
namespace SkiStation.Visuals
{
    // Collider roots are native logical anchors. Only their renderer children are replaceable/cullable.
    public sealed partial class TerrainGeneratedScenery:MonoBehaviour
    {
        sealed class Instance {public TerrainSceneryAnchor anchor;public GameObject root,skin;public Renderer[] renderers;}
        TerrainGenerationWorld world;readonly List<Instance> instances=new List<Instance>();readonly HashSet<Transform> roots=new HashSet<Transform>();readonly List<Material> owned=new List<Material>();
        string requested=VisualProfileRegistry.DefaultId;int revision=-1,tier=2;float refresh;Mesh treeMesh,rockMesh,snowCapMesh;
        static readonly float[] distances={600,1000,1600,2400},densities={.35f,.6f,.85f,1};
        public void Connect(TerrainGenerationWorld owner,bool checkpoint=false)
        {
            if(world!=null)throw new InvalidOperationException("Décor déjà lié");checkpointSuspended=checkpoint;
            world=owner;world.Recipe.scenery.Validate();treeMesh=Cone();rockMesh=Rock();snowCapMesh=RockCap(rockMesh);
            foreach(var a in world.Recipe.scenery.anchors)
            {
                if(a.removed)continue;var root=new GameObject((a.kind==TerrainSceneryKind.TREE?"Tree_":"Rock_")+a.id);root.transform.SetParent(transform,false);root.transform.SetPositionAndRotation(a.position,a.rotation);
                if(a.kind==TerrainSceneryKind.TREE){var c=root.AddComponent<CapsuleCollider>();c.radius=.25f;c.height=a.height*.75f;c.center=Vector3.up*c.height*.5f;}
                else{var c=root.AddComponent<BoxCollider>();c.size=new Vector3(a.radius*1.5f,a.height*.8f,a.radius*1.5f);c.center=Vector3.up*a.height*.4f;}
                roots.Add(root.transform);instances.Add(new Instance{anchor=a,root=root});
            }
            if(!checkpoint)SubscribeCheckpointEvents();
            Select(!string.IsNullOrEmpty(world.Recipe.visualProfileId)?world.Recipe.visualProfileId:DomainVisualPreferences.Get(world.domainId));
            if(!checkpoint)foreach(var lift in LiftRuntime.Active)LiftReady(lift);
        }
        public bool Owns(Transform value){for(var p=value;p!=null;p=p.parent)if(roots.Contains(p))return true;return false;}
        public void RefreshClimate(){RebuildSkins();}
        public void Select(string id){requested=id;RebuildSkins();}
        public void SetQuality(int quality){tier=Mathf.Clamp(quality,0,3);refresh=0;}
        void Update()
        {
            if(checkpointSuspended||world==null||!world.IsReady)return;if(revision!=VisualProfileRegistry.Revision)RebuildSkins();
            if(Time.unscaledTime<refresh)return;refresh=Time.unscaledTime+.25f;
            var camera=Camera.main;if(camera==null)camera=FindFirstObjectByType<Camera>();if(camera==null)return;
            foreach(var instance in instances)if(instance.root!=null&&!instance.anchor.removed)
            {
                float distance=Vector3.Distance(camera.transform.position,instance.anchor.position);
                // Always render near colliders; reduced distant density never creates invisible nearby obstacles.
                bool visible=distance<180||distance<distances[tier]&&instance.anchor.rank<=densities[tier];
                foreach(var renderer in instance.renderers??Array.Empty<Renderer>())if(renderer!=null){renderer.enabled=visible;renderer.shadowCastingMode=tier==0?ShadowCastingMode.Off:ShadowCastingMode.On;}
            }
        }
        void RebuildSkins()
        {
            if(world==null)return;revision=VisualProfileRegistry.Revision;var profile=VisualProfileRegistry.ResolveProfileOrFallback(requested);
            foreach(var m in owned)if(m!=null)Destroy(m);owned.Clear();
            var tree=VisualPipelineFallback.CreateMesh(new Color(.07f,.2f,.12f));var rock=VisualPipelineFallback.CreateMesh(new Color(.28f,.27f,.25f));if(tree!=null)owned.Add(tree);if(rock!=null)owned.Add(rock);
            var nativeSnow=VisualPipelineFallback.CreateMesh(new Color(.92f,.95f,.98f));if(nativeSnow!=null)owned.Add(nativeSnow);
            int rendererBudget=4096,remaining=instances.Count;
            foreach(var instance in instances)
            {
                remaining--;if(instance.root==null||instance.anchor.removed)continue;
                if(instance.skin!=null){instance.skin.SetActive(false);Destroy(instance.skin);}
                instance.skin=new GameObject("SkiStation visual only");instance.skin.transform.SetParent(instance.root.transform,false);
                var variants=instance.anchor.kind==TerrainSceneryKind.TREE?profile.trees?.variants:instance.anchor.kind==TerrainSceneryKind.CLIFF?profile.cliffs?.variants:profile.rocks?.variants;
                VisualVariant variant=null;
                bool fitRock = instance.anchor.kind != TerrainSceneryKind.TREE &&
                    (profile.profileId == VisualProfileRegistry.PureNatureId || profile.profileId == ValAlpin.ValAlpinNatureRendering.ProfileId);
                if(!profile.inheritNativeRendering&&variants!=null&&variants.Length>0)
                {
                    string category = fitRock && instance.anchor.kind == TerrainSceneryKind.ROCK
                        ? instance.anchor.height <= .8f ? "PEBBLE" : instance.anchor.height < 2f ? "STONE" : "BOULDER"
                        : null;
                    variant = ChooseVariant(variants, instance.anchor, rendererBudget-remaining*2-1, fitRock, category);
                    if (variant == null && category != null)
                        variant = ChooseVariant(variants, instance.anchor, rendererBudget-remaining*2-1, fitRock, null);
                }
                if(variant!=null)
                {
                    var parts=variant.renderParts;
                    var bounds=variant.renderBounds;
                    float diameter=instance.anchor.radius*2f;
                    float uniform=Mathf.Min(instance.anchor.height/bounds.size.y,diameter/Mathf.Max(bounds.size.x,bounds.size.z));
                    var scale=fitRock ? new Vector3(diameter/bounds.size.x,instance.anchor.height/bounds.size.y,diameter/bounds.size.z) : Vector3.one*uniform;
                    instance.skin.transform.localScale=scale;
                    var pivot=fitRock?new Vector3(bounds.center.x,bounds.min.y,bounds.center.z):Vector3.up*bounds.min.y;
                    instance.skin.transform.localPosition=-Vector3.Scale(pivot,scale);
                    var lods=new List<Renderer>[] {new List<Renderer>(),new List<Renderer>(),new List<Renderer>()};
                    foreach(var part in parts){var go=new GameObject("Mesh render part");go.transform.SetParent(instance.skin.transform,false);go.transform.localPosition=part.position;go.transform.localRotation=part.rotation;go.transform.localScale=part.scale;
                        go.AddComponent<MeshFilter>().sharedMesh=part.mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=part.materials;lods[Mathf.Clamp(part.lod,0,2)].Add(renderer);rendererBudget--;}
                    var levels=new List<LOD>();for(int i=0;i<3;i++)if(lods[i].Count>0)levels.Add(new LOD(i==0?.2f:i==1?.07f:.015f,lods[i].ToArray()));
                    if(levels.Count>1){var group=instance.skin.AddComponent<LODGroup>();group.SetLODs(levels.ToArray());group.RecalculateBounds();}
                }
                else
                {
                    var material=instance.anchor.kind==TerrainSceneryKind.TREE?tree:rock;
                    if(material!=null){instance.skin.AddComponent<MeshFilter>().sharedMesh=instance.anchor.kind==TerrainSceneryKind.TREE?treeMesh:rockMesh;
                        instance.skin.AddComponent<MeshRenderer>().sharedMaterial=material;instance.skin.transform.localScale=new Vector3(instance.anchor.radius,instance.anchor.height,instance.anchor.radius);rendererBudget--;}
                }
                // Native cap is only applied to the owned native rock shape; never a fictitious cap on an unknown external mesh.
                if(variant==null&&instance.anchor.kind!=TerrainSceneryKind.TREE&&TerrainVisualBridge.Sample(world,instance.anchor.position).snowCoverage>.3f)
                {var overlay=profile.snow!=null&&VisualSafety.Usable(profile.snow.rockOverlay,profile.snow.pipeline)?profile.snow.rockOverlay:nativeSnow;
                 if(overlay!=null){var cap=new GameObject("SkiStation upward snow cap");cap.transform.SetParent(instance.skin.transform,false);cap.transform.localPosition=Vector3.up*.004f;cap.AddComponent<MeshFilter>().sharedMesh=snowCapMesh;cap.AddComponent<MeshRenderer>().sharedMaterial=overlay;rendererBudget--;}}
                instance.renderers=instance.skin.GetComponentsInChildren<Renderer>();
            }
            refresh=0;
        }
        static VisualVariant ChooseVariant(VisualVariant[] variants,TerrainSceneryAnchor anchor,int budget,bool fitRock,string category)
        {
            int start=Mathf.FloorToInt(anchor.rank*variants.Length)%variants.Length;
            for(int i=0;i<variants.Length;i++)
            {
                var candidate=variants[(start+i)%variants.Length];
                if(candidate!=null&&(category==null||candidate.category==category)&&Usable(candidate,anchor,budget,fitRock))
                    return candidate;
            }
            return null;
        }
        static bool Usable(VisualVariant variant,TerrainSceneryAnchor anchor,int budget,bool fitRock)
        {
            if(variant==null||!variant.lodReviewed||!variant.colliderReviewed||variant.renderParts==null||variant.renderParts.Length==0||variant.renderParts.Length>16||variant.renderParts.Length>budget||variant.renderBounds.size.y<=.01f)return false;
            float scale=Mathf.Min(anchor.height/variant.renderBounds.size.y,anchor.radius*2/Mathf.Max(variant.renderBounds.size.x,variant.renderBounds.size.z));
            if(fitRock)
            {
                if(variant.renderBounds.size.x<=.01f||variant.renderBounds.size.z<=.01f)return false;
            }
            else if(scale*variant.renderBounds.size.y<anchor.height*.8f||
                anchor.kind!=TerrainSceneryKind.TREE&&(variant.renderBounds.size.x*scale<anchor.radius*1.3f||variant.renderBounds.size.z*scale<anchor.radius*1.3f))return false;
            foreach(var part in variant.renderParts){if(part==null||part.mesh==null||part.materials==null||part.materials.Length!=part.mesh.subMeshCount)return false;foreach(var material in part.materials)if(!VisualSafety.Usable(material,VisualPipeline.Any))return false;}return true;
        }
        void LiftReady(LiftRuntime lift)
        {if(checkpointSuspended||world==null||lift==null||lift.gameObject.scene!=gameObject.scene||lift.Route==null)return;ClearCommitted(new[]{lift.Bottom,lift.Top},18);ClearCommitted(lift.Route,5);}
        void ClearCommitted(IReadOnlyList<Vector3> route,float radius)
        {
            if(checkpointSuspended||world==null||gameObject.scene!=UnityEngine.SceneManagement.SceneManager.GetActiveScene()||route==null)return;
            foreach(var instance in instances)
            {if(instance.root==null||instance.anchor.removed)continue;bool inside=false;for(int i=0;i<route.Count;i++)if(ConstructionRules349.SegmentDistanceXZ(instance.anchor.position,route[i],route[Mathf.Min(i+1,route.Count-1)])<=radius+instance.anchor.radius)inside=true;
             if(!inside)continue;instance.anchor.removed=true;instance.root.SetActive(false);Destroy(instance.root);}
        }
        static Mesh Cone()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int layer=0;layer<3;layer++){float bottom=.12f+layer*.23f,top=.65f+layer*.17f,radius=1-layer*.27f;int offset=vertices.Count;vertices.Add(new Vector3(0,top,0));for(int i=0;i<8;i++){float a=i*Mathf.PI/4;vertices.Add(new Vector3(Mathf.Cos(a)*radius,bottom,Mathf.Sin(a)*radius));}for(int i=0;i<8;i++){triangles.Add(offset);triangles.Add(offset+1+(i+1)%8);triangles.Add(offset+1+i);}}
            // Native trunk is represented by a thin rectangular prism; collider remains the owned simple capsule.
            int at=vertices.Count;foreach(var p in new[]{new Vector3(-.08f,0,-.08f),new Vector3(.08f,0,-.08f),new Vector3(.08f,.5f,-.08f),new Vector3(-.08f,.5f,-.08f),new Vector3(-.08f,0,.08f),new Vector3(.08f,0,.08f),new Vector3(.08f,.5f,.08f),new Vector3(-.08f,.5f,.08f)})vertices.Add(p);
            foreach(int i in new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5})triangles.Add(at+i);
            var mesh=new Mesh{name="SkiStation native conifer"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh Rock()
        {var mesh=new Mesh{name="SkiStation native boulder"};mesh.vertices=new[]{new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(1,0,1),new Vector3(-1,0,1),new Vector3(-.6f,.8f,-.7f),new Vector3(.7f,1,-.4f),new Vector3(.5f,.7f,.8f),new Vector3(-.8f,.65f,.5f)};mesh.triangles=new[]{0,4,5,0,5,1,1,5,6,1,6,2,2,6,7,2,7,3,3,7,4,3,4,0,4,7,6,4,6,5};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        static Mesh RockCap(Mesh source){var mesh=new Mesh{name="SkiStation upward snow only"};mesh.vertices=source.vertices;mesh.triangles=new[]{4,7,6,4,6,5};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        void OnDestroy(){UnsubscribeCheckpointEvents();foreach(var m in owned)if(m!=null)Destroy(m);if(treeMesh!=null)Destroy(treeMesh);if(rockMesh!=null)Destroy(rockMesh);if(snowCapMesh!=null)Destroy(snowCapMesh);}
    }
}

