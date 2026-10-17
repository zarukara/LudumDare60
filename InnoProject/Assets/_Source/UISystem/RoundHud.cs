using RoundSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UISystem
{
    /// <summary>
    /// Placeholder on-screen text until the real UI: the rules at the start of a run, the white-out and result at
    /// the exit, and a debug line (F3) with the progress and the active anomaly. The round number itself is on the
    /// sign in the corridor.
    /// </summary>
    public class RoundHud : MonoBehaviour
    {
        public RoundManager round;
        public float rulesSeconds = 16f;

        float rulesUntil, whiteOut;
        bool debug, finished;
        GUIStyle title, body, small;

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
            rulesUntil = Time.time + rulesSeconds;
            whiteOut = 0f;
            finished = false;
        }

        void OnExitReached() => finished = true;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame) debug = !debug;
            if (!finished) return;
            whiteOut = Mathf.MoveTowards(whiteOut, 1f, Time.deltaTime / 1.5f);
            if (whiteOut >= 1f && keyboard != null && keyboard.rKey.wasPressedThisFrame) round.Restart();
        }

        void OnGUI()
        {
            if (round == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                body = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
                small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft };
            }
            float scale = Screen.height / 1080f;
            title.fontSize = Mathf.RoundToInt(64 * scale);
            body.fontSize = Mathf.RoundToInt(24 * scale);
            small.fontSize = Mathf.RoundToInt(18 * scale);

            if (Time.time < rulesUntil && !finished)
            {
                float alpha = Mathf.Clamp01((rulesUntil - Time.time) / 1.5f);
                var box = new Rect(Screen.width * 0.18f, Screen.height * 0.72f, Screen.width * 0.64f, Screen.height * 0.22f);
                GUI.color = new Color(0f, 0f, 0f, 0.55f * alpha);
                GUI.DrawTexture(box, Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                string mistake = round.resetOnMistake ? "сбрасывает прогресс до нуля" : $"отнимает {round.mistakePenalty} раунда";
                GUI.Label(box,
                    "Осмотри офис. Если всё как обычно — иди вперёд, к дальней двери.\n" +
                    "Заметил старую технику, которой здесь быть не должно, — разворачивайся назад.\n" +
                    $"Ошибка {mistake}. Пройди {round.roundsToWin} раундов подряд.\n" +
                    "F — фонарик. Клик — захватить мышь, Esc — отпустить.", body);
            }

            if (debug)
            {
                string anomaly = round.CurrentAnomaly != null ? round.CurrentAnomaly.anomalyName : "нет";
                GUI.color = Color.yellow;
                GUI.Label(new Rect(12f, 10f, 900f, 60f), $"Прогресс {round.Progress}/{round.roundsToWin} · аномалия: {anomaly}", small);
            }

            if (finished)
            {
                GUI.color = new Color(1f, 1f, 1f, whiteOut);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                if (whiteOut >= 1f)
                {
                    GUI.color = new Color(0.12f, 0.12f, 0.14f);
                    int seconds = Mathf.FloorToInt(round.RunTime);
                    GUI.Label(new Rect(0f, Screen.height * 0.35f, Screen.width, Screen.height * 0.12f), "ФИНИШ", title);
                    GUI.Label(new Rect(0f, Screen.height * 0.48f, Screen.width, Screen.height * 0.1f),
                        $"Ты выбрался из офиса за {seconds / 60}:{seconds % 60:00}\n\nR — пройти заново", body);
                }
            }
            GUI.color = Color.white;
        }
    }
}
