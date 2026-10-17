using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BabaikaSystem
{
    /// <summary>
    /// Procedural legs of the <see cref="Babaika"/>. Every leg looks for the surface nearest to where it would like
    /// to be (floor, wall, ceiling, furniture: any collider on <see cref="surfaces"/>), plants its claws there and
    /// steps on once the body has moved away. Neighbouring legs never step together, so the six legs alternate
    /// like two tripods and the head is always held. Legs with nothing in reach hang in the air and grope.
    /// Each leg is a two-bone IK chain: hip and knee point their +Z along the segment and bend around +X.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Babaika))]
    public class BabaikaLegs : MonoBehaviour
    {
        [Tooltip("Found by name when empty: Leg_<id> (hip) > Leg_<id>_Knee > Leg_<id>_Wrist.")]
        public List<Leg> legs = new List<Leg>();

        [Header("Surfaces")]
        [Tooltip("Layers the claws can hold on to.")]
        public LayerMask surfaces = Physics.DefaultRaycastLayers;
        [Tooltip("Share of the full leg length a planted leg may use.")]
        [Range(0.5f, 1f)] public float reachUse = 0.93f;
        [Tooltip("Height of the wrist above the surface; the claws curl down from it.")]
        public float clawLift = 0.075f;
        [Tooltip("Tilts every leg's preferred direction up by this many degrees: holds higher on walls and the ceiling.")]
        public float aimUp = 25f;

        [Header("Steps")]
        public float walkStepDistance = 0.38f;
        public float runStepDistance = 0.75f;
        public float walkStepTime = 0.2f;
        public float runStepTime = 0.1f;
        public float stepHeight = 0.19f;
        [Range(1, 6)] public int maxSteppingLegs = 3;
        [Tooltip("Seconds between restless little steps while standing (min, max).")]
        public Vector2 fidgetInterval = new Vector2(0.7f, 2.2f);

        [Header("Sound")]
        public AudioSource clawSource;
        public AudioClip[] clawClips = new AudioClip[0];
        [Range(0f, 1f)] public float clawVolume = 0.4f;
        public Vector2 clawPitch = new Vector2(1.5f, 1.9f);

        /// <summary>Fired when a leg plants its claws: leg index and the point it holds.</summary>
        public event Action<int, Vector3> Planted;

        [Serializable]
        public class Leg
        {
            public string id;
            public Transform hip;
            public Transform knee;
            public Transform wrist;

            [NonSerialized] public float upper, lower, restReach, seed;
            [NonSerialized] public Vector3 restDirection, restHip, ideal;
            [NonSerialized] public readonly List<int> neighbours = new List<int>();
            [NonSerialized] public Vector3 foot, normal;
            [NonSerialized] public bool gripping;
            [NonSerialized] public bool stepping, fromGrip, toGrip;
            [NonSerialized] public Vector3 from, to, fromNormal, toNormal;
            [NonSerialized] public float t, duration;
            [NonSerialized] public Quaternion hand;
            [NonSerialized] public Hold hold;
            [NonSerialized] public Vector3 pole;
            [NonSerialized] public int poleChoice;

            public float Reach(float use) => (upper + lower) * use;
        }

        public struct Hold
        {
            public bool found;
            public Vector3 point;
            public Vector3 normal;
        }

        static readonly Regex HipName = new Regex(@"^Leg_([LR])(\d+)$");

        Babaika body;
        bool started;
        float fidgetTimer;
        int lastClip = -1;
        readonly List<int> order = new List<int>();

        void Awake()
        {
            body = GetComponent<Babaika>();
            if (legs.Count == 0) FindLegs();
            for (int i = 0; i < legs.Count; i++) Setup(legs[i]);
            LinkNeighbours();
            body.Teleported += Replant;
            fidgetTimer = UnityEngine.Random.Range(fidgetInterval.x, fidgetInterval.y);
        }

        void Start()
        {
            started = true;
            Replant();
        }

        void OnEnable()
        {
            if (started) Replant();
        }

        void OnDestroy()
        {
            if (body != null) body.Teleported -= Replant;
        }

        // ------------------------------------------------------------------ setup

        void FindLegs()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (!HipName.IsMatch(t.name)) continue;
                var knee = t.Find(t.name + "_Knee");
                var wrist = knee != null ? knee.Find(t.name + "_Wrist") : null;
                if (wrist == null) continue;
                legs.Add(new Leg { id = t.name.Substring(4), hip = t, knee = knee, wrist = wrist });
            }
            legs.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        }

        void Setup(Leg leg)
        {
            leg.upper = Vector3.Distance(leg.hip.position, leg.knee.position);
            leg.lower = Vector3.Distance(leg.knee.position, leg.wrist.position);
            Vector3 reach = leg.wrist.position - leg.hip.position;
            leg.restReach = reach.magnitude;
            leg.restDirection = transform.InverseTransformDirection(reach / Mathf.Max(leg.restReach, 1e-4f));
            leg.restHip = transform.InverseTransformPoint(leg.hip.position);
            leg.hand = leg.wrist.rotation;
            leg.seed = UnityEngine.Random.value * 100f;
        }

        /// <summary>Same side front/back, and the leg across: those may not step together.</summary>
        void LinkNeighbours()
        {
            for (int i = 0; i < legs.Count; i++)
            {
                var a = HipName.Match("Leg_" + legs[i].id);
                for (int j = 0; j < legs.Count; j++)
                {
                    if (i == j) continue;
                    var b = HipName.Match("Leg_" + legs[j].id);
                    if (!a.Success || !b.Success) continue;
                    int ai = int.Parse(a.Groups[2].Value), bi = int.Parse(b.Groups[2].Value);
                    bool sameSide = a.Groups[1].Value == b.Groups[1].Value;
                    if ((sameSide && Mathf.Abs(ai - bi) == 1) || (!sameSide && ai == bi)) legs[i].neighbours.Add(j);
                }
            }
        }

        /// <summary>Plants every leg at once around the current place (after a teleport or on start).</summary>
        public void Replant()
        {
            foreach (var leg in legs)
            {
                var hold = FindHold(leg, Vector3.zero);
                leg.gripping = hold.found;
                leg.foot = hold.found ? hold.point : AirPoint(leg, Vector3.zero);
                leg.normal = hold.found ? hold.normal : transform.up;
                leg.stepping = false;
                leg.hand = leg.wrist.rotation;
            }
        }

        // ------------------------------------------------------------------ per frame

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || legs.Count == 0) return;

            float pace = body.runSpeed > body.walkSpeed ? Mathf.InverseLerp(body.walkSpeed, body.runSpeed, body.Speed) : 0f;
            bool moving = body.Speed > 0.05f;
            float stepDistance = Mathf.Lerp(walkStepDistance, runStepDistance, pace);
            float stepTime = Mathf.Lerp(walkStepTime, runStepTime, pace);
            Vector3 velocity = body.Velocity;
            Vector3 lead = moving ? Vector3.ClampMagnitude(velocity * stepTime + velocity.normalized * (stepDistance * 0.45f), stepDistance) : Vector3.zero;

            // Where would each leg hold now, and how badly does it want to get there?
            order.Clear();
            for (int i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                leg.hold = FindHold(leg, lead);
                if (!leg.stepping) order.Add(i);
            }
            order.Sort((a, b) => Urgency(legs[b], moving, stepDistance).CompareTo(Urgency(legs[a], moving, stepDistance)));

            int stepping = 0;
            foreach (var leg in legs) if (leg.stepping) stepping++;
            foreach (int i in order)
            {
                var leg = legs[i];
                float urgency = Urgency(leg, moving, stepDistance);
                if (urgency < 1f || stepping >= maxSteppingLegs || NeighbourStepping(leg)) continue;
                StartStep(leg, leg.hold.found ? leg.hold.point : AirPoint(leg, lead), leg.hold.found ? leg.hold.normal : transform.up, leg.hold.found, stepTime);
                stepping++;
            }

            if (!moving) Fidget(dt, stepTime);

            for (int i = 0; i < legs.Count; i++) Move(i, dt, lead, stepDistance);
            foreach (var leg in legs) Pose(leg, dt);
        }

        /// <summary>Above 1 the leg should step: it is far from its hold, out of reach, or can grab something.</summary>
        float Urgency(Leg leg, bool moving, float stepDistance)
        {
            if (leg.stepping) return 0f;
            float reach = leg.Reach(reachUse);
            float stretch = Vector3.Distance(leg.hip.position, leg.foot) / reach;
            if (leg.gripping && stretch > 1.04f) return 10f + stretch;
            // Something came between the body and the claws (a door slammed, she went round a corner): let go.
            if (leg.gripping && Blocked(SteadyHip(leg), leg.foot)) return 9f;
            if (!leg.hold.found) return 0f;
            if (!leg.gripping) return 5f;
            // How much closer to where the leg wants to be the new hold is. A hold that is merely different
            // (another surface just as good) is not worth a step.
            float gain = Vector3.Distance(leg.foot, leg.ideal) - Vector3.Distance(leg.hold.point, leg.ideal);
            float threshold = moving ? stepDistance : stepDistance * 0.6f;
            return gain / threshold;
        }

        bool NeighbourStepping(Leg leg)
        {
            foreach (int n in leg.neighbours) if (legs[n].stepping) return true;
            return false;
        }

        void StartStep(Leg leg, Vector3 target, Vector3 normal, bool grip, float time)
        {
            leg.stepping = true;
            leg.t = 0f;
            leg.duration = time * UnityEngine.Random.Range(0.85f, 1.15f);
            leg.from = leg.foot;
            leg.fromGrip = leg.gripping;
            leg.fromNormal = leg.gripping ? leg.normal : transform.up;
            leg.gripping = false;
            leg.to = target;
            leg.toNormal = normal;
            leg.toGrip = grip;
        }

        /// <summary>While standing, now and then one leg shifts its grip a little: it never stays quite still.</summary>
        void Fidget(float dt, float stepTime)
        {
            fidgetTimer -= dt;
            if (fidgetTimer > 0f) return;
            fidgetTimer = UnityEngine.Random.Range(fidgetInterval.x, fidgetInterval.y);
            var leg = legs[UnityEngine.Random.Range(0, legs.Count)];
            if (leg.stepping || NeighbourStepping(leg)) return;
            Vector3 jitter = Vector3.ProjectOnPlane(UnityEngine.Random.insideUnitSphere, transform.up) * 0.18f;
            var hold = FindHold(leg, jitter);
            if (hold.found) StartStep(leg, hold.point, hold.normal, true, stepTime * 1.6f);
        }

        void Move(int index, float dt, Vector3 lead, float stepDistance)
        {
            var leg = legs[index];
            if (leg.stepping)
            {
                if (leg.toGrip && leg.hold.found) leg.to = Vector3.Lerp(leg.to, leg.hold.point, 1f - Mathf.Exp(-10f * dt));
                else if (!leg.toGrip) leg.to = AirPoint(leg, lead);

                leg.t += dt / leg.duration;
                float t = Mathf.Clamp01(leg.t);
                float e = t * t * (3f - 2f * t);
                Vector3 lift = leg.fromNormal + leg.toNormal;
                lift = lift.sqrMagnitude > 1e-4f ? lift.normalized : transform.up;
                float height = stepHeight * Mathf.Clamp(Vector3.Distance(leg.from, leg.to) / Mathf.Max(stepDistance, 0.01f) + 0.25f, 0.3f, 1.4f);
                leg.foot = Vector3.Lerp(leg.from, leg.to, e) + lift * (height * Mathf.Sin(t * Mathf.PI));
                leg.normal = Vector3.Slerp(leg.fromNormal, leg.toNormal, e);
                if (t >= 1f)
                {
                    leg.stepping = false;
                    leg.gripping = leg.toGrip;
                    leg.foot = leg.to;
                    leg.normal = leg.toNormal;
                    if (leg.gripping) Plant(index);
                }
            }
            else if (!leg.gripping)
            {
                leg.foot = Vector3.Lerp(leg.foot, AirPoint(leg, lead), 1f - Mathf.Exp(-5f * dt));
                leg.normal = transform.up;
            }
        }

        void Pose(Leg leg, float dt)
        {
            // Knees go up and out like a spider's, unless that puts a leg through a wall or a door: then the first
            // bend that keeps both segments clear (the last good one is kept while it stays clear).
            Vector3 up = transform.up;
            Vector3 outward = Vector3.ProjectOnPlane(leg.hip.position - transform.position, up);
            outward = outward.sqrMagnitude > 1e-6f ? outward.normalized : Vector3.zero;
            poles[0] = up + outward * 0.6f;
            poles[1] = up;
            poles[2] = up - outward * 0.5f;
            poles[3] = outward;
            poles[4] = up * 0.3f - outward;
            poles[5] = outward - up * 0.5f;
            int choice = ClearBend(leg, 0) ? 0 : leg.poleChoice != 0 && ClearBend(leg, leg.poleChoice) ? leg.poleChoice : -1;
            for (int i = 1; choice < 0 && i < poles.Length; i++)
                if (ClearBend(leg, i)) choice = i;
            leg.poleChoice = Mathf.Max(choice, 0);
            Vector3 goalPole = poles[leg.poleChoice];
            leg.pole = leg.pole.sqrMagnitude > 1e-6f ? Vector3.Slerp(leg.pole, goalPole, 1f - Mathf.Exp(-10f * dt)) : goalPole;
            Solve(leg, leg.foot, leg.pole, out Vector3 lowerDirection, out Vector3 bendNormal);

            Quaternion air = Quaternion.LookRotation(lowerDirection, Vector3.Cross(lowerDirection, bendNormal)) * Quaternion.Euler(35f, 0f, 0f);
            Quaternion goal;
            if (leg.stepping)
            {
                float t = Mathf.Clamp01(leg.t);
                Quaternion start = leg.fromGrip ? Grip(lowerDirection, leg.fromNormal, leg) : air;
                Quaternion end = leg.toGrip ? Grip(lowerDirection, leg.toNormal, leg) : air;
                goal = t < 0.5f ? Quaternion.Slerp(start, air, t * 2f) : Quaternion.Slerp(air, end, t * 2f - 1f);
            }
            else
            {
                goal = leg.gripping ? Grip(lowerDirection, leg.normal, leg) : air;
            }
            leg.hand = Quaternion.Slerp(leg.hand, goal, 1f - Mathf.Exp(-18f * dt));
            leg.wrist.rotation = leg.hand;
        }

        /// <summary>Claws flat on the surface, pointing on along the forearm.</summary>
        Quaternion Grip(Vector3 forearm, Vector3 normal, Leg leg)
        {
            Vector3 forward = Vector3.ProjectOnPlane(forearm, normal);
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.ProjectOnPlane(leg.foot - leg.hip.position, normal);
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.ProjectOnPlane(transform.forward, normal);
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.ProjectOnPlane(transform.up, normal);
            return Quaternion.LookRotation(forward.normalized, normal);
        }

        /// <summary>Two-bone IK: the knee goes towards the pole, the hip and knee keep their bend around +X.</summary>
        void Solve(Leg leg, Vector3 target, Vector3 pole, out Vector3 lowerDirection, out Vector3 bendNormal)
        {
            Chain(leg, target, pole, out Vector3 knee, out Vector3 wrist, out Vector3 direction, out Vector3 bend);
            bendNormal = Vector3.Cross(bend, direction).normalized;
            Vector3 upperDirection = (knee - leg.hip.position).normalized;
            lowerDirection = (wrist - knee).normalized;
            leg.hip.rotation = Quaternion.LookRotation(upperDirection, Vector3.Cross(upperDirection, bendNormal));
            leg.knee.rotation = Quaternion.LookRotation(lowerDirection, Vector3.Cross(lowerDirection, bendNormal));
        }

        /// <summary>Where the knee and wrist go for this target and pole (law of cosines).</summary>
        void Chain(Leg leg, Vector3 target, Vector3 pole, out Vector3 knee, out Vector3 wrist, out Vector3 direction, out Vector3 bend)
        {
            Vector3 hip = leg.hip.position;
            Vector3 toTarget = target - hip;
            float a = leg.upper, b = leg.lower;
            float distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(a - b) + 1e-3f, a + b - 1e-3f);
            direction = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : -transform.up;
            bend = Vector3.ProjectOnPlane(pole, direction);
            if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(transform.forward, direction);
            bend.Normalize();
            float cos = Mathf.Clamp((a * a + distance * distance - b * b) / (2f * a * distance), -1f, 1f);
            knee = hip + direction * (a * cos) + bend * (a * Mathf.Sqrt(1f - cos * cos));
            wrist = hip + direction * distance;
        }

        /// <summary>With this pole neither segment goes through anything (the last bit at the claws is not checked).</summary>
        bool ClearBend(Leg leg, int pole)
        {
            Chain(leg, leg.foot, poles[pole], out Vector3 knee, out Vector3 wrist, out _, out _);
            Vector3 claws = wrist - (wrist - knee).normalized * 0.12f;
            return !Blocked(leg.hip.position, knee) && !Blocked(knee, claws);
        }

        /// <summary>Something solid (not the player, not the Babaika itself) lies between two points.</summary>
        bool Blocked(Vector3 from, Vector3 to)
        {
            Vector3 line = to - from;
            float length = line.magnitude;
            if (length < 1e-3f) return false;
            int count = Physics.RaycastNonAlloc(from, line / length, Hits, length, surfaces, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = Hits[i].collider;
                if (c is CharacterController || c.transform.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }

        static readonly RaycastHit[] Hits = new RaycastHit[8];
        readonly Vector3[] poles = new Vector3[6];

        // ------------------------------------------------------------------ surfaces

        static readonly Vector2[] Fan =
        {
            new Vector2(0f, 0f), new Vector2(35f, 0f), new Vector2(-35f, 0f), new Vector2(0f, 35f),
            new Vector2(0f, -35f), new Vector2(0f, -70f), new Vector2(60f, 20f), new Vector2(-60f, 20f),
        };

        /// <summary>The hip where the rest pose has it: holds are searched from here, so the bobbing and
        /// twitching head does not make the legs change their minds.</summary>
        Vector3 SteadyHip(Leg leg) => transform.TransformPoint(leg.restHip);

        /// <summary>The leg's preferred direction: its rest pose, tilted up by <see cref="aimUp"/>.</summary>
        Vector3 Aim(Leg leg)
        {
            Vector3 direction = transform.TransformDirection(leg.restDirection);
            Vector3 side = Vector3.Cross(direction, transform.up);
            return side.sqrMagnitude > 1e-4f ? Quaternion.AngleAxis(aimUp, side.normalized) * direction : direction;
        }

        Vector3 IdealPoint(Leg leg, Vector3 lead) => SteadyHip(leg) + Aim(leg) * leg.restReach + lead;

        /// <summary>The reachable surface point closest to where the leg would like to be.</summary>
        public Hold FindHold(Leg leg, Vector3 lead)
        {
            Vector3 hip = SteadyHip(leg);
            Vector3 ideal = IdealPoint(leg, lead);
            leg.ideal = ideal;
            float reach = leg.Reach(reachUse);
            Vector3 aim = (ideal - hip).normalized;
            Vector3 side = Vector3.Cross(aim, transform.up);
            if (side.sqrMagnitude < 1e-4f) side = transform.right;
            side.Normalize();

            var best = new Hold();
            float bestScore = float.MaxValue;
            foreach (var fan in Fan)
            {
                Vector3 direction = Quaternion.AngleAxis(fan.x, transform.up) * (Quaternion.AngleAxis(fan.y, side) * aim);
                Consider(new Ray(hip, direction), reach + clawLift + 0.2f, leg, hip, ideal, reach, ref best, ref bestScore);
            }
            // Straight down onto the floor under the ideal point; that one may lie beyond a wall or a door, so it
            // only counts when the hip can see it.
            Consider(new Ray(ideal + transform.up * 0.9f, -transform.up), 2.2f, leg, hip, ideal, reach, ref best, ref bestScore, true);
            Consider(new Ray(hip, -transform.up), reach + clawLift, leg, hip, ideal, reach, ref best, ref bestScore);
            if (best.found) return best;

            // Nothing near the rest pose (under a ceiling, in a corner): grab whatever is in reach.
            foreach (var direction in AllAround)
            {
                Consider(new Ray(hip, transform.TransformDirection(direction)), reach + clawLift, leg, hip, ideal, reach, ref best, ref bestScore);
            }
            return best;
        }

        static readonly Vector3[] AllAround = BuildAllAround();

        static Vector3[] BuildAllAround()
        {
            var list = new List<Vector3>();
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                        if (x != 0 || y != 0 || z != 0) list.Add(new Vector3(x, y, z).normalized);
            return list.ToArray();
        }

        void Consider(Ray ray, float length, Leg leg, Vector3 hip, Vector3 ideal, float reach, ref Hold best, ref float bestScore, bool checkSight = false)
        {
            if (!Physics.Raycast(ray, out var hit, length, surfaces, QueryTriggerInteraction.Ignore)) return;
            if (hit.collider is CharacterController || hit.collider.transform.IsChildOf(transform)) return;
            Vector3 point = hit.point + hit.normal * clawLift;
            if (Vector3.Distance(hip, point) > reach) return;
            if (checkSight && Blocked(hip, point)) return;
            float score = Vector3.Distance(point, ideal);
            if (leg.gripping && Vector3.Distance(point, leg.foot) < 0.06f) score -= 0.05f;
            if (score >= bestScore) return;
            bestScore = score;
            best = new Hold { found = true, point = point, normal = hit.normal };
        }

        /// <summary>Where a leg with nothing to hold hangs: its rest direction, groping slowly.</summary>
        Vector3 AirPoint(Leg leg, Vector3 lead)
        {
            float time = Time.time;
            var grope = new Vector3(Mathf.Sin(time * 1.7f + leg.seed), Mathf.Sin(time * 2.3f + leg.seed * 1.3f), Mathf.Sin(time * 1.1f + leg.seed * 0.7f)) * 0.1f;
            return SteadyHip(leg) + Aim(leg) * (leg.restReach * 0.95f) + lead * 0.5f + grope;
        }

        void Plant(int index)
        {
            Planted?.Invoke(index, legs[index].foot);
            if (clawSource == null || clawClips.Length == 0) return;
            int clip = UnityEngine.Random.Range(0, clawClips.Length);
            if (clawClips.Length > 1 && clip == lastClip) clip = (clip + 1) % clawClips.Length;
            lastClip = clip;
            clawSource.pitch = UnityEngine.Random.Range(clawPitch.x, clawPitch.y);
            clawSource.PlayOneShot(clawClips[clip], clawVolume);
        }

        void OnDrawGizmosSelected()
        {
            foreach (var leg in legs)
            {
                if (leg.hip == null) continue;
                Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
                if (Application.isPlaying) Gizmos.DrawWireSphere(leg.hip.position, leg.Reach(reachUse));
                Gizmos.color = leg.gripping ? Color.green : Color.red;
                if (Application.isPlaying) Gizmos.DrawSphere(leg.foot, 0.03f);
            }
        }
    }
}
