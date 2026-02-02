using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CustomWeapons
{
	public class RocketBooster : Weapon
	{
		[SerializeField] private float thrust;
		[SerializeField] private float burnTime;
		[SerializeField] private ParticleSystem[] engineParticles;
		[SerializeField] private AudioSource fireSound;
		[SerializeField] private TrailEmitter[] engineTrails;
		private float fireTime;
		private Aircraft attachedAircraft;
		private bool fired;
		private bool burnout;

		public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation,
			GlobalPosition aimpoint)
		{
			if (fired)
			{
				return;
			}
			fired = true;
			if (attachedAircraft == null)
			{
				burnout = true;
				return;
			}
			ammo = 0;
			fireTime = Time.timeSinceLevelLoad;
			foreach (var engineParticle in engineParticles)
			{
				engineParticle.Play();
			}
			fireSound.Play();
			weaponStation.Updated();
		}

		public override void AttachToUnit(Unit unit)
		{
			base.AttachToUnit(unit);
			if (attachedUnit is Aircraft aircraft)
			{
				attachedAircraft = aircraft;
			}
		}

		private void FixedUpdate()
		{
			if (!fired || burnout)
			{
				return;
			}
			hardpoint.part.rb.AddForceAtPosition(transform.forward * thrust, transform.position);
			if (Time.timeSinceLevelLoad > fireTime + burnTime)
			{
				foreach (var engineParticle in engineParticles)
					engineParticle.Stop();
				foreach (var trail in engineTrails)
				{
					trail.StopTrail();
				}
				fireSound.Stop();
				burnout = true;
			}
		}
	}
}