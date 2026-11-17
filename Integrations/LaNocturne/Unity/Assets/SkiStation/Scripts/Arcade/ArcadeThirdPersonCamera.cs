using UnityEngine;
namespace SkiStation.Arcade
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)] // Real lift pose is updated first, at order -20.
    public sealed partial class ArcadeThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public ArcadeSkiController controller;
        public Vector3 lookOffset=new Vector3(0,.85f,0);
        [Header("Distance — rider proche (metres)")]
        public float distance=5.6f,height=2.15f,extraCameraDistanceAtSpeed=.18f;
        public float followSharpness=9f,rotationSharpness=11f;
        [Header("FOV — vitesses reelles, seuils en km/h")]
        public float normalFieldOfView=68f,cruiseFieldOfView=74f,maximumFieldOfView=80f;
        public float boostFieldOfView=3f,fovResponse=3.5f;
        // Legacy serialized reference retained; new curve uses absolute speed bands.
        public float maximumSpeedForEffects=52f;
        [Header("Trajectoire — horizon stable")]
        public float lookAheadAtSpeed=2.1f,headingDeadZone=2f,maximumOrbitDegreesPerSecond=150f;
        public float maximumHeadingLag=14f,highSpeedHeadingResponse=5f,highSpeedOrbitLimit=90f;
        public float curveSideOffset=.25f,terrainPitchInfluence=.7f;
        [Header("Confort essais A2.63.1.5 — grandes courbes")]
        public bool progressiveTurnCamera=true;
        public float comfortableOrbitSpeed=65f,comfortableOrbitAcceleration=110f,comfortableHeadingResponse=3f;
        float comfortableYawRate;
        [Header("Relief — deux echantillons a 10 Hz")]
        public float terrainProbeDistance=20f,verticalLookAhead=.18f;
        [Header("Vol et reception")]
        public float extraBigAirDistance=.85f,extraBigAirHeight=.45f;
        public float bigAirThreshold=12f,airFovBonus=1.5f,landingFeedback=.065f;
        [Header("Protection — contact et retour amorti")]
        public float collisionRadius=.3f,groundClearance=.5f;
        public float obstructionReleaseDelay=.2f,obstructionRecovery=3.5f;
        public LayerMask occlusionMask=~0;
        [Header("Confort — multiplicateurs, zero desactive")]
        [Range(0f,1f)] public float dynamicFovIntensity=1f,shakeIntensity=1f;
        [Range(0f,1f)] public float framingIntensity=1f;
        // Kept for existing profiles; now an EVENT-only displacement cap.
        public float maximumSpeedVibration=.018f;
        public enum ViewState { Ground,Airborne,Rail,Wallride,Lift,Crash,Stopped,Heli }
        public bool EntrySequenceActive2612 {get;set;}
        public void CompleteEntryPose2612()
        {
            if(target==null)return;
            ResetReliefPreview();
            previousTargetPosition=target.position;initialized=true;comfortableYawRate=0;
            Vector3 heading=Vector3.ProjectOnPlane(target.forward,Vector3.up);
            cameraYaw=filteredYaw=Mathf.Atan2(heading.x,heading.z)*Mathf.Rad2Deg;
            downhillPitch=airBlend=airFramePeak=airAge=terrainLook=terrainTarget=impact=collisionPulse=sideOffset=boostBlend=0;
            wasAirborne=false;airStart=target.position.y;probeAt=0;
            protectedLength=Vector3.Distance(transform.position,target.position+lookOffset);releaseAt=0;
            ResetNocturneFraming();
            EntrySequenceActive2612=false;
        }
        public ViewState State { get; private set; }
        public float SpeedKmh { get; private set; }
        public bool HasBinding { get { return target!=null&&controller!=null&&attachedCamera!=null; } }
        readonly RaycastHit[] hits=new RaycastHit[64];
        readonly Collider[] overlaps=new Collider[32];
        Camera attachedCamera;
        ArcadeLiftRide liftRide;
        ArcadeSkiController subscribedController;
        Vector3 previousTargetPosition;
        float downhillPitch;
        float nocturneApproach,nocturneReveal;

        void UpdateNocturneFraming(float dt,float speed)
        {
            bool skiFrame=State==ViewState.Ground||State==ViewState.Airborne;
            float weight=framingIntensity*(DemoSettings263.Data.reducedMotion?.45f:1f);
            if(!skiFrame||controller==null)
            {
                nocturneApproach=nocturneReveal=0;
                return;
            }
            float low=State==ViewState.Ground&&controller.IsChargingJump?controller.JumpCharge01*weight:0;
            nocturneApproach=Mathf.MoveTowards(nocturneApproach,low,dt*4f);
            float opened=State==ViewState.Airborne
                ? Mathf.SmoothStep(0,1,Mathf.Clamp01(airAge/.34f))*Mathf.InverseLerp(5f,12f,speed)*weight:0;
            nocturneReveal=State==ViewState.Airborne?opened:Mathf.MoveTowards(nocturneReveal,0,dt*weight/.60f);
            if(weight<=0)nocturneApproach=nocturneReveal=0;
        }

        void ResetNocturneFraming(){nocturneApproach=nocturneReveal=0;}
        float cameraYaw,filteredYaw,airBlend,airFramePeak,airStart,airAge,terrainLook,terrainTarget;
        float impact,collisionPulse,previousObstacle,sideOffset,boostBlend,probeAt,protectedLength,releaseAt;
        bool initialized,wasAirborne;
        float liftLookYaw,liftLookPitch,liftLookAt,liftBlend;
        void Awake(){attachedCamera=GetComponent<Camera>();ValidateSettings();}
        void OnValidate(){ValidateSettings();}
        public void ValidateSettings()
        {
            distance=ArcadeGlideRules.Safe(distance,5.6f,3,8);
            height=ArcadeGlideRules.Safe(height,2.15f,1,4);
            maximumSpeedForEffects=ArcadeGlideRules.Safe(maximumSpeedForEffects,52,10,100);
            if(!ArcadeGlideRules.Finite(lookOffset))lookOffset=new Vector3(0,.85f,0);
            lookOffset=Vector3.ClampMagnitude(lookOffset,2);
            normalFieldOfView=ArcadeGlideRules.Safe(normalFieldOfView,68,50,78);
            maximumFieldOfView=ArcadeGlideRules.Safe(maximumFieldOfView,80,normalFieldOfView+2,84);
            cruiseFieldOfView=ArcadeGlideRules.Safe(cruiseFieldOfView,74,normalFieldOfView,maximumFieldOfView-1);
            boostFieldOfView=ArcadeGlideRules.Safe(boostFieldOfView,3,0,4);
            fovResponse=ArcadeGlideRules.Safe(fovResponse,3.5f,1,10);
            followSharpness=ArcadeGlideRules.Safe(followSharpness,9,3,20);
            rotationSharpness=ArcadeGlideRules.Safe(rotationSharpness,11,3,20);
            comfortableOrbitSpeed=ArcadeGlideRules.Safe(comfortableOrbitSpeed,65,25,90);
            comfortableOrbitAcceleration=ArcadeGlideRules.Safe(comfortableOrbitAcceleration,110,40,180);
            comfortableHeadingResponse=ArcadeGlideRules.Safe(comfortableHeadingResponse,3,1,5);
            highSpeedHeadingResponse=ArcadeGlideRules.Safe(highSpeedHeadingResponse,5,2,10);
            maximumOrbitDegreesPerSecond=ArcadeGlideRules.Safe(maximumOrbitDegreesPerSecond,150,45,200);
            highSpeedOrbitLimit=ArcadeGlideRules.Safe(highSpeedOrbitLimit,90,35,maximumOrbitDegreesPerSecond);
            maximumHeadingLag=ArcadeGlideRules.Safe(maximumHeadingLag,14,5,30);
            headingDeadZone=ArcadeGlideRules.Safe(headingDeadZone,2,.5f,6);
            extraCameraDistanceAtSpeed=ArcadeGlideRules.Safe(extraCameraDistanceAtSpeed,.18f,0,.4f);
            lookAheadAtSpeed=ArcadeGlideRules.Safe(lookAheadAtSpeed,2.1f,.3f,2.5f);
            curveSideOffset=ArcadeGlideRules.Safe(curveSideOffset,.25f,0,.45f);
            terrainProbeDistance=ArcadeGlideRules.Safe(terrainProbeDistance,20,8,28);
            verticalLookAhead=ArcadeGlideRules.Safe(verticalLookAhead,.18f,0,.3f);
            terrainPitchInfluence=ArcadeGlideRules.Safe(terrainPitchInfluence,.7f,0,1);
            extraBigAirDistance=ArcadeGlideRules.Safe(extraBigAirDistance,.85f,0,1);
            extraBigAirHeight=ArcadeGlideRules.Safe(extraBigAirHeight,.45f,0,.6f);
            bigAirThreshold=ArcadeGlideRules.Safe(bigAirThreshold,12,5,30);
            airFovBonus=ArcadeGlideRules.Safe(airFovBonus,1.5f,0,2);
            landingFeedback=ArcadeGlideRules.Safe(landingFeedback,.065f,0,.1f);
            maximumSpeedVibration=ArcadeGlideRules.Safe(maximumSpeedVibration,.018f,0,.04f);
            collisionRadius=ArcadeGlideRules.Safe(collisionRadius,.3f,.2f,.5f);
            groundClearance=ArcadeGlideRules.Safe(groundClearance,.5f,collisionRadius+.05f,1);
            obstructionReleaseDelay=ArcadeGlideRules.Safe(obstructionReleaseDelay,.2f,.05f,.5f);
            obstructionRecovery=ArcadeGlideRules.Safe(obstructionRecovery,3.5f,1,8);
            dynamicFovIntensity=ArcadeGlideRules.Safe(dynamicFovIntensity,1,0,1);
            shakeIntensity=ArcadeGlideRules.Safe(shakeIntensity,1,0,1);
            framingIntensity=ArcadeGlideRules.Safe(framingIntensity,1,0,1);
        }
        void OnEnable(){Subscribe();}
        void Start(){Subscribe();if(!initialized)SnapBehind();}
        public void Bind(Transform rider,ArcadeSkiController solver)
        {
            if(target!=rider||controller!=solver){initialized=false;airFramePeak=0;wasAirborne=false;ResetReliefPreview();ResetNocturneFraming();}
            target=rider;controller=solver;Subscribe();
            // The session may just have installed its existing real-lift adapter.
            liftRide=solver!=null?solver.GetComponent<ArcadeLiftRide>():null;
        }
        public void UnbindDomain()
        {
            ResetNocturneFraming();
            ResetHybridCamera26315();
            EntrySequenceActive2612=false;
            ResetReliefPreview();
            Unsubscribe();target=null;controller=null;initialized=false;
            downhillPitch=liftBlend=liftLookYaw=liftLookPitch=liftLookAt=airFramePeak=0;SpeedKmh=0;
            System.Array.Clear(hits,0,hits.Length);System.Array.Clear(overlaps,0,overlaps.Length);
        }
        void Subscribe()
        {
            if(subscribedController==controller)return;
            Unsubscribe();subscribedController=controller;
            liftRide=controller!=null?controller.GetComponent<ArcadeLiftRide>():null;
            if(controller!=null){controller.Retried+=SnapBehind;controller.Landed+=OnLanding;}
        }
        void Unsubscribe()
        {
            if(subscribedController!=null){subscribedController.Retried-=SnapBehind;subscribedController.Landed-=OnLanding;}
            subscribedController=null;liftRide=null;
        }
        void OnDisable(){ResetNocturneFraming();ResetHybridCamera26315();Unsubscribe();initialized=false;ResetReliefPreview();}
        void OnLanding(float force){impact=Mathf.Clamp01((force-6)/24);}
        public void SnapBehind()
        {
            if(target==null)return;
            ResetHybridCamera26315();
            ResetNocturneFraming();
            Subscribe();ValidateSettings();ResetReliefPreview();
            downhillPitch=airBlend=airFramePeak=airAge=terrainLook=terrainTarget=impact=collisionPulse=sideOffset=boostBlend=0;
            previousObstacle=0;wasAirborne=false;probeAt=0;releaseAt=0;protectedLength=distance;comfortableYawRate=0;
            Vector3 heading=controller!=null?Vector3.ProjectOnPlane(controller.TravelVelocity,Vector3.up):Vector3.zero;
            if(heading.sqrMagnitude<.01f)heading=Vector3.ProjectOnPlane(target.forward,Vector3.up);
            if(heading.sqrMagnitude<.01f)heading=Vector3.forward;
            cameraYaw=filteredYaw=Mathf.Atan2(heading.x,heading.z)*Mathf.Rad2Deg;
            previousTargetPosition=target.position;initialized=true;
            Vector3 focus=target.position+lookOffset;
            transform.position=Protect(focus,DesiredPosition(focus,0),transform.position,0,true);
            transform.rotation=Quaternion.LookRotation(focus-transform.position,Vector3.up);
            if(attachedCamera!=null)attachedCamera.fieldOfView=normalFieldOfView;
        }
        bool Ignore(Collider collider)
        {
            if(collider==null||(controller!=null&&!controller.InCurrentDomain(collider)))return true;
            Transform t=collider.transform;
            return t==target||t.IsChildOf(target)||(controller!=null&&controller.IsRidingLift
                &&liftRide!=null&&liftRide.Vehicle!=null&&t.IsChildOf(liftRide.Vehicle.transform));
        }
        bool TerrainHeight(Vector3 point,out float y)
        {
            var alpine=controller!=null?controller.DomainAlpine:null;
            if(alpine!=null&&alpine.IsReady&&alpine.Contains(point.x,point.z))
            {y=alpine.HeightAt(point.x,point.z);return ArcadeGlideRules.Finite(y);}
            int count=Physics.RaycastNonAlloc(point+Vector3.up*35,Vector3.down,hits,175,occlusionMask,QueryTriggerInteraction.Ignore);
            y=float.NegativeInfinity;
            for(int i=0;i<count;i++)
            {
                var h=hits[i];
                if(Ignore(h.collider)||h.normal.y<.25f)continue;
                if(!(h.collider is MeshCollider)&&!(h.collider is TerrainCollider))continue;
                y=Mathf.Max(y,h.point.y);
            }
            return ArcadeGlideRules.Finite(y);
        }
        float ClearLength(Vector3 origin,Vector3 ray)
        {
            float length=ray.magnitude;if(length<.001f)return length;
            int count=Physics.SphereCastNonAlloc(origin,collisionRadius,ray/length,hits,length,occlusionMask,QueryTriggerInteraction.Ignore);
            float allowed=length;
            for(int i=0;i<count;i++)if(!Ignore(hits[i].collider))
                allowed=Mathf.Min(allowed,Mathf.Max(.15f,hits[i].distance-.1f));
            return allowed;
        }
        Vector3 RaiseAboveTerrain(Vector3 p)
        {if(controller!=null&&controller.IsRidingLift&&liftRide!=null&&liftRide.Riding!=null
            &&liftRide.Riding.type==LiftType.Funicular&&liftRide.Riding.source.IsUnderground)return p;
        float y;if(TerrainHeight(p,out y))p.y=Mathf.Max(p.y,y+groundClearance);return p;}
        Vector3 Protect(Vector3 focus,Vector3 desired,Vector3 previous,float dt,bool snap,float recoveryLength=-1f)
        {
            Vector3 ray=desired-focus;float length=ray.magnitude;
            if(length<.01f)return RaiseAboveTerrain(desired);
            float allowed=ClearLength(focus,ray);
            if(allowed<length-.02f)releaseAt=Time.unscaledTime+obstructionReleaseDelay;
            // Recover toward the full framing distance, not an already position-filtered
            // intermediate length. Otherwise two per-frame blends make release FPS-dependent.
            float fullLength=recoveryLength>0&&ArcadeGlideRules.Finite(recoveryLength)?recoveryLength:length;
            float wanted=Time.unscaledTime<releaseAt?Mathf.Min(protectedLength,allowed):fullLength;
            protectedLength=snap?allowed:ArcadeViewRules.RecoverDistance(protectedLength,allowed,wanted,obstructionRecovery,dt);
            Vector3 candidate=RaiseAboveTerrain(focus+ray/length*Mathf.Max(.15f,protectedLength));
            if(!snap)
            {
                // Sweep actual prior world position, not the render-carried translation.
                Vector3 movement=candidate-previous;float travel=movement.magnitude;
                if(travel>.001f&&travel<30)candidate=previous+movement/travel*ClearLength(previous,movement);
                candidate=RaiseAboveTerrain(candidate);
            }
            ray=candidate-focus;length=ray.magnitude;
            if(length>.001f)candidate=focus+ray/length*ClearLength(focus,ray);
            int occupied=Physics.OverlapSphereNonAlloc(candidate,collisionRadius*.9f,overlaps,occlusionMask,QueryTriggerInteraction.Ignore);
            for(int i=0;i<occupied;i++)if(!Ignore(overlaps[i]))
            {
                // Inside-collider guard for newly enabled colliders; ordinary motion is swept.
                if(!snap)return previous;
                break;
            }
            return candidate;
        }
        Vector3 DesiredPosition(Vector3 focus,float speed01)
        {
            Vector3 heading=Quaternion.Euler(liftLookPitch*liftBlend+downhillPitch*(1-liftBlend),cameraYaw+liftLookYaw*liftBlend,0)*Vector3.forward;
            float liftDistance=distance,liftHeight=height;
            if(liftRide!=null&&liftRide.Riding!=null)
            {
                LiftType type=liftRide.Riding.type;
                liftDistance=liftRide.Riding.IsTow?4f:type==LiftType.Funicular?3.5f:type==LiftType.CableCar?7f:6.5f;
                liftHeight=liftRide.Riding.IsTow?1.8f:2.5f;
            }
            Vector3 right=Vector3.Cross(Vector3.up,heading);
            return focus-heading*(Mathf.Lerp(distance,liftDistance,liftBlend)+extraCameraDistanceAtSpeed*speed01+airBlend*extraBigAirDistance-nocturneApproach*.3f+nocturneReveal*1.45f)
                +right*sideOffset+Vector3.up*(Mathf.Lerp(height,liftHeight,liftBlend)-.15f*speed01+airBlend*extraBigAirHeight
                -impact*landingFeedback*shakeIntensity-collisionPulse*maximumSpeedVibration*shakeIntensity
                -nocturneApproach*.45f+nocturneReveal*1.02f);
        }
        void ProbeTerrain(Vector3 heading,float speed01,bool air)
        {
            ResetReliefPreview();
            if(State==ViewState.Ground&&controller!=null&&framingIntensity>0&&terrainPitchInfluence>0)
            {ProbeGroundRelief(heading,speed01);return;}
            if(State==ViewState.Lift||State==ViewState.Crash||State==ViewState.Stopped||State==ViewState.Heli){terrainTarget=0;return;}
            Vector3 origin=target.position;
            float near,far,range=ArcadeCameraFramingRules.ProbeDistance(terrainProbeDistance,SpeedKmh/3.6f,speed01,framingIntensity);
            bool n=TerrainHeight(origin+heading*range,out near),f=TerrainHeight(origin+heading*range*1.75f,out far);
            if(!n&&!f){terrainTarget=0;return;}
            float ahead=n&&f?Mathf.Lerp(near,far,.3f):n?near:far;
            float descent=air&&controller!=null?Mathf.Clamp01(-controller.TravelVelocity.y/16):1;
            terrainTarget=Mathf.Clamp((ahead-(origin.y-.12f))*verticalLookAhead*terrainPitchInfluence,-1.1f,.45f)*descent;
        }
        void LateUpdate()
        {
            if(EntrySequenceActive2612||target==null||attachedCamera==null||!attachedCamera.enabled||Time.timeScale<=0f)return;
            if(!Application.isFocused||DemoRuntime263.OptionsOpen||(controller!=null&&controller.WorldSession!=null&&controller.WorldSession.InputBlocked))return;
            if(!initialized){SnapBehind();return;}
            float dt=Time.unscaledDeltaTime;if(dt<=0)return;dt=Mathf.Min(dt,.1f);
            State=controller==null?ViewState.Ground:controller.HeliHeld2612?ViewState.Heli:controller.IsStopped2612?ViewState.Stopped:controller.IsRidingLift?ViewState.Lift:
                controller.IsCrashed?ViewState.Crash:controller.IsRailRiding?ViewState.Rail:
                controller.IsWallriding?ViewState.Wallride:controller.IsAirborne?ViewState.Airborne:ViewState.Ground;
            if(State!=ViewState.Ground)ResetReliefPreview();
            SampleHybridCamera26315();
            bool lift=State==ViewState.Lift||State==ViewState.Stopped||State==ViewState.Heli;
            liftBlend=Mathf.Lerp(liftBlend,lift?1:0,ArcadeGlideRules.Blend(4,dt));
            if(lift&&Input.GetMouseButton(1))
            {
                liftLookYaw=Mathf.Clamp(liftLookYaw+Input.GetAxis("Mouse X")*2f*DemoSettings263.Data.arcadeHorizontal,-150,150);
                liftLookPitch=Mathf.Clamp(liftLookPitch-Input.GetAxis("Mouse Y")*1.5f*DemoSettings263.Data.arcadeVertical,-25,35);liftLookAt=Time.unscaledTime;
            }
            Vector2 padLook=DemoGamepad263.Look;
            if(lift&&padLook.sqrMagnitude>.001f)
            {
                liftLookYaw=Mathf.Clamp(liftLookYaw+padLook.x*100f*dt*DemoSettings263.Data.arcadeHorizontal,-150,150);
                liftLookPitch=Mathf.Clamp(liftLookPitch-padLook.y*70f*dt*DemoSettings263.Data.arcadeVertical,-25,35);liftLookAt=Time.unscaledTime;
            }
            if(!lift||ArcadeInput263.Down(ArcadeAction263.Recenter,controller)||(State==ViewState.Lift&&Time.unscaledTime-liftLookAt>3f)||(liftRide!=null&&liftRide.Progress>.94f))
            {
                liftLookYaw=Mathf.LerpAngle(liftLookYaw,0,ArcadeGlideRules.Blend(2,dt));
                liftLookPitch=Mathf.Lerp(liftLookPitch,0,ArcadeGlideRules.Blend(2,dt));
            }
            float speed=ArcadeViewRules.Speed(controller),speed01=ArcadeViewRules.SpeedEnvelope(speed);SpeedKmh=speed*3.6f;
            Vector3 previous=transform.position,translation=target.position-previousTargetPosition;
            if(translation.sqrMagnitude>900){SnapBehind();return;}
            Vector3 carried=previous+translation;previousTargetPosition=target.position;
            float obstacle=controller!=null?controller.ObstacleFeedback:0;
            if(obstacle>previousObstacle+.1f)collisionPulse=Mathf.Max(collisionPulse,obstacle);
            previousObstacle=obstacle;
            Vector3 travel=controller!=null?controller.TravelVelocity:Vector3.zero;
            Vector3 flat=Vector3.ProjectOnPlane(travel,Vector3.up);
            if(controller!=null&&(State==ViewState.Rail||State==ViewState.Wallride))flat=Vector3.ProjectOnPlane(controller.CurrentInteractionDirection,Vector3.up)*Mathf.Max(1f,speed);
            if(State==ViewState.Lift)flat=Vector3.ProjectOnPlane(target.forward,Vector3.up);
            bool comfortableTurn=progressiveTurnCamera&&(State==ViewState.Ground||State==ViewState.Airborne);
            if(flat.sqrMagnitude>.2f&&State!=ViewState.Crash)
            {
                float yaw=Mathf.Atan2(flat.x,flat.z)*Mathf.Rad2Deg;
                float response=State==ViewState.Rail?10:Mathf.Lerp(8,highSpeedHeadingResponse,speed01);
                if(comfortableTurn)response=comfortableHeadingResponse;
                else if(State==ViewState.Ground&&obstacle<=.1f&&collisionPulse<=.1f)
                    response=ArcadeCameraFramingRules.CurveResponse(response,Mathf.DeltaAngle(filteredYaw,yaw),maximumHeadingLag,framingIntensity);
                filteredYaw=Mathf.LerpAngle(filteredYaw,yaw,ArcadeGlideRules.Blend(response,dt));
                float rate=State==ViewState.Lift?55:Mathf.Lerp(maximumOrbitDegreesPerSecond,highSpeedOrbitLimit,speed01);
                if(State==ViewState.Ground)rate=Mathf.Max(rate,Mathf.Min(maximumOrbitDegreesPerSecond,130f)*framingIntensity);
                if(obstacle>.1f||collisionPulse>.1f)rate=Mathf.Min(rate,45);
                float lag=Mathf.Abs(Mathf.DeltaAngle(cameraYaw,filteredYaw));
                if(comfortableTurn)
                    cameraYaw=ArcadePlaytestComfortRules.FollowHeading(cameraYaw,filteredYaw,comfortableYawRate,
                        Mathf.Min(comfortableOrbitSpeed,rate),comfortableOrbitAcceleration,comfortableHeadingResponse,
                        headingDeadZone,dt,out comfortableYawRate);
                else
                {
                    comfortableYawRate=0;
                    cameraYaw=ArcadeViewRules.FollowYaw(cameraYaw,filteredYaw,State==ViewState.Rail?.7f:headingDeadZone,
                        rotationSharpness*(lag>maximumHeadingLag?1.3f:1),rate,dt);
                }
            }
            else comfortableYawRate=0;
            bool air=State==ViewState.Airborne;
            if(air&&!wasAirborne){airStart=target.position.y;airAge=airFramePeak=0;}
            airAge=air?airAge+Time.deltaTime:0;
            airFramePeak=ArcadeCameraFramingRules.AirFramePeak(airFramePeak,air,airAge,target.position.y-airStart,bigAirThreshold);
            airBlend=Mathf.Lerp(airBlend,airFramePeak,ArcadeGlideRules.Blend(3,dt));wasAirborne=air;
            UpdateNocturneFraming(dt,speed);
            Vector3 heading=Quaternion.Euler(0,cameraYaw,0)*Vector3.forward;
            Vector3 aimHeading=heading;
            if((State==ViewState.Ground||air)&&flat.sqrMagnitude>.2f&&obstacle<=.1f&&collisionPulse<=.1f)
            {
                float previewYaw=ArcadeCameraFramingRules.PreviewYaw(cameraYaw,comfortableTurn?filteredYaw:Mathf.Atan2(flat.x,flat.z)*Mathf.Rad2Deg,maximumHeadingLag,framingIntensity);
                aimHeading=Quaternion.Euler(0,previewYaw,0)*Vector3.forward;
            }
            if(Time.unscaledTime>=probeAt){probeAt=Time.unscaledTime+.1f;ProbeTerrain(aimHeading,speed01,air);}
            terrainLook=Mathf.Lerp(terrainLook,terrainTarget,ArcadeGlideRules.Blend(3,dt));
            float pitchStrength=Mathf.Min(framingIntensity,terrainPitchInfluence/.7f);
            float pitchTarget=State==ViewState.Ground?ArcadeCameraFramingRules.DownhillPitch(travel.y,flat.magnitude,pitchStrength):0;
            if(air&&controller!=null)
                pitchTarget=ArcadeCameraFramingRules.AirPitch(travel,controller.GroundNormal,controller.IsApproachingLanding,pitchStrength);
            if(State==ViewState.Ground&&reliefPreviewValid)
                pitchTarget=ArcadeCameraReliefRules.GroundPitch(pitchTarget,reliefPreviewPitch,reliefPreviewConfidence,speed,pitchStrength);
            downhillPitch=(State==ViewState.Ground||air)?ArcadeCameraReliefRules.FollowPitch(downhillPitch,pitchTarget,dt)
                :Mathf.Lerp(downhillPitch,pitchTarget,ArcadeGlideRules.Blend(4,dt));
            float lean=controller!=null&&State==ViewState.Ground?controller.CarveLeanDegrees/42:0;
            sideOffset=Mathf.Lerp(sideOffset,-Mathf.Clamp(lean,-1,1)*curveSideOffset*speed01,ArcadeGlideRules.Blend(3,dt));
            boostBlend=Mathf.Lerp(boostBlend,controller!=null&&controller.BoostPresentationActive&&controller.IsBoosting?1:0,ArcadeGlideRules.Blend(4,dt));
            Vector3 focus=target.position+lookOffset;
            float anticipation=lookAheadAtSpeed*speed01*(air?1+Mathf.Clamp01(-travel.y/25)*.15f:1);
            float groundPitch=downhillPitch*Mathf.Deg2Rad;
            Vector3 surfaceAim=aimHeading*Mathf.Cos(groundPitch)+Vector3.down*Mathf.Sin(groundPitch);
            anticipation+=1.5f*Mathf.Clamp01(downhillPitch/60f);
            // Keep the rider in frame while revealing the downhill line and near landing.
            anticipation+=nocturneReveal*(air&&controller.IsApproachingLanding?1.15f:2.0f);
            Vector3 aim=focus+surfaceAim*anticipation+Vector3.up*(terrainLook-nocturneReveal*.38f);
            float follow=State==ViewState.Lift?5:Mathf.Lerp(followSharpness,followSharpness*.8f,speed01);
            Vector3 desired=DesiredPosition(focus,speed01);
            transform.position=Protect(focus,Vector3.Lerp(carried,desired,ArcadeGlideRules.Blend(follow,dt)),previous,dt,false,Vector3.Distance(focus,desired));
            Vector3 aimRay=aim-transform.position;
            if(aimRay.sqrMagnitude>.001f)
            {
                Quaternion facing=Quaternion.LookRotation(aimRay,Vector3.up);
                Quaternion smooth=Quaternion.Slerp(transform.rotation,facing,ArcadeGlideRules.Blend(rotationSharpness,dt));
                transform.rotation=Quaternion.RotateTowards(transform.rotation,smooth,(obstacle>.1f?60:comfortableTurn?75:120)*dt);
            }
            impact=Mathf.MoveTowards(impact,0,dt*3.5f);collisionPulse=Mathf.MoveTowards(collisionPulse,0,dt*4);
            bool phaseActive=controller!=null&&controller.BoostPresentationActive;
            ArcadeBoostPhase phase=phaseActive?controller.BoostPhase:ArcadeBoostPhase.Yellow;
            float extra=phaseActive?ArcadeBoostSpeedRules.ExtraFov(phase,controller.IsBoosting,speed):0;
            float cap=phase==ArcadeBoostPhase.Red?94:phase==ArcadeBoostPhase.Green?89:86;
            float fov=ArcadeViewRules.Fov(speed,normalFieldOfView,cruiseFieldOfView,maximumFieldOfView)
                +boostBlend*boostFieldOfView+extra+airBlend*airFovBonus-impact*.6f
                +(DemoSettings263.Data.reducedMotion?0:nocturneReveal*4.2f);
            var hybrid=controller!=null?controller.hybridVisual26315:null;
            fov=ArcadeHybridVisualRules26315.FovTarget(fov,normalFieldOfView,controller!=null?controller.HybridVisualAngularSpeed26315:0,
                controller!=null?Vector3.Angle(controller.GroundNormal,Vector3.up):0,hybrid);
            fov=Mathf.Lerp(normalFieldOfView,Mathf.Clamp(fov,normalFieldOfView-1,cap),dynamicFovIntensity);
            attachedCamera.fieldOfView=ArcadeHybridVisualRules26315.FollowFov(attachedCamera.fieldOfView,fov,fovResponse,dt,hybrid);
        }
    }
}
