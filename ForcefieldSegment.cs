using System.Collections;
using UnityEngine;

namespace CustomWeapons
{
	public class ForcefieldSegment : MonoBehaviour
	{
		private bool active;
		private Collider collider;
		private Renderer renderer;
		[SerializeField] private float fadeTime = 1f;
		[SerializeField] private float opacity = 0.5f;
		
		private void Start()
		{
			collider = GetComponent<Collider>();
			renderer = GetComponent<Renderer>();
			if (renderer == null || collider == null)
			{
				Destroy(gameObject);
			}
			collider.enabled = false;
			renderer.enabled = false;
		}
		
		public void SetActive(bool active)
		{
			if (active == this.active)
			{
				return;
			}
			this.active = active;
			StopAllCoroutines();
			if (active)
			{
				renderer.enabled = true;
				collider.enabled = true;
				StartCoroutine(FadeMaterial(0f, opacity));
			}
			else
			{
				StartCoroutine(Deactivate());
			}
		}

		private IEnumerator Deactivate()
		{
			yield return StartCoroutine(FadeMaterial(opacity, 0f));
			renderer.enabled = false;
			collider.enabled = false;
		}

		private IEnumerator FadeMaterial(float start, float end)
		{
			var m = renderer.material;
			if (m == null)
			{
				yield break;
			}
			Color startColor = new Color(m.color.r, m.color.g, m.color.b, start);
			Color endColor = new Color(m.color.r, m.color.g, m.color.b, end);
			float timePassed = 0f;

			while (timePassed < fadeTime)
			{
				m.color = Color.Lerp(startColor, endColor, timePassed / fadeTime);
				timePassed += Time.deltaTime;
				yield return null;
			}
			m.color = endColor;
		}
	}
}