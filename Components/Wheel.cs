using UnityEngine;

namespace CustomWeapons.Components
{
	public class Wheel : MonoBehaviour
	{
		[SerializeField] private float maxSteerAngle = 30f;
		[SerializeField] private float steerSpeed = 120f;

		[SerializeField] private bool driveWheel = true;
		
		[SerializeField] private WheelCollider wheelCollider;
		[SerializeField] private Transform visualWheel;

		private float currentSteerAngle;

		public float GetRPM()
		{
			return wheelCollider.rpm;
		}

		public void SetTorque(float motor, float brake)
		{
			if (driveWheel)
			{
				wheelCollider.motorTorque = motor;
			}
			else
			{
				wheelCollider.motorTorque = 1f; // i think this is the issue?
			}
			wheelCollider.brakeTorque = brake;
		}
		
		public void Steer(float steerAmount)
		{
			float targetAngle = steerAmount * maxSteerAngle;
			currentSteerAngle = Mathf.MoveTowards(
				currentSteerAngle,
				targetAngle,
				steerSpeed * Time.deltaTime
			);

			wheelCollider.steerAngle = currentSteerAngle;
		}

		private void LateUpdate()
		{
			UpdateVisual();
		}

		private void UpdateVisual()
		{
			if (visualWheel == null || wheelCollider == null)
				return;

			wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
			visualWheel.position = pos;
			visualWheel.rotation = rot;
		}
	}
}