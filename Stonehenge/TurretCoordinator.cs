using System.Collections.Generic;
using UnityEngine;

namespace CustomWeapons.Stonehenge
{
	public class TurretCoordinator : MonoBehaviour
	{
		public readonly float SEARCH_RANGE = 2000f;

		[SerializeField] private Unit attachedUnit;
		[SerializeField] private float searchInterval = 10f;

		private FactionHQ oldHQ;
		private List<StonehengeControl> controllers;

		private void Awake()
		{
			controllers = new List<StonehengeControl>();
			this.StartSlowUpdate(searchInterval, SearchTurretControllers);
			this.attachedUnit.onDisableUnit += Coordinator_OnUnitDisabled;
			this.attachedUnit.onChangeFaction += Coordinator_OnTeamChanged;
		}

		private void OnEnable()
		{
			if (attachedUnit?.NetworkHQ != null)
			{
				oldHQ = attachedUnit.NetworkHQ;
				StonehengeRegistry.RegisterCoordinator(this, attachedUnit.NetworkHQ);
			}
		}

		private void OnDisable()
		{
			if (attachedUnit?.NetworkHQ != null)
			{
				StonehengeRegistry.DeregisterCoordinator(this, attachedUnit.NetworkHQ);
			}
		}

		public void RegisterController(StonehengeControl controller)
		{
			if (!controllers.Contains(controller))
			{
				controllers.Add(controller);
				controller.SetCoordinator(this);
			}
		}

		public void DeregisterController(StonehengeControl controller)
		{
			if (controllers.Remove(controller))
			{
				controller.SetCoordinator(null);
			}
		}

		private void SearchTurretControllers()
		{
			if (attachedUnit?.NetworkHQ == null)
			{
				return;
			}
			IReadOnlyList<StonehengeControl> controls = StonehengeRegistry.GetTurrets(attachedUnit.NetworkHQ);
			foreach (StonehengeControl control in controls)
			{
				if (control.HasCoordinator())
				{
					continue;
				}
				if (FastMath.Distance(control.AttachedUnit.transform.position, attachedUnit.transform.position) <
				    SEARCH_RANGE)
				{
					RegisterController(control);
				}
			}
		}

		private void Coordinator_OnUnitDisabled(Unit unit)
		{
			if (attachedUnit.NetworkHQ != null)
			{
				StonehengeRegistry.DeregisterCoordinator(this, attachedUnit.NetworkHQ);
			}
		}

		private void Coordinator_OnTeamChanged(Unit unit)
		{
			if (oldHQ != null)
			{
				StonehengeRegistry.DeregisterCoordinator(this, oldHQ);
			}

			oldHQ = attachedUnit.NetworkHQ;
			if (attachedUnit.NetworkHQ != null)
			{
				StonehengeRegistry.RegisterCoordinator(this, attachedUnit.NetworkHQ);
			}

			foreach (StonehengeControl controller in controllers)
			{
				controller.AttachedUnit.NetworkHQ = attachedUnit?.NetworkHQ;
			}
		}

		private void OnDestroy()
		{
			if (attachedUnit?.NetworkHQ != null)
			{
				StonehengeRegistry.DeregisterCoordinator(this, attachedUnit.NetworkHQ);
			}
			attachedUnit.onDisableUnit -= Coordinator_OnUnitDisabled;
			attachedUnit.onChangeFaction -= Coordinator_OnTeamChanged;
			
		}
	}
}