using System.Collections;
using LevelSystem;
using PlayerSystem;
using RoundSystem;
using SoundSystem;
using UnityEngine;

namespace ScareSystem
{
    /// <summary>
    /// The Babaika's aftermath. Once the chase is over (escaped or caught), the office of <see cref="level"/> shows
    /// what she left: the people who worked there flayed in their chairs, blood on the floor, desks and walls, and
    /// with <see cref="guts"/> heaps and mountains of guts (<see cref="GoreBuilder"/>). No anomaly spawns there, and
    /// either way out counts as right: the player may take it for an anomaly or for decoration.
    /// The first time in a run the player steps in, a short startle: the view snaps to the body facing the door and
    /// zooms in, the player flinches back, the <see cref="SoundReveal"/> slot plays and the ambience steps aside.
    /// </summary>
    public class GoreOffice : MonoBehaviour
    {
        public const string SoundReveal = "gore_reveal";

        public RoundManager round;
        public BabaikaChase chase;
        public PlayerController player;
        public Camera view;
        public SoundBank sounds;
        [Tooltip("Found in the scene when empty.")]
        public Ambience ambience;
        public GoreBuilder.Kit kit = new GoreBuilder.Kit();
        [Tooltip("The level whose office is filled once the chase is over.")]
        public int level = 6;
        [Tooltip("Heaps and mountains of guts too (off: the bodies and blood only).")]
        public bool guts;

        [Header("Startle")]
        public float zoomFov = 46f;
        [Tooltip("How far the player flinches back, metres.")]
        public float flinch = 0.35f;

        Transform office;
        AudioSource voice;
        Vector3 lookAt;
        bool startled;

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
            round.AnomalyBans.Add(IsGoreLevel);
            round.FreeChoices.Add(IsGoreLevel);
        }

        void OnDisable()
        {
            if (round == null) return;
            round.RoundStarted -= OnRoundStarted;
            round.RunStarted -= OnRunStarted;
            round.AnomalyBans.Remove(IsGoreLevel);
            round.FreeChoices.Remove(IsGoreLevel);
        }

        /// <summary>The chase is over and this progress is the gore level.</summary>
        bool IsGoreLevel(int progress) => chase != null && chase.State == BabaikaChase.Phase.Done && progress + 1 == level;

        void OnRunStarted() => startled = false;

        /// <summary>Play Mode: the chase counts as over and the player starts the gore level (right-click the component).</summary>
        [ContextMenu("Debug: Jump To The Gore Office")]
        void DebugJump()
        {
            if (!Application.isPlaying || round == null || chase == null) return;
            chase.SkipToDone();
            startled = false;
            round.JumpTo(level - 1);
        }

        void OnRoundStarted(int progress, bool hasAnomaly)
        {
            office = null;
            if (!IsGoreLevel(progress) || round.CurrentStage == null || round.CurrentStage.Room == null) return;
            office = round.CurrentStage.Room.transform;
            lookAt = GoreBuilder.Build(office, kit, new System.Random(System.Environment.TickCount), guts);
        }

        void Update()
        {
            // Only once the player stands in the office: never from a corridor, where the walls hide the room.
            if (startled || office == null || player == null || view == null || !InOffice()) return;
            startled = true;
            StartCoroutine(Startle());
        }

        bool InOffice()
        {
            var local = office.InverseTransformPoint(player.transform.position);
            return local.x > 0.2f && local.x < OfficeRoomBuilder.Width - 0.2f && local.z > 0.4f && local.z < OfficeRoomBuilder.Length - 0.2f;
        }

        IEnumerator Startle()
        {
            if (sounds != null) sounds.PlayOn(voice, SoundReveal);
            if (ambience != null) ambience.Duck(0.35f);
            player.canMove = false;
            player.canLook = false;
            player.Shake(0.5f);
            float normalFov = view.fieldOfView;
            var controller = player.GetComponent<CharacterController>();
            Vector3 back = -Vector3.ProjectOnPlane(lookAt - player.transform.position, Vector3.up).normalized;
            float moved = 0f;

            // Snap to it and zoom in, stepping back.
            for (float t = 0f; t < 1.3f; t += Time.deltaTime)
            {
                ScareView.TurnTowards(player, view, lookAt, t < 0.3f ? 14f : 6f);
                view.fieldOfView = Mathf.Lerp(normalFov, zoomFov, ScareView.Smooth(t / 0.3f));
                float step = flinch * ScareView.Smooth(t / 0.45f) - moved;
                if (step > 0f && controller != null && controller.enabled)
                {
                    controller.Move(back * step);
                    moved += step;
                }
                yield return null;
            }
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                view.fieldOfView = Mathf.Lerp(zoomFov, normalFov, ScareView.Smooth(t / 0.5f));
                yield return null;
            }
            view.fieldOfView = normalFov;
            player.canMove = true;
            player.canLook = true;

            // Let the sting ring out before the ambience comes back.
            yield return new WaitForSeconds(4.5f);
            if (ambience != null) ambience.Duck(1f);
        }
    }
}
