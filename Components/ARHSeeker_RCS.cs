using System.Reflection;
using UnityEngine;

namespace CustomWeapons.Components
{
	public class ARHSeeker_RCS : ARHSeeker
	{
		[SerializeField] private RCS rcs;
		private FieldInfo? aimpointField;
		private FieldInfo? velocityField;
		
		public override void Seek()
		{
			base.Seek();
			GlobalPosition aimpoint = (GlobalPosition)aimpointField.GetValue(missile);
			Vector3 targetVel = (Vector3)velocityField.GetValue(missile);
			rcs.CorrectTrajectory(missile.airDensity, aimpoint, targetVel, missile.rb, aimpoint);
		}

		public override void Initialize(Unit target, GlobalPosition aimpoint)
		{
			base.Initialize(target, aimpoint);
			aimpointField = missile.GetType().GetField("aimPoint", BindingFlags.NonPublic | BindingFlags.Instance);
			velocityField = missile.GetType().GetField("targetVel", BindingFlags.NonPublic | BindingFlags.Instance);
			if (aimpointField == null || velocityField == null)
			{
				Destroy(this);
			}
		}
	}
}