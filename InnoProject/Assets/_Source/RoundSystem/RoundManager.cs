using System;
using System.Collections.Generic;
using AnomalySystem;
using LevelSystem;
using UnityEngine;
using UnityEngine.Events;

namespace RoundSystem
{
    /// <summary>
    /// The round loop (Exit 8 style). Each round the office is spawned anew and with <see cref="anomalyChance"/> gets
    /// one random anomaly prefab from <see cref="anomalies"/> (never the same one twice in a row). The player looks
    /// around and leaves through one of the
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
        [Range(0f, 1f)] public float anomalyChance = 0.8f;
        [Tooltip("With no progress yet the office is always normal, so the player can learn it.")]
        public bool normalRoomAtZero;
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
        public Stage CurrentStage { get; private set; }
        public float RunTime => (Won ? winTime : Time.time) - startTime;

        /// <summary>A round began: progress so far and whether the room has an anomaly.</summary>
        public event Action<int, bool> RoundStarted;
        /// <summary>The player decided: was it right, and the progress after it.</summary>
        public event Action<bool, int> DecisionMade;
        /// <summary>The run was reset to the first round.</summary>
        public event Action RunStarted;
        public event Action GameWon;
        /// <summary>The player walked into the daylight at the very end.</summary>
        public event Action ExitReached;

        static readonly Vector3 StartPoint = new Vector3(0f, 0.05f, 0.6f);
        const float LineHalfWidth = 1.2f;

        System.Random rng;
        readonly List<string> recent = new List<string>();
        Vector3 lastPosition;
        float startTime, winTime;
        bool exitReached;

        void Start() => Restart();

        // Created on first use: a script reload in Play Mode drops it, and the loop must survive that.
        System.Random Rng => rng ??= new System.Random(seed != 0 ? seed : Environment.TickCount);

        /// <summary>Back to round one with no progress.</summary>
        public void Restart()
        {
            Progress = 0;
            Won = false;
            exitReached = false;
            startTime = Time.time;
            recent.Clear();
            SetPlayerControl(true);
            MovePlayer(transform.position + StartPoint, Quaternion.identity);
            RunStarted?.Invoke();
            StartRound();
        }

        /// <summary>
        /// The player's choice: true = "there is an anomaly" (went back), false = "no anomaly" (went on).
        /// Checks it, updates the progress, fires the events and starts the next round. Returns whether it was right.
        /// </summary>
        public bool SubmitDecision(bool reportedAnomaly)
        {
            if (Won) return false;
            bool right = reportedAnomaly == HasAnomaly;
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

            var layout = StageLayout.Generate(Rng);
            string sign = Won ? "ВЫХОД" : $"РАУНД {Progress + 1}";
            CurrentStage = StageBuilder.Build(transform, palette, roomPrefab, layout, Rng, sign, Won);
            bool mayHaveAnomaly = !Won && anomalies != null && !(normalRoomAtZero && Progress == 0);
            if (mayHaveAnomaly && (!string.IsNullOrEmpty(forceAnomaly) || Rng.NextDouble() < anomalyChance))
                CurrentAnomaly = AnomalyDirector.Spawn(anomalies.prefabs, CurrentStage.Room.transform, Rng, recent, forceAnomaly);

            Physics.SyncTransforms();
            lastPosition = player.position - transform.position;
            RoundStarted?.Invoke(Progress, HasAnomaly);
        }

        void LateUpdate()
        {
            if (CurrentStage == null || player == null || exitReached) return;
            Vector3 pos = player.position - transform.position;
            Vector3 exit = CurrentStage.ExitLine;

            if (lastPosition.z >= 0f && pos.z < 0f && Mathf.Abs(pos.x) < LineHalfWidth)
            {
                // Walked back over the entry line: "there is an anomaly". Turn the player around onto the entry line.
                MovePlayer(transform.position + new Vector3(-pos.x, pos.y, -pos.z), Quaternion.Euler(0f, 180f, 0f) * player.rotation);
                if (IsFinishStage) StartRound(); // the exit is still ahead
                else SubmitDecision(true);
            }
            else if (!IsFinishStage && lastPosition.z < exit.z && pos.z >= exit.z && Mathf.Abs(pos.x - exit.x) < LineHalfWidth)
            {
                // Went on over the exit line: "no anomaly".
                MovePlayer(transform.position + (pos - exit), player.rotation);
                SubmitDecision(false);
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
