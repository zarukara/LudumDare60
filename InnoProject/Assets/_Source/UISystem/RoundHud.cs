using LocalizationSystem;
using PauseSystem;
using RoundSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UISystem
{
    /// <summary>
    /// Placeholder on-screen text until the real UI. At the start of a run: "F — flashlight" (until the torch is
    /// switched on) and "go to the whiteboard and read it" (until the player gets near the board, which explains
    /// the rules). At the bottom of the exit stairs: a fade to black with the time, R to play again, M for the menu.
    /// F3: debug line.
    /// </summary>
    public class RoundHud : MonoBehaviour
    {
        public RoundManager round;
        public string menuScene = "Menu";
        [Tooltip("How close to the whiteboard counts as having read it.")]
        public float boardDistance = 3f;

        bool showFlashlightHint, showBoardHint, finished, debug;
        float whiteOut;
        GUIStyle title, body, hint, small;

        void OnEnable()
        {
            if (round == null) return;
            round.RunStarted += OnRunStarted;
            round.ExitReached += OnExitReached;
        }

        void OnDisable()
        {
            if (round == null) return;
            round.RunStarted -= OnRunStarted;
            round.ExitReached -= OnExitReached;
        }

        void OnRunStarted()
        {
            showFlashlightHint = true;
            showBoardHint = true;
            whiteOut = 0f;
            finished = false;
        }

        void OnExitReached() => finished = true;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame) debug = !debug;
            if (GamePause.Paused) return;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame) showFlashlightHint = false;
            if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) showFlashlightHint = false;
            if (showBoardHint && NearBoard()) showBoardHint = false;

            if (!finished) return;
            whiteOut = Mathf.MoveTowards(whiteOut, 1f, Time.deltaTime / 1.5f);
            if (whiteOut < 1f || keyboard == null) return;
            if (keyboard.rKey.wasPressedThisFrame) round.Restart();
            else if (keyboard.mKey.wasPressedThisFrame && Application.CanStreamedLevelBeLoaded(menuScene)) SceneManager.LoadScene(menuScene);
        }

        bool NearBoard()
        {
            if (round == null || round.player == null || round.CurrentStage == null || round.CurrentStage.Room == null) return false;
            var board = round.CurrentStage.Room.transform.Find("Props/Whiteboard");
            return board != null && Vector3.Distance(board.position, round.player.position) < boardDistance;
        }

        void OnGUI()
        {
            if (round == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                body = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
                hint = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft };
            }
            float scale = Screen.height / 1080f;
            title.fontSize = Mathf.RoundToInt(64 * scale);
            body.fontSize = Mathf.RoundToInt(26 * scale);
            hint.fontSize = Mathf.RoundToInt(28 * scale);
            small.fontSize = Mathf.RoundToInt(18 * scale);

            if (!finished && !GamePause.Paused) // the pause screen stays clean
            {
                float y = Screen.height * 0.8f;
                if (showFlashlightHint) Hint(Localization.Get("hint.flashlight"), ref y);
                if (showBoardHint) Hint(Localization.Get("hint.board"), ref y);
            }

            if (debug)
            {
                string anomaly = round.CurrentAnomaly != null ? round.CurrentAnomaly.anomalyName : Localization.Get("hud.none");
                GUI.color = Color.yellow;
                GUI.Label(new Rect(12f, 10f, 900f, 60f), Localization.Format("hud.debug", round.Progress, round.roundsToWin, anomaly), small);
            }

            if (finished)
            {
                GUI.color = new Color(0f, 0f, 0f, whiteOut);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                if (whiteOut >= 1f)
                {
                    GUI.color = new Color(0.78f, 0.76f, 0.72f);
                    int seconds = Mathf.FloorToInt(round.RunTime);
                    GUI.Label(new Rect(0f, Screen.height * 0.35f, Screen.width, Screen.height * 0.12f), Localization.Get("hud.finish"), title);
                    GUI.Label(new Rect(0f, Screen.height * 0.48f, Screen.width, Screen.height * 0.12f),
                        Localization.Format("hud.time", $"{seconds / 60}:{seconds % 60:00}") + "\n\n" + Localization.Get("hud.again"), body);
                }
            }
            GUI.color = Color.white;
        }

        /// <summary>One line of text on a dark strip, going up from y.</summary>
        void Hint(string text, ref float y)
        {
            var size = hint.CalcSize(new GUIContent(text));
            var rect = new Rect((Screen.width - size.x) / 2f - 18f, y - size.y / 2f - 6f, size.x + 36f, size.y + 12f);
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, 0.92f);
            GUI.Label(rect, text, hint);
            y -= rect.height + 10f;
        }
    }
}
