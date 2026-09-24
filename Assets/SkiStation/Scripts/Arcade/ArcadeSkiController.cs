using UnityEngine;

namespace SkiStation.Arcade
{
    // Isolated player avatar: visitors and piste route simulation are untouched.
    public sealed class ArcadeSkiController : MonoBehaviour
    {
        public float maximumSpeed = 40f, steering = 125f;
        public float jumpImpulse = 13f, gravity = 29f;
        [Header("Charged jump - hold Space, release at the lip")]
        public float chargeSeconds = 0.85f;
        public float minimumJumpMultiplier = 0.65f;
        public float maximumJumpMultiplier = 1.65f;
        public float lipBonus = 0.20f;
        public float JumpCharge01 { get; private set; }
        public bool IsChargingJump { get; private set; }
        float jumpHoldStart;
        float lastGroundedAt;
        [Header("Trick-earned boost")]
        public float boostCapacity = 100f;
        public float boostGainPerCompletedRotation = 22f;
        public float boostMinimumTrickReward = 12f;
        public float boostConsumptionPerSecond = 26f;
        public float boostedMaximumSpeed = 68f;
        public float boostAcceleration = 48f;
        [Header("Full meter: 30 seconds of boost, timer runs ONLY while held")]
        public float unlimitedBoostSeconds = 30f;
        public float unlimitedBoostTimeRemaining { get; private set; }
        public bool UnlimitedBoostUnlocked { get; private set; }
        public bool IsUnlimitedBoosting { get { return IsBoosting && UnlimitedBoostUnlocked; } }
        public bool IsRegularBoosting { get { return IsBoosting && !UnlimitedBoostUnlocked; } }
        [Header("Trick recognition")]
        public float grabReward = 16f;
        public float trickComboReward = 9f;
        public string LastTrickName { get; private set; }
        public System.Action<string> TrickLanded;
        bool grabbedDuringJump;
        float grabDuration;
        float jumpPeakRotation;
        [Header("Consecutive landed tricks")]
        [Min(0f)] public float comboWindowSeconds = 12f;
        [Min(0f)] public float comboBonusPerTrick = 0.15f;
        [Range(1f, 3f)] public float maximumComboMultiplier = 2f;
        public int ComboCount { get; private set; }
        public float ComboMultiplier { get { return Mathf.Min(maximumComboMultiplier, 1f + Mathf.Max(0, ComboCount - 1) * comboBonusPerTrick); } }
        public float ComboTimeRemaining { get { return Mathf.Max(0f, comboExpiresAt - Time.time); } }
        public System.Action<int, float> ComboChanged;
        float comboExpiresAt;
        public KeyCode boostKey = KeyCode.LeftShift;
        public float Boost { get; private set; }
        public bool IsBoosting { get; private set; }
        public float BoostFraction { get { return Boost / Mathf.Max(1f, boostCapacity); } }
        public System.Action<float> BoostChanged;
        float jumpTotalRotation;
        float jumpMaximumRotation;
        [Header("Arcade turning - tune after first Unity test")]
        [Range(0f, 15f)] public float groundedCarveResponse = 7.5f;
        public LayerMask terrainAndObstacles = ~0;
        public bool IsCrashed { get; private set; }
        public bool IsAirborne { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float SteeringInput { get; private set; }
        public bool IsBraking { get; private set; }
        public bool IsSwitch { get; private set; }
        public Vector3 TravelVelocity { get { return horizontalVelocity; } }
        public float LastLandingAngleError { get; private set; }
        Vector3 jumpTravelDirection = Vector3.forward;
        bool checkpointSwitch;
        public float Speed { get { return horizontalVelocity.magnitude; } }
        public float TrickAngle { get { return trickAngle; } }
        public float LastLandingImpact { get; private set; }
        public float LastJumpDrop { get; private set; }
        public bool LastLandingHadSuccessfulTrick { get; private set; }
        float jumpHighestY;
        public System.Action<float> Landed;
        public System.Action Crashed;
        public System.Action Retried;

        Vector3 horizontalVelocity, checkpointPosition, checkpointVelocity;
        Quaternion checkpointRotation;
        float verticalVelocity, trickAngle, lastCheckpointTime;
        bool checkpointReady, attemptedTrick;
        Collider ownCollider;

        void Awake() { ownCollider = GetComponent<Collider>(); }
        public void BeginAt(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            horizontalVelocity = Vector3.zero; verticalVelocity = 0f;
            trickAngle = 0f; attemptedTrick = false; checkpointReady = false;
            Boost = 0f; IsBoosting = false; UnlimitedBoostUnlocked = false; unlimitedBoostTimeRemaining = 0f; LastTrickName = ""; grabbedDuringJump = false; grabDuration = 0f; jumpPeakRotation = 0f; ComboCount = 0; comboExpiresAt = 0f; jumpTotalRotation = 0f; jumpMaximumRotation = 0f;
            IsCrashed = false; IsAirborne = false; IsChargingJump = false; JumpCharge01 = 0f; jumpHighestY = position.y; LastJumpDrop = 0f; LastLandingHadSuccessfulTrick = false; enabled = true;
            GroundNormal = Vector3.up; lastGroundedAt = Time.time;
            LastLandingImpact = 0f;
            IsSwitch = false; checkpointSwitch = false; LastLandingAngleError = 0f;
            jumpTravelDirection = transform.forward;
            if (Retried != null) Retried();
        }
        bool GroundBelow(out RaycastHit hit, float distance, float raise = 0f)
        {
            RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * (0.65f + raise),
                Vector3.down, distance + 0.65f + raise, terrainAndObstacles, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity; hit = new RaycastHit(); bool found = false;
            foreach (RaycastHit candidate in hits)
            {
                if (candidate.collider == ownCollider || candidate.collider.transform.IsChildOf(transform) || candidate.normal.y < 0.45f) continue;
                if (candidate.distance < nearest) { nearest = candidate.distance; hit = candidate; found = true; }
            }
            return found;
        }
        void Update()
        {
            if (IsCrashed) { IsBoosting = false; if (Input.GetKeyDown(KeyCode.R)) Retry(); return; }
            if (ComboCount > 0 && Time.time > comboExpiresAt) ResetCombo();
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;
            bool wasAirborne = IsAirborne;
            SteeringInput = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            IsBraking = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            RaycastHit ground;
            bool nearGround = GroundBelow(out ground, 0.5f);
            // An airborne frame finishes through the landing branch, preserving
            // impact speed, trick reward and the pre-jump retry checkpoint.
            bool grounded = !wasAirborne && nearGround && verticalVelocity <= 0f && transform.position.y <= ground.point.y + 0.22f;
            IsAirborne = !grounded;
            if (nearGround) GroundNormal = ground.normal;
            if (!wasAirborne && !grounded)
            {
                jumpHighestY = transform.position.y;
                LastLandingHadSuccessfulTrick = false;
                jumpTravelDirection = horizontalVelocity.sqrMagnitude > .01f ? horizontalVelocity.normalized
                    : transform.forward * (IsSwitch ? -1f : 1f);
            }
            if (grounded) lastGroundedAt = Time.time;
            if (grounded && Input.GetKeyDown(KeyCode.Space))
            {
                IsChargingJump = true;
                jumpHoldStart = Time.time;
            }
            if (IsChargingJump)
                JumpCharge01 = Mathf.Clamp01((Time.time - jumpHoldStart) / Mathf.Max(0.1f, chargeSeconds));
            bool releaseJump = IsChargingJump && Input.GetKeyUp(KeyCode.Space);
            if (grounded)
            {
                transform.position = new Vector3(transform.position.x, ground.point.y + 0.12f, transform.position.z);
                verticalVelocity = -0.5f;
                // Keep the approach checkpoint a short distance before takeoff, not at the impact.
                if (!checkpointReady || Time.time - lastCheckpointTime > 0.3f)
                {
                    checkpointPosition = transform.position; checkpointRotation = transform.rotation;
                    checkpointVelocity = horizontalVelocity; checkpointReady = true; lastCheckpointTime = Time.time;
                    checkpointSwitch = IsSwitch;
                }
                if (releaseJump)
                {
                    // Preserve a safe approach point, not the landing position, for immediate retries.
                    if (checkpointReady)
                    {
                        Vector3 approach = horizontalVelocity.sqrMagnitude > 0.1f
                            ? -horizontalVelocity.normalized : -transform.forward;
                        RaycastHit previousGround;
                        Vector3 candidate = transform.position + approach * 3.5f;
                        if (Physics.Raycast(candidate + Vector3.up * 5f, Vector3.down,
                            out previousGround, 12f, terrainAndObstacles, QueryTriggerInteraction.Ignore)
                            && previousGround.normal.y > 0.5f)
                            checkpointPosition = previousGround.point + Vector3.up * 0.12f;
                        else checkpointPosition = transform.position;
                        checkpointRotation = transform.rotation;
                        checkpointVelocity = horizontalVelocity;
                        checkpointSwitch = IsSwitch;
                    }
                    float chargePower = Mathf.Lerp(minimumJumpMultiplier, maximumJumpMultiplier, JumpCharge01);
                    // A release close to a natural lip receives a modest reward; no forced fall.
                    if (ground.normal.z > 0.18f) chargePower += lipBonus;
                    verticalVelocity = jumpImpulse * chargePower;
                    IsChargingJump = false; JumpCharge01 = 0f;
                    IsAirborne = true; jumpHighestY = transform.position.y;
                    jumpTravelDirection = horizontalVelocity.sqrMagnitude > .01f ? horizontalVelocity.normalized
                        : transform.forward * (IsSwitch ? -1f : 1f);
                    attemptedTrick = false; grabbedDuringJump = false; grabDuration = 0f; trickAngle = 0f; jumpTotalRotation = 0f; jumpMaximumRotation = 0f; jumpPeakRotation = 0f;
                }
            }
            else
            {
                if (releaseJump && Time.time - lastGroundedAt <= 0.12f)
                {
                    verticalVelocity = jumpImpulse * Mathf.Lerp(minimumJumpMultiplier, maximumJumpMultiplier, JumpCharge01);
                    IsChargingJump = false; JumpCharge01 = 0f;
                    jumpHighestY = transform.position.y;
                    attemptedTrick = false; grabbedDuringJump = false; grabDuration = 0f;
                    trickAngle = 0f; jumpTotalRotation = 0f; jumpMaximumRotation = 0f; jumpPeakRotation = 0f;
                }
                else if (releaseJump || (IsChargingJump && !Input.GetKey(KeyCode.Space)))
                {
                    IsChargingJump = false; JumpCharge01 = 0f;
                }
                if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.G))
                {
                    attemptedTrick = true;
                    float deltaRotation = (Input.GetKey(KeyCode.F) ? 1f : -1f) * 400f * dt;
                    trickAngle += deltaRotation;
                    jumpTotalRotation += deltaRotation;
                    jumpMaximumRotation = Mathf.Max(jumpMaximumRotation, Mathf.Abs(jumpTotalRotation));
                }
                // H is a simple grab: a valid non-rotation trick when held in the air.
                if (Input.GetKey(KeyCode.H)) { grabbedDuringJump = true; grabDuration += dt; attemptedTrick = true; }
                jumpPeakRotation = Mathf.Max(jumpPeakRotation, Mathf.Abs(jumpTotalRotation));
                jumpHighestY = Mathf.Max(jumpHighestY, transform.position.y);
                verticalVelocity -= gravity * dt;
            }
            transform.Rotate(0f, SteeringInput * steering * dt * (!IsAirborne && IsSwitch ? -1f : 1f), 0f, Space.World);
            // Up/W/Z provide no propulsion. In flight, turning the skier must not
            // redirect the trajectory and automatically forgive a sideways landing.
            if (!IsAirborne)
            {
                Vector3 desired = Vector3.ProjectOnPlane(transform.forward, ground.normal).normalized
                    * (IsSwitch ? -1f : 1f);
                Vector3 direction = horizontalVelocity.sqrMagnitude > .0025f
                    ? Vector3.Slerp(horizontalVelocity.normalized, desired,
                        1f - Mathf.Exp(-groundedCarveResponse * dt)).normalized : desired;
                direction = Vector3.ProjectOnPlane(direction, ground.normal).normalized;
                float nextSpeed = ArcadeSkiRules.StepSpeed(horizontalVelocity.magnitude, direction,
                    gravity, IsBraking ? 2.1f : .10f, dt);
                if (nextSpeed < 0f) IsSwitch = !IsSwitch;
                horizontalVelocity = direction * nextSpeed;
            }
            IsBoosting = Input.GetKey(boostKey) && !IsBraking
                && (UnlimitedBoostUnlocked ? unlimitedBoostTimeRemaining > 0f : Boost > 0f);
            if (IsBoosting)
            {
                if (UnlimitedBoostUnlocked)
                {
                    unlimitedBoostTimeRemaining = Mathf.Max(0f, unlimitedBoostTimeRemaining - dt);
                    if (unlimitedBoostTimeRemaining <= 0f)
                    {
                        UnlimitedBoostUnlocked = false; Boost = 0f; IsBoosting = false;
                    }
                }
                else Boost = Mathf.Max(0f, Boost - boostConsumptionPerSecond * dt);
                if (IsBoosting)
                {
                    Vector3 boostDirection = horizontalVelocity.sqrMagnitude > .01f ? horizontalVelocity.normalized
                        : (IsAirborne ? jumpTravelDirection : Vector3.ProjectOnPlane(transform.forward, GroundNormal).normalized * (IsSwitch ? -1f : 1f));
                    horizontalVelocity += boostDirection * boostAcceleration * dt;
                }
                if (BoostChanged != null) BoostChanged(BoostFraction);
            }
            // Momentum remains after releasing boost; normal drag brings speed down gradually.
            horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity,
                IsBoosting ? Mathf.Max(maximumSpeed, boostedMaximumSpeed) : Mathf.Max(maximumSpeed, horizontalVelocity.magnitude - 6f * dt));
            if (!IsBoosting && horizontalVelocity.magnitude > maximumSpeed)
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity,
                    horizontalVelocity.normalized * maximumSpeed, 6f * dt);
            Vector3 motion = (horizontalVelocity + Vector3.up * verticalVelocity) * dt;
            // Ground-tangent velocity already contains vertical motion; do not retain
            // that component in flight, where gravity owns the vertical channel.
            if (IsAirborne) { horizontalVelocity.y = 0f; motion = (horizontalVelocity + Vector3.up * verticalVelocity) * dt; }
            if (motion.sqrMagnitude > 0.00001f)
            {
                RaycastHit obstacle;
                if (Physics.SphereCast(transform.position + Vector3.up * 0.6f, 0.42f, motion.normalized,
                    out obstacle, motion.magnitude + 0.3f, terrainAndObstacles, QueryTriggerInteraction.Ignore)
                    && obstacle.collider != ownCollider && obstacle.normal.y < 0.45f)
                {
                    horizontalVelocity = Vector3.ProjectOnPlane(horizontalVelocity, obstacle.normal) * 0.82f;
                    motion = (horizontalVelocity + Vector3.up * verticalVelocity) * dt;
                }
            }
            transform.position += motion;
            // Sweep down from the previous height so a fast fall cannot skip
            // a thin terrain collider between two frames.
            if ((wasAirborne || IsAirborne) && verticalVelocity <= 0f && GroundBelow(out ground, 1.5f, Mathf.Max(0f, -motion.y))
                && transform.position.y <= ground.point.y + 0.15f)
            {
                LastLandingImpact = Mathf.Abs(verticalVelocity);
                LastJumpDrop = Mathf.Max(0f, jumpHighestY - ground.point.y);
                LastLandingHadSuccessfulTrick = false;
                Vector3 landingTrajectory = Vector3.ProjectOnPlane(horizontalVelocity, Vector3.up);
                if (landingTrajectory.sqrMagnitude < .01f) landingTrajectory = jumpTravelDirection;
                Quaternion landingRotation = transform.rotation * Quaternion.Euler(0f, trickAngle, 0f);
                Vector3 skiDirection = landingRotation * Vector3.forward;
                LastLandingAngleError = ArcadeSkiRules.LandingError(skiDirection, landingTrajectory, ground.normal);
                transform.position = new Vector3(transform.position.x, ground.point.y + 0.12f, transform.position.z);
                verticalVelocity = -0.5f; IsAirborne = false;
                GroundNormal = ground.normal;
                // Every landing is checked, even if the player only used arrows in air.
                if (!ArcadeSkiRules.CanLand(skiDirection, landingTrajectory, ground.normal))
                {
                    IsCrashed = true; ResetCombo(); if (Crashed != null) Crashed();
                }
                else
                {
                    transform.rotation = landingRotation;
                    IsSwitch = Vector3.Dot(Vector3.ProjectOnPlane(skiDirection, ground.normal),
                        Vector3.ProjectOnPlane(landingTrajectory, ground.normal)) < 0f;
                    // A completed grab OR aligned rotation OR both earns boost.
                    // Rotation count and grab duration increase reward; failed landings earn nothing.
                    float rotations = Mathf.Abs(jumpTotalRotation) / 360f;
                    bool validRotation = Mathf.Abs(jumpTotalRotation) >= 160f;
                    bool validGrab = grabbedDuringJump && grabDuration >= 0.12f;
                    if (validRotation || validGrab)
                    {
                        LastLandingHadSuccessfulTrick = true;
                        ComboCount = (ComboCount > 0 && Time.time <= comboExpiresAt) ? ComboCount + 1 : 1;
                        comboExpiresAt = Time.time + comboWindowSeconds;
                        float reward = (validRotation ? boostGainPerCompletedRotation * Mathf.Max(1f, rotations) : 0f)
                            + (validGrab ? grabReward * Mathf.Clamp(grabDuration / 0.4f, 1f, 2f) : 0f)
                            + (validRotation && validGrab ? trickComboReward : 0f);
                        reward = Mathf.Max(boostMinimumTrickReward, reward) * ComboMultiplier;
                        int roundedDegrees = Mathf.RoundToInt(Mathf.Abs(jumpTotalRotation) / 180f) * 180;
                        LastTrickName = validRotation ? (validGrab ? "Grab + " : "") + "Rotation " + roundedDegrees + "°" : "Grab";
                        if (ComboChanged != null) ComboChanged(ComboCount, ComboMultiplier);
                        if (!UnlimitedBoostUnlocked)
                        {
                            Boost = Mathf.Min(boostCapacity, Boost + reward);
                            if (Boost >= boostCapacity - 0.001f)
                            {
                                Boost = boostCapacity;
                                UnlimitedBoostUnlocked = true;
                                unlimitedBoostTimeRemaining = unlimitedBoostSeconds;
                            }
                        }
                        if (BoostChanged != null) BoostChanged(BoostFraction);
                        if (TrickLanded != null) TrickLanded(LastTrickName);
                    }
                    if (Landed != null) Landed(LastLandingImpact);
                }
                attemptedTrick = false; grabbedDuringJump = false; grabDuration = 0f; trickAngle = 0f; jumpTotalRotation = 0f; jumpMaximumRotation = 0f; jumpPeakRotation = 0f;
            }
        }
        void ResetCombo()
        {
            if (ComboCount == 0) return;
            ComboCount = 0; comboExpiresAt = 0f;
            if (ComboChanged != null) ComboChanged(0, 1f);
        }
        public void Retry()
        {
            if (!checkpointReady) return;
            transform.SetPositionAndRotation(checkpointPosition, checkpointRotation);
            horizontalVelocity = checkpointVelocity; verticalVelocity = -0.5f;
            IsSwitch = checkpointSwitch; LastLandingAngleError = 0f;
            jumpTravelDirection = checkpointVelocity.sqrMagnitude > .01f ? checkpointVelocity.normalized
                : transform.forward * (IsSwitch ? -1f : 1f);
            IsChargingJump = false; JumpCharge01 = 0f;
            lastGroundedAt = Time.time; jumpHighestY = checkpointPosition.y;
            LastLandingImpact = 0f; LastJumpDrop = 0f; LastLandingHadSuccessfulTrick = false;
            attemptedTrick = false; grabbedDuringJump = false; grabDuration = 0f; trickAngle = 0f; jumpTotalRotation = 0f; jumpMaximumRotation = 0f; jumpPeakRotation = 0f; IsCrashed = false; IsAirborne = false; IsBoosting = false;
            if (Retried != null) Retried();
        }
        public void StopPlaying() { enabled = false; IsBoosting = false; horizontalVelocity = Vector3.zero; }
    }
}
