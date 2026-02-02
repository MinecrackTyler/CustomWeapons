using UnityEngine;

namespace CustomWeapons
{
	public class RCS : MonoBehaviour
    {
        public bool enabled = true;
        private float lastFired;
        
        [SerializeField]
        private ParticleSystem[] thrusters_PosX; // thrust right  left nozzle
        [SerializeField]
        private ParticleSystem[] thrusters_NegX; // thrust left  right nozzle
        [SerializeField]
        private ParticleSystem[] thrusters_PosY; // thrust up  bottom nozzle
        [SerializeField]
        private ParticleSystem[] thrusters_NegY; // thrust down  top nozzle
        [SerializeField]
        private ParticleSystem[] thrusters_PosZ; // thrust forward  rear nozzle
        [SerializeField]
        private ParticleSystem[] thrusters_NegZ; // thrust back  front nozzle

        public void CorrectTrajectory(
            float airDensity,
            GlobalPosition targetPosition,
            Vector3 targetKnownVel,
            Rigidbody rb,
            GlobalPosition aimpoint)
        {
            if (!enabled)
                return;
            
            if (airDensity <= 0.01f && (Time.timeSinceLevelLoad - lastFired) >= 0.1f)
            {
                lastFired = Time.timeSinceLevelLoad;
                
                float timeToIntercept = Mathf.Max(
                    0.001f,
                    (targetPosition - rb.transform.GlobalPosition()).magnitude /
                    Mathf.Max(0.1f, rb.velocity.magnitude)
                );

                GlobalPosition predictedTargetPos = targetPosition + targetKnownVel * timeToIntercept;
                
                Vector3 toAimpoint = (aimpoint - rb.transform.GlobalPosition());
                Vector3 desiredVel = toAimpoint.normalized * rb.velocity.magnitude;
                Vector3 desiredVelChange = desiredVel - rb.velocity;
                
                Vector3 localVelChange = rb.transform.InverseTransformDirection(desiredVelChange);
                
                localVelChange.x = Mathf.Clamp(localVelChange.x, -10f, 10f);
                localVelChange.y = Mathf.Clamp(localVelChange.y, -10f, 10f);
                localVelChange.z = Mathf.Clamp(localVelChange.z, -0f, 0f); //it doesn't like this one
                
                Vector3 clampedWorldForce = rb.transform.TransformDirection(localVelChange);
                rb.AddForce(clampedWorldForce, ForceMode.VelocityChange);
                
                FireThrusters(localVelChange);
            }
        }

        private void FireThrusters(Vector3 localVelChange)
        {
            if (localVelChange.x > 0.01f) TriggerThrusters(thrusters_PosX); 
            else if (localVelChange.x < -0.01f) TriggerThrusters(thrusters_NegX); 


            if (localVelChange.y > 0.01f) TriggerThrusters(thrusters_PosY); 
            else if (localVelChange.y < -0.01f) TriggerThrusters(thrusters_NegY); 


            if (localVelChange.z > 0.01f) TriggerThrusters(thrusters_PosZ); 
            else if (localVelChange.z < -0.01f) TriggerThrusters(thrusters_NegZ); 
        }

        private void TriggerThrusters(ParticleSystem[] thrusterList)
        {
            foreach (var ps in thrusterList)
            {
                if (ps != null && !ps.isPlaying)
                    ps.Play();
            }
        }
    }
}