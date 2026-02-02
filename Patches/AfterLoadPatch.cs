/*using HarmonyLib;
using System;
using System.Reflection;

namespace CustomWeapons.Patches
{
    [HarmonyPatch]
    public static class AfterLoadPatch
    {
        private static MethodInfo _processBlueprints;
        private static MethodInfo _getDefsGeneric;
        static void Prepare()
        {
            var pluginType = typeof(Blueprinter.Plugin);

            _processBlueprints = AccessTools.Method(
                pluginType,
                "ProcessBlueprints"
            );

            _getDefsGeneric = AccessTools.Method(
                pluginType,
                "GetDefs"
            );

            if (_processBlueprints == null || _getDefsGeneric == null)
            {
                throw new Exception("Failed to resolve Blueprinter private methods");
            }
        }
        
        [HarmonyPatch(typeof(Encyclopedia), "AfterLoad", new Type[]{})]
        private static bool Prefix(Encyclopedia __instance)
        {
            var instance = Blueprinter.Plugin.Instance;
            if (instance == null)
                return true;

            // Call ProcessBlueprints()
            _processBlueprints.Invoke(instance, null);

            // Helper local function for GetDefs<T>()
            void AddDefs<T>(System.Collections.Generic.List<T> target) where T : UnityEngine.Object
            {
                var method = _getDefsGeneric.MakeGenericMethod(typeof(T));
                var defs = (System.Collections.Generic.IEnumerable<T>)method.Invoke(instance, null);

                foreach (var def in defs)
                {
                    // Check by name or a specific ID field to prevent the Dictionary crash
                    if (!target.Exists(x => x.name == def.name))
                    {
                        target.Add(def);
                    }
                }
            }

            AddDefs<AircraftDefinition>(__instance.aircraft);
            AddDefs<VehicleDefinition>(__instance.vehicles);
            AddDefs<MissileDefinition>(__instance.missiles);
            AddDefs<BuildingDefinition>(__instance.buildings);
            AddDefs<ShipDefinition>(__instance.ships);
            AddDefs<UnitDefinition>(__instance.otherUnits);
            AddDefs<WeaponMount>(__instance.weaponMounts);

            return true; // allow original AfterLoad() to continue
        }
    }
}*/