using UnityEngine;

namespace SkiStation
{
    /// <summary>Management-only: road arrival, optional real-visitor building visit, then first lift.</summary>
    [DisallowMultipleComponent]
    public sealed class StationVisitorRoadArrival248 : MonoBehaviour
    {
        private SkierRuntime skier;
        private Vector3[] roadPath;
        private Vector3[] accessPath;
        private GameObject visitBuilding;
        private int nextPoint;
        private int accessIndex;
        private int phase; // 0 road, 1 roadside approach, 2 door, 3 waiting, 4 returning, 5 walking back to front de neige
        private float waitUntil;
        private float lastProgressTime;
        private Vector3 lastProgressPosition;
        private bool warnedRoadBlock;
        // Equipment is assigned once per arrival, never rerolled while waiting.
        private bool ownsEquipment;
        private bool roadCompleted;
        private bool arrivalCancelled;
        // Read-only diagnostics: never advance a visit or grant an admission from the UI.
        public bool HasEquipment => ownsEquipment;
        public string DiagnosticGoal => arrivalCancelled ? "Journee terminee" :
            phase == 0 ? "Rejoindre le front de neige" : phase == 6 ? "Trouver une location accessible" :
            phase == 4 || phase == 5 ? "Rejoindre le front de neige" :
            mandatoryEquipmentStop ? "Louer du materiel" : mandatoryTicketStop ? "Obtenir un forfait" : "Rejoindre le service";
        public string DiagnosticDestination => visitBuilding != null ? visitBuilding.name : "Front de neige";
        public string DiagnosticWait
        {
            get
            {
                if (arrivalCancelled) return "Arrivee interrompue a la fermeture";
                if (!IsArriving) return "";
                if (roadPath == null || roadPath.Length < 2) return "Route d'arrivee invalide";
                if (phase == 6) return "Aucune location ouverte et accessible trouvee a proximite";
                if (phase == 3) return mandatoryTicketStop ? "File ou service au guichet" : "Service de location en cours";
                if (roadCompleted && ownsEquipment && skier != null && !VisitorSkiAdmission.HasPass(skier)
                    && (ArrivalHub250.I == null || ArrivalHub250.I.Office == null)) return "Guichet forfaits indisponible";
                return "";
            }
        }
        public bool ReadyToSki => !IsArriving && roadCompleted && ownsEquipment && !arrivalCancelled;
        public string ArrivalStage => !IsArriving ? "Arrivee terminee" : phase == 0 ? "Sur la route" : phase == 6 ? "Attend un magasin de ski" : phase == 3 ? (mandatoryTicketStop ? "Au guichet forfaits" : "Dans le commerce") : phase == 5 ? "Retour au front de neige" : "Vers le commerce";
        private bool mandatoryEquipmentStop;
        private bool mandatoryTicketStop;
        private float nextShopSearch;
        private bool warnedNoShop;
        public bool IsArriving { get; private set; } = true;

        public void Configure(Vector3[] permanentRoad)
        {
            skier = GetComponent<SkierRuntime>();
            roadPath = permanentRoad == null ? null : (Vector3[])permanentRoad.Clone();
            accessPath = null;
            visitBuilding = null;
            accessIndex = 0;
            waitUntil = 0f;
            nextPoint = 1;
            phase = 0;
            roadCompleted = false;
            arrivalCancelled = false;
            IsArriving = true;
            lastProgressTime = Time.time;
            lastProgressPosition = transform.position;
            warnedRoadBlock = false;
            var identity = GetComponent<ArrivalIdentity250>();
            ownsEquipment = identity != null ? identity.OwnEquipment : Random.value < .65f;
            mandatoryEquipmentStop = false;
            nextShopSearch = 0f;
            warnedNoShop = false;
            if (skier != null) skier.SetState(SkierState.Arriving);
            if (roadPath != null && roadPath.Length > 1) WalkToNextRoadPoint();
            else { skier?.StopMoving(); Debug.LogWarning("SkiStation: route invalide, arrivee bloquee.", this); }
        }

        private void Update()
        {
            if (!IsArriving && !arrivalCancelled && skier != null && skier.State == SkierState.Resting &&
                GetComponent<ArrivalIdentity250>() != null && !VisitorSkiAdmission.HasPass(skier) &&
                ManagementJourneyAccess.CanExitToVillage(transform.position))
            { IsArriving = true; skier.SetState(SkierState.Arriving); ReturnToFrontDeNeige(); }
            if (!IsArriving || skier == null || roadPath == null || roadPath.Length < 2) return;
            // Closing may end equipment waiting, but never skips the arrival road.
            if (roadCompleted && GameClock.I != null && GameClock.I.CurrentHour >= 18.5f)
            {
                arrivalCancelled = true;
                IsArriving = false;
                skier.StopMoving();
                skier.SetState(SkierState.Resting);
                return;
            }
            // A building can be removed while a visitor is approaching or waiting at its door.
            // Never continue towards an orphaned access path or dereference it after deletion.
            if (phase >= 1 && phase <= 4 && visitBuilding == null)
            {
                Debug.LogWarning("SkiStation: destination building removed during optional visit; resuming lift search.", this);
                ReturnToFrontDeNeige();
                return;
            }
            // A stalled optional building visit must never trap a real skier indefinitely.
            // Do not teleport a visitor or bypass the mandatory arrival road.
            if (phase != 3 && phase != 6)
            {
                Vector3 current = transform.position;
                if ((current - lastProgressPosition).sqrMagnitude > 1f)
                {
                    lastProgressPosition = current;
                    lastProgressTime = Time.time;
                }
                else if (Time.time - lastProgressTime > 12f)
                {
                    if (phase == 0)
                    {
                        if (!warnedRoadBlock)
                        {
                            Debug.LogWarning("SkiStation: visitor stopped on arrival road; check road waypoint and skier movement.", this);
                            warnedRoadBlock = true;
                        }
                        lastProgressTime = Time.time;
                    }
                    else
                    {
                        ReturnToFrontDeNeige();
                        return;
                    }
                }
            }
            if (phase == 6)
            {
                if (Time.time >= nextShopSearch) FindEquipmentShop();
                return;
            }
            if (phase == 3)
            {
                if (Time.time < waitUntil) return;
                if (mandatoryEquipmentStop)
                {
                    // Equipment is acquired only after reaching the actual shop door.
                    var shop = visitBuilding != null ? visitBuilding.GetComponent<StationEquipmentShop249>() : null;
                    int indicativePrice;
                    if (shop == null || !shop.TryProvideEquipment(0, out indicativePrice))
                    {
                        // Shop closed or removed: never grant equipment or board a lift.
                        mandatoryEquipmentStop = false;
                        ReturnToFrontDeNeige();
                        return;
                    }
                    ownsEquipment = true;
                    var identity = GetComponent<ArrivalIdentity250>(); if (identity != null) identity.OwnEquipment = true;
                    mandatoryEquipmentStop = false;
                }
                if (mandatoryTicketStop)
                {
                    var office = visitBuilding != null ? visitBuilding.GetComponent<ArrivalTicketOffice250>() : null;
                    if (office == null) { mandatoryTicketStop = false; ReturnToFrontDeNeige(); return; }
                    if (!office.TryServe(this)) { waitUntil = Time.time + .25f; return; }
                    mandatoryTicketStop = false;
                }
                phase = 4;
                lastProgressPosition = transform.position;
                lastProgressTime = Time.time;
                accessIndex = accessPath.Length - 2;
                WalkToAccessPoint();
                return;
            }
            Vector3 target;
            if (phase == 0)
            {
                if (roadPath == null || nextPoint >= roadPath.Length) { AtRoadTerminus(); return; }
                target = roadPath[nextPoint];
            }
            else if (phase == 1) target = accessPath[0];
            else if (phase == 5)
            {
                if (roadPath == null || roadPath.Length == 0) { FinishArrival(); return; }
                target = roadPath[roadPath.Length - 1];
            }
            else if (phase == 2 || phase == 4)
            {
                if (accessPath == null || accessIndex < 0 || accessIndex >= accessPath.Length)
                { FinishArrival(); return; }
                target = accessPath[accessIndex];
            }
            else return;
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                new Vector2(target.x, target.z)) > 2f) return;
            if (phase == 0)
            {
                nextPoint++;
                if (nextPoint < roadPath.Length) WalkToNextRoadPoint();
                else AtRoadTerminus();
            }
            else if (phase == 1) { phase = 2; accessIndex = 1; WalkToAccessPoint(); }
            else if (phase == 2)
            {
                accessIndex++;
                if (accessIndex < accessPath.Length) WalkToAccessPoint();
                else { skier.StopMoving(); phase = 3; waitUntil = Time.time + 3f; }
            }
            else if (phase == 4)
            {
                accessIndex--;
                if (accessIndex >= 0) WalkToAccessPoint();
                else ReturnToFrontDeNeige();
            }
            else if (phase == 5) FinishArrival();
        }

        private void AtRoadTerminus()
        {
            roadCompleted = true;
            // Assign lodging only after the visitor has physically completed the arrival road.
            var stay = GetComponent<StationVisitorStay249>();
            if (stay == null) stay = gameObject.AddComponent<StationVisitorStay249>();
            stay.AssignOnArrival();
            if (!ownsEquipment) { FindEquipmentShop(); return; }
            FinishArrival();
        }

        private static bool IsEquipmentShop(GameObject building)
        {
            if (building == null) return false;
            var shop = building.GetComponent<StationEquipmentShop249>();
            if (shop == null || !shop.IsOpen) return false;
            var status = building.GetComponent<StationStructureStatus249>();
            return status == null || status.CanWelcomeVisitors;
        }

        private void FindEquipmentShop()
        {
            var network = StationPedestrianNetwork249.I;
            if (network != null) network.PruneDestroyed();
            StationPedestrianNetwork249.Access nearestAccess = null;
            float nearestSq = 80f * 80f;
            if (network != null)
                foreach (var access in network.Accesses)
                {
                    if (!IsEquipmentShop(access.Building) || access.RoadToDoor == null || access.RoadToDoor.Length < 2) continue;
                    Vector3 delta = access.RoadToDoor[0] - transform.position;
                    float distanceSq = delta.x * delta.x + delta.z * delta.z;
                    if (distanceSq >= nearestSq) continue;
                    nearestSq = distanceSq;
                    nearestAccess = access;
                }
            if (nearestAccess == null)
            {
                // Never board a lift without equipment; wait at the front de neige.
                phase = 6;
                skier.StopMoving();
                nextShopSearch = Time.time + 5f;
                if (!warnedNoShop)
                {
                    Debug.LogWarning("SkiStation: visitor without equipment is waiting for an accessible ski equipment shop; lift boarding blocked.", this);
                    warnedNoShop = true;
                }
                return;
            }
            warnedNoShop = false;
            mandatoryEquipmentStop = true;
            accessPath = (Vector3[])nearestAccess.RoadToDoor.Clone();
            visitBuilding = nearestAccess.Building;
            phase = 1;
            lastProgressPosition = transform.position;
            lastProgressTime = Time.time;
            MoveTo(accessPath[0]);
        }

        // A building visit ends at its roadside access, not necessarily at the front de neige.
        // Return to the road terminus before searching for the first lift.
        private void ReturnToFrontDeNeige()
        {
            accessPath = null;
            visitBuilding = null;
            if (roadPath == null || roadPath.Length == 0) { FinishArrival(); return; }
            phase = 5;
            lastProgressPosition = transform.position;
            lastProgressTime = Time.time;
            MoveTo(roadPath[roadPath.Length - 1]);
        }

        private void WalkToNextRoadPoint() { MoveTo(roadPath[nextPoint]); }
        private void WalkToAccessPoint()
        {
            if (accessIndex < 0 || accessIndex >= accessPath.Length) { FinishArrival(); return; }
            MoveTo(accessPath[accessIndex]);
        }
        private void MoveTo(Vector3 point)
        {
            if (ManagementSurface.TryGet(point, out var ground)) point = ground;
            skier.MoveTo(point);
        }
        private void FinishArrival()
        {
            if (!IsArriving || !roadCompleted) return;
            if (roadPath != null && roadPath.Length > 0 &&
                Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                    new Vector2(roadPath[roadPath.Length - 1].x, roadPath[roadPath.Length - 1].z)) > 2f)
            { ReturnToFrontDeNeige(); return; }
            if (!ownsEquipment)
            {
                FindEquipmentShop();
                return;
            }
            if (GetComponent<ArrivalIdentity250>() != null && !VisitorSkiAdmission.HasPass(skier))
            {
                var hub = ArrivalHub250.I;
                if (hub == null || hub.Office == null) { skier.StopMoving(); return; }
                mandatoryTicketStop = true;
                mandatoryEquipmentStop = false;
                visitBuilding = hub.Office.gameObject;
                accessPath = (Vector3[])hub.Office.Access.Clone();
                phase = 1;
                lastProgressPosition = transform.position;
                lastProgressTime = Time.time;
                MoveTo(accessPath[0]);
                return;
            }
            IsArriving = false;
            accessPath = null;
            visitBuilding = null;
            if (skier == null) return;
            skier.StopMoving();
            skier.SetState(SkierState.Resting);
            var agent = GetComponent<SkierRouteAgent>();
            if (agent != null) agent.QueueNextAvailableLift(transform.position);
        }
    }
}
