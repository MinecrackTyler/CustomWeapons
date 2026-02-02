using System.IO;
using BepInEx;
using HarmonyLib;

namespace CustomWeapons
{
	[BepInPlugin("com.minec.customweapons", "Custom Weapons", "1.0.0")]
	[BepInDependency("com.nikkorap.blueprinter")]
	public class CustomWeapons : BaseUnityPlugin
	{
		private void Awake()
		{
			var harmony = new Harmony("com.minec.customweapons");
			harmony.PatchAll();
			
			//fix blueprinter??

			/*var info = (BaseUnityPlugin)Blueprinter.Plugin.Instance;
			var modPathField = AccessTools.Field(typeof(Blueprinter.Plugin), "_modPath");
			modPathField?.SetValue(
				Blueprinter.Plugin.Instance,
				Path.GetDirectoryName(info.Info.Location)
			);*/
		}
	}
}