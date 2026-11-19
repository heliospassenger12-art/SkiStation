using System.Collections.Generic;
using UnityEngine;
using SkiStation.Arcade;
using SkiStation.AlpineEmpires;
namespace SkiStation
{
    // Official front door. Existing class and GUID retained; player rules remain GameModeContext262.
    [DisallowMultipleComponent]
    public sealed class SkiStationStartMenu249:MonoBehaviour
    {
        const string FreeKey="SkiStation.P54.SandboxFree";
        public bool IsShowing263=>isActiveAndEnabled&&instance==this&&showing;
        public bool NavigationPending263=>navigation.Pending;
        internal bool IsAlpineSlot263 => managementIsAlpine && DemoFlow263.CurrentSlot == managementSlot;
        internal AlpineSession ActiveAlpineWorld263 => IsAlpineSlot263 ? alpinePreview.ActiveWorld : null;
        internal AlpineSessionSave CaptureAlpineWorld263() => alpinePreview.CaptureWorld();
        internal void BindRestoredAlpineWorld263(AlpineSession restored)
            => alpinePreview.BindRestoredWorld(restored, StationRuntime.I);
        internal bool TryAuthorizeAlpineService263(Vector3 point, Vector2 size, out string companyId,
            out string sectorId, out string reason)
            => alpinePreview.TryAuthorizeService(point, size, out companyId, out sectorId, out reason);
        static SkiStationStartMenu249 instance;static int scannedFrame=-1;
        readonly DemoHomeNavigation263 navigation=new DemoHomeNavigation263();
        readonly AlpinePreviewPage alpinePreview = new AlpinePreviewPage();
        bool initialized263;
        bool managementIsAlpine;
        public static SkiStationStartMenu249 Owner263
        {
            get
            {
                if(instance!=null)return instance;
                if(scannedFrame==Time.frameCount)return null;scannedFrame=Time.frameCount;
                foreach(var menu in FindObjectsByType<SkiStationStartMenu249>(FindObjectsSortMode.None))
                    if(menu.isActiveAndEnabled){instance=menu;return menu;}
                return null;
            }
        }
        public static bool HomeOpen263=>Owner263!=null&&Owner263.isActiveAndEnabled&&Owner263.showing;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void HookScenes263(){instance=null;scannedFrame=-1;UnityEngine.SceneManagement.SceneManager.sceneLoaded-=SceneLoaded263;UnityEngine.SceneManagement.SceneManager.sceneLoaded+=SceneLoaded263;}
        static void SceneLoaded263(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode){if(scene.name==DemoFlow263.HomeScene)Create();}
        bool showing=true,sandboxPage,otherReprises,freeResearch=true,confirmNew;
        GameMode262 choice=GameMode262.ManagementOnly;bool choiceArcade;
        bool hasPlayedManagement;GameMode262 managementMode;bool managementFree,managementMaxMastery=true,pendingMaxMastery=true;string managementSlot="";DemoResumeRecord263 confirmingResume;float previousTimeScale=1;
        Vector2 scroll;GUIStyle title,subtitle,card;Texture2D backdrop;
        List<DemoResumeRecord263> reprises=new List<DemoResumeRecord263>();string notice="";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (SkiStation.ValAlpin.ValAlpinScene.IsActive) return;
            if(ArcadeSceneIsolation.IsActive)return;
            var owner=Owner263;
            if(owner==null)
            {
                foreach(var menu in FindObjectsByType<SkiStationStartMenu249>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(menu.gameObject.activeInHierarchy){owner=menu;break;}
            }
            if(owner==null)owner=new GameObject("Accueil SkiStation").AddComponent<SkiStationStartMenu249>();
            // Rebind surviving scene objects even when Awake does not rerun in the Editor.
            instance=owner;owner.enabled=true;owner.EnsureComponents263();owner.showing=true;
            owner.navigation.Cancel();owner.alpinePreview.Close();owner.RefreshReprises();owner.SuppressReplicas263();Time.timeScale=0;
        }
        bool ClaimOwner263()
        {
            if(instance!=null&&instance!=this){showing=false;navigation.Cancel();enabled=false;return false;}
            instance=this;scannedFrame=-1;return true;
        }
        void OnEnable(){ClaimOwner263();}
        void Awake()
        {
            if(!ClaimOwner263())return;
            EnsureComponents263();RefreshReprises();Time.timeScale=0;
        }
        void EnsureComponents263()
        {
            DemoRuntime263.Ensure();
            if(GetComponent<ManagementOnboarding263>()==null)gameObject.AddComponent<ManagementOnboarding263>();
            var pause=GetComponent<DemoManagementMenu263>()??gameObject.AddComponent<DemoManagementMenu263>();pause.BindHome263(this);
            if(!initialized263){initialized263=true;freeResearch=PlayerPrefs.GetInt(FreeKey,1)==1;previousTimeScale=Time.timeScale;}
        }
        void SuppressReplicas263()
        {
            foreach(var menu in FindObjectsByType<SkiStationStartMenu249>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(menu!=this){menu.showing=false;menu.navigation.Cancel();menu.enabled=false;var pause=menu.GetComponent<DemoManagementMenu263>();if(pause!=null)pause.enabled=false;var guide=menu.GetComponent<ManagementOnboarding263>();if(guide!=null)guide.enabled=false;}
        }
        void QueueNavigation263(System.Action command)
        {
            if(!navigation.Schedule(command))return;
            DemoDiagnostics263.Record("home.choice.queued");
            if(Event.current!=null)GUIUtility.keyboardControl=0;
        }
        void Update()
        {
            if(instance!=this)return;
            if(showing)Time.timeScale=0;
            if(!navigation.Take(out var command))return;
            try{command();}
            catch(System.Exception ex)
            {
                showing=true;Time.timeScale=0;notice="Impossible d’ouvrir cette expérience. Votre sauvegarde est conservée. Vous pouvez réessayer ou choisir un autre mode.";
                DemoDiagnostics263.Record("home.choice.failed",ex.GetType().Name+": "+ex.Message);
            }
        }
        void RefreshReprises(){reprises=DemoFlow263.ValidReprises(out notice);}
        public void ShowHome263()
        {var owner=Owner263;if(owner!=null&&owner!=this){owner.ShowHome263();return;}instance=this;enabled=true;EnsureComponents263();SuppressReplicas263();navigation.Cancel();alpinePreview.Close();showing=true;sandboxPage=otherReprises=confirmNew=false;confirmingResume=null;scroll=Vector2.zero;previousTimeScale=Time.timeScale;Time.timeScale=0;RefreshReprises();}
        void OnDestroy(){bool owner=instance==this;if(owner){instance=null;scannedFrame=-1;if(showing)Time.timeScale=previousTimeScale;}navigation.Cancel();if(backdrop!=null)Destroy(backdrop);}
        void Styles()
        {
            if(title!=null){ApplyFonts263();return;}
            title=new GUIStyle(GUI.skin.label){fontSize=36,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};title.normal.textColor=Color.white;
            subtitle=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true,alignment=TextAnchor.MiddleCenter};subtitle.normal.textColor=Color.white;
            card=new GUIStyle(GUI.skin.button){fontSize=17,wordWrap=true,alignment=TextAnchor.MiddleCenter};ApplyFonts263();
            // Native lightweight silhouette. No external asset, renderer, hidden demo world or music claim.
            backdrop=new Texture2D(512,256,TextureFormat.RGB24,false);backdrop.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[512*256];
            for(int x=0;x<512;x++)for(int y=0;y<256;y++)
            {
                float distant=90+45*Mathf.Abs(Mathf.Sin(x*.014f))+25*Mathf.Abs(Mathf.Sin(x*.033f));
                float near=45+40*Mathf.Abs(Mathf.Sin(x*.022f+1));
                Color color=Color.Lerp(new Color(.07f,.16f,.24f),new Color(.025f,.06f,.12f),y/256f);
                if(y<distant)color=y>distant-6?new Color(.28f,.38f,.46f):new Color(.14f,.23f,.31f);
                if(y<near)color=new Color(.045f,.105f,.16f);pixels[y*512+x]=color;
            }
            backdrop.SetPixels(pixels);backdrop.Apply(false,true);
        }
        void ApplyFonts263(){float scale=DemoSettings263.UiScale;title.fontSize=Mathf.RoundToInt(36*scale);subtitle.fontSize=Mathf.RoundToInt(16*scale);card.fontSize=Mathf.RoundToInt(17*scale);}
        internal void DrawModal263(int id)
        {
            Styles();GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),backdrop,ScaleMode.StretchToFill);DrawHome(id);
        }
        void DrawHome(int id)
        {
            bool prior=GUI.enabled;GUI.enabled=prior&&!navigation.Pending;
            try{DrawHomeBody263();}finally{GUI.enabled=prior;}
        }
        void DrawHomeBody263()
        {
            float width=Mathf.Min(660,Screen.width-24),height=Mathf.Min(690,Screen.height-24);
            Rect panel=new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height);GUI.Box(panel,"");
            GUILayout.BeginArea(new Rect(panel.x+20,panel.y+12,width-40,height-24));
            scroll=DemoPadUi263.BeginScrollView(scroll);
            GUILayout.Label("SKISTATION",title,GUILayout.Height(52));
            GUILayout.Label("Votre montagne. Votre façon de jouer.",subtitle,GUILayout.Height(32));GUILayout.Space(12);
            GUILayout.Label("Clavier ou manette : votre choix dans Options → Commandes.",subtitle);
            if(DemoGamepad263.PadPrompts)GUILayout.Label("Croix directionnelle : choisir · Croix/A : valider · Stick droit : défiler",subtitle);
            if(!string.IsNullOrEmpty(notice))GUILayout.Label(notice,subtitle);
            if(!string.IsNullOrEmpty(DemoFlow263.Notice))GUILayout.Label(DemoFlow263.Notice,subtitle);

            if(alpinePreview.IsOpen)
            {
                alpinePreview.Draw(subtitle, card, QueueNavigation263, LaunchAlpineStation263, () =>
                {
                    alpinePreview.Close();
                    scroll = Vector2.zero;
                });
            }
            else if(confirmNew)
            {
                GUILayout.Label("Ouvrir une nouvelle expérience ?",subtitle);
                GUILayout.Label("Votre station actuelle n’est pas sauvegardée automatiquement. Vos reprises enregistrées et vos riders sont conservés.",subtitle);
                if(DemoPadUi263.Button("Revenir à la station",GUILayout.Height(44)))ResumeManagement263();
                if(DemoPadUi263.Button(confirmingResume!=null?"Reprendre "+confirmingResume.domainName:"Ouvrir "+DemoFlow263.ModeName(choice),GUILayout.Height(44))){QueueNavigation263(()=>{confirmNew=false;if(confirmingResume!=null){var record=confirmingResume;confirmingResume=null;ContinueNow263(record);}else LaunchNow263(choice,choiceArcade);});}
                if(DemoPadUi263.Button("Rester à l’accueil"))QueueNavigation263(()=>confirmNew=false);
            }
            else if(sandboxPage)
            {
                GUILayout.Label("BAC À SABLE",subtitle);
                GUILayout.Label("Construisez et ridez librement. Progression et compétitions facultatives.",subtitle);
                freeResearch=DemoPadUi263.Toggle(freeResearch,"Toutes les remontées disponibles");
                pendingMaxMastery=DemoPadUi263.Toggle(pendingMaxMastery,"Riders aux maîtrises maximales (carrière préservée)");
                if(DemoPadUi263.Button("CONSTRUIRE — ouvrir la station",card,GUILayout.Height(68)))Choose(GameMode262.Sandbox,false);
                if(DemoPadUi263.Button("RIDER — partir sur Grand Aigle",card,GUILayout.Height(68)))Choose(GameMode262.Sandbox,true);
                if(DemoPadUi263.Button("Retour",GUILayout.Height(35)))QueueNavigation263(()=>sandboxPage=false);
            }
            else if(otherReprises)
            {
                GUILayout.Label("VOS REPRISES",subtitle);
                foreach(var r in reprises)if(DemoPadUi263.Button("Continuer\n"+Summary(r),card,GUILayout.Height(74)))Continue(r);
                if(DemoPadUi263.Button("Retour",GUILayout.Height(35)))QueueNavigation263(()=>otherReprises=false);
            }
            else
            {
                if(reprises.Count>0)
                {
                    if(DemoPadUi263.Button("CONTINUER\n"+Summary(reprises[0]),card,GUILayout.Height(76)))Continue(reprises[0]);
                    if(reprises.Count>1&&DemoPadUi263.Button("Autres reprises",GUILayout.Height(28)))QueueNavigation263(()=>otherReprises=true);
                    GUILayout.Space(12);
                }
                GUILayout.Label("NOUVELLE EXPÉRIENCE",subtitle,GUILayout.Height(28));
                if (DemoPadUi263.Button("ALPINE EMPIRES\nAvant-première solo", card, GUILayout.Height(70)))
                    QueueNavigation263(() => { alpinePreview.Open(); scroll = Vector2.zero; });
                if(DemoPadUi263.Button("GESTION\nConstruisez et gérez votre station.",card,GUILayout.Height(70)))Choose(GameMode262.ManagementOnly,false);
                if(DemoPadUi263.Button("ARCADE\nChoisissez un rider et partez skier.",card,GUILayout.Height(70)))Choose(GameMode262.ArcadeOnly,true);
                if(DemoPadUi263.Button("CARRIÈRE\nConstruisez votre station et faites progresser vos riders.",card,GUILayout.Height(70)))Choose(GameMode262.Career,false);
                if(DemoPadUi263.Button("BAC À SABLE\nTout est libre. Construisez, testez et ridez.",card,GUILayout.Height(70))){QueueNavigation263(()=>{sandboxPage=true;pendingMaxMastery=RiderService262.MaxSandboxMastery;});}
                GUILayout.Space(8);GUILayout.Label("Chaque expérience se joue librement. Aucun passage obligé par un autre mode.",subtitle);
                GUILayout.Label("Les riders existants sont conservés lors d’un nouveau départ.",subtitle);
                if(hasPlayedManagement&&DemoPadUi263.Button("Revenir à ma station actuelle",GUILayout.Height(32)))ResumeManagement263();
            }
            GUILayout.Space(8);
            if(DemoPadUi263.Button("Options / confort",GUILayout.Height(32)))DemoRuntime263.OpenOptions();
            if(DemoPadUi263.Button("Quitter SkiStation",GUILayout.Height(32)))
            {
#if UNITY_EDITOR
                QueueNavigation263(()=>notice="Dans Unity, arrêtez Play pour quitter le jeu.");
#else
                DemoModalSurface263.Post(Application.Quit);
#endif
            }
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        static string Summary(DemoResumeRecord263 r)
        {
            string when=System.DateTimeOffset.TryParse(r.savedUtc,out var time)?time.ToLocalTime().ToString("dd/MM/yyyy HH:mm"):"";
            return (r.alpine?"Alpine Empires":DemoFlow263.ModeName(r.mode))+" · "+r.domainName+(r.arcade?" · "+r.riderName:" · domaine matériel")+"\n"+when;
        }
        void Choose(GameMode262 mode,bool arcade){QueueNavigation263(()=>ChooseNow263(mode,arcade));}
        void ChooseNow263(GameMode262 mode,bool arcade)
        {confirmingResume=null;choice=mode;choiceArcade=arcade;if(hasPlayedManagement){if(!arcade){notice="Un nouveau monde Gestion ne peut pas encore être créé pendant cette session. Votre station actuelle et ses règles sont conservées. Vous pouvez y revenir.";return;}confirmNew=true;return;}LaunchNow263(mode,arcade);}
        void Launch(GameMode262 mode,bool arcade){QueueNavigation263(()=>LaunchNow263(mode,arcade));}
        void LaunchAlpineStation263()
        {
            if (alpinePreview.HasStation)
            {
                ResumeManagementNow263();
                return;
            }
            if (hasPlayedManagement)
            {
                notice = "Une autre station est déjà ouverte. Arrêtez puis relancez Play pour commencer une partie Alpine Empires.";
                return;
            }
            LaunchNow263(GameMode262.ManagementOnly, false, alpine: true);
        }
        void LaunchNow263(GameMode262 mode,bool arcade,bool alpine=false)
        {
            if(hasPlayedManagement&&!arcade){notice="Votre station actuelle est conservée. Aucun autre monde Gestion chargé ni changement de règles appliqué.";return;}
            if (alpine && (StationRuntime.I == null || FinanceRuntime.I == null || ArcadeSceneIsolation.IsActive))
            {
                notice = "La Gestion 3D n’est pas prête. Ouvrez SkiStation_Bootstrap puis relancez Play.";
                return;
            }
            string scene="";
            if(arcade)
            {
                foreach(var d in RiderCatalogue262.Data.domains)if(!d.validationDomain){scene=d.sceneName;break;}
                if(string.IsNullOrEmpty(scene)||!Application.CanStreamedLevelBeLoaded(scene)){notice="La montagne n’est pas disponible dans cette installation. Vous pouvez revenir aux autres modes.";return;}
            }
            if(!GameModeContext262.Select(mode)){notice="Terminez la session Arcade avant de choisir un autre mode.";return;}
            if(mode==GameMode262.Sandbox)RiderService262.MaxSandboxMastery=pendingMaxMastery;
            DemoFlow263.BeginNew();DemoSaveCoordinator263.NewContext();DemoDiagnostics263.Context();DemoDiagnostics263.Record("mode.new",DemoFlow263.ModeName(mode));
            if(!arcade){managementMode=mode;managementFree=freeResearch;managementMaxMastery=RiderService262.MaxSandboxMastery;managementSlot=DemoFlow263.CurrentSlot;LiftCareerCatalog249.StartNewSession(mode==GameMode262.Sandbox,mode==GameMode262.Sandbox&&freeResearch);GameSpeedController.I?.SetSpeed(GameSpeed.Normal);previousTimeScale=1;}
            else {LiftCareerCatalog249.SetSandboxSession(mode==GameMode262.Sandbox);LiftCareerCatalog249.SetSandboxResearchRules(freeResearch);}
            if (alpine)
            {
                if (DemoManagementEntry263.PrepareManagementSource() == null)
                    throw new System.InvalidOperationException("Domaine de Gestion indisponible");
                alpinePreview.AttachManagement(StationRuntime.I, FinanceRuntime.I);
                managementIsAlpine = true;
                hasPlayedManagement = true;
            }
            else
            {
                PlayerPrefs.SetInt(FreeKey,freeResearch?1:0);PlayerPrefs.Save();
            }
            ReleaseHome();
            if(arcade){DemoSceneTravel263.LoadArcade(scene);return;}
            hasPlayedManagement = true;
            if (!alpine) DemoManagementEntry263.PrepareManagementSource();
            FindFirstObjectByType<PlayableCameraController>()?.FocusEntrance();
        }
        void Continue(DemoResumeRecord263 record){QueueNavigation263(()=>ContinueChoiceNow263(record));}
        void ContinueChoiceNow263(DemoResumeRecord263 record)
        {
            if(hasPlayedManagement){if(record.arcade){confirmingResume=record;confirmNew=true;}else notice="Votre station actuelle reste en mémoire. La restauration d’une autre station sera disponible avec le chargeur complet.";return;}
            ContinueNow263(record);
        }
        void Continue263(DemoResumeRecord263 record){QueueNavigation263(()=>ContinueNow263(record));}
        void ContinueNow263(DemoResumeRecord263 record)
        {
            if(record.sceneName==DemoFlow263.HomeScene)DemoManagementEntry263.PrepareManagementSource();
            if(!DemoFlow263.Continue(record)){notice=DemoFlow263.Notice;RefreshReprises();return;}
            if(record.sceneName==DemoFlow263.HomeScene)
            {
                if(!DemoFlow263.TryRestoreManagement()){notice=DemoFlow263.Notice;return;}hasPlayedManagement=true;managementIsAlpine=record.alpine;if(record.alpine)previousTimeScale=1;managementMode=record.mode;managementFree=record.sandboxFree;managementMaxMastery=record.maxMastery;managementSlot=record.slotId;ReleaseHome();
            }
            else ReleaseHome();
        }
        void ResumeManagement263(){QueueNavigation263(ResumeManagementNow263);}
        void ResumeManagementNow263()
        {
            GameModeContext262.Select(managementMode);LiftCareerCatalog249.SetSandboxSession(managementMode==GameMode262.Sandbox);
            LiftCareerCatalog249.SetSandboxResearchRules(managementFree);RiderService262.MaxSandboxMastery=managementMaxMastery;DemoFlow263.UseSlot(managementSlot);ReleaseHome();
        }
        void ReleaseHome(){instance=this;showing=false;SuppressReplicas263();Time.timeScale=previousTimeScale;scroll=Vector2.zero;DemoDiagnostics263.Record("home.closed",DemoFlow263.ModeName(GameModeContext262.Mode));enabled=false;}
    }
}
