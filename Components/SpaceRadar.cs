using NuclearOption.Jobs;
using UnityEngine;

namespace CustomWeapons.Components
{
	public class SpaceRadar : Radar
	{
		[SerializeField] private float minAltitude;
		[SerializeField] private float minRange;
		[SerializeField] private float maxAltitude;
		
		protected override void TargetSearch()
		{
			if (RadarParameters.maxRange > 0f)
			{
				RadarCheck();
			}

			if (visualRange > 0f)
			{
				VisualCheck();
			}
		}

		private void RadarCheck()
		{
			GlobalPosition position = scanner.GlobalPosition();
			foreach (FactionHQ hq in FactionRegistry.GetAllHQs())
			{
				if (attachedUnit.NetworkHQ == hq)
				{
					continue;
				}

				for (int i = 0; i < hq.factionRadarReturn.Count; i++)
				{
					if (UnitRegistry.TryGetUnit(hq.factionRadarReturn[i], out Unit unit))
					{
						IRadarReturn radarReturn = unit as IRadarReturn;
						GlobalPosition unitPos = unit.GlobalPosition();
						if (FastMath.InRange(unitPos, position, RadarParameters.maxRange * 2f) &&
						    unitPos.y > minAltitude && unitPos.y < maxAltitude &&
						    FastMath.OutOfRange(unitPos, position, minRange))
						{
							DetectorManager.RequestRadarCheck(this, unit, radarReturn);
						}
					}
				}
			}
		}
	}
}