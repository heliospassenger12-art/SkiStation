using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SkiStation
{
    // Material-domain checkpoint owned by Management, not a complete active-day/career save.
    // Explicit operations only. No Update, autosave, visit cancellation, teleport or economic transaction.
    [DisallowMultipleComponent]
    public sealed class ManagementDomainPersistence261:MonoBehaviour,IManagementDomainLoader261
    {
        public ManagementDomainSource261 source;
        public ManagementSaveAssetCatalogue261 assets;
        public string loadDomainId="";
        [Tooltip("Inclure le registre global de ce prototype. Ne constitue pas une sauvegarde carrière.")] public bool includeGlobalLedger;
        [Tooltip("Conserver les visiteurs compatibles, files et occupations natives. Exige horloge, solde et registre financier dans le même checkpoint.")] public bool includeActiveLiftTraffic391=true;
        public string LastResult {get;private set;}="Domaine en mémoire ; aucun checkpoint chargé";
        public string LoadedDomainId=>source!=null?source.DomainId:"";
        string diskRevision="",revisionDomain="";
        public string LoadedSaveRevision {get=>revisionDomain==LoadedDomainId?diskRevision:"";private set{diskRevision=value;revisionDomain=LoadedDomainId;}}
        public bool ManagementStateReady=>source!=null&&!loading;
        bool loading,checkpointTransaction;GameObject ownedRoot;ManagementGeometryCodec261 ownedGeometry;
        readonly List<UnityEngine.Object> ownedDefinitions=new List<UnityEngine.Object>();
        public ManagementDomainFileStore261 Store=>new ManagementDomainFileStore261(Path.Combine(Application.persistentDataPath,"ManagementDomains261"));
        public void Bind(ManagementDomainSource261 value)
        {if(SkiStation.Arcade.ArcadeSkiSession.ActiveSession!=null)throw new InvalidOperationException("Lier en Gestion seulement");if(source!=value){diskRevision="";revisionDomain="";}source=value;if(source!=null){source.saveLoader=this;loadDomainId=source.DomainId;}if(assets==null)assets=Resources.Load<ManagementSaveAssetCatalogue261>("ManagementPersistence261/Assets");}
        void Awake(){if(source!=null)Bind(source);}
        void EnsureQuiet(bool allowTraffic391=false,bool alpine=false)
        {
            if(LiftTrafficPersistence391.Replacing)throw new InvalidOperationException("Un chargement Gestion est déjà en cours");
            if(source==null||!source.isActiveAndEnabled)throw new InvalidOperationException("Source Gestion absente");
            ReturnConstructionRuntime.RequireCheckpointQuiet(source);
            if(loading||checkpointTransaction||SkiStation.Arcade.ArcadeSkiSession.ActiveSession!=null||SkiStation.Arcade.ArcadeSceneIsolation.IsActive)throw new InvalidOperationException("Sauvegarder / charger hors Arcade");
            foreach(var s in UnityEngine.Object.FindObjectsByType<SlopeRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(source.Contains(s)&&!s.isActiveAndEnabled)throw new NotSupportedException("Piste source désactivée : checkpoint non tronqué");
            foreach(var l in UnityEngine.Object.FindObjectsByType<LiftRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(source.Contains(l)&&!l.isActiveAndEnabled)throw new NotSupportedException("Lift source désactivée : checkpoint non tronqué");
            foreach(var b in UnityEngine.Object.FindObjectsByType<StationStructureStatus249>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(source.Contains(b)&&!b.isActiveAndEnabled)throw new NotSupportedException("Bâtiment source désactivé : checkpoint non tronqué");
            // These ledgers cannot be reconstituted from geometry. Refuse before writing or swapping.
            if(!allowTraffic391)
            {
                foreach(var v in UnityEngine.Object.FindObjectsByType<SkierRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(OperationsRules390.InDomain(source,v))throw new NotSupportedException("Activer le checkpoint de trafic : visiteurs présents");
                foreach(var l in LiftRuntime.Active)if(source.Contains(l)&&(l.HasOnboardPassengers||(l.StationLogic!=null&&l.StationLogic.QueueSize>0)))throw new NotSupportedException("Trajet / file engagés : aucun passager ne sera supprimé");
            }
            foreach(var lift in LiftRuntime.Active)if(source.Contains(lift))foreach(var vehicle in LiftTrafficPersistence391.Fleet(lift))
                if(vehicle.SimulationPassengerCount!=0)throw new NotSupportedException("Passager joueur actif non sérialisable : terminer sa session avant de charger/sauvegarder");
            if(!allowTraffic391)
            {
                foreach(var b in StationServiceRuntime.All)if(source.Contains(b)&&(b.ActiveCustomers>0||b.QueueCount>0))throw new NotSupportedException("Activer le checkpoint de trafic pour les clients des services");
                foreach(var school in SkiSchoolRuntime.All)if(source.Contains(school))foreach(var lesson in school.Sessions)if(lesson.active)throw new NotSupportedException("Activer le checkpoint de trafic pour les cours");
            }
            foreach(var site in ConstructionSiteRuntime376.Active)if(site!=null&&!site.Completed&&!site.IsRecommendation389&&(source.Contains(site)||TargetsSource(site)))throw new NotSupportedException("Chantier non sérialisable engagé");
            foreach(var project in UnityEngine.Object.FindObjectsByType<SlopeConstructionProjectRuntime>(FindObjectsSortMode.None))if((source.Contains(project)||source.Contains(project.Slope))&&!project.IsFinished)throw new NotSupportedException("Projet de piste inachevé");
            foreach(var lodging in UnityEngine.Object.FindObjectsByType<StationAccommodation249>(FindObjectsSortMode.None))if(source.Contains(lodging)&&lodging.CurrentGuests>0)throw new NotSupportedException("Séjours / réservations engagés");
            foreach(var slope in UnityEngine.Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))if(source.Contains(slope))
            {var m=slope.GetComponent<SlopeMaintenanceRuntime>();if(m!=null&&m.groomingInProgress)throw new NotSupportedException("Damage engagé");}
            foreach(var connector in UnityEngine.Object.FindObjectsByType<SlopeLiftConnectorRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(source.Contains(connector)&&!connector.isActiveAndEnabled)throw new NotSupportedException("Raccord source désactivé : checkpoint non tronqué");
            var roads=StationRoadNetwork247.I;if(!alpine&&source.Contains(roads)&&roads.IsReady)throw new NotSupportedException("Réseau routier logique : adaptateur de sauvegarde encore requis");
        }
        bool TargetsSource(ConstructionSiteRuntime376 site)
        {foreach(var s in UnityEngine.Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))if(source.Contains(s)&&site.TargetsCheckpoint261(s))return true;foreach(var l in LiftRuntime.Active)if(source.Contains(l)&&site.TargetsCheckpoint261(l))return true;foreach(var b in StationServiceRuntime.All)if(source.Contains(b)&&site.TargetsCheckpoint261(b))return true;foreach(var b in UnityEngine.Object.FindObjectsByType<StationStructureStatus249>(FindObjectsSortMode.None))if(source.Contains(b)&&site.TargetsCheckpoint261(b))return true;return false;}
        static string Id(Component c)
        {var identity=c.GetComponent<ManagementElementIdentity261>();if(identity==null||!ManagementElementIdentity261.Valid(identity.Id))throw new InvalidOperationException("ID Gestion absent : "+c.name);return identity.Id;}
        static T[] Copy<T>(IReadOnlyList<T> values){var result=new T[values.Count];for(int i=0;i<result.Length;i++)result[i]=values[i];return result;}
        static List<ManagementClosure261> Closures(Component c,bool open)
        {
            var reasons=OperationalClosureRegistry381.I!=null?OperationalClosureRegistry381.I.CaptureCheckpointReasons261(c):new List<ManagementClosure261>();
            if(open&&reasons.Count>0)throw new InvalidOperationException("État d'ouverture divergent du registre Gestion : "+c.name);
            if(!open&&reasons.Count==0&&!(c is LiftRuntime faulted&&faulted.Faulted))reasons.Add(new ManagementClosure261{source=OperationalClosureSource381.Manual,reason="Fermeture Gestion enregistrée"});return reasons;
        }
        static List<ManagementSlopeLink261> Links(IReadOnlyList<SlopeEndpointConnection> links,HashSet<string> ids)
        {
            var result=new List<ManagementSlopeLink261>();if(links==null)return result;
            foreach(var l in links)
            {
                if(!l.IsValid)continue;string target=null;
                if(l.slope!=null)target=Id(l.slope);else if(l.target!=null){var identity=l.target.GetComponent<ManagementElementIdentity261>();if(identity!=null)target=identity.Id;else throw new NotSupportedException("Cible de connexion sans ID Gestion");}
                if(target!=null&&!ids.Contains(target))throw new NotSupportedException("Connexion hors domaine : "+target);
                result.Add(new ManagementSlopeLink261{kind=l.kind,target=target,distance=l.distance});
            }
            return result;
        }
        public ManagementSaveData261 CaptureCheckpoint() => CaptureCheckpoint(null);
        internal ManagementSaveData261 CaptureAlpineCheckpoint(AlpineEmpires.AlpineSessionSave world)
            => CaptureCheckpoint(world??throw new ArgumentNullException(nameof(world)));
        ManagementSaveData261 CaptureCheckpoint(AlpineEmpires.AlpineSessionSave alpineWorld)
        {
            bool alpine=alpineWorld!=null;
            EnsureQuiet(includeActiveLiftTraffic391&&!alpine,alpine);if(ReturnConstructionRuntime.HasPaid(source)&&!includeGlobalLedger)throw new InvalidOperationException("Inclure le solde et le registre financier pour conserver les coûts de terrassement");
            var snap=source.Capture();var stationIdentity262=SkiStation.Arcade.StationIdentity262.Ensure(source);
            if(snap.Slopes.Count!=snap.ManagementSlopeCount||snap.Lifts.Count!=snap.ManagementLiftCount||snap.Buildings.Count!=snap.ManagementBuildingCount||snap.Connectors.Count!=snap.ManagementConnectorCount)throw new InvalidOperationException("Mappings Gestion incomplets ; sauvegarde refusée");
            var d=new ManagementSaveData261{schema=alpine?ManagementSaveData261.AlpineSchema:ManagementSaveData261.CurrentSchema,domainId=source.DomainId,revision="save."+Guid.NewGuid().ToString("N"),createdUtc=DateTime.UtcNow.ToString("O"),sourceScene=source.gameObject.scene.name,domainRevision=source.Revision,
                hasFront=source.frontDeNeige!=null,front=source.frontDeNeige!=null?source.frontDeNeige.position:Vector3.zero,
                connectors=new List<ManagementSavedConnector261>(),slopes=new List<ManagementSavedSlope261>(),lifts=new List<ManagementSavedLift261>(),buildings=new List<ManagementSavedBuilding261>(),scenery=new List<ManagementGeometryNode261>(),meshes=new List<ManagementMesh261>(),materials=new List<ManagementMaterial261>(),
                limitations=new List<string>{"Checkpoint du domaine matériel à un point calme ; pas une sauvegarde carrière complète.","Visiteurs, cours, files, séjours et chantiers engagés refusés.","Recherche, crédits, réputation et autres gestionnaires de carrière ne sont pas restaurés.","Décor statique supporté ; Terrain généré SkiStation via STF1 ; autres Terrain/LOD/géométries dynamiques refusés."}};
            d.hasReturnWorksCosts=ReturnConstructionRuntime.HasPaid(source);
            d.generatedTerrain=ManagementTerrainCheckpointBridge.Capture(source);
            d.mountainRelief=MountainManagementCheckpointBridge.Capture(source);
            if(d.generatedTerrain!=null)d.limitations.Add("Relief/zones/décor/profil figés STF1 inclus. Chargement explicite du relief compatible avant les constructions ; aucun mesh commercial sérialisé comme décor.");
            d.stationIdentity=stationIdentity262.profile.Clone();
            d.hasClock=source.Contains(GameClock.I);if(d.hasClock){d.clockEnabled=GameClock.I.enabled;d.day=GameClock.I.CurrentDay;d.hour=GameClock.I.CurrentHour;d.clockConfiguration=JsonUtility.ToJson(GameClock.I);}
            d.hasStation=includeGlobalLedger&&StationRuntime.I!=null;if(d.hasStation)d.stationState=JsonUtility.ToJson(StationRuntime.I.State);
            if(includeGlobalLedger&&FinanceRuntime.I!=null)d.finance=FinanceRuntime.I.CaptureCheckpoint261();
            d.construction389=RecommendationConstructionPersistence389.Capture(source);
            d.operations390=StationOperationsPersistence390.Capture(source);
            bool captureTraffic391=includeActiveLiftTraffic391&&!alpine;
            if(captureTraffic391&&(!d.hasClock||!d.hasStation||d.finance==null))
            {
                bool occupied=false;foreach(var skier in UnityEngine.Object.FindObjectsByType<SkierRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(OperationsRules390.InDomain(source,skier)){occupied=true;break;}
                foreach(var lift in LiftRuntime.Active)if(source.Contains(lift)&&(lift.HasOnboardPassengers||(lift.StationLogic!=null&&lift.StationLogic.QueueSize>0)))occupied=true;
                if(occupied)throw new InvalidOperationException("Trafic actif : inclure horloge, station et registre financier avant de sauvegarder");
                captureTraffic391=false;
            }
            if(source.Contains(ResortRouteNetwork.I))d.graphConfiguration=JsonUtility.ToJson(ResortRouteNetwork.I);
            var ids=new HashSet<string>(StringComparer.Ordinal);var elements=new HashSet<Transform>();
            foreach(var v in snap.Slopes){ids.Add(v.Id);elements.Add(v.Source.transform);}foreach(var v in snap.Lifts){ids.Add(v.Id);elements.Add(v.Source.transform);}foreach(var v in snap.Buildings){ids.Add(v.Id);elements.Add(v.Source.transform);}
            var connectors=new List<SlopeLiftConnectorRuntime>();
            foreach(var c in UnityEngine.Object.FindObjectsByType<SlopeLiftConnectorRuntime>(FindObjectsSortMode.None))if(source.Contains(c))
            {
                if(!source.Contains(c.SourceSlope)||!source.Contains(c.TargetLift)||!ids.Contains(Id(c.SourceSlope))||!ids.Contains(Id(c.TargetLift)))throw new NotSupportedException("Raccord hors domaine / référence orpheline : "+c.name);
                if(!ids.Add(Id(c)))throw new InvalidOperationException("ConnectorId dupliqué : "+Id(c));
                connectors.Add(c);elements.Add(c.transform);
            }
            connectors.Sort((a,b)=>StringComparer.Ordinal.Compare(Id(a),Id(b)));
            using(var codec=new ManagementGeometryCodec261(d,assets,elements,captureTraffic391))
            {
                foreach(var v in snap.Slopes)
                {
                    var s=v.Source;var maintenance=s.GetComponent<SlopeMaintenanceRuntime>();var connection=s.GetComponent<SlopeConnectionRuntime>();
                    d.slopes.Add(new ManagementSavedSlope261{id=v.Id,title=s.name,points=Copy(s.Points),widths=Copy(v.Widths),designed=s.HasDesignedDifficulty,difficulty=s.Difficulty,open=s.Open,groomed=s.Groomed,runs=s.CompletedRuns,popularity=s.PopularityScore,
                        maintenance=maintenance!=null?JsonUtility.ToJson(maintenance):null,top=Links(connection!=null?connection.TopConnections:null,ids),bottom=Links(connection!=null?connection.BottomConnections:null,ids),closures=Closures(s,s.Open),visual=codec.Capture(s.transform,true)});
                }
                foreach(var v in snap.Lifts){var l=v.Source;d.lifts.Add(new ManagementSavedLift261{id=v.Id,title=l.name,definition=JsonUtility.ToJson(l.Definition),route=Copy(l.Route),underground=l.IsUnderground,open=l.Open,faulted=l.Faulted,closures=Closures(l,l.Open)});}
                foreach(var v in snap.Buildings)
                {
                    var go=v.Source.gameObject;var service=go.GetComponent<StationServiceRuntime>();var lodging=go.GetComponent<StationAccommodation249>();var shop=go.GetComponent<StationEquipmentShop249>();var status=go.GetComponent<StationStructureStatus249>();var school=go.GetComponent<SkiSchoolRuntime>();
                    d.buildings.Add(new ManagementSavedBuilding261{id=v.Id,title=go.name,service=service!=null&&service.definition!=null?JsonUtility.ToJson(service.definition):null,accommodation=lodging!=null?JsonUtility.ToJson(lodging):null,shop=shop!=null?JsonUtility.ToJson(shop):null,hasStatus=status!=null,status=status!=null?status.Current:StationStructureStatus249.State.Closed,hasSchool=school!=null,
                        alpineCompany=service?.AlpineCompanyId,alpineTerritory=service?.AlpineTerritoryId,
                        school=school!=null?new SkiSchoolConfiguration261{zoneType=school.zoneType,individualLessons=school.individualLessons,groupLessons=school.groupLessons,maxGroupSize=school.maxGroupSize,instructors=school.instructors,piouPiouMaxAge=school.piouPiouMaxAge,adultMinAge=school.adultMinAge}:null,visual=codec.Capture(go.transform,true)});
                }
                foreach(var c in connectors)d.connectors.Add(new ManagementSavedConnector261{id=Id(c),title=c.name,slopeId=Id(c.SourceSlope),liftId=Id(c.TargetLift),entry=c.EntryPoint,route=Copy(c.Route),width=c.Width,open=c.Open,visual=codec.Capture(c.transform,true)});
                if(alpine)
                {
                    var roads=AlpineMaterialCheckpoint261.RequireRoad(source);
                    var paths=AlpineMaterialCheckpoint261.Paths(source);
                    var state=new AlpineMaterialCheckpoint261{world=alpineWorld,roadSha256=AlpineMaterialCheckpoint261.RoadHash(roads)};
                    if (d.sourceScene == ValAlpin.ValAlpinScene.SceneName)
                        state.CaptureValAlpinTerrain();
                    foreach(var path in paths)
                    {
                        var identity=path.Building.GetComponent<ManagementElementIdentity261>();
                        if(identity==null||!d.buildings.Exists(b=>b.id==identity.Id))
                            throw new NotSupportedException("Accès piéton sans bâtiment persistant.");
                        state.accesses.Add(new AlpinePedestrianCheckpoint261{buildingId=identity.Id,points=path.RoadToDoor,sceneryIndex=d.scenery.Count});
                        d.scenery.Add(codec.Capture(path.Visual.transform,true));
                    }
                    d.alpineMaterial=JsonUtility.ToJson(state);
                    foreach(var t in SceneryRoots(source,elements,AlpineMaterialCheckpoint261.SkipScenery(roads,paths)))d.scenery.Add(codec.Capture(t,true));
                }
                else foreach(var t in SceneryRoots(source,elements))d.scenery.Add(codec.Capture(t,true));
                if(captureTraffic391)
                {
                    d.traffic391=LiftTrafficPersistence391.Capture(source,codec,assets);
                    d.limitations[0]="Checkpoint matériel et trafic compatible. Pas une sauvegarde carrière universelle.";
                    d.limitations[1]="Files, véhicules, occupations et visiteurs compatibles inclus. Clients, cours, séjours et activités non prises en charge restent refusés.";
                }
            }
            ManagementSaveValidation261.Validate(d);return d;
        }
        static bool HasGeometry(Transform t)=>t.GetComponent<TerrainGeneration.WaterCrossing>()!=null||t.GetComponent<Renderer>()!=null||t.GetComponent<Collider>()!=null||t.GetComponent<SkiStation.Arcade.ArcadeSector>()!=null||t.GetComponent<SkiStation.Arcade.ArcadeRail>()!=null||t.GetComponent<SkiStation.Arcade.ArcadeWallrideSurface>()!=null;
        static List<Transform> SceneryRoots(ManagementDomainSource261 src,HashSet<Transform> elements,HashSet<Transform> skipRoots=null)
        {
            var result=new List<Transform>();
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!src.Contains(t)||!HasGeometry(t)||t.GetComponentInParent<StationOperationsRuntime390>()!=null||ManagementTerrainCheckpointBridge.OwnedScenery(src,t)||MountainManagementCheckpointBridge.OwnedTerrain(src,t)||ReturnConstructionRuntime.Transient(t)||RecommendationConstructionRules389.IsTransient(t)
                    ||StationRoadConstruction249.IsPreview(t)||ReliefViewModesRuntime.IsGridVisual(t)
                    ||ConstructionSelectionRuntime.IsSelectionVisual(t))continue;bool skip=false;
                for(var p=t;p!=null;p=p.parent)if(elements.Contains(p)||(skipRoots!=null&&skipRoots.Contains(p))||p.GetComponent<Camera>()!=null||p.GetComponent<Canvas>()!=null||p.GetComponent<SkierRuntime>()!=null){skip=true;break;}
                if(skip)continue;
                for(var p=t.parent;p!=null;p=p.parent)if(src.Contains(p)&&HasGeometry(p)&&!MountainManagementCheckpointBridge.OwnedTerrain(src,p)){skip=true;break;}
                if(!skip)result.Add(t);
            }
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.name,b.name));return result;
        }
        public bool SaveCheckpoint()
        {
            try{var d=CaptureCheckpoint();var store=Store;store.Save(d,LoadedSaveRevision);LoadedSaveRevision=d.revision;LastResult=store.LastResult;return true;}
            catch(Exception e){LastResult="Sauvegarde refusée : "+e.Message;Debug.LogWarning(LastResult,this);return false;}
        }
        public bool LoadCheckpoint(){return LoadCheckpoint(string.IsNullOrEmpty(loadDomainId)?LoadedDomainId:loadDomainId);}
        public bool LoadCheckpoint(string id)
        {
            try{EnsureQuiet(includeActiveLiftTraffic391);var store=Store;var d=store.Load(id);RestoreCheckpointWithRelief(d);LastResult=store.LastResult+" · domaine et trafic disponible restaurés";return true;}
            catch(Exception e){LastResult="Chargement refusé : "+e.Message;Debug.LogWarning(LastResult,this);return false;}
        }
        static T Definition<T>(string json,List<UnityEngine.Object> owned) where T:ScriptableObject
        {var def=ScriptableObject.CreateInstance<T>();owned.Add(def);JsonUtility.FromJsonOverwrite(json,def);return def;}
        public void RestoreCheckpoint(ManagementSaveData261 d){RestoreCheckpoint(d,false);}
        public void RestoreCheckpointWithRelief(ManagementSaveData261 d){RestoreCheckpoint(d,true);}
        internal AlpineEmpires.AlpineSession RestoreAlpineCheckpoint(ManagementSaveData261 d)
        {
            var state=AlpineMaterialCheckpoint261.Read(d);
            if(state==null)throw new ArgumentException("Checkpoint Alpine Empires attendu.");
            var world=AlpineEmpires.AlpineSession.RestoreWorld(state.world);
            RestoreCheckpoint(d,false,state);
            return world;
        }
        void RestoreCheckpoint(ManagementSaveData261 d,bool installSavedRelief,AlpineMaterialCheckpoint261 alpine=null)
        {
            if(d?.schema==ManagementSaveData261.AlpineSchema&&alpine==null)
                throw new NotSupportedException("Ouvrez ce checkpoint depuis Alpine Empires.");
            EnsureQuiet(includeActiveLiftTraffic391&&alpine==null,alpine!=null);ManagementSaveValidation261.Validate(d);
            if (alpine != null && d.sourceScene != source.gameObject.scene.name)
                throw new NotSupportedException("Chargez la carte d’origine du checkpoint Alpine Empires.");
            var road=alpine!=null?AlpineMaterialCheckpoint261.RequireRoad(source):null;
            if(road!=null&&AlpineMaterialCheckpoint261.RoadHash(road)!=alpine.roadSha256)
                throw new NotSupportedException("La route permanente est différente ; restauration refusée.");
            if (alpine != null && d.sourceScene == ValAlpin.ValAlpinScene.SceneName)
                alpine.ValidateValAlpinTerrainBase();
            var oldPaths=alpine!=null?AlpineMaterialCheckpoint261.Paths(source):null;
            var registeredPaths=new List<GameObject>();
            var oldTrafficActors391=LiftTrafficPersistence391.Actors(source);
            if(oldTrafficActors391.Count>0&&d.traffic391==null)throw new NotSupportedException("Ancien checkpoint sans visiteurs : charger depuis un domaine vide pour ne perdre aucun visiteur actif");
            if(RecommendationConstructionRuntime389.I!=null&&RecommendationConstructionRuntime389.I.Current!=null)throw new InvalidOperationException("Fermer l'édition du brouillon avant de charger le domaine");
            var oldConstruction389=RecommendationConstructionPersistence389.Capture(source);
            var oldOperations390=StationOperationsPersistence390.Capture(source);
            var rollbackGoals391=MultiLiftDestinationPlanner301.I?.CaptureTrafficRollback391();
            var rollbackObjectives391=DestinationObjectiveRuntime3782.I?.CaptureTrafficRollback391();
            var rollbackMeasurements391=RecommendationConstructionMeasurements389.I?.CaptureRollback391();
            var rollbackPistes393=SlopeTrafficRuntime393.Ensure().CaptureRollback393();
            var randomState391=UnityEngine.Random.state;
            if(!installSavedRelief)ManagementTerrainCheckpointBridge.RequireInstalled(source,d);
            if(!installSavedRelief)MountainManagementCheckpointBridge.RequireInstalled(source,d);
            // Same shared ledgers: no second station balance. An unrelated domain's active visitors are unsafe too.
            foreach(var visitor in SkierRuntime.Active)if(!OperationsRules390.InDomain(source,visitor))throw new NotSupportedException("Visiteurs d'un autre domaine : remplacement des registres globaux refusé");
            if(d.hasStation&&StationRuntime.I==null)throw new NotSupportedException("StationRuntime Gestion doit être chargé avant sa restauration");
            if(d.finance!=null&&FinanceRuntime.I==null)throw new NotSupportedException("FinanceRuntime Gestion doit être chargé avant sa restauration");
            if(d.hasClock&&GameClock.I!=null&&!source.Contains(GameClock.I))throw new NotSupportedException("Horloge globale appartenant à un autre scope Gestion");
            if(ResortRouteNetwork.I!=null&&!source.Contains(ResortRouteNetwork.I))throw new NotSupportedException("Graphe global appartenant à un autre scope Gestion");
            var terrainRelief=alpine?.PrepareValAlpinTerrainRestore()
                ?? (installSavedRelief?ManagementTerrainCheckpointBridge.PrepareRestore(source,d):null);
            var relief=installSavedRelief?MountainManagementCheckpointBridge.PrepareRestore(source,d):null;
            var codec=new ManagementGeometryCodec261(d,assets);var definitions=new List<UnityEngine.Object>();GameObject next=null;
            var oldObjects=new List<GameObject>();var oldStates=new List<bool>();var oldConnectors=new List<SlopeLiftConnectorRuntime>();var oldSlopes=new List<SlopeRuntime>();var oldLifts=new List<LiftRuntime>();var openLifts=new List<bool>();
            var oldIdentity262=SkiStation.Arcade.StationIdentity262.Read(source)?.Clone();
            var oldRoot=source.geometryRoot;var oldFront=source.frontDeNeige;string oldId=source.DomainId,oldRevision=LoadedSaveRevision;
            var clock=source.Contains(GameClock.I)?GameClock.I:null;string oldClock=clock!=null?JsonUtility.ToJson(clock):null;int oldDay=clock!=null?clock.CurrentDay:1;float oldHour=clock!=null?clock.CurrentHour:6;bool oldClockEnabled=clock!=null&&clock.enabled;
            string oldStation=StationRuntime.I!=null?JsonUtility.ToJson(StationRuntime.I.State):null;var oldFinance=FinanceRuntime.I!=null?FinanceRuntime.I.CaptureCheckpoint261():null;
            var graph=ResortRouteNetwork.I;string oldGraph=graph!=null?JsonUtility.ToJson(graph):null;Transform clockParent=clock!=null?clock.transform.parent:null,graphParent=graph!=null?graph.transform.parent:null;
            source.CaptureState261(out long sourceRevision,out string sourceHash);
            bool changed=false,commitStarted=false;float oldTime=Time.timeScale;
            try
            {
                SkiStation.Arcade.ArcadeReturnLoadAudit.BeforeLoad(source);
                checkpointTransaction=true;Time.timeScale=0;LiftTrafficPersistence391.Replacing=true;
                codec.Prepare();var liftDefs=new List<LiftDefinition>();foreach(var l in d.lifts)liftDefs.Add(Definition<LiftDefinition>(l.definition,definitions));
                ManagementSaveValidation261.ValidateDefinitions(d,liftDefs);
                var serviceDefs=new Dictionary<string,StationServiceDefinition>(StringComparer.Ordinal);foreach(var b in d.buildings)if(!string.IsNullOrEmpty(b.service)){var definition=Definition<StationServiceDefinition>(b.service,definitions);ManagementSaveValidation261.ValidateService(definition);serviceDefs.Add(b.id,definition);}
                var snapshot=source.Capture();if(snapshot.Slopes.Count!=snapshot.ManagementSlopeCount||snapshot.Lifts.Count!=snapshot.ManagementLiftCount||snapshot.Buildings.Count!=snapshot.ManagementBuildingCount||snapshot.Connectors.Count!=snapshot.ManagementConnectorCount)throw new InvalidOperationException("Mappings de la source actuelle incomplets");var elements=new HashSet<Transform>();foreach(var s in snapshot.Slopes){oldSlopes.Add(s.Source);elements.Add(s.Source.transform);}foreach(var l in snapshot.Lifts){oldLifts.Add(l.Source);elements.Add(l.Source.transform);}foreach(var b in snapshot.Buildings)elements.Add(b.Source.transform);
                foreach(var c in UnityEngine.Object.FindObjectsByType<SlopeLiftConnectorRuntime>(FindObjectsSortMode.None))if(source.Contains(c)){oldConnectors.Add(c);elements.Add(c.transform);}
                foreach(var t in elements)oldObjects.Add(t.gameObject);
                var skip=alpine!=null?AlpineMaterialCheckpoint261.SkipScenery(road,oldPaths):null;
                foreach(var t in SceneryRoots(source,elements,skip))oldObjects.Add(t.gameObject);
                if(oldPaths!=null)foreach(var path in oldPaths)if(!oldObjects.Contains(path.Visual))oldObjects.Add(path.Visual);
                foreach(var actor in oldTrafficActors391)if(!oldObjects.Contains(actor))oldObjects.Add(actor);
                foreach(var site in ConstructionSiteRuntime376.Active)if(site!=null&&site.IsRecommendation389&&source.Contains(site))oldObjects.Add(site.gameObject);
                foreach(var go in oldObjects)oldStates.Add(go.activeSelf);foreach(var l in oldLifts)openLifts.Add(l.Open);
                terrainRelief?.Apply();relief?.Apply();ManagementTerrainCheckpointBridge.RequireInstalled(source,d);MountainManagementCheckpointBridge.RequireInstalled(source,d);
                next=new GameObject("Management checkpoint "+d.domainId);SceneManager.MoveGameObjectToScene(next,source.gameObject.scene);next.SetActive(false);
                if(oldRoot!=null){next.transform.SetParent(oldRoot,true);var matrix=next.transform.localToWorldMatrix;for(int k=0;k<16;k++)if(Mathf.Abs(matrix[k]-Matrix4x4.identity[k])>.001f)throw new NotSupportedException("Racine Gestion cisaillée : rechargement matériel refusé");}
                var scenery=alpine!=null?new List<GameObject>(d.scenery.Count):null;
                foreach(var n in d.scenery)
                {
                    var restoredVisual=codec.Restore(n,next.transform);
                    scenery?.Add(restoredVisual);
                }
                var map=new Dictionary<string,GameObject>(StringComparer.Ordinal);
                foreach(var s in d.slopes){var go=codec.Restore(s.visual,next.transform);go.SetActive(false);go.AddComponent<ManagementElementIdentity261>().Restore(s.id);var slope=go.AddComponent<SlopeRuntime>();slope.Build(s.points,s.widths);if(s.designed)slope.SetDesignedDifficulty(s.difficulty);slope.SetOpen(s.open);slope.SetGroomed(s.groomed);slope.RestoreCheckpointStats261(s.runs,s.popularity);if(!string.IsNullOrEmpty(s.maintenance))JsonUtility.FromJsonOverwrite(s.maintenance,go.AddComponent<SlopeMaintenanceRuntime>());map.Add(s.id,go);}
                foreach(var c in d.connectors){var go=codec.Restore(c.visual,next.transform);go.SetActive(false);go.AddComponent<ManagementElementIdentity261>().Restore(c.id);map.Add(c.id,go);}
                foreach(var b in d.buildings)
                {
                    var go=codec.Restore(b.visual,next.transform);go.SetActive(false);go.AddComponent<ManagementElementIdentity261>().Restore(b.id);
                    if(b.hasStatus)go.AddComponent<StationStructureStatus249>().Current=b.status;
                    StationServiceRuntime service=null;if(serviceDefs.TryGetValue(b.id,out var def)){service=go.AddComponent<StationServiceRuntime>();service.definition=def;
                        service.AlpineCompanyId=b.alpineCompany;service.AlpineTerritoryId=b.alpineTerritory;}
                    if(!string.IsNullOrEmpty(b.accommodation))JsonUtility.FromJsonOverwrite(b.accommodation,go.AddComponent<StationAccommodation249>());
                    if(!string.IsNullOrEmpty(b.shop))JsonUtility.FromJsonOverwrite(b.shop,go.AddComponent<StationEquipmentShop249>());
                    if(b.hasSchool){var school=go.AddComponent<SkiSchoolRuntime>();school.service=service;school.zoneType=b.school.zoneType;school.individualLessons=b.school.individualLessons;school.groupLessons=b.school.groupLessons;school.maxGroupSize=b.school.maxGroupSize;school.instructors=b.school.instructors;school.piouPiouMaxAge=b.school.piouPiouMaxAge;school.adultMinAge=b.school.adultMinAge;}
                    map.Add(b.id,go);
                }
                // Synchronous mode-boundary transaction: old content retained until the new capture succeeds.
                loading=true;Time.timeScale=0;changed=true;
                foreach(var c in oldConnectors)SlopeLiftConnectorNetwork.I?.Unregister(c);
                foreach(var s in oldSlopes){SlopeNetwork.I?.Unregister(s);graph?.Unregister349(s);var node=s.GetComponent<SlopeConnectionRuntime>();if(node!=null)ResortSlopeGraph.I?.Unregister(node);}
                foreach(var l in oldLifts){l.SetOpen(false);graph?.Unregister349(l);}foreach(var go in oldObjects)go.SetActive(false);
                if(clock==null&&d.hasClock)clock=new GameObject("Management restored clock").AddComponent<GameClock>();
                if(clock!=null&&d.hasClock){clock.transform.SetParent(next.transform,true);JsonUtility.FromJsonOverwrite(d.clockConfiguration,clock);clock.RestoreCheckpoint261(d.day,d.hour);clock.enabled=d.clockEnabled;}
                if(graph==null)graph=new GameObject("Management restored graph").AddComponent<ResortRouteNetwork>();graph.transform.SetParent(next.transform,true);if(!string.IsNullOrEmpty(d.graphConfiguration))JsonUtility.FromJsonOverwrite(d.graphConfiguration,graph);
                next.SetActive(true);ManagementSurface.InvalidateCheckpoint261();Physics.SyncTransforms();
                for(int i=0;i<d.lifts.Count;i++)
                {var l=d.lifts[i];var go=new GameObject(l.title);go.transform.SetParent(next.transform,false);go.AddComponent<ManagementElementIdentity261>().Restore(l.id);var lift=go.AddComponent<LiftRuntime>();var slots391=d.traffic391!=null?d.traffic391.lifts.Find(x=>x.id==l.id)?.slotSettings:null;lift.Build(liftDefs[i],l.route,l.underground,slots391);lift.SetFaulted(l.faulted);lift.SetOpen(l.open);map.Add(l.id,go);}
                foreach(var s in d.slopes){var go=map[s.id];go.SetActive(s.visual.active);var slope=go.GetComponent<SlopeRuntime>();SlopeNetwork.I?.Register(slope);graph.RegisterSlope(slope);var connection=go.AddComponent<SlopeConnectionRuntime>();connection.Initialize(slope);RestoreLinks(connection,s.top,true,map);RestoreLinks(connection,s.bottom,false,map);RestoreClosures(slope,s.closures);}
                foreach(var c in d.connectors){var go=map[c.id];var connector=go.AddComponent<SlopeLiftConnectorRuntime>();connector.RestoreCheckpoint261(map[c.slopeId].GetComponent<SlopeRuntime>(),map[c.liftId].GetComponent<LiftRuntime>(),c.entry,c.route,c.width,c.open);go.SetActive(true);}
                foreach(var b in d.buildings)map[b.id].SetActive(b.visual.active);
                foreach(var l in d.lifts)RestoreClosures(map[l.id].GetComponent<LiftRuntime>(),l.closures);
                if(d.hasFront){var front=new GameObject("Front de neige restauré Gestion");front.transform.SetParent(next.transform,false);front.transform.position=d.front;source.frontDeNeige=front.transform;}else source.frontDeNeige=null;
                if(d.hasStation)JsonUtility.FromJsonOverwrite(d.stationState,StationRuntime.I.State);if(d.finance!=null)FinanceRuntime.I.RestoreCheckpoint261(d.finance);
                source.GetComponent<SkiStation.TerrainGeneration.TerrainGenerationWorld>()?.AdoptManagedFront(source.frontDeNeige);
                source.geometryRoot=oldRoot;source.SetDomainIdentity(d.domainId);source.RestoreCaptureState261(d.domainRevision,"");loading=false;LoadedSaveRevision=d.revision;loadDomainId=d.domainId;
                var restored=source.Capture();if(restored.Slopes.Count!=d.slopes.Count||restored.Lifts.Count!=d.lifts.Count||restored.Buildings.Count!=d.buildings.Count)throw new InvalidOperationException("Domaine restauré incomplet");
                int restoredConnectors=0;foreach(var c in UnityEngine.Object.FindObjectsByType<SlopeLiftConnectorRuntime>(FindObjectsSortMode.None))if(source.Contains(c))restoredConnectors++;
                if(restoredConnectors!=d.connectors.Count)throw new InvalidOperationException("Raccords restaurés incomplets");
                SkiStation.Arcade.StationIdentity262.Ensure(source).Restore(d.stationIdentity);
                terrainRelief?.VerifyInstalled();relief?.VerifyInstalled();
                RecommendationConstructionPersistence389.Restore(source,d.construction389,next.transform);
                if(alpine!=null)
                    foreach(var path in alpine.accesses)
                    {
                        var building=map[path.buildingId];
                        StationPedestrianNetwork249.GetOrCreate().RegisterCheckpoint249(building,scenery[path.sceneryIndex],path.points);
                        registeredPaths.Add(building);
                    }
                LiftTrafficPersistence391.Restore(d.traffic391,source,codec,assets,next.transform,map);
                StationOperationsPersistence390.Restore(source,d.operations390);
                // Commit only after the new identities, native occupancy and graph are validated.
                commitStarted=true;
                if(oldPaths!=null)foreach(var path in oldPaths)StationPedestrianNetwork249.I?.Unregister349(path.Building);
                VisitorVisualPool392.ClearForCheckpoint392();
                foreach(var lift in oldLifts)foreach(var vehicle in LiftTrafficPersistence391.Fleet(lift))vehicle.RetireCheckpointCopy391();
                foreach(var s in oldSlopes)OperationalClosureRegistry381.I?.ForgetCheckpointTarget261(s);foreach(var l in oldLifts)OperationalClosureRegistry381.I?.ForgetCheckpointTarget261(l);
                foreach(var go in oldObjects)ManagementGeometryCodec261.Release(go);
                if(ownedRoot!=null)ManagementGeometryCodec261.Release(ownedRoot);ownedGeometry?.Dispose();foreach(var def in ownedDefinitions)ManagementGeometryCodec261.Release(def);ownedDefinitions.Clear();
                ownedRoot=next;ownedGeometry=codec;ownedDefinitions.AddRange(definitions);next=null;codec=null;definitions.Clear();
                terrainRelief?.Commit();relief?.Commit();
                if(d.hasReturnWorksCosts)ReturnConstructionRuntime.MarkPaid(source);
            }
            catch(Exception failure)
            {
                SkiStation.Arcade.ArcadeReturnLoadAudit.LoadFailed(source);
                if(commitStarted)
                {
                    terrainRelief?.MarkUncertain();relief?.MarkUncertain();ReturnConstructionRuntime.MarkFault(source);
                    throw new InvalidOperationException("Validation finale du checkpoint interrompue : recharger le domaine",failure);
                }
                Exception recovery=null;
                try
                {
                    foreach(var building in registeredPaths)StationPedestrianNetwork249.I?.Unregister349(building);
                    if(changed)
                    {
                        if(next!=null)foreach(var c in next.GetComponentsInChildren<SlopeLiftConnectorRuntime>(true))SlopeLiftConnectorNetwork.I?.Unregister(c);
                        if(next!=null)foreach(var s in next.GetComponentsInChildren<SlopeRuntime>(true)){SlopeNetwork.I?.Unregister(s);graph?.Unregister349(s);var node=s.GetComponent<SlopeConnectionRuntime>();if(node!=null)ResortSlopeGraph.I?.Unregister(node);OperationalClosureRegistry381.I?.ForgetCheckpointTarget261(s);}
                        if(next!=null){next.SetActive(false);}if(next!=null)foreach(var l in next.GetComponentsInChildren<LiftRuntime>(true)){l.SetOpen(false);graph?.Unregister349(l);OperationalClosureRegistry381.I?.ForgetCheckpointTarget261(l);}
                        if(clock!=null&&oldClock==null&&clock.transform.parent!=next?.transform)ManagementGeometryCodec261.Release(clock.gameObject);
                        if(clock!=null&&oldClock!=null){clock.transform.SetParent(clockParent,true);JsonUtility.FromJsonOverwrite(oldClock,clock);clock.RestoreCheckpoint261(oldDay,oldHour);clock.enabled=oldClockEnabled;}
                        if(graph!=null&&oldGraph!=null){graph.transform.SetParent(graphParent,true);JsonUtility.FromJsonOverwrite(oldGraph,graph);}
                        source.geometryRoot=oldRoot;source.frontDeNeige=oldFront;source.GetComponent<SkiStation.TerrainGeneration.TerrainGenerationWorld>()?.AdoptManagedFront(oldFront);source.SetDomainIdentity(oldId);source.RestoreCaptureState261(sourceRevision,sourceHash);LoadedSaveRevision=oldRevision;loadDomainId=oldId;
                        SkiStation.Arcade.StationIdentity262.Ensure(source).Restore(oldIdentity262??SkiStation.Arcade.StationIdentity262.Default(oldId));
                        for(int i=0;i<oldObjects.Count;i++)if(oldObjects[i]!=null)oldObjects[i].SetActive(oldStates[i]);foreach(var c in oldConnectors)SlopeLiftConnectorNetwork.I?.Register(c);foreach(var s in oldSlopes){SlopeNetwork.I?.Register(s);graph?.RegisterSlope(s);var node=s.GetComponent<SlopeConnectionRuntime>();if(node!=null)ResortSlopeGraph.I?.Register(node);}for(int i=0;i<oldLifts.Count;i++){oldLifts[i].SetOpen(openLifts[i]);graph?.RegisterLift(oldLifts[i]);}
                        if(oldStation!=null)JsonUtility.FromJsonOverwrite(oldStation,StationRuntime.I.State);if(oldFinance!=null)FinanceRuntime.I.RestoreCheckpoint261(oldFinance);
                        RecommendationConstructionRuntime389.I?.ReplaceRecords(source,oldConstruction389);
                        StationOperationsPersistence390.Restore(source,oldOperations390);
                        rollbackGoals391?.Invoke();rollbackObjectives391?.Invoke();rollbackMeasurements391?.Invoke();rollbackPistes393?.Invoke();StationVisitorDemandRuntime.I?.RecountTraffic391();
                    }
                }
                catch(Exception e){recovery=e;}
                try{terrainRelief?.Rollback();}catch(Exception e){recovery=recovery==null?e:new AggregateException(recovery,e);}
                try{relief?.Rollback();}catch(Exception e){recovery=recovery==null?e:new AggregateException(recovery,e);}
                if(recovery!=null){ReturnConstructionRuntime.MarkFault(source);throw new InvalidOperationException("Chargement échoué et restauration incomplète : recharger le domaine",new AggregateException(failure,recovery));}
                throw;
            }
            finally{loading=false;checkpointTransaction=false;Time.timeScale=oldTime;if(next!=null){next.SetActive(false);ManagementGeometryCodec261.Release(next);}codec?.Dispose();foreach(var def in definitions)ManagementGeometryCodec261.Release(def);ManagementSurface.InvalidateCheckpoint261();Physics.SyncTransforms();UnityEngine.Random.state=randomState391;LiftTrafficPersistence391.Replacing=false;}
            SkiStation.Arcade.ArcadeReturnLoadAudit.AfterLoad(source,LoadedSaveRevision);
            // Rebuilding a loaded graph is not a new closure/reroute incident.
            LiftTrafficPersistence391.Replacing=true;
            try{LiveSkiDomainNetwork302.I?.ForceRefresh();}finally{LiftTrafficPersistence391.Replacing=false;}
            LiftStationSignals392.CheckpointCompleted392();
        }
        static void RestoreClosures(Component c,List<ManagementClosure261> closures)
        {if(closures.Count==0)return;var registry=OperationalClosureRegistry381.I;if(registry==null)registry=new GameObject("OperationalClosureRegistry381").AddComponent<OperationalClosureRegistry381>();foreach(var v in closures){if(c is SlopeRuntime s)registry.SetSlopeClosure(s,v.source,true,v.reason);else if(c is LiftRuntime l)registry.SetLiftClosure(l,v.source,true,v.reason);}}
        static void RestoreLinks(SlopeConnectionRuntime connection,List<ManagementSlopeLink261> links,bool top,Dictionary<string,GameObject> map)
        {
            foreach(var l in links)
            {var target=string.IsNullOrEmpty(l.target)?null:map[l.target];if(target!=null){if(l.kind==SlopeEndpointKind.SlopeTop||l.kind==SlopeEndpointKind.SlopeBottom){if(top)connection.AddTopSlope(target.GetComponent<SlopeRuntime>(),l.distance);else connection.AddBottomSlope(target.GetComponent<SlopeRuntime>(),l.distance);}else{if(top)connection.AddTopTarget(l.kind,target,l.distance);else connection.AddBottomTarget(l.kind,target,l.distance);}}else if(l.kind==SlopeEndpointKind.ResortPoint){if(top)connection.SetTopTarget(l.kind,null,l.distance);else connection.SetBottomTarget(l.kind,null,l.distance);}}
        }
        void OnDestroy(){ownedGeometry?.Dispose();foreach(var def in ownedDefinitions)ManagementGeometryCodec261.Release(def);}
    }
}
