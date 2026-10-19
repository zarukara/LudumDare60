using System.Collections;
using System.Collections.Generic;
using BabaikaSystem;
using CameraSystem;
using LevelSystem;
using PauseSystem;
using PlayerSystem;
using RoundSystem;
using SoundSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScareSystem
{

    public partial class StairsHunt : MonoBehaviour
    {
        public const string SoundAlarm = "stairs_alarm";
        public const string SoundBreathIn = "breath_hold";
        public const string SoundBreathOut = "breath_out";
        public const string SoundGasp = "breath_gasp";
        public const string SoundRewind = "tape_rewind";

        public enum Phase
        {
            Off,
            Calm,
            Cutscene,
            Waiting,
            Passing,
            Chasing,
            Fleeing,
            Escaped,
            Caught,
            Done,
        }

        public RoundManager round;
        public PlayerController player;
        public PlayerHide hide;
        public Flashlight flashlight;
        public Camera view;
        public Babaika babaikaPrefab;
        public SoundBank sounds;
        [Tooltip("Found in the scene when empty.")]
        public Ambience ambience;
        public TapeRewind tape;

        [Header("Floors")]
        [Tooltip("Where it begins: the lamps turn red and she is first seen.")]
        public int startFloor = 7;
        [Tooltip("A slip rewinds the tape this many floors up (never above the start floor).")]
        public int rewindFloors = 3;

        [Header("Her")]
        [Tooltip("Crawling speed, m/s: on the start floor, on the 1st.")]
        public Vector2 crawlSpeed = new Vector2(1.9f, 2.6f);
        [Tooltip("Seconds she waits round the corner below after the player reaches a landing (min, max).")]
        public Vector2 waitBelow = new Vector2(0.6f, 1.4f);
        [Tooltip("She sees a player who is not hidden this close if nothing is in between, metres.")]
        public float seeDistance = 5f;
        [Tooltip("A flashlight on gives the player away when she is this close and nothing is in between, metres.")]
        public float lightDistance = 7.5f;
        [Tooltip("Seconds after a rewind before she can find the player again.")]
        public float rewindGrace = 2.5f;
        [Tooltip("The player has to hold their breath while she is this close, metres.")]
        public float breathDistance = 4.6f;
        [Tooltip("Chance that she stops over the cart and stares: on the start floor, on the 1st.")]
        public Vector2 stareChance = new Vector2(0.55f, 0.95f);
        [Tooltip("How long she stares: on the start floor, on the 1st, seconds.")]
        public Vector2 stareTime = new Vector2(1.6f, 3.6f);

        [Header("Cutscene")]
        public float zoomFov = 40f;
        [Tooltip("Volume of the siren that wails while the lamps flash red (quieter while she is near).")]
        public float sirenVolume = 0.35f;

        [Header("Chase")]
        [Tooltip("This floor has no cart: she comes down at the player from above and chases them down to the way out.")]
        public int chaseFloor = 4;
        [Tooltip("Landings the player runs down past before she breaks out of a door below them.")]
        public int descentLandings = 4;
        [Tooltip("Landings the player then runs back up, to the door out.")]
        public int climbLandings = 4;
        [Tooltip("Gap she keeps behind a running player, metres along their track: when the chase begins, at its end.")]
        public Vector2 chaseGap = new Vector2(4.5f, 1.8f);
        [Tooltip("Her slowest pace, m/s: when the chase begins (below walking), at its end (between walking and running).")]
        public Vector2 chaseMinSpeed = new Vector2(2f, 3.1f);
        public float chaseMaxSpeed = 6.5f;
        [Tooltip("Gap along the track at which she has the player.")]
        public float chaseCatchGap = 0.6f;
        [Tooltip("Her head above the player's feet while she runs along the ceilings, metres.")]
        public float chaseHeadHeight = 2.7f;

        public Phase State { get; private set; }

        public int Floor { get; private set; }

        public BreathHold Breath { get; private set; }

        public IReadOnlyList<string> Hints => hints;

        static readonly RaycastHit[] Hits = new RaycastHit[8];

        readonly List<string> hints = new List<string>();
        Stairwell well;
        Babaika babaika;
        AudioSource voice, presence, heartbeat, music, rewindLoop, siren;
        float sirenLevel;
        string hintKey;
        float hintUntil, graceUntil, lightTime, normalFov;
        bool cutsceneDone, passedStare, locked;

        void Awake()
        {
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            if (ambience == null) ambience = FindFirstObjectByType<Ambience>();
        }

        void OnEnable()
        {
            if (round == null) return;
            round.RoundStarted += OnRoundStarted;
            round.RunStarted += OnRunStarted;
            round.ExitReached += OnExitReached;
        }

        void OnDisable()
        {
            if (round != null)
            {
                round.RoundStarted -= OnRoundStarted;
                round.RunStarted -= OnRunStarted;
                round.ExitReached -= OnExitReached;
            }
            Stop();
        }

        void OnRunStarted()
        {
            Stop();
            State = Phase.Off;
        }

        void OnExitReached()
        {
            Stop();
            State = Phase.Done;
        }

        void OnRoundStarted(int progress, bool hasAnomaly)
        {
            Stop();
            var room = round.IsFinishStage && round.CurrentStage != null ? round.CurrentStage.Room : null;
            well = room != null ? room.GetComponent<Stairwell>() : null;
            if (well == null)
            {
                State = Phase.Off;
                return;
            }
            State = Phase.Calm;
            Floor = startFloor;
            cutsceneDone = false;
            well.RemoveCover(chaseFloor);
            well.OpenWaysUp();
            if (tape != null) tape.Begin();
        }

        void Stop()
        {
            StopAllCoroutines();
            Breath = null;
            hintUntil = 0f;
            StopSound(ref presence);
            StopSound(ref heartbeat);
            StopSound(ref music);
            StopSound(ref rewindLoop);
            StopSound(ref siren);
            if (babaika != null) Destroy(babaika.gameObject);
            babaika = null;
            VhsEffect.Rewinding = false;
            escape = Escape.None;
            if (tape != null) tape.Stop();
            if (well != null) well.ResetNumbers();
            if (well != null) well.OpenWaysUp();
            if (view != null && normalFov > 0f) view.fieldOfView = normalFov;
            if (hide != null) hide.allowed = true;
            if (locked) Lock(false);
        }

        void Update()
        {
            Advise();
            if (State == Phase.Escaped && player != null && AtTheWayOut(player.transform.position))
            {
                State = Phase.Done;
                round.ReachExit();
                return;
            }
            if (well == null || player == null || (State != Phase.Calm && State != Phase.Waiting)) return;

            var feet = player.transform.position;
            int landing = Mathf.RoundToInt(well.Level(feet));
            if (landing < 1 || landing > Floor || !well.OnLanding(landing, feet)) return;
            Floor = landing;
            well.ShutWaysUp(Floor);
            if (tape != null) tape.Mark(Floor);
            if (!cutsceneDone && Floor == startFloor) StartCoroutine(Cutscene());
            else if (Floor == chaseFloor) StartCoroutine(Descent());
            else StartCoroutine(Pass(Floor, FirstPoint, true, false));
        }

        void Advise()
        {
            hints.Clear();
            if (Time.time < hintUntil && hintKey != null) hints.Add(hintKey);
            if (State != Phase.Passing || babaika == null || !babaika.gameObject.activeSelf || view == null) return;
            string hideHint = HideHint(Floor);
            if (!passedStare && (hide == null || !hide.Hidden) && !hints.Contains(hideHint)) hints.Add(hideHint);
            bool lit = flashlight != null && flashlight.IsOn;
            if (lit && Vector3.Distance(babaika.transform.position, view.transform.position) < lightDistance + 3f) hints.Add("hint.lightOff");
        }

        IEnumerator Cutscene()
        {
            State = Phase.Cutscene;
            cutsceneDone = true;
            Lock(true);
            if (hide != null) hide.allowed = false;
            RaiseAlarm();
            if (ambience != null) ambience.Duck(0.35f);
            normalFov = view.fieldOfView;

            var route = well.Route(startFloor, out _);
            Spawn(route[3], route[4]);
            babaika.walkSpeed = 1.6f;
            presence = LoopOn(BabaikaChase.SoundChaseLoop, babaika.gameObject, true, 0.7f);
            yield return Glide(well.Lookout(startFloor), 12f, 0.5f);
            babaika.Follow(new[] { Vector3.Lerp(route[3], route[4], 0.35f) }, BabaikaGait.Walk);
            for (float t = 0f; babaika.IsMoving || t < 0.8f; t += Time.deltaTime)
            {
                ScareView.TurnTowards(player, view, babaika.transform.position, 6f);
                view.fieldOfView = Mathf.Lerp(normalFov, zoomFov, ScareView.Smooth(t / 0.6f));
                yield return null;
            }

            babaika.LookAt(view.transform.position);
            var scream = sounds != null ? sounds.PlayAt(BabaikaChase.SoundScream, babaika.transform.position) : null;
            babaika.Scream(scream != null ? Mathf.Clamp(scream.length * 0.9f, 1.3f, 3f) : 1.3f);
            player.Shake(0.55f);
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                ScareView.TurnTowards(player, view, babaika.transform.position, 8f);
                yield return null;
            }
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                view.fieldOfView = Mathf.Lerp(zoomFov, normalFov, ScareView.Smooth(t / 0.25f));
                yield return null;
            }
            view.fieldOfView = normalFov;
            Lock(false);
            if (hide != null) hide.allowed = true;
            Hint(HideHint(startFloor), 4f);

            graceUntil = Time.time + 4f;
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                babaika.LookAt(view.transform.position);
                if (!Watch(startFloor, false)) yield break;
                yield return null;
            }
            yield return Pass(startFloor, 3, false, true);
        }

        IEnumerator Glide(Pose to, float pitch, float seconds)
        {
            var body = player.GetComponent<CharacterController>();
            Vector3 from = player.transform.position, goal = new Vector3(to.position.x, from.y, to.position.z);
            Quaternion turnFrom = player.transform.rotation;
            float pitchFrom = player.Pitch;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float u = ScareView.Smooth(t / seconds);
                if (body != null) body.enabled = false;
                player.transform.SetPositionAndRotation(Vector3.Lerp(from, goal, u), Quaternion.Slerp(turnFrom, to.rotation, u));
                if (body != null) body.enabled = true;
                player.Pitch = Mathf.Lerp(pitchFrom, pitch, u);
                yield return null;
            }
        }

        const int FirstPoint = 1;

        IEnumerator Pass(int floor, int from, bool fresh, bool forceStare)
        {
            State = Phase.Passing;
            Floor = floor;
            passedStare = false;
            cutsceneDone = true;
            RaiseAlarm();

            var route = well.Route(floor, out int stare, out bool peeks);
            float depth = Depth(floor);
            if (fresh || babaika == null) Spawn(route[from], route[from + 1]);
            babaika.walkSpeed = Mathf.Lerp(crawlSpeed.x, crawlSpeed.y, depth);
            babaika.StopLooking();
            if (presence == null) presence = LoopOn(BabaikaChase.SoundChaseLoop, babaika.gameObject, true, 0.7f);
            if (heartbeat == null) heartbeat = LoopOn(BabaikaChase.SoundHeartbeat, gameObject, false, 0.3f);

            if (fresh)
                for (float wait = Random.Range(waitBelow.x, waitBelow.y); wait > 0f; wait -= Time.deltaTime)
                {
                    if (!Watch(floor, false)) yield break;
                    yield return null;
                }

            for (int leg = 0; leg < 2; leg++)
            {
                if (leg == 1)
                {
                    if (!peeks) break;
                    babaika.bodyUp = Vector3.up;
                }
                if (!Crawl(route, leg == 0 ? from + 1 : stare, peeks && leg == 0 ? stare - 1 : stare)) yield break;
                while (babaika.IsMoving)
                {
                    if (!Watch(floor, false)) yield break;
                    yield return null;
                }
            }

            if (peeks || forceStare || Random.value < Mathf.Lerp(stareChance.x, stareChance.y, depth))
            {
                babaika.Stand();
                float seconds = forceStare ? 2.2f : Mathf.Lerp(stareTime.x, stareTime.y, depth) + Random.Range(0f, 0.8f);
                for (float t = 0f; t < seconds; t += Time.deltaTime)
                {
                    babaika.LookAt(view.transform.position);
                    if (!Watch(floor, true)) yield break;
                    yield return null;
                }
                babaika.StopLooking();
            }

            passedStare = true;
            babaika.bodyUp = Vector3.down;
            Crawl(route, stare + 1, route.Length - 1);
            while (babaika.IsMoving)
            {
                if (!Watch(floor, false)) yield break;
                yield return null;
            }

            Park();
            StopSound(ref heartbeat);
            if (Breath != null)
            {
                Breath = null;
                Play(SoundBreathOut);
            }
            if (well.NearFloor(floor, player.transform.position)) Floor = floor - 1;
            State = Floor >= 1 ? Phase.Waiting : Phase.Done;
        }

        bool Crawl(Vector3[] route, int first, int last)
        {
            var points = new List<Vector3>();
            for (int i = first; i <= last && i < route.Length; i++) points.Add(route[i]);
            if (points.Count == 0) return false;
            babaika.Follow(points, BabaikaGait.Walk);
            return true;
        }

        bool Watch(int floor, bool staring)
        {
            if (GamePause.Paused || babaika == null) return true;
            float dt = Time.deltaTime;
            Vector3 head = babaika.transform.position, eye = view.transform.position;
            float distance = Vector3.Distance(head, eye);
            float near = Mathf.InverseLerp(9f, 2.5f, distance);
            if (heartbeat != null)
            {
                heartbeat.volume = Mathf.Lerp(0.15f, 0.9f, near);
                heartbeat.pitch = Mathf.Lerp(0.9f, 1.4f, near);
            }
            if (siren != null) siren.volume = sirenLevel * Mathf.Lerp(1f, 0.35f, near);

            bool hidden = hide != null && hide.Hidden;
            if (!hidden) Breath = null;
            bool clear = distance < Mathf.Max(seeDistance, lightDistance) && Sees(head, eye);
            if (!hidden && distance < seeDistance && Time.time > graceUntil && clear) return Caught();

            bool lit = flashlight != null && flashlight.IsOn;
            lightTime = lit && distance < lightDistance && clear && Time.time > graceUntil ? lightTime + dt : 0f;
            if (lightTime > 0.7f) return Caught();

            if (hidden && Breath == null && distance < breathDistance && !passedStare)
            {
                Breath = new BreathHold();
                Play(SoundBreathIn);
            }
            if (Breath == null) return true;
            Breath.Difficulty = Mathf.Clamp01(Depth(floor) * 0.6f + (staring ? 0.45f : 0f));
            if (Breath.Tick(dt, BreathPressed()) < 0)
            {
                Play(SoundGasp);
                return Caught();
            }
            if (passedStare && distance > breathDistance + 0.8f)
            {
                Breath = null;
                Play(SoundBreathOut);
            }
            return true;
        }

        bool Sees(Vector3 head, Vector3 eye)
        {
            Vector3 line = eye - head;
            float length = line.magnitude;
            int count = Physics.RaycastNonAlloc(head, line / length, Hits, length - 0.1f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var t = Hits[i].collider.transform;
                if (t.IsChildOf(player.transform) || t.IsChildOf(babaika.transform)) continue;
                return false;
            }
            return true;
        }

        static bool BreathPressed() =>
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        bool Caught()
        {
            if (State == Phase.Caught) return false;
            StopAllCoroutines();
            StartCoroutine(Screamer());
            return false;
        }

        IEnumerator Screamer()
        {
            State = Phase.Caught;
            Breath = null;
            hintUntil = 0f;
            Lock(true);
            if (hide != null) hide.allowed = false;
            StopSound(ref presence);
            StopSound(ref heartbeat);
            StopSound(ref music);

            var cam = view.transform;
            if (normalFov <= 0f) normalFov = view.fieldOfView;
            view.fieldOfView = 55f;
            if (babaika == null) Spawn(cam.position + cam.forward * 2f, cam.position);
            babaika.gameObject.SetActive(true);
            babaika.bodyUp = Vector3.up;
            babaika.StopLooking();
            if (sounds != null) sounds.PlayOn(voice, BabaikaChase.SoundScreamer);
            player.Shake(1f);
            babaika.Scream(2f);
            Quaternion facing = Quaternion.LookRotation(-cam.forward, cam.up);
            Vector3 start = babaika.transform.position;
            for (float t = 0f; t < 0.18f; t += Time.deltaTime)
            {
                babaika.transform.rotation = facing;
                babaika.DriveTo(Vector3.Lerp(start, cam.position + cam.forward * 0.72f - cam.up * 0.04f, ScareView.Smooth(t / 0.18f)), BabaikaGait.Run);
                yield return null;
            }
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                babaika.transform.rotation = facing;
                babaika.DriveTo(cam.position + cam.forward * 0.72f - cam.up * 0.04f + Random.insideUnitSphere * 0.015f, BabaikaGait.Run);
                player.Shake(0.8f);
                yield return null;
            }
            view.fieldOfView = normalFov;
            Park();
            if (hide != null) hide.Release();
            well.ResetNumbers();

            var stage = escape;
            int target = stage == Escape.Down ? chaseFloor : stage == Escape.Up ? BurstMark : Mathf.Min(Floor + rewindFloors, startFloor);

            rewindLoop = LoopOn(SoundRewind, gameObject, false, 1f);
            if (ambience != null) ambience.Duck(0.15f);
            if (tape != null && tape.HasMark(target))
            {
                yield return tape.Rewind(target);
            }
            else
            {

                VhsEffect.Rewinding = true;
                yield return new WaitForSeconds(2f);
                MovePlayer(stage == Escape.Up ? BurstLanding() : well.Arrival(stage == Escape.Down ? chaseFloor : target));
                VhsEffect.Rewinding = false;
            }
            StopSound(ref rewindLoop);
            if (ambience != null) ambience.Duck(0.35f);

            Lock(false);
            if (hide != null) hide.allowed = true;
            graceUntil = Time.time + rewindGrace;
            lightTime = 0f;

            if (stage == Escape.Up)
            {
                yield return Burst();
                yield break;
            }
            escape = Escape.None;
            Floor = stage == Escape.Down ? chaseFloor : target;
            well.ShutWaysUp(Floor);
            State = Phase.Waiting;
        }

        float Depth(int floor) => Mathf.InverseLerp(startFloor, 1f, floor);

        void Spawn(Vector3 at, Vector3 toward, bool upright = false)
        {
            if (babaika == null)
            {
                babaika = Instantiate(babaikaPrefab, at, Quaternion.identity);
                babaika.name = "Babaika_Stairs";
                var legs = babaika.GetComponent<BabaikaLegs>();
                var claws = sounds != null ? sounds.Find(BabaikaChase.SoundClaws) : null;
                if (legs != null && claws != null && claws.clips.Length > 0) legs.clawClips = claws.clips;
            }
            babaika.gameObject.SetActive(true);
            babaika.bodyUp = upright ? Vector3.up : Vector3.down;
            Vector3 facing = Vector3.ProjectOnPlane(toward - at, Vector3.up);
            if (facing.sqrMagnitude < 1e-4f) facing = Vector3.forward;
            babaika.Teleport(at, Quaternion.LookRotation(facing.normalized, babaika.bodyUp));
        }

        void Park()
        {
            StopSound(ref presence);
            if (siren != null) siren.volume = sirenLevel;
            if (babaika == null) return;
            babaika.Stand();
            babaika.StopLooking();
            babaika.gameObject.SetActive(false);
        }

        void Lock(bool on)
        {
            locked = on;
            if (player == null) return;
            player.canMove = !on;
            player.canLook = !on;
        }

        void MovePlayer(Pose pose, bool levelView = true)
        {
            var body = player.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
            player.transform.SetPositionAndRotation(pose.position, pose.rotation);
            if (body != null) body.enabled = true;
            if (levelView) player.Pitch = 0f;
        }

        void RaiseAlarm()
        {
            if (!well.Alarm) well.SetAlarm(true);
            if (siren != null) return;
            siren = LoopOn(SoundAlarm, gameObject, false, sirenVolume);
            if (siren != null) sirenLevel = siren.volume;
        }

        string HideHint(int floor)
        {
            var cover = well != null ? well.Cover(floor) : null;
            return cover != null ? cover.hintKey : "hint.hideNow";
        }

        void Hint(string key, float seconds)
        {
            hintKey = key;
            hintUntil = Time.time + seconds;
        }

        void Play(string slot)
        {
            if (sounds != null) sounds.PlayOn(voice, slot);
        }

        AudioSource LoopOn(string slot, GameObject host, bool spatial, float volume)
        {
            if (sounds == null || host == null) return null;
            var holder = new GameObject("Loop_" + slot);
            holder.transform.SetParent(host.transform, false);
            var source = sounds.StartLoop(slot, holder, spatial, volume);
            if (source == null) Destroy(holder);
            return source;
        }

        static void StopSound(ref AudioSource source)
        {
            if (source != null) Destroy(source.gameObject);
            source = null;
        }

        [ContextMenu("Debug: Jump To The Stairs")]
        void DebugJump() => DebugToFloor(Stairwell.Top);

        internal void DebugToFloor(int floor)
        {
            if (!Application.isPlaying || round == null) return;
            if (!round.IsFinishStage)
            {
                var chase = GetComponent<BabaikaChase>();
                if (chase != null) chase.SkipToDone();
                round.JumpTo(round.roundsToWin - 1);
                round.SubmitDecision(round.HasAnomaly);
            }
            if (well == null) return;
            if (floor < startFloor) cutsceneDone = true;
            if (floor <= startFloor) RaiseAlarm();
            Floor = Mathf.Min(floor, startFloor);
            MovePlayer(well.Arrival(floor));
        }

        [ContextMenu("Debug: Jump To The Burst")]
        void DebugBurst() => DebugEscape(false);

        [ContextMenu("Debug: Jump To The Exit")]
        void DebugExit() => DebugEscape(true);

        void DebugEscape(bool atExit)
        {
            if (!Application.isPlaying || well == null) return;
            Stop();
            cutsceneDone = true;
            Floor = chaseFloor;
            RaiseAlarm();
            if (tape != null) tape.Begin();
            if (atExit)
            {
                MovePlayer(ExitLanding());
                StartCoroutine(BackToExit());
                return;
            }
            MovePlayer(BurstLanding());
            StartCoroutine(Burst());
        }
    }
}
