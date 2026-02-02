using UnityEngine;

namespace CustomWeapons
{
	public class TracerVisibilitySync : MonoBehaviour
	{
		public Renderer parentRenderer;

		private Renderer tracerRenderer;

		void Awake()
		{
			tracerRenderer = GetComponent<Renderer>();
			if (parentRenderer == null)
			{
				Debug.LogWarning($"{nameof(TracerVisibilitySync)}: No parentRenderer assigned.");
			}
		}

		void Update()
		{
			if (parentRenderer != null && tracerRenderer != null)
			{
				tracerRenderer.enabled = parentRenderer.enabled;
			}
		}
	}
}