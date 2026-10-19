using System.Collections.Generic;
using UnityEngine;

namespace AnomalySystem
{

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
