using System;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Office door that swings open away from the player when they come near and closes once they are gone.
    /// Sits on the hinge: the leaf runs along local +X from here, the wall's normal is local Z.
    /// Scripts can slam it shut and lock it (<see cref="Slam"/>) and make the closed leaf rattle (<see cref="Knock"/>).
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
        [Tooltip("A locked door stays shut.")]
        public bool locked;

        [Header("Knocks")]
        [Tooltip("Degrees the leaf rattles at a blow of strength 1.")]
        public float knockRattle = 4f;
        [Tooltip("Degrees a blow of strength 1 bends the shut leaf away from the one who hits it, against the latch.")]
        public float knockPush = 3.5f;

        /// <summary>Fired when the door starts to open for the player.</summary>
        public event Action Opened;

        public bool IsOpen => targetAngle != 0f;

        Transform viewer;
        Quaternion closedRotation;
        float angle, targetAngle, slamSpeed, rattle, rattleTime, push, pushSign = 1f;

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
            if (targetAngle == 0f && distance < openDistance && !locked)
            {
                targetAngle = local.z > 0f ? openAngle : -openAngle; // positive yaw swings the leaf towards -Z
                Opened?.Invoke();
            }
            else if (targetAngle != 0f && distance > closeDistance)
            {
                targetAngle = 0f;
            }

            angle = Mathf.MoveTowards(angle, targetAngle, (slamSpeed > 0f ? slamSpeed : degreesPerSecond) * Time.deltaTime);
            if (Mathf.Approximately(angle, targetAngle)) slamSpeed = 0f;

            rattle = Mathf.MoveTowards(rattle, 0f, Time.deltaTime * 2.2f);
            push = Mathf.MoveTowards(push, 0f, Time.deltaTime * 5f);
            rattleTime += Time.deltaTime;
            float shake = rattle * (Mathf.Sin(rattleTime * 61f) * 0.6f + Mathf.Sin(rattleTime * 37f) * 0.4f) * knockRattle;
            float bulge = pushSign * push * push * knockPush;
            transform.localRotation = closedRotation * Quaternion.Euler(0f, angle + shake + bulge, 0f);
            if (leafCollider != null) leafCollider.enabled = Mathf.Approximately(angle, targetAngle);
        }

        /// <summary>Shuts at once, hard, and stays shut until <see cref="Unlock"/>.</summary>
        public void Slam(float speed = 1100f)
        {
            locked = true;
            targetAngle = 0f;
            slamSpeed = speed;
            Knock(0.8f);
        }

        public void Unlock() => locked = false;

        /// <summary>Rattles the leaf as from a blow (about 0..1.5).</summary>
        public void Knock(float strength) => rattle = Mathf.Max(rattle, strength);

        /// <summary>A blow from <paramref name="from"/>: the leaf rattles and bends away from it for a moment.</summary>
        public void Knock(float strength, Vector3 from)
        {
            Knock(strength);
            Vector3 local = Quaternion.Inverse(transform.parent != null ? transform.parent.rotation * closedRotation : closedRotation)
                            * (from - transform.position);
            pushSign = local.z > 0f ? 1f : -1f; // positive yaw swings the leaf towards -Z, away from a hitter at +Z
            push = Mathf.Max(push, Mathf.Min(strength, 1.3f));
        }
    }
}
