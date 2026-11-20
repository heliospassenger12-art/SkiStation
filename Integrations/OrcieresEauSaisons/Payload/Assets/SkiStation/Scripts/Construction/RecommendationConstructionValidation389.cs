using System;
using System.Collections.Generic;
using UnityEngine;
namespace SkiStation
{
    public static class RecommendationConstructionValidation389
    {
        public static ManagementDomainSource261 Domain(string id)
        {ManagementDomainSource261 found=null;foreach(var d in UnityEngine.Object.FindObjectsByType<ManagementDomainSource261>(FindObjectsSortMode.None))if(d.DomainId==id){if(found!=null)return null;found=d;}return found;}
        public static bool Validate(RecommendationConstructionProject389 d,out string reason)
        {
            reason="Domaine / projet absent";
            var source=d!=null?Domain(d.DomainId):null;
            if(source==null||ReturnConstructionRuntime.DomainFaulted(source)||!RecommendationConstructionRules389.ValidControls(d.controls))return false;
            if(!ManagementDomainSource261.ValidRoute(d.route)){reason="Route réelle invalide / hors budget";return false;}
            if(!Altitude.AltitudeConstructionPolicy.ValidateRoute(d.route,d.isLift?5:d.width*.5f,out reason,source))return false;
            foreach(var p in d.controls)
                if(!ManagementSurface.TryGet(p,out var g)||Mathf.Abs(p.y-g.y)>.5f){reason="Terrain modifié : replacer un point du projet";return false;}
            if(d.isLift)
            {
                var model=ScriptableObject.CreateInstance<LiftDefinition>();
                try
                {
                    if(string.IsNullOrEmpty(d.liftDefinitionJson)){reason="Modèle absent";return false;}
                    JsonUtility.FromJsonOverwrite(d.liftDefinitionJson,model);
                    if(!LiftCareerCatalog249.IsUnlocked(model.id)){reason="Technologie non débloquée";return false;}
                    if(model.type==LiftType.Funicular){reason="Funiculaire : étude surface/tunnel spécifique requise ; ce handoff ne la remplace pas";return false;}
                    if(!ConstructionRules349.LiftStations(d.controls[0],d.controls[d.controls.Length-1],LiftCareerCatalog249.EffectiveMaxLength(model.id,model.maxLineLengthMeters),out reason))return false;
                    if(d.length>LiftCareerCatalog249.EffectiveMaxLength(model.id,model.maxLineLengthMeters)){reason="Portée totale dépassée";return false;}
                    if(!ConstructionPlacement349.ValidateSite(d.controls[0],new Vector2(13,13),out reason,true)||!ConstructionPlacement349.ValidateSite(d.controls[d.controls.Length-1],new Vector2(13,13),out reason,true))return false;
                    bool surface=NetworkRecommendationPreview.IsSurface(model.type);
                    if(surface&&(d.controls.Length-2>model.maxTowDirectionPoints||(!model.allowTowDirectionChanges&&d.controls.Length>2))){reason="Virages incompatibles avec ce téléski";return false;}
                    if(surface)
                    {
                        if(!TerrainGeneration.RealTerrainWater.AllowsRoute(d.route,10f,out reason,false))return false;
                        var profile=RecommendationGeometryValidator.Profile(d.route,true,false,SlopeDifficulty.Green,true);
                        if(profile.status==PreviewGeometryStatus388.Invalid){reason=profile.warnings.Count>0?profile.warnings[0]:"Téléski incompatible avec le relief";return false;}
                        foreach(var s in UnityEngine.Object.FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.None))
                            if(source.Contains(s)&&ConstructionRules349.SurfaceLiftCrosses(s.Points,s.WidestWidth*.5f,d.route)){reason="Téléski croisant une piste";return false;}
                    }
                    else
                    {
                        for(int i=1;i<d.route.Length;i++)
                        {
                            int samples=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(d.route[i-1],d.route[i])/4f));
                            if(samples>2048){reason="Portée / budget de validation dépassé";return false;}
                            for(int j=0;j<=samples;j++)
                            {
                                var q=Vector3.Lerp(d.route[i-1],d.route[i],j/(float)samples);
                                if(!ManagementSurface.TryGet(q,out var g)){reason="Ligne hors terrain";return false;}
                                if(q.y<g.y-.1f){reason="Câble pénétrant le relief";return false;}
                            }
                        }
                    }
                    if(!LiftGeometryIntegration391.Validate(model,d.route,source,out reason))return false;
                }
                finally{if(Application.isPlaying)UnityEngine.Object.Destroy(model);else UnityEngine.Object.DestroyImmediate(model);}
            }
            else
            {
                if(d.width<4||d.width>28||!Enum.IsDefined(typeof(SlopeConstructionDifficulty),d.difficulty)){reason="Largeur / difficulté hors limites du vrai outil";return false;}
                if(!ConstructionRules349.SlopeRoute(d.route,out reason)||ConstructionClearance249.ConflictsWithSurfaceLift(d.route,d.width*.5f,out reason))return false;
                if(!TerrainGeneration.RealTerrainWater.AllowsRoute(d.route,d.width,out reason,true))return false;
                foreach(var p in d.route)if(!ManagementSurface.TryGet(p,out var g)||Mathf.Abs(g.y-p.y)>.5f){reason="Terrain changé sous la piste";return false;}
                var assessment=SlopeDesignIntelligenceRuntime.AssessRoute389(d.route,(SlopeDifficulty)d.difficulty);
                if(!assessment.valid){reason=assessment.message;return false;}
            }
            if(!ConstructionPlacement349.RouteClearOfBuildings(d.route,d.isLift?3:d.width*.5f,out reason))return false;
            // Reuse 3.88's indexed REAL scenery data against the final native route, not its conceptual straight line.
            var scenery=new RecommendationWorld388();scenery.Refresh();
            bool groundRoute=!d.isLift;
            if(d.isLift){var model=ScriptableObject.CreateInstance<LiftDefinition>();try{JsonUtility.FromJsonOverwrite(d.liftDefinitionJson,model);groundRoute=NetworkRecommendationPreview.IsSurface(model.type);}finally{if(Application.isPlaying)UnityEngine.Object.Destroy(model);else UnityEngine.Object.DestroyImmediate(model);}}
            for(int i=1;i<d.route.Length;i++)
            {
                int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(d.route[i-1],d.route[i])/4));
                if(steps>2048){reason="Budget d'analyse d'obstacles dépassé";return false;}
                for(int j=0;j<=steps;j++)
                {
                    var q=Vector3.Lerp(d.route[i-1],d.route[i],j/(float)steps);
                    foreach(var obstacle in scenery.Near(q))if(!obstacle.soft)
                    {var bounds=obstacle.bounds;bounds.Expand(new Vector3(groundRoute?d.width:2,1,groundRoute?d.width:2));if(bounds.Contains(q+Vector3.up*.5f)){reason=obstacle.label;return false;}}
                    if(!groundRoute)continue;
                    var side=Vector3.Cross(Vector3.up,d.route[i]-d.route[i-1]).normalized*(d.width*.5f);
                    if(!ManagementSurface.TryGet(q+side,out var left)||!ManagementSurface.TryGet(q-side,out var right)||Mathf.Abs(left.y-right.y)>d.width*1.73f)
                    {reason="Emprise hors terrain ou sur falaise / dévers excessif";return false;}
                }
            }
            var runtime=RecommendationConstructionRuntime389.I;
            if(runtime!=null)foreach(var other in runtime.Projects)
            {
                if(other.ProjectId==d.ProjectId||other.DomainId!=d.DomainId||!other.IsWork||other.route==null)continue;
                if(Conflicts(d,other)){reason="CONFLICT — chantier "+other.ProjectId;return false;}
            }
            reason="";return true;
        }
        public static bool Conflicts(RecommendationConstructionProject389 a,RecommendationConstructionProject389 b)
        {
            if(a==null||b==null||a.controls==null||b.controls==null||a.controls.Length<2||b.controls.Length<2||a.route==null||b.route==null)return false;
            var aa=new[]{a.controls[0],a.controls[a.controls.Length-1]};var bb=new[]{b.controls[0],b.controls[b.controls.Length-1]};
            if(a.isLift&&b.isLift)foreach(var x in aa)foreach(var y in bb)if(ConstructionRules349.Overlaps(x,new Vector2(13,13),y,new Vector2(13,13),2))return true;
            for(int i=1;i<a.route.Length;i++)for(int j=1;j<b.route.Length;j++)
                if(ConstructionRules349.SegmentsNearXZ(a.route[i-1],a.route[i],b.route[j-1],b.route[j],a.isLift||b.isLift?4:(a.width+b.width)*.5f))return true;
            return false;
        }
        public static void AccessWarnings(RecommendationConstructionProject389 d)
        {
            var world=new RecommendationWorld388();world.Refresh();
            foreach(var point in new[]{d.controls[0],d.controls[d.controls.Length-1]})
            {
                bool connected=false;
                foreach(var s in world.Slopes)if(s!=null&&s.Open&&(Vector3.Distance(point,s.TopPoint)<32||Vector3.Distance(point,s.BottomPoint)<32))connected=true;
                foreach(var l in world.Lifts)if(l!=null&&l.Open&&(Vector3.Distance(point,l.Top)<32||Vector3.Distance(point,l.Bottom)<32))connected=true;
                if(Vector3.Distance(point,ManagementSurface.Entrance)<100)connected=true;
                if(!connected)d.warnings.Add("Accès / sortie sans raccord terminal proche : liaison à prévoir, aucune liaison créée automatiquement.");
            }
            if(d.isLift)d.warnings.Add("Orientation des gares issue du tracé natif ; génie civil et espace de file à contrôler sur le massif.");
            else d.warnings.Add("Raccords provisoires par extrémités : bifurcations/convergences natives calculées seulement après ouverture.");
            d.warnings.Add("Impact sur files / temps / accessibilité : NON ESTIMÉ tant que les observations ne le permettent pas.");
        }
    }
}
