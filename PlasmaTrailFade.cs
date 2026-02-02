using UnityEngine;

namespace CustomWeapons
{
	[RequireComponent(typeof(ParticleSystem))]
	public class PlasmaTrailFade : MonoBehaviour
	{
		public float fadeStartAltitude = 80000f; 
		public float fadeEndAltitude = 65000f; 
		private ParticleSystem.EmissionModule emission;
		private float emissionRate;
		private bool isFading;
		private ParticleSystem.MainModule main;

		private ParticleSystem ps;

		private void Start()
		{
			ps = GetComponent<ParticleSystem>();
			emission = ps.emission;
			main = ps.main;
			emissionRate = emission.rateOverTimeMultiplier;
			emission.rateOverTime = 0f;
		}

		private void Update()
		{
			var altitude = transform.position.GlobalY();

			if (altitude <= fadeStartAltitude && !isFading) isFading = true;

			if (isFading)
			{
				var t = Mathf.Lerp(fadeStartAltitude, fadeEndAltitude, altitude);
				var emissionRate = Mathf.Lerp(0f, this.emissionRate, t); 
				

				// Fade emission
				emission.rate = emissionRate;

				if (altitude <= fadeEndAltitude)
				{
					enabled = false;
				}
			}
		}
	}
}