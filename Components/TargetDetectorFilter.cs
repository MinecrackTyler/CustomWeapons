using System.Collections.Generic;
using UnityEngine;

namespace CustomWeapons.Components
{
	public class TargetDetectorFilter : TargetDetector
	{
		[SerializeField] private TargetDetector targetDetector;
		
		[Header("Filtering Options")]
		[SerializeField] private bool useWhitelist = false;
		[SerializeField] private bool filterAircraft = false;
		[SerializeField] private bool filterMissiles = true;
		[SerializeField] private bool filterGround = false;
		[SerializeField] private bool filterShips = false;
		[SerializeField] private bool filterBuildings;

		[Tooltip("json keys")]
		[SerializeField] private List<string> filterList = new List<string>();
		
		private HashSet<string> filterSet;
		
		protected override void Awake()
		{
			base.Awake();
			targetDetector.onDetectTarget += TargetDetector_OnDetectTarget;
			filterSet = new HashSet<string>(filterList);
		}

		protected override void TargetSearch() {} //only filter targets

		private void TargetDetector_OnDetectTarget(Unit unit)
		{
			switch (unit.definition)
			{
				case AircraftDefinition when !filterAircraft:
				case MissileDefinition when !filterMissiles:
				case ShipDefinition when !filterShips:
				case BuildingDefinition when !filterBuildings:
				case VehicleDefinition when !filterGround:
					DetectTarget(unit);
					return;
			}

			string key = unit.definition?.jsonKey ?? "";
			if (string.IsNullOrEmpty(key))
			{
				return;
			}

			bool inList = filterSet.Contains(key);

			switch (inList)
			{
				case true when useWhitelist:
				case false when !useWhitelist:
					DetectTarget(unit);
					break;
			}
		}
	}
}