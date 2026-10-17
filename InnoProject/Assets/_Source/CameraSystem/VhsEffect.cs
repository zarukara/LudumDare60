using PauseSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CameraSystem
{
    /// <summary>
    /// Makes the camera look like a played-back VHS tape. The camera draws into a texture that a full-screen
    /// RawImage shows through the "Office/VHS" material, with the VCR's "PLAY" / "PAUSE" mark, the pause key under
    /// it and a running tape counter on top; an empty camera clears the screen behind it. Only this camera is
    /// affected, no render pipeline asset changes. Runs in Play Mode only.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class VhsEffect : MonoBehaviour
    {
        public Material material;
        [Range(0f, 1f)] public float strength = 1f;
        [Tooltip("The tape picture is rendered smaller and stretched, which also makes it soft.")]
        [Range(0.3f, 1f)] public float renderScale = 0.9f;
        public bool showOverlay = true;
        public string date = "17.10.1996";

        static readonly int Strength = Shader.PropertyToID("_Strength");
        static readonly Color OsdColor = new Color(0.95f, 0.97f, 0.95f, 0.9f);
        static readonly Color KeyColor = new Color(0.95f, 0.97f, 0.95f, 0.7f);
        const float Left = 80f, ModeTop = -60f, KeyTop = -128f;

        Camera source, output;
        RenderTexture frame;
        Material instance;
        GameObject canvasObject;
        RawImage image;
        Text mode, key, counter;
        RectTransform modeSign, keySign;
        float started;
        bool shownPaused;

        void OnEnable()
        {
            source = GetComponent<Camera>();
            if (material == null)
            {
                enabled = false;
                return;
            }
            instance = new Material(material) { name = material.name + " (Instance)" };
            started = Time.time;
            BuildOutput();
            Resize();
        }

        void OnDisable()
        {
            if (source != null) source.targetTexture = null;
            if (frame != null)
            {
                frame.Release();
                Destroy(frame);
            }
            if (canvasObject != null) Destroy(canvasObject);
            if (output != null) Destroy(output.gameObject);
            if (instance != null) Destroy(instance);
        }

        void LateUpdate()
        {
            if (frame == null || frame.width != Width || frame.height != Height) Resize();
            instance.SetFloat(Strength, strength);
            if (GamePause.Paused != shownPaused) ShowPause(GamePause.Paused);
            if (counter != null)
            {
                int seconds = Mathf.FloorToInt(Time.time - started);
                counter.text = $"{date}    SP {seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}";
            }
        }

        int Width => Mathf.Max(64, Mathf.RoundToInt(Screen.width * renderScale));
        int Height => Mathf.Max(64, Mathf.RoundToInt(Screen.height * renderScale));

        void Resize()
        {
            source.targetTexture = null;
            if (frame != null)
            {
                frame.Release();
                Destroy(frame);
            }
            frame = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "VHS_Frame",
                filterMode = FilterMode.Bilinear,
            };
            frame.Create();
            source.targetTexture = frame;
            image.texture = frame;
        }

        void BuildOutput()
        {
            var cameraObject = new GameObject("VHS_Output_Camera") { hideFlags = HideFlags.DontSave };
            output = cameraObject.AddComponent<Camera>();
            output.cullingMask = 0;
            output.clearFlags = CameraClearFlags.SolidColor;
            output.backgroundColor = Color.black;
            output.depth = source.depth + 1f;
            output.orthographic = true;
            output.useOcclusionCulling = false;

            canvasObject = new GameObject("VHS_Canvas", typeof(Canvas), typeof(CanvasScaler)) { hideFlags = HideFlags.DontSave };
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100; // any game UI goes on top
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            image = new GameObject("Frame", typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(canvasObject.transform, false);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.material = instance;
            image.raycastTarget = false;

            if (!showOverlay) return;
            // Top left: the VCR mode and, under it, the key that pauses the game.
            mode = Label("", TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Left, ModeTop), 46, OsdColor);
            modeSign = PauseSign(32f, OsdColor, 3f);
            key = Label("Tab", TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Left, KeyTop), 30, KeyColor);
            keySign = PauseSign(21f, KeyColor, 2f);
            counter = Label("", TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(Left, 60f), 46, OsdColor);
            ShowPause(false);
        }

        /// <summary>
        /// "PLAY ►" with "❚❚ Tab" under it, or while paused "PAUSE ❚❚" with "► Tab" and the still picture faded
        /// almost to black under the settings.
        /// </summary>
        void ShowPause(bool paused)
        {
            shownPaused = paused;
            image.color = paused ? new Color(0.22f, 0.22f, 0.22f) : Color.white;
            if (mode == null) return;
            mode.text = paused ? "PAUSE" : "PLAY ►";
            modeSign.gameObject.SetActive(paused);
            modeSign.anchoredPosition = new Vector2(Left + mode.preferredWidth + 16f, ModeTop - 9f);
            key.text = paused ? "► Tab" : "Tab";
            key.rectTransform.anchoredPosition = new Vector2(paused ? Left : Left + keySign.sizeDelta.x + 12f, KeyTop);
            keySign.gameObject.SetActive(!paused);
            keySign.anchoredPosition = new Vector2(Left, KeyTop - 6f);
        }

        Text Label(string text, TextAnchor alignment, Vector2 anchor, Vector2 offset, int size, Color color)
        {
            var label = new GameObject("OSD", typeof(Text), typeof(Shadow)).GetComponent<Text>();
            label.transform.SetParent(canvasObject.transform, false);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(900f, 80f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.text = text;
            var shadow = label.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(size / 15f, -size / 15f);
            return label;
        }

        /// <summary>The pause sign, two upright bars of the given height, anchored top left like the labels.</summary>
        RectTransform PauseSign(float height, Color color, float shadowDistance)
        {
            var sign = new GameObject("PauseSign", typeof(RectTransform)).GetComponent<RectTransform>();
            sign.SetParent(canvasObject.transform, false);
            sign.anchorMin = sign.anchorMax = sign.pivot = new Vector2(0f, 1f);
            sign.sizeDelta = new Vector2(height * 0.8f, height);
            for (int i = 0; i < 2; i++)
            {
                var bar = new GameObject("Bar", typeof(Image), typeof(Shadow)).GetComponent<Image>();
                bar.transform.SetParent(sign, false);
                bar.rectTransform.anchorMin = new Vector2(i * 0.62f, 0f);
                bar.rectTransform.anchorMax = new Vector2(i * 0.62f + 0.38f, 1f);
                bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
                bar.color = color;
                bar.raycastTarget = false;
                var shadow = bar.GetComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
                shadow.effectDistance = new Vector2(shadowDistance, -shadowDistance);
            }
            return sign;
        }
    }
}
