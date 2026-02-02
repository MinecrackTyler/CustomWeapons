using UnityEngine;
using UnityEngine.Rendering;

namespace CustomWeapons
{
	[RequireComponent(typeof(LensFlareComponentSRP))]
	public class LensFlareCurve : MonoBehaviour
	{
		public AnimationCurve intensityCurve = AnimationCurve.Linear(0, 1, 1, 0);

		private LensFlareComponentSRP lensFlare;
		private float startTime;

		void Awake()
		{
			lensFlare = GetComponent<LensFlareComponentSRP>();
			startTime = Time.time;
		}

		void Update()
		{
			float elapsed = Time.time - startTime;
			float newIntensity = intensityCurve.Evaluate(elapsed);

			// Update intensity
			lensFlare.intensity = newIntensity;

			// Kill if gone
			if (newIntensity <= 0f)
			{
				lensFlare.enabled = false;
				Destroy(this); // remove driver as well
			}
		}
	}
}