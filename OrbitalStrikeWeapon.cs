using UnityEngine;

namespace CustomWeapons
{
	public class OrbitalStrikeWeapon : Weapon
	{
		[SerializeField] private float requiredLaseDuration = 8f;

		private Unit currentTarget;
		private bool isLaseCounting;

		private float laseTimer;

		private void Awake()
		{
			InvokeRepeating("DelayedUpdate", 0f, 0.05f);
		}

		public override int GetAmmoLoaded()
		{
			return ammo;
		}
		
		public override int GetAmmoTotal()
		{
			return ammo;
		}
		
		public override float GetReloadProgress()
		{
			return 1f;
		}


		private void Update()
		{
			if (isLaseCounting && currentTarget != null)
			{
				if (attachedUnit.NetworkHQ != null && attachedUnit.NetworkHQ.IsTargetLased(currentTarget))
				{
					laseTimer += Time.deltaTime;

					if (laseTimer >= requiredLaseDuration) TryFireStrike();
				}
				else
				{
					CancelLase();
				}
			}

			ammo = OrbitalStrikeController.Instance.GetAmmo(attachedUnit);
			weaponStation.Ammo = ammo;
		}

		public override void AttachToUnit(Unit unit)
		{
			base.AttachToUnit(unit);
			attachedUnit = unit;
		}

		public override void SetTarget(Unit unit)
		{
			currentTarget = unit;
		}

		public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation,
			GlobalPosition aimpoint)
		{
			if (ammo <= 0 || currentTarget == null) return;

			if (!OrbitalStrikeController.Instance.IsReady(attachedUnit))
				return;

			laseTimer = 0f;
			isLaseCounting = true;
		}

		private void CancelLase()
		{
			laseTimer = 0f;
			isLaseCounting = false;
		}

		private void TryFireStrike()
		{
			if (OrbitalStrikeController.Instance.TryFire(currentTarget, attachedUnit))
			{
				lastFired = Time.time;
				ammo = OrbitalStrikeController.Instance.GetAmmo(attachedUnit);
				weaponStation.Ammo = ammo;
				weaponStation.Updated();
			}

			CancelLase();
		}
	}
}