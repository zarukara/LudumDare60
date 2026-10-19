using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnomalySystem
{

    public static class AnomalyDirector
    {

        public static Anomaly Spawn(IReadOnlyList<GameObject> prefabs, Transform office, System.Random rng, List<string> recent, string forced = null)
        {
            var valid = prefabs?.Where(p => p != null).ToList();
            if (valid == null || valid.Count == 0) return null;

            GameObject pick = null;
            if (!string.IsNullOrEmpty(forced))
                pick = valid.FirstOrDefault(p => p.name == forced || DisplayName(p) == forced);
            if (pick == null)
            {
                var pool = valid.Where(p => !recent.Contains(p.name)).ToList();
                if (pool.Count == 0)
                {

                    string previous = recent.Count > 0 ? recent[recent.Count - 1] : null;
                    recent.Clear();
                    pool = valid.Where(p => p.name != previous).ToList();
                    if (pool.Count == 0) pool = valid;
                }
                pick = pool[rng.Next(pool.Count)];
            }

            var instance = Object.Instantiate(pick, office, false);
            instance.name = pick.name;
            instance.SetActive(true);
            var anomaly = instance.GetComponent<Anomaly>();
            if (anomaly == null) anomaly = instance.AddComponent<AnomalyObject>();
            if (string.IsNullOrEmpty(anomaly.anomalyName)) anomaly.anomalyName = pick.name;
            anomaly.Activate();

            if (!recent.Contains(pick.name)) recent.Add(pick.name);
            return anomaly;
        }

        public static void Remove(Anomaly anomaly)
        {
            if (anomaly == null) return;
            anomaly.Deactivate();
            Object.Destroy(anomaly.gameObject);
        }

        static string DisplayName(GameObject prefab)
        {
            var anomaly = prefab.GetComponent<Anomaly>();
            return anomaly != null ? anomaly.anomalyName : prefab.name;
        }
    }
}
