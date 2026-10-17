using UnityEngine;
using UnityEngine.InputSystem;

namespace PauseSystem
{
    /// <summary>
    /// Tab (Start on a gamepad) pauses and resumes the game: time stops, every sound pauses, and the player's
    /// scripts check <see cref="Paused"/> to ignore keys and the mouse. The cursor is free while paused (the
    /// <see cref="PauseMenu"/> settings) and captured again on resume. The VHS overlay shows the state.
    /// Disabling or unloading this component resumes the game.
    /// </summary>
    public class GamePause : MonoBehaviour
    {
        /// <summary>True while the game is paused.</summary>
        public static bool Paused { get; private set; }

        static float resumeTimeScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Paused = false;

        void Update()
        {
            var keyboard = Keyboard.current;
            bool pressed = (keyboard != null && keyboard.tabKey.wasPressedThisFrame && !keyboard.altKey.isPressed) // not Alt+Tab
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
