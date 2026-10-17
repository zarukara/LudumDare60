using LocalizationSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Building blocks for the menus in the look of a VCR's on-screen display: bold white letters with a drop shadow
    /// and a faint red/blue colour fringe, a blinking green ► at the chosen line, and rows of segments for levels.
    /// Sizes are in a 1920×1080 reference canvas; positions are from the parent's centre unless anchored otherwise.
    /// </summary>
    public static class VhsUi
    {
        public static readonly Color Bright = new Color(0.95f, 0.97f, 0.95f);
        public static readonly Color Faded = new Color(0.95f, 0.97f, 0.95f, 0.5f);
        public static readonly Color Accent = new Color(0.45f, 1f, 0.55f);
        public static readonly Color Unlit = new Color(0.32f, 0.36f, 0.32f, 0.55f);

        static Font font;

        public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>On and off like a VCR menu cursor; runs on real time, so it blinks while the game is paused.</summary>
        public static bool BlinkOn => Time.unscaledTime % 0.8f < 0.55f;

        /// <summary>A screen-space canvas scaled from 1920×1080 by height, drawn above canvases with a lower order.</summary>
        public static RectTransform NewCanvas(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            return (RectTransform)go.transform;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        /// <summary>A plain rectangle (no sprite) filling its parent.</summary>
        public static Image Fill(Transform parent, string name, Color color)
        {
            var image = new GameObject(name, typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            Stretch(image.rectTransform);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// One line of OSD text: a localization <paramref name="key"/> or a fixed <paramref name="text"/>. The
        /// alignment also sets the pivot, so <paramref name="position"/> is where that corner or edge goes.
        /// </summary>
        public static Text Label(Transform parent, string key, string text, int size, TextAnchor alignment, Vector2 position, Color color)
        {
            var label = new GameObject(key ?? text, typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = Pivot(alignment);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(1500f, size * 1.5f);
            label.font = Font;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (key != null)
            {
                var localized = label.gameObject.AddComponent<LocalizedText>();
                localized.key = key;
                localized.Apply();
            }
            else
            {
                label.text = text;
            }
            Osd(label.gameObject, size);
            return label;
        }

        /// <summary>The tape look on any graphic: red and blue ghosts either side and a dark drop shadow.</summary>
        public static void Osd(GameObject graphic, float size)
        {
            float fringe = Mathf.Max(1f, size / 26f);
            AddShadow(graphic, new Color(1f, 0.15f, 0.2f, 0.35f), new Vector2(-fringe, 0f));
            AddShadow(graphic, new Color(0.2f, 0.55f, 1f, 0.35f), new Vector2(fringe, 0f));
            AddShadow(graphic, new Color(0f, 0f, 0f, 0.75f), new Vector2(fringe * 1.5f, -fringe * 1.5f));
        }

        static void AddShadow(GameObject graphic, Color color, Vector2 offset)
        {
            var shadow = graphic.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = offset;
        }

        /// <summary>A row of <paramref name="count"/> segments left to right inside <paramref name="bar"/>.</summary>
        public static Image[] Segments(RectTransform bar, int count, float gap)
        {
            var segments = new Image[count];
            float width = (bar.sizeDelta.x - gap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                var segment = new GameObject("Segment_" + i, typeof(Image)).GetComponent<Image>();
                segment.transform.SetParent(bar, false);
                var rect = segment.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(width, 0f);
                rect.anchoredPosition = new Vector2(i * (width + gap), 0f);
                segment.raycastTarget = false;
                AddShadow(segment.gameObject, new Color(0f, 0f, 0f, 0.75f), new Vector2(2f, -2f));
                segments[i] = segment;
            }
            return segments;
        }

        /// <summary>Lights the first <paramref name="lit"/> segments.</summary>
        public static void LightUp(Image[] segments, int lit)
        {
            for (int i = 0; i < segments.Length; i++) segments[i].color = i < lit ? Accent : Unlit;
        }

        /// <summary>An arrow, WASD, d-pad or stick direction pressed just now (not held from before).</summary>
        public static bool NavigationPressed()
        {
            var k = Keyboard.current;
            var pad = Gamepad.current;
            return (k != null && (k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame
                                  || k.rightArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame
                                  || k.aKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame))
                   || (pad != null && (pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame || pad.dpad.left.wasPressedThisFrame
                                       || pad.dpad.right.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame
                                       || pad.leftStick.down.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame
                                       || pad.leftStick.right.wasPressedThisFrame));
        }

        /// <summary>True while the mouse is over <paramref name="rect"/> and has just moved (hovering picks a line).</summary>
        public static bool Hovered(RectTransform rect)
        {
            var mouse = Mouse.current;
            if (mouse == null || mouse.delta.ReadValue().sqrMagnitude < 0.01f) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, mouse.position.ReadValue(), null);
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Vector2 Pivot(TextAnchor alignment) => alignment switch
        {
            TextAnchor.UpperLeft => new Vector2(0f, 1f),
            TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
            TextAnchor.UpperRight => new Vector2(1f, 1f),
            TextAnchor.MiddleLeft => new Vector2(0f, 0.5f),
            TextAnchor.MiddleRight => new Vector2(1f, 0.5f),
            TextAnchor.LowerLeft => new Vector2(0f, 0f),
            TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
            TextAnchor.LowerRight => new Vector2(1f, 0f),
            _ => new Vector2(0.5f, 0.5f),
        };
    }
}
