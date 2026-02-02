/*using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using Blueprinter;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;

namespace CustomWeapons.Patches
{
	[HarmonyPatch]
	public class AudioMixerPatch
	{
		private const string GUID = "com.nikkorap.blueprinter";
		private const string MixerName = "MasterAudioMixer";
		private static Blueprinter.Plugin BlueprinterInstance;
		private static List<UnityEngine.Object> _loadedAssets;
		static bool Prepare()
		{
			BlueprinterInstance = Blueprinter.Plugin.Instance;
			if (BlueprinterInstance != null) return true;
			Debug.Log("[AudioMixerPatch] Cannot find Blueprinter plugin instance");
			return false;
		}

		private static AudioMixer FindGameMixer()
		{
			var mixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
			foreach (var mixer in mixers)
			{
				if (mixer.name == MixerName)
				{
					return mixer;
				}
				
			}
			return null;
		}

		[HarmonyPatch(typeof(Encyclopedia), "AfterLoad", new Type[] { })]
		private static void Postfix(Encyclopedia __instance)
		{
			var type = BlueprinterInstance.GetType();
			var loadedAssetsField =  type.GetField("_loadedAssets", BindingFlags.Instance | BindingFlags.NonPublic);
			if (loadedAssetsField == null)
			{
				Debug.Log("[AudioMixerPatch] Cannot find loaded assets field");
				return;
			}
			_loadedAssets = (List<UnityEngine.Object>) loadedAssetsField.GetValue(BlueprinterInstance);
			
			var mixer = FindGameMixer();
			if (mixer == null)
			{
				Debug.LogError("[AudioMixerPatch] Cannot find AudioMixer");
				return;
			}

			AudioMixerGroup[] realGroups = mixer.FindMatchingGroups("");
			Dictionary<string, AudioMixerGroup> groups = new Dictionary<string, AudioMixerGroup>();

			foreach (var g in realGroups)
			{
				groups.TryAdd(g.name, g);
			}
			
			if (_loadedAssets == null || _loadedAssets.Count == 0)
			{
				Debug.LogError("[AudioMixerPatch] Cannot find Assets");
				if (_loadedAssets != null)
				{
					Debug.LogError("[AudioMixerPatch] Assets found: " + _loadedAssets.Count);
				}
				return;
			}
			Debug.Log("[AudioMixerPatch] Loaded " + _loadedAssets.Count + " assets");
			
			int fixCount = 0;

			foreach (var asset in _loadedAssets)
			{
				if (asset is GameObject go)
				{
					AudioSource[] sources = go.GetComponentsInChildren<AudioSource>(true);
					foreach (var source in sources)
					{
						if (source.outputAudioMixerGroup == null)
						{
							continue;
						}
						string groupName = source.outputAudioMixerGroup.name;
						if (groups.TryGetValue(groupName, out AudioMixerGroup group))
						{
							source.outputAudioMixerGroup = group;
							fixCount++;
						}
					}
				}
			}
			
			Debug.Log("[AudioMixerPatch] Fixed " + fixCount + " AudioSources");
			
		}
	}
}*/