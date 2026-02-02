using System.Collections;
using NuclearOption.Networking;
using UnityEngine;

namespace CustomWeapons
{
	public class LaserCannon : Weapon
	{
		[SerializeField] private float maxAngle;

		[SerializeField] private float trackingRate;

		[SerializeField] private AnimationCurve damageAtRange;

		[ColorUsage(true, true)] [SerializeField]
		private Color color;

		[SerializeField] private ParticleSystem[] muzzleParticles;

		[SerializeField] private GameObject hitEffectPrefab;

		[SerializeField] private Renderer beamRenderer;

		[SerializeField] private ParticleSystem[] beamParticles;

		[SerializeField] private float blastDamage;

		[SerializeField] private float fireDamage;

		[SerializeField] private float pierceDamage;

		[SerializeField] private Transform directionTransform;

		[SerializeField] private AudioClip fireStart;

		[SerializeField] private bool vehicularPowerSupply;

		[SerializeField] private float cooldownTime = 2f;

		[SerializeField] private float fireDuration = 0.2f;

		[SerializeField] private float chargeTime = 1f;
		
		[SerializeField] private GameObject chargeEffectPrefab;

		[SerializeField] private Transform chargeEffectTransform;

		private float beamScale;

		private GameObject chargeEffect;

		private Transform currentTargetTransform;

		private Unit currentTargetUnit;

		private bool fireCommanded;

		private float fireStartTime;

		private GameObject hitEffectSpawn;

		private ParticleSystem[] hitParticles;

		private int lastAmmo = -1;

		private float lastDamageTick;

		private float nextAllowedFireTime;

		private AudioSource source;

		private void Start()
		{
			beamRenderer.enabled = false;
			lastDamageTick = Time.timeSinceLevelLoad;
			source = gameObject.AddComponent<AudioSource>();
			source.outputAudioMixerGroup = SoundManager.i.EffectsMixer;
			source.spatialBlend = 1f;
			source.spread = 20f;
			source.dopplerLevel = 0f;
			source.minDistance = 10f;
			source.maxDistance = 50f;
		}

		private void FixedUpdate()
		{
			var vector = currentTargetTransform != null
				? currentTargetTransform.position
				: transform.position + transform.forward * 20000f;
			if (currentTargetUnit != null && !attachedUnit.NetworkHQ.IsTargetBeingTracked(currentTargetUnit) &&
			    attachedUnit.NetworkHQ.TryGetKnownPosition(currentTargetUnit, out var knownPosition))
				vector = knownPosition.ToLocalPosition();
			var vector2 = vector - transform.position;
			directionTransform.rotation =
				Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, vector2, maxAngle * (Mathf.PI / 180f),
					0f));
			var num = Vector3.Angle(directionTransform.forward, vector2);
			if (num > 1f) vector = transform.position + directionTransform.forward * 20000f;
			if (fireCommanded)
			{
				var array = muzzleParticles;
				for (var i = 0; i < array.Length; i++) array[i].Play();
				var num2 = 1f;
				beamRenderer.enabled = true;
				if (num > 1f) return;
				if (Physics.Linecast(directionTransform.position, vector, out var hitInfo, -8193))
				{
					beamRenderer.transform.localScale = new Vector3(beamScale, beamScale, hitInfo.distance);
					foreach (var ps in beamParticles)
					{
						var sh = ps.shape;
						var scale = sh.scale.x;
						sh.scale = new Vector3(scale, scale, hitInfo.distance / 100);
					}


					hitEffectSpawn.transform.SetParent(hitInfo.transform);
					hitEffectSpawn.transform.position = hitInfo.point;
					array = hitParticles;
					for (var i = 0; i < array.Length; i++) array[i].Play();
					var component = hitInfo.collider.gameObject.GetComponent<IDamageable>();
					if (NetworkManagerNuclearOption.i.Server.Active && component != null &&
					    Time.timeSinceLevelLoad - lastDamageTick > 0.2f)
					{
						lastDamageTick = Time.timeSinceLevelLoad;
						var num3 = damageAtRange.Evaluate(hitInfo.distance) * num2;
						component.TakeDamage(pierceDamage * num3 * 0.2f, blastDamage * num3 * 0.2f, 1f,
							fireDamage * num3 * 0.2f, 0f, attachedUnit.persistentID);
					}
				}
			}
			else
			{
				beamRenderer.enabled = false;
			}
		}

		private void LateUpdate()
		{
			if (weaponStation != null && ammo != lastAmmo)
			{
				lastAmmo = ammo;
				weaponStation.Ammo = ammo;
				weaponStation.Updated();
			}

			if (!fireCommanded)
				return;

			if (Time.timeSinceLevelLoad > fireStartTime + fireDuration)
			{
				if (chargeEffect != null)
				{
					Destroy(chargeEffect);
				}
				fireCommanded = false;
				beamRenderer.enabled = false;
				foreach (var ps in beamParticles) ps.Pause();
			}
		}

		public override void AttachToUnit(Unit unit)
		{
			attachedUnit = unit;
		}

		public override void SetTarget(Unit target)
		{
			enabled = true;
			currentTargetTransform = target != null ? target.transform : null;
			currentTargetUnit = target;
		}

		public override void Fire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation,
			GlobalPosition aimpoint)
		{
			beamScale = beamRenderer.transform.localScale.x;
			if (Time.timeSinceLevelLoad < nextAllowedFireTime)
				return;

			if (ammo <= 0) return;

			ammo--;

			nextAllowedFireTime = Time.timeSinceLevelLoad + cooldownTime + chargeTime;
			StartCoroutine(ChargeFire(owner, target, inheritedVelocity, weaponStation, aimpoint));
			
		}

		private IEnumerator ChargeFire(Unit owner, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation,
			GlobalPosition aimpoint)
		{
			chargeEffect = Instantiate(chargeEffectPrefab, chargeEffectTransform);
			yield return new WaitForSeconds(chargeTime);
			fireStartTime = Time.timeSinceLevelLoad;
			beamRenderer.enabled = true;
			fireCommanded = true;

			lastFired = Time.timeSinceLevelLoad;
			weaponStation.LastFiredTime = Time.timeSinceLevelLoad;

			if (hitEffectSpawn == null)
			{
				hitEffectSpawn = Instantiate(hitEffectPrefab, null);
				hitParticles = hitEffectSpawn.GetComponentsInChildren<ParticleSystem>();
			}

			if (source != null && fireStart != null) source.PlayOneShot(fireStart);

			foreach (var ps in beamParticles) ps.Play();

			var mat = beamRenderer.material;
			var baseColor = mat.color; // Or whatever your beam base color is
			var maxIntensity = 9.2f; // HDR intensity (you probably already have this value)
			var fadeDuration = 0.2f;

			StartCoroutine(FadeInEmission(mat, baseColor, maxIntensity, fadeDuration));
			foreach (var ps in beamParticles)
			{
				mat = ps.GetComponent<Renderer>().material;
				baseColor = mat.color; // Or whatever your beam base color is
				maxIntensity = 3f; // HDR intensity (you probably already have this value)
				fadeDuration = 0.2f;
				StartCoroutine(FadeInEmission(mat, baseColor, maxIntensity, fadeDuration));
			}
		}

		private IEnumerator FadeInEmission(Material mat, Color baseColor, float maxIntensity, float duration)
		{
			var t = 0f;
			while (t < duration)
			{
				t += Time.deltaTime;
				var intensity = Mathf.Lerp(1f, maxIntensity, t / duration);
				SetCustomMaterialEmissionIntensity(mat, intensity);
				yield return null;
			}

			SetCustomMaterialEmissionIntensity(mat, maxIntensity);
		}

		private void SetCustomMaterialEmissionIntensity(Material mat, float intensity)
		{
			// get the material at this path
			var color = mat.GetColor("_Color");

			// for some reason, the desired intensity value (set in the UI slider) needs to be modified slightly for proper internal consumption
			var adjustedIntensity = intensity - 0.4169F;

			// redefine the color with intensity factored in - this should result in the UI slider matching the desired value
			color *= Mathf.Pow(2.0F, adjustedIntensity);
			mat.SetColor("_EmissionColor", color);
		}
	}
}