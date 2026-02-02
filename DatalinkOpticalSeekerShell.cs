using UnityEngine;

namespace CustomWeapons
{
    public class DatalinkOpticalSeekerShell : MissileSeeker
    {
        [SerializeField] private float searchRadius = 50f;
        [SerializeField] private float maxTargetSpeed = 30f;
        [SerializeField] private float datalinkPositionalError = 15f;
        [SerializeField, Range(0f, 1f)] private float correctionFactor = 0.15f; 
        // 0 = pure ballistic, 1 = full correction

        private GlobalPosition knownPos;
        private Vector3 knownVel;
        private bool hasVisual;
        private float lastVisualCheck;
        private float timeToTarget;
        private float trajectoryError;
        private Transform targetTransform;
        private GlobalPosition datalinkPos;
        private Vector3 positionalErrorVector;

        private GameObject aimpointDebug;

        public override void Initialize(Unit target, GlobalPosition aimpoint)
        {
            float fallTime = Kinematics.FallTime(missile.GlobalPosition().y, missile.rb.velocity.y);
            Vector3 flatVel = new Vector3(missile.rb.velocity.x, 0f, missile.rb.velocity.z);

            knownPos = missile.GlobalPosition() + flatVel * fallTime;
            knownPos.y = 0f;

            missile.DeployFins();
            missile.NetworkseekerMode = Missile.SeekerMode.passive;

            if (UnitRegistry.TryGetUnit(missile.targetID, out targetUnit))
            {
                targetTransform = target.GetRandomPart();

                if (missile.NetworkHQ.TryGetKnownPosition(targetUnit, out var knownPosition))
                {
                    knownPos = knownPosition;
                }
            }

            positionalErrorVector = Random.insideUnitSphere * datalinkPositionalError;

            if (PlayerSettings.debugVis)
            {
                aimpointDebug = Object.Instantiate(GameAssets.i.debugPoint, Datum.origin);
                aimpointDebug.transform.localPosition = knownPos.AsVector3();
                aimpointDebug.transform.localScale = Vector3.one * 3f;
            }

            this.StartSlowUpdateDelayed(0.5f, SlowChecks);
        }

        private void SlowChecks()
        {
            if (missile.disabled) return;

            if (missile.NetworkHQ != null && targetUnit != null)
            {
                if (missile.NetworkHQ.TryGetKnownPosition(targetUnit, out datalinkPos))
                {
                    knownPos = datalinkPos + positionalErrorVector;
                }
            }

            if (missile.speed < 1f && missile.IsArmed())
            {
                missile.Detonate(Vector3.up, hitArmor: false, hitTerrain: true);
            }

            missile.UpdateRadarAlt();

            Vector3 diff = knownPos - missile.GlobalPosition();
            float fallTime = Kinematics.FallTime(-diff.y, missile.rb.velocity.y);
            diff.y = 0f;
            float distance = diff.magnitude;
            float alongVel = Vector3.Dot(diff.normalized, missile.rb.velocity);
            timeToTarget = Mathf.Max(distance, 10f) / Mathf.Max(alongVel, 10f);
            trajectoryError = timeToTarget / fallTime;
        }

        public override string GetSeekerType() => "Optical";

        private bool TrackVisual()
        {
            lastVisualCheck = Time.timeSinceLevelLoad;

            if (FastMath.InRange(targetUnit.GlobalPosition(), knownPos, searchRadius + targetUnit.maxRadius))
            {
                return targetUnit.LineOfSight(base.transform.position, 1000f);
            }
            return false;
        }

        private void GetTargetParameters()
        {
            if (targetUnit == null || targetTransform == null) return;

            if (Time.timeSinceLevelLoad - lastVisualCheck > 0.25f)
            {
                hasVisual = TrackVisual();
            }

            if (hasVisual)
            {
                knownPos = targetTransform.GlobalPosition();
                knownVel = (targetUnit.rb != null) ? targetUnit.rb.velocity : Vector3.zero;
            }
            else
            {
                knownPos += knownVel * Time.fixedDeltaTime;
            }
        }

        private void SendTargetInfo()
        {
            if (hasVisual && missile.targetID.NotValid) missile.SetTarget(targetUnit);
            if (!hasVisual && missile.targetID.IsValid) missile.SetTarget(null);

            // Default: ballistic continuation
            GlobalPosition ballisticAim = missile.GlobalPosition() + missile.rb.velocity * Time.fixedDeltaTime;
            GlobalPosition guidedAim = ballisticAim;

            if (missile.rb.velocity.y < 0f)
            {
                Vector3 clampedVel = (maxTargetSpeed < 1000f)
                    ? Vector3.ClampMagnitude(knownVel, maxTargetSpeed)
                    : knownVel;

                Vector3 lead = TargetCalc.GetLeadVector(
                    knownPos, missile.GlobalPosition(), clampedVel, missile.rb.velocity, 10f);

                guidedAim = knownPos + lead + timeToTarget * timeToTarget * 4.905f * trajectoryError * Vector3.up;
            }

            // Blend ballistic with guided correction
            Vector3 blended = Vector3.Lerp(
                ballisticAim.AsVector3(),
                guidedAim.AsVector3(),
                correctionFactor
            );

            GlobalPosition finalAim = new GlobalPosition(blended);

            if (PlayerSettings.debugVis)
            {
                aimpointDebug.transform.localPosition = finalAim.AsVector3();
            }

            timeToTarget -= Time.fixedDeltaTime;

            if (!missile.IsTangible() && missile.owner != null &&
                !FastMath.InRange(missile.owner.GlobalPosition(), missile.GlobalPosition(), 15f))
            {
                missile.SetTangible(true);
            }

            missile.SetAimpoint(finalAim, knownVel);
        }

        public override void Seek()
        {
            GetTargetParameters();
            SendTargetInfo();
        }
    }
}
