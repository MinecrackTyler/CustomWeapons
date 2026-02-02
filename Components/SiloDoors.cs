using System.Reflection;
using UnityEngine;

namespace CustomWeapons.Components
{
	public class SiloDoors : MonoBehaviour
	{
		[SerializeField] private Unit attachedUnit;
		[SerializeField] private FireControl fireControl;

		private static FieldInfo? fireControlDeployedField;
		private bool deployed;
		
		private void Awake()
		{
			attachedUnit.onInitialize += SiloDoors_OnInitialize;

			// Cache reflection once
			if (fireControlDeployedField == null)
			{
				fireControlDeployedField = typeof(FireControl)
					.GetField("deployed", BindingFlags.Instance | BindingFlags.NonPublic);

				if (fireControlDeployedField == null)
				{
					Debug.LogError("SiloDoors: Failed to find FireControl.deployed field via reflection");
				}
			}
		}

		private void SiloDoors_OnInitialize()
		{
			this.StartSlowUpdate(5f, StrategicEscalationCheck);
		}

		private void StrategicEscalationCheck()
		{
			float currentEscalation = NetworkSceneSingleton<MissionManager>.i.currentEscalation;
			float strategicThreshold = NetworkSceneSingleton<MissionManager>.i.strategicThreshold;

			if (currentEscalation > strategicThreshold)
			{
				if (!deployed)
				{
					fireControl.DeployOrStowLaunchers(true);
					deployed = true;
				}
				if (fireControlDeployedField != null)
				{
					fireControlDeployedField.SetValue(fireControl, true);
				}
			}
		}
	}
}