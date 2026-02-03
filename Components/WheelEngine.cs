using System;
using CustomWeapons.Utils;
using UnityEngine;

namespace CustomWeapons.Components
{
	public class WheelEngine : MonoBehaviour, IEngine, IReportDamage
	{
		[SerializeField] private float maxRPM;
		[SerializeField] private float maxTorque;
		[SerializeField] private float idleRPM;
		[SerializeField] private float[] gearRatios;
		[SerializeField] private float finalDrive;
		[SerializeField] private AnimationCurve torqueCurve;
		
		[SerializeField] private Wheel[] wheels;

		[SerializeField] private UnitPart[] criticalParts;
		
		[SerializeField] private string failureMessage;
		[SerializeField] private AudioClip failureMessageAudio;
		[SerializeField] private float damageThreshold;
		[SerializeField] private AudioSource engineAudio;
		[SerializeField] private float engineMaxPitch;
		[SerializeField] private float fuelConsumption;
		[SerializeField] private float shiftDelay;
		
		private Aircraft aircraft;
		private ControlInputs controlInputs;
		private float rpm;
		private bool operable = true;
		private float currentTorque;
		private int currentGear;
		private bool hasFuel = true;
		private float lastFuelCheck;
		private float lastShiftTime;
		private float condition;

		private void Awake()
		{
			if (criticalParts == null || criticalParts.Length == 0)
			{
				Debug.LogError("WheelEngine has no critical parts!");
				enabled = false;
				return;
			}
			aircraft = criticalParts[0].parentUnit as Aircraft;
			aircraft.engineStates.Add(this);
			controlInputs = aircraft.GetInputs();
			UnitPart[] array = criticalParts;
			foreach (UnitPart part in array)
			{
				part.onApplyDamage += WheelEngine_OnApplyDamage;
				part.onParentDetached += WheelEngine_OnPartDetach;
			}
		}
		
		public float GetThrust()
		{
			return Math.Abs(currentTorque);
		}

		public float GetMaxThrust()
		{
			return maxTorque * finalDrive;
		}

		public float GetRPM()
		{
			return rpm;
		}

		public float GetRPMRatio()
		{
			return Mathf.Clamp01(rpm / maxRPM);
		}

		public void SetInteriorSounds(bool useInteriorSound)
		{
			//throw new NotImplementedException();
		}

		private void WheelEngine_OnApplyDamage(UnitPart.OnApplyDamage damage)
		{
			if (operable)
			{
				this.OnEngineDamage?.Invoke();
				condition = Mathf.Clamp((damage.hitPoints - damageThreshold) / (100f - damageThreshold), 0f, condition);
				if (condition <= 0f)
				{
					KillEngine();
				}
			}
		}

		private void WheelEngine_OnPartDetach(UnitPart part)
		{
			if (operable)
			{
				KillEngine();
			}
		}

		private void KillEngine()
		{
			operable = false;
			UnitPart[] parts = criticalParts;
			foreach (UnitPart part in parts)
			{
				if (!(part == null))
				{
					part.onApplyDamage -= WheelEngine_OnApplyDamage;
					part.onParentDetached -= WheelEngine_OnPartDetach;
				}
			}
			this.OnEngineDisable?.Invoke();
			this.onReportDamage?.Invoke(new OnReportDamage
			{
				failureMessage = failureMessage,
				audioReport = failureMessageAudio
			});
		}

		private void Animate()
		{
			if (rpm > 0f && !engineAudio.isPlaying)
			{
				engineAudio.Play();
			}

			engineAudio.dopplerLevel = 1f;
			engineAudio.pitch = rpm / maxRPM * engineMaxPitch;
		}

		private float GetAvgRPM()
		{
			float totalRPM = 0;
			foreach (Wheel wheel in wheels)
			{
				totalRPM += wheel.GetRPM();
			}
			return totalRPM / wheels.Length;
		}
		
		private void FixedUpdate()
		{
			Animate();
			bool flag = aircraft.Ignition && operable && hasFuel;
			
			float throttle = controlInputs.pitch;
			if (throttle is < 0.2f and > -0.2f)
			{
				throttle = 0f;
			}
			float yaw = controlInputs.yaw;
			float roll = controlInputs.roll;
			float steer = Mathf.Clamp(yaw + roll, -1f, 1f);
			float wheelRPM = Math.Abs(GetAvgRPM());
			
			float targetRPM = Mathf.Max(
				wheelRPM * gearRatios[currentGear],
				idleRPM
			);
			targetRPM = Mathf.Clamp(targetRPM, 0f, maxRPM);
			rpm = Mathf.Lerp(rpm, targetRPM, Time.fixedDeltaTime * 5f);

			float rpmRatio = rpm / maxRPM;
			if (Time.timeSinceLevelLoad > lastShiftTime + shiftDelay)
			{
				switch (Math.Abs(rpmRatio))
				{
					case > 0.9f when currentGear < gearRatios.Length - 1:
						currentGear++;
						break;
					case < 0.35f when currentGear > 0:
						currentGear--;
						break;
				}
			}


			float torqueFactor = 0.5f;
			float rpm01 = Mathf.Clamp(Mathf.Abs(rpm) / maxRPM, 0.01f, 0.99f);
			if (!float.IsNaN(rpm01) && !float.IsInfinity(rpm01))
			{
				torqueFactor = torqueCurve.EvaluateNormalizedTime(rpm01);
			}
			currentTorque = throttle * torqueFactor * maxTorque * gearRatios[currentGear] * finalDrive;
			
			float torquePerWheel = currentTorque / wheels.Length;

			bool braking = controlInputs.brake != 0f;
			float brakeTorque = braking ? maxTorque * 0.2f : 0f;
			if (!flag)
			{
				foreach (Wheel wheel in wheels)
				{
					wheel.SetTorque(0f, brakeTorque);
				}
				rpm = Mathf.Lerp(rpm, 0f, Time.fixedDeltaTime * 2f);
				return;
			}
			foreach (Wheel wheel in wheels)
			{
				wheel.SetTorque(torquePerWheel, brakeTorque);
				wheel.Steer(steer);
			}

			if (Time.timeSinceLevelLoad - lastFuelCheck > 1f)
			{
				UseFuel(fuelConsumption);
			}
		}

		private void UseFuel(float fuel)
		{
			lastFuelCheck = Time.timeSinceLevelLoad;
			hasFuel = aircraft.UseFuel(fuel);
		}

		public event Action? OnEngineDisable;
		public event Action? OnEngineDamage;
		public event Action<OnReportDamage>? onReportDamage;
	}
}