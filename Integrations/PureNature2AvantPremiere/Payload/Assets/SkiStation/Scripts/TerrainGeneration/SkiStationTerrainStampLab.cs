using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkiStation.Visuals;
namespace SkiStation.TerrainGeneration
{
    public sealed class SkiStationTerrainStampLab:MonoBehaviour
    {
        readonly string[] profiles={"SKISTATION_PROCEDURAL","STAMP_TEST","TWO_PEAKS_INTERNAL","THREE_MASSIFS_INTERNAL","GRAND_VALLEY_INTERNAL","GRAND_MASSIF_CANDIDATE_A","GRAND_MASSIF_CANDIDATE_B","GRAND_MASSIF_CANDIDATE_C","IMPORTED_INTERNAL_TEST","SKISTATION_LEGACY_ALPINE","ALTITUDE_SMALL","ALTITUDE_ALPINE","ALTITUDE_GLACIER","ALTITUDE_EXTREME","ALTITUDE_SCENIC"};
        public string initialProfile="";
        bool externalCurrent;
        readonly Queue<Action> commands=new Queue<Action>();TerrainGenerationWorld world;GameObject root;Camera cameraView;Light sun;
        TerrainPotentialReport report;Vector2 scroll;Vector3 target;float distance=5600,yaw=30,pitch=35;int selected=5,seed=6311;bool busy,closeView;
        string lastSavedDomain="";string message="",visual=VisualProfileRegistry.DefaultId;Color oldAmbient;Light oldSun;bool oldFog;UnityEngine.Rendering.AmbientMode oldAmbientMode;
        void Start()
        {
            if(!string.IsNullOrEmpty(initialProfile)){int index=Array.IndexOf(profiles,initialProfile);if(index>=0){selected=index;seed=6312;}}
            oldAmbient=RenderSettings.ambientLight;oldSun=RenderSettings.sun;oldFog=RenderSettings.fog;oldAmbientMode=RenderSettings.ambientMode;RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.43f,.48f,.56f);
            var cameraObject=new GameObject("Caméra TerrainStampLab");cameraObject.transform.SetParent(transform,false);cameraView=cameraObject.AddComponent<Camera>();cameraView.nearClipPlane=.2f;cameraView.farClipPlane=24000;cameraView.backgroundColor=new Color(.42f,.55f,.67f);cameraObject.AddComponent<AudioListener>();
            var lightObject=new GameObject("Soleil TerrainStampLab");lightObject.transform.SetParent(transform,false);sun=lightObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(42,-35,0);RenderSettings.sun=sun;
            StartCoroutine(LoadProfile(false));
        }
        string DomainId=>"lab.terrain."+profiles[selected].ToLowerInvariant()+"."+seed;
        IEnumerator LoadProfile(bool generate,bool saved=false)
        {
            busy=true;message=saved?"Chargement du snapshot sauvegardé…":generate?"Génération C# (à profiler dans Unity)…":"Chargement du bake interne figé…";yield return null;
            if(SkiStation.Arcade.ArcadeSkiSession.ActiveSession!=null){message="Quittez la session Arcade avant de remplacer un terrain de laboratoire.";busy=false;yield break;}
            TerrainHeightfield data=null;string saveId=!string.IsNullOrEmpty(lastSavedDomain)?lastSavedDomain:world!=null?world.domainId:DomainId;
            try
            {
                if(saved){if(!TerrainSnapshotStore.Load(saveId,out data,out var text))throw new InvalidOperationException(text);message=text;}
                else if(generate)
                {var profile=TerrainGenerationRegistry.GetProfile(profiles[selected]);if(profile==null)throw new InvalidOperationException("Profil absent.");var recipe=profile.Read();recipe.seed=seed;data=TerrainGenerationEngine.Generate(recipe);}
                else{var source=Resources.Load<TextAsset>("TerrainGeneration/Frozen/"+profiles[selected]);if(source==null)throw new InvalidOperationException("Bake interne absent.");data=TerrainHeightfieldCodec.Decode(source.bytes);seed=data.recipe.seed;}
            }
            catch(Exception ex){message="Terrain conservé ; chargement refusé : "+ex.Message;data=null;}
            if(data==null){busy=false;yield break;}
            if(saved){seed=data.recipe.seed;int restored=Array.IndexOf(profiles,data.recipe.profileId);if(restored>=0)selected=restored;}
            // Old valid terrain remains until all source/snapshot checks succeed.
            var previousRoot=root;var previousWorld=world;
            if(previousRoot!=null)previousRoot.SetActive(false);yield return null;
            root=new GameObject("Domaine TerrainLab "+profiles[selected]);root.transform.SetParent(transform,false);world=root.AddComponent<TerrainGenerationWorld>();world.domainId=saved?saveId:DomainId;
            if(!world.Install(data)){message=world.LastMessage;root.SetActive(false);Destroy(root);root=previousRoot;world=previousWorld;if(root!=null)root.SetActive(true);busy=false;yield break;}
            if(previousRoot!=null)Destroy(previousRoot);
            externalCurrent=Array.IndexOf(profiles,data.recipe.profileId)<0;
            visual=DomainVisualPreferences.Get(world.domainId);world.SelectVisual(visual,false);report=TerrainPotentialAnalysis.Analyze(data);target=new Vector3(0,world.ClampedHeight(0,0),0);distance=5600;message="Relief chargé. "+data.notice;busy=false;
        }
        // Receives a completed field from an optional Editor provider, never a commercial texture.
        public bool PreviewLocalHeightfield(TerrainHeightfield field)
        {
            if(!Application.isPlaying||busy||commands.Count>0||SkiStation.Arcade.ArcadeSkiSession.ActiveSession!=null)return false;
            var frozen=TerrainHeightfieldCodec.Decode(TerrainHeightfieldCodec.Encode(field));
            Post(()=>StartCoroutine(LoadLocalHeightfield(frozen)));return true;
        }
        IEnumerator LoadLocalHeightfield(TerrainHeightfield data)
        {
            busy=true;
            var previousRoot=root;var previousWorld=world;
            if(previousRoot!=null)previousRoot.SetActive(false);yield return null;
            root=new GameObject("Domaine TerrainLab local "+data.recipe.profileId);root.transform.SetParent(transform,false);
            world=root.AddComponent<TerrainGenerationWorld>();
            string identity=TerrainHeightfieldCodec.Fingerprint(TerrainHeightfieldCodec.Encode(data));
            world.domainId=data.recipe.altitudeData!=null&&!string.IsNullOrEmpty(data.recipe.altitudeData.domainId)?data.recipe.altitudeData.domainId:"lab.local."+data.recipe.profileId.ToLowerInvariant()+"."+identity.Substring(0,12);
            if(!world.Install(data))
            {message=world.LastMessage;root.SetActive(false);Destroy(root);root=previousRoot;world=previousWorld;if(root!=null)root.SetActive(true);busy=false;yield break;}
            if(previousRoot!=null)Destroy(previousRoot);
            world.MarkLocalRecipeOnly();
            externalCurrent=true;seed=data.recipe.seed;lastSavedDomain="";
            string fallback=string.IsNullOrEmpty(data.recipe.visualProfileId)?VisualProfileRegistry.DefaultId:data.recipe.visualProfileId;
            visual=DomainVisualPreferences.Get(world.domainId,fallback);world.SelectVisual(visual,false);report=TerrainPotentialAnalysis.Analyze(data);
            float x=data.recipe.originX+data.recipe.sizeX*.5f,z=data.recipe.originZ+data.recipe.sizeZ*.5f;
            target=new Vector3(x,world.ClampedHeight(x,z),z);distance=Mathf.Max(data.recipe.sizeX,data.recipe.sizeZ)*.7f;
            message="Relief local chargé — données non redistribuées. "+data.notice;busy=false;
        }
        void Update()
        {
            if(!busy&&commands.Count>0)commands.Dequeue()();
            if(cameraView==null||SkiStation.Arcade.ArcadeSkiSession.ActiveSession!=null)return;
            if(Input.GetMouseButton(2)){yaw+=Input.GetAxis("Mouse X")*3;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*3,5,85);}
            distance=Mathf.Clamp(distance*(1-Input.mouseScrollDelta.y*.1f),4,14000);
            target+=Quaternion.Euler(0,yaw,0)*new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"))*Mathf.Max(3,distance*.3f)*Time.unscaledDeltaTime;
            Quaternion angle=Quaternion.Euler(pitch,yaw,0);Vector3 position=target+Vector3.up*(closeView?2:15)-angle*Vector3.forward*distance;
            if(world!=null&&world.IsReady&&world.Contains(position.x,position.z))position.y=Mathf.Max(position.y,world.ClampedHeight(position.x,position.z)+2);
            cameraView.transform.SetPositionAndRotation(position,angle);
        }
        void Post(Action action){if(!busy)commands.Enqueue(action);}
        void OnGUI()
        {
            var session=SkiStation.Arcade.ArcadeSkiSession.ActiveSession;
            if(session!=null)
            {if(GUI.Button(new Rect(10,10,220,30),"Quitter le test Arcade → laboratoire"))Post(()=>session.ExitWorld());return;}
            GUILayout.BeginArea(new Rect(10,10,Mathf.Min(450,Screen.width-20),Mathf.Min(720,Screen.height-20)),GUI.skin.box);scroll=GUILayout.BeginScrollView(scroll);
            GUILayout.Label("SKISTATION — TERRAIN / ALTITUDE LAB");GUILayout.Label(externalCurrent?"Relief issu d'une bibliothèque locale — sources hors export.":"Stamps internes SkiStation; les bibliothèques externes restent optionnelles.");
            GUILayout.Label("WASD / flèches : déplacement ; molette : zoom ; bouton central : rotation.");GUILayout.Label(message);
            GUI.enabled=!busy;
            for(int i=0;i<profiles.Length;i++){int value=i;if(GUILayout.Button(profiles[i]))Post(()=>{selected=value;StartCoroutine(LoadProfile(profiles[value]=="SKISTATION_LEGACY_ALPINE"));});}
            GUILayout.Label("Graine : "+seed);
            if(!externalCurrent)
            {
                if(GUILayout.Button("Graine suivante"))Post(()=>seed++);
                if(GUILayout.Button("Générer ce profil interne"))Post(()=>StartCoroutine(LoadProfile(true)));
            }
            else GUILayout.Label("Pour changer de graine, utilisez l’outil Terrain qui a chargé ce relief.");
            if(world!=null&&world.LocalRecipeOnly)GUILayout.Label("Source locale : recette/référence seules; aucun snapshot commercial dérivé enregistré.");
            else if(GUILayout.Button("Sauvegarder le relief courant (pas les constructions)"))Post(()=>{if(world!=null&&world.IsReady){if(world.Save())lastSavedDomain=world.domainId;message=world.LastMessage;}});
            if(GUILayout.Button("Recharger le relief sauvegardé"))Post(()=>StartCoroutine(LoadProfile(false,true)));
            if(GUILayout.Button("Style : SkiStation Default"))Post(()=>{visual=VisualProfileRegistry.DefaultId;world?.SelectVisual(visual,true);});
            if(GUILayout.Button("Style : Pure Nature (fallback Default si indisponible)"))Post(()=>{visual=VisualProfileRegistry.PureNatureId;world?.SelectVisual(visual,true);});
            if(GUILayout.Button(closeView?"Vue Gestion":"Vue proche du sol (caméra seule)"))Post(()=>{closeView=!closeView;distance=closeView?15:5600;pitch=closeView?8:35;});
            if(GUILayout.Button("Front de neige potentiel"))Post(()=>{if(world?.FrontDeNeige!=null){target=world.FrontDeNeige.position;distance=700;}});
            if(report!=null)
            {
                GUILayout.Label("Potentiel local : "+report.peaks.Length+" sommets détectés ; zones basses constructibles "+report.lowBuildablePercent.ToString("0.0")+" % ; pente locale skiable "+report.localSkiablePercent.ToString("0.0")+" %.");
                GUILayout.Label(report.suspectedContinuousRidge?"Crête continue suspecte : à revoir.":"Aucune crête continue suspecte sur les transects testés (pas une preuve topologique complète).");
                for(int i=0;i<report.peaks.Length;i++){int index=i;if(GUILayout.Button("Observer sommet "+(i+1)))Post(()=>{target=report.peaks[index];distance=closeView?15:1300;});}
            }
            if(world!=null&&world.IsReady)
            {
                var altitude=world.GetComponent<SkiStation.Altitude.DomainAltitudeRuntime>();
                var a=altitude?.Data;
                if(a!=null)
                {
                    GUILayout.Label("Altitude ici : "+a.reference.ToMeters(world.ClampedHeight(target.x,target.z)).ToString("0")+" m · construction "+a.reference.constructionMinAltitude.ToString("0")+"–"+a.reference.constructionMaxAltitude.ToString("0")+" m");
                    GUILayout.Label("Glaciers / zones explicites : "+a.zones.Length+" · lignes candidates : "+a.lines.Length);
                    if(GUILayout.Button(a.reference.summer?"Passer en hiver":"Tester l'été (secteurs saisonniers sans neige)"))Post(()=>altitude.SetSeason(!a.reference.summer,a.reference.summer));
                    if(GUILayout.Button("Évaluer les limites 4030 / 4070 m"))Post(()=>{bool low=SkiStation.Altitude.AltitudeConstructionPolicy.AllowsAltitude(4030,a.reference,out _);bool high=SkiStation.Altitude.AltitudeConstructionPolicy.AllowsAltitude(4070,a.reference,out _);message="4030 m : "+(low?"compatible altitude":"hors limite domaine")+" ; 4070 m : "+(high?"ERREUR":"refus absolu")+". Les autres règles restent nécessaires.";});
                    foreach(var line in a.lines)
                    {var chosen=line;if(GUILayout.Button("Valider / observer "+line.lineId+" ("+a.reference.ToMeters(line.heli.point.y).ToString("0")+" m)"))Post(()=>{target=chosen.heli.point;distance=700;message=SkiStation.Altitude.ExtremeRideValidator.Validate(altitude,chosen,out var reason)?reason:"Ligne refusée : "+reason;});}
                    if(GUILayout.Button("Créer scénario laboratoire : funiculaire + piste glacier"))Post(()=>{var fixture=world.GetComponent<SkiStation.Altitude.AltitudeLabInfrastructure>()??world.gameObject.AddComponent<SkiStation.Altitude.AltitudeLabInfrastructure>();message=fixture.BuildFixture();});
                    if(GUILayout.Button("Test Arcade natif — choisir rider / départ (Sandbox)"))Post(()=>
                    {
                        SkiStation.Arcade.GameModeContext262.Select(SkiStation.Arcade.GameMode262.Sandbox);
                        var context=world.GetComponent<SkiStation.Arcade.ArcadeDomainContext>();
                        // Dedicated isolated lab: native standalone path, no invented Management clock.
                        context.managementSource=null;
                        var entry=world.GetComponent<SkiStation.Arcade.ArcadeDomainEntry>()??world.gameObject.AddComponent<SkiStation.Arcade.ArcadeDomainEntry>();
                        entry.enterOnStart=false;entry.domain=context;entry.domainId=world.domainId;entry.managementCamera=cameraView;
                        entry.Enter();message="Test Sandbox : le vrai rider utilise ce TerrainCollider. Pas de station complète ni visiteurs inventés.";
                    });
                }
            }
            GUILayout.BeginHorizontal();for(int i=0;i<8;i++){int angle=i*45;if(GUILayout.Button(angle+"°"))Post(()=>yaw=angle);}GUILayout.EndHorizontal();
            if(GUILayout.Button("Comparer queries Gestion / collider ici"))Post(()=>{bool found=ManagementSurface.TryGet(target,out var point);message=found?"ManagementSurface : "+point+" ; même TerrainCollider que le domaine Arcade.":"Aucun terrain trouvé ici.";});
            GUILayout.BeginHorizontal();for(int i=0;i<4;i++){int tier=i;if(GUILayout.Button(new[]{"LOW","MED","HIGH","ULTRA"}[i]))Post(()=>world?.ApplyQuality((SkiStation.Arcade.ArcadeGraphics257)tier));}GUILayout.EndHorizontal();
            GUI.enabled=true;GUILayout.Label("Aucun test Unity effectué lors de la livraison. Cette caméra n'est pas une session Arcade. Les pistes/Lift/routes restent à construire avec les vrais systèmes.");GUILayout.EndScrollView();GUILayout.EndArea();
        }
        void OnDestroy(){RenderSettings.ambientLight=oldAmbient;RenderSettings.sun=oldSun;RenderSettings.fog=oldFog;RenderSettings.ambientMode=oldAmbientMode;}
    }
}

