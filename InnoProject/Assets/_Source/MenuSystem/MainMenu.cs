using LocalizationSystem;
using UISystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MenuSystem
{
    /// <summary>
    /// Start menu in the look of a VCR's on-screen display on a black screen, built in code on start: the title,
    /// Play and the language (Русский / English, remembered between runs), with "STOP ■" and the tape's date in the
    /// corners. A blinking ► marks the chosen line. The mouse picks lines by hovering and clicks them; arrows or a
    /// stick and Enter work too, left and right switch the language. Play loads <see cref="gameScene"/>.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public string gameScene = "Office";
        [Tooltip("The game's name, the same in every language.")]
        public string title = "The INNOffice";
        [Tooltip("The tape's date in the corner, as in the game.")]
        public string date = "17.10.1996";

        const float Left = -360f;
        const int ItemSize = 52;

        Button play, language;
        Text playLabel, languageLabel, languageValue, cursor;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            VhsUi.EnsureEventSystem();
            Build();
            Localization.Changed += Refresh;
            Refresh();
            EventSystem.current.SetSelectedGameObject(play.gameObject);
        }

        void OnDestroy() => Localization.Changed -= Refresh;

        void Update()
        {
            var events = EventSystem.current;
            if (events == null) return;
            foreach (var item in new[] { play, language })
                if (VhsUi.Hovered((RectTransform)item.transform)) events.SetSelectedGameObject(item.gameObject);
            var selected = events.currentSelectedGameObject;
            if (selected == null && VhsUi.NavigationPressed())
                events.SetSelectedGameObject(selected = play.gameObject); // a click on empty space must not lose the keys
            if (selected == language.gameObject && SidePressed()) ToggleLanguage();

            bool onLanguage = selected == language.gameObject;
            cursor.enabled = selected != null && VhsUi.BlinkOn;
            cursor.rectTransform.anchoredPosition = new Vector2(cursor.rectTransform.anchoredPosition.x, (onLanguage ? languageLabel : playLabel).rectTransform.anchoredPosition.y);
            playLabel.color = selected == play.gameObject ? VhsUi.Bright : VhsUi.Faded;
            languageLabel.color = languageValue.color = onLanguage ? VhsUi.Bright : VhsUi.Faded;
        }

        static bool SidePressed()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            return (keyboard != null && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame
                                         || keyboard.aKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame))
                   || (pad != null && (pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame
                                       || pad.leftStick.left.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame));
        }

        static void ToggleLanguage() =>
            Localization.Current = Localization.Current == Language.Russian ? Language.English : Language.Russian;

        void Refresh() => languageValue.text = Localization.Current == Language.Russian ? "◄ РУССКИЙ ►" : "◄ ENGLISH ►";

        // ------------------------------------------------------------------ layout

        void Build()
        {
            var canvas = VhsUi.NewCanvas("Menu_Canvas", transform, 0);
            VhsUi.Fill(canvas, "Black", Color.black);
            Corner(canvas, "STOP ■", new Vector2(0f, 1f), new Vector2(80f, -60f), TextAnchor.UpperLeft);
            Corner(canvas, $"{date}    SP 0:00:00", new Vector2(0f, 0f), new Vector2(80f, 60f), TextAnchor.LowerLeft);

            VhsUi.Label(canvas, null, title, 132, TextAnchor.MiddleCenter, new Vector2(0f, 230f), VhsUi.Bright);
            play = Item(canvas, "menu.play", 10f, out playLabel, () => SceneManager.LoadScene(gameScene));
            language = Item(canvas, "menu.language", -90f, out languageLabel, ToggleLanguage);
            languageValue = VhsUi.Label(canvas, null, "", ItemSize, TextAnchor.MiddleLeft, new Vector2(Left + 340f, -90f), VhsUi.Faded);
            cursor = VhsUi.Label(canvas, null, "►", ItemSize, TextAnchor.MiddleRight, new Vector2(Left - 24f, 10f), VhsUi.Accent);

            play.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = language, selectOnDown = language };
            language.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = play, selectOnDown = play };
        }

        /// <summary>OSD text in a corner of the screen, like on the tape in the game.</summary>
        static void Corner(RectTransform canvas, string text, Vector2 corner, Vector2 offset, TextAnchor alignment)
        {
            var label = VhsUi.Label(canvas, null, text, 46, alignment, offset, VhsUi.Bright);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = corner;
            label.rectTransform.anchoredPosition = offset;
        }

        /// <summary>A menu line: the text, and an invisible button over the whole line for the mouse and the keys.</summary>
        static Button Item(RectTransform canvas, string key, float y, out Text label, UnityEngine.Events.UnityAction onClick)
        {
            var area = new GameObject("Item_" + key, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            area.SetParent(canvas, false);
            area.anchorMin = area.anchorMax = new Vector2(0.5f, 0.5f);
            area.pivot = new Vector2(0f, 0.5f);
            area.anchoredPosition = new Vector2(Left - 80f, y);
            area.sizeDelta = new Vector2(880f, 90f);
            area.GetComponent<Image>().color = Color.clear;
            var button = area.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            label = VhsUi.Label(canvas, key, null, ItemSize, TextAnchor.MiddleLeft, new Vector2(Left, y), VhsUi.Faded);
            return button;
        }
    }
}
