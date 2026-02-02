using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace CustomWeapons
{
	public class PlasmaTrailAnchor : MonoBehaviour
	{
		[SerializeField] private GameObject plasmaTrail;
		[SerializeField] private GameObject shell;
		private ParticleSystem ps;
		private GameObject anchor;
		private Transform trailTransform;

		private void Start()
		{
			if (plasmaTrail == null)
			{
				Debug.LogWarning("No plasmaTrail assigned.");
				return;
			}


			anchor = new GameObject("PlasmaTrailAnchor");
			anchor.transform.position = plasmaTrail.transform.position;
			anchor.transform.rotation = plasmaTrail.transform.rotation;


			plasmaTrail.transform.SetParent(anchor.transform, true);
			trailTransform = plasmaTrail.transform;
			ps = plasmaTrail.GetComponent<ParticleSystem>();
			var main = ps.main;
			main.simulationSpace = ParticleSystemSimulationSpace.Custom;
			main.customSimulationSpace = anchor.transform;
		}

		private void Update()
		{
			if (shell == null)
			{
				if (ps != null)
					ps.Stop();
				Destroy(anchor,10f);
				return;
			}
			if (trailTransform != null)
				trailTransform.position = shell.transform.position;
		}
	}
}