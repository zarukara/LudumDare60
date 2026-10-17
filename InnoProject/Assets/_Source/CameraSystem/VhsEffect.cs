using UnityEngine;
using UnityEngine.UI;

namespace CameraSystem
{
    /// <summary>
    /// Makes the camera look like a played-back VHS tape. The camera draws into a texture that a full-screen
    /// RawImage shows through the "Office/VHS" material, with the VCR's "PLAY" mark and a running tape counter on
    /// top; an empty camera clears the screen behind it. Only this camera is affected, no render pipeline asset
    /// changes. Runs in Play Mode only.
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

        Camera source, output;
        RenderTexture frame;
        Material instance;
        GameObject canvasObject;
        RawImage image;
        Text counter;
        float started;

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
            Label("PLAY ►", TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(80f, -60f));
            counter = Label("", TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(80f, 60f));
        }

        Text Label(string text, TextAnchor alignment, Vector2 anchor, Vector2 offset)
        {
            var label = new GameObject("OSD", typeof(Text), typeof(Shadow)).GetComponent<Text>();
            label.transform.SetParent(canvasObject.transform, false);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(900f, 80f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 46;
            label.fontStyle = FontStyle.Bold;
            label.alignment = alignment;
            label.color = new Color(0.95f, 0.97f, 0.95f, 0.9f);
            label.raycastTarget = false;
            label.text = text;
            var shadow = label.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(3f, -3f);
            return label;
        }
    }
}
