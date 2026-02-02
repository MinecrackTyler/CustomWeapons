using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomWeapons.Stonehenge
{
    public class ChargedGun : Gun
    {
        [Header("Charge Settings")]
        [SerializeField] private float chargeTime = 2f;

        [Header("Charge Effects")]
        [SerializeField] private GameObject[] chargePrefabs;       // Spawned at muzzle
        [SerializeField] private AudioClip[] chargeSounds;         // Played during charge
        [SerializeField] private ParticleSystem[] chargeParticles; // Played during charge
        
        [Header("Fire Effects")]
        [SerializeField] private GameObject[] firePrefabs; // Played during fire

        private bool isCharging;
        [SerializeField] private AudioSource audioSource; // assignable in inspector

        //wait, it's all private?
        //always has been
        private Transform[] muzzles;
        private FieldInfo bulletsLoadedField;

        private float lastFireTime = 0f;
        
        private Unit firingUnit;
        private Unit target;
        private Vector3 inheritedVelocity;
        private WeaponStation tempWeaponStation;
        private GlobalPosition aimpoint;

        protected void Awake()
        {
            //IHATEPRIVATEMETHODS
            Type baseType = typeof(ChargedGun).BaseType;
            MethodInfo awakeMethod = baseType.GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            if (awakeMethod != null)
            {
                awakeMethod.Invoke(this, null);
            }
            else
            {
                //uh oh
                Destroy(this);
            }
            
            FieldInfo muzzleField = baseType.GetField("muzzles", BindingFlags.NonPublic | BindingFlags.Instance);
            if (muzzleField != null)
            {
                this.muzzles = muzzleField.GetValue(this) as Transform[];
            }
            else
            {
                //why :(
            }
            
            bulletsLoadedField = baseType.GetField("bulletsLoaded", BindingFlags.NonPublic | BindingFlags.Instance);
            if (bulletsLoadedField == null)
            {
                //aw man
            }
            
            

            // If no audio source assigned in inspector, try to find one
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1f; // 3D sound
                }
            }
        }

        public override void Fire(Unit firingUnit, Unit target, Vector3 inheritedVelocity, WeaponStation weaponStation, GlobalPosition aimpoint)
        {
            if (isCharging || bulletsLoadedField == null)
            {
                return;
            }

            if (Time.timeSinceLevelLoad - lastFireTime < 12 || Time.timeSinceLevelLoad < 31)
            {
                return;
            }
            
            int currentBullets = (int)bulletsLoadedField.GetValue(this);
            
            if (currentBullets > 0)
            {
                lastFireTime = Time.timeSinceLevelLoad;
                isCharging = true;
                
                this.firingUnit = firingUnit;
                this.target = target;
                this.inheritedVelocity = inheritedVelocity;
                this.tempWeaponStation = weaponStation;
                this.aimpoint = aimpoint;

                StartCoroutine(FireWithCharge());
            }
        }

        private IEnumerator FireWithCharge()
        {
            // Play particles
            if (chargeParticles != null)
            {
                foreach (var ps in chargeParticles)
                {
                    if (ps != null) ps.Play();
                }
            }

            // Play sounds
            if (chargeSounds != null && chargeSounds.Length > 0 && audioSource != null)
            {
                var clip = chargeSounds[Random.Range(0, chargeSounds.Length)];
                if (clip != null) audioSource.PlayOneShot(clip);
            }

            // Spawn prefabs at muzzle
            if (chargePrefabs != null && chargePrefabs.Length > 0 && muzzles != null && muzzles.Length > 0)
            {
                foreach (var prefab in chargePrefabs)
                {
                    if (prefab != null)
                        Instantiate(prefab, muzzles[0].position, muzzles[0].rotation);
                }
            }

            // Wait charge time
            yield return new WaitForSeconds(chargeTime);

            // Actually fire the gun
            attachedUnit.displayDetail = attachedUnit.displayDetail < 1 ? 1 : attachedUnit.displayDetail;
            base.Fire(firingUnit, target, inheritedVelocity, tempWeaponStation, aimpoint);

            if (firePrefabs != null && firePrefabs.Length > 0 && muzzles != null && muzzles.Length > 0)
            {
                foreach (var prefab in firePrefabs)
                {
                    if (prefab != null)
                        Instantiate(prefab, muzzles[0].position, muzzles[0].rotation);
                }
            }
            
            isCharging = false;
        }
    }
}
