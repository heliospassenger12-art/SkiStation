#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using SkiStation.Visuals;
using SkiStation.ValAlpin;
namespace SkiStation.EditorTools
{
    [Serializable] public sealed class PureNatureLocalBinding
    {public string guid,signature,family="UNKNOWN";public bool reviewed;}
    [Serializable] public sealed class PureNatureLocalConfig
    {public int schema=1;public List<PureNatureLocalBinding> bindings=new List<PureNatureLocalBinding>();}
    public sealed class PureNatureLocalRecord
    {public string name,path,guid,type,proposed,signature;public UnityEngine.Object asset;public string details;}
    // Only this Editor adapter knows the commercial installation path.
    public sealed class PureNature2VisualProvider:IDisposable
    {
        public const string Folder="Assets/BK/PureNature_Mountains";
        public const string LocalFolder="Library/SkiStation/LocalPureNature2";
        public const string PlayerFolder="Assets/LocalVisualBindings/Resources/VisualProfiles";
        public readonly List<PureNatureLocalRecord> records=new List<PureNatureLocalRecord>();
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();public VisualProfile Profile {get;private set;}
        public bool Present=>AssetDatabase.IsValidFolder(Folder);public string Notice {get;private set;}
        public void Scan()
        {
            records.Clear();if(!Present){Notice="Pure Nature absent : SKISTATION_DEFAULT.";return;}
            var guids=AssetDatabase.FindAssets("",new[]{Folder});Array.Sort(guids,StringComparer.Ordinal);
            if(guids.Length>10000)throw new InvalidOperationException("Budget scan dépassé; aucun asset modifié.");
            foreach(var guid in guids)
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);if(AssetDatabase.IsValidFolder(path))continue;var asset=AssetDatabase.LoadMainAssetAtPath(path);if(asset==null)continue;
                records.Add(new PureNatureLocalRecord{name=asset.name,path=path,guid=guid,asset=asset,type=asset.GetType().Name,proposed=Proposed(asset),signature=AssetDatabase.GetAssetDependencyHash(path).ToString(),details=Describe(asset)});
            }
            Notice=records.Count+" assets réellement trouvés; classifications proposées, mappings non confirmés par défaut.";
        }
        static string Proposed(UnityEngine.Object asset)
        {
            if(asset is TerrainLayer)return "TERRAIN_SURFACE";
            string name=asset.name.ToLowerInvariant();
            if(name.Contains("mountain")||name.Contains("background"))return "BACKGROUND_MOUNTAIN";
            if(name.Contains("cliff"))return "CLIFF";if(name.Contains("boulder"))return "BOULDER";if(name.Contains("rock"))return "ROCK";
            if(name.Contains("tree")||name.Contains("pine")||name.Contains("fir")||name.Contains("spruce"))return "TREE";
            if(name.Contains("grass")||name.Contains("bush"))return "VEGETATION";
            if(name.Contains("ground")||name.Contains("snow"))return "GROUND_DETAIL";return "UNKNOWN";
        }
        static string Describe(UnityEngine.Object asset)
        {
            var text=new StringBuilder();
            if(asset is Texture2D texture)text.Append("Texture2D "+texture.width+"×"+texture.height+" format="+texture.graphicsFormat+" sRGB="+texture.isDataSRGB+" filter="+texture.filterMode+" wrap="+texture.wrapMode);
            if(asset is Material material)text.Append("material="+material.name+" shader="+(material.shader!=null?material.shader.name:"MISSING")+" pipelineTag="+material.GetTag("RenderPipeline",false,"")+" usableHere="+VisualSafety.Usable(material,VisualPipeline.Any));
            if(asset is TerrainLayer layer)text.Append("albedo="+(layer.diffuseTexture!=null?AssetDatabase.GetAssetPath(layer.diffuseTexture):"MISSING")+" normal="+(layer.normalMapTexture!=null?AssetDatabase.GetAssetPath(layer.normalMapTexture):"MISSING"));
            if(asset is GameObject prefab)
            {
                text.Append(" renderers="+prefab.GetComponentsInChildren<Renderer>(true).Length+" colliders="+prefab.GetComponentsInChildren<Collider>(true).Length+" LODGroups="+prefab.GetComponentsInChildren<LODGroup>(true).Length);
                foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {var filter=renderer.GetComponent<MeshFilter>();text.Append(" | renderer="+renderer.GetType().Name+" mesh="+(filter!=null&&filter.sharedMesh!=null?filter.sharedMesh.name:"UNKNOWN/SKINNED"));foreach(var m in renderer.sharedMaterials)text.Append(" material="+(m!=null?m.name:"MISSING")+" shader="+(m!=null&&m.shader!=null?m.shader.name:"MISSING"));}
            }
            if(asset is Mesh mesh)text.Append(" mesh="+mesh.name+" vertices="+mesh.vertexCount+" subMeshes="+mesh.subMeshCount);return text.Length==0?"Information non applicable/non lue.":text.ToString();
        }
        public static PureNatureLocalConfig LoadConfig()
        {
            string path=Path.Combine(LocalFolder,"bindings.json");if(!File.Exists(path))return new PureNatureLocalConfig();
            var cfg=JsonUtility.FromJson<PureNatureLocalConfig>(File.ReadAllText(path));if(cfg==null||cfg.schema!=1||cfg.bindings==null||cfg.bindings.Count>256)throw new InvalidOperationException("Configuration locale inconnue : conserver le fichier.");return cfg;
        }
        public static void SaveConfig(PureNatureLocalConfig cfg)
        {if(cfg.schema!=1||cfg.bindings.Count>256)throw new InvalidOperationException("Budget mappings invalide.");Directory.CreateDirectory(LocalFolder);File.WriteAllText(Path.Combine(LocalFolder,"bindings.json"),JsonUtility.ToJson(cfg,true));}
        T Own<T>() where T:ScriptableObject {var value=ScriptableObject.CreateInstance<T>();value.hideFlags=HideFlags.HideAndDontSave;owned.Add(value);return value;}
        public VisualProfile Build(PureNatureLocalConfig cfg)
        {
            DisposeProfile();if(!Present){Notice="Pure Nature absent : fallback Default.";return null;}
            var profile=Own<VisualProfile>();profile.profileId=VisualProfileRegistry.PureNatureId;profile.inheritNativeRendering=false;
            var terrain=Own<TerrainVisualSet>();terrain.pipeline=VisualPipelineFallback.Current;
            var rock=Own<RockVisualSet>();rock.pipeline=terrain.pipeline;var cliff=Own<CliffVisualSet>();cliff.pipeline=terrain.pipeline;var tree=Own<TreeVisualSet>();tree.pipeline=terrain.pipeline;
            var vegetation=Own<VegetationVisualSet>();vegetation.pipeline=terrain.pipeline;
            var snow=Own<SnowVisualSet>();snow.pipeline=terrain.pipeline;
            var layers=new List<TerrainVisualLayer>();var rocks=new List<VisualVariant>();var cliffs=new List<VisualVariant>();var trees=new List<VisualVariant>();var sources=new List<UnityEngine.Object>();var warnings=new List<string>();
            var understory=new List<VisualVariant>();
            foreach(var binding in EffectiveBindings(cfg))
            {
                if(!binding.reviewed)continue;var record=records.Find(a=>a.guid==binding.guid);
                if(record==null||record.signature!=binding.signature){warnings.Add("Source absente/modifiée : revue requise "+binding.guid);continue;}
                try
                {
                    if(record.asset is TerrainLayer layer&&TerrainFamily(binding.family))layers.Add(new TerrainVisualLayer{category=Semantic(binding.family),terrainLayer=layer});
                    else if(record.asset is Texture2D texture&&TerrainFamily(BaseFamily(binding.family)))BindTexture(layers,binding.family,texture);
                    else if(record.asset is GameObject prefab&&(binding.family=="TREE"||binding.family=="ROCK"||binding.family=="CLIFF"||binding.family=="VEGETATION"))
                    {
                        string category=binding.family=="ROCK"?RockCategory(prefab.name):binding.family;
                        var variant=Extract(prefab,category);
                        if(binding.family=="TREE")trees.Add(variant);
                        else if(binding.family=="CLIFF")cliffs.Add(variant);
                        else if(binding.family=="VEGETATION")understory.Add(variant);
                        else rocks.Add(variant);
                    }
                    else if(record.asset is Material material&&VisualSafety.Usable(material,terrain.pipeline))
                    {switch(binding.family){case "SNOW_FRESH":snow.fresh=material;break;case "SNOW_GROOMED":snow.groomed=material;break;case "SNOW_WINDPACKED":snow.windpacked=material;break;case "ROCK_OVERLAY":snow.rockOverlay=material;break;case "CLIFF_OVERLAY":snow.cliffOverlay=material;break;default:continue;}}
                    else{warnings.Add("Type/famille/shader non compatible : "+record.path);continue;}
                    sources.Add(record.asset);
                }
                catch(Exception ex){warnings.Add(record.path+" : "+ex.Message);}
            }
            terrain.useNaturalSnowLayers=BindMountainSurfaces(layers,sources);
            if(terrain.useNaturalSnowLayers)
            {
                const string snowAssets="Assets/SkiStation/Resources/ArcadeA257/";
                if(snow.fresh==null)snow.fresh=AssetDatabase.LoadAssetAtPath<Material>(snowAssets+"SnowFresh.mat");
                if(snow.groomed==null)snow.groomed=AssetDatabase.LoadAssetAtPath<Material>(snowAssets+"SnowGroomed.mat");
                if(snow.windpacked==null)snow.windpacked=AssetDatabase.LoadAssetAtPath<Material>(snowAssets+"SnowWindpacked.mat");
            }
            terrain.layers=layers.ToArray();terrain.terrainMaterial=VisualPipelineFallback.CreateTerrain();if(terrain.terrainMaterial!=null){terrain.terrainMaterial.hideFlags=HideFlags.HideAndDontSave;owned.Add(terrain.terrainMaterial);}
            rock.variants=rocks.ToArray();cliff.variants=cliffs.ToArray();tree.variants=trees.ToArray();profile.terrain=layers.Count>0?terrain:null;profile.rocks=rocks.Count>0?rock:null;profile.cliffs=cliffs.Count>0?cliff:null;profile.trees=trees.Count>0?tree:null;profile.snow=snow;profile.sourceAssets=sources.ToArray();
            vegetation.variants=understory.ToArray();profile.vegetation=understory.Count>0?vegetation:null;
            Profile=profile;VisualProfileRegistry.RegisterLocalPureNature(profile);Notice=sources.Count+" mappings confirmés. Familles absentes : SkiStation natif. "+string.Join("; ",warnings);return profile;
        }
        IEnumerable<PureNatureLocalBinding> EffectiveBindings(PureNatureLocalConfig cfg)
        {
            var manual=new HashSet<string>(StringComparer.Ordinal);
            foreach(var binding in cfg.bindings)
            {
                if(binding==null)continue;
                manual.Add(binding.guid);
                yield return binding;
            }
            foreach(var record in records)
            {
                if(manual.Contains(record.guid))continue;
                string family=InstalledPrefabFamily(record);
                if(family!=null)yield return new PureNatureLocalBinding
                    {guid=record.guid,signature=record.signature,family=family,reviewed=true};
            }
        }
        static string InstalledPrefabFamily(PureNatureLocalRecord record)
        {
            if(!(record.asset is GameObject)||
                !record.path.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase))return null;
            if(record.path.StartsWith(Folder+"/Prefabs/Plants/",StringComparison.Ordinal))return "VEGETATION";
            if(record.path.StartsWith(Folder+"/Prefabs/Trees/",StringComparison.Ordinal))
            {
                string name=Path.GetFileNameWithoutExtension(record.path);
                if(name.StartsWith("Bush",StringComparison.Ordinal))return "VEGETATION";
                if(name.StartsWith("Fir",StringComparison.Ordinal)||name.StartsWith("Pine",StringComparison.Ordinal)||
                    name.StartsWith("Spruce",StringComparison.Ordinal))return "TREE";
            }
            if(!record.path.StartsWith(Folder+"/Prefabs/Rocks/",StringComparison.Ordinal))return null;
            if(record.name.StartsWith("Cliff",StringComparison.OrdinalIgnoreCase))return "CLIFF";
            return RockCategory(record.name)=="ROCK"?null:"ROCK";
        }
        static string RockCategory(string name)
        {
            if(name.StartsWith("Boulder",StringComparison.OrdinalIgnoreCase))return "BOULDER";
            if(name.StartsWith("Stone",StringComparison.OrdinalIgnoreCase))return "STONE";
            if(name.StartsWith("Pebble",StringComparison.OrdinalIgnoreCase))return "PEBBLE";
            return "ROCK";
        }
        static string BaseFamily(string family){if(family.EndsWith("_NORMAL"))return family.Substring(0,family.Length-7);if(family.EndsWith("_MASK"))return family.Substring(0,family.Length-5);return family;}
        static bool BindMountainSurfaces(List<TerrainVisualLayer> layers,List<UnityEngine.Object> sources)
        {
            const string surfaces=Folder+"/Textures/Surfaces/Layers/";
            var mappings=new (string category,string file)[]
            {
                ("ALPINE_GROUND","Grass01"),("SCREE","Gravel1"),("SCREE_COARSE","Gravel2"),
                ("SOIL","Mud01"),("SOIL_DAMP","Mud02")
            };
            bool found=false;
            foreach(var (category,file) in mappings)
            {
                var terrainLayer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(surfaces+file+".terrainlayer");
                if(terrainLayer==null||terrainLayer.diffuseTexture==null)continue;
                layers.RemoveAll(layer=>layer.category==category);
                layers.Add(new TerrainVisualLayer{category=category,terrainLayer=terrainLayer});
                if(!sources.Contains(terrainLayer))sources.Add(terrainLayer);
                found=true;
            }
            const string rocks=Folder+"/Models/Rocks/Textures/";
            BindRockSurface("ROCK","_RockDetail",28);
            BindRockSurface("CLIFF","Cliff4",36);
            return found;

            void BindRockSurface(string category,string file,float tiling)
            {
                var diffuse=AssetDatabase.LoadAssetAtPath<Texture2D>(rocks+file+"_a.png");
                if(diffuse==null||layers.Exists(layer=>layer.category==category))return;
                var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(rocks+file+"_n.png");
                var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(rocks+file+"_mask.png");
                layers.Add(new TerrainVisualLayer{category=category,albedo=diffuse,normal=normal,mask=mask,
                    tiling=Vector2.one*tiling,smoothness=.1f});
                sources.Add(diffuse);
                if(normal!=null)sources.Add(normal);
                if(mask!=null)sources.Add(mask);
            }
        }
        static void BindTexture(List<TerrainVisualLayer> layers,string family,Texture2D texture)
        {string category=Semantic(BaseFamily(family));var layer=layers.Find(l=>l.category==category);if(layer==null){layer=new TerrainVisualLayer{category=category};layers.Add(layer);}if(family.EndsWith("_NORMAL"))layer.normal=texture;else if(family.EndsWith("_MASK"))layer.mask=texture;else layer.albedo=texture;}
        static string Semantic(string family)=>family=="ROCK_SURFACE"?"ROCK":family=="CLIFF_SURFACE"?"CLIFF":family=="VEGETATION_SURFACE"?"VEGETATION":family;
        static bool TerrainFamily(string family)=>family=="SNOW"||family=="GLACIER"||family=="ROCK_SURFACE"||family=="CLIFF_SURFACE"||family=="GROUND"||family=="ALPINE_GROUND"||family=="SCREE"||family=="VEGETATION_SURFACE";
        static VisualVariant Extract(GameObject prefab,string family)
        {
            if(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length>0)throw new InvalidOperationException("Modèle skinned non pris en charge par cet adaptateur statique.");
            var renderers=prefab.GetComponentsInChildren<MeshRenderer>(true);if(renderers.Length==0||renderers.Length>16)throw new InvalidOperationException("Renderer statique requis, maximum 16 parties. SkinnedMeshRenderer non pris en charge.");
            var parts=new List<VisualMeshPart>();Bounds bounds=new Bounds();bool hasBounds=false;var groups=prefab.GetComponentsInChildren<LODGroup>(true);
            foreach(var renderer in renderers)
            {
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null||filter.sharedMesh.vertexCount>80000||renderer.sharedMaterials.Length!=filter.sharedMesh.subMeshCount)throw new InvalidOperationException("Mesh/matériaux incomplets.");
                foreach(var material in renderer.sharedMaterials)
                    if(!VisualSafety.Usable(material,VisualPipeline.Any)&&!ValAlpinNatureMaterials.Supports(material))
                        throw new InvalidOperationException("Shader/pipeline non compatible ; aucune conversion automatique.");
                Matrix4x4 matrix=prefab.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix;int level=0;
                foreach(var group in groups){var lods=group.GetLODs();for(int i=0;i<lods.Length;i++)foreach(var r in lods[i].renderers)if(r==renderer)level=Mathf.Min(i,2);}
                var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++){var p=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(!hasBounds){bounds=new Bounds(p,Vector3.zero);hasBounds=true;}else bounds.Encapsulate(p);}
                parts.Add(new VisualMeshPart{mesh=filter.sharedMesh,materials=renderer.sharedMaterials,position=matrix.GetColumn(3),rotation=matrix.rotation,scale=matrix.lossyScale,lod=level});
            }
            if(bounds.size.y<=.01f||Mathf.Max(bounds.size.x,bounds.size.z)<=.01f)throw new InvalidOperationException("Bounds inutilisables.");
            return new VisualVariant{category=family,sourcePrefab=prefab,renderBounds=bounds,renderParts=parts.ToArray(),lodReviewed=true,colliderReviewed=true,review="Local user-confirmed visual fit. Original scripts/colliders not instantiated; native SkiStation proxies unchanged."};
        }
        [MenuItem("SkiStation/Visuals/Audit Pure Nature 2")]
        public static void Audit()
        {
            using(var provider=new PureNature2VisualProvider())
            {
                provider.Scan();var text=new StringBuilder("# Inventaire Pure Nature 2 local\n\nScan exécuté sur cette machine. Pipeline actif : "+VisualPipelineFallback.Current+".\n\n");
                if(!provider.Present)text.Append("Package absent : aucun asset inventorié ; fallback SKISTATION_DEFAULT.\n");
                text.Append("| Nom | Chemin | GUID | Type Unity | Catégorie proposée | Matériau/shader/mesh/LOD/collider/renderer lus |\n|---|---|---|---|---|---|\n");
                foreach(var row in provider.records)text.Append("| "+Clean(row.name)+" | "+Clean(row.path)+" | "+row.guid+" | "+row.type+" | "+row.proposed+" | "+Clean(row.details)+" |\n");
                text.Append("\n## Matériaux SkiStation : audit non destructif\n");
                foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/SkiStation"}))
                {var path=AssetDatabase.GUIDToAssetPath(guid);var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)text.Append("- "+Clean(path)+" : "+Clean(Describe(m))+"\n");}
                File.WriteAllText("PURE_NATURE_2_LOCAL_INVENTORY.md",text.ToString());Debug.Log(provider.Notice+" Inventaire écrit sans modifier les assets.");
            }
        }
        static string Clean(string s)=>(s??"UNKNOWN").Replace("|","/").Replace("\n"," ").Replace("\r","");
        void DisposeProfile(){if(Profile!=null)VisualProfileRegistry.RemoveLocalPureNature(Profile);Profile=null;foreach(var value in owned)if(value!=null)UnityEngine.Object.DestroyImmediate(value);owned.Clear();}
        public void Dispose(){DisposeProfile();}
        public void WriteLocalPlayerCatalog()
        {
            if(Profile==null||Profile.Availability==VisualAvailability.MISSING)throw new InvalidOperationException("Profil local confirmé et exploitable requis.");
            // Only own binding assets are written here. All commercial objects remain references, never copies.
            Directory.CreateDirectory(PlayerFolder);AssetDatabase.Refresh();
            var terrain=Persist(Profile.terrain,"Terrain");var rocks=Persist(Profile.rocks,"Rocks");var cliffs=Persist(Profile.cliffs,"Cliffs");var trees=Persist(Profile.trees,"Trees");var snow=Persist(Profile.snow,"Snow");
            var vegetation=Persist(Profile.vegetation,"Vegetation");
            if(terrain!=null&&Profile.terrain.terrainMaterial!=null){var m=UnityEngine.Object.Instantiate(Profile.terrain.terrainMaterial);m.hideFlags=HideFlags.None;var target=AssetDatabase.LoadAssetAtPath<Material>(PlayerFolder+"/Terrain.mat");if(target==null){AssetDatabase.CreateAsset(m,PlayerFolder+"/Terrain.mat");target=m;}else{EditorUtility.CopySerialized(m,target);UnityEngine.Object.DestroyImmediate(m);}terrain.terrainMaterial=target;EditorUtility.SetDirty(terrain);}
            var clone=UnityEngine.Object.Instantiate(Profile);clone.hideFlags=HideFlags.None;clone.terrain=terrain;clone.rocks=rocks;clone.cliffs=cliffs;clone.trees=trees;clone.snow=snow;
            clone.vegetation=vegetation;
            string path=PlayerFolder+"/PureNatureLocal.asset";var previous=AssetDatabase.LoadAssetAtPath<VisualProfile>(path);if(previous==null)AssetDatabase.CreateAsset(clone,path);else{EditorUtility.CopySerialized(clone,previous);EditorUtility.SetDirty(previous);UnityEngine.Object.DestroyImmediate(clone);}AssetDatabase.SaveAssets();
            Notice="Catalogue Player local créé : références + matériaux SkiStation uniquement. Ce dossier est exclu de Git/ZIP partagé.";
        }
        T Persist<T>(T source,string name) where T:ScriptableObject
        {if(source==null)return null;var clone=UnityEngine.Object.Instantiate(source);clone.hideFlags=HideFlags.None;string path=PlayerFolder+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<T>(path);if(existing==null){AssetDatabase.CreateAsset(clone,path);return clone;}EditorUtility.CopySerialized(clone,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(clone);return existing;}
    }
}
#endif

