using System.Collections.Generic;
using UnityEngine;

namespace BabaikaSystem
{
    /// <summary>
    /// Path for a scripted run: the child transforms, in order. The head flies through them on a smooth curve,
    /// so place them where the head should be: about half a metre above the floor, along a wall, under the
    /// ceiling. The legs find their own holds around the head.
    /// </summary>
    public class BabaikaRoute : MonoBehaviour
    {
        [Tooltip("Curve through the points instead of straight lines.")]
        public bool smooth = true;
        [Tooltip("Metres between samples of the curve.")]
        public float spacing = 0.15f;

        public int Count => transform.childCount;

        public Vector3 this[int index] => transform.GetChild(index).position;

        /// <summary>Facing along the route at its first point.</summary>
        public Quaternion StartRotation()
        {
            if (Count < 2) return transform.rotation;
            Vector3 direction = Vector3.ProjectOnPlane(this[1] - this[0], Vector3.up);
            return direction.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(direction, Vector3.up) : transform.rotation;
        }

        /// <summary>World points from <paramref name="from"/> through every route point.</summary>
        public List<Vector3> Sample(Vector3 from)
        {
            var points = new List<Vector3> { from };
            for (int i = 0; i < Count; i++)
            {
                if ((this[i] - points[points.Count - 1]).sqrMagnitude > 0.0025f) points.Add(this[i]);
            }
            if (!smooth || points.Count < 3) return points;

            var samples = new List<Vector3> { points[0] };
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector3 p0 = i > 0 ? points[i - 1] : 2f * points[0] - points[1];
                Vector3 p3 = i + 2 < points.Count ? points[i + 2] : 2f * points[i + 1] - points[i];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(points[i], points[i + 1]) / Mathf.Max(spacing, 0.02f)));
                for (int s = 1; s <= steps; s++) samples.Add(CatmullRom(p0, points[i], points[i + 1], p3, (float)s / steps));
            }
            return samples;
        }

        /// <summary>Centripetal Catmull-Rom: no loops or overshoot at sharp corners.</summary>
        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t1 = Knot(p0, p1);
            float t2 = t1 + Knot(p1, p2);
            float t3 = t2 + Knot(p2, p3);
            float u = Mathf.Lerp(t1, t2, t);
            Vector3 a1 = (t1 - u) / t1 * p0 + u / t1 * p1;
            Vector3 a2 = (t2 - u) / (t2 - t1) * p1 + (u - t1) / (t2 - t1) * p2;
            Vector3 a3 = (t3 - u) / (t3 - t2) * p2 + (u - t2) / (t3 - t2) * p3;
            Vector3 b1 = (t2 - u) / t2 * a1 + u / t2 * a2;
            Vector3 b2 = (t3 - u) / (t3 - t1) * a2 + (u - t1) / (t3 - t1) * a3;
            return (t2 - u) / (t2 - t1) * b1 + (u - t1) / (t2 - t1) * b2;
        }

        static float Knot(Vector3 a, Vector3 b) => Mathf.Max(Mathf.Sqrt(Vector3.Distance(a, b)), 1e-3f);

        void OnDrawGizmos()
        {
            if (Count == 0) return;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
            var samples = Sample(this[0]);
            for (int i = 1; i < samples.Count; i++) Gizmos.DrawLine(samples[i - 1], samples[i]);
            for (int i = 0; i < Count; i++) Gizmos.DrawWireSphere(this[i], i == 0 ? 0.12f : 0.07f);
        }
    }
}
