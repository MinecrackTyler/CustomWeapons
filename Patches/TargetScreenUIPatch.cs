using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomWeapons.Patches
{
	[HarmonyPatch(typeof(TargetScreenUI), "UpdateTargetInfo")]
	public static class TargetScreenUIPatch
	{
		[HarmonyPostfix]
		public static void Postfix(
			TargetScreenUI __instance,
			List<Unit> ___targetList,
			FactionHQ ___hq,
			Text ___typeText,
			Text ___heading,
			Text ___altitude,
			Text ___rel_altitude,
			Text ___speed,
			Text ___rel_speed)
		{
			if (___targetList == null || ___targetList.Count == 0)
				return;

			Unit unit2 = ___targetList[0];
			if (!(unit2 is Satellite sat))
				return; 
			
			if (___hq.IsTargetPositionAccurate(unit2, 20f))
			{
				GlobalPosition globalPosition = unit2.GlobalPosition();
				Vector3 relVector = globalPosition - SceneSingleton<CombatHUD>.i.aircraft.GlobalPosition();

				___heading.text = $"HDG {unit2.transform.eulerAngles.y:F0}°";
				___altitude.text = "ALT " + UnitConverter.AltitudeReading(globalPosition.y);
				___rel_altitude.text = "REL " + UnitConverter.AltitudeReading(relVector.y);
				___speed.text = "SPD " + UnitConverter.SpeedReading(unit2.speed);
				___rel_speed.text = "REL " + UnitConverter.SpeedReading(
					Vector3.Dot(SceneSingleton<CombatHUD>.i.aircraft.rb.velocity, relVector.normalized) -
					Vector3.Dot(unit2.rb.velocity, relVector.normalized));
			}
			else
			{
				___heading.text = "HDG -";
				___altitude.text = "ALT -";
				___rel_altitude.text = "REL -";
				___speed.text = "SPD -";
				___rel_speed.text = "REL -";
			}

			if (___targetList.Count > 1)
			{
				___typeText.text = $"{___targetList.Count} targets";
			}
			else
			{
				___typeText.text = unit2.unitName;
			}
		}
	}
}