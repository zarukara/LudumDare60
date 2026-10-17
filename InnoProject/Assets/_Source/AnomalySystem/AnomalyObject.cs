using System.Collections.Generic;
using UnityEngine;

namespace AnomalySystem
{
    /// <summary>
    /// Root of an anomaly prefab (old tech that should not be in the office). The prefab's own position and
    /// rotation are its place in the office (office space: origin at the inner south-west corner on the floor, +Z
    /// towards the exit), so spawning it under the office puts it where it belongs. While it is there, the office
    /// objects listed in <see cref="replaces"/> are hidden.
    /// </summary>
    public class AnomalyObject : Anomaly
    {
        [Tooltip("Office objects hidden while this anomaly is present: paths under the office root, e.g. Workstations/Desk_E2/Phone.")]
        public string[] replaces = new string[0];

        readonly List<GameObject> hidden = new List<GameObject>();

        protected override void OnActivate()
        {
            hidden.Clear();
            var room = transform.parent;
            if (room == null) return;
            foreach (var path in replaces)
            {
                var target = room.Find(path);
                if (target == null)
                {
                    Debug.LogWarning($"Anomaly '{anomalyName}': no '{path}' under {room.name}.", this);
                    continue;
                }
                if (!target.gameObject.activeSelf) continue;
                target.gameObject.SetActive(false);
                hidden.Add(target.gameObject);
            }
        }

        protected override void OnDeactivate()
        {
            foreach (var go in hidden)
                if (go != null) go.SetActive(true);
            hidden.Clear();
        }
    }
}
