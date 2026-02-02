using System;
using System.Reflection;
using UnityEngine;

namespace CustomWeapons
{
    public class ASATSeeker : MissileSeeker
    {
        [Header("Seeker")]
        [SerializeField] private float lockPerseverance = 2f;
        [SerializeField] private float homingLockDelay = 0.5f;
        [SerializeField] private float minReacquireRange = 2000f;
        [SerializeField] private float datalinkPositionalError = 20f;
        [SerializeField] private float maxTrackingAngle = 75f;
        [SerializeField] private float armDelay = 0.5f;
        [SerializeField] private float guidanceDelay = 0.25f;
        [SerializeField] private float terminalRange = 80000f;
        [SerializeField] private float maxLeadTime = 90f;
        [SerializeField] private float loftAmount = 0.0f;
        [SerializeField] private RadarParams radarParameters;
        [Range(0f, 1f)]
        [SerializeField] private float jamTolerance = 0.25f;
        
        [SerializeField] private RCS rcs;

        [Header("Booster Controller")]
        [SerializeField] private MultiStageBoosterController multiStageBoosterController;

        // internal state
        private GlobalPosition knownPos;
        private Vector3 knownVel;
        private Vector3 knownVelPrev;
        private Vector3 knownAccel; 
        private float lastActiveTrackAttempt;
        private float lastDatalinkTrackAttempt;
        private float timeWithoutReturn;
        private float returnStrength;
        private float homingLockTime;
        private float jamAccumulation;
        private float topSpeed;
        private float targetDist;
        private float timeToTarget;
        private Vector3 positionalErrorVector;
        private bool armed;
        private bool guidance;
        private bool isJammed;
        private bool radarLockEstablished;
        private bool achievedLock;
        private object warheadInstance;
        private FieldInfo warheadField;
        private FieldInfo detonatedField;
        private bool hasDetonated = false;
        
        
        public override void Initialize(Unit target, GlobalPosition aimpoint)
        {
            missile.NetworkseekerMode = Missile.SeekerMode.passive;
            positionalErrorVector = UnityEngine.Random.insideUnitSphere * datalinkPositionalError;
            missile.onJam += ASATSeeker_OnJam;
            lastActiveTrackAttempt = Time.timeSinceLevelLoad;
            topSpeed = missile.GetTopSpeed(0f, 0f);
            targetUnit = target;
            knownPos = missile.GlobalPosition() + missile.transform.forward * 100000f;
            missile.SetAimpoint(knownPos, Vector3.zero);
            this.StartSlowUpdateDelayed(1f, SlowChecks);
            
            Type missileType = typeof(Missile);
            warheadField = missileType.GetField("warhead", BindingFlags.NonPublic | BindingFlags.Instance);

            if (warheadField != null)
            {
                warheadInstance = warheadField.GetValue(missile);

                if (warheadInstance != null)
                {
                    Type warheadType = warheadInstance.GetType();
                    detonatedField = warheadType.GetField("detonated", BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }

            if (warheadField == null || detonatedField == null)
            {
                Debug.LogError("[ASATSeeker] Could not reflect warhead or detonated field!");
            }
        }

        private void SlowChecks()
        {
            if (missile.disabled) return;

            if (!missile.IsArmed() && missile.timeSinceSpawn > armDelay)
            {
                missile.Arm();
            }

            if (!guidance && missile.timeSinceSpawn > guidanceDelay)
            {
                guidance = true;
                missile.DeployFins();
            }
            
            if (loftAmount > 0f)
            {
                Vector3 vector = knownPos - missile.GlobalPosition();
                float a = Vector3.Dot(vector.normalized, missile.rb.velocity);
                if (targetUnit != null && missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var knownPosition))
                {
                    vector = knownPosition - missile.GlobalPosition();
                }
                targetDist = vector.magnitude;
                timeToTarget = targetDist / Mathf.Max(a, 10f);
            }
        }

        public override string GetSeekerType()
        {
            return "ARH";
        }

        private void ASATSeeker_OnJam(Unit.JamEventArgs e)
        {
            jamAccumulation += e.jamAmount;
            missile.RecordDamage(e.jammingUnit.persistentID, 0.01f);
        }

        private float GetRadarReturn()
        {
            if (Time.timeSinceLevelLoad - lastActiveTrackAttempt < 0.5f)
                return returnStrength;
            lastActiveTrackAttempt = Time.timeSinceLevelLoad;

            if (!(targetUnit is IRadarReturn radarReturn))
                return 0f;
            if (isJammed)
                return 0f;

            GlobalPosition gpMissile = missile.GlobalPosition();
            GlobalPosition gpTarget = targetUnit.GlobalPosition();
            Vector3 flat = gpTarget - gpMissile;
            flat.y = 0f;
            float distance = FastMath.Distance(gpMissile, gpTarget);
            float horiz = flat.magnitude;

            float sqrtA = Mathf.Sqrt(12742000f * gpMissile.y);
            float sqrtB = Mathf.Sqrt(12742000f * gpTarget.y);

            // simple earth-curvature check (as original)
            if (sqrtA + sqrtB < distance)
                return 0f;

            if (distance > radarParameters.maxRange)
                return 0f;

            if (returnStrength < radarParameters.minSignal && distance < minReacquireRange)
                return 0f;

            if (Vector3.Angle(transform.forward, targetUnit.transform.position - transform.position) > maxTrackingAngle)
                return 0f;

            if (!TargetCalc.LineOfSight(transform, targetUnit.transform, 10f))
                return 0f;

            float num4 = 0f;
            if (horiz < sqrtA && gpTarget.y < gpMissile.y * (1f - horiz / sqrtA))
            {
                float num5 = distance * targetUnit.radarAlt / (gpMissile.y - gpTarget.y);
                num4 += Mathf.Min(distance, 1000f) / num5;
            }
            num4 += targetUnit.maxRadius * targetUnit.maxRadius * 2f / (targetUnit.radarAlt * targetUnit.radarAlt);
            
            float val = radarReturn.GetRadarReturn(missile.transform.position, null, missile, distance, num4, radarParameters, triggerWarning: true);
            
            Debug.Log($"[Radar] Dist={distance}, RCS={targetUnit.RCS}, return={val}");
            return val;
        }

        private void Update()
        {
            CheckDetonation();
        }
        
        public override void Seek()
        {
            
            if (missile.targetID.NotValid)
            {
                missile.SetAimpoint(knownPos, Vector3.zero);
                return;
            }
            
            if (!armed && missile.timeSinceSpawn > armDelay)
            {
                armed = true;
                missile.Arm();
                missile.SetTangible(true);
            }

            if (!guidance && missile.timeSinceSpawn > guidanceDelay)
            {
                guidance = true;
                missile.DeployFins();
            }
            
            jamAccumulation -= Mathf.Max(jamAccumulation, 0.2f) * Mathf.Max(jamTolerance, 0.1f) * Time.deltaTime;
            jamAccumulation = Mathf.Clamp01(jamAccumulation);
            isJammed = jamAccumulation > jamTolerance;

            if (targetUnit == null)
            {
                missile.SetTarget(null);
                missile.SetAimpoint(missile.GlobalPosition() + missile.transform.forward * 10000f, Vector3.zero);
                return;
            }
            
            if (!radarLockEstablished)
            {
                DatalinkMode();
            }
            else
            {
                TerminalMode();
            }

            if (!guidance) return;
            
            Vector3 platformVel = ((missile.timeSinceSpawn < 3f) ? (missile.transform.forward * topSpeed) : missile.rb.velocity);
            
            float remainingDV = missile.GetRemainingDeltaV() + multiStageBoosterController.GetRemainingDeltaV();
            remainingDV = Math.Max(remainingDV, 0f);
            float effSpeed = Mathf.Max(platformVel.magnitude, topSpeed);
            effSpeed += remainingDV * 0.07f;
            
            float distance = FastMath.Distance(missile.GlobalPosition(), knownPos);
            float estTime = distance / Mathf.Max(effSpeed, 50f);
            float adaptiveMaxLead = Mathf.Clamp(estTime, 0f, maxLeadTime);
            
            Vector3 leadVectorWithAccel = TargetCalc.GetLeadVectorWithAccel(knownPos, missile.GlobalPosition(), knownVel, platformVel, knownAccel, adaptiveMaxLead);
            
            if (loftAmount > 0f)
            {
                if (missile.timeSinceSpawn < 3f)
                {
                    timeToTarget = distance / topSpeed;
                }
                float loft = Mathf.Min(timeToTarget * timeToTarget * 4.905f * loftAmount, distance * loftAmount);
                leadVectorWithAccel += loft * Vector3.up;
                timeToTarget -= Time.fixedDeltaTime;
            }
            
            missile.SetAimpoint(knownPos + leadVectorWithAccel, knownVel);
            
            if (rcs != null && rcs.enabled && multiStageBoosterController.IsFinished())
            {
                rcs.CorrectTrajectory(missile.airDensity, knownPos, knownVel, missile.rb, knownPos + leadVectorWithAccel);
            }

            if (this.transform.position.GlobalY() > armDelay)
            {
                missile.Arm();
            }
        }

        private bool CheckDetonation() //speed very fast, warhead no worky normally.
        {
            if (hasDetonated) 
                return true;

            if (warheadInstance == null || detonatedField == null) 
                return false;

            bool detonated = (bool)detonatedField.GetValue(warheadInstance);
            if (detonated)
            {
                hasDetonated = true;

                float distance = Vector3.Distance(missile.transform.position, targetUnit.transform.position);
                
                if (targetUnit != null && targetUnit is Satellite sat && distance < 1000)
                {
                    PersistentID ownerId = missile.ownerID;
                    float damageAmount = 99999f;

                    sat.RecordDamage(ownerId, damageAmount);
                    sat.Networkdisabled = true;
                    sat.ReportKilled();
                }

                return true;
            }

            return false;
        }
        
        
        private void DatalinkMode()
        {
            if (Time.timeSinceLevelLoad - lastDatalinkTrackAttempt < 1f) return;
            lastDatalinkTrackAttempt = Time.timeSinceLevelLoad;

            if (FastMath.Distance(knownPos, missile.GlobalPosition()) < terminalRange)
            {
                returnStrength = GetRadarReturn();
                Missile.SeekerMode seekerMode = ((returnStrength > radarParameters.minSignal) ? Missile.SeekerMode.activeLock : Missile.SeekerMode.activeSearch);
                if (missile.seekerMode != seekerMode)
                {
                    missile.NetworkseekerMode = seekerMode;
                }
            }

            if (returnStrength > radarParameters.minSignal)
            {
                if (missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var pos))
                {
                    knownPos = pos;
                    knownVel = ((targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
                }
                radarLockEstablished = true;
                if (!achievedLock && targetUnit is Aircraft aircraft)
                {
                    aircraft.RecordDamage(missile.ownerID, 0.001f);
                    achievedLock = true;
                }
                return;
            }
            
            if (missile.NetworkHQ.IsTargetBeingTracked(targetUnit))
            {
                knownVel = ((targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
            }

            GlobalPosition knownPosition;
            if (!missile.NetworkHQ.IsTargetPositionAccurate(targetUnit, 2000f) || Vector3.Angle(targetUnit.transform.position - transform.position, transform.forward) > maxTrackingAngle)
            {
                knownVel = Vector3.zero;
                knownPos = missile.GlobalPosition() + missile.transform.forward * 10000f;
                missile.SetTarget(null);
            }
            else if (missile.NetworkHQ.TryGetKnownPosition(targetUnit, out knownPosition))
            {
                knownPos = knownPosition + positionalErrorVector;
            }
        }

        private void TerminalMode()
        {
            returnStrength = GetRadarReturn();
            Missile.SeekerMode seekerMode = ((returnStrength > radarParameters.minSignal) ? Missile.SeekerMode.activeLock : Missile.SeekerMode.activeSearch);
            if (missile.seekerMode != seekerMode)
            {
                missile.NetworkseekerMode = seekerMode;
            }

            if (returnStrength < radarParameters.minSignal)
            {
                homingLockTime = 0f;
                if (missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var kp))
                {
                    knownPos = kp + positionalErrorVector;
                }
                if (Vector3.Angle(knownPos - missile.GlobalPosition(), missile.transform.forward) > maxTrackingAngle)
                {
                    knownPos = missile.GlobalPosition() + missile.transform.forward * 1000f;
                }
                timeWithoutReturn += Time.deltaTime;
                if (timeWithoutReturn > lockPerseverance)
                {
                    missile.SetTarget(null);
                    targetUnit = null;
                }
            }
            else
            {
                homingLockTime += Time.fixedDeltaTime;
                timeWithoutReturn = 0f;
                if (homingLockTime > homingLockDelay)
                {
                    if (targetUnit != null)
                    {
                        knownPos = targetUnit.GlobalPosition();
                        knownVel = ((targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero);
                        knownAccel = (knownVel - knownVelPrev) / Time.fixedDeltaTime;
                        knownVelPrev = knownVel;
                        missile.SetTarget(targetUnit);
                    }
                }
            }
        }
    }
}
