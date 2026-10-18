using System.Collections;
using PauseSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerSystem
{
    /// <summary>
    /// Hiding behind cover (<see cref="HidingSpot"/>): stand next to it, or look at it from close by, and press E
    /// (gamepad X) to crouch down behind it, E again to get up and step out. While hidden the player cannot walk but
    /// can look round and peek over the cover. <see cref="PromptKey"/> says what E would do now, for the HUD.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHide : MonoBehaviour
    {
        public Camera view;
        [Tooltip("How far away cover can be aimed at, metres.")]
        public float reach = 2.3f;
        [Tooltip("Cover this close (from the feet, sideways) counts without aiming at it, if the player roughly faces it.")]
        public float nearReach = 1.6f;
        [Tooltip("Seconds to crouch down behind it, or to get up.")]
        public float moveTime = 0.45f;
        [Tooltip("Off: E does nothing (cutscenes, scares).")]
        public bool allowed = true;

        /// <summary>Crouched behind cover (not while still getting there).</summary>
        public bool Hidden { get; private set; }
        /// <summary>Crouching down or getting up just now.</summary>
        public bool Moving { get; private set; }
        public HidingSpot Spot { get; private set; }
        /// <summary>The cover E would hide behind now, or null.</summary>
        public HidingSpot Aimed { get; private set; }

        /// <summary>Localization key of what E would do now, or null.</summary>
        public string PromptKey => !allowed || Moving || !isActiveAndEnabled ? null : Hidden ? "hint.unhide" : Aimed != null ? "hint.hide" : null;

        PlayerController walker;
        CharacterController body;

        void Awake()
        {
            walker = GetComponent<PlayerController>();
            body = GetComponent<CharacterController>();
            if (view == null) view = GetComponentInChildren<Camera>();
        }

        void Update()
        {
            if (GamePause.Paused) return;
            Aimed = allowed && !Hidden && !Moving ? Find() : null;
            if (!allowed || Moving || !Pressed()) return;
            if (Hidden) StartCoroutine(GetUp());
            else if (Aimed != null) StartCoroutine(Crouch(Aimed));
        }

        static bool Pressed() =>
            (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);

        HidingSpot Find()
        {
            if (view == null) return null;
            var t = view.transform;
            if (Physics.Raycast(t.position, t.forward, out var hit, reach, ~0, QueryTriggerInteraction.Ignore))
            {
                var aimed = hit.collider.GetComponentInParent<HidingSpot>();
                if (aimed != null) return aimed;
            }

            // Low cover is easy to look past: standing right by it and roughly facing it is enough.
            Vector3 feet = transform.position, facing = Vector3.ProjectOnPlane(t.forward, Vector3.up);
            HidingSpot best = null;
            float bestDistance = nearReach;
            foreach (var spot in HidingSpot.All)
            {
                Vector3 closest = spot.ClosestPoint(feet + Vector3.up * 0.3f);
                if (Mathf.Abs(closest.y - feet.y) > 1.2f) continue; // another floor
                Vector3 to = Vector3.ProjectOnPlane(closest - feet, Vector3.up);
                float distance = to.magnitude;
                if (distance > bestDistance || (distance > 0.25f && Vector3.Angle(facing, to) > 75f)) continue;
                best = spot;
                bestDistance = distance;
            }
            return best;
        }

        IEnumerator Crouch(HidingSpot spot)
        {
            Moving = true;
            Spot = spot;
            walker.canMove = false;
            walker.canLook = false;
            Vector3 from = transform.position, to = spot.seat != null ? spot.seat.position : from;
            to.y = from.y;
            float yawFrom = transform.eulerAngles.y, yawTo = spot.seat != null ? spot.seat.eulerAngles.y : yawFrom, pitchFrom = walker.Pitch;
            yield return Glide(from, to, yawFrom, yawTo, pitchFrom, -5f, 0f, spot.eyeDrop); // eyes just over the edge
            walker.canLook = true;
            Hidden = true;
            Moving = false;
        }

        IEnumerator GetUp()
        {
            Moving = true;
            Hidden = false;
            Vector3 from = transform.position, to = Spot != null && Spot.exit != null ? Spot.exit.position : from;
            to.y = from.y;
            float yaw = transform.eulerAngles.y;
            yield return Glide(from, to, yaw, yaw, walker.Pitch, walker.Pitch, walker.headDrop, 0f);
            Spot = null;
            walker.canMove = true;
            Moving = false;
        }

        IEnumerator Glide(Vector3 from, Vector3 to, float yawFrom, float yawTo, float pitchFrom, float pitchTo, float dropFrom, float dropTo)
        {
            body.enabled = false;
            for (float t = 0f; t < moveTime; t += Time.deltaTime)
            {
                float u = t / moveTime;
                u = u * u * (3f - 2f * u);
                transform.SetPositionAndRotation(Vector3.Lerp(from, to, u), Quaternion.Euler(0f, Mathf.LerpAngle(yawFrom, yawTo, u), 0f));
                walker.Pitch = Mathf.Lerp(pitchFrom, pitchTo, u);
                walker.headDrop = Mathf.Lerp(dropFrom, dropTo, u);
                yield return null;
            }
            transform.SetPositionAndRotation(to, Quaternion.Euler(0f, yawTo, 0f));
            walker.Pitch = pitchTo;
            walker.headDrop = dropTo;
            body.enabled = true;
        }

        /// <summary>Up at once where the player is (a scare takes over). Walking and looking are left to the caller.</summary>
        public void Release()
        {
            StopAllCoroutines();
            if (body != null) body.enabled = true;
            if (walker != null) walker.headDrop = 0f;
            Hidden = false;
            Moving = false;
            Spot = null;
            Aimed = null;
        }
    }
}
