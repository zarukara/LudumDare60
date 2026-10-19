using UnityEngine;

namespace AnomalySystem
{

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
