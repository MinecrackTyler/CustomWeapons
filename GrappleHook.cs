using System;
using System.Reflection;
using UnityEngine;

namespace CustomWeapons
{
    public class GrappleHook : SlingloadHook
    {
        [SerializeField] private float launchSpeed = 200f;
        [SerializeField] private GameObject hookPrefab;

        [Header("Options")]
        [SerializeField] private bool reinforcedMode = true;

        private bool fired;
        private GameObject currentHook;
        private Rigidbody currentHookRb;

        private Aircraft aircraft;

        // Reflection helpers
        private Action retractingState;
        private Action connectedState;
        private FieldInfo baseAircraftField;
        private MethodInfo loadEnterWaterMethod;
        private MethodInfo loadExitWaterMethod;
        private FieldInfo suspendedUnitField;
        private FieldInfo loadInWaterField;
        private FieldInfo lineRendererField;
        private FieldInfo lineLengthField;

        private ConfigurableJoint joint;

        private Unit suspendedUnitRef => suspendedUnitField != null ? (Unit)suspendedUnitField.GetValue(this) : null;
        private bool loadInWaterRef
        {
            get => loadInWaterField != null && (bool)loadInWaterField.GetValue(this);
            set { if (loadInWaterField != null) loadInWaterField.SetValue(this, value); }
        }
        private LineRenderer lineRendererRef => lineRendererField != null ? (LineRenderer)lineRendererField.GetValue(this) : null;

        private void Awake()
        {
            Type baseType = typeof(SlingloadHook);

            MethodInfo mRetracting = baseType.GetMethod("RetractingState", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo mConnected = baseType.GetMethod("ConnectedState", BindingFlags.NonPublic | BindingFlags.Instance);
            if (mRetracting != null)
                retractingState = (Action)Delegate.CreateDelegate(typeof(Action), this, mRetracting);
            if (mConnected != null)
                connectedState = (Action)Delegate.CreateDelegate(typeof(Action), this, mConnected);

            baseAircraftField = baseType.GetField("aircraft", BindingFlags.NonPublic | BindingFlags.Instance);
            suspendedUnitField = baseType.GetField("suspendedUnit", BindingFlags.NonPublic | BindingFlags.Instance);
            loadInWaterField = baseType.GetField("loadInWater", BindingFlags.NonPublic | BindingFlags.Instance);
            lineRendererField = baseType.GetField("lineRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
            lineLengthField = baseType.GetField("lineLength", BindingFlags.NonPublic | BindingFlags.Instance);

            loadEnterWaterMethod = baseType.GetMethod("LoadEnterWater", BindingFlags.NonPublic | BindingFlags.Instance);
            loadExitWaterMethod = baseType.GetMethod("LoadExitWater", BindingFlags.NonPublic | BindingFlags.Instance);

            RefreshAircraft();
        }

        public override void AttachToUnit(Unit unit)
        {
            base.AttachToUnit(unit);
            RefreshAircraft();
        }

        public new void FixedUpdate()
        {
            switch (deployState)
            {
                case DeployState.Retracting:
                    retractingState?.Invoke();
                    break;
                case DeployState.Connected:
                    connectedState?.Invoke();
                    break;
                case DeployState.RescuePilot:
                    break;
                case DeployState.Deployed:
                    DeployedState();
                    break;
            }

            // Cleanup when fully retracted
            if (deployState == DeployState.Retracted)
            {
                if (currentHook != null)
                {
                    if (joint != null)
                    {
                        Destroy(joint);
                        joint = null;
                    }
                    Destroy(currentHook);
                    currentHook = null;
                    currentHookRb = null;
                }

                fired = false;
            }

            // Pull rope shorter while retracting
            if (deployState == DeployState.Retracting && currentHook != null && joint != null)
            {
                joint.linearLimit = new SoftJointLimit { limit = GetLineLength() };
            }
        }

        private void DeployedState()
        {
            if (!fired && GetLineLength() > 0f)
            {
                deployState = DeployState.Retracting;
                return;
            }

            if (!fired)
            {
                fired = true;
                LaunchHook();
            }

            if (currentHook != null)
                currentHook.SetActive(true);

            // Try to attach if the hook is active
            if (fired && currentHook != null && aircraft != null && aircraft.weaponManager != null)
            {
                var targets = aircraft.weaponManager.GetTargetList();
                foreach (var unit in targets)
                {
                    if (unit == null) continue;
                    if (unit.IsSlung()) continue;

                    Vector3 targetPoint = unit.transform.position +
                                          0.5f * unit.definition.height * unit.transform.up;

                    float dist = Vector3.Distance(currentHook.transform.position, targetPoint);
                    
                    if (dist < 25f) // tweak this radius
                    {
                        if (FastMath.InRange(currentHookRb.velocity, unit.rb.velocity, 1000f))
                        {
                            Rigidbody attachBody = reinforcedMode
                                ? aircraft.rb
                                : GetComponentInParent<Rigidbody>();

                            float dist2 = Vector3.Distance(attachBody.position, targetPoint);
                            

                            lineLengthField?.SetValue(this, dist2);
                            Debug.Log(lineLengthField?.GetValue(this));

                            // Attach the unit
                            aircraft.SetSlingLoadAttachment(unit, DeployState.Connected);

                            // Hide hook visuals once attached
                            if (currentHook != null)
                                currentHook.SetActive(false);

                            break;
                        }
                    }
                }
            }
        }

        private void LaunchHook()
        {
            if (hookPrefab == null) return;

            currentHook = Instantiate(hookPrefab, winch.position, winch.rotation);
            currentHookRb = currentHook.GetComponent<Rigidbody>();

            if (currentHookRb != null)
                currentHookRb.velocity = winch.forward * launchSpeed;

            // Add/configure joint
            Rigidbody attachBody = reinforcedMode
                ? aircraft.rb
                : GetComponentInParent<Rigidbody>();

            joint = attachBody.gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = winch.localPosition;
            joint.connectedAnchor = Vector3.zero;
            joint.connectedBody = currentHookRb;

            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;

            // Limit angular swinging so it doesn't whip around aircraft
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit angLimit = default;
            angLimit.limit = 20f; // degrees allowed to swing
            joint.angularYLimit = angLimit;
            joint.angularZLimit = angLimit;

            // Use actual lineMaxLength for first shot
            joint.linearLimit = new SoftJointLimit { limit = GetLineMaxLength() };
            joint.linearLimitSpring = new SoftJointLimitSpring
            {
                spring = 500f,
                damper = 500f
            };

            // Start with rope fully extended
            if (deployState != DeployState.Connected)
            {
                lineLengthField?.SetValue(this, GetLineMaxLength());
            }
            
        }

        private new void Update()
        {
            UpdateRopeCustom();

            var suspended = suspendedUnitRef;
            if (suspended == null) return;

            if (!loadInWaterRef)
            {
                if (suspended.transform.position.y < Datum.LocalSeaY)
                    loadEnterWaterMethod?.Invoke(this, null);
            }
            else if (suspended.transform.position.y > Datum.LocalSeaY)
            {
                loadExitWaterMethod?.Invoke(this, null);
            }
        }

        private void UpdateRopeCustom()
        {
            var lineRend = lineRendererRef;
            if (lineRend == null) return;

            Vector3 ropeEnd = hook.position;

            var suspended = suspendedUnitRef;
            if (deployState == DeployState.Connected && suspended != null)
            {
                ropeEnd = suspended.transform.position +
                          0.5f * suspended.definition.height * suspended.transform.up;
            }
            else if (fired && currentHook != null)
            {
                ropeEnd = currentHook.transform.position;
            }

            for (int i = 0; i < lineRend.positionCount; i++)
            {
                float t = (float)i / (lineRend.positionCount - 1);
                lineRend.SetPosition(i, Vector3.Lerp(winch.position, ropeEnd, t));
            }
        }

        private void RefreshAircraft()
        {
            if (baseAircraftField != null)
                aircraft = (Aircraft)baseAircraftField.GetValue(this);
        }
    }
}
