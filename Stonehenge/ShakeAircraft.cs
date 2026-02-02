using UnityEngine;

namespace CustomWeapons.Stonehenge
{
	public class ShakeAircraft : MonoBehaviour
	{
		public float shakeRadius = 5000f; // meters
		public float shakeFactor = 2f;    // passed into Aircraft.ShakeAircraft
		public float lifetime = 0.1f;     // auto-destroy

		void Start()
		{
			foreach (var aircraft in FindObjectsOfType<Aircraft>())
			{
				if (!aircraft || aircraft.disabled) continue;

				float dist = Vector3.Distance(transform.position, aircraft.transform.position);
				if (dist < shakeRadius)
				{
					float intensity = Mathf.Lerp(shakeFactor, 0f, dist / shakeRadius);
					aircraft.ShakeAircraft(intensity, intensity);
				}
			}

			Destroy(gameObject, lifetime);
		}
	}
}