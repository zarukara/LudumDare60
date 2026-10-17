using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Office door that swings open away from the player when they come near and closes once they are gone.
    /// Sits on the hinge: the leaf runs along local +X from here, the wall's normal is local Z.
    /// </summary>
    public class AutoDoor : MonoBehaviour
    {
        public float leafWidth = 0.92f;
        public float openAngle = 95f;
        public float degreesPerSecond = 220f;
        public float openDistance = 2.2f;
        public float closeDistance = 3.4f;
        [Tooltip("Disabled while the leaf moves, so it never shoves the player.")]
        public Collider leafCollider;

        Transform viewer;
        Quaternion closedRotation;
        float angle, targetAngle;

        void Awake() => closedRotation = transform.localRotation;

        void Update()
        {
            if (viewer == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                viewer = cam.transform;
            }

            // Hinge space with the door closed (the leaf's current rotation must not affect the measurement).
            Vector3 local = Quaternion.Inverse(transform.parent != null ? transform.parent.rotation * closedRotation : closedRotation)
                            * (viewer.position - transform.position);
            float distance = new Vector2(local.x - leafWidth / 2f, local.z).magnitude;
            if (targetAngle == 0f && distance < openDistance)
                targetAngle = local.z > 0f ? openAngle : -openAngle; // positive yaw swings the leaf towards -Z
            else if (targetAngle != 0f && distance > closeDistance)
                targetAngle = 0f;

            angle = Mathf.MoveTowards(angle, targetAngle, degreesPerSecond * Time.deltaTime);
            transform.localRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
            if (leafCollider != null) leafCollider.enabled = Mathf.Approximately(angle, targetAngle);
        }
    }
}
