using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CustomWeapons.Patches
{
	[HarmonyPatch(typeof(UnitDebug), "UpdateWeaponDisplay")]
    public static class UnitDebug_UpdateWeaponDisplay_Prefix
    {
        static bool Prefix(UnitDebug __instance, Unit unit)
        {
            // --- Grab private fields ---
            var weaponStationsPanel =
                AccessTools.Field(typeof(UnitDebug), "weaponStationsPanel")
                    .GetValue(__instance) as GameObject;

            var weaponStationDisplays =
                AccessTools.Field(typeof(UnitDebug), "weaponStationDisplays")
                    .GetValue(__instance) as UnitDebug.WeaponStationDebug[];

            var weaponInfoToShow =
                AccessTools.Field(typeof(UnitDebug), "weaponInfoToShow")
                    .GetValue(__instance) as List<WeaponInfo>;

            if (weaponStationsPanel == null ||
                weaponStationDisplays == null ||
                weaponInfoToShow == null ||
                unit == null)
            {
                return true;
            }
            
            weaponStationsPanel.SetActive(false);

            foreach (var display in weaponStationDisplays)
                display.Hide();

            weaponInfoToShow.Clear();

            foreach (var weaponStation in unit.weaponStations)
            {
                var info = weaponStation.WeaponInfo;
                if (info != null && !weaponInfoToShow.Contains(info))
                    weaponInfoToShow.Add(info);
            }

            GameManager.GetLocalFaction(out var localFaction);

            bool flag =
                (weaponInfoToShow.Count > 0 && localFaction == null) ||
                (unit.NetworkHQ != null && localFaction == unit.NetworkHQ.faction);

            weaponStationsPanel.SetActive(flag && !PlayerSettings.cinematicMode);

            weaponInfoToShow.Sort((a, b) => a.costPerRound.CompareTo(b.costPerRound));
            
            int displayCount = Mathf.Min(
                weaponInfoToShow.Count,
                weaponStationDisplays.Length
            );

            for (int i = 0; i < displayCount; i++)
                weaponStationDisplays[i].Show(unit, weaponInfoToShow[i]);
            
            return false;
        }
    }
}