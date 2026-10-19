using LocalizationSystem;
using PauseSystem;
using RoundSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISystem
{

    public class RoundHud : MonoBehaviour
    {
        public RoundManager round;
        public string menuScene = "Menu";
        [Tooltip("How close to the whiteboard counts as having read it.")]
        public float boardDistance = 3f;

        bool showFlashlightHint, showBoardHint, finished, debug;
        float whiteOut;
        GUIStyle hint, small;
        Image black;
        Text endTitle, endMenu;

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
            if (black != null) Destroy(black.canvas.gameObject);
            black = null;
        }

        void OnExitReached() => finished = true;

        void Update()
        {
            var keyboard = Keyboard.current;
            if ((Application.isEditor || Debug.isDebugBuild) && keyboard != null && keyboard.f3Key.wasPressedThisFrame) debug = !debug;
            if (GamePause.Paused) return;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame) showFlashlightHint = false;
            if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) showFlashlightHint = false;
            if (showBoardHint && NearBoard()) showBoardHint = false;

            if (!finished) return;
            if (black == null) BuildEndCard();
            whiteOut = Mathf.MoveTowards(whiteOut, 1f, Time.unscaledDeltaTime / 1.5f);
            black.color = new Color(0f, 0f, 0f, whiteOut);
            float words = Mathf.Clamp01((whiteOut - 0.8f) * 5f);
            endTitle.color = new Color(1f, 1f, 1f, words);
            endMenu.color = new Color(1f, 1f, 1f, words * (VhsUi.BlinkOn ? 1f : 0.15f));
            if (whiteOut < 1f) return;
            bool back = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                        || (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
            if (back && Application.CanStreamedLevelBeLoaded(menuScene)) SceneManager.LoadScene(menuScene);
        }

        void BuildEndCard()
        {
            var canvas = VhsUi.NewCanvas("End_Card", transform, 300);
            black = VhsUi.Fill(canvas, "Black", new Color(0f, 0f, 0f, 0f));
            var stop = VhsUi.Label(canvas, null, "STOP ■", 46, TextAnchor.UpperLeft, new Vector2(80f, -60f), VhsUi.Bright);
            stop.rectTransform.anchorMin = stop.rectTransform.anchorMax = new Vector2(0f, 1f);
            stop.rectTransform.anchoredPosition = new Vector2(80f, -60f);
            endTitle = VhsUi.Label(canvas, "end.title", null, 76, TextAnchor.MiddleCenter, new Vector2(0f, 50f), VhsUi.Bright);
            endMenu = VhsUi.Label(canvas, "end.menu", null, 36, TextAnchor.MiddleCenter, new Vector2(0f, -90f), VhsUi.Bright);
            stop.transform.SetAsLastSibling();
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
            if (hint == null)
            {
                hint = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft };
            }
            float scale = Screen.height / 1080f;
            hint.fontSize = Mathf.RoundToInt(28 * scale);
            small.fontSize = Mathf.RoundToInt(18 * scale);

            if (!finished && !GamePause.Paused)
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
            GUI.color = Color.white;
        }

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
