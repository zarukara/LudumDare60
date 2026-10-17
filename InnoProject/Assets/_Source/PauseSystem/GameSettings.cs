using UnityEngine;

namespace PauseSystem
{
    /// <summary>
    /// The player's settings from the pause menu, kept between runs in PlayerPrefs: mouse sensitivity (a multiplier
    /// on the look speed) and the master volume of every sound.
    /// </summary>
    public static class GameSettings
    {
        const string SensitivityKey = "settings.mouseSensitivity";
        const string VolumeKey = "settings.volume";
        public const float MinSensitivity = 0.25f;
        public const float MaxSensitivity = 4f;

        static float sensitivity = 1f;

        /// <summary>Multiplier on the mouse look speed: 1 as designed, from 0.25 to 4.</summary>
        public static float MouseSensitivity
        {
            get => sensitivity;
            set
            {
                sensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
                PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
            }
        }

        /// <summary>Master volume, 0 to 1.</summary>
        public static float Volume
        {
            get => AudioListener.volume;
            set
            {
                AudioListener.volume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
            }
        }

        /// <summary>Writes the settings to disk (a web build keeps them only after this).</summary>
        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, 1f), MinSensitivity, MaxSensitivity);
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        }
    }
}
