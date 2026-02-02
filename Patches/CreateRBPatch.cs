using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CustomWeapons.Patches
{
    [HarmonyPatch(typeof(AeroPart), "CreateRB")]
    public class AeroPartDiagnosticPatch
    {
        // Caching the FieldInfo for performance, though not strictly necessary for a crash diagnostic
        private static readonly FieldInfo AttachInfoField = typeof(UnitPart).GetField("attachInfo", BindingFlags.Instance | BindingFlags.NonPublic);

        static void Prefix(AeroPart __instance)
        {
            // 1. Get the value of the protected attachInfo field via Reflection
            object attachInfoValue = AttachInfoField?.GetValue(__instance);
        
            // 2. Replicate the game's internal check logic
            bool isAttachInfoNull = attachInfoValue == null;
            bool isMassZero = __instance.mass == 0f;
        
            // Check if rb exists but doesn't match the parent unit (common in nested prefabs)
            bool rbMismatch = __instance.rb != null && __instance.parentUnit != null && __instance.rb != __instance.parentUnit.rb;

            if (isMassZero || isAttachInfoNull || rbMismatch)
            {
                Debug.Log($"--- [AeroPart Diagnostic: {__instance.name}] ---");
                Debug.Log($"- mass: {__instance.mass}");
                Debug.Log($"- attachInfo is null: {isAttachInfoNull}");
                Debug.Log($"- rb is null: {__instance.rb == null}");

                if (__instance.parentUnit != null)
                {
                    Debug.Log($"- parentUnit.rb is null: {__instance.parentUnit.rb == null}");
                }
                else
                {
                    Debug.Log("- parentUnit is NULL!");
                }

                // The NRE happens here in the game code if we entered this block and rb is null
                if (__instance.rb == null)
                {
                    Debug.LogError($"[CRITICAL] AeroPart '{__instance.name}' is entering the fallback block but 'rb' is null. NRE is imminent.");
                
                    // If this is the root part (attachInfo is null), it SHOULD have the parentUnit's RB
                    if (isAttachInfoNull && __instance.parentUnit != null && __instance.parentUnit.rb != null)
                    {
                        Debug.Log("Diagnostic: This looks like a root part. Attempting to auto-fix by assigning parentUnit.rb.");
                        __instance.rb = __instance.parentUnit.rb;
                    }
                }
                Debug.Log("------------------------------------------");
            }
        }
    }
}