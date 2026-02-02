using System.Reflection;
using UnityEngine;

namespace CustomWeapons
{
	public class CommandableOpticalSeekerCruiseMissile : OpticalSeekerCruiseMissile
	{
		[SerializeField] private float waypointRadius;
		[SerializeField] private CommandableMissile commandableMissile;
		private int currentWaypointIndex;
		private GlobalPosition? currentWaypoint;
		private bool terminalMode;
		private float lastTerminalCheckTime;
		private GlobalPosition knownPos;
		private FieldInfo terminalField;
		
		public override void Seek()
		{
			if (missile.timeSinceSpawn < 10f)
			{
				base.Seek();
				return;
			}

			GlobalPosition? knownPosition = missile.NetworkHQ.GetKnownPosition(targetUnit);
			if (knownPosition.HasValue)
			{
				knownPos = knownPosition.Value;
			}
			
			if (!(bool)terminalField.GetValue(this))
			{
				PreTerminalCommandMode();
			}
			else
			{
				commandableMissile.TriggerTerminalBoost();
				TerminalMode();
			}
			
		}

		public override void Initialize(Unit target, GlobalPosition aimPoint)
		{
			base.Initialize(target, aimPoint);
			currentWaypointIndex = 0;
			if (commandableMissile.Waypoints.Count > 0)
			{
				currentWaypoint = commandableMissile.Waypoints[currentWaypointIndex];
			}
			commandableMissile.onClearWaypoints += CommandableMissile_OnClearWaypoints;
			terminalField = typeof(OpticalSeekerCruiseMissile).GetField("terminalMode", BindingFlags.NonPublic | BindingFlags.Instance);
			if (terminalField == null)
			{
				Destroy(gameObject);
			}
		}

		private void CommandableMissile_OnClearWaypoints()
		{
			currentWaypointIndex = 0;
			currentWaypoint = null;
		}
		
		private bool WaypointInRange()
		{
			if (currentWaypoint == null) return false;
			return FastMath.InRange(currentWaypoint.Value, missile.GlobalPosition(), waypointRadius);
		}

		private bool HasNextWaypoint()
		{
			if (currentWaypoint == null) return false;
			return currentWaypointIndex + 1 < commandableMissile.Waypoints.Count;
		}
		
		private void PreTerminalCommandMode()
		{
			if (Time.timeSinceLevelLoad - lastTerminalCheckTime < 0.1f)
			{
				return;
			}
			lastTerminalCheckTime = Time.timeSinceLevelLoad;
			base.PreTerminalMode(); //update some needed info

			if (currentWaypoint == null)
			{
				if (commandableMissile.Waypoints.Count > 0)
				{
					currentWaypoint = commandableMissile.Waypoints[0];
				}
			}
			
			if (WaypointInRange() && HasNextWaypoint())
			{
				currentWaypointIndex++;
				currentWaypoint = commandableMissile.Waypoints[currentWaypointIndex];
			}
			
			GlobalPosition navTarget = currentWaypoint ?? knownPos;
			GlobalPosition aimPoint = TerrainWaypoint(navTarget);
			
			if (missile.timeSinceSpawn >= 10f)
			{
				missile.SetAimpoint(aimPoint, Vector3.zero);
			}

			/*if (missile.timeSinceSpawn > 3f && !HasNextWaypoint() && FastMath.InRange(missile.GlobalPosition(), navTarget, waypointRadius))
			{
				terminalMode = true;
				missile.Arm();
			}*/
		}
	}
}