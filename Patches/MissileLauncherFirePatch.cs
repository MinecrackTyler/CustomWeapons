using HarmonyLib;
using UnityEngine;

namespace CustomWeapons.Patches
{
	[HarmonyPatch(typeof(MissileLauncher), "Fire")]
	public static class MissileLauncherFirePatch
	{
		static void Postfix(MissileLauncher __instance)
		{
			var launchTransformsField = AccessTools.Field(typeof(MissileLauncher), "launchTransforms");
			var currentCellField = AccessTools.Field(typeof(MissileLauncher), "currentCell");

			var launchTransforms = launchTransformsField.GetValue(__instance) as Transform[];
			int currentCell = (int)currentCellField.GetValue(__instance);

			if (launchTransforms != null && launchTransforms.Length > 0)
			{
				currentCell++;

				if (currentCell >= launchTransforms.Length)
				{
					currentCell = 0;
				}
				
				currentCellField.SetValue(__instance, currentCell);
			}
		}
	}
}