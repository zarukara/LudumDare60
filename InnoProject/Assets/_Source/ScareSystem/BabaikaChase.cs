using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BabaikaSystem;
using LevelSystem;
using LocalizationSystem;
using PlayerSystem;
using RoundSystem;
using SoundSystem;
using UnityEngine;

namespace ScareSystem
{
    /// <summary>
    /// The one-time Babaika chase. On level <see cref="triggerLevel"/> the office is always normal; when its far
    /// door opens, the Babaika comes down from the corridor ceiling behind it (cutscene: the view turns to her and
    /// zooms in) and goes after the player, who has to run back to level <see cref="escapeLevel"/>.
    /// She runs along the player's own track, so she takes the same doors and corners. A running player keeps her
    /// right behind (<see cref="pressureGap"/>); one who walks or stops gets caught.
    /// Escaped: on the escape level the office door slams shut behind the player and stays locked, and she throws
    /// herself at it so the door shakes. Caught: a screamer, then back to the first level with the doors as usual.
    /// Either way it does not happen again in this run. Sounds are slots of <see cref="sounds"/> (see the Sound*
    /// names); the game's <see cref="ambience"/> is pushed down from the cutscene until the chase is over.
    /// </summary>
    public class BabaikaChase : MonoBehaviour
    {
        public const string SoundScream = "babaika_scream";
        public const string SoundChaseLoop = "babaika_chase_loop";
        public const string SoundClaws = "babaika_claws";
        public const string SoundMusic = "chase_music";
        public const string SoundHeartbeat = "heartbeat";
        public const string SoundSlam = "door_slam";
        public const string SoundBang = "door_bang";
        public const string SoundScreamer = "screamer";

        public enum Phase
        {
            Waiting,
            Cutscene,
            Chase,
            Banging,
            Screamer,
            Done,
        }

        public RoundManager round;
        public PlayerController player;
        public Camera view;
        public Babaika babaikaPrefab;
        public SoundBank sounds;
        [Tooltip("Found in the scene when empty.")]
        public Ambience ambience;

        [Header("Event")]
        public int triggerLevel = 5;
        public int escapeLevel = 1;

        [Header("Cutscene")]
        public float zoomFov = 30f;
        [Tooltip("Metres per second while she comes down from the ceiling.")]
        public float descendSpeed = 0.4f;

        [Header("Chase")]
        [Tooltip("Gap she keeps behind a running player, metres along the track.")]
        public float pressureGap = 1.2f;
        [Tooltip("She never runs slower than this: faster than walking, slower than running.")]
        public float minSpeed = 3f;
        public float maxSpeed = 6.5f;
        [Tooltip("Gap along the track at which she has the player.")]
        public float catchGap = 0.45f;
        [Tooltip("Height of her head above the floor while she runs: high enough for the legs to reach walls and ceiling.")]
        public float headHeight = 1.45f;

        public Phase State { get; private set; }

        // The entry seam's first half in stage space, from the dead end to the stage line.
        static readonly Vector2Int[] SeamBehind = { new Vector2Int(2, -2), new Vector2Int(2, -1), new Vector2Int(2, 0), new Vector2Int(1, 0) };

        readonly List<Vector3> track = new List<Vector3>();
        Babaika babaika;
        AutoDoor watchedDoor, slamDoor;
        AudioSource oneShots, heartbeat, music, chaseLoop;
        Vector3 lastSample;
        float chaseTime, fade, runHint;

        void Awake()
        {
            oneShots = gameObject.AddComponent<AudioSource>();
            oneShots.playOnAwake = false;
            oneShots.spatialBlend = 0f;
            if (ambience == null) ambience = FindFirstObjectByType<Ambience>();
        }

        void OnEnable()
        {
            if (round == null) return;
            round.RoundStarted += OnRoundStarted;
            round.LineCrossed += OnLineCrossed;
            round.RunStarted += OnRunStarted;
            round.ForbidAnomaly = progress => State == Phase.Waiting && progress + 1 == triggerLevel;
        }

        void OnDisable()
        {
            if (round == null) return;
            round.RoundStarted -= OnRoundStarted;
            round.LineCrossed -= OnLineCrossed;
            round.RunStarted -= OnRunStarted;
            round.ForbidAnomaly = null;
        }

        // ------------------------------------------------------------------ round events

        void OnRunStarted()
        {
            StopAllCoroutines();
            Cleanup();
            fade = 0f;
            State = Phase.Waiting;
        }

        void OnRoundStarted(int progress, bool hasAnomaly)
        {
            watchedDoor = null;

            if (State == Phase.Waiting && round.Level == triggerLevel)
            {
                watchedDoor = Door(round.CurrentStage, "Exit_Door");
            }
            else if (State == Phase.Chase)
            {
                slamDoor = round.Level <= escapeLevel ? Door(round.CurrentStage, "Entrance_Door") : null;
            }
            else if (State == Phase.Banging)
            {
                // The player has gone on: she stays behind with the old stage.
                Cleanup();
                State = Phase.Done;
            }
        }

        /// <summary>
        /// Starts the cutscene once the trigger level's far door is open with the player inside that office; a door
        /// that opened for a player somewhere else does not count.
        /// </summary>
        void WaitForDoor()
        {
            if (watchedDoor == null || !watchedDoor.IsOpen || round.Level != triggerLevel || babaikaPrefab == null || !InOffice()) return;
            var local = round.CurrentStage.Room.transform.InverseTransformPoint(player.transform.position);
            Debug.Log($"[BabaikaChase] Cutscene on level {round.Level}, player in the office at x {local.x:F1}, z {local.z:F1}");
            StartCoroutine(Cutscene());
        }

        /// <summary>The player stands in the current office, not in a corridor behind its walls.</summary>
        bool InOffice()
        {
            var local = round.CurrentStage.Room.transform.InverseTransformPoint(player.transform.position);
            return local.x > 0f && local.x < OfficeRoomBuilder.Width && local.z > 0f && local.z < OfficeRoomBuilder.Length;
        }

        static AutoDoor Door(Stage stage, string name)
        {
            if (stage == null || stage.Room == null) return null;
            return stage.Room.GetComponentsInChildren<AutoDoor>(true).FirstOrDefault(d => d.transform.parent != null && d.transform.parent.name == name);
        }

        // ------------------------------------------------------------------ cutscene

        IEnumerator Cutscene()
        {
            State = Phase.Cutscene;
            var room = round.CurrentStage.Room.transform;
            Vector3 hang = room.TransformPoint(new Vector3(OfficeRoomBuilder.ExitX, 2.2f, OfficeRoomBuilder.Length + 1.3f));
            Vector3 low = room.TransformPoint(new Vector3(OfficeRoomBuilder.ExitX, headHeight, OfficeRoomBuilder.Length + 1f));
            babaika = Instantiate(babaikaPrefab, hang, room.rotation * Quaternion.Euler(0f, 180f, 0f));
            babaika.name = "Babaika";
            var legs = babaika.GetComponent<BabaikaLegs>();
            var claws = sounds != null ? sounds.Find(SoundClaws) : null;
            if (legs != null && claws != null && claws.clips.Length > 0) legs.clawClips = claws.clips;

            player.canMove = false;
            player.canLook = false;
            float normalFov = view.fieldOfView;
            if (ambience != null) ambience.Duck(0.25f); // a hush while she comes down
            babaika.GoTo(low, BabaikaGait.Walk, descendSpeed);

            float descent = Vector3.Distance(hang, low) / descendSpeed + 0.4f;
            for (float t = 0f; t < descent; t += Time.deltaTime)
            {
                TurnView(babaika.transform.position, 7f);
                view.fieldOfView = Mathf.Lerp(normalFov, zoomFov, Smooth(t / 1.4f));
                yield return null;
            }

            var scream = sounds != null ? sounds.PlayAt(SoundScream, babaika.transform.position) : null;
            babaika.Scream(scream != null ? Mathf.Clamp(scream.length * 0.9f, 1.3f, 3f) : 1.3f); // the jaw stays open for the cry
            player.Shake(0.6f);
            for (float t = 0f; t < 1.1f; t += Time.deltaTime)
            {
                TurnView(babaika.transform.position, 7f);
                yield return null;
            }

            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                view.fieldOfView = Mathf.Lerp(zoomFov, normalFov, Smooth(t / 0.35f));
                yield return null;
            }
            view.fieldOfView = normalFov;
            player.canMove = true;
            player.canLook = true;
            BeginChase(room);
        }

        /// <summary>Turns the player's view towards a point (yaw on the body, pitch on the head).</summary>
        void TurnView(Vector3 target, float sharpness)
        {
            float k = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            Vector3 to = target - view.transform.position;
            Vector3 flat = Vector3.ProjectOnPlane(to, Vector3.up);
            if (flat.sqrMagnitude > 1e-4f)
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(flat, Vector3.up), k);
            player.Pitch = Mathf.Lerp(player.Pitch, -Mathf.Atan2(to.y, flat.magnitude) * Mathf.Rad2Deg, k);
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        // ------------------------------------------------------------------ chase

        void BeginChase(Transform room)
        {
            State = Phase.Chase;
            round.BeginRetreat();
            chaseTime = 0f;
            runHint = 4f;
            track.Clear();
            track.Add(room.TransformPoint(new Vector3(OfficeRoomBuilder.ExitX, headHeight, OfficeRoomBuilder.Length - 0.5f)));
            lastSample = PlayerPoint();
            track.Add(lastSample);

            heartbeat = Loop(SoundHeartbeat, gameObject, false, 0.5f);
            music = Loop(SoundMusic, gameObject, false, 0.6f);
            chaseLoop = Loop(SoundChaseLoop, babaika.gameObject, true, 1f);
        }

        Vector3 PlayerPoint() => player.transform.position + Vector3.up * headHeight;

        void Update()
        {
            if (State == Phase.Waiting) WaitForDoor();
            if (State != Phase.Chase || babaika == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            chaseTime += dt;
            runHint -= dt;

            Vector3 target = PlayerPoint();
            if ((target - lastSample).sqrMagnitude > 0.0625f)
            {
                track.Add(target);
                lastSample = target;
            }

            Vector3 position = babaika.transform.position;
            float gap = TrackLength(position, target);
            float speed = Mathf.Clamp(player.Speed + 2.2f * (gap - pressureGap), minSpeed, maxSpeed);
            if (chaseTime < 2f) speed = Mathf.Min(speed, 1.2f + 0.9f * chaseTime); // a moment to turn round and run

            float step = speed * dt;
            while (step > 0f)
            {
                Vector3 next = track.Count > 0 ? track[0] : target;
                Vector3 to = next - position;
                float distance = to.magnitude;
                if (distance <= step)
                {
                    position = next;
                    step -= distance;
                    if (track.Count == 0) break;
                    track.RemoveAt(0);
                }
                else
                {
                    position += to / distance * step;
                    step = 0f;
                }
            }
            babaika.DriveTo(position, BabaikaGait.Run);

            gap = TrackLength(position, target);
            if (gap < catchGap || Vector3.Distance(position, target) < catchGap)
            {
                StartCoroutine(Screamer());
                return;
            }

            float near = Mathf.InverseLerp(4f, pressureGap * 0.8f, gap);
            if (heartbeat != null)
            {
                heartbeat.volume = Mathf.Lerp(0.25f, 0.9f, near);
                heartbeat.pitch = Mathf.Lerp(0.9f, 1.45f, near);
            }
            if (near > 0.6f) player.Shake(0.22f * near);
            if (player.Speed < 3.2f && chaseTime > 1.5f) runHint = Mathf.Max(runHint, 1.5f);

            if (slamDoor != null && PastDoor()) Slam();
        }

        float TrackLength(Vector3 from, Vector3 target)
        {
            float length = 0f;
            Vector3 previous = from;
            foreach (var point in track)
            {
                length += Vector3.Distance(previous, point);
                previous = point;
            }
            return length + Vector3.Distance(previous, target);
        }

        /// <summary>The player is a step inside the escape level's office.</summary>
        bool PastDoor()
        {
            var room = round.CurrentStage.Room.transform;
            var local = room.InverseTransformPoint(player.transform.position);
            return local.z > 0.9f && local.x > 0f && local.x < OfficeRoomBuilder.Width;
        }

        /// <summary>
        /// Keeps her behind the player over a stage line: she is moved like the player was. If that puts her past the
        /// built seam (she was far behind), she comes out of the seam's dead end, round a corner from the player.
        /// </summary>
        void OnLineCrossed(RoundManager.StageShift shift)
        {
            if (State != Phase.Chase || babaika == null) return;
            Vector3 origin = round.transform.position;
            float y = player.transform.position.y + headHeight;
            Vector3 mapped = shift.Map(babaika.transform.position);
            Vector3 local = mapped - origin;

            int cell = -1;
            for (int i = 0; i < SeamBehind.Length; i++)
            {
                var c = SeamBehind[i];
                if (local.x >= c.x * StageLayout.Cell - 1f && local.x <= c.x * StageLayout.Cell + 1f &&
                    local.z >= c.y * StageLayout.Cell && local.z <= c.y * StageLayout.Cell + StageLayout.Cell) cell = i;
            }
            Vector3 At(Vector3 stagePoint) => new Vector3(origin.x + stagePoint.x, y, origin.z + stagePoint.z);
            if (cell < 0)
            {
                cell = 0;
                mapped = At(StageLayout.CellCenter(SeamBehind[0]));
            }
            mapped.y = y;

            track.Clear();
            for (int i = cell + 1; i < SeamBehind.Length; i++) track.Add(At(StageLayout.CellCenter(SeamBehind[i])));
            track.Add(At(StageLayout.EntryLine));
            lastSample = PlayerPoint();
            track.Add(lastSample);
            babaika.Teleport(mapped, shift.Map(babaika.transform.rotation));
        }

        // ------------------------------------------------------------------ escaped: the door

        void Slam()
        {
            var door = slamDoor;
            slamDoor = null;
            door.Slam();
            if (sounds != null) sounds.PlayAt(SoundSlam, door.transform.position + Vector3.up);
            player.Shake(0.7f);
            round.EndRetreat();
            StopLoops();
            State = Phase.Banging;

            var room = round.CurrentStage.Room.transform;
            Vector3 rest = BangRest(room);
            if (room.InverseTransformPoint(babaika.transform.position).z > -0.6f)
                babaika.Teleport(rest, room.rotation); // the slamming door throws her back out
            StartCoroutine(Bang(door, room));
        }

        Vector3 BangRest(Transform room) => room.TransformPoint(new Vector3(OfficeRoomBuilder.EntranceX, headHeight - 0.15f, -1.05f));

        IEnumerator Bang(AutoDoor door, Transform room)
        {
            Vector3 rest = BangRest(room);
            Vector3 hit = room.TransformPoint(new Vector3(OfficeRoomBuilder.EntranceX + 0.1f, headHeight - 0.05f, -0.6f));
            babaika.LookAt(room.TransformPoint(new Vector3(OfficeRoomBuilder.EntranceX, headHeight - 0.1f, 1f)));
            // Once the leaf is shut, every claw takes a new hold on this side of it.
            yield return new WaitForSeconds(0.15f);
            if (babaika != null) babaika.Teleport(babaika.transform.position, babaika.transform.rotation);
            while (State == Phase.Banging && babaika != null && door != null)
            {
                yield return Drive(rest, 0.35f);
                yield return new WaitForSeconds(Random.Range(0.2f, 0.75f));
                yield return Drive(hit + Random.insideUnitSphere * 0.08f, 0.1f);
                if (babaika == null || door == null) yield break;
                door.Knock(Random.Range(1.2f, 1.6f), babaika.transform.position);
                if (sounds != null) sounds.PlayAt(SoundBang, door.transform.position + Vector3.up);
                float distance = Vector3.Distance(player.transform.position, door.transform.position);
                if (distance < 8f) player.Shake(Mathf.Lerp(0.75f, 0.15f, distance / 8f));
                if (Random.value < 0.25f) babaika.Scream(0.3f);
            }
        }

        IEnumerator Drive(Vector3 to, float seconds)
        {
            if (babaika == null) yield break;
            Vector3 from = babaika.transform.position;
            for (float t = 0f; t < seconds && babaika != null; t += Time.deltaTime)
            {
                babaika.DriveTo(Vector3.Lerp(from, to, Smooth(t / seconds)), BabaikaGait.Walk);
                yield return null;
            }
            if (babaika != null) babaika.DriveTo(to, BabaikaGait.Walk);
        }

        // ------------------------------------------------------------------ caught: the screamer

        IEnumerator Screamer()
        {
            State = Phase.Screamer;
            StopLoops();
            player.canMove = false;
            player.canLook = false;
            var cam = view.transform;
            float normalFov = view.fieldOfView;
            view.fieldOfView = 55f;
            if (sounds != null) sounds.PlayOn(oneShots, SoundScreamer);
            player.Shake(1f);
            babaika.Scream(2f);

            Quaternion facing = Quaternion.LookRotation(-cam.forward, cam.up);
            for (float t = 0f; t < 0.16f; t += Time.deltaTime)
            {
                babaika.transform.rotation = facing;
                babaika.DriveTo(cam.position + cam.forward * Mathf.Lerp(1.1f, 0.72f, Smooth(t / 0.16f)) - cam.up * 0.04f, BabaikaGait.Run);
                yield return null;
            }
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                babaika.transform.rotation = facing;
                babaika.DriveTo(cam.position + cam.forward * 0.72f - cam.up * 0.04f + Random.insideUnitSphere * 0.015f, BabaikaGait.Run);
                player.Shake(0.8f);
                yield return null;
            }

            fade = 1f;
            view.fieldOfView = normalFov;
            Cleanup();
            yield return new WaitForSeconds(0.9f);
            round.ResetProgress();
            player.Pitch = 0f;
            player.canMove = true;
            player.canLook = true;
            State = Phase.Done;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                fade = 1f - Smooth(t / 1.2f);
                yield return null;
            }
            fade = 0f;
        }

        // ------------------------------------------------------------------ helpers

        AudioSource Loop(string slot, GameObject host, bool spatial, float volume)
        {
            if (sounds == null || host == null) return null;
            var holder = new GameObject("Loop_" + slot);
            holder.transform.SetParent(host.transform, false);
            var source = sounds.StartLoop(slot, holder, spatial, volume);
            if (source == null) Destroy(holder);
            return source;
        }

        void StopLoops()
        {
            foreach (var source in new[] { heartbeat, music, chaseLoop })
                if (source != null) Destroy(source.gameObject);
            heartbeat = music = chaseLoop = null;
            if (ambience != null) ambience.Duck(1f);
        }

        void Cleanup()
        {
            StopLoops();
            if (babaika != null) Destroy(babaika.gameObject);
            babaika = null;
            slamDoor = null;
            track.Clear();
            if (round != null) round.EndRetreat();
        }

        void OnGUI()
        {
            if (State == Phase.Chase && runHint > 0f && !PauseSystem.GamePause.Paused)
            {
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(34 * Screen.height / 1080f) };
                GUI.color = new Color(1f, 0.85f, 0.8f, Mathf.Clamp01(runHint) * (0.75f + 0.25f * Mathf.Sin(Time.time * 9f)));
                GUI.Label(new Rect(0f, Screen.height * 0.72f, Screen.width, Screen.height * 0.08f), Localization.Get("hint.run"), style);
            }
            if (fade > 0f)
            {
                GUI.depth = -1000;
                GUI.color = new Color(0f, 0f, 0f, fade);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }
    }
}
