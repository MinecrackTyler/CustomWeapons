using System.Collections.Generic;
using UnityEngine;

namespace CustomWeapons
{
	public class OrbitalStrikeController : MonoBehaviour
	{
		private readonly Dictionary<string, HashSet<OrbitalStrikePlatform>> platformsByTeam =
			new Dictionary<string, HashSet<OrbitalStrikePlatform>>();

		private readonly Dictionary<string, HashSet<UplinkHandler>> uplinksByTeam =
			new Dictionary<string, HashSet<UplinkHandler>>();

		public static OrbitalStrikeController Instance { get; private set; }

		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(this);
				return;
			}

			Instance = this;
		}

		public void RegisterUplink(string team, UplinkHandler uplink)
		{
			if (!uplinksByTeam.ContainsKey(team))
				uplinksByTeam[team] = new HashSet<UplinkHandler>();

			uplinksByTeam[team].Add(uplink);
			Debug.Log($"RegisterUplink {team} for {uplink}");
		}

		public void DeregisterUplink(string team, UplinkHandler uplink)
		{
			if (uplinksByTeam.TryGetValue(team, out var set))
			{
				set.Remove(uplink);
				if (set.Count == 0)
					uplinksByTeam.Remove(team);
			}
		}

		public void RegisterPlatform(string team, OrbitalStrikePlatform strikePlatform)
		{
			if (!platformsByTeam.ContainsKey(team))
				platformsByTeam[team] = new HashSet<OrbitalStrikePlatform>();

			platformsByTeam[team].Add(strikePlatform);
			Debug.Log($"RegisterPlatform {team} for {strikePlatform}");
		}

		public void DeregisterPlatform(string team, OrbitalStrikePlatform strikePlatform)
		{
			if (platformsByTeam.TryGetValue(team, out var set))
			{
				set.Remove(strikePlatform);
				if (set.Count == 0)
					platformsByTeam.Remove(team);
			}
		}

		public bool IsReady(Unit caller)
		{
			var team = caller.NetworkHQ.faction.factionName;

			if (!uplinksByTeam.ContainsKey(team) || uplinksByTeam[team].Count == 0) return false;


			if (!platformsByTeam.ContainsKey(team)) return false;

			foreach (var platform in platformsByTeam[team])
				if (platform != null && platform.IsReady())
					return true;

			return false;
		}

		public bool TryFire(Unit target, Unit caller)
		{
			var team = caller.NetworkHQ.faction.factionName;

			if (!platformsByTeam.TryGetValue(team, out var platforms))
				return false;

			foreach (var platform in platforms)
				if (platform.IsReady())
				{
					platform.Fire(target, caller);
					return true;
				}

			return false;
		}

		public int GetAmmo(Unit caller)
		{
			var team = caller.NetworkHQ.faction.factionName;

			if (!platformsByTeam.TryGetValue(team, out var platforms))
				return 0;

			var ammo = 0;

			foreach (var platform in platforms) ammo += platform.GetAmmo();

			return ammo;
		}
	}
}