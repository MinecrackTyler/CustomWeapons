using UnityEngine;

namespace CustomWeapons
{
	public class UnitParachuteSystem : MonoBehaviour
	{
		[SerializeField] private GameObject parachutePrefab;

		private Unit unit;

		private void Awake()
		{
			unit = GetComponent<Unit>();
			if (unit != null && unit.Identity != null)
			{
				unit.Identity.OnStartClient.AddListener(OnStartClient);
			}
		}

		private void OnStartClient()
		{
			if (GameManager.gameState == GameState.Encyclopedia)
				return;

			if (parachutePrefab == null)
				return;

			Vector3 pos = transform.position;
			
			if (!Physics.Linecast(pos, pos - Vector3.up * 10f, 8256))
			{
				var chute = Instantiate(parachutePrefab, transform);
				var cargoDeploy = chute.GetComponent<CargoDeploymentSystem>();
				if (cargoDeploy != null)
					cargoDeploy.Initialize(unit);
			}
		}
	}
}