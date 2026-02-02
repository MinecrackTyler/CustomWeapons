using System.Collections;
using UnityEngine;

namespace CustomWeapons
{
    public class MultiStageBoosterController : MonoBehaviour
    {
        [Tooltip("Boosters in firing order. First in list fires first.")]
        [SerializeField] private AirBooster[] boosters;

        [Tooltip("Delay before the very first booster ignites (seconds).")]
        [SerializeField] private float initialDelay = 0f;

        [Tooltip("Delay between booster separation and next booster ignition (seconds). One per booster stage except last.")]
        [SerializeField] private float[] stageDelays;

        [SerializeField] private Missile missile;

        private int currentStage = 0;
        private bool running = false;

        private void Start()
        {
            if (boosters == null || boosters.Length == 0)
            {
                Debug.LogWarning($"{nameof(MultiStageBoosterController)} has no boosters assigned.");
                return;
            }
            
            if (stageDelays.Length != boosters.Length - 1)
            {
                Debug.LogWarning($"{nameof(MultiStageBoosterController)} stageDelays length should be boosters.Length - 1.");
            }

            StartCoroutine(StageRoutine());
        }

        private IEnumerator StageRoutine()
        {
            running = true;
            
            if (initialDelay > 0f)
                yield return new WaitForSeconds(initialDelay);

            while (currentStage < boosters.Length)
            {
                AirBooster currentBooster = boosters[currentStage];
                
                currentBooster.Activate();
                
                yield return new WaitUntil(() => currentBooster.IsSeparated());
                
                if (currentStage < boosters.Length - 1)
                {
                    float delay = (stageDelays.Length > currentStage) ? stageDelays[currentStage] : 0f;
                    if (delay > 0f)
                        yield return new WaitForSeconds(delay);
                }

                currentStage++;
            }

            running = false;
        }
        
        public float GetRemainingDeltaV()
        {
            float currentMass = missile.GetMass();
            float totalDeltaV = 0f;
            
            foreach (var booster in boosters)
            {
                if (!booster.IsSeparated())
                {
                    float dv = booster.GetRemainingDeltaV(currentMass);
                    totalDeltaV += dv;
                    currentMass -= booster.FuelMass;
                    currentMass -= booster.DryMass;
                }
            }

            return totalDeltaV;
        }

        public float GetRemainingBurnTime()
        {
            float totalBurnTime = 0f;

            foreach (var booster in boosters)
            {
                if (!booster.IsSeparated())
                {
                    totalBurnTime += booster.GetRemainingBurnTime();
                }
            }
            

            return totalBurnTime;
        }

        public bool IsFinished()
        {
            return running == false && currentStage >= boosters.Length;
        }
    }
}
