using LocalizationSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MenuSystem
{
    /// <summary>
    /// Start menu, built in code on start: title, Play, the language (Русский / English, remembered between runs)
    /// and Quit. Play loads <see cref="gameScene"/>.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public string gameScene = "Office";
        [Tooltip("The game's name, the same in every language.")]
        public string title = "The INNO";

        static readonly Color Background = new Color(0.025f, 0.03f, 0.035f);
        static readonly Color ButtonColor = new Color(0.13f, 0.14f, 0.15f);
        static readonly Color Selected = new Color(0.78f, 0.8f, 0.74f);

        Font font;
        Button russian, english;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Build();
            Localization.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() => Localization.Changed -= Refresh;

        void Build()
        {
            var canvasObject = new GameObject("Menu_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            var root = canvasObject.transform;

            var background = new GameObject("Background", typeof(Image)).GetComponent<Image>();
            background.transform.SetParent(root, false);
            Stretch(background.rectTransform);
            background.color = Background;

            Label(root, null, 132, FontStyle.Bold, new Vector2(0f, 260f), new Vector2(1400f, 170f), new Color(0.9f, 0.92f, 0.88f), title);

            Button(root, "menu.play", new Vector2(0f, 40f), new Vector2(420f, 90f), 44, () => SceneManager.LoadScene(gameScene));
            Label(root, "menu.language", 28, FontStyle.Normal, new Vector2(0f, -70f), new Vector2(600f, 50f), new Color(0.6f, 0.63f, 0.6f));
            russian = Button(root, null, new Vector2(-110f, -140f), new Vector2(200f, 70f), 32, () => Localization.Current = Language.Russian, "Русский");
            english = Button(root, null, new Vector2(110f, -140f), new Vector2(200f, 70f), 32, () => Localization.Current = Language.English, "English");
            Button(root, "menu.quit", new Vector2(0f, -260f), new Vector2(420f, 80f), 36, Quit);
        }

        /// <summary>Marks the chosen language.</summary>
        void Refresh()
        {
            Mark(russian, Localization.Current == Language.Russian);
            Mark(english, Localization.Current == Language.English);
        }

        static void Mark(Button button, bool chosen)
        {
            if (button == null) return;
            button.GetComponent<Image>().color = chosen ? Selected : ButtonColor;
            button.GetComponentInChildren<Text>().color = chosen ? Background : Color.white;
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        Text Label(Transform parent, string key, int size, FontStyle style, Vector2 position, Vector2 box, Color color, string fixedText = null)
        {
            var text = new GameObject(key ?? fixedText, typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = box;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            if (key != null)
            {
                var localized = text.gameObject.AddComponent<LocalizedText>();
                localized.key = key;
                localized.Apply(); // OnEnable already ran without a key
            }
            else
            {
                text.text = fixedText;
            }
            return text;
        }

        Button Button(Transform parent, string key, Vector2 position, Vector2 box, int size, UnityEngine.Events.UnityAction onClick, string fixedText = null)
        {
            var image = new GameObject("Button_" + (key ?? fixedText), typeof(Image), typeof(Button)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = box;
            image.color = ButtonColor;
            var button = image.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;
            button.onClick.AddListener(onClick);
            var label = Label(image.transform, key, size, FontStyle.Normal, Vector2.zero, box, Color.white, fixedText);
            Stretch(label.rectTransform);
            return button;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
