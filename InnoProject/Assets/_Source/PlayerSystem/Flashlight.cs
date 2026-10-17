using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerSystem
{
    /// <summary>
    /// Hand torch on the camera: F (or the gamepad's north button) switches it on and off with a click.
    /// The light sits a little right of and below the eyes, so the things it lights cast visible shadows.
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
        [Tooltip("Plays the click; the player's footstep source if empty.")]
        public AudioSource clickSource;

        AudioClip click;
        bool isOn;

        public bool IsOn => isOn;

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
            if (clickSource == null) clickSource = GetComponentInParent<AudioSource>();
            click = MakeClick();
            Switch(startOn, false);
        }

        void Update()
        {
            bool pressed = (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                           || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
            if (pressed) Switch(!isOn, true);
        }

        public void Switch(bool on, bool withSound = true)
        {
            isOn = on;
            if (torch != null) torch.enabled = on;
            if (withSound && clickSource != null) clickSource.PlayOneShot(click, 0.6f);
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
