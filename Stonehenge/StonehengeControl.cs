using System;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomWeapons.Stonehenge
{
	public class StonehengeControl : MonoBehaviour
	{
		public Turret turret;
		[SerializeField] private float timeToTargetCalcInterval;
		[SerializeField] private float targetLeadAccuracy;
		[SerializeField] private Unit attachedUnit;

		public Unit AttachedUnit => attachedUnit;

		private WeaponStation weaponStation;
		private TurretCoordinator turretCoordinator;

		private Vector3 targetPos;
		private Vector3 targetVel;
		private Vector3 prevTargetVel;
		private FactionHQ oldHQ;

		private bool onTarget;
		
		private float lastTimeCalc;
		private float targetRange;
		private float timeToTarget;
		private float velocityGuess;
		private Transform elevationTransform;
		private static MethodInfo aimTurretMethod;



		private void Awake()
		{
			if (turret != null)
			{
				FieldInfo disabledField = typeof(Turret).GetField("disabled",
					BindingFlags.NonPublic | BindingFlags.Instance);

				if (disabledField != null)
				{
					disabledField.SetValue(turret, true);
					Debug.Log($"[Stonehenge] Disabled internal Turret logic on {turret.name}");
				}
				else
				{
					Debug.LogWarning("[Stonehenge] Could not find 'disabled' field on Turret via reflection.");
				}
			}

			// Grab the private "disabled" field from the Turret class
			
			FieldInfo elevationTransformField = typeof(Turret).GetField("elevationTransform", BindingFlags.NonPublic | BindingFlags.Instance);

			if (elevationTransformField != null)
			{
				elevationTransform = (Transform)elevationTransformField.GetValue(turret);
			}
			else
			{
				//welp
				Destroy(this);
			}

			weaponStation = turret.GetWeaponStation();
			attachedUnit.onDisableUnit += StonehengeControl_OnUnitDisable;
			attachedUnit.onChangeFaction += StonehengeControl_OnChangeFaction;
		}

		private void OnEnable()
		{
			if (attachedUnit?.NetworkHQ != null)
			{
				oldHQ = attachedUnit.NetworkHQ;
				StonehengeRegistry.RegisterTurret(this, attachedUnit.NetworkHQ);
			}
		}

		private void OnDisable()
		{
			if (attachedUnit?.NetworkHQ != null)
			{
				StonehengeRegistry.DeregisterTurret(this, attachedUnit.NetworkHQ);
			}
		}

		public void Aim(Vector3 targetPos, Vector3 targetVel)
		{
			this.targetPos = targetPos;
			this.targetVel = targetVel;
			
			Vector3 predictedPos = targetPos + timeToTarget * velocityGuess * targetVel;
			Vector3 accel = Vector3.zero;
			if (prevTargetVel != Vector3.zero)
			{
				accel = (targetVel - prevTargetVel) / Time.fixedDeltaTime;
			}
			
			prevTargetVel = targetVel;
			if (accel.sqrMagnitude > 0f)
			{
				float accelTime = Mathf.Min(timeToTarget, 3f);
				predictedPos += accel * (0.5f * accelTime * accelTime);
			}

			Vector3 gravity = Vector3.up * (4.905f * (1f - weaponStation.WeaponInfo.dragCoef) * timeToTarget * timeToTarget);

			Vector3 aimVector = predictedPos + gravity - elevationTransform.position;
			
			turret.SetVector(aimVector);
			AimTurret(aimVector);
			onTarget = turret.IsOnTarget();
		}

		private void AimTurret(Vector3 aimVector)
		{
			if (turret == null) return;

			Type turretType = typeof(Turret);
			
			Type[] parameterTypes = new Type[] { typeof(Vector3) };
			
			
			if (aimTurretMethod == null)
			{
				aimTurretMethod = turretType.GetMethod("AimTurret", 
					BindingFlags.NonPublic | BindingFlags.Instance, 
					null, // Binder
					parameterTypes,
					null); // ParameterModifier


				if (aimTurretMethod == null)
				{
					Debug.LogWarning("[Stonehenge] Could not find private AimTurret(Vector3) method.");
					return;
				}
			}
			
			aimTurretMethod.Invoke(turret, new object[] { aimVector });
		}
		
		private void FixedUpdate()
		{
			if (!attachedUnit.disabled)
			{
				UpdateTOT();
			}
			
		}
		
		private void UpdateTOT()
		{
			targetRange = FastMath.Distance(elevationTransform.position, targetPos);
			float maxRange = weaponStation.WeaponInfo.targetRequirements.maxRange;
			if (targetRange > maxRange * 1.5)
			{
				timeToTarget = targetRange / Mathf.Max(weaponStation.WeaponInfo.muzzleVelocity * 0.4f, 1f);
			}
			else
			{
				float deltaTime = Time.timeSinceLevelLoad - lastTimeCalc;
				Vector3 vel = targetVel;
				RaycastHit hit;
				float radarAlt = targetPos.ToGlobalPosition().y;
				// if (Physics.Linecast(targetPos, targetPos - Vector3.up * 10000f, out hit, 2112))
				// {
				// 	radarAlt = hit.distance;
				// }
				// if (radarAlt < 10f)
				// {
				// 	vel.y = 0;
				// }

				if (deltaTime > timeToTargetCalcInterval)
				{
					lastTimeCalc = Time.timeSinceLevelLoad;
					velocityGuess = Mathf.Lerp(Random.Range(0, 2), 1f, targetLeadAccuracy);
					timeToTarget = CalcTools.TargetPosLead(targetPos, targetVel, elevationTransform.gameObject, weaponStation.WeaponInfo.muzzleVelocity, weaponStation.WeaponInfo.dragCoef, 1);
				}

				timeToTarget = Mathf.Min(timeToTarget, (maxRange / weaponStation.WeaponInfo.muzzleVelocity) * 0.25f);
			}
		}

		public bool IsOnTarget()
		{
			return onTarget;
		}

		public void SetCoordinator(TurretCoordinator coordinator)
		{
			this.turretCoordinator = coordinator;
		}

		private void OnDestroy()
		{
			if (turretCoordinator != null)
			{
				turretCoordinator.DeregisterController(this);
			}
			if (attachedUnit?.NetworkHQ != null)
			{
				StonehengeRegistry.DeregisterTurret(this, attachedUnit.NetworkHQ);
			}

			attachedUnit.onDisableUnit -= StonehengeControl_OnUnitDisable;
			attachedUnit.onChangeFaction -= StonehengeControl_OnChangeFaction;
		}

		private void StonehengeControl_OnUnitDisable(Unit unit)
		{
			if (turretCoordinator != null)
			{
				turretCoordinator.DeregisterController(this);
			}
			if (attachedUnit?.NetworkHQ != null)
			{
				StonehengeRegistry.DeregisterTurret(this, attachedUnit.NetworkHQ);
			}
		}

		private void StonehengeControl_OnChangeFaction(Unit unit)
		{
			if (turretCoordinator != null)
			{
				turretCoordinator.DeregisterController(this);
			}

			if (oldHQ != null)
			{
				StonehengeRegistry.DeregisterTurret(this, oldHQ);
			}
			if (attachedUnit?.NetworkHQ != null)
			{
				oldHQ = attachedUnit.NetworkHQ;
				StonehengeRegistry.RegisterTurret(this, attachedUnit.NetworkHQ);
			}
			else
			{
				oldHQ = null;
			}
			
			turretCoordinator = null;
		}

		public bool HasCoordinator()
		{
			return turretCoordinator != null;
		}
	}
}