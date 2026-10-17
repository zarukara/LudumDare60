using UISystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PauseSystem
{
    /// <summary>
    /// The VCR-style settings screen while the game is paused (<see cref="GamePause"/>): mouse sensitivity and volume
    /// as rows of OSD segments over the darkened still picture. The mouse clicks or drags a row, hovering picks it;
    /// the arrows or a stick pick a row (up/down) and change it (left/right). A blinking ► marks the chosen row.
    /// Values go to <see cref="GameSettings"/> at once. Built in code.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        const int Levels = 20;
        const float Left = -430f, Right = 430f;

        class Row
        {
            public RectTransform area;
            public Slider slider;
            public Text label, value, cursor;
            public Image[] segments;
        }

        GameObject screen;
        Row sensitivity, volume;
        bool shown;

        void Start()
        {
            VhsUi.EnsureEventSystem();
            Build();
            screen.SetActive(false);
        }

        void Update()
        {
            if (GamePause.Paused != shown) Show(GamePause.Paused);
            if (!shown) return;

            var events = EventSystem.current;
            foreach (var row in new[] { sensitivity, volume })
                if (events != null && VhsUi.Hovered(row.area)) events.SetSelectedGameObject(row.slider.gameObject);
            var selected = events != null ? events.currentSelectedGameObject : null;
            // Nothing is chosen until the mouse comes or a direction is pressed anew: keys still held from walking
            // when the game was paused must not slide a level.
            if (selected == null && events != null && VhsUi.NavigationPressed())
                events.SetSelectedGameObject(selected = sensitivity.slider.gameObject);
            foreach (var row in new[] { sensitivity, volume })
            {
                bool chosen = selected == row.slider.gameObject;
                row.cursor.enabled = chosen && VhsUi.BlinkOn;
                row.label.color = chosen ? VhsUi.Bright : VhsUi.Faded;
            }
        }

        void Show(bool show)
        {
            shown = show;
            screen.SetActive(show);
            var events = EventSystem.current;
            if (show)
            {
                sensitivity.slider.SetValueWithoutNotify(SensitivityLevel(GameSettings.MouseSensitivity));
                volume.slider.SetValueWithoutNotify(Mathf.Round(GameSettings.Volume * Levels));
                Refresh();
                if (events != null) events.SetSelectedGameObject(null);
            }
            else
            {
                if (events != null) events.SetSelectedGameObject(null);
                GameSettings.Save();
            }
        }

        // ------------------------------------------------------------------ values

        // 21 levels, the middle one as designed: each 5 levels double or halve the speed (0.25 to 4).
        static float Sensitivity(float level) => Mathf.Pow(4f, (level - Levels / 2f) / (Levels / 2f));

        static float SensitivityLevel(float multiplier) =>
            Mathf.Clamp(Mathf.Round(Levels / 2f + Levels / 2f * Mathf.Log(multiplier, 4f)), 0f, Levels);

        void Refresh()
        {
            ShowValue(sensitivity, $"{Sensitivity(sensitivity.slider.value):0.00}×");
            ShowValue(volume, $"{Mathf.RoundToInt(volume.slider.value * 100f / Levels)}%");
        }

        static void ShowValue(Row row, string value)
        {
            VhsUi.LightUp(row.segments, Mathf.RoundToInt(row.slider.value));
            row.value.text = value;
        }

        // ------------------------------------------------------------------ layout

        void Build()
        {
            var canvas = VhsUi.NewCanvas("Pause_Canvas", transform, 100);
            screen = canvas.gameObject;
            VhsUi.Label(canvas, "pause.title", null, 64, TextAnchor.MiddleCenter, new Vector2(0f, 250f), VhsUi.Bright);
            sensitivity = NewRow(canvas, "pause.sensitivity", 120f, value =>
            {
                GameSettings.MouseSensitivity = Sensitivity(value);
                Refresh();
            });
            volume = NewRow(canvas, "pause.volume", -60f, value =>
            {
                GameSettings.Volume = value / Levels;
                Refresh();
            });
            Link(sensitivity.slider, volume.slider);
            VhsUi.Label(canvas, "pause.resume", null, 34, TextAnchor.MiddleCenter, new Vector2(0f, -250f), VhsUi.Faded);
        }

        /// <summary>The name and value on one line, the segments under it; the whole block is the hover area.</summary>
        static Row NewRow(RectTransform parent, string key, float top, UnityEngine.Events.UnityAction<float> changed)
        {
            var row = new Row();
            row.area = new GameObject(key, typeof(RectTransform)).GetComponent<RectTransform>();
            row.area.SetParent(parent, false);
            row.area.anchorMin = row.area.anchorMax = new Vector2(0.5f, 0.5f);
            row.area.pivot = new Vector2(0.5f, 1f);
            row.area.anchoredPosition = new Vector2(0f, top);
            row.area.sizeDelta = new Vector2(Right - Left + 120f, 130f);

            row.cursor = VhsUi.Label(parent, null, "►", 42, TextAnchor.UpperRight, new Vector2(Left - 18f, top), VhsUi.Accent);
            row.label = VhsUi.Label(parent, key, null, 42, TextAnchor.UpperLeft, new Vector2(Left, top), VhsUi.Faded);
            row.value = VhsUi.Label(parent, null, "", 42, TextAnchor.UpperRight, new Vector2(Right, top), VhsUi.Bright);

            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image), typeof(Slider)).GetComponent<RectTransform>();
            bar.SetParent(parent, false);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
            bar.pivot = new Vector2(0f, 1f);
            bar.anchoredPosition = new Vector2(Left, top - 64f);
            bar.sizeDelta = new Vector2(Right - Left, 46f);
            bar.GetComponent<Image>().color = Color.clear; // catches clicks and drags on the whole bar
            row.segments = VhsUi.Segments(bar, Levels, 8f);

            // The slider only needs a fill to measure the bar by; the segments show the value.
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(bar, false);
            VhsUi.Stretch(fillArea);
            var fill = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
            fill.SetParent(fillArea, false);
            VhsUi.Stretch(fill);

            row.slider = bar.GetComponent<Slider>();
            row.slider.transition = Selectable.Transition.None;
            row.slider.fillRect = fill;
            row.slider.direction = Slider.Direction.LeftToRight;
            row.slider.minValue = 0f;
            row.slider.maxValue = Levels;
            row.slider.wholeNumbers = true;
            row.slider.onValueChanged.AddListener(changed);
            return row;
        }

        /// <summary>Up and down go from one row to the other; left and right stay with the slider.</summary>
        static void Link(Slider top, Slider bottom)
        {
            top.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = bottom, selectOnDown = bottom };
            bottom.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = top, selectOnDown = top };
        }
    }
}
