using System.Collections.Generic;
using UnityEngine;

namespace CustomWeapons
{
    public class DatalinkTargetDetector : TargetDetector
    {
        [Header("Filtering Options")]
        public bool useWhitelist = false;

        [Tooltip("json keys")]
        public List<string> filterList = new List<string>();

        private HashSet<string> filterSet;

        protected override void Awake()
        {
            base.Awake();
            // Build hashset for fast lookup
            filterSet = new HashSet<string>(filterList);
        }

        protected override void TargetSearch()
        {
            if (attachedUnit == null || attachedUnit.NetworkHQ == null)
                return;

            detectedTargets.Clear();
            
            List<Unit> potentialTargets = DatalinkCheck();
            
            foreach (var unit in potentialTargets)
            {
                DetectTarget(unit);
            }
        }

        private List<Unit> DatalinkCheck()
        {
            List<Unit> results = new List<Unit>();

            foreach (var kvp in attachedUnit.NetworkHQ.trackingDatabase)
            {
                if (kvp.Value == null) 
                    continue;

                if (kvp.Value.TryGetUnit(out Unit unit) && unit != null && !unit.disabled)
                {
                    if (unit.definition is AircraftDefinition)
                    {
                        results.Add(unit);
                        continue;
                    }
                    
                    string key = unit.definition?.jsonKey ??  "";

                    if (string.IsNullOrEmpty(key))
                        continue;

                    bool inList = filterSet.Contains(key);
                    
                    switch (useWhitelist)
                    {
                        case true when inList:
                        case false when !inList:
                            results.Add(unit);
                            break;
                    }
                }
            }

            return results;
        }
    }
}