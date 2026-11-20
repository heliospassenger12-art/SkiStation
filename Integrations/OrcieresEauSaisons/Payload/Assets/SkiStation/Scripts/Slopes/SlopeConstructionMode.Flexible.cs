using System.Collections.Generic;
using UnityEngine;
namespace SkiStation
{
    public sealed partial class SlopeConstructionMode
    {
        [SerializeField] bool flexibleCurves=true,automaticPisteSnap=true;
        readonly List<SlopeDraftSnap> flexibleSnaps=new List<SlopeDraftSnap>();
        SlopeDraftSnap hoverSnap,pendingStartSnap,pendingEndSnap;
        SlopeRuntime[] snapCandidates=System.Array.Empty<SlopeRuntime>();
        float nextCandidateScan,nextFlexiblePreview;
        int draggedControl=-1,selectedControl=-1,hoveredControl=-1,draftRevision,previewRevision=-1;
        bool moveFlexiblePoints;
        Vector3 rawFlexibleHover,flexibleDragOffset;
        public bool IsDraggingFlexiblePoint=>drawing&&FlexibleAuthoring&&draggedControl>=0&&Input.GetMouseButton(0);
        Vector3 lastPreviewHover;
        bool lastPreviewHasHover,flexiblePreviewValid;
        GameObject flexiblePreviewRoot;Mesh flexiblePreviewMesh;Material flexiblePreviewMaterial;
        string flexibleHint="";
        bool FlexibleAuthoring=>RecommendationDraft389==null&&!Arcade.ArcadeSceneIsolation.IsActive&&Arcade.ArcadeSkiSession.ActiveSession==null;
        public string FlexibleConnectionSummary=>pendingPositions==null?"":
            "Raccord départ : "+pendingStartSnap.Label+"\nRaccord arrivée : "+pendingEndSnap.Label+
            "\nConnexions disponibles après construction et ouverture, selon le GPS et les niveaux natifs.";
        void SyncFlexibleSnaps()
        {
            while(flexibleSnaps.Count>points.Count)flexibleSnaps.RemoveAt(flexibleSnaps.Count-1);
            while(flexibleSnaps.Count<points.Count)flexibleSnaps.Add(default);
        }
        void DirtyFlexible(){draftRevision++;nextFlexiblePreview=0;valAlpinRefusal=null;flexibleHint="";}
        void SnapFlexibleHover()
        {
            hoverSnap=default;if(!FlexibleAuthoring||!hasHover||!automaticPisteSnap||Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt))return;
            if(Time.unscaledTime>=nextCandidateScan)
            {snapCandidates=FindObjectsByType<SlopeRuntime>(FindObjectsSortMode.InstanceID);nextCandidateScan=Time.unscaledTime+.5f;}
            if(SlopeDraftSnapping.TryFind(hoverPoint,snapCandidates,this,out hoverSnap))hoverPoint=hoverSnap.position;
        }
        void AddFlexibleMousePoint()
        {
            int before=points.Count;AddPoint(hoverPoint);SyncFlexibleSnaps();
            if(points.Count>before){flexibleSnaps[points.Count-1]=hoverSnap;selectedControl=-1;DirtyFlexible();}
        }
        void ReleaseFlexibleDrag()
        {if(Input.GetMouseButtonUp(0)||!drawing)draggedControl=-1;}
        // Same screen coordinates for drawing and picking. A direct drag on a
        // numbered handle works without a modifier; the mode/Shift enlarge capture.
        public static int PickFlexibleControl(Camera camera,IReadOnlyList<SlopeControlPoint> controls,Vector2 mouse,float radius)
        {
            if(camera==null||controls==null)return -1;float best=radius*radius;int found=-1;
            for(int i=0;i<controls.Count;i++)
            {
                var screen=camera.WorldToScreenPoint(controls[i].position);
                if(screen.z<=0)continue;float distance=((Vector2)screen-mouse).sqrMagnitude;
                if(distance<best){best=distance;found=i;}
            }
            return found;
        }
        bool EditFlexibleControl()
        {
            if(!FlexibleAuthoring)return false;
            bool shift=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
            bool edit=moveFlexiblePoints||shift;
            hoveredControl=PickFlexibleControl(buildCamera,points,(Vector2)Input.mousePosition,edit?32:16);
            if(Input.GetMouseButtonDown(0)&&hoveredControl>=0)
            {
                draggedControl=selectedControl=hoveredControl;currentWidth=points[selectedControl].width;
                flexibleDragOffset=hasHover?points[selectedControl].position-rawFlexibleHover:Vector3.zero;
                flexibleDragOffset.y=0;
            }
            if(draggedControl>=0&&draggedControl<points.Count&&Input.GetMouseButton(0))
            {
                if(hasHover&&ManagementSurface.TryTrail(rawFlexibleHover+flexibleDragOffset,out var ground,this))
                {
                    var snap=default(SlopeDraftSnap);
                    if(automaticPisteSnap&&!Input.GetKey(KeyCode.LeftAlt)&&!Input.GetKey(KeyCode.RightAlt)&&
                        SlopeDraftSnapping.TryFind(ground,snapCandidates,this,out snap))ground=snap.position;
                    SyncFlexibleSnaps();
                    if((points[draggedControl].position-ground).sqrMagnitude>.0001f)
                    {
                        points[draggedControl]=new SlopeControlPoint(ground,points[draggedControl].width);
                        flexibleSnaps[draggedControl]=snap;DirtyFlexible();
                    }
                }
                return true;
            }
            if(edit&&Input.GetMouseButtonDown(0))flexibleHint="Aucun point visé : cliquez sur un numéro pour le déplacer.";
            return edit&&Input.GetMouseButtonDown(0);
        }
        Vector3 SnapDirection(SlopeDraftSnap snap)=>snap.attached?snap.direction:Vector3.zero;
        bool TryFlexibleRoute(bool withHover,out SlopeControlPoint[] route,out string reason)
        {
            SyncFlexibleSnaps();var controls=new List<SlopeControlPoint>(points);
            if(withHover)controls.Add(new SlopeControlPoint(hoverPoint,currentWidth));
            var first=flexibleSnaps.Count>0?flexibleSnaps[0]:default;
            var last=withHover?hoverSnap:flexibleSnaps.Count>0?flexibleSnaps[flexibleSnaps.Count-1]:default;
            bool Query(Vector3 p,out Vector3 g)=>ManagementSurface.TryTrail(p,out g,this);
            return SlopeFlexibleDraftGeometry.TryBuild(controls,flexibleCurves,sampleSpacing,Query,out route,out reason,SnapDirection(first),SnapDirection(last));
        }
        bool PrepareFlexiblePending(out string reason)
        {
            SyncFlexibleSnaps();pendingStartSnap=flexibleSnaps[0];pendingEndSnap=flexibleSnaps[flexibleSnaps.Count-1];
            if(!pendingStartSnap.Validate(out reason)||!pendingEndSnap.Validate(out reason))return false;
            if(!TryFlexibleRoute(false,out var route,out reason))return false;
            pendingPositions=new Vector3[route.Length];pendingWidths=new float[route.Length];
            for(int i=0;i<route.Length;i++){pendingPositions[i]=route[i].position;pendingWidths[i]=route[i].width;}
            return true;
        }
        bool ValidateFlexibleConnections(out string reason)
        {
            reason="";return RecommendationDraft389!=null||pendingStartSnap.Validate(out reason)&&pendingEndSnap.Validate(out reason);
        }
        void HideFlexiblePreview(){flexiblePreviewValid=false;if(flexiblePreviewRoot!=null)flexiblePreviewRoot.SetActive(false);}
        bool RefreshFlexiblePreview()
        {
            if(!FlexibleAuthoring)return false;
            if(preview!=null)preview.positionCount=0;
            bool hover=hasHover&&points.Count>0&&draggedControl<0&&!moveFlexiblePoints&&!Input.GetKey(KeyCode.LeftShift)&&!Input.GetKey(KeyCode.RightShift)&&Vector3.Distance(points[points.Count-1].position,hoverPoint)>.5f;
            if(!drawing||points.Count+(hover?1:0)<2){HideFlexiblePreview();return true;}
            if(flexiblePreviewRoot!=null)flexiblePreviewRoot.SetActive(flexiblePreviewValid);
            if(Time.unscaledTime<nextFlexiblePreview)return true;
            if(previewRevision==draftRevision&&hover==lastPreviewHasHover&&(!hover||(hoverPoint-lastPreviewHover).sqrMagnitude<.0625f))return true;
            nextFlexiblePreview=Time.unscaledTime+.15f;previewRevision=draftRevision;lastPreviewHasHover=hover;lastPreviewHover=hoverPoint;
            if(!TryFlexibleRoute(hover,out var route,out flexibleHint)){HideFlexiblePreview();return true;}
            if(flexiblePreviewRoot==null)
            {
                flexiblePreviewRoot=new GameObject("Aperçu de piste souple — sans collision");flexiblePreviewRoot.hideFlags=HideFlags.DontSave;
                flexiblePreviewRoot.layer=2;flexiblePreviewRoot.transform.SetParent(transform,false);
                flexiblePreviewMesh=new Mesh{name="Flexible piste draft"};flexiblePreviewMesh.MarkDynamic();
                flexiblePreviewRoot.AddComponent<MeshFilter>().sharedMesh=flexiblePreviewMesh;
                flexiblePreviewMaterial=RuntimeMaterialFactory.Create(new Color(.12f,.78f,.84f));
                var renderer=flexiblePreviewRoot.AddComponent<MeshRenderer>();renderer.sharedMaterial=flexiblePreviewMaterial;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            // Display LOD only: retain samples of the exact authored curve and
            // both endpoints; final geometry/validation always uses all samples.
            if(route.Length>385)
            {
                var display=new List<SlopeControlPoint>();int stride=Mathf.CeilToInt((route.Length-1)/384f);
                for(int i=0;i<route.Length-1;i+=stride)display.Add(route[i]);display.Add(route[route.Length-1]);route=display.ToArray();
            }
            int n=route.Length;var vertices=new Vector3[n*3];var triangles=new int[(n-1)*12];
            for(int i=0;i<n;i++)
            {
                var forward=route[Mathf.Min(n-1,i+1)].position-route[Mathf.Max(0,i-1)].position;forward.y=0;
                var side=Vector3.Cross(Vector3.up,forward.normalized);
                for(int col=0;col<3;col++)
                {
                    var p=route[i].position+side*((col-1)*route[i].width*.5f);
                    if(!ManagementSurface.TryTrail(p,out var g,this)){HideFlexiblePreview();flexibleHint="Bord de piste hors terrain : modifier le tracé.";return true;}
                    vertices[i*3+col]=flexiblePreviewRoot.transform.InverseTransformPoint(g+Vector3.up*.35f);
                    if(i<n-1&&col<2){int t=(i*2+col)*6,v=i*3+col;triangles[t]=v;triangles[t+1]=v+3;triangles[t+2]=v+1;triangles[t+3]=v+1;triangles[t+4]=v+3;triangles[t+5]=v+4;}
                }
            }
            flexiblePreviewMesh.Clear();flexiblePreviewMesh.vertices=vertices;flexiblePreviewMesh.triangles=triangles;
            flexiblePreviewMesh.RecalculateNormals();flexiblePreviewMesh.RecalculateBounds();flexiblePreviewValid=true;flexiblePreviewRoot.SetActive(true);return true;
        }
        void DrawFlexibleToolbar(Rect panel)
        {
            if(!FlexibleAuthoring)return;
            var area=new Rect(panel.x,panel.yMax+4,panel.width,148);ConstructionInput349.PanelArea(this,"flexible-piste",area);
            GUILayout.BeginArea(area,GUI.skin.box);GUILayout.BeginHorizontal();
            bool curves=GUILayout.Toggle(flexibleCurves,"Courbes souples"),snap=GUILayout.Toggle(automaticPisteSnap,"Raccord automatique");
            if(curves!=flexibleCurves||snap!=automaticPisteSnap){flexibleCurves=curves;automaticPisteSnap=snap;hoverSnap=default;DirtyFlexible();}
            GUILayout.EndHorizontal();
            bool move=GUILayout.Toggle(moveFlexiblePoints,moveFlexiblePoints?"DÉPLACER LES POINTS — actif (cliquer-glisser un numéro)":"Déplacer les points (sans touche Maj)",GUI.skin.button);
            if(move!=moveFlexiblePoints){moveFlexiblePoints=move;draggedControl=-1;DirtyFlexible();}
            GUILayout.Label("Glisser un numéro ou Maj + glisser · Alt : terrain libre · Clic droit : retirer le dernier point");
            GUILayout.BeginHorizontal();GUILayout.Label("Largeur "+(selectedControl>=0?"du point "+(selectedControl+1):"suivant")+" : "+currentWidth.ToString("0")+" m",GUILayout.Width(220));
            float width=GUILayout.HorizontalSlider(currentWidth,minWidth,maxWidth);if(Mathf.Abs(width-currentWidth)>.05f)SetWidth(width);GUILayout.EndHorizontal();
            GUILayout.Label(!string.IsNullOrEmpty(flexibleHint)?flexibleHint:hoverSnap.attached?"Raccord visé : "+hoverSnap.Label+" (extrémité du tracé)":"Départ/arrivée sur une piste : bifurcation ou convergence. Entrée : récapitulatif.");
            GUILayout.EndArea();
            var action=new Rect(panel.x,area.yMax+4,panel.width,32);ConstructionInput349.PanelArea(this,"flexible-piste-finish",action);
            bool prior=GUI.enabled;GUI.enabled=prior&&points.Count>=2&&!ConstructionInput349.ModalOpen;
            if(GUI.Button(action,"TERMINER LE TRACÉ → RÉCAPITULATIF"))Finish();GUI.enabled=prior;
            if(!string.IsNullOrEmpty(valAlpinRefusal)){var error=new Rect(panel.x,action.yMax+4,panel.width,48);ConstructionInput349.PanelArea(this,"flexible-piste-error",error);GUI.Box(error,valAlpinRefusal);}
            for(int i=0;i<points.Count;i++)
            {
                var screen=buildCamera!=null?buildCamera.WorldToScreenPoint(points[i].position):Vector3.zero;if(screen.z<=0)continue;
                var priorColor=GUI.backgroundColor;
                if(i==selectedControl||i==hoveredControl)GUI.backgroundColor=Color.yellow;
                GUI.Box(new Rect(screen.x-15,Screen.height-screen.y-15,30,30),(i+1).ToString());GUI.backgroundColor=priorColor;
            }
        }
        void ResetFlexibleDraft()
        {flexibleSnaps.Clear();hoverSnap=pendingStartSnap=pendingEndSnap=default;draggedControl=selectedControl=hoveredControl=-1;moveFlexiblePoints=false;flexibleDragOffset=Vector3.zero;flexibleHint="";HideFlexiblePreview();DirtyFlexible();}
        void DisposeFlexibleDraft()
        {if(flexiblePreviewRoot!=null)Destroy(flexiblePreviewRoot);if(flexiblePreviewMesh!=null)Destroy(flexiblePreviewMesh);if(flexiblePreviewMaterial!=null)Destroy(flexiblePreviewMaterial);}
    }
}
