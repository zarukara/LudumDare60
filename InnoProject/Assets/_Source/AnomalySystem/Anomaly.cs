using UnityEngine;

namespace AnomalySystem
{
    /// <summary>
    /// One thing that can be wrong with the room (for now: old tech that should not be there). The room keeps all
    /// of them switched off; <see cref="AnomalyDirector"/> activates at most one per round and deactivates it when
    /// the round changes. To add an anomaly, put an object into the room's "Anomalies" group, give it a
    /// <see cref="ToggleAnomaly"/> and switch the object off.
    /// </summary>
    public abstract class Anomaly : MonoBehaviour
    {
        [Tooltip("Readable name, shown in the debug overlay (F3) and usable in RoundManager.forceAnomaly.")]
        public string anomalyName;

        public bool IsActive { get; private set; }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            OnActivate();
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            OnDeactivate();
        }

        protected abstract void OnActivate();
        protected abstract void OnDeactivate();
    }
}
