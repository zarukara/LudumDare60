using PauseSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerSystem
{
    /// <summary>
    /// First-person walking without jumping, like in The Exit 8: WASD / arrows / left stick to move, Shift or
    /// stick click to run, mouse / right stick to look. A click captures the cursor, Esc releases it.
    /// Head bob and footsteps follow the distance walked; the step sounds are generated on start.
    /// Yaw lives in the root's rotation, so other code (round teleports) may turn the player.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform head;
        public float walkSpeed = 2.6f;
        public float runSpeed = 4f;
        [Tooltip("Degrees per pixel of mouse movement.")]
        public float mouseSensitivity = 0.1f;
        [Tooltip("Degrees per second at full right-stick tilt.")]
        public float stickLookSpeed = 140f;
        public float gravity = -20f;
        public float bobHeight = 0.025f;
        public float stepLength = 0.75f;

        [Header("Footsteps")]
        public AudioSource footsteps;
        [Range(0f, 1f)] public float footstepVolume = 0.5f;
        [Tooltip("Steps on soft floors (carpet).")]
        public AudioClip[] softSteps = new AudioClip[0];
        [Tooltip("Steps on every other floor.")]
        public AudioClip[] hardSteps = new AudioClip[0];
        [Tooltip("Floor materials that sound soft.")]
        public Material[] softFloors = new Material[0];

        [Header("Control")]
        [Tooltip("Off: the player cannot walk (cutscenes, scares). Gravity still works.")]
        public bool canMove = true;
        [Tooltip("Off: mouse and stick do not turn the view; scripts set it with Pitch and the root's yaw.")]
        public bool canLook = true;
        /// <summary>Scripted walking instead of the player's keys (cutscenes, tests): x = strafe, y = forward.</summary>
        [System.NonSerialized] public Vector2? scriptedMove;
        [System.NonSerialized] public bool scriptedRun;

        /// <summary>Up/down look angle in degrees (positive looks down).</summary>
        public float Pitch
        {
            get => pitch;
            set => pitch = Mathf.Clamp(value, -85f, 85f);
        }

        /// <summary>Horizontal speed over the last frame, m/s.</summary>
        public float Speed { get; private set; }
        /// <summary>Distance walked so far; one step every <see cref="stepLength"/>.</summary>
        public float Travelled => travelled;
        /// <summary>0 standing .. 1 walking, smoothed (drives head bob and the body's gait).</summary>
        public float WalkWeight => bobWeight;

        CharacterController controller;
        float pitch, fallSpeed, travelled, bobWeight, shake, shakeTime;
        Vector3 headRest;
        AudioClip[] generatedSteps;
        int lastClip = -1;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (head == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) head = cam.transform;
            }
            headRest = head != null ? head.localPosition : new Vector3(0f, 1.6f, 0f);
            generatedSteps = MakeStepClips();
        }

        void OnDisable()
        {
            if (head != null) head.localPosition = headRest;
        }

        void Update()
        {
            if (GamePause.Paused) return; // the pause menu owns the cursor, and a frozen view must not turn
            UpdateCursor();
            Look();
            Move();
            ApplyShake();
        }

        /// <summary>Shakes the view (a blow, something very close); stronger calls override weaker ones.</summary>
        public void Shake(float strength) => shake = Mathf.Max(shake, strength);

        void ApplyShake()
        {
            if (head == null || shake <= 0f) return;
            shakeTime += Time.deltaTime;
            float a = shake * shake;
            head.localRotation *= Quaternion.Euler(
                (Mathf.PerlinNoise(shakeTime * 23f, 0.1f) - 0.5f) * 5f * a,
                (Mathf.PerlinNoise(0.3f, shakeTime * 23f) - 0.5f) * 5f * a,
                (Mathf.PerlinNoise(shakeTime * 17f, 0.7f) - 0.5f) * 4f * a);
            head.localPosition += new Vector3(Mathf.PerlinNoise(shakeTime * 29f, 0.5f) - 0.5f, Mathf.PerlinNoise(0.9f, shakeTime * 29f) - 0.5f, 0f) * (0.03f * a);
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 1.8f);
        }

        void UpdateCursor()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void Look()
        {
            var delta = Vector2.zero;
            if (canLook && Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
                delta += Mouse.current.delta.ReadValue() * (mouseSensitivity * GameSettings.MouseSensitivity);
            if (canLook && Gamepad.current != null)
                delta += Gamepad.current.rightStick.ReadValue() * (stickLookSpeed * Time.deltaTime);

            pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + delta.x, 0f);
            if (head != null) head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void Move()
        {
            var input = Vector2.zero;
            bool run = false;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) input.y += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) input.y -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) input.x += 1f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) input.x -= 1f;
                run = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
            }
            var pad = Gamepad.current;
            if (pad != null)
            {
                input += pad.leftStick.ReadValue();
                run |= pad.leftStickButton.isPressed;
            }
            if (scriptedMove.HasValue)
            {
                input = scriptedMove.Value;
                run = scriptedRun;
            }
            input = canMove ? Vector2.ClampMagnitude(input, 1f) : Vector2.zero;

            Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * (run ? runSpeed : walkSpeed);
            fallSpeed = controller.isGrounded ? -2f : fallSpeed + gravity * Time.deltaTime;
            velocity.y = fallSpeed;

            Vector3 before = transform.position;
            controller.Move(velocity * Time.deltaTime);
            Vector3 moved = transform.position - before;
            moved.y = 0f;
            Speed = Time.deltaTime > 0f ? moved.magnitude / Time.deltaTime : 0f;
            Bob(moved.magnitude, input.sqrMagnitude > 0.01f && controller.isGrounded);
        }

        void Bob(float distance, bool walking)
        {
            if (walking)
            {
                float previous = travelled;
                travelled += distance;
                if (Mathf.Floor(travelled / stepLength) > Mathf.Floor(previous / stepLength)) Step();
            }
            bobWeight = Mathf.MoveTowards(bobWeight, walking ? 1f : 0f, Time.deltaTime * 4f);
            if (head != null)
                head.localPosition = headRest + Vector3.down * (Mathf.Abs(Mathf.Sin(Mathf.PI * travelled / stepLength)) * bobHeight * bobWeight);
        }

        void Step()
        {
            if (footsteps == null) return;
            var clips = ClipsUnderfoot();
            if (clips == null || clips.Length == 0) clips = generatedSteps;
            if (clips == null || clips.Length == 0) return;
            int i = Random.Range(0, clips.Length);
            if (clips.Length > 1 && i == lastClip) i = (i + 1) % clips.Length; // never the same sample twice in a row
            lastClip = i;
            footsteps.pitch = Random.Range(0.92f, 1.08f);
            footsteps.PlayOneShot(clips[i], footstepVolume * Random.Range(0.85f, 1f));
        }

        /// <summary>Carpet or hard floor, judged by the material of the floor under the feet.</summary>
        AudioClip[] ClipsUnderfoot()
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var hit, 1f, ~0, QueryTriggerInteraction.Ignore))
                return hardSteps;
            var floor = hit.collider.GetComponent<Renderer>();
            bool soft = floor != null && System.Array.IndexOf(softFloors, floor.sharedMaterial) >= 0;
            return soft && softSteps.Length > 0 ? softSteps : hardSteps;
        }

        /// <summary>Fallback when no samples are assigned: soft thuds made from low-passed noise.</summary>
        static AudioClip[] MakeStepClips()
        {
            const int rate = 44100;
            var random = new System.Random(3);
            var clips = new AudioClip[4];
            for (int c = 0; c < clips.Length; c++)
            {
                int length = (int)(rate * 0.16f);
                var data = new float[length];
                float low = 0f;
                for (int i = 0; i < length; i++)
                {
                    float t = (float)i / rate;
                    float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                    low += (noise - low) * (0.07f + c * 0.01f);
                    float envelope = Mathf.Exp(-t * 38f) * Mathf.Min(1f, t * 400f);
                    data[i] = (low * 2.4f + Mathf.Sin(t * 2f * Mathf.PI * (68f + c * 7f)) * 0.45f) * envelope;
                }
                clips[c] = AudioClip.Create("Step_" + c, length, 1, rate, false);
                clips[c].SetData(data, 0);
            }
            return clips;
        }
    }
}
