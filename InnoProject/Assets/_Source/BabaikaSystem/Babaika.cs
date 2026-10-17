using System;
using System.Collections.Generic;
using UnityEngine;

namespace BabaikaSystem
{
    public enum BabaikaGait
    {
        Stand,
        Walk,
        Run,
    }

    /// <summary>
    /// The Babaika: a flying head that drags itself along on six clawed legs (<see cref="BabaikaLegs"/>).
    /// It has no AI. Scripts tell it where to go with <see cref="WalkTo"/>, <see cref="RunTo"/>, <see cref="Follow"/>
    /// and <see cref="Stand"/>, and the legs find floors, walls, ceilings and furniture by themselves.
    /// The transform is the centre of the head and goes exactly along the path. The model bobs, leans and
    /// twitches on top of it.
    /// </summary>
    [DisallowMultipleComponent]
    public class Babaika : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 1.1f;
        public float runSpeed = 4.2f;
        [Tooltip("How fast it speeds up and slows down, m/s².")]
        public float acceleration = 9f;
        [Tooltip("Degrees per second when turning towards where it goes.")]
        public float turnSpeed = 320f;

        [Header("Body")]
        [Tooltip("The visible model under this transform. It bobs and leans while the root follows the path.")]
        public Transform model;
        [Tooltip("Lower jaw, opened around its X axis.")]
        public Transform jaw;
        public float hoverBob = 0.035f;
        [Tooltip("Forward lean at full running speed, degrees.")]
        public float runLean = 16f;
        [Tooltip("Seconds between sudden head twitches while standing (min, max).")]
        public Vector2 twitchInterval = new Vector2(1.2f, 3.5f);
        public float jawStand = 8f;
        public float jawWalk = 16f;
        public float jawRun = 34f;
        public float jawScream = 58f;

        public BabaikaGait Gait { get; private set; } = BabaikaGait.Stand;
        public Vector3 Velocity { get; private set; }
        public float Speed => speed;
        public bool IsMoving => path.Count > 0;

        /// <summary>Fired when the end of the path or route is reached.</summary>
        public event Action Arrived;
        /// <summary>Fired after <see cref="Teleport"/>, so the legs can grab new surfaces at once.</summary>
        public event Action Teleported;

        readonly List<Vector3> path = new List<Vector3>();
        readonly List<float> remainingFrom = new List<float>();
        int pathIndex;
        float speed, cruiseOverride, screamTime;
        Vector3? lookTarget;
        bool driven;
        Vector3 drivenPosition;

        Vector3 modelPosition;
        Quaternion modelRotation = Quaternion.identity;
        Quaternion jawRotation = Quaternion.identity;
        float phase, lean, jawAngle, twitchTimer;
        Vector3 twitch, twitchGoal;

        void Awake()
        {
            if (model != null)
            {
                modelPosition = model.localPosition;
                modelRotation = model.localRotation;
            }
            if (jaw != null) jawRotation = jaw.localRotation;
            twitchTimer = UnityEngine.Random.Range(twitchInterval.x, twitchInterval.y);
        }

        // ------------------------------------------------------------------ commands

        /// <summary>Stops where it is (slowing down quickly) and stays there.</summary>
        public void Stand()
        {
            path.Clear();
            remainingFrom.Clear();
            driven = false;
            cruiseOverride = 0f;
            Gait = BabaikaGait.Stand;
        }

        public void WalkTo(Vector3 point) => GoTo(point, BabaikaGait.Walk);

        public void RunTo(Vector3 point) => GoTo(point, BabaikaGait.Run);

        public void GoTo(Vector3 point, BabaikaGait gait) => SetPath(new[] { transform.position, point }, gait);

        /// <summary>Goes to a point at exactly this speed (m/s), e.g. a slow descent.</summary>
        public void GoTo(Vector3 point, BabaikaGait gait, float metresPerSecond)
        {
            SetPath(new[] { transform.position, point }, gait);
            cruiseOverride = metresPerSecond;
        }

        /// <summary>
        /// Another script steers it: the head is put here this frame (call it every frame, e.g. from a chase).
        /// The legs, lean and jaw follow the resulting speed. <see cref="Stand"/> hands control back.
        /// </summary>
        public void DriveTo(Vector3 position, BabaikaGait gait = BabaikaGait.Run)
        {
            path.Clear();
            remainingFrom.Clear();
            driven = true;
            drivenPosition = position;
            Gait = gait;
        }

        /// <summary>Opens the jaw all the way and shakes the head for a while.</summary>
        public void Scream(float seconds) => screamTime = Mathf.Max(screamTime, seconds);

        /// <summary>Goes through the route's points on a smooth curve, starting from where it is now.</summary>
        public void Follow(BabaikaRoute route, BabaikaGait gait)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            SetPath(route.Sample(transform.position), gait);
        }

        /// <summary>Goes through world points in order (straight lines between them).</summary>
        public void Follow(IList<Vector3> points, BabaikaGait gait)
        {
            var list = new List<Vector3> { transform.position };
            list.AddRange(points);
            SetPath(list, gait);
        }

        /// <summary>Moves instantly; the legs grab surfaces around the new place.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Stand();
            speed = 0f;
            Velocity = Vector3.zero;
            transform.SetPositionAndRotation(position, rotation);
            Teleported?.Invoke();
        }

        /// <summary>Turns to face a point while standing (for example the player). Moving overrides it.</summary>
        public void LookAt(Vector3 point) => lookTarget = point;

        public void StopLooking() => lookTarget = null;

        void SetPath(IList<Vector3> points, BabaikaGait gait)
        {
            path.Clear();
            remainingFrom.Clear();
            driven = false;
            cruiseOverride = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                if ((points[i] - (path.Count > 0 ? path[path.Count - 1] : points[0])).sqrMagnitude > 1e-6f) path.Add(points[i]);
            }
            float total = 0f;
            for (int i = path.Count - 1; i >= 0; i--)
            {
                remainingFrom.Insert(0, total);
                if (i > 0) total += Vector3.Distance(path[i - 1], path[i]);
            }
            pathIndex = 0;
            Gait = path.Count > 0 ? (gait == BabaikaGait.Stand ? BabaikaGait.Walk : gait) : BabaikaGait.Stand;
        }

        // ------------------------------------------------------------------ motion

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 start = transform.position;
            if (driven)
            {
                transform.position = drivenPosition;
                Velocity = (drivenPosition - start) / dt;
                speed = Mathf.Lerp(speed, Velocity.magnitude, 1f - Mathf.Exp(-10f * dt));
                Face(drivenPosition, dt);
                AnimateBody(dt);
                return;
            }

            Vector3 position = start;
            float target = 0f;
            if (pathIndex < path.Count)
            {
                float remaining = Vector3.Distance(position, path[pathIndex]) + remainingFrom[pathIndex];
                float cruise = cruiseOverride > 0f ? cruiseOverride : Gait == BabaikaGait.Run ? runSpeed : walkSpeed;
                target = Mathf.Min(cruise, Mathf.Sqrt(2f * acceleration * remaining) + 0.05f);
            }
            speed = Mathf.MoveTowards(speed, target, acceleration * dt);

            float step = speed * dt;
            while (step > 0f && pathIndex < path.Count)
            {
                Vector3 to = path[pathIndex] - position;
                float distance = to.magnitude;
                if (distance <= step)
                {
                    position = path[pathIndex];
                    step -= distance;
                    pathIndex++;
                }
                else
                {
                    position += to / distance * step;
                    step = 0f;
                }
            }
            transform.position = position;
            Velocity = (position - start) / dt;

            if (path.Count > 0 && pathIndex >= path.Count)
            {
                Stand();
                speed = 0f;
                Arrived?.Invoke();
            }

            Face(position, dt);
            AnimateBody(dt);
        }

        /// <summary>Turns towards where it goes, or to the look target while it stands or is steered by a script.</summary>
        void Face(Vector3 position, float dt)
        {
            bool look = lookTarget.HasValue && (driven || Velocity.sqrMagnitude <= 0.04f);
            Vector3 facing = look ? lookTarget.Value - position : Velocity.sqrMagnitude > 0.04f ? Velocity : Vector3.zero;
            facing = Vector3.ProjectOnPlane(facing, Vector3.up);
            if (facing.sqrMagnitude < 1e-4f) return;
            var goal = Quaternion.LookRotation(facing.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeed * dt);
        }

        void AnimateBody(float dt)
        {
            float pace = runSpeed > 0f ? Mathf.Clamp01(speed / runSpeed) : 0f;
            phase += dt * Mathf.Lerp(0.9f, 6.5f, pace);

            if (model != null)
            {
                Vector3 local = transform.InverseTransformDirection(Velocity);
                float leanGoal = runSpeed > 0f ? Mathf.Clamp(local.z / runSpeed, -1f, 1f) * runLean : 0f;
                lean = Mathf.Lerp(lean, leanGoal, 1f - Mathf.Exp(-6f * dt));

                twitchTimer -= dt;
                if (twitchTimer <= 0f)
                {
                    twitchTimer = UnityEngine.Random.Range(twitchInterval.x, twitchInterval.y) * (Gait == BabaikaGait.Stand ? 1f : 2.5f);
                    twitchGoal = new Vector3(UnityEngine.Random.Range(-14f, 14f), UnityEngine.Random.Range(-25f, 25f), UnityEngine.Random.Range(-28f, 28f));
                }
                twitch = Vector3.Lerp(twitch, twitchGoal, 1f - Mathf.Exp(-28f * dt));
                twitchGoal = Vector3.Lerp(twitchGoal, Vector3.zero, 1f - Mathf.Exp(-1.6f * dt));

                float bob = Mathf.Sin(phase * Mathf.PI * 2f) * hoverBob * Mathf.Lerp(1f, 1.8f, pace);
                float sway = Mathf.Sin(phase * Mathf.PI) * Mathf.Lerp(2f, 6f, pace);
                Vector3 fit = screamTime > 0f ? UnityEngine.Random.insideUnitSphere * 7f : Vector3.zero;
                model.localPosition = modelPosition + Vector3.up * bob;
                model.localRotation = modelRotation * Quaternion.Euler(lean + twitch.x + fit.x, twitch.y + fit.y, sway + twitch.z + fit.z);
            }
            screamTime = Mathf.Max(0f, screamTime - dt);

            if (jaw != null)
            {
                float open = screamTime > 0f ? jawScream : Gait == BabaikaGait.Run ? jawRun : Gait == BabaikaGait.Walk ? jawWalk : jawStand;
                open += Mathf.Sin(Time.time * 23f) * 1.5f + Mathf.Sin(Time.time * 7f) * 3f;
                jawAngle = Mathf.Lerp(jawAngle, open, 1f - Mathf.Exp(-6f * dt));
                jaw.localRotation = jawRotation * Quaternion.Euler(jawAngle, 0f, 0f);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (path.Count == 0) return;
            Gizmos.color = Gait == BabaikaGait.Run ? Color.red : Color.yellow;
            Vector3 previous = transform.position;
            for (int i = pathIndex; i < path.Count; i++)
            {
                Gizmos.DrawLine(previous, path[i]);
                previous = path[i];
            }
        }
    }
}
