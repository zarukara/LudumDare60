using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Cover the player can crouch behind with <see cref="PlayerHide"/> (stand close, face it, E). Needs a collider
    /// on this object or a child. <see cref="seat"/> is where the player sits and which way they face,
    /// <see cref="exit"/> where they stand up again.
    /// </summary>
    public class HidingSpot : MonoBehaviour
    {
        /// <summary>Every enabled spot, for finding the one the player stands next to.</summary>
        public static readonly List<HidingSpot> All = new List<HidingSpot>();

        public Transform seat;
        public Transform exit;
        [Tooltip("How far the eyes go down behind it, metres.")]
        public float eyeDrop = 0.85f;

        Collider body;

        void Awake() => body = GetComponentInChildren<Collider>();

        void OnEnable() => All.Add(this);

        void OnDisable() => All.Remove(this);

        /// <summary>The point of the cover nearest to a point (its middle when it has no collider).</summary>
        public Vector3 ClosestPoint(Vector3 point) => body != null && body.enabled ? body.ClosestPoint(point) : transform.position;
    }
}
