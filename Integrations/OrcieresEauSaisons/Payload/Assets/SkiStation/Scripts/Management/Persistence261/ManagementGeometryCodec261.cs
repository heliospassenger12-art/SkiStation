using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SkiStation.Arcade;
namespace SkiStation
{
    // Lossless supported static mesh subset. Unsupported geometry is refused, never silently flattened.
    public sealed class ManagementGeometryCodec261 : IDisposable
    {
        readonly HashSet<Transform> elementRoots;
        readonly ManagementSaveData261 data;
        readonly ManagementSaveAssetCatalogue261 catalogue;
        readonly bool traffic391;
        readonly Dictionary<Mesh,int> meshIds=new Dictionary<Mesh,int>();
        readonly Dictionary<Material,int> materialIds=new Dictionary<Material,int>();
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        Mesh[] meshes;Material[] materials;readonly HashSet<string> checkedTags=new HashSet<string>(StringComparer.Ordinal);
        public ManagementGeometryCodec261(ManagementSaveData261 d,ManagementSaveAssetCatalogue261 assets,HashSet<Transform> roots=null,bool traffic391=false){data=d;catalogue=assets;elementRoots=roots;this.traffic391=traffic391;}
        const string AlpineRockKey="alpine261.rock-material";
        string Key(UnityEngine.Object value)
        {
            if(data.schema==ManagementSaveData261.AlpineSchema && value is Material material
                && material==AlpineRockMaterial.Shared) return AlpineRockKey;
            return value==null?"":value==Texture2D.whiteTexture?"builtin.white":value==Texture2D.blackTexture?"builtin.black":value==Texture2D.grayTexture?"builtin.gray":catalogue!=null?catalogue.Key(value):"";
        }
        string RequiredKey(UnityEngine.Object value)
        {if(value==null)return "";string key=Key(value);if(string.IsNullOrEmpty(key))throw new InvalidOperationException("Asset non référencé dans le catalogue Gestion : "+value.name);return key;}
        public static Type MarkerType(string kind)
        {switch(kind){case "water-crossing":return typeof(TerrainGeneration.WaterCrossing);case "school-instructor394":return typeof(SkiInstructorRuntime);case "entry-terrain":return typeof(ArcadeEntryTerrain2612);case "sector":return typeof(ArcadeSector);case "rail":return typeof(ArcadeRail);case "wall":return typeof(ArcadeWallrideSurface);case "snow":return typeof(ArcadeSurfaceVisual257);
            case "slope-condition":return typeof(SlopeConditionRuntime);case "slope-conformity389":return typeof(SlopeTerrainConformityRuntime);case "slope-visibility389":return typeof(SlopePlayableVisibilityRuntime);case "slope-sign389":return typeof(SlopeSignRuntime);case "slope-safety389":return typeof(SlopeSafetySignageRuntime);case "slope-endpoint389":return typeof(SlopeEndpointAutoConnector);default:return null;}}
        static string MarkerKind(Component c)
        {return c is SkiInstructorRuntime instructor394&&instructor394.School!=null&&instructor394.School.OwnsInstructor394(instructor394.transform)?"school-instructor394":c is ArcadeEntryTerrain2612?"entry-terrain":c is ArcadeSector?"sector":c is ArcadeRail?"rail":c is ArcadeWallrideSurface?"wall":c is ArcadeSurfaceVisual257?"snow":
            c is TerrainGeneration.WaterCrossing?"water-crossing":c is SlopeConditionRuntime?"slope-condition":c is SlopeTerrainConformityRuntime?"slope-conformity389":c is SlopePlayableVisibilityRuntime?"slope-visibility389":c is SlopeSignRuntime?"slope-sign389":c is SlopeSafetySignageRuntime?"slope-safety389":c is SlopeEndpointAutoConnector?"slope-endpoint389":null;}
        public ManagementGeometryNode261 Capture(Transform t,bool world=false)
        {
            if(t.GetComponent<Terrain>()!=null||t.GetComponent<TerrainCollider>()!=null||t.GetComponent<LODGroup>()!=null||t.GetComponent<SkinnedMeshRenderer>()!=null||t.GetComponent<LineRenderer>()!=null||t.GetComponent<Rigidbody>()!=null||t.GetComponent<TextMesh>()!=null)
                throw new NotSupportedException("Géométrie dynamique / Terrain / LOD non prise en charge par ce checkpoint : "+t.name);
            var n=new ManagementGeometryNode261{title=t.name,tag=t.tag,layer=t.gameObject.layer,position=world?t.position:t.localPosition,rotation=world?t.rotation:t.localRotation,scale=world?t.lossyScale:t.localScale,active=t.gameObject.activeSelf,
                colliders=new List<ManagementCollider261>(),markers=new List<ManagementMarker261>(),children=new List<ManagementGeometryNode261>()};
            if(world&&t.parent!=null)
            {
                var m=t.localToWorldMatrix;var trs=Matrix4x4.TRS(t.position,t.rotation,t.lossyScale);
                for(int i=0;i<16;i++)if(Mathf.Abs(m[i]-trs[i])>.001f)throw new NotSupportedException("Transform cisaillé : "+t.name);
            }
            var filter=t.GetComponent<MeshFilter>();if(filter!=null&&filter.sharedMesh!=null)n.mesh=CaptureMesh(filter.sharedMesh);
            var renderer=t.GetComponent<MeshRenderer>();if(renderer!=null)
            {
                if(renderer.HasPropertyBlock())throw new NotSupportedException("PropertyBlock visuel non sérialisable : "+t.name);
                n.renderer=true;n.rendererEnabled=renderer.enabled;n.receiveShadows=renderer.receiveShadows;n.shadowCasting=(int)renderer.shadowCastingMode;
                var values=renderer.sharedMaterials;n.materials=new int[values.Length];for(int i=0;i<values.Length;i++)n.materials[i]=CaptureMaterial(values[i]);
            }
            foreach(var collider in t.GetComponents<Collider>())
            {
                var c=new ManagementCollider261{enabled=collider.enabled,trigger=collider.isTrigger,physicsMaterial=RequiredKey(collider.sharedMaterial)};
                if(collider is BoxCollider box){c.kind="box";c.center=box.center;c.size=box.size;}
                else if(collider is SphereCollider sphere){c.kind="sphere";c.center=sphere.center;c.radius=sphere.radius;}
                else if(collider is CapsuleCollider capsule){c.kind="capsule";c.center=capsule.center;c.radius=capsule.radius;c.height=capsule.height;c.direction=capsule.direction;}
                else if(collider is MeshCollider mesh){c.kind="mesh";c.convex=mesh.convex;c.mesh=mesh.sharedMesh!=null?CaptureMesh(mesh.sharedMesh):-1;}
                else throw new NotSupportedException("Collider non supporté : "+collider.GetType().Name);
                n.colliders.Add(c);
            }
            var alpineBeacon=data.schema==ManagementSaveData261.AlpineSchema?t.GetComponent<StationStatusBeacon348>():null;
            foreach(var c in t.GetComponents<MonoBehaviour>())
            {
                if(c==null)throw new NotSupportedException("Script manquant : "+t.name);string kind=MarkerKind(c);
                if(alpineBeacon!=null&&c==alpineBeacon)continue; // The native scan rebuilds this status display.
                if(kind!=null)n.markers.Add(new ManagementMarker261{kind=kind,json=JsonUtility.ToJson(c)});
                else if(!(c is ManagementElementIdentity261||c is SlopeRuntime||c is SlopeMeshGenerator||c is SlopeMaintenanceRuntime||c is SlopeConnectionRuntime||c is SlopeLiftConnectorRuntime||c is StationServiceRuntime||c is StationAccommodation249||c is StationEquipmentShop249||c is StationStructureStatus249||c is SkiSchoolRuntime||(traffic391&&(TrafficStateCodec391.Supports(c)||TrafficStateCodec391.DerivedVisual(c)))))
                    throw new NotSupportedException("Composant matériel non restaurable : "+c.GetType().Name+" / "+t.name);
            }
            var safety=t.GetComponent<SlopeSafetySignageRuntime>();
            // Native safety signs rebuild from the restored piste. Do not serialize a duplicate of that owned geometry.
            for(int i=0;i<t.childCount;i++){var child=t.GetChild(i);if((safety!=null&&safety.OwnsGeometry389(child))||(alpineBeacon!=null&&alpineBeacon.OwnsCheckpointVisual(child)))continue;if(elementRoots==null||!elementRoots.Contains(child))n.children.Add(Capture(child));}
            return n;
        }
        int CaptureMesh(Mesh mesh)
        {
            if(meshIds.TryGetValue(mesh,out int index))return index;
            var value=new ManagementMesh261{asset=Key(mesh)};
            if(string.IsNullOrEmpty(value.asset))
            {
                if(!mesh.isReadable)throw new NotSupportedException("Mesh non lisible sans référence catalogue : "+mesh.name);
                value.uvChannels=new List<ManagementUVChannel261>();for(int channel=0;channel<8;channel++){var extra=new List<Vector4>();mesh.GetUVs(channel,extra);if(extra.Count>0)value.uvChannels.Add(new ManagementUVChannel261{channel=channel,values=extra});}
                value.vertices=mesh.vertices;value.normals=mesh.normals;value.uv=mesh.uv;value.tangents=mesh.tangents;value.colors=mesh.colors;value.submeshes=new List<ManagementTriangles261>();
                for(int i=0;i<mesh.subMeshCount;i++){if(mesh.GetTopology(i)!=MeshTopology.Triangles)throw new NotSupportedException("Mesh non triangulaire : "+mesh.name);value.submeshes.Add(new ManagementTriangles261{indices=mesh.GetTriangles(i)});}
            }
            index=data.meshes.Count;data.meshes.Add(value);meshIds.Add(mesh,index);return index;
        }
        int CaptureMaterial(Material material)
        {
            if(material==null)throw new NotSupportedException("Matériau manquant");
            if(materialIds.TryGetValue(material,out int index))return index;
            var value=new ManagementMaterial261{asset=Key(material)};
            if(string.IsNullOrEmpty(value.asset))
            {
                if(material.shader==null)throw new NotSupportedException("Shader manquant");
                value.shader=material.shader.name;value.renderQueue=material.renderQueue;value.instancing=material.enableInstancing;value.keywords=material.shaderKeywords;value.properties=new List<ManagementShaderValue261>();
                for(int i=0;i<material.shader.GetPropertyCount();i++)
                {
                    string name=material.shader.GetPropertyName(i);var p=new ManagementShaderValue261{name=name};
                    switch(material.shader.GetPropertyType(i))
                    {
                        case ShaderPropertyType.Color:p.kind="color";p.vector=material.GetColor(name);break;
                        case ShaderPropertyType.Vector:p.kind="vector";p.vector=material.GetVector(name);break;
                        case ShaderPropertyType.Float:case ShaderPropertyType.Range:p.kind="float";p.number=material.GetFloat(name);break;
                        case ShaderPropertyType.Int:p.kind="int";p.integer=material.GetInteger(name);break;
                        case ShaderPropertyType.Texture:p.kind="texture";p.asset=RequiredKey(material.GetTexture(name));p.scale=material.GetTextureScale(name);p.offset=material.GetTextureOffset(name);break;
                        default:throw new NotSupportedException("Propriété shader non supportée : "+name);
                    }
                    value.properties.Add(p);
                }
            }
            index=data.materials.Count;data.materials.Add(value);materialIds.Add(material,index);return index;
        }
        T Asset<T>(string key) where T:UnityEngine.Object
        {
            if(key==AlpineRockKey)
            {
                if(data.schema!=ManagementSaveData261.AlpineSchema || !(AlpineRockMaterial.Shared is T material))
                    throw new InvalidOperationException("Matériau alpin natif indisponible.");
                return material;
            }
            var builtin=ManagementSaveAssetCatalogue261.Builtin(key);if(builtin is T typed)return typed;if(catalogue==null)throw new InvalidOperationException("Catalogue d'assets Gestion manquant");return catalogue.Resolve<T>(key);
        }
        // Allocate/resolve everything before replacing any live Management geometry.
        public void Prepare()
        {
            ManagementSaveValidation261.Validate(data);meshes=new Mesh[data.meshes.Count];materials=new Material[data.materials.Count];
            for(int i=0;i<meshes.Length;i++)
            {
                var s=data.meshes[i];if(!string.IsNullOrEmpty(s.asset)){meshes[i]=Asset<Mesh>(s.asset);continue;}
                var mesh=new Mesh{name="Gestion restored mesh",indexFormat=s.vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};owned.Add(mesh);
                mesh.vertices=s.vertices;mesh.subMeshCount=s.submeshes.Count;for(int j=0;j<s.submeshes.Count;j++)mesh.SetTriangles(s.submeshes[j].indices,j);
                if(s.normals!=null&&s.normals.Length>0)mesh.normals=s.normals;else mesh.RecalculateNormals();
                if(s.uv!=null&&s.uv.Length>0)mesh.uv=s.uv;if(s.tangents!=null&&s.tangents.Length>0)mesh.tangents=s.tangents;if(s.colors!=null&&s.colors.Length>0)mesh.colors=s.colors;if(s.uvChannels!=null)foreach(var channel in s.uvChannels)mesh.SetUVs(channel.channel,channel.values);mesh.RecalculateBounds();meshes[i]=mesh;
            }
            for(int i=0;i<materials.Length;i++)
            {
                var s=data.materials[i];if(!string.IsNullOrEmpty(s.asset)){materials[i]=Asset<Material>(s.asset);continue;}
                var shader=Shader.Find(s.shader);if(shader==null)throw new InvalidOperationException("Shader Gestion introuvable : "+s.shader);
                var mat=new Material(shader);owned.Add(mat);mat.renderQueue=s.renderQueue;mat.enableInstancing=s.instancing;mat.shaderKeywords=s.keywords??new string[0];
                foreach(var p in s.properties)
                {
                    if(!mat.HasProperty(p.name))throw new InvalidOperationException("Propriété shader disparue : "+p.name);
                    switch(p.kind){case "float":mat.SetFloat(p.name,p.number);break;case "int":mat.SetInteger(p.name,p.integer);break;case "color":mat.SetColor(p.name,p.vector);break;case "vector":mat.SetVector(p.name,p.vector);break;
                    case "texture":mat.SetTexture(p.name,string.IsNullOrEmpty(p.asset)?null:Asset<Texture>(p.asset));mat.SetTextureScale(p.name,p.scale);mat.SetTextureOffset(p.name,p.offset);break;}
                }
                materials[i]=mat;
            }
            foreach(var n in data.scenery)CheckNode(n);foreach(var b in data.buildings)CheckNode(b.visual);foreach(var slope in data.slopes)CheckNode(slope.visual);foreach(var c in data.connectors)CheckNode(c.visual);
            if(data.traffic391!=null)foreach(var a in data.traffic391.actors)CheckNode(a.visual);
        }
        void CheckNode(ManagementGeometryNode261 n)
        {if(!string.IsNullOrEmpty(n.tag)&&checkedTags.Add(n.tag)){var probe=new GameObject("Tag probe");try{probe.tag=n.tag;}finally{Release(probe);}}foreach(var c in n.colliders)if(!string.IsNullOrEmpty(c.physicsMaterial))Asset<PhysicsMaterial>(c.physicsMaterial);foreach(var child in n.children)CheckNode(child);}
        public GameObject Restore(ManagementGeometryNode261 n,Transform parent)
        {
            var go=new GameObject(n.title);go.SetActive(false);go.transform.SetParent(parent,false);go.transform.localPosition=n.position;go.transform.localRotation=n.rotation;go.transform.localScale=n.scale;go.layer=n.layer;
            if(!string.IsNullOrEmpty(n.tag))go.tag=n.tag;
            if(n.mesh>=0)go.AddComponent<MeshFilter>().sharedMesh=meshes[n.mesh];
            if(n.renderer){var r=go.AddComponent<MeshRenderer>();var values=new Material[n.materials!=null?n.materials.Length:0];for(int i=0;i<values.Length;i++)values[i]=materials[n.materials[i]];r.sharedMaterials=values;r.enabled=n.rendererEnabled;r.receiveShadows=n.receiveShadows;r.shadowCastingMode=(ShadowCastingMode)n.shadowCasting;}
            foreach(var c in n.colliders)
            {
                Collider collider;
                switch(c.kind)
                {case "box":var box=go.AddComponent<BoxCollider>();box.center=c.center;box.size=c.size;collider=box;break;
                case "sphere":var sphere=go.AddComponent<SphereCollider>();sphere.center=c.center;sphere.radius=c.radius;collider=sphere;break;
                case "capsule":var capsule=go.AddComponent<CapsuleCollider>();capsule.center=c.center;capsule.radius=c.radius;capsule.height=c.height;capsule.direction=c.direction;collider=capsule;break;
                default:var mesh=go.AddComponent<MeshCollider>();mesh.convex=c.convex;mesh.sharedMesh=c.mesh>=0?meshes[c.mesh]:null;collider=mesh;break;}
                collider.enabled=c.enabled;collider.isTrigger=c.trigger;if(!string.IsNullOrEmpty(c.physicsMaterial))collider.sharedMaterial=Asset<PhysicsMaterial>(c.physicsMaterial);
            }
            foreach(var m in n.markers)JsonUtility.FromJsonOverwrite(m.json,go.AddComponent(MarkerType(m.kind)));
            foreach(var child in n.children)Restore(child,go.transform);go.SetActive(n.active);return go;
        }
        public static void Release(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        public void Dispose(){foreach(var obj in owned)Release(obj);owned.Clear();}
    }
}
