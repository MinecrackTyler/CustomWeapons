using System.Collections.Generic;
using NuclearOption.SavedMission;
using UnityEngine;

namespace CustomWeapons.Components
{
	public class DecoyPod : Weapon
	{
		[SerializeField] private int decoyCount;
		
		private bool fireCommanded;
		private bool decoysSpawned;
		private List<Aircraft> decoys;
		private GameObject prefab;

		private void Awake()
		{
			if (decoyCount <= 0) decoyCount = 2;
			decoys = new List<Aircraft>();
		}
		
		public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
		{
			fireCommanded = true;
			lastFired = Time.timeSinceLevelLoad;
		}

		private void FixedUpdate()
		{
			if (!fireCommanded) return;
			if (!decoysSpawned)
			{
				for (int i = 0; i < decoyCount; i++)
				{
					Aircraft aircraft = Spawner.i.SpawnAircraft(null, prefab, new Loadout(), 1f, new LiveryKey(),
						attachedUnit.GlobalPosition() + (Random.insideUnitSphere * 500f),
						attachedUnit.transform.rotation, attachedUnit.rb.velocity, null, attachedUnit.NetworkHQ,
						attachedUnit.unitName, 0f, 0f);
					decoys.Add(aircraft);
					aircraft.SetLocalSim(true);
					aircraft.SetSimplePhysics();
				}
				decoysSpawned = true;
			}
			foreach (Aircraft aircraft in decoys)
			{
				aircraft.gameObject.transform.position = attachedUnit.transform.position + (Random.insideUnitSphere * 500f);
			}
		}

		private void LateUpdate()
		{
			if (lastFired > Time.timeSinceLevelLoad + 0.2)
			{
				fireCommanded = false;
				foreach (Aircraft aircraft in decoys)
				{
					Destroy(aircraft.gameObject);
				}
				decoysSpawned = false;
			}
		}

		public override void AttachToUnit(Unit unit)
		{
			base.AttachToUnit(unit);
			if (unit is Aircraft aircraft)
			{
				prefab = aircraft.definition.unitPrefab;
			}
			else
			{
				Destroy(this);
			}
		}
	}
}