using UnityEngine;
namespace SkiStation.Arcade
{
    public sealed partial class ArcadeSkiController
    {
        // Read-only telemetry from the existing boost solver. No additional force or fuel modification.
        public System.Action<float> BoostConsumedVisual;
        void ReportBoostVisualConsumption(float amount){BoostConsumedVisual?.Invoke(amount);}
    }
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(60)] // Sample final arm transforms after the rider's presentation.
    public sealed class RiderBoostVisual:MonoBehaviour
    {
        ArcadeSkiSession session;ArcadeSkiController controller;GameObject visual;
        readonly TrailRenderer[] trails=new TrailRenderer[2];Material material;ParticleSystem particles;
        readonly Transform[] forearms=new Transform[2];
        Color tint;bool reduced;float consumed,smoothed,particleCarry,waveDistance;
        public static bool Visible(bool playing,bool paused,bool blocked,bool boosting,bool crashed,bool lift)=>playing&&!paused&&!blocked&&boosting&&!crashed&&!lift;
        public void Bind(ArcadeSkiSession world,RiderAppearanceProfile262 look)
        {
            Unbind();session=world;controller=world?.player;if(controller==null)return;
            reduced=look!=null&&look.reducedBoostEffects;
            tint=controller.BoostPhaseColor;
            Shader shader=Resources.Load<Shader>("ArcadeA243/FeedbackSurface");
            if(shader==null||!shader.isSupported){session=null;controller=null;return;}
            material=new Material(shader){name="SkiStation original boost (runtime)"};if(material.HasProperty("_Color"))material.SetColor("_Color",Color.white);if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",Color.white);
            if(material.HasProperty("_Surface")){material.SetFloat("_Surface",1);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");}
            if(material.HasProperty("_SrcBlend"))material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if(material.HasProperty("_DstBlend"))material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if(material.HasProperty("_ZWrite"))material.SetFloat("_ZWrite",0);material.renderQueue=3000;
            visual=new GameObject("SkiStation boost signature");visual.transform.SetParent(transform,false);visual.layer=gameObject.layer;
            var root=transform.Find("Arcade body visual");

            for(int i=0;i<2;i++)
            {
                var go=new GameObject("Boost trail "+i);go.layer=gameObject.layer;
                forearms[i]=root?.Find(i==0?"Left forearm mesh":"Right forearm mesh");
                go.transform.SetParent(visual.transform,false);
                var trail=go.AddComponent<TrailRenderer>();trails[i]=trail;trail.sharedMaterial=material;trail.time=.27f;trail.minVertexDistance=.08f;trail.numCornerVertices=1;trail.numCapVertices=1;trail.startWidth=1;trail.endWidth=0;trail.startColor=tint;trail.endColor=new Color(tint.r,tint.g,tint.b,0);trail.emitting=false;trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;trail.receiveShadows=false;
            }
            particles=visual.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.loop=true;main.playOnAwake=false;main.maxParticles=48;main.startLifetime=.22f;main.startSize=.045f;main.startSpeed=0;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=tint;
            var emission=particles.emission;emission.enabled=false;var shape=particles.shape;shape.enabled=false;
            var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            particles.Play();
            controller.BoostConsumedVisual+=Consume;controller.Discontinuity+=Clear;
        }
        void Consume(float amount){if(amount>0&&ChallengeRules259.Finite(amount))consumed+=amount;}
        void LateUpdate()
        {
            if(controller==null||session==null||visual==null)return;
            if(!controller.BoostPresentationActive||!session.IsPlaying||session.IsPaused||session.InputBlocked){Clear();return;}
            Color nextTint=controller.BoostPhaseColor;
            if(nextTint!=tint){tint=nextTint;foreach(var trail in trails)if(trail!=null){trail.startColor=tint;trail.endColor=new Color(tint.r,tint.g,tint.b,0);}}
            bool active=Visible(session.IsPlaying,session.IsPaused,session.InputBlocked,controller.IsBoosting,controller.IsCrashed,controller.IsRidingLift)&&consumed>0;
            float rate=consumed/Mathf.Max(.001f,Time.deltaTime);consumed=0;
            smoothed=Mathf.MoveTowards(smoothed,active?Mathf.Clamp01(rate/23f):0,Time.unscaledDeltaTime*12);
            bool low=reduced||DemoSettings263.Data.reducedMotion||ArcadeRenderSettings257.Quality==ArcadeGraphics257.Low;
            float phase=controller.BoostPhase==ArcadeBoostPhase.Red?1:controller.BoostPhase==ArcadeBoostPhase.Green?.45f:0;
            if(active)waveDistance=(waveDistance+controller.Speed*Time.deltaTime*1.5f)%(Mathf.PI*2);
            for(int i=0;i<trails.Length;i++)
            {
                var trail=trails[i];
                if(trail==null)continue;
                var arm=forearms[i];
                // Segment() centers the forearm and puts its wrist at positive local Y.
                if(arm==null){trail.emitting=false;continue;}
                Vector3 wrist=arm.TransformPoint(Vector3.up);
                float wave=low?0:Mathf.Sin(waveDistance+i*1.8f)*(.015f+phase*.015f)*smoothed;
                trail.transform.position=wrist+transform.right*wave;
                trail.emitting=active;
                trail.time=low?.17f:.36f+phase*.12f;
                trail.widthMultiplier=(low?.045f:.082f+phase*.052f)*Mathf.Lerp(.55f,1,smoothed);
            }
            if(!active){particleCarry=0;return;}
            int budget=low?0:ArcadeRenderSettings257.Quality==ArcadeGraphics257.Medium?12:26+Mathf.RoundToInt(phase*10);
            particleCarry+=budget*smoothed*Time.deltaTime;int count=Mathf.Min(6,Mathf.FloorToInt(particleCarry));particleCarry-=count;
            if(count>0){var emit=new ParticleSystem.EmitParams{position=transform.position+Vector3.up*.4f,velocity=-transform.forward*.8f+Vector3.up*.5f,startColor=tint,startSize=.025f+.025f*smoothed};particles.Emit(emit,count);}
        }
        public void Clear(){consumed=smoothed=particleCarry=waveDistance=0;foreach(var trail in trails)if(trail!=null){trail.emitting=false;trail.Clear();}if(particles!=null)particles.Clear();}
        public void Unbind()
        {
            if(controller!=null){controller.BoostConsumedVisual-=Consume;controller.Discontinuity-=Clear;}Clear();
            for(int i=0;i<trails.Length;i++){if(trails[i]!=null){trails[i].gameObject.SetActive(false);Destroy(trails[i].gameObject);trails[i]=null;}}
            if(visual!=null){visual.SetActive(false);Destroy(visual);}if(material!=null)Destroy(material);
            System.Array.Clear(forearms,0,forearms.Length);
            visual=null;material=null;particles=null;session=null;controller=null;
        }
        void OnDisable(){Unbind();}void OnDestroy(){Unbind();}
    }
}
