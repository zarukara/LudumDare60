using UnityEngine;

namespace PauseSystem
{

    public static class GameSettings
    {
        const string SensitivityKey = "settings.mouseSensitivity";
        const string VolumeKey = "settings.volume";
        public const float MinSensitivity = 0.25f;
        public const float MaxSensitivity = 4f;

        static float sensitivity = 1f;

        public static float MouseSensitivity
        {
            get => sensitivity;
            set
            {
                sensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
                PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
            }
        }

        public static float Volume
        {
            get => AudioListener.volume;
            set
            {
                AudioListener.volume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
            }
        }

        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, 1f), MinSensitivity, MaxSensitivity);
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        }
    }
}
