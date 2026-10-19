using System;
using System.Collections.Generic;
using AnomalySystem;
using LevelSystem;
using LocalizationSystem;
using UnityEngine;
using UnityEngine.Events;

namespace RoundSystem
{

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

        public int Progress { get; private set; }
        public Anomaly CurrentAnomaly { get; private set; }
        public bool HasAnomaly => CurrentAnomaly != null;
        public bool IsFinishStage { get; private set; }
        public bool Won { get; private set; }

        public bool SawOffice { get; private set; }

        public bool SawAnomaly { get; private set; }
        public Stage CurrentStage { get; private set; }
        public float RunTime => (Won ? winTime : Time.time) - startTime;

        public event Action<int, bool> RoundStarted;

        public event Action<bool, int> DecisionMade;

        public event Action RunStarted;
        public event Action GameWon;

        public event Action ExitReached;

        public event Action<StageShift> LineCrossed;

        public int Level => Progress + 1;

        public bool Retreating { get; private set; }

        public readonly List<Predicate<int>> AnomalyBans = new List<Predicate<int>>();

        public readonly List<Predicate<int>> FreeChoices = new List<Predicate<int>>();

        const float LineHalfWidth = 1.2f;
        const float SightSeconds = 0.25f, SightRange = 14f;

        static readonly Vector3[] OfficeSamples = { new Vector3(4f, 1.2f, 3f), new Vector3(4f, 1.2f, 6f), new Vector3(4f, 1.2f, 9f) };

        System.Random rng;
        readonly List<string> recent = new List<string>();
        readonly List<Vector3> anomalySamples = new List<Vector3>();
        Vector3 lastPosition;
        float startTime, winTime, anomalyInView;
        bool exitReached;

        void Start() => Restart();

        System.Random Rng => rng ??= new System.Random(seed != 0 ? seed : Environment.TickCount);

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

        public void ResetProgress()
        {
            Progress = 0;
            Retreating = false;
            MoveToStart();
            StartRound();
        }

        public void JumpTo(int progress)
        {
            Progress = Mathf.Clamp(progress, 0, roundsToWin - 1);
            Retreating = false;
            MoveToStart();
            StartRound();
        }

        public void BeginRetreat() => Retreating = true;

        public void EndRetreat() => Retreating = false;

        void MoveToStart() =>
            MovePlayer(transform.position + StageLayout.EntryLine + StageLayout.Forward * 0.6f + Vector3.up * 0.05f,
                Quaternion.LookRotation(StageLayout.Forward));

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

                var mirrored = 2f * entry - pos;
                MovePlayer(transform.position + new Vector3(mirrored.x, pos.y, mirrored.z), Quaternion.Euler(0f, 180f, 0f) * player.rotation);
                if (Retreating) StepBack();
                else if (!IsFinishStage && (HasAnomaly ? SawAnomaly : SawOffice)) SubmitDecision(true);
                LineCrossed?.Invoke(new StageShift(true, transform.position + entry, Vector3.zero));
            }
            else if (!IsFinishStage && Along(lastPosition, exit) < 0f && Along(pos, exit) >= 0f && Across(pos, exit) < LineHalfWidth)
            {

                MovePlayer(transform.position + entry + (pos - exit), player.rotation);
                if (Retreating) StepBack();
                else SubmitDecision(false);
                LineCrossed?.Invoke(new StageShift(false, Vector3.zero, entry - exit));
            }
            else if (IsFinishStage && CurrentStage.FinishZone.Contains(pos))
            {
                ReachExit();
            }

            lastPosition = player.position - transform.position;
        }

        public void ReachExit()
        {
            if (exitReached) return;
            exitReached = true;
            SetPlayerControl(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ExitReached?.Invoke();
        }

        void StepBack()
        {
            Progress = Mathf.Max(0, Progress - 1);
            StartRound();
        }

        static float Along(Vector3 point, Vector3 line) => Vector3.Dot(point - line, StageLayout.Forward);

        static float Across(Vector3 point, Vector3 line) =>
            Mathf.Abs(Vector3.Dot(point - line, Vector3.Cross(Vector3.up, StageLayout.Forward)));

        bool InsideOffice(Vector3 worldPosition)
        {
            var local = CurrentStage.Room.transform.InverseTransformPoint(worldPosition);
            return local.x > 0.2f && local.x < OfficeRoomBuilder.Width - 0.2f && local.z > 0.6f && local.z < OfficeRoomBuilder.Length - 0.2f;
        }

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

        void SetPlayerControl(bool on)
        {
            foreach (var behaviour in player.GetComponents<MonoBehaviour>())
                behaviour.enabled = on;
        }
    }
}
