using PauseSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace PlayerSystem
{
    /// <summary>
    /// Hand torch: F (or the gamepad's north button) switches it on and off with a click. With a
    /// <see cref="viewmodel"/> (the hand holding the torch, a child of the camera) the hand comes up into view while
    /// it is on, sways with the steps, pulls back near walls, and the light shines from the torch's "Lens".
    /// Without one the light sits a little right of and below the eyes.
    /// </summary>
    public class Flashlight : MonoBehaviour
    {
        [Tooltip("Created on start if empty.")]
        public Light torch;
        public bool startOn;
        public float intensity = 10f;
        public float range = 16f;
        public float angle = 50f;
        public Color color = new Color(1f, 0.93f, 0.8f);
        [Tooltip("Plays the switch sounds; the player's footstep source if empty.")]
        public AudioSource clickSource;
        [Tooltip("Switching on and off; a generated click while empty.")]
        public AudioClip onSound, offSound;
        [Range(0f, 1f)] public float clickVolume = 0.6f;

        [Header("Hand")]
        [Tooltip("The hand holding the torch (child of this camera). The light moves to its child named \"Lens\".")]
        public Transform viewmodel;
        public Vector3 raisedPosition = new Vector3(0.15f, -0.165f, 0.33f);
        public Vector3 raisedRotation = new Vector3(4f, -7f, 0f);
        public Vector3 loweredPosition = new Vector3(0.26f, -0.72f, 0.16f);
        public Vector3 loweredRotation = new Vector3(45f, -12f, 0f);
        [Tooltip("Raises per second.")]
        public float raiseSpeed = 4.5f;
        [Tooltip("Steps for the sway; found on the player if empty.")]
        public PlayerController walker;

        AudioClip click;
        bool isOn;
        Renderer[] handRenderers = new Renderer[0];
        float wallPush;

        public bool IsOn => isOn;
        /// <summary>0: hand down and hidden, 1: torch held up.</summary>
        public float Raise { get; private set; }

        void Awake()
        {
            if (torch == null)
            {
                var go = new GameObject("Torch");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0.18f, -0.18f, 0.1f);
                go.transform.localRotation = Quaternion.Euler(2f, -3f, 0f);
                torch = go.AddComponent<Light>();
                torch.type = LightType.Spot;
                torch.spotAngle = angle;
                torch.innerSpotAngle = angle * 0.55f;
                torch.range = range;
                torch.intensity = intensity;
                torch.color = color;
                torch.shadows = LightShadows.Soft;
                torch.shadowStrength = 0.85f;
                torch.shadowNearPlane = 0.1f;
            }
            if (viewmodel != null)
            {
                var lens = FindChild(viewmodel, "Lens");
                if (lens != null)
                {
                    torch.transform.SetParent(lens, false);
                    torch.transform.localPosition = new Vector3(0f, 0f, 0.02f);
                    torch.transform.localRotation = Quaternion.identity;
                }
                handRenderers = viewmodel.GetComponentsInChildren<Renderer>(true);
                foreach (var r in handRenderers) r.shadowCastingMode = ShadowCastingMode.Off;
            }
            if (walker == null) walker = GetComponentInParent<PlayerController>();
            if (clickSource == null) clickSource = GetComponentInParent<AudioSource>();
            click = MakeClick();
            Switch(startOn, false);
            Raise = startOn ? 1f : 0f;
            PoseHand(0f);
        }

        void Update()
        {
            if (GamePause.Paused) return;
            bool pressed = (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                           || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
            if (pressed) Switch(!isOn, true);
            Raise = Mathf.MoveTowards(Raise, isOn ? 1f : 0f, raiseSpeed * Time.deltaTime);
            PoseHand(Time.deltaTime);
        }

        public void Switch(bool on, bool withSound = true)
        {
            isOn = on;
            if (torch != null) torch.enabled = on;
            var sound = on ? onSound : offSound;
            if (sound == null) sound = click != null ? click : click = MakeClick();
            if (withSound && clickSource != null) clickSource.PlayOneShot(sound, clickVolume);
        }

        void PoseHand(float dt)
        {
            if (viewmodel == null) return;
            bool visible = Raise > 0.01f;
            foreach (var r in handRenderers) r.enabled = visible;
            if (!visible) return;

            float t = Raise * Raise * (3f - 2f * Raise);
            Vector3 position = Vector3.Lerp(loweredPosition, raisedPosition, t);
            Quaternion rotation = Quaternion.Slerp(Quaternion.Euler(loweredRotation), Quaternion.Euler(raisedRotation), t);

            if (walker != null)
            {
                float phase = walker.Travelled / Mathf.Max(walker.stepLength, 0.01f) * Mathf.PI;
                float w = walker.WalkWeight;
                position += new Vector3(Mathf.Cos(phase) * 0.007f, -Mathf.Abs(Mathf.Sin(phase)) * 0.012f, 0f) * w;
                rotation *= Quaternion.Euler(Mathf.Sin(phase * 2f) * 1.2f * w, Mathf.Cos(phase) * 1.5f * w, 0f);
            }

            // Close to a wall the torch would go through it: pull the hand back and tip it up.
            float clear = 0.62f;
            float goal = 0f;
            if (Physics.Raycast(transform.position, transform.forward, out var hit, clear, ~0, QueryTriggerInteraction.Ignore)
                && (walker == null || !hit.collider.transform.IsChildOf(walker.transform)))
                goal = clear - hit.distance;
            wallPush = dt > 0f ? Mathf.MoveTowards(wallPush, goal, dt * 2.5f) : goal;
            position += new Vector3(0f, -0.35f, -0.9f) * wallPush;
            rotation *= Quaternion.Euler(-wallPush * 60f, 0f, 0f);

            viewmodel.localPosition = position;
            viewmodel.localRotation = rotation;
        }

        static Transform FindChild(Transform root, string childName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return t;
            return null;
        }

        /// <summary>A short plastic click.</summary>
        static AudioClip MakeClick()
        {
            const int rate = 44100;
            var random = new System.Random(4);
            int length = (int)(rate * 0.035f);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / rate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                data[i] = (noise * 0.6f + Mathf.Sin(t * 2f * Mathf.PI * 2400f) * 0.4f) * Mathf.Exp(-t * 180f);
            }
            var clip = AudioClip.Create("Flashlight_Click", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
