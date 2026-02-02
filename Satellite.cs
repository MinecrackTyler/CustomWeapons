using NuclearOption.Networking;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomWeapons
{
	public class Satellite : Building, IRadarReturn
	{
		[Header("Orbital Parameters")] public float orbitHeight = 300000f; // Altitude above the map center
		public float orbitalSpeed = 1f; // How fast to orbit (rotation or linear pass rate)

		[Header("Orbiting Behavior")]
		public bool continuousOrbit; // If true, satellite will keep orbiting over its target
		public float passCooldown = 30f; // Delay between passes in continuous mode

		[Header("Orbit Path Settings")] public float mapEdgeOvershootFraction = 0.5f;
		public float minPassAngleDeg = -10f;
		public float maxPassAngleDeg = 10f;
		public bool leftToRight = true;
		
		[SerializeField]
		private GameObject exclusionDisplay;
		
		private readonly GlobalPosition
			hiddenPosition = new GlobalPosition(1000000, 0, 1000000); // Used when satellite is 'not in orbit'


		private GameObject exclusionLine;

		private GlobalPosition currentTargetPosition;
		private Unit currentTargetUnit;
		private bool isOrbiting;
		private float nextPassTime;
		private GlobalPosition passEndGlobal;
		

		private GlobalPosition passStartGlobal;


		private void Start()
		{
			if (NetworkManagerNuclearOption.i?.Server?.Active == true &&
			    GameManager.gameState != GameState.Editor)
				// Place the satellite out of view until orbiting begins
				transform.position = hiddenPosition.ToLocalPosition();
		}

		private void Update()
		{
			if (continuousOrbit && HasTarget())
				if (Time.time >= nextPassTime && !isOrbiting)
					StartOrbitPass();

			if (isOrbiting) UpdateOrbitPath();
			this.radarAlt = this.transform.GlobalPosition().y;
		}
		
		protected override void OnDestroy()
		{
			if (exclusionLine != null)
			{
				Destroy(exclusionLine);
			}
		}
		
		public void SetTarget(Unit target)
		{
			currentTargetUnit = target;
			currentTargetPosition = target.transform.position.ToGlobalPosition();
			GeneratePassPath(currentTargetPosition);
		}
		
		public void SetTarget(Vector3 position)
		{
			currentTargetUnit = null;
			currentTargetPosition = position.ToGlobalPosition();
			GeneratePassPath(currentTargetPosition);
		}
		
		public bool HasTarget()
		{
			return currentTargetUnit != null || currentTargetPosition != null;
		}
		
		public void StartOrbitPass()
		{
			if (!HasTarget())
				return;

			isOrbiting = true;
			
			rb.position = passStartGlobal.ToLocalPosition();
			
			var startPos = passStartGlobal.ToLocalPosition();
			var endPos = passEndGlobal.ToLocalPosition();
			var direction = (endPos - startPos).normalized;
			
			rb.velocity = direction * orbitalSpeed;
			
			rb.rotation = Quaternion.LookRotation(direction);
			exclusionLine = DisplayExclusionLine();
		}

		public void FixedUpdate()
		{
			speed = rb.velocity.magnitude;
		}

		
		public void TriggerManualPass()
		{
			if (!continuousOrbit)
				StartOrbitPass();
		}

		public void TriggerManualPass(float minDistance, Unit target)
		{
			if (!continuousOrbit)
			{
				GeneratePassPath(target.transform.position.ToGlobalPosition(), minDistance);
				StartOrbitPass();
			}
		}

		protected override void OnStartClient()
		{
			if (GameManager.gameState != GameState.Encyclopedia && GameManager.gameState != GameState.Editor)
			{
				transform.position = hiddenPosition.ToLocalPosition();
				RegisterUnit(1f);
				InitializeUnit();
			}
		}


		private void UpdateOrbitPath()
		{
			if (!isOrbiting)
				return;
			
			var currentPos = transform.position;
			var start = passStartGlobal.ToLocalPosition();
			var end = passEndGlobal.ToLocalPosition();
			
			var pathDirection = (end - start).normalized;
			
			var toCurrent = currentPos - start;
			var projected = Vector3.Dot(toCurrent, pathDirection);
			var pathLength = Vector3.Distance(start, end);

			if (projected >= pathLength) EndOrbitPass();
		}
		
		private void EndOrbitPass()
		{
			isOrbiting = false;
			nextPassTime = Time.time + passCooldown;
			Destroy(exclusionLine);
			HideSatellite();
		}

		private void HideSatellite()
		{
			transform.position = hiddenPosition.ToLocalPosition();
		}

		private void GeneratePassPath(GlobalPosition targetGlobal)
		{
			var mapSize = NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings.MapSize;
			var overshootX = mapSize.x * mapEdgeOvershootFraction;

			GeneratePassPath(targetGlobal, overshootX);
		}

		private void GeneratePassPath(GlobalPosition targetGlobal, float minDistanceX)
		{
			var mapSize = NetworkSceneSingleton<LevelInfo>.i.LoadedMapSettings.MapSize;
			var overshootX = minDistanceX;

			var angleDeg = Random.Range(minPassAngleDeg, maxPassAngleDeg);
			var angleRad = angleDeg * Mathf.Deg2Rad;
			
			var baseZ = Mathf.Clamp(targetGlobal.z, -mapSize.y / 2f, mapSize.y / 2f);
			
			var halfMapX = mapSize.x / 2f;
			var deltaZ = Mathf.Tan(angleRad) * halfMapX;

			var startX = leftToRight ? -halfMapX - overshootX : halfMapX + overshootX;
			var endX = leftToRight ? halfMapX + overshootX : -halfMapX - overshootX;

			var startZ = baseZ - deltaZ;
			var endZ = baseZ + deltaZ;

			passStartGlobal = new GlobalPosition(startX, orbitHeight, startZ);
			passEndGlobal = new GlobalPosition(endX, orbitHeight, endZ);
		}

		private GameObject DisplayExclusionLine()
		{
			DynamicMap dm = SceneSingleton<DynamicMap>.i;
			GameObject icon = Instantiate(exclusionDisplay, dm.iconLayer.transform);
			
			GlobalPosition mapTargetPos = new GlobalPosition(currentTargetPosition.x, currentTargetPosition.z, 0f);
			Vector3 mapSelfPos = new Vector3(rb.position.x, rb.position.z, 0f);

			icon.transform.localPosition = mapTargetPos.AsVector3() * dm.mapDisplayFactor;
			
			icon.transform.localScale = new Vector3(4000f, 1000000000, 4000f) * dm.mapDisplayFactor;
			
			Vector2 v1 = new Vector2(mapSelfPos.x, mapSelfPos.y);
			Vector2 v2 = new Vector2(mapTargetPos.ToLocalPosition().x, mapTargetPos.ToLocalPosition().y);
			float angle = Vector2.SignedAngle(v1, v2);
			icon.transform.localEulerAngles = new Vector3(0f, 0f, angle);

			return icon;
		}

		public float GetRadarReturn(Vector3 source, Radar radar, Unit emitter, float dist, float clutter, RadarParams radarParams,
			bool triggerWarning)
		{
			Vector3 direction = FastMath.NormalizedDirection(source, base.transform.position);

			return radarParams.GetSignalStrength(direction, dist, base.rb, RCS, clutter, 0f);
		}

		public float GetJammingIntensity()
		{
			return 0f;
		}
	}
}