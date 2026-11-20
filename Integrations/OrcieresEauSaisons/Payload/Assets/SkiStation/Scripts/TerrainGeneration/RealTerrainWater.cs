using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiStation.TerrainGeneration
{
    internal enum AlpineSeason { Winter, Spring, Summer, Autumn }
    internal enum WaterSurfaceState { Open, Ice, Snow }

    internal sealed class RealTerrainWater : MonoBehaviour
    {
        const float CellSize = 32f;
        const string WetGround = "Emprise sur une zone d’eau, même gelée ou enneigée.";
        const string NeedsBridge = "Un franchissement sur pont ski est nécessaire au-dessus de l’eau.";
        struct Triangle { public Vector2 a, b, c; public bool lake; }
        readonly Dictionary<Vector2Int, List<Triangle>> tiles = new Dictionary<Vector2Int, List<Triangle>>();
        TerrainGenerationWorld world;
        Material lakeMaterial, streamMaterial;
        Color lakeColor, streamColor;
        MountainActivityRuntime diving, swimming;
        int refreshedDay = -1;
        float nextWeatherCheck;
        internal WaterSurfaceState Lakes { get; private set; }
        internal WaterSurfaceState Streams { get; private set; }
        internal static RealTerrainWater Current => TerrainGenerationWorld.Current?.GetComponent<RealTerrainWater>();
        internal static bool SeasonalDomain => ValAlpin.ValAlpinScene.ActiveMapId == ValAlpin.ValAlpinScene.OrcieresDomainId;
        internal static AlpineSeason SeasonFor(int day)
        {
            int length = SeasonCalendarRuntime361.I != null ? SeasonCalendarRuntime361.I.SeasonLengthDays : 42;
            return (AlpineSeason)(((Mathf.Max(1, day) - 1) / length) % 4);
        }
        internal static AlpineSeason Season => SeasonFor(GameClock.I != null ? GameClock.I.CurrentDay : 1);
        internal string Summary => $"{SeasonLabel(Season)} · Lacs {Label(Lakes)} · Cours d’eau {Label(Streams)}";
        internal string Activities => diving != null && diving.OpenForVisitors ? "Plongée sous glace ouverte aux Estaris"
            : swimming != null && swimming.OpenForVisitors ? "Baignade ouverte aux Estaris" : "Activités aquatiques fermées aux Estaris";

        internal void Bind(TerrainGenerationWorld owner, Material lakes, Material streams, Transform presentation, Vector3? center)
        {
            world = owner;
            lakeMaterial = lakes;
            streamMaterial = streams;
            lakeColor = lakes.GetColor("_BaseColor");
            streamColor = streams.GetColor("_BaseColor");
            if (center.HasValue && TryBank(center.Value, out var bank))
            {
                diving = Activity(presentation, bank, MountainActivityType.IceDiving,
                    "Plongée sous glace · Estaris", 6, 45, () => Season == AlpineSeason.Winter && Lakes != WaterSurfaceState.Open);
                swimming = Activity(presentation, bank, MountainActivityType.LakeSwimming,
                    "Baignade · Estaris", 20, 0, () => Season == AlpineSeason.Summer && Lakes == WaterSurfaceState.Open);
            }
            Refresh();
        }

        static MountainActivityRuntime Activity(Transform parent, Vector3 bank, MountainActivityType type,
            string label, int capacity, float price, Func<bool> available)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, true);
            go.transform.position = bank;
            var activity = go.AddComponent<MountainActivityRuntime>();
            activity.ConfigureSeasonalSite(type, label, capacity, price, available);
            return activity;
        }

        bool TryBank(Vector3 center, out Vector3 bank)
        {
            for (int r = 30; r <= 320; r += 10)
                for (int dir = 0; dir < 40; dir++)
                {
                    float angle = dir * 2 * Mathf.PI / 40;
                    var p = center + new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r);
                    if (world.Contains(p.x,p.z) && !Intersects(p,p,2))
                    {
                        bank = new Vector3(p.x,world.ClampedHeight(p.x,p.z),p.z);
                        return true;
                    }
                }
            bank = default;
            return false;
        }

        static string Label(WaterSurfaceState state) => state == WaterSurfaceState.Open ? "eau libre"
            : state == WaterSurfaceState.Ice ? "gelés" : "gelés sous la neige";
        static string SeasonLabel(AlpineSeason season) => season switch
        {
            AlpineSeason.Winter => "Hiver",
            AlpineSeason.Spring => "Printemps",
            AlpineSeason.Summer => "Été",
            _ => "Automne"
        };

        void Update()
        {
            if (world == null || !world.IsReady || world.CheckpointTerrainBusy || Time.unscaledTime < nextWeatherCheck) return;
            nextWeatherCheck = Time.unscaledTime + .65f;
            int day = GameClock.I != null ? GameClock.I.CurrentDay : 1;
            if (day != refreshedDay) Refresh();
        }

        void Refresh()
        {
            int day = GameClock.I != null ? GameClock.I.CurrentDay : 1;
            int freezeDays = 0, snowDays = 0;
            float degreeDays = 0;
            var forecast = SeasonWeatherRuntime363.I;
            for (int at = day - 6; at <= day; at++)
            {
                var weather = at > 0 && forecast != null ? forecast.GetForecast(at) : default;
                float temp = at > 0 && forecast != null ? weather.temperatureC : -6f;
                if (temp < -1f) freezeDays++;
                if (weather.weather == MountainWeatherType.Snow || weather.weather == MountainWeatherType.HeavySnow) snowDays++;
                degreeDays += Mathf.Max(0,-temp) - Mathf.Max(0,temp)*1.5f;
            }
            bool mayFreeze = SeasonFor(day) != AlpineSeason.Summer;
            Lakes = Surface(mayFreeze && freezeDays >= 4 && degreeDays >= 13, snowDays >= 2);
            Streams = Surface(mayFreeze && freezeDays >= 6 && degreeDays >= 29, snowDays >= 3);
            Apply(lakeMaterial, lakeColor, Lakes);
            Apply(streamMaterial, streamColor, Streams);
            refreshedDay = day;
        }

        static WaterSurfaceState Surface(bool frozen, bool snowy) =>
            !frozen ? WaterSurfaceState.Open : snowy ? WaterSurfaceState.Snow : WaterSurfaceState.Ice;

        static void Apply(Material material, Color water, WaterSurfaceState state)
        {
            material.SetColor("_BaseColor", state == WaterSurfaceState.Open ? water
                : state == WaterSurfaceState.Ice ? new Color(.46f,.65f,.72f) : new Color(.82f,.87f,.91f));
            material.SetFloat("_Smoothness", state == WaterSurfaceState.Snow ? .13f : .65f);
        }

        internal void AddSurface(Vector2[] points, int[] triangles, bool lake)
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var t = new Triangle {a=points[triangles[i]],b=points[triangles[i+1]],c=points[triangles[i+2]],lake=lake};
                if (Mathf.Abs(Cross(t.b-t.a,t.c-t.a)) < .0001f) continue;
                var min = Cell(Vector2.Min(t.a, Vector2.Min(t.b,t.c)));
                var max = Cell(Vector2.Max(t.a, Vector2.Max(t.b,t.c)));
                for (int z = min.y; z <= max.y; z++)
                for (int x = min.x; x <= max.x; x++)
                {
                    var key = new Vector2Int(x,z);
                    if (!tiles.TryGetValue(key, out var list))
                    {
                        list = new List<Triangle>();
                        tiles.Add(key,list);
                    }
                    list.Add(t);
                }
            }
        }

        static Vector2 XZ(Vector3 p) => new Vector2(p.x,p.z);
        static Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x/CellSize),Mathf.FloorToInt(p.y/CellSize));
        static float Cross(Vector2 a,Vector2 b) => a.x*b.y-a.y*b.x;
        static bool Inside(Vector2 p, Triangle t)
        {
            float a=Cross(t.b-t.a,p-t.a), b=Cross(t.c-t.b,p-t.b), c=Cross(t.a-t.c,p-t.c);
            return (a>=0&&b>=0&&c>=0)||(a<=0&&b<=0&&c<=0);
        }

        static float DistanceSq(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab=b-a;
            return (p-a-Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(.000001f,ab.sqrMagnitude))*ab).sqrMagnitude;
        }

        static bool Near(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float radiusSq)
        {
            var u = b-a;
            var v = d-c;
            float denominator = Cross(u,v);
            if (Mathf.Abs(denominator) > .000001f)
            {
                float s=Cross(c-a,v)/denominator,t=Cross(c-a,u)/denominator;
                if (s>=0&&s<=1&&t>=0&&t<=1) return true;
            }
            return DistanceSq(a,c,d)<=radiusSq || DistanceSq(b,c,d)<=radiusSq ||
                DistanceSq(c,a,b)<=radiusSq || DistanceSq(d,a,b)<=radiusSq;
        }

        internal bool Intersects(Vector3 from, Vector3 to, float radius, bool lakeOnly = false)
        {
            Vector2 a=XZ(from),b=XZ(to);
            var r=Vector2.one*radius;
            var min=Cell(Vector2.Min(a,b)-r);
            var max=Cell(Vector2.Max(a,b)+r);
            for(int z=min.y;z<=max.y;z++)
            for(int x=min.x;x<=max.x;x++)
                if(tiles.TryGetValue(new Vector2Int(x,z),out var candidates))
                    foreach(var t in candidates)
                        if((!lakeOnly||t.lake) && (Inside(a,t)||Inside(b,t)||
                            Near(a,b,t.a,t.b,radius*radius)||Near(a,b,t.b,t.c,radius*radius)||Near(a,b,t.c,t.a,radius*radius)))
                            return true;
            return false;
        }

        internal static bool AllowsFootprint(Vector3 center,Vector2 size,out string reason)
        {
            var water=Current;
            if(water==null){reason="";return true;}
            // Subdivide the rectangle into short parallel capsules. Edges stay protected
            // even where a very narrow stream crosses between heightfield samples.
            float pad = Mathf.Max(.26f, size.y / Mathf.Max(1, Mathf.Ceil(size.y)));
            int lanes = Mathf.Max(1,Mathf.CeilToInt(size.y/pad));
            var a=center-Vector3.right*size.x*.5f;
            var b=center+Vector3.right*size.x*.5f;
            for(int z=0;z<=lanes;z++)
            {
                var shift=Vector3.forward * ((z/(float)lanes-.5f)*size.y);
                if(water.Intersects(a+shift,b+shift,pad*.5f)){reason=WetGround;return false;}
            }
            reason="";
            return true;
        }

        internal static bool AllowsRoute(IReadOnlyList<Vector3> route,float width,out string reason,bool trail)
        {
            var water=Current;
            if(water!=null)
            for(int i=0;i<route.Count;i++)
            {
                var a=route[Mathf.Max(0,i-1)];
                var b=route[i];
                float radius=width*.5f;
                if(!water.Intersects(a,b,radius))continue;
                if(!trail){reason=WetGround;return false;}
                int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/2f));
                for(int j=0;j<steps;j++)
                {
                    var from=Vector3.Lerp(a,b,j/(float)steps);
                    var to=Vector3.Lerp(a,b,(j+1f)/steps);
                    if(water.Intersects(from,to,radius)&&!WaterCrossing.CoversTrail(from,to,radius))
                    {
                        reason=NeedsBridge;
                        return false;
                    }
                }
            }
            reason="";
            return true;
        }
    }
}
