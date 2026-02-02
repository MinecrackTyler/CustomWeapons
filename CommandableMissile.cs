using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomWeapons
{
	public class CommandableMissile : Missile, ICommandable
	{
		public UnitCommand UnitCommand => unitCommand;
		bool ICommandable.Disabled => base.disabled;
		FactionHQ ICommandable.HQ => base.NetworkHQ;
		public IReadOnlyList<GlobalPosition> Waypoints => waypoints;
		public event Action onClearWaypoints; 
		
		[SerializeField]
		private UnitCommand unitCommand;
		private List<GlobalPosition> waypoints;
		private const float WAYPOINT_CLEAR_RADIUS = 250f;

		[SerializeField] private float terminalBoostForce;
		[SerializeField] private float terminalBoostDuration;
		[SerializeField] private ParticleSystem[] boostParticles;
		[SerializeField] private AudioSource boostSource;

		private Coroutine boostCoroutine;
		private bool terminalBoostActive;
		private float terminalBoostStartTime;

		private void UnitCommand_ProcessSetDestination(ref UnitCommand.Command command)
		{
			if (!command.FromPlayer)
			{
				return; //how
			}

			if (FastMath.InRange(command.position, base.transform.GlobalPosition(), WAYPOINT_CLEAR_RADIUS))
			{
				waypoints.Clear();
				onClearWaypoints?.Invoke();
			}
			
			waypoints.Add(command.position);
		}

		private void OnStartServer()
		{
			unitCommand.ProcessSetDestination += UnitCommand_ProcessSetDestination;
		}

		public override void Awake()
		{
			base.Awake();
			base.Identity.OnStartServer.AddListener(OnStartServer);
			waypoints = new List<GlobalPosition>();
		}

		public void TriggerTerminalBoost()
		{
			if (terminalBoostActive)
			{
				return;
			}
			terminalBoostActive = true;
			foreach (ParticleSystem particleSystem in boostParticles)
			{
				particleSystem.Play();
			}
			terminalBoostStartTime = Time.timeSinceLevelLoad;
		}

		public override void OnEnable()
		{
			base.OnEnable();
			if (boostCoroutine == null)
			{
				boostCoroutine = StartCoroutine(ScuffedFixedUpdate());
			}
			
		}

		public void OnDisable()
		{
			if (boostCoroutine == null) return;
			StopCoroutine(ScuffedFixedUpdate());
			boostCoroutine = null;

		}

		private IEnumerator ScuffedFixedUpdate() //dont want to override base FixedUpdate
		{
			while (true)
			{
				LateFixedUpdate();
				yield return new WaitForFixedUpdate();
			}
		}

		private void LateFixedUpdate()
		{
			if (terminalBoostActive)
			{
				rb.AddForce(transform.forward * terminalBoostForce);

				if (Time.timeSinceLevelLoad > terminalBoostStartTime + terminalBoostDuration)
				{
					terminalBoostActive = false;
					foreach (ParticleSystem particleSystem in boostParticles)
					{
						particleSystem.Stop();
					}
				}
			}
		}
	}
}