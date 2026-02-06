/*using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomWeapons.Stonehenge
{
	public class BackupCommand : MonoBehaviour
	{
		private class TargetData
		{
			public Vector3 pos;
			public Vector3 vel;
			public Vector3 error;

			public TargetData(Vector3 pos, Vector3 vel, Vector3 error)
			{
				this.pos = pos;
				this.vel = vel;
				this.error = error;
			}
		}
		
		[SerializeField] private StonehengeControl control;
		[SerializeField] private float maxError;
		[SerializeField] private TargetDetector targetDetector;
		[SerializeField] private float targetAssessmentInterval;
		[SerializeField] private Unit attachedUnit;


		private Unit target;
		private Dictionary<Unit, TargetData> targets = new Dictionary<Unit, TargetData>();
		private float lastTargetAssesment;
		private float priorityThreshold;
		private WeaponStation weaponStation;

		private void Awake()
		{
			if (targetDetector != null)
			{
				targetDetector.onDetectTarget += BackupCommand_OnDetectTarget;
				targetDetector.onScan += BackupCommand_OnCompleteScan;
			}

			weaponStation = control.turret.GetWeaponStation();
		}

		private void FixedUpdate()
		{
			if (attachedUnit == null || attachedUnit.disabled)
			{
				base.enabled = false;
				return;
			}

			TargetData data;
			if ( target != null && targets.TryGetValue(target, out data))
			{
				control.Aim(data.pos + data.error, data.vel);
			}

			if (control.IsOnTarget())
			{
				weaponStation.Fire(attachedUnit, target);
			}
			
		}

		private void BackupCommand_OnDetectTarget(Unit unit)
		{
			if (targets.ContainsKey(unit)) return;
			if (unit.rb != null)
			{
				Vector3 error = Random.insideUnitSphere * maxError;
				targets.Add(unit, new TargetData(unit.transform.position, unit.rb.velocity, error));
			}
		}

		private void BackupCommand_OnCompleteScan()
		{
			var toRemove = new List<Unit>();
			foreach (var kvp in targets)
			{
				var unit = kvp.Key;
				if (targetDetector.detectedTargets.Contains(unit))
				{
					targets[unit].pos = unit.transform.position;
					targets[unit].vel = unit.rb?.velocity ?? Vector3.zero;
				}
				else
				{
					toRemove.Add(unit);
				}
			}
			foreach (var unit in toRemove) targets.Remove(unit);
			if (Time.timeSinceLevelLoad - lastTargetAssesment > targetAssessmentInterval)
			{
				ChooseTarget(true);
			}
		}

		private void BackupCommand_OnTargetDisabled(Unit unit)
		{
			target = null;
			targets.Remove(unit);
		}

		private void ChooseTarget(bool clearAfterSearch)
		{
			if (attachedUnit.disabled)
			{
				//how
				return;
			}

			lastTargetAssesment = Time.timeSinceLevelLoad;
			if (target != null)
			{
				target.onDisableUnit -= BackupCommand_OnTargetDisabled;
				if (attachedUnit.NetworkHQ.trackingDatabase.TryGetValue(target.persistentID, out var value))
				{
					value.attackers--;
				}
			}
			Unit unit = target;
			target = null;
			priorityThreshold = 0f;
			foreach (Unit potentialTarget in targets.Keys)
			{
				TargetData data;
				if (targets.TryGetValue(potentialTarget, out data))
				{
					AssessTargetPriority(potentialTarget, data);
				}
				
			}
			if (target != null)
			{
				target.onDisableUnit += BackupCommand_OnTargetDisabled;
				attachedUnit.NetworkHQ.trackingDatabase[target.persistentID].attackers++;
			}

			Span<PersistentID> span = stackalloc PersistentID[1];
			span[0] = ((target != null) ? target.persistentID : PersistentID.None);

			if (target != unit)
			{
				attachedUnit.RpcSetStationTargets(weaponStation.Number, span);
			}
		}

		private void AssessTargetPriority(Unit targetCandidate, TargetData data)
		{
			TrackingInfo trackingData = attachedUnit.NetworkHQ.GetTrackingData(targetCandidate.persistentID);
			if (trackingData == null || targetCandidate == null || targetCandidate.disabled ||
			    targetCandidate.NetworkHQ == null || attachedUnit.disabled)
			{
				return;
			}

			float num = FastMath.Distance(data.pos, base.transform.position);
			float num2 = weaponStation.WeaponInfo.targetRequirements.maxRange / num;
			if (!(num2 < 0.7f))
			{
				OpportunityThreat opportunityThreat =
					CombatAI.AnalyzeTarget(weaponStation, attachedUnit, trackingData, 2f, num);
				float num3 = opportunityThreat.opportunity * (1f + opportunityThreat.threat) * num2;
				if (num3 != 0f)
				{
					if (weaponStation.Reloading || weaponStation.Ammo <= 0)
					{
						num3 = 0.01f;
					}

					if (num > weaponStation.WeaponInfo.targetRequirements.maxRange ||
					    num < weaponStation.WeaponInfo.targetRequirements.minRange)
					{
						num3 *= 0.01f;
					}

					if (targetCandidate.speed > 0f && num2 < 2f)
					{
						num3 *= 0.6f;
					}

					if (num3 > priorityThreshold)
					{
						target = targetCandidate;
						priorityThreshold = num3;
					}
				}
			}
		}
	}
}*/