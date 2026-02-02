using NuclearOption.Networking;
using UnityEngine;

namespace CustomWeapons
{
	public class UplinkHandler : MonoBehaviour
	{
		[SerializeField] private string team;
		[SerializeField] private Unit attachedUnit;

		public string Team => team;

		private void Awake()
		{
			if (NetworkManagerNuclearOption.i?.Server?.Active == true &&
			    GameManager.gameState != GameState.Editor)
				Invoke("Register", 1.0f);
		}

		private void OnDestroy()
		{
			if (!string.IsNullOrEmpty(team) && OrbitalStrikeController.Instance != null)
				OrbitalStrikeController.Instance.DeregisterUplink(team, this);
		}

		private void Register()
		{
			team = attachedUnit.NetworkHQ.faction.factionName;

			if (!string.IsNullOrEmpty(team) && OrbitalStrikeController.Instance != null)
				OrbitalStrikeController.Instance.RegisterUplink(team, this);
		}

		public void SetTeam(string newTeam)
		{
			if (OrbitalStrikeController.Instance != null)
			{
				OrbitalStrikeController.Instance.DeregisterUplink(team, this);
				team = newTeam;
				OrbitalStrikeController.Instance.RegisterUplink(team, this);
			}
		}
	}
}