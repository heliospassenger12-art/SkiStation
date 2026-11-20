using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SkiStation;
using SkiStation.Arcade;
using SkiStation.TerrainGeneration;

[InitializeOnLoad]
public static class OrcieresWaterCheck
{
    const string Key="OrcieresWaterCheck.";
    const string Domain="lab.gestion.orcieres";
    const BindingFlags Any=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static;
    static OrcieresWaterCheck()=>EditorApplication.update+=Tick;
    public static void Run()
    {
        PlayerSettings.productName="SkiStation_OrcieresWater_Smoke";
        SessionState.SetBool(Key+"Run",true);
        SessionState.SetBool(Key+"Legacy",false);
        Stage(0);
        Restart();
    }

    public static void RunLegacy()
    {
        PlayerSettings.productName="SkiStation_OrcieresWaterLegacy_Smoke";
        SessionState.SetBool(Key+"Run",true);
        SessionState.SetBool(Key+"Legacy",true);
        Stage(6);
        Restart();
    }

    static void Stage(int stage,float delay=0)
    {
        SessionState.SetInt(Key+"Stage",stage);
        SessionState.SetFloat(Key+"At",(float)EditorApplication.timeSinceStartup+delay);
    }

    static void Restart()
    {
        SessionState.SetFloat(Key+"Deadline",(float)EditorApplication.timeSinceStartup+145);
        EditorSceneManager.OpenScene("Assets/Scenes/SkiStation_Bootstrap.unity");
        EditorApplication.EnterPlaymode();
    }

    static object Call(object value,string fn,params object[] args)=>value.GetType().GetMethod(fn,Any).Invoke(value,args);
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static void Tick()
    {
        if(!SessionState.GetBool(Key+"Run",false))return;
        try
        {
            int stage=SessionState.GetInt(Key+"Stage",-1);
            double now=EditorApplication.timeSinceStartup;
            if(now>SessionState.GetFloat(Key+"Deadline",0))throw new Exception("Délai étape "+stage+" scène "+SceneManager.GetActiveScene().name);
            if(now<SessionState.GetFloat(Key+"At",0))return;
            if(!EditorApplication.isPlaying)
            {
                if(stage==5&&!EditorApplication.isPlayingOrWillChangePlaymode){Stage(6);Restart();}
                return;
            }
            var menu=SkiStationStartMenu249.Owner263;
            if(menu==null)return;
            if(stage==0)
            {
                if(!SkiStationStartMenu249.HomeOpen263||SceneManager.GetActiveScene().name!="SkiStation_Bootstrap")return;
                Action<SkiStationStartMenu249> launch=loaded=>
                {
                    var page=loaded.GetType().GetField("alpinePreview",Any).GetValue(loaded);
                    Call(page,"CreateSession");
                };
                Call(menu,"LoadValAlpin263",launch,Domain);
                Stage(1);
                return;
            }
            if(stage==6)
            {
                if(!SkiStationStartMenu249.HomeOpen263||SceneManager.GetActiveScene().name!="SkiStation_Bootstrap")return;
                var saved=DemoFlow263.ValidReprises(out var notice).Find(r=>r.domainId==Domain);
                Need(saved!=null,"Reprise absente "+notice);
                Call(menu,"ContinueChoiceNow263",saved);
                Stage(7);
                return;
            }
            var world=TerrainGenerationWorld.Current;
            if(world==null||world.domainId!=Domain||ArrivalHub250.I==null||!ArrivalHub250.I.Ready)return;
            if(stage==1)
            {
                var page=menu.GetType().GetField("alpinePreview",Any).GetValue(menu);
                if(page.GetType().GetField("session",Any).GetValue(page)==null)return;
                Call(menu,"LaunchAlpineStation263");
                Need(!SkiStationStartMenu249.HomeOpen263,"Lancement refusé");
                Stage(2,.9f);
                return;
            }
            var hydro=world.GetComponents<Component>().FirstOrDefault(c=>c.GetType().Name=="RealTerrainWater");
            if(hydro==null)return;
            var dive=MountainActivityRuntime.All.Find(a=>a!=null&&a.definition!=null&&a.definition.type==MountainActivityType.IceDiving);
            var beach=MountainActivityRuntime.All.Find(a=>a!=null&&a.definition!=null&&a.definition.type==MountainActivityType.LakeSwimming);
            var lake=new Vector3(1689,world.ClampedHeight(1689,1337),1337);
            if(stage==2)
            {
                Need(dive!=null&&beach!=null,"Activités absentes");
                Need(!State(hydro,"Lakes").Equals("Open"),"Lac d’hiver ouvert");
                Need(dive.TryEnter()&&!beach.TryEnter(),"Disponibilité hivernale erronée");
                Need(!SkiStation.Altitude.AltitudeConstructionPolicy.ValidateFootprint(lake,new Vector2(8,10),out var refusal)&&refusal.Contains("d’eau"),
                    "Construction sur glace acceptée : "+refusal);
                Need(!RouteAllowed(hydro,lake+Vector3.right*-280,lake+Vector3.right*280,8,true),"Piste sur lac acceptée");
                int snowDay=0;
                for(int day=2;day<=42;day++)
                {
                    GameClock.I.RestoreCheckpoint261(day,11);
                    Call(hydro,"Refresh");
                    if(State(hydro,"Lakes")=="Snow"){snowDay=day;break;}
                }
                Need(snowDay>0,"Aucun lac enneigé en hiver");
                Need(!SkiStation.Altitude.AltitudeConstructionPolicy.ValidateFootprint(lake,new Vector2(8,10),out _),"Lac enneigé constructible");
                Debug.Log("WATER_PASS glace, lac enneigé, plongée saisonnière et emprise protégée jour "+snowDay);
                Snap(world,lake,"estaris-hiver.png");
                GameClock.I.RestoreCheckpoint261(55,11);
                Call(hydro,"Refresh");
                Need(!beach.TryEnter()&&!dive.TryEnter(),"Activité estivale ou hivernale ouverte au printemps");
                GameClock.I.RestoreCheckpoint261(145,11);
                Call(hydro,"Refresh");
                Need(!beach.TryEnter()&&!dive.TryEnter(),"Activité estivale ou hivernale ouverte en automne");
                Debug.Log("WATER_PASS activités fermées au printemps et en automne");
                GameClock.I.RestoreCheckpoint261(95,11);
                Stage(3,.9f);
                return;
            }
            if(stage==3)
            {
                Need(State(hydro,"Lakes")=="Open"&&State(hydro,"Streams")=="Open","Eau d’été gelée");
                Need(SeasonWeatherRuntime363.I.Today.temperatureC>=13f,"Météo d’hiver pendant l’été");
                Need(beach.TryEnter()&&!dive.TryEnter(),"Disponibilité estivale erronée");
                Need(!SkiStation.Altitude.AltitudeConstructionPolicy.ValidateFootprint(lake,new Vector2(8,10),out _),"Lac d’été constructible");
                Debug.Log("WATER_PASS eau libre et baignade en été, plongée fermée, lac protégé");
                Snap(world,lake,"estaris-ete.png");
                var a=BuildBridge(world,hydro,false,null);
                var b=BuildBridge(world,hydro,true,a.Item1);
                var mid=(a.Item1+a.Item2)*.5f;
                Need(ManagementSurface.TryGet(mid,out var ground)&&ManagementSurface.TryTrail(mid,out var riding)&&riding.y>ground.y+.5f,
                    "Le tracé ne rejoint pas le vrai tablier");
                var bikeMid=(b.Item1+b.Item2)*.5f;
                Need(ManagementSurface.TryGet(bikeMid,out ground)&&ManagementSurface.TryTrail(bikeMid,out riding)&&Mathf.Abs(ground.y-riding.y)<.1f,
                    "Une passerelle VTT est utilisable comme pont de ski");
                Need(!RouteAllowed(hydro,b.Item1,b.Item2,3f,true),"Piste autorisée sur la passerelle VTT");
                Need(!RouteAllowed(hydro,a.Item1,a.Item2,8f,false),"Route au sol autorisée à travers l’eau");
                object[] duplicate={hydro,a.Item1,a.Item2,false,null,null,null};
                var plan=typeof(WaterCrossing).GetMethod("TryPlan",Any);
                Need(!(bool)plan.Invoke(null,duplicate)&&((string)duplicate[6]).Contains("déjà"),
                    "Un deuxième pont occupe le même passage");
                Debug.Log("WATER_PASS route au sol refusée, doublon bloqué et séparation des passages ski/VTT");
                var mode=Find<SlopeConstructionMode>();
                mode.SetActive(true);
                mode.SetWidth(8f);
                Call(mode,"AddPoint",a.Item2);
                Call(mode,"AddPoint",a.Item1);
                mode.Finish();
                Need(mode.CanConfirm349(out var why),"Confirmation native de la piste refusée "+why);
                mode.ConfirmPendingSlope();
                var slope=Find<SlopeRuntime>();
                Need(slope!=null,"Construction native de la piste échouée");
                Need(slope.GetComponent<MeshCollider>()!=null,"Piste non matérialisée");
                Debug.Log("WATER_PASS pont de piste, passerelle VTT et piste sur tablier");
                Stage(4,.6f);
                return;
            }
            if(stage==4)
            {
                Need(DemoFlow263.SaveManagement(),"Enregistrement refusé "+DemoFlow263.Notice);
                Debug.Log("WATER_PASS enregistrement des deux ponts et de la piste");
                Stage(5);
                EditorApplication.ExitPlaymode();
                return;
            }
            if(stage==7&&!SkiStationStartMenu249.HomeOpen263)
            {
                if(SessionState.GetBool(Key+"Legacy",false))
                {
                    Need(Find<SlopeRuntime>()!=null&&Find<LiftRuntime>()!=null,"Anciennes constructions non restaurées");
                    Need(GameClock.I!=null&&Find<WaterCrossing>()==null,"Ancien fichier modifié pendant la reprise");
                    Need(dive!=null&&beach!=null,"Nouvelles activités absentes de l’ancienne partie");
                    Debug.Log("WATER_PASS reprise de l’ancienne sauvegarde avec piste et téléski");
                    Finish(0);
                    return;
                }
                var crossings=UnityEngine.Object.FindObjectsByType<WaterCrossing>(FindObjectsSortMode.None);
                Need(crossings.Length==2&&Find<SlopeRuntime>()!=null,"Infrastructure absente à la reprise");
                var ski=crossings.First(x=>x.name.Contains("piste"));
                var tab=Enumerable.Range(0,ski.transform.childCount).Select(i=>ski.transform.GetChild(i)).First(x=>x.name=="Tablier").position;
                Need(ManagementSurface.TryGet(tab,out var hill)&&ManagementSurface.TryTrail(tab,out var deck)&&deck.y>hill.y+.5f,"Tablier restauré muet");
                Need(GameClock.I.CurrentDay==95&&beach.AvailableCapacity>0&&dive.AvailableCapacity==0,"Été non restauré");
                Debug.Log("WATER_PASS reprise à froid de l’été, des ponts et de la piste");
                Finish(0);
            }
        }
        catch(Exception e){Debug.LogError("WATER_FAIL "+e);Finish(1);}
    }

    static string State(Component hydro,string name)=>hydro.GetType().GetProperty(name,Any).GetValue(hydro).ToString();

    static bool RouteAllowed(Component hydro,Vector3 a,Vector3 b,float width,bool trail)
    {
        return (bool)hydro.GetType().GetMethod("AllowsRoute",Any).Invoke(null,new object[]{new[]{a,b},width,null,trail});
    }

    [Serializable] sealed class Map { public Surface[] streams; }
    [Serializable] sealed class Surface { public Vector2[] vertices; public int[] triangles; }

    static (Vector3,Vector3) BuildBridge(TerrainGenerationWorld world,Component hydro,bool bike,Vector3? other)
    {
        var resource=Resources.Load<TextAsset>("TerrainGeneration/Nature/Orcieres");
        var stream=JsonUtility.FromJson<Map>(resource.text).streams;
        var plan=typeof(WaterCrossing).GetMethod("TryPlan",Any);
        var build=typeof(WaterCrossing).GetMethod("Build",Any);
        int checkedCount=0;
        foreach(var surface in stream)
        foreach(int j in surface.triangles.Where((v,k)=>k%18==0))
        {
            if(j+3>=surface.vertices.Length)continue;
            var v=surface.vertices;
            var center2=(v[j]+v[j+1]+v[j+2]+v[j+3])*.25f;
            var center=new Vector3(center2.x,world.ClampedHeight(center2.x,center2.y),center2.y);
            if(other.HasValue&&Vector3.Distance(other.Value,center)<100)continue;
            if(!world.Contains(center.x,center.z)||Vector3.Distance(world.FrontDeNeige.position,center)>2400)continue;
            for(int n=0;n<12;n++)
            {
                var dir=Quaternion.Euler(0,n*15,0)*Vector3.forward;
                foreach(int reach in new[]{23,31,40})
                {
                    if(!ManagementSurface.TryGet(center-dir*reach,out var a)||!ManagementSurface.TryGet(center+dir*reach,out var b))continue;
                    if(Mathf.Abs(a.y-b.y)/Vector3.Distance(a,b)>.22f)continue;
                    object[] args={hydro,a,b,bike,null,null,null};
                    if(!(bool)plan.Invoke(null,args)){checkedCount++;continue;}
                    if(RouteAllowed(hydro,a,b,bike?3f:8f,true))continue;
                    build.Invoke(null,new[]{(object)a,b,args[4],args[5],bike});
                    if(!bike)Need(RouteAllowed(hydro,a,b,8f,true),"Tracé encore interdit malgré le pont");
                    Debug.Log("WATER_PASS "+(bike?"passerelle":"pont")+" testé "+a+" à "+b+" ("+checkedCount+" essais)");
                    return (a,b);
                }
            }
        }
        throw new Exception("Franchissement compatible introuvable après "+checkedCount+" essais");
    }

    static void Need(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Snap(TerrainGenerationWorld world,Vector3 lake,string file)
    {
        var cam=Camera.main;
        if(cam==null||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        var origin=cam.transform.position;
        var rotation=cam.transform.rotation;
        var target=RenderTexture.GetTemporary(1366,768,24);
        var previous=RenderTexture.active;
        var pixels=new Texture2D(1366,768,TextureFormat.RGB24,false);
        try
        {
            cam.transform.position=lake+new Vector3(-590,430,-515);
            cam.transform.LookAt(lake+new Vector3(90,60,145));
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam,
                new UnityEngine.Rendering.RenderPipeline.StandardRequest {destination=target});
            RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1366,768),0,0);
            pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(file),pixels.EncodeToPNG());
        }
        finally
        {
            cam.transform.SetPositionAndRotation(origin,rotation);
            RenderTexture.active=previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.Destroy(pixels);
        }
    }
    static void Finish(int exit){SessionState.SetBool(Key+"Run",false);EditorApplication.Exit(exit);}
}
