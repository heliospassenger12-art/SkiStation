
#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SkiStation.Visuals;
using SkiStation.ValAlpin;
namespace SkiStation.EditorTools
{
    [InitializeOnLoad]
    public static class ValAlpinNatureInstaller
    {
        const string Output="outputs/ValAlpin-runtime";
        const string Catalog=PureNature2VisualProvider.PlayerFolder;
        static ValAlpinNatureInstaller(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isPlaying||!File.Exists("Temp/install-valalpin-nature.txt"))return;
            File.Delete("Temp/install-valalpin-nature.txt");Directory.CreateDirectory(Output);
            try{Install();File.WriteAllText(Output+"/install.txt","Runtime catalogue created with persistent references. "+DateTime.Now.ToString("O"));}
            catch(Exception ex){File.WriteAllText(Output+"/install-error.txt",ex.ToString());Debug.LogException(ex);}
        }
        [MenuItem("SkiStation/Visuals/ValAlpin/Installer le rendu en jeu")]
        public static void Install()
        {
            foreach(var file in new[]{"BK_VegetationLeaves_Winter.shader","BK_StandardLayer_Winter.shader"})
            {
                string source="Assets/SkiStation/Editor/"+file,target="Assets/SkiStation/Shaders/"+file;
                if(AssetDatabase.LoadAssetAtPath<Shader>(source)!=null)
                {
                    var error=AssetDatabase.MoveAsset(source,target);
                    if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
                }
            }
            using(var provider=new PureNature2VisualProvider())
            {
                provider.Scan();
                provider.Build(PureNature2VisualProvider.LoadConfig());
                Debug.Log(provider.Notice);
                provider.WriteLocalPlayerCatalog();
            }
            var profile=AssetDatabase.LoadAssetAtPath<VisualProfile>(Catalog+"/PureNatureLocal.asset");
            if(profile==null||profile.terrain==null)throw new InvalidOperationException("Catalogue Pure Nature absent.");
            var leaves=Shader.Find("SkiStation/Winter Leaves");var rock=Shader.Find("SkiStation/Winter Rock");var impostor=Shader.Find("SkiStation/Winter Impostor");
            if(leaves==null||rock==null||impostor==null)throw new InvalidOperationException("Shaders hiver absents.");
            var cache=new Dictionary<Material,Material>();
            Func<Material,Material> convert=source=>
            {
                if(source==null)return null;if(cache.TryGetValue(source,out var found))return found;

                if(!ValAlpinNatureMaterials.Supports(source))return source;
                var mat=new Material(source){hideFlags=HideFlags.None};
                ValAlpinNatureMaterials.Configure(mat,source,leaves,rock,impostor);
                string id=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
                var target=Save(mat,Catalog+"/Winter_"+id+".mat");cache[source]=target;return target;
            };
            Action<VisualVariant[]> variants=list=>
            {
                if(list==null)return;
                foreach(var variant in list)foreach(var part in variant.renderParts)
                    for(int i=0;i<part.materials.Length;i++)part.materials[i]=convert(part.materials[i]);
            };
            if(profile.trees!=null){variants(profile.trees.variants);EditorUtility.SetDirty(profile.trees);}
            if(profile.vegetation!=null){variants(profile.vegetation.variants);EditorUtility.SetDirty(profile.vegetation);}
            if(profile.rocks!=null){variants(profile.rocks.variants);EditorUtility.SetDirty(profile.rocks);}
            if(profile.cliffs!=null){variants(profile.cliffs.variants);EditorUtility.SetDirty(profile.cliffs);}
            var clone=UnityEngine.Object.Instantiate(profile);clone.hideFlags=HideFlags.None;clone.profileId="VALALPIN_PURE_NATURE_TEMPLATE";
            var template=Save(clone,Catalog+"/ValAlpinNatureProfile.asset");
            var data=ScriptableObject.CreateInstance<ValAlpinNatureAssets>();data.profile=template;
            var trees=new List<GameObject>();foreach(var family in new[]{"Fir","Spruce","Pine"})for(int i=1;i<=6;i++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PureNature2VisualProvider.Folder+"/Prefabs/Trees/"+family+i+".prefab");
                if(prefab==null)throw new InvalidOperationException("Prefab manquant: "+family+i);trees.Add(prefab);
            }
            data.trees=trees.ToArray();data.impostorShader=impostor;
            data.cliffs=RockPrefabs("Cliff");
            data.boulders=RockPrefabs("Boulder");
            data.stones=RockPrefabs("Stone");
            data.pebbles=RockPrefabs("Pebble");
            data.rockAlbedo=AssetDatabase.LoadAssetAtPath<Texture2D>(PureNature2VisualProvider.Folder+"/Models/Rocks/Textures/_RockDetail_a.png");
            data.rockNormal=AssetDatabase.LoadAssetAtPath<Texture2D>(PureNature2VisualProvider.Folder+"/Models/Rocks/Textures/_RockDetail_n.png");
            data.terrainShader=Shader.Find("SkiStation/ValAlpin Nature Terrain");data.exteriorShader=Shader.Find("SkiStation/ValAlpin Nature Exterior");
            data.leavesShader=leaves;data.rockShader=rock;data.skyShader=Shader.Find("Skybox/Procedural");data.litShader=Shader.Find("Universal Render Pipeline/Lit");
            if(!data.IsReady)throw new InvalidOperationException("Catalogue incomplet.");
            Save(data,Catalog+"/ValAlpinNature.asset");AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("[ValAlpin Pure Nature] Catalogue Player et shaders runtime installés.");
        }
        static GameObject[] RockPrefabs(string family)
        {
            const string folder=PureNature2VisualProvider.Folder+"/Prefabs/Rocks";
            var paths=Array.ConvertAll(AssetDatabase.FindAssets("t:Prefab",new[]{folder}),AssetDatabase.GUIDToAssetPath);
            Array.Sort(paths,StringComparer.Ordinal);
            var prefabs=new List<GameObject>();
            foreach(var path in paths)
            {
                if(!Path.GetFileNameWithoutExtension(path).StartsWith(family,StringComparison.OrdinalIgnoreCase))continue;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab!=null)prefabs.Add(prefab);
            }
            if(prefabs.Count==0)throw new InvalidOperationException("Famille de roches introuvable : "+family);
            return prefabs.ToArray();
        }
        static T Save<T>(T value,string path) where T:UnityEngine.Object
        {
            var target=AssetDatabase.LoadAssetAtPath<T>(path);
            if(target==null){AssetDatabase.CreateAsset(value,path);return value;}
            EditorUtility.CopySerialized(value,target);EditorUtility.SetDirty(target);UnityEngine.Object.DestroyImmediate(value);return target;
        }
    }
}
#endif

