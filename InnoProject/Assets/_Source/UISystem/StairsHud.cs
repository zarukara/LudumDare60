using System.Collections.Generic;
using LocalizationSystem;
using PauseSystem;
using PlayerSystem;
using ScareSystem;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// The stairwell's on-screen bits in the VCR look (<see cref="VhsUi"/>): what E would do now (hide behind the
    /// cart, get out), the hunt's hints one under another, and the hold-your-breath bar: a row of segments with the
    /// lit zone and the running marker to catch with Space, or, while the breath is held, how much of it is left.
    /// </summary>
    public class StairsHud : MonoBehaviour
    {
        public StairsHunt hunt;
        public PlayerHide hide;

        const int SegmentCount = 41;
        static readonly Color MarkerColor = new Color(1f, 0.95f, 0.75f);
        static readonly Color HeldColor = new Color(0.45f, 1f, 0.55f, 0.45f);
        static readonly Color HurryColor = new Color(1f, 0.35f, 0.3f);

        RectTransform bar;
        Text prompt, hint, caption;
        Image[] segments;
        string shownPrompt, shownCaption;
        readonly List<string> shownHints = new List<string>();

        void Start()
        {
            var root = VhsUi.NewCanvas("Stairs_HUD", transform, 20);
            hint = VhsUi.Label(root, null, "", 46, TextAnchor.UpperCenter, new Vector2(0f, -120f), VhsUi.Bright);
            hint.verticalOverflow = VerticalWrapMode.Overflow; // a line per hint
            caption = VhsUi.Label(root, null, "", 32, TextAnchor.MiddleCenter, new Vector2(0f, -292f), VhsUi.Bright);
            bar = new GameObject("Breath_Bar", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(root, false);
            bar.sizeDelta = new Vector2(960f, 38f);
            bar.anchoredPosition = new Vector2(0f, -345f);
            segments = VhsUi.Segments(bar, SegmentCount, 5f);
            bar.gameObject.SetActive(false);
            prompt = VhsUi.Label(root, null, "", 30, TextAnchor.MiddleCenter, new Vector2(0f, -420f), VhsUi.Bright);
            Localization.Changed += Relabel;
        }

        void OnDestroy() => Localization.Changed -= Relabel;

        void Relabel()
        {
            shownPrompt = shownCaption = null;
            shownHints.Clear();
            if (hint != null) hint.text = "";
        }

        void LateUpdate()
        {
            if (bar == null) return;
            bool show = !GamePause.Paused;
            Show(prompt, show && hide != null ? hide.PromptKey : null, ref shownPrompt);
            ShowHints(show && hunt != null ? hunt.Hints : null);
            hint.color = new Color(1f, 1f, 1f, VhsUi.BlinkOn ? 1f : 0.45f);

            var breath = show && hunt != null ? hunt.Breath : null;
            bar.gameObject.SetActive(breath != null);
            Show(caption, breath == null ? null : breath.State == BreathHold.Phase.Hold ? "hint.holding" : "hint.breath", ref shownCaption);
            if (breath != null) Paint(breath);
        }

        static void Show(Text label, string key, ref string shown)
        {
            if (key == shown) return;
            shown = key;
            label.text = key != null ? Localization.Get(key) : "";
        }

        /// <summary>The hints one per line; the text is rebuilt only when they change.</summary>
        void ShowHints(IReadOnlyList<string> keys)
        {
            int count = keys != null ? keys.Count : 0;
            bool same = count == shownHints.Count;
            for (int i = 0; same && i < count; i++) same = keys[i] == shownHints[i];
            if (same) return;
            shownHints.Clear();
            var lines = new List<string>();
            for (int i = 0; i < count; i++)
            {
                shownHints.Add(keys[i]);
                lines.Add(Localization.Get(keys[i]));
            }
            hint.text = string.Join("\n", lines);
        }

        /// <summary>Catching: the zone green, the marker bright (the zone blinks red when time is nearly up). Holding: the breath left.</summary>
        void Paint(BreathHold breath)
        {
            bool flash = Time.time - breath.LastCatch < 0.15f;
            bool hurry = breath.State == BreathHold.Phase.Catch && breath.TimeLeft < 0.7f && !VhsUi.BlinkOn;
            int marker = Mathf.Clamp(Mathf.FloorToInt(breath.Marker * SegmentCount), 0, SegmentCount - 1);
            for (int i = 0; i < SegmentCount; i++)
            {
                float u = (i + 0.5f) / SegmentCount;
                Color color;
                if (flash) color = VhsUi.Bright;
                else if (breath.State == BreathHold.Phase.Hold) color = u <= breath.HoldShare ? HeldColor : VhsUi.Unlit;
                else if (i == marker) color = MarkerColor;
                else if (Mathf.Abs(u - breath.ZoneCenter) <= breath.ZoneWidth / 2f) color = hurry ? HurryColor : VhsUi.Accent;
                else color = VhsUi.Unlit;
                segments[i].color = color;
            }
        }
    }
}
