using UnityEngine;

namespace CustomWeapons.Stonehenge
{
	public class BallisticSeeker : MissileSeeker
	{
		[SerializeField] private float armDelay = 1f;
		
		private bool armed;

		public override void Initialize(Unit target, GlobalPosition aimpoint)
		{
			missile.NetworkseekerMode = Missile.SeekerMode.passive;

			targetUnit = target;

			missile.DeployFins();
		}

		public override string GetSeekerType() => "Ballistic";

		private void UpdateAimpoint()
		{
			if (targetUnit == null)
			{
				return;
			}
			missile.SetAimpoint(targetUnit.GlobalPosition(), Vector3.zero);
		}

		private void CheckDetonate()
		{
			if (!armed && missile.timeSinceSpawn > armDelay)
			{
				missile.Arm();
				armed = true;
			}

			if (armed)
			{
				if (missile.LosingGround() || missile.MissedTarget())
				{
					missile.Detonate(Vector3.up, false, false);
				}
			}
		}
		
		

		public override void Seek()
		{
			UpdateAimpoint();
			CheckDetonate();
		}
	}
}