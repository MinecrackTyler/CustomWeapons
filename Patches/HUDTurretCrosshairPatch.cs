using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomWeapons.Patches
{
    [HarmonyPatch(typeof(HUDTurretCrosshair))] //it hates my grappling hook, may not be needed but i am too lazy to check
    public static class HUDTurretCrosshairPatch
    {
        [HarmonyPatch("Refresh")]
        [HarmonyPrefix]
        public static bool Prefix(HUDTurretCrosshair __instance, Camera mainCamera, out Vector3 crosshairPosition)
        {
            crosshairPosition = Vector3.one * 10000f;

            var turret = Traverse.Create(__instance).Field("turret").GetValue<Turret>();
            var gun = Traverse.Create(__instance).Field("gun").GetValue<Gun>();
            var crosshair = Traverse.Create(__instance).Field("crosshair").GetValue<Image>();
            var circle = Traverse.Create(__instance).Field("circle").GetValue<Image>();
            var readinessCircle = Traverse.Create(__instance).Field("readinessCircle").GetValue<Image>();

            Vector3 direction = turret.GetDirection();
            bool flag = turret.IsOnTarget();

            if (Vector3.Dot(mainCamera.transform.forward, direction - mainCamera.transform.position) > 0f)
            {
                crosshairPosition = SceneSingleton<CameraStateManager>.i.mainCamera.WorldToScreenPoint(direction);
                crosshairPosition.z = 0f;
                __instance.transform.position = crosshairPosition;
                crosshair.enabled = true;

                float reloadProgress = 0f;
                if (gun != null)
                    reloadProgress = gun.GetReloadProgress();

                if (gun != null && reloadProgress > 0f)
                {
                    if (!readinessCircle.enabled)
                    {
                        readinessCircle.enabled = true;
                        crosshair.color = Color.red + Color.green * 0.5f;
                    }
                    readinessCircle.fillAmount = reloadProgress;
                }
                else if (gun != null && readinessCircle.enabled)
                {
                    readinessCircle.enabled = false;
                    crosshair.color = Color.green;
                }

                circle.enabled = flag && (gun == null || reloadProgress <= 0f);
            }
            else
            {
                circle.enabled = false;
                readinessCircle.enabled = false;
                crosshair.enabled = false;
            }
            
            return false;
        }
    }
}
