using System;
using UnityEngine;

namespace ExitSystem
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ExitTrigger : MonoBehaviour
    {
        public static event Action<ExitDirection> ExitEntered;

        [Header("Settings")]
        [SerializeField] private ExitDirection _direction;

        private const float SideThreshold = 0.05f;

        private bool _isArmed;
        private bool _hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<CharacterController>(out _))
                return;

            if (_hasTriggered)
                return;

            switch (_direction)
            {
                case ExitDirection.Forward:
                    TriggerExit();
                    break;

                case ExitDirection.Backward:
                    if (_isArmed)
                        TriggerExit();
                    break;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_direction != ExitDirection.Backward)
                return;

            if (_isArmed || _hasTriggered)
                return;

            if (!other.TryGetComponent<CharacterController>(out _))
                return;

            Vector3 offset =
                other.transform.position - transform.position;

            float side = Vector3.Dot(
                offset,
                transform.forward
            );

            if (side <= SideThreshold)
                return;

            _isArmed = true;

            Debug.Log("BackwardExit armed");
        }

        private void TriggerExit()
        {
            _hasTriggered = true;

            Debug.Log($"Player selected: {_direction}");

            ExitEntered?.Invoke(_direction);
        }

        public void ResetTrigger()
        {
            _isArmed = false;
            _hasTriggered = false;
        }
    }
}