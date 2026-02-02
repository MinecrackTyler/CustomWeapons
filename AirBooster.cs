using System.Collections;
using UnityEngine;

namespace CustomWeapons
{
    public class AirBooster : MonoBehaviour
    {
        [Header("Missile & Physics")]
        [SerializeField] private Missile missile;
        private Rigidbody rb;

        [Header("Booster Settings")]
        [SerializeField] private float thrust;
        [SerializeField] private float burnTime;
        [SerializeField] private float fuelMass;
        [SerializeField] private float dryMass;
        [SerializeField] private float torque;
        [SerializeField] private float maxTurnRate;

        private float burnRate;
        private float originalTorque;
        private float originalMaxTurnRate;
        private bool activated;
        private bool separated;
        private bool splashed;

        [Header("FX References")]
        [SerializeField] private ParticleSystem[] particleSystems;
        [SerializeField] private TrailEmitter[] trailEmitters;
        [SerializeField] private AudioSource[] audioSources;
        [SerializeField] private Light[] lights;
        [SerializeField] private GameObject splashPrefab;
        

        [Header("Staging")]
        [Tooltip("Extra objects to detach after this booster burns out.")]
        [SerializeField] private DetachablePart[] detachableParts;

        public float DryMass => dryMass;
        public float FuelMass => fuelMass;
        
        private void Awake()
        {
            burnRate = fuelMass / burnTime;
        }

        public void Activate()
        {
            if (activated) return;

            activated = true;

            foreach (var ps in particleSystems)
                ps.Play();

            foreach (var audio in audioSources)
                audio.Play();

            foreach (var light in lights)
                light.enabled = true;

            originalTorque = missile.GetTorque();
            originalMaxTurnRate = missile.GetMaxTurnRate();
            missile.SetTorque(torque, maxTurnRate);
        }

        public float Thrust()
        {
            if (!activated) return 0f;

            fuelMass -= burnRate * Time.fixedDeltaTime;
            missile.rb.mass -= burnRate * Time.fixedDeltaTime;

            if (fuelMass <= 0f || transform.position.y < Datum.LocalSeaY)
                Burnout();

            if (missile.LocalSim)
                missile.rb.AddForce(thrust * missile.transform.forward);

            return thrust;
        }

        public void Splash()
        {
            splashed = true;
            GameObject obj = Instantiate(splashPrefab, Datum.origin);
            obj.transform.position = new Vector3(transform.position.x, Datum.LocalSeaY, transform.position.z);
            obj.transform.rotation = Quaternion.LookRotation(Vector3.up + rb.velocity.normalized);
            enabled = false;
            Destroy(obj, 20f);
        }

        public void Burnout()
        {
            if (separated) return;

            foreach (var ps in particleSystems)
                ps.Stop();

            foreach (var audio in audioSources)
                audio.Stop();

            foreach (var light in lights)
                light.enabled = false;

            separated = true;
            transform.SetParent(null);

            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = dryMass;
            rb.drag = 0.1f;
            rb.angularDrag = 0.01f;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.velocity = (missile.rb.isKinematic ? Vector3.zero : missile.rb.velocity);

            missile.SetTorque(originalTorque, originalMaxTurnRate);
            
            foreach (var dp in detachableParts)
            {
                if (dp.part != null)
                {
                    StartCoroutine(DetachPartAfterDelay(dp));
                }
            }

            Destroy(gameObject, 10f);
        }

        private IEnumerator DetachPartAfterDelay(DetachablePart dp)
        {
            yield return new WaitForSeconds(dp.detachDelay);

            dp.part.transform.SetParent(null);

            var partRb = dp.part.GetComponent<Rigidbody>();
            if (partRb == null)
                partRb = dp.part.AddComponent<Rigidbody>();

            partRb.mass = dp.mass;
            partRb.velocity = missile.rb.isKinematic ? Vector3.zero : missile.rb.velocity;
            partRb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void FixedUpdate()
        {
            if (fuelMass > 0f)
                Thrust();

            if (separated && !splashed && transform.position.y < Datum.LocalSeaY)
                Splash();
        }
        
        public float GetRemainingDeltaV(float currentMass)
        {
            if (burnRate == 0f)
                burnRate = fuelMass / burnTime;

            float massAfterBurn = currentMass - fuelMass;
            float burnDuration = fuelMass / burnRate;
            
            return thrust * burnDuration / ((currentMass + massAfterBurn) * 0.5f);
        }

        public float GetRemainingBurnTime()
        {
            if (burnRate == 0f)
                burnRate = fuelMass / burnTime;

            return fuelMass / burnRate;
        }

        public bool IsSeparated() => separated;
    }
}