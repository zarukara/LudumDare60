using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnomalySystem
{
    /// <summary>Spawns the round's anomaly into the office and removes it again; at most one at a time.</summary>
    public static class AnomalyDirector
    {
        /// <summary>
        /// Puts one random anomaly prefab into the office. They come like cards from a shuffled deck: none repeats
        /// until every one has been shown, then the deck starts over (never with the one just shown).
        /// </summary>
        /// <param name="recent">Names shown from the current deck, newest last; updated here.</param>
        /// <param name="forced">Prefab or anomaly name to use instead of a random one (for testing).</param>
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
                    // All shown: a new deck.
                    string previous = recent.Count > 0 ? recent[recent.Count - 1] : null;
                    recent.Clear();
                    pool = valid.Where(p => p.name != previous).ToList();
                    if (pool.Count == 0) pool = valid;
                }
                pick = pool[rng.Next(pool.Count)];
            }

            var instance = Object.Instantiate(pick, office, false); // the prefab's pose is its place in the office
            instance.name = pick.name;
            instance.SetActive(true);
            var anomaly = instance.GetComponent<Anomaly>();
            if (anomaly == null) anomaly = instance.AddComponent<AnomalyObject>();
            if (string.IsNullOrEmpty(anomaly.anomalyName)) anomaly.anomalyName = pick.name;
            anomaly.Activate();

            if (!recent.Contains(pick.name)) recent.Add(pick.name);
            return anomaly;
        }

        /// <summary>Takes the anomaly out of the office and brings back what it replaced.</summary>
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
