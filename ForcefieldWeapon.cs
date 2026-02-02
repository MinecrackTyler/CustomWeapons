using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using UnityEngine;
using Random = System.Random;

namespace CustomWeapons
{
	public class ForcefieldWeapon : Weapon
	{
		[SerializeField] private ForcefieldSegment[] forcefields;
		[SerializeField] private PowerSupply powerSupply;
		[SerializeField] private float powerPerForcefield;
		private bool fireCommanded = false;
		private float previousLastFired;
		private int activeForcefieldCount;
		private List<ForcefieldSegment> activeForcefields;
		private List<ForcefieldSegment> inactiveForcefields;
		Random rnd = new Random();
		
		public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
		{
			if (!base.enabled)
			{
				base.enabled = true;
			}

			if (!fireCommanded) //started firing after break
			{
				activeForcefields = forcefields.ToList();
			}
			fireCommanded = true;
			previousLastFired = lastFired;
			lastFired = Time.timeSinceLevelLoad;
			weaponStation.LastFiredTime = Time.timeSinceLevelLoad;
			//enable drag increase somehow (tbd)
		}

		public override void AttachToUnit(Unit unit)
		{
			base.AttachToUnit(unit);
			powerSupply = unit.GetPowerSupply();
			powerSupply.AddUser();
			attachedUnit.onDisableUnit += AttachedUnit_onDisableUnit;
		}

		private void AttachedUnit_onDisableUnit(Unit unit)
		{
			foreach (ForcefieldSegment forcefield in forcefields)
				forcefield.SetActive(false);
		}

		public void LateUpdate()
		{
			if (lastFired == previousLastFired || Time.timeSinceLevelLoad > lastFired + 0.2f)
			{
				fireCommanded = false;
				foreach (ForcefieldSegment forcefield in forcefields)
					forcefield.SetActive(false);
				base.enabled = false;
			}
		}
		
		private void FixedUpdate()
		{
			if (fireCommanded)
			{
				int forcefieldCount = forcefields.Length;
				activeForcefieldCount = forcefieldCount;
				float totalPower = powerPerForcefield * forcefieldCount;
				var powerDrawn = powerSupply.DrawPower(totalPower); //alternatively, we reduce power drawn depending on how many it can actually supply?
				powerDrawn = Mathf.Round(powerDrawn * 100f) / 100f;

				if (powerDrawn < totalPower)
				{
					activeForcefieldCount = (int)Mathf.Floor(activeForcefieldCount * (powerDrawn / totalPower));
					while (activeForcefields.Count > 0 && activeForcefieldCount < activeForcefields.Count)
					{
						activeForcefields.RemoveAt(rnd.Next(activeForcefields.Count));
					}
				}
				else if (activeForcefields.Count != forcefields.Length)
				{
					activeForcefields = forcefields.ToList();
				}
				inactiveForcefields = forcefields.Except(activeForcefields).ToList();
				
				foreach (ForcefieldSegment forcefield in activeForcefields)
					forcefield.SetActive(true);
				
				foreach (ForcefieldSegment forcefield in inactiveForcefields)
					forcefield.SetActive(false);
			}
		}
	}
}