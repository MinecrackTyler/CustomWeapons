using NuclearOption.Networking;
using UnityEngine;

namespace CustomWeapons
{
	public class OrbitalStrikePlatform : MonoBehaviour
	{
		[SerializeField] private string team;
		[SerializeField] private int ammoCount = 1;
		[SerializeField] private float cooldownSeconds = 60f;
		[SerializeField] private GameObject projectilePrefab;
		[SerializeField] private Satellite satellite;
		[SerializeField] private float fireSpeed = 3000f;
		[SerializeField] private float fireDistance = 1000f;
		private Unit currentCaller;
		private Unit currentTarget;
		private bool inPass;

		private float lastFireTime = -Mathf.Infinity;

		public string Team => team;

		private void Awake()
		{
			if (NetworkManagerNuclearOption.i?.Server?.Active == true &&
			    GameManager.gameState != GameState.Editor) Invoke("Register", 1.0f);
		}

		public void Update()
		{
			if (inPass)
			{
				var min = GetMinimumFiringDistance() - fireDistance;
				var max = GetMinimumFiringDistance() + fireDistance;
				var distance = Vector3.Distance(currentTarget.transform.position, transform.position);
				if (distance > min && distance < max)
				{
					FireProjectile(currentTarget, currentCaller);
					inPass = false;
					lastFireTime = Time.time;
				}
			}
		}

		private void OnDestroy()
		{
			if (!string.IsNullOrEmpty(team) && OrbitalStrikeController.Instance != null)
				OrbitalStrikeController.Instance.DeregisterPlatform(team, this);
		}

		private void Register()
		{
			team = satellite.NetworkHQ.faction.factionName;

			if (!string.IsNullOrEmpty(team) && OrbitalStrikeController.Instance != null)
				OrbitalStrikeController.Instance.RegisterPlatform(team, this);
		}

		public bool IsReady()
		{
			return ammoCount > 0 && Time.time >= lastFireTime + cooldownSeconds;
		}
		
		public void RefillAmmo(int amount)
		{
			ammoCount += amount;
		}

		public void SetTeam(string newTeam)
		{
			if (OrbitalStrikeController.Instance != null)
			{
				OrbitalStrikeController.Instance.DeregisterPlatform(team, this);
				team = newTeam;
				OrbitalStrikeController.Instance.RegisterPlatform(team, this);
			}
		}

		public void Fire(Unit target, Unit caller)
		{
			if (!IsReady())
				return;


			satellite.SetTarget(target);
			satellite.TriggerManualPass(GetMinimumFiringDistance(), target);
			inPass = true;
			currentTarget = target;
			currentCaller = caller;

			ammoCount--;
		}

		private void FireProjectile(Unit target, Unit caller)
		{
			var spawnPosition = satellite.transform.position - satellite.transform.forward * 100f;
			var rotation = Quaternion.LookRotation(satellite.transform.forward);

			var spawned = NetworkSceneSingleton<Spawner>.i.SpawnMissile(
				projectilePrefab,
				spawnPosition,
				rotation,
				satellite.transform.forward * satellite.orbitalSpeed - satellite.transform.forward * fireSpeed,
				target,
				caller
			);
		}

		public int GetAmmo()
		{
			return ammoCount;
		}

		public float GetMinimumFiringDistance()
		{
			if (satellite == null)
				return 0f;

			return CalculateMinimumRange(satellite.orbitHeight, satellite.orbitalSpeed, fireSpeed);
		}

		private static float CalculateMinimumRange(float orbitHeight, float orbitalSpeed, float fireSpeed)
		{
			var v_rel = orbitalSpeed - fireSpeed;

			if (v_rel <= 0)
				return float.PositiveInfinity;
			
			var t_fall = Mathf.Sqrt(2 * orbitHeight / Physics.gravity.y);
			
			return v_rel * t_fall;
		}

		private float Vector2DDistance(Vector3 v1, Vector3 v2)
		{
			var xDiff = v1.x - v2.x;
			var zDiff = v1.z - v2.z;
			return Mathf.Sqrt(xDiff * xDiff + zDiff * zDiff);
		}
	}
}