using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Procedural alpine lift presentation. Funiculars support realistic surface and underground tunnels.</summary>
    public class LiftVisualBuilder : MonoBehaviour
    {
        [SerializeField] private int pylonCount = 0;
        [SerializeField] private bool generateVehicles = true;
        [SerializeField] private bool generateTechnicalDetails = true;
        [SerializeField] private Material metalMaterial;
        [SerializeField] private Material darkMaterial;
        [SerializeField] private Material glassMaterial;
        [SerializeField] private Material snowMaterial;
        [SerializeField] private Material concreteMaterial;

        private readonly List<GameObject> generated = new List<GameObject>();

        public void Build(LiftDefinition definition, Vector3 bottom, Vector3 top)
        {
            Build(definition, new[] { bottom, top }, false);
        }

        public void Build(LiftDefinition definition, IReadOnlyList<Vector3> route, bool underground)
        {
            Clear();
            if (definition == null || route == null || route.Count < 2) return;

            LiftType type = definition.type;
            bool surfaceTow = type == LiftType.Platter || type == LiftType.TBar || type == LiftType.Fork;
            bool funicular = type == LiftType.Funicular;
            bool carpet = type == LiftType.MagicCarpet;
            bool aerial = !surfaceTow && !funicular && !carpet;

            if (funicular)
            {
                if (underground) BuildUndergroundFunicular(definition, route);
                else BuildSurfaceFunicular(definition, route);
                return;
            }

            Vector3 a = route[0];
            Vector3 b = route[route.Count - 1];
            if (carpet) BuildMagicCarpet(definition, a, b);
            else
            {
                BuildStations(definition, a, b, aerial, surfaceTow);
                if (aerial) BuildAerialLine(definition, a, b);
                else BuildSurfaceLine(definition, route);
                if (generateVehicles) BuildVehicles(definition, aerial ? new[] { a, b } : route, aerial);
            }
        }

        private void BuildSurfaceFunicular(LiftDefinition d, IReadOnlyList<Vector3> route)
        {
            BuildFunicularTrack(route, false);
            BuildStationHouse(d, route[0], RouteDirection(route, 0), false);
            BuildStationHouse(d, route[route.Count - 1], RouteDirection(route, route.Count - 1), false);
            if (generateVehicles) BuildRailVehicles(d, route, false);
        }

        private void BuildUndergroundFunicular(LiftDefinition d, IReadOnlyList<Vector3> route)
        {
            BuildFunicularTrack(route, true);
            BuildTunnel(route, d);
            BuildUndergroundStation(d, route[0], RouteDirection(route, 0), "BottomUndergroundStation");
            BuildUndergroundStation(d, route[route.Count - 1], RouteDirection(route, route.Count - 1), "TopUndergroundStation");
            BuildAccessShaft(route[0], "BottomAccessShaft");
            BuildAccessShaft(route[route.Count - 1], "TopAccessShaft");
            if (generateVehicles) BuildRailVehicles(d, route, true);
        }

        private void BuildFunicularTrack(IReadOnlyList<Vector3> route, bool underground)
        {
            for (int i = 1; i < route.Count; i++)
            {
                Vector3 a = route[i - 1];
                Vector3 b = route[i];
                Vector3 dir = (b - a).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                CreateBeam("RailLeft", Vector3.Lerp(a, b, .5f) + side * .85f + Vector3.up * .08f,
                    new Vector3(.16f, .16f, Vector3.Distance(a, b)), dir, metalMaterial);
                CreateBeam("RailRight", Vector3.Lerp(a, b, .5f) - side * .85f + Vector3.up * .08f,
                    new Vector3(.16f, .16f, Vector3.Distance(a, b)), dir, metalMaterial);

                int sleepers = Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(a, b) / 2.2f), 2, 40);
                for (int s = 0; s < sleepers; s++)
                {
                    float t = (s + .5f) / sleepers;
                    Vector3 p = Vector3.Lerp(a, b, t) + Vector3.up * .01f;
                    CreateBeam("Sleeper", p, new Vector3(2.8f, .12f, .22f), dir, metalMaterial);
                }
            }
        }

        private void BuildTunnel(IReadOnlyList<Vector3> route, LiftDefinition d)
        {
            Mesh mesh = CreateTunnelMesh(route, Mathf.Max(2f, d.tunnelRadius), 12);
            GameObject tunnel = new GameObject("FunicularTunnel");
            tunnel.transform.SetParent(transform, false);
            var mf = tunnel.AddComponent<MeshFilter>();
            var mr = tunnel.AddComponent<MeshRenderer>();
            var mc = tunnel.AddComponent<MeshCollider>();
            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
            mr.sharedMaterial = concreteMaterial != null ? concreteMaterial : darkMaterial;
            generated.Add(tunnel);

            if (d.tunnelLighting)
            {
                int lights = Mathf.Clamp(route.Count / 8, 4, 24);
                for (int i = 0; i < lights; i++)
                {
                    float t = (i + .5f) / lights;
                    Vector3 p = EvaluateRoute(route, t) + Vector3.up * (d.tunnelRadius * .65f);
                    var lightGo = new GameObject("TunnelLight");
                    lightGo.transform.SetParent(transform, false);
                    lightGo.transform.position = p;
                    var light = lightGo.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = d.tunnelRadius * 2.5f;
                    light.intensity = .7f;
                    generated.Add(lightGo);
                }
            }

            if (d.emergencyAccess)
            {
                int doors = Mathf.Clamp(route.Count / 16, 2, 10);
                for (int i = 1; i <= doors; i++)
                {
                    float t = i / (float)(doors + 1);
                    Vector3 p = EvaluateRoute(route, t);
                    CreateBox("EmergencyNiche", p + Vector3.up * 1.4f,
                        new Vector3(d.tunnelRadius * 1.2f, 2.4f, .25f), darkMaterial,
                        Quaternion.LookRotation(RouteDirection(route, Mathf.RoundToInt(t * (route.Count - 1))), Vector3.up));
                }
            }
        }

        private Mesh CreateTunnelMesh(IReadOnlyList<Vector3> route, float radius, int sides)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < route.Count; i++)
            {
                Vector3 tangent = RouteDirection(route, i);
                Quaternion rot = Quaternion.LookRotation(tangent, Vector3.up);
                for (int s = 0; s < sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    Vector3 local = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                    vertices.Add(route[i] + rot * local);
                }
            }
            for (int i = 0; i < route.Count - 1; i++)
            for (int s = 0; s < sides; s++)
            {
                int a = i * sides + s;
                int b = i * sides + (s + 1) % sides;
                int c = (i + 1) * sides + s;
                int d = (i + 1) * sides + (s + 1) % sides;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
            var mesh = new Mesh { name = "FunicularTunnelMesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void BuildUndergroundStation(LiftDefinition d, Vector3 p, Vector3 dir, string name)
        {
            CreateBox(name, p, new Vector3(14f, 6f, 11f), concreteMaterial != null ? concreteMaterial : darkMaterial,
                Quaternion.LookRotation(dir, Vector3.up));
            CreateBox(name + "Platform", p + Vector3.up * 2.8f, new Vector3(16f, .35f, 8f), metalMaterial,
                Quaternion.LookRotation(dir, Vector3.up));
        }

        private void BuildAccessShaft(Vector3 p, string name)
        {
            CreateCylinder(name, p + Vector3.up * 5f, 2.4f, 10f, concreteMaterial != null ? concreteMaterial : darkMaterial, Quaternion.identity);
            CreateBox(name + "Cover", p + Vector3.up * 10.1f, new Vector3(5.4f, .35f, 5.4f), metalMaterial, Quaternion.identity);
        }

        private void BuildRailVehicles(LiftDefinition d, IReadOnlyList<Vector3> route, bool underground)
        {
            int count = 2;
            for (int i = 0; i < count; i++)
            {
                float t = i * .5f + .05f;
                GameObject car = BuildVehicle(d, EvaluateRoute(route, t));
                var runtime = car.AddComponent<LiftVehicleRuntime>();
                runtime.Configure(d, route, t, true);
            }
        }

        private Vector3 RouteDirection(IReadOnlyList<Vector3> route, int index)
        {
            if (index <= 0) return (route[1] - route[0]).normalized;
            if (index >= route.Count - 1) return (route[route.Count - 1] - route[route.Count - 2]).normalized;
            return (route[index + 1] - route[index - 1]).normalized;
        }


        private void BuildStationHouse(LiftDefinition d, Vector3 position, Vector3 direction, bool underground)
        {
            Vector3 end = position + (direction.sqrMagnitude > .001f ? direction.normalized : Vector3.forward) * 6f;
            BuildStations(d, position, end, true, false);
        }

        private void BuildStations(LiftDefinition d, Vector3 a, Vector3 b, bool aerial, bool surface)
        {
            Vector3 dir = (b - a).sqrMagnitude > .001f ? (b - a).normalized : Vector3.forward;
            float h = aerial ? 4.2f : 2.8f;
            Quaternion qBottom = Quaternion.LookRotation(dir, Vector3.up);
            Quaternion qTop = Quaternion.LookRotation(-dir, Vector3.up);

            CreateStationEnvelope("BottomStation", a, qBottom, h, aerial, d);
            CreateStationEnvelope("TopStation", b, qTop, h, aerial, d);
            BuildTerminalMechanics(d, a, dir, true, aerial);
            BuildTerminalMechanics(d, b, -dir, false, aerial);
        }

        private void CreateStationEnvelope(string name, Vector3 center, Quaternion rotation, float height, bool aerial, LiftDefinition d)
        {
            float width = d.type == LiftType.CableCar ? 13f : d.type == LiftType.ThreeS ? 14f : aerial ? 10f : 8f;
            float depth = d.type == LiftType.CableCar ? 12f : d.type == LiftType.ThreeS ? 11f : 8f;
            CreateBox(name + "Frame", center + Vector3.up * height * .5f, new Vector3(width, height, depth), darkMaterial, rotation);
            CreateBox(name + "Roof", center + Vector3.up * (height + .35f), new Vector3(width + 1.2f, .35f, depth + 1.1f), metalMaterial, rotation);
            CreateBox(name + "FrontGlass", center + rotation * (Vector3.forward * (depth * .5f + .03f)) + Vector3.up * (height * .55f), new Vector3(width * .82f, height * .68f, .08f), glassMaterial, rotation);
            CreateBox(name + "Platform", center + Vector3.up * .55f + rotation * Vector3.forward * 1.2f, new Vector3(width + 2.5f, 1.1f, depth + 2.5f), metalMaterial, rotation);
            CreateBox(name + "Canopy", center + Vector3.up * (height + 1.0f) + rotation * Vector3.forward * 2.0f, new Vector3(width + 3f, .16f, 3.5f), metalMaterial, rotation);
            // 2.66 visual pass: alpine terminal details only. No lift gameplay/placement changes.
            CreateBox(name + "SnowCap", center + Vector3.up * (height + .58f), new Vector3(width + 1.45f, .18f, depth + 1.35f), snowMaterial != null ? snowMaterial : metalMaterial, rotation);
            CreateBox(name + "StonePlinth", center + Vector3.up * .18f, new Vector3(width + .7f, .36f, depth + .7f), concreteMaterial != null ? concreteMaterial : darkMaterial, rotation);
            CreateBox(name + "Fascia", center + rotation * (Vector3.forward * (depth * .5f + .12f)) + Vector3.up * (height - .18f), new Vector3(width * .72f, .42f, .12f), darkMaterial, rotation);
            BuildStationLights(center, rotation, width, height);
        }

        private void BuildTerminalMechanics(LiftDefinition d, Vector3 center, Vector3 dir, bool bottom, bool aerial)
        {
            Quaternion q = Quaternion.LookRotation(dir, Vector3.up);
            float z = aerial ? 5.15f : 3.15f;
            if (d.type == LiftType.DetachableChair || d.type == LiftType.BubbleChair || d.type == LiftType.Gondola || d.type == LiftType.ThreeS)
            {
                BuildTerminalWheel(center + Vector3.up * z, q, 3.25f, (bottom ? "Bottom" : "Top") + "Bullwheel");
                CreateBox((bottom ? "Bottom" : "Top") + "DecelerationRail", center + Vector3.up * (z + .7f) + q * Vector3.forward * (bottom ? 2.3f : -2.3f), new Vector3(2.8f,.18f,5.5f), metalMaterial, q);
                BuildLoadingGates(center, q, d);
            }
            else if (d.type == LiftType.FixedChair)
            {
                BuildTerminalWheel(center + Vector3.up * z, q, 2.75f, (bottom ? "Bottom" : "Top") + "Bullwheel");
                BuildLoadingGates(center, q, d);
            }
            else if (d.type == LiftType.Platter || d.type == LiftType.TBar || d.type == LiftType.Fork)
            {
                BuildTerminalWheel(center + Vector3.up * 3.05f, q, 1.3f, (bottom ? "Bottom" : "Top") + "DriveWheel");
                CreateBox((bottom ? "Bottom" : "Top") + "TowGuide", center + Vector3.up * 2.9f + q * Vector3.forward * 1.6f, new Vector3(2.5f,.12f,3.8f), metalMaterial, q);
            }
            else if (d.type == LiftType.CableCar)
            {
                BuildTerminalWheel(center + Vector3.up * 5.1f, q, 4f, (bottom ? "Bottom" : "Top") + "CableWheel");
                BuildLoadingGates(center, q, d);
            }
        }

        private void BuildLoadingGates(Vector3 center, Quaternion q, LiftDefinition d)
        {
            float width = d.type == LiftType.CableCar ? 4.5f : 3.2f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 p = center + q * (Vector3.right * side * width * .5f + Vector3.forward * 1.8f) + Vector3.up * 1.0f;
                CreateBox("QueueGate", p, new Vector3(.12f,1.0f,3.0f), metalMaterial, q);
                CreateBox("GateTop", p + Vector3.up * .85f, new Vector3(.2f,.12f,3.0f), metalMaterial, q);
            }
            for (int i = 0; i < 4; i++)
            {
                float z = -2.4f + i * 1.6f;
                CreateCylinder("QueuePost", center + q * (Vector3.forward * z + Vector3.right * (width * .5f + .65f)) + Vector3.up * .65f, .07f, 1.3f, metalMaterial, Quaternion.identity);
                CreateCylinder("QueuePost", center + q * (Vector3.forward * z - Vector3.right * (width * .5f + .65f)) + Vector3.up * .65f, .07f, 1.3f, metalMaterial, Quaternion.identity);
            }
        }

        private void BuildStationLights(Vector3 center, Quaternion q, float width, float height)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject lamp = CreateCylinder("StationLight", center + q * Vector3.right * side * width * .38f + Vector3.up * (height + .15f), .055f, .35f, metalMaterial, Quaternion.identity);
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 6f;
                light.intensity = .9f;
            }
        }

        private void BuildAerialLine(LiftDefinition d, Vector3 a, Vector3 b)
        {
            int count = pylonCount > 0 ? pylonCount : Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(a,b)/55f), 2, 18);
            Vector3 dir = (b-a).normalized;
            for (int i=1;i<=count;i++)
            {
                float t=i/(float)(count+1);
                Vector3 p=Vector3.Lerp(a,b,t);
                float terrainClearance=Mathf.Lerp(7f,13f,Mathf.InverseLerp(0f,1200f,p.y));
                p.y += terrainClearance;
                BuildPylon(p, dir, d);
            }
            BuildCable("CableUp", a+Vector3.up*5.2f, b+Vector3.up*5.2f, .11f);
            BuildCable("CableDown", a+Vector3.up*4.8f, b+Vector3.up*4.8f, .11f);
            if (d.type == LiftType.ThreeS) BuildCable("CableCarrier", a+Vector3.up*5f, b+Vector3.up*5f, .18f);
        }

        private void BuildSurfaceLine(LiftDefinition d, IReadOnlyList<Vector3> route)
        {
            if (route == null || route.Count < 2) return;

            // The rope follows every segment, allowing real-looking direction changes
            // through dedicated corner sheave assemblies.
            for (int segment = 1; segment < route.Count; segment++)
            {
                Vector3 a = route[segment - 1] + Vector3.up * 2.8f;
                Vector3 b = route[segment] + Vector3.up * 2.8f;
                BuildCable("TowRope_" + segment, a, b, .08f);

                float segmentLength = Vector3.Distance(a, b);
                int count = Mathf.Max(1, Mathf.RoundToInt(segmentLength / 70f));
                for (int i = 1; i <= count; i++)
                {
                    float t = i / (float)(count + 1);
                    Vector3 p = Vector3.Lerp(a, b, t);
                    Vector3 dir = (b - a).normalized;
                    BuildPylon(p, dir, d);
                }
            }

            // A tow cable can turn around a horizontal sheave battery. The corner
            // tower is deliberately visible so the player immediately understands
            // where the line changes direction.
            for (int i = 1; i < route.Count - 1; i++)
                BuildTowCorner(route[i] + Vector3.up * 2.8f, route[i - 1], route[i + 1], d);
        }

        private void BuildTowCorner(Vector3 point, Vector3 previous, Vector3 next, LiftDefinition d)
        {
            Vector3 incoming = (point - previous).normalized;
            Vector3 outgoing = (next - point).normalized;
            if (incoming.sqrMagnitude < .001f || outgoing.sqrMagnitude < .001f) return;

            GameObject root = new GameObject("TowDirectionChange");
            root.transform.SetParent(transform);
            root.transform.position = point;
            generated.Add(root);

            float angle = Vector3.SignedAngle(incoming, outgoing, Vector3.up);
            float mastHeight = d.type == LiftType.Fork || d.type == LiftType.TBar || d.type == LiftType.Platter ? 3.2f : 4f;
            CreateCylinder("CornerMast", point + Vector3.down * mastHeight * .5f, .2f, mastHeight, metalMaterial, Quaternion.identity, root.transform);

            Vector3 bisector = (incoming + outgoing).sqrMagnitude > .001f ? (incoming + outgoing).normalized : outgoing;
            Quaternion q = Quaternion.LookRotation(bisector, Vector3.up);
            CreateBox("CornerArm", point + Vector3.up * .1f, new Vector3(2.6f, .18f, .3f), metalMaterial, q, root.transform);

            float wheelRadius = .24f;
            CreateCylinder("CornerSheaveIn", point + incoming * .65f + Vector3.up * .12f, wheelRadius, .18f, darkMaterial, Quaternion.Euler(90, 0, 0), root.transform);
            CreateCylinder("CornerSheaveOut", point + outgoing * .65f + Vector3.up * .12f, wheelRadius, .18f, darkMaterial, Quaternion.Euler(90, 0, 0), root.transform);

            var label = root.AddComponent<LiftDirectionChangeMarker>();
            label.TurnAngle = angle;
        }

        private void BuildVehicles(LiftDefinition d, IReadOnlyList<Vector3> route, bool aerial)
        {
            if (route == null || route.Count < 2) return;

            float routeLength = 0f;
            for (int i = 1; i < route.Count; i++) routeLength += Vector3.Distance(route[i - 1], route[i]);

            int count = Mathf.Clamp(Mathf.RoundToInt(routeLength / Mathf.Max(25f, d.speedKmh * 2.2f)), 2, 28);
            if (d.type == LiftType.CableCar) count = 2;
            if (d.type == LiftType.ThreeS) count = Mathf.Clamp(count, 2, 12);

            var vehicleRoute = new List<Vector3>(route.Count);
            float height = aerial ? 5f : 2.75f;
            for (int i = 0; i < route.Count; i++) vehicleRoute.Add(route[i] + Vector3.up * height);

            for (int i = 0; i < count; i++)
            {
                float t = (i + .5f) / count;
                Vector3 basePoint = EvaluateRoute(vehicleRoute, t);
                GameObject v = BuildVehicle(d, basePoint);
                var runtime = v.AddComponent<LiftVehicleRuntime>();
                runtime.Configure(d, vehicleRoute, t, false);
            }
        }

        private Vector3 EvaluateRoute(IReadOnlyList<Vector3> route, float t)
        {
            if (route == null || route.Count == 0) return transform.position;
            if (route.Count == 1) return route[0];

            float total = 0f;
            for (int i = 1; i < route.Count; i++) total += Vector3.Distance(route[i - 1], route[i]);
            float distance = Mathf.Clamp01(t) * total;

            for (int i = 1; i < route.Count; i++)
            {
                float segment = Vector3.Distance(route[i - 1], route[i]);
                if (distance <= segment)
                    return Vector3.Lerp(route[i - 1], route[i], segment <= .001f ? 0f : distance / segment);
                distance -= segment;
            }
            return route[route.Count - 1];
        }

        private GameObject BuildVehicle(LiftDefinition d, Vector3 position)
        {
            GameObject root=new GameObject(d.type+"_Vehicle"); root.transform.SetParent(transform); root.transform.position=position; generated.Add(root);
            switch(d.type)
            {
                case LiftType.FixedChair: case LiftType.DetachableChair: case LiftType.BubbleChair: BuildChair(root,d); break;
                case LiftType.Gondola: case LiftType.ThreeS: BuildCabin(root,d); break;
                case LiftType.CableCar: BuildLargeCabin(root,d); break;
                case LiftType.Funicular: BuildFunicularCar(root,d); break;
                default: BuildTow(root,d); break;
            }
            return root;
        }

        private void BuildFunicularCar(GameObject root, LiftDefinition d)
        {
            CreateBox("FunicularBody", root.transform.position, new Vector3(3.8f,1.9f,7.5f), darkMaterial, Quaternion.identity, root.transform);
            CreateBox("FunicularWindows", root.transform.position + Vector3.up*.25f, new Vector3(3.5f,1.15f,6.8f), glassMaterial, Quaternion.identity, root.transform);
            CreateBox("FunicularRoof", root.transform.position + Vector3.up*1.15f, new Vector3(4f,.2f,7.8f), metalMaterial, Quaternion.identity, root.transform);
        }

        private void BuildChair(GameObject root,LiftDefinition d)
        {
            float width=Mathf.Lerp(1.5f,2.4f,Mathf.InverseLerp(2,8,d.seatsPerVehicle));
            CreateBox("Suspension",root.transform.position+Vector3.up*1.5f,new Vector3(.08f,3f,.08f),metalMaterial,Quaternion.identity,root.transform);
            CreateBox("Seat",root.transform.position+Vector3.up*.2f,new Vector3(width,.12f,.65f),darkMaterial,Quaternion.identity,root.transform);
            CreateBox("Backrest",root.transform.position+Vector3.up*.7f,new Vector3(width,.85f,.1f),darkMaterial,Quaternion.identity,root.transform);
            CreateBox("Footrest",root.transform.position+Vector3.down*.15f,new Vector3(width,.08f,.65f),metalMaterial,Quaternion.identity,root.transform);
            CreateBox("SafetyBar", root.transform.position + Vector3.up * .62f + Vector3.forward * .36f, new Vector3(width + .08f,.07f,.07f), metalMaterial, Quaternion.identity, root.transform);
            CreateBox("SafetyBarLeft", root.transform.position + new Vector3(-width*.42f,.30f,.36f), new Vector3(.07f,.65f,.07f), metalMaterial, Quaternion.identity, root.transform);
            CreateBox("SafetyBarRight", root.transform.position + new Vector3(width*.42f,.30f,.36f), new Vector3(.07f,.65f,.07f), metalMaterial, Quaternion.identity, root.transform);
            if(d.type==LiftType.BubbleChair || d.windProtection) CreateBox("Bubble",root.transform.position+Vector3.up*.75f,new Vector3(width,.95f,.04f),glassMaterial,Quaternion.Euler(15,0,0),root.transform);
        }

        private void BuildCabin(GameObject root,LiftDefinition d)
        {
            CreateBox("CabinBody",root.transform.position,new Vector3(2.5f,2.2f,2.5f),darkMaterial,Quaternion.identity,root.transform);
            CreateBox("CabinGlass",root.transform.position+Vector3.up*.05f,new Vector3(2.25f,1.7f,2.35f),glassMaterial,Quaternion.identity,root.transform);
            CreateBox("Hanger",root.transform.position+Vector3.up*1.7f,new Vector3(.12f,1.7f,.12f),metalMaterial,Quaternion.identity,root.transform);
        }

        private void BuildLargeCabin(GameObject root,LiftDefinition d)
        {
            CreateBox("CableCarBody",root.transform.position,new Vector3(5f,3.2f,2.8f),darkMaterial,Quaternion.identity,root.transform);
            CreateBox("CableCarGlass",root.transform.position+Vector3.up*.1f,new Vector3(4.7f,2.4f,2.55f),glassMaterial,Quaternion.identity,root.transform);
            CreateBox("CableCarHanger",root.transform.position+Vector3.up*2.7f,new Vector3(.2f,2.5f,.2f),metalMaterial,Quaternion.identity,root.transform);
        }

        private void BuildTow(GameObject root,LiftDefinition d)
        {
            CreateCylinder("TowPole",root.transform.position+Vector3.up*.5f,.06f,1.3f,metalMaterial,Quaternion.identity,root.transform);
            if(d.type==LiftType.TBar) CreateBox("TBar",root.transform.position,new Vector3(1.1f,.1f,.12f),darkMaterial,Quaternion.identity,root.transform);
            else if(d.type==LiftType.Fork)
            {
                CreateBox("ForkStem",root.transform.position+Vector3.up*.05f,new Vector3(.1f,.1f,1.0f),metalMaterial,Quaternion.Euler(90,0,0),root.transform);
                CreateBox("ForkLeft",root.transform.position+new Vector3(-.38f,0,.55f),new Vector3(.1f,.1f,.8f),metalMaterial,Quaternion.Euler(0,35,0),root.transform);
                CreateBox("ForkRight",root.transform.position+new Vector3(.38f,0,.55f),new Vector3(.1f,.1f,.8f),metalMaterial,Quaternion.Euler(0,-35,0),root.transform);
            }
            else CreateCylinder("Platter",root.transform.position+Vector3.down*.05f,.3f,.08f,darkMaterial,Quaternion.identity,root.transform);
        }

        private void BuildPylon(Vector3 p,Vector3 dir,LiftDefinition d)
        {
            GameObject root=new GameObject("Pylon");root.transform.SetParent(transform);root.transform.position=p;generated.Add(root);
            float height=d.type==LiftType.TBar||d.type==LiftType.Platter||d.type==LiftType.Fork?2.8f:7.5f;
            // Keep the sheaves at the planned cable altitude, extend the shaft to the terrain.
            float ground=p.y-height;
            foreach(var hit in Physics.RaycastAll(p+Vector3.up*100f,Vector3.down,300f))
                if((hit.collider is MeshCollider) && hit.point.y<=p.y && hit.point.y>ground)
                    ground=hit.point.y-.15f;
            height=Mathf.Max(height,p.y-ground);
            CreateCylinder("Tower",p+Vector3.down*height*.5f,.18f,height,metalMaterial,Quaternion.identity,root.transform);
            CreateBox("Crossarm",p+Vector3.up*.1f,new Vector3(4f,.22f,.35f),metalMaterial,Quaternion.LookRotation(dir,Vector3.up),root.transform);
            CreateCylinder("ConcreteFoot", new Vector3(p.x, ground + .18f, p.z), .42f, .36f, concreteMaterial != null ? concreteMaterial : darkMaterial, Quaternion.identity, root.transform);
            CreateBox("PylonSnowCollar", new Vector3(p.x, ground + .38f, p.z), new Vector3(1.15f,.10f,1.15f), snowMaterial != null ? snowMaterial : metalMaterial, Quaternion.identity, root.transform);
            Quaternion orientation = Quaternion.LookRotation(dir.sqrMagnitude > .001f ? dir.normalized : Vector3.forward, Vector3.up);
            // Two mirrored sheave batteries: one for the uphill strand, one for the return strand.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 sideOffset = orientation * Vector3.right * (side * 1.05f);
                CreateBox("SheaveBatteryFrame", p + sideOffset + Vector3.down * .12f,
                    new Vector3(.20f, .12f, 2.45f), metalMaterial, orientation, root.transform);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 wheelPosition = p + sideOffset + orientation * Vector3.forward * ((i - 1.5f) * .62f);
                    BuildSheaveWheel(wheelPosition, orientation, root.transform, "Sheave_" + side + "_" + i);
                }
            }
        }

        // Procedural wheel details are visual only; the lift's existing movement logic is unchanged.
        // The wheel lies in the horizontal plane at the terminal; spokes make its rotation readable.
        private void BuildTerminalWheel(Vector3 center, Quaternion stationRotation, float radius, string name)
        {
            GameObject wheel = new GameObject(name + "_Assembly");
            wheel.transform.SetParent(transform);
            wheel.transform.position = center;
            wheel.transform.rotation = stationRotation;
            generated.Add(wheel);
            CreateCylinder(name + "_Hub", center, radius * .17f, .34f, metalMaterial,
                Quaternion.identity, wheel.transform);
            const int segments = 24;
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                float next = 2f * Mathf.PI * (i + 1) / segments;
                Vector3 a = center + stationRotation * new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Vector3 b = center + stationRotation * new Vector3(Mathf.Cos(next) * radius, 0, Mathf.Sin(next) * radius);
                Vector3 direction = b - a;
                CreateBeam(name + "_Rim", (a + b) * .5f, new Vector3(.15f, .19f, direction.magnitude),
                    direction.normalized, darkMaterial, wheel.transform);
                if (i % 3 == 0)
                {
                    Vector3 spoke = a - center;
                    CreateBeam(name + "_Spoke", center + spoke * .5f,
                        new Vector3(.10f, .13f, spoke.magnitude), spoke.normalized, metalMaterial, wheel.transform);
                }
            }
            CreateCylinder(name + "_Axle", center + Vector3.up * .18f, radius * .08f, .55f,
                darkMaterial, Quaternion.identity, wheel.transform);
        }

        // Pylon rollers have a transverse horizontal axle and sit in pairs beside the cable.
        private void BuildSheaveWheel(Vector3 center, Quaternion lineRotation, Transform parent, string name)
        {
            Quaternion axleRotation = lineRotation * Quaternion.Euler(0f, 0f, 90f);
            CreateCylinder(name + "_Tread", center, .25f, .11f, darkMaterial, axleRotation, parent);
            CreateCylinder(name + "_Hub", center + lineRotation * Vector3.right * .065f,
                .095f, .13f, metalMaterial, axleRotation, parent);
            CreateCylinder(name + "_Flange", center - lineRotation * Vector3.right * .07f,
                .28f, .025f, metalMaterial, axleRotation, parent);
        }

        private void BuildMagicCarpet(LiftDefinition d,Vector3 a,Vector3 b)
        {
            Vector3 mid=Vector3.Lerp(a,b,.5f)+Vector3.up*.45f;
            CreateBox("Carpet",mid,new Vector3(4f,.3f,Vector3.Distance(a,b)),darkMaterial,Quaternion.LookRotation((b-a).normalized,Vector3.up));
            CreateBox("SafetyRailLeft",mid+Vector3.right*2.3f,new Vector3(.1f,1.1f,Vector3.Distance(a,b)),metalMaterial,Quaternion.LookRotation((b-a).normalized,Vector3.up));
            CreateBox("SafetyRailRight",mid+Vector3.left*2.3f,new Vector3(.1f,1.1f,Vector3.Distance(a,b)),metalMaterial,Quaternion.LookRotation((b-a).normalized,Vector3.up));
        }

        private void BuildCable(string name,Vector3 a,Vector3 b,float thickness)
        {
            GameObject go=new GameObject(name);go.transform.SetParent(transform);generated.Add(go);
            var lr=go.AddComponent<LineRenderer>();lr.positionCount=16;lr.startWidth=thickness;lr.endWidth=thickness;lr.useWorldSpace=true;
            for(int i=0;i<16;i++){float t=i/15f;Vector3 p=Vector3.Lerp(a,b,t);p.y-=Mathf.Sin(t*Mathf.PI)*Mathf.Min(Vector3.Distance(a,b)*.035f,18f);lr.SetPosition(i,p);} 
        }

        private GameObject CreateBeam(string n, Vector3 pos, Vector3 scale, Vector3 dir, Material mat, Transform parent=null)
            => CreateBox(n, pos, scale, mat, Quaternion.LookRotation(dir, Vector3.up), parent);

        private GameObject CreateBox(string n,Vector3 pos,Vector3 scale,Material mat,Quaternion rot,Transform parent=null)
        {GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent??transform);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=scale;if(mat)g.GetComponent<Renderer>().sharedMaterial=mat;generated.Add(g);return g;}
        private GameObject CreateCylinder(string n,Vector3 pos,float radius,float height,Material mat,Quaternion rot,Transform parent=null)
        {GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(parent??transform);g.transform.position=pos;g.transform.rotation=rot;g.transform.localScale=new Vector3(radius,height*.5f,radius);if(mat)g.GetComponent<Renderer>().sharedMaterial=mat;generated.Add(g);return g;}
        private void Clear(){for(int i=generated.Count-1;i>=0;i--)if(generated[i]){if(Application.isPlaying)Destroy(generated[i]);else DestroyImmediate(generated[i]);}generated.Clear();}
    }
}
