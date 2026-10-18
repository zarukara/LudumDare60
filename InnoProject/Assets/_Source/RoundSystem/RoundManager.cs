using System;
using System.Collections.Generic;
using AnomalySystem;
using LevelSystem;
using LocalizationSystem;
using UnityEngine;
using UnityEngine.Events;

namespace RoundSystem
{
    /// <summary>
    /// The round loop (Exit 8 style). Each round the office is spawned anew and with <see cref="anomalyChance"/> gets
    /// one random anomaly prefab from <see cref="anomalies"/> (none repeats until every one has been shown, mistakes
    /// or not). The player looks around and leaves through one of the
    /// two exits: forward = "no anomaly", back = "there is an anomaly". A right call adds 1 to the progress, a wrong
    /// one resets it to 0 (or takes <see cref="mistakePenalty"/> off). After <see cref="roundsToWin"/> right calls
    /// in a row the win event fires and the corridor leads to the exit.
    ///
    /// Other systems pass the player's decision with <see cref="SubmitDecision"/> and listen to the events. In this
    /// level the exits are the stage lines: crossing one rebuilds the stage around the player and moves them onto
    /// the entry line of the new one (see <see cref="StageLayout"/>). Stages are built relative to this object;
    /// keep it unrotated.
    /// </summary>
    public class RoundManager : MonoBehaviour
    {
        [Tooltip("Player root with a CharacterController (any controller that turns the root for yaw works).")]
        public Transform player;
        public OfficePalette palette;
        public GameObject roomPrefab;
        [Tooltip("Anomaly prefabs (the library in Assets/_Presentation/Anomalies follows that folder by itself).")]
        public AnomalyLibrary anomalies;

        [Header("Rules")]
        [Min(1)] public int roundsToWin = 8;
        [Tooltip("A wrong call sets the progress to 0. Off: takes Mistake Penalty off instead.")]
        public bool resetOnMistake = true;
        [Min(1)] public int mistakePenalty = 3;
        [Range(0f, 1f)] public float anomalyChance = 0.7f;
        [Tooltip("With no progress yet the office is always normal, so the player can learn it.")]
        public bool normalRoomAtZero;
        [Tooltip("Chance that the corridor to the office takes one sideways jog (two more turns).")]
        [Range(0f, 1f)] public float corridorSideStepChance;
        [Tooltip("0 = different every run.")]
        public int seed;
        [Tooltip("Debug: every round that may have an anomaly gets this one (its name).")]
        public string forceAnomaly;

        [Header("Events")]
        public UnityEvent<bool> onDecision = new UnityEvent<bool>();
        public UnityEvent onWin = new UnityEvent();

        /// <summary>Right calls in a row (0 .. roundsToWin).</summary>
        public int Progress { get; private set; }
        public Anomaly CurrentAnomaly { get; private set; }
        public bool HasAnomaly => CurrentAnomaly != null;
        public bool IsFinishStage { get; private set; }
        public bool Won { get; private set; }
        /// <summary>The player has seen into this round's office (from inside or through the open door).</summary>
        public bool SawOffice { get; private set; }
        /// <summary>The round's anomaly has been clearly in view (on screen, not hidden behind anything).</summary>
        public bool SawAnomaly { get; private set; }
        public Stage CurrentStage { get; private set; }
        public float RunTime => (Won ? winTime : Time.time) - startTime;

        /// <summary>A round began: progress so far and whether the room has an anomaly.</summary>
        public event Action<int, bool> RoundStarted;
        /// <summary>The player decided: was it right, and the progress after it.</summary>
        public event Action<bool, int> DecisionMade;
        /// <summary>The run was reset to the first round.</summary>
        public event Action RunStarted;
        public event Action GameWon;
        /// <summary>The player walked down the stairs at the very end.</summary>
        public event Action ExitReached;
        /// <summary>
        /// The player was carried over a stage line (after the new stage, if any, was built). Other things that should
        /// stay where they were relative to the player (a chaser) are moved with <see cref="StageShift.Map(Vector3)"/>.
        /// </summary>
        public event Action<StageShift> LineCrossed;

        /// <summary>Level shown on the signs (progress + 1).</summary>
        public int Level => Progress + 1;
        /// <summary>
        /// Scripted retreat (a chase): every stage line, either way, takes one level off and the stages have no
        /// anomalies. See <see cref="BeginRetreat"/>.
        /// </summary>
        public bool Retreating { get; private set; }
        /// <summary>
        /// Rules from other systems, by progress: no anomaly in that office (a scripted event waits there or fills it).
        /// One rule saying yes is enough.
        /// </summary>
        public readonly List<Predicate<int>> AnomalyBans = new List<Predicate<int>>();
        /// <summary>
        /// Rules from other systems, by progress: either way out of that office is right (its scene counts as an
        /// anomaly or as decoration, the player's choice). Turning back still needs a look into the office first.
        /// </summary>
        public readonly List<Predicate<int>> FreeChoices = new List<Predicate<int>>();

        const float LineHalfWidth = 1.2f;
        const float SightSeconds = 0.25f, SightRange = 14f;
        // Points in the office (office space) that show it has been looked into.
        static readonly Vector3[] OfficeSamples = { new Vector3(4f, 1.2f, 3f), new Vector3(4f, 1.2f, 6f), new Vector3(4f, 1.2f, 9f) };

        System.Random rng;
        readonly List<string> recent = new List<string>();
        readonly List<Vector3> anomalySamples = new List<Vector3>();
        Vector3 lastPosition;
        float startTime, winTime, anomalyInView;
        bool exitReached;

        void Start() => Restart();

        // Created on first use: a script reload in Play Mode drops it, and the loop must survive that.
        System.Random Rng => rng ??= new System.Random(seed != 0 ? seed : Environment.TickCount);

        /// <summary>Back to round one with no progress.</summary>
        public void Restart()
        {
            Progress = 0;
            Won = false;
            Retreating = false;
            exitReached = false;
            startTime = Time.time;
            recent.Clear();
            SetPlayerControl(true);
            MoveToStart();
            RunStarted?.Invoke();
            StartRound();
        }

        /// <summary>Back to the first level without restarting the run: the clock keeps going, no hints again.</summary>
        public void ResetProgress()
        {
            Progress = 0;
            Retreating = false;
            MoveToStart();
            StartRound();
        }

        /// <summary>Straight to this progress, at the start of a fresh stage (scripted jumps, debugging a level).</summary>
        public void JumpTo(int progress)
        {
            Progress = Mathf.Clamp(progress, 0, roundsToWin - 1);
            Retreating = false;
            MoveToStart();
            StartRound();
        }

        /// <summary>From now on every stage line takes one level off (down to the first) and stages stay normal.</summary>
        public void BeginRetreat() => Retreating = true;

        /// <summary>Back to the normal rules from the current stage on.</summary>
        public void EndRetreat() => Retreating = false;

        /// <summary>Just past the entry line, walking on.</summary>
        void MoveToStart() =>
            MovePlayer(transform.position + StageLayout.EntryLine + StageLayout.Forward * 0.6f + Vector3.up * 0.05f,
                Quaternion.LookRotation(StageLayout.Forward));

        /// <summary>
        /// The player's choice: true = "there is an anomaly" (went back), false = "no anomaly" (went on).
        /// Checks it, updates the progress, fires the events and starts the next round. Returns whether it was right.
        /// </summary>
        public bool SubmitDecision(bool reportedAnomaly)
        {
            if (Won) return false;
            bool right = reportedAnomaly == HasAnomaly || FreeChoices.Exists(rule => rule(Progress));
            Progress = right ? Progress + 1 : resetOnMistake ? 0 : Mathf.Max(0, Progress - mistakePenalty);
            DecisionMade?.Invoke(right, Progress);
            onDecision.Invoke(right);

            if (Progress >= roundsToWin)
            {
                Won = true;
                winTime = Time.time;
                GameWon?.Invoke();
                onWin.Invoke();
            }
            StartRound();
            return right;
        }

        void StartRound()
        {
            // The previous anomaly goes away with its office.
            AnomalyDirector.Remove(CurrentAnomaly);
            if (CurrentStage != null) Destroy(CurrentStage.gameObject);
            CurrentAnomaly = null;
            IsFinishStage = Won;
            SawOffice = false;
            SawAnomaly = false;
            anomalyInView = 0f;
            anomalySamples.Clear();

            var layout = StageLayout.Generate(Rng, corridorSideStepChance);
            string sign = Won ? Localization.Get("sign.exit") : Localization.Format("sign.round", Progress + 1);
            CurrentStage = StageBuilder.Build(transform, palette, roomPrefab, layout, Rng, sign, Won);
            bool mayHaveAnomaly = !Won && !Retreating && anomalies != null && !(normalRoomAtZero && Progress == 0)
                                  && !AnomalyBans.Exists(rule => rule(Progress));
            if (mayHaveAnomaly && (!string.IsNullOrEmpty(forceAnomaly) || Rng.NextDouble() < anomalyChance))
                CurrentAnomaly = AnomalyDirector.Spawn(anomalies.prefabs, CurrentStage.Room.transform, Rng, recent, forceAnomaly);
            if (CurrentAnomaly != null) CollectSamples(CurrentAnomaly.transform);

            Physics.SyncTransforms();
            lastPosition = player.position - transform.position;
            RoundStarted?.Invoke(Progress, HasAnomaly);
        }

        void LateUpdate()
        {
            if (CurrentStage == null || player == null || exitReached) return;
            Vector3 pos = player.position - transform.position;
            Vector3 entry = CurrentStage.EntryLine, exit = CurrentStage.ExitLine;
            UpdateSight();

            if (Along(lastPosition, entry) >= 0f && Along(pos, entry) < 0f && Across(pos, entry) < LineHalfWidth)
            {
                // Walked back over the entry line. Turn the player around onto the entry line; the seam looks the
                // same both ways, so they just keep walking. It is the "anomaly" answer only once they have really
                // looked: the anomaly itself was in view, or (with no anomaly) the office was. Otherwise it just
                // leads into the same corridor again, so rooms cannot be skipped by guessing.
                var mirrored = 2f * entry - pos;
                MovePlayer(transform.position + new Vector3(mirrored.x, pos.y, mirrored.z), Quaternion.Euler(0f, 180f, 0f) * player.rotation);
                if (Retreating) StepBack();
                else if (!IsFinishStage && (HasAnomaly ? SawAnomaly : SawOffice)) SubmitDecision(true);
                LineCrossed?.Invoke(new StageShift(true, transform.position + entry, Vector3.zero));
            }
            else if (!IsFinishStage && Along(lastPosition, exit) < 0f && Along(pos, exit) >= 0f && Across(pos, exit) < LineHalfWidth)
            {
                // Went on over the exit line: "no anomaly". Same spot relative to the entry line.
                MovePlayer(transform.position + entry + (pos - exit), player.rotation);
                if (Retreating) StepBack();
                else SubmitDecision(false);
                LineCrossed?.Invoke(new StageShift(false, Vector3.zero, entry - exit));
            }
            else if (IsFinishStage && CurrentStage.FinishZone.Contains(pos))
            {
                exitReached = true;
                SetPlayerControl(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ExitReached?.Invoke();
            }

            lastPosition = player.position - transform.position;
        }

        /// <summary>Retreat: one level down, a fresh normal stage.</summary>
        void StepBack()
        {
            Progress = Mathf.Max(0, Progress - 1);
            StartRound();
        }

        /// <summary>How far past a stage line a point is, in the walking direction.</summary>
        static float Along(Vector3 point, Vector3 line) => Vector3.Dot(point - line, StageLayout.Forward);

        /// <summary>Sideways distance from the middle of a stage line.</summary>
        static float Across(Vector3 point, Vector3 line) =>
            Mathf.Abs(Vector3.Dot(point - line, Vector3.Cross(Vector3.up, StageLayout.Forward)));

        /// <summary>A step past the door is enough: the whole office is in view from there.</summary>
        bool InsideOffice(Vector3 worldPosition)
        {
            var local = CurrentStage.Room.transform.InverseTransformPoint(worldPosition);
            return local.x > 0.2f && local.x < OfficeRoomBuilder.Width - 0.2f && local.z > 0.6f && local.z < OfficeRoomBuilder.Length - 0.2f;
        }

        /// <summary>Notes whether the office and the anomaly have been seen this round.</summary>
        void UpdateSight()
        {
            var cam = Camera.main;
            if (cam == null || CurrentStage.Room == null) return;
            var office = CurrentStage.Room.transform;
            if (!SawOffice)
            {
                SawOffice = InsideOffice(player.position);
                foreach (var sample in OfficeSamples)
                    SawOffice |= InView(cam, office.TransformPoint(sample), null);
            }
            if (CurrentAnomaly == null || SawAnomaly) return;
            foreach (var sample in anomalySamples)
            {
                if (!InView(cam, sample, CurrentAnomaly.transform)) continue;
                anomalyInView += Time.deltaTime;
                SawAnomaly = anomalyInView >= SightSeconds;
                return;
            }
        }

        /// <summary>Middle, top and sides of the anomaly: seeing any of them counts.</summary>
        void CollectSamples(Transform anomaly)
        {
            var renderers = anomaly.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            var e = b.extents * 0.7f;
            anomalySamples.AddRange(new[]
            {
                b.center, b.center + Vector3.up * e.y, b.center + Vector3.right * e.x, b.center - Vector3.right * e.x,
                b.center + Vector3.forward * e.z, b.center - Vector3.forward * e.z,
            });
        }

        /// <summary>On screen, not too far, and nothing solid (walls, doors, desks, partitions) in between.</summary>
        bool InView(Camera cam, Vector3 point, Transform target)
        {
            var viewport = cam.WorldToViewportPoint(point);
            if (viewport.z < 0.1f || viewport.z > SightRange || viewport.x < 0.03f || viewport.x > 0.97f || viewport.y < 0.03f || viewport.y > 0.97f)
                return false;
            Vector3 from = cam.transform.position, to = point - from;
            float distance = to.magnitude;
            foreach (var hit in Physics.RaycastAll(from, to / distance, distance - 0.05f, ~0, QueryTriggerInteraction.Ignore))
            {
                var t = hit.collider.transform;
                if (t.IsChildOf(player) || (target != null && t.IsChildOf(target))) continue;
                return false;
            }
            return true;
        }

        /// <summary>
        /// How a stage-line crossing moved the player: walking back turns the world half around the entry line,
        /// walking on shifts it from the exit line to the entry line.
        /// </summary>
        public readonly struct StageShift
        {
            public readonly bool Back;
            readonly Vector3 pivot, offset;

            public StageShift(bool back, Vector3 pivot, Vector3 offset)
            {
                Back = back;
                this.pivot = pivot;
                this.offset = offset;
            }

            public Vector3 Map(Vector3 world) => Back ? new Vector3(2f * pivot.x - world.x, world.y, 2f * pivot.z - world.z) : world + offset;

            public Quaternion Map(Quaternion rotation) => Back ? Quaternion.Euler(0f, 180f, 0f) * rotation : rotation;
        }

        void MovePlayer(Vector3 position, Quaternion rotation)
        {
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.SetPositionAndRotation(position, rotation);
            if (controller != null) controller.enabled = true;
        }

        /// <summary>Switches the player's scripts (movement, look) on or off.</summary>
        void SetPlayerControl(bool on)
        {
            foreach (var behaviour in player.GetComponents<MonoBehaviour>())
                behaviour.enabled = on;
        }
    }
}
