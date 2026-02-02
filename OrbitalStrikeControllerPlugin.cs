using BepInEx;
using UnityEngine;

namespace CustomWeapons
{
	[BepInPlugin("com.minec.orbitalstrikecontroller", "Orbital Strike Controller", "1.0.0")]
	public class OrbitalStrikeControllerPlugin : BaseUnityPlugin
	{
		private void Awake()
		{
			Invoke(nameof(Setup), 5f);
		}

		private void Setup()
		{
			if (OrbitalStrikeController.Instance != null) return;
			var go = new GameObject("OrbitalStrikeController");
			go.AddComponent<OrbitalStrikeController>();
			DontDestroyOnLoad(go);
		}
	}
}