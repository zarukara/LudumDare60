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
        public AudioSource footsteps;
        [Range(0f, 1f)] public float footstepVolume = 0.35f;

        CharacterController controller;
        float pitch, fallSpeed, travelled, bobWeight;
        Vector3 headRest;
        AudioClip[] stepClips;
        int nextClip;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (head == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) head = cam.transform;
            }
            headRest = head != null ? head.localPosition : new Vector3(0f, 1.6f, 0f);
            stepClips = MakeStepClips();
        }

        void OnDisable()
        {
            if (head != null) head.localPosition = headRest;
        }

        void Update()
        {
            UpdateCursor();
            Look();
            Move();
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
            if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
                delta += Mouse.current.delta.ReadValue() * mouseSensitivity;
            if (Gamepad.current != null)
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
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * (run ? runSpeed : walkSpeed);
            fallSpeed = controller.isGrounded ? -2f : fallSpeed + gravity * Time.deltaTime;
            velocity.y = fallSpeed;

            Vector3 before = transform.position;
            controller.Move(velocity * Time.deltaTime);
            Vector3 moved = transform.position - before;
            moved.y = 0f;
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
            if (footsteps == null || stepClips == null) return;
            footsteps.pitch = Random.Range(0.9f, 1.1f);
            footsteps.PlayOneShot(stepClips[nextClip], footstepVolume);
            nextClip = (nextClip + 1 + Random.Range(0, stepClips.Length - 1)) % stepClips.Length;
        }

        /// <summary>Soft thuds on carpet: low-passed noise with a little body, a few variants.</summary>
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
