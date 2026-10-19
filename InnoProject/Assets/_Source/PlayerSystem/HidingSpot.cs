using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem
{

    public class HidingSpot : MonoBehaviour
    {

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

        public bool Enclosed => peek != null;

        void Awake() => body = GetComponentInChildren<Collider>();

        void OnEnable() => All.Add(this);

        void OnDisable() => All.Remove(this);

        public Vector3 ClosestPoint(Vector3 point) => body != null && body.enabled ? body.ClosestPoint(point) : transform.position;

        public void SetOpen(float amount)
        {
            if (closed == null || closed.Length != doors.Length)
            {

                closed = new Quaternion[doors.Length];
                for (int i = 0; i < doors.Length; i++) closed[i] = doors[i] != null ? doors[i].localRotation : Quaternion.identity;
            }
            for (int i = 0; i < doors.Length; i++)
                if (doors[i] != null) doors[i].localRotation = closed[i] * Quaternion.Euler(0f, (i % 2 == 0 ? -1f : 1f) * doorSwing * amount, 0f);
        }
    }
}
