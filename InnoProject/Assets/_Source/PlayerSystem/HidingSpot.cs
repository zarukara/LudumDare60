using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Cover the player can hide behind or inside with <see cref="PlayerHide"/> (stand close, face it, E). Needs a
    /// collider on this object or a child. <see cref="seat"/> is where the player sits and which way they face,
    /// <see cref="exit"/> where they stand up again. A closet has doors that swing open while the player gets in or
    /// out, and a <see cref="peek"/> point: where something outside puts its eye to look in through the gap.
    /// </summary>
    public class HidingSpot : MonoBehaviour
    {
        /// <summary>Every enabled spot, for finding the one the player stands next to.</summary>
        public static readonly List<HidingSpot> All = new List<HidingSpot>();

        public Transform seat;
        public Transform exit;
        [Tooltip("How far the eyes go down behind it, metres.")]
        public float eyeDrop = 0.85f;
        [Tooltip("Localization key of the hint that tells the player to hide here.")]
        public string hintKey = "hint.hideNow";

        [Header("Closet")]
        [Tooltip("Hinges of the doors: the first swings out from the left edge, the second from the right.")]
        public Transform[] doors = new Transform[0];
        public float doorSwing = 80f;
        [Tooltip("Where an eye outside looks in through the gap (empty: the spot is not shut).")]
        public Transform peek;

        Collider body;
        Quaternion[] closed;

        /// <summary>The player is shut inside rather than behind it.</summary>
        public bool Enclosed => peek != null;

        void Awake() => body = GetComponentInChildren<Collider>();

        void OnEnable() => All.Add(this);

        void OnDisable() => All.Remove(this);

        /// <summary>The point of the cover nearest to a point (its middle when it has no collider).</summary>
        public Vector3 ClosestPoint(Vector3 point) => body != null && body.enabled ? body.ClosestPoint(point) : transform.position;

        /// <summary>Doors 0 (shut) .. 1 (wide open).</summary>
        public void SetOpen(float amount)
        {
            if (closed == null || closed.Length != doors.Length)
            {
                // Taken the first time, while they are still shut (the doors may be set after this was added).
                closed = new Quaternion[doors.Length];
                for (int i = 0; i < doors.Length; i++) closed[i] = doors[i] != null ? doors[i].localRotation : Quaternion.identity;
            }
            for (int i = 0; i < doors.Length; i++)
                if (doors[i] != null) doors[i].localRotation = closed[i] * Quaternion.Euler(0f, (i % 2 == 0 ? -1f : 1f) * doorSwing * amount, 0f);
        }
    }
}
