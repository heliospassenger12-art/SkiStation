using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    public enum VisitorPlanActivity { Ski, Lift, Shop, Restaurant, Leisure, Rest, Explore, Leave }

    [Serializable]
    public struct VisitorPlanSnapshot
    {
        public VisitorPlanActivity current;
        public VisitorPlanActivity next;
        public string targetId;
        public float remainingMinutes;
        public int completedSteps;
        public float planConfidence01;
        public int day;
    }

    /// <summary>Builds a lightweight daily plan for real management visitors. It observes and guides; it never creates visitors.</summary>
    public sealed class VisitorDayPlanRuntime : MonoBehaviour
    {
        public static VisitorDayPlanRuntime I { get; private set; }
        [SerializeField] private float refreshSeconds = 2f;
        [SerializeField] private float defaultActivityMinutes = 35f;
        private readonly Dictionary<SkierRuntime, VisitorPlanSnapshot> plans = new();
        private float timer;
        public IReadOnlyDictionary<SkierRuntime, VisitorPlanSnapshot> Plans => plans;
        public event Action Changed;

        private void Awake(){ if(I!=null&&I!=this){Destroy(gameObject);return;} I=this; DontDestroyOnLoad(gameObject); }
        private void Start()=>Refresh();
        private void Update(){ timer+=Time.unscaledDeltaTime; if(timer>=Mathf.Max(.5f,refreshSeconds)){timer=0; Refresh();} TickPlans(); }

        public VisitorPlanSnapshot GetPlan(SkierRuntime skier){ if(skier!=null&&!plans.ContainsKey(skier)) BuildPlan(skier); return skier!=null&&plans.TryGetValue(skier,out var p)?p:default; }

        /// <summary>Marks a concrete service as completed and lets the daily plan advance around the real service duration.</summary>
        public void RegisterServiceCompleted(SkierRuntime skier, StationServiceType type, float durationMinutes)
        {
            if (skier == null) return;
            if (!plans.TryGetValue(skier, out var p)) BuildPlan(skier);
            p = plans[skier];
            p.completedSteps++;
            p.current = VisitorPlanActivity.Rest;
            p.next = ChooseNext(VisitorIntentRuntime.I != null ? VisitorIntentRuntime.I.GetIntent(skier) : VisitorIntent.SkiDay, p.completedSteps);
            p.remainingMinutes = Mathf.Max(1f, durationMinutes);
            p.planConfidence01 = Mathf.Clamp01(p.planConfidence01 + .02f);
            p.targetId = type.ToString();
            plans[skier] = p;
            Changed?.Invoke();
        }

        private void Refresh()
        {
            var live = new HashSet<SkierRuntime>(FindObjectsOfType<SkierRuntime>());
            foreach(var skier in live) if(skier!=null&&skier.State!=SkierState.Leaving) EnsurePlan(skier);
            var dead=new List<SkierRuntime>(); foreach(var p in plans) if(!live.Contains(p.Key)) dead.Add(p.Key); foreach(var s in dead) plans.Remove(s);
            Changed?.Invoke();
        }
        private void EnsurePlan(SkierRuntime skier){ if(!plans.ContainsKey(skier)) BuildPlan(skier); else Reevaluate(skier); }
        private void BuildPlan(SkierRuntime skier)
        {
            var intent=VisitorIntentRuntime.I!=null?VisitorIntentRuntime.I.GetIntent(skier):VisitorIntent.SkiDay;
            var current=MapIntentToFirst(intent);
            var p=new VisitorPlanSnapshot{current=current,next=VisitorPlanActivity.Ski,remainingMinutes=defaultActivityMinutes,completedSteps=0,planConfidence01=.78f,day=GetDay()};
            plans[skier]=p;
        }
        private void Reevaluate(SkierRuntime skier)
        {
            var p=plans[skier];
            if(p.day!=GetDay()){p.day=GetDay();p.completedSteps=0;p.current=VisitorPlanActivity.Ski;p.remainingMinutes=defaultActivityMinutes;}
            var intent=VisitorIntentRuntime.I!=null?VisitorIntentRuntime.I.GetIntent(skier):VisitorIntent.SkiDay;
            if(p.current==VisitorPlanActivity.Leave) return;
            if(skier.State==SkierState.Leaving) p.current=VisitorPlanActivity.Leave;
            else if(skier.State==SkierState.Skiing) p.current=VisitorPlanActivity.Ski;
            else if(skier.State==SkierState.RidingLift||skier.State==SkierState.WaitingForLift) p.current=VisitorPlanActivity.Lift;
            else if(skier.State==SkierState.Resting) p.current=VisitorPlanActivity.Rest;
            p.next=ChooseNext(intent,p.completedSteps);
            p.planConfidence01=Mathf.Lerp(p.planConfidence01,.82f,.08f);
            plans[skier]=p;
        }
        private void TickPlans()
        {
            float minutes=Time.unscaledDeltaTime/60f;
            if(minutes<=0) return;
            var keys=new List<SkierRuntime>(plans.Keys);
            foreach(var skier in keys){ if(skier==null||!plans.ContainsKey(skier))continue; var p=plans[skier]; p.remainingMinutes-=minutes; if(p.remainingMinutes<=0){p.completedSteps++;p.current=p.next;p.next=ChooseNext(VisitorIntentRuntime.I!=null?VisitorIntentRuntime.I.GetIntent(skier):VisitorIntent.SkiDay,p.completedSteps);p.remainingMinutes=defaultActivityMinutes; } plans[skier]=p; }
        }
        private static VisitorPlanActivity MapIntentToFirst(VisitorIntent i){ switch(i){case VisitorIntent.FamilyDay:return VisitorPlanActivity.Explore;case VisitorIntent.Relaxation:return VisitorPlanActivity.Rest;case VisitorIntent.ApresSki:return VisitorPlanActivity.Rest;default:return VisitorPlanActivity.Ski;} }
        private static VisitorPlanActivity ChooseNext(VisitorIntent i,int steps){ if(steps>=6)return VisitorPlanActivity.Leave; switch(i){case VisitorIntent.FamilyDay:return steps%3==1?VisitorPlanActivity.Rest:steps%3==2?VisitorPlanActivity.Leisure:VisitorPlanActivity.Ski;case VisitorIntent.Relaxation:return steps%2==0?VisitorPlanActivity.Rest:VisitorPlanActivity.Leisure;case VisitorIntent.PremiumExperience:return steps%3==0?VisitorPlanActivity.Rest:VisitorPlanActivity.Ski;case VisitorIntent.ApresSki:return steps>=4?VisitorPlanActivity.Rest:VisitorPlanActivity.Ski;case VisitorIntent.SportPerformance:case VisitorIntent.Training:return VisitorPlanActivity.Ski;default:return steps%4==3?VisitorPlanActivity.Rest:VisitorPlanActivity.Ski;} }
        private static int GetDay(){ var clock=GameClock.I; return clock!=null?clock.CurrentDay:0; }
    }
}
