using UnityEngine;
using UnityEngine.InputSystem;

namespace PauseSystem
{

    public class GamePause : MonoBehaviour
    {

        public static bool Paused { get; private set; }

        static float resumeTimeScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Paused = false;

        void Update()
        {
            var keyboard = Keyboard.current;
            bool pressed = (keyboard != null && keyboard.tabKey.wasPressedThisFrame && !keyboard.altKey.isPressed)
                           || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (pressed) Set(!Paused);
        }

        void OnDisable() => Set(false);

        public static void Set(bool paused)
        {
            if (Paused == paused) return;
            Paused = paused;
            if (paused)
            {
                resumeTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = resumeTimeScale;
            }
            AudioListener.pause = paused;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }
    }
}
