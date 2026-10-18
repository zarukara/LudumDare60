using System.Collections;
using System.Collections.Generic;
using CameraSystem;
using PlayerSystem;
using UnityEngine;

namespace ScareSystem
{
    /// <summary>
    /// The camcorder's tape. While recording it keeps where the player was and how they looked, with marks at chosen
    /// moments (<see cref="Mark"/>). <see cref="Rewind"/> runs it backwards fast, the picture in rewind
    /// (<see cref="VhsEffect.Rewinding"/>), back to a mark; recording then goes on from there over what came after
    /// it, like a tape recorded over.
    /// </summary>
    public class TapeRewind : MonoBehaviour
    {
        public PlayerController player;
        [Tooltip("How many times faster than it was recorded the tape runs back.")]
        public float speed = 14f;
        [Tooltip("Shortest and longest rewind, seconds.")]
        public Vector2 length = new Vector2(2f, 4f);

        struct Frame
        {
            public float time, yaw, pitch, drop;
            public Vector3 position;
        }

        readonly List<Frame> frames = new List<Frame>();
        readonly Dictionary<int, int> marks = new Dictionary<int, int>();
        bool recording;
        float clock;

        public bool Rewinding { get; private set; }

        /// <summary>Starts a new tape from here.</summary>
        public void Begin()
        {
            frames.Clear();
            marks.Clear();
            clock = 0f;
            Rewinding = false;
            recording = player != null;
            if (recording) frames.Add(Now());
        }

        public void Stop() => recording = false;

        public bool HasMark(int key) => marks.ContainsKey(key);

        /// <summary>Marks this moment under a key (a floor), replacing an older mark with that key.</summary>
        public void Mark(int key)
        {
            if (!recording) return;
            frames.Add(Now());
            marks[key] = frames.Count - 1;
        }

        void LateUpdate()
        {
            if (!recording || Rewinding || player == null) return;
            clock += Time.deltaTime;
            var now = Now();
            var last = frames[frames.Count - 1];
            bool still = (now.position - last.position).sqrMagnitude < 0.0004f && Mathf.Abs(Mathf.DeltaAngle(now.yaw, last.yaw)) < 0.5f
                         && Mathf.Abs(now.pitch - last.pitch) < 0.5f && Mathf.Abs(now.drop - last.drop) < 0.01f;
            if (!still || now.time - last.time > 0.2f) frames.Add(now);
        }

        Frame Now() => new Frame
        {
            time = clock,
            position = player.transform.position,
            yaw = player.transform.eulerAngles.y,
            pitch = player.Pitch,
            drop = player.headDrop,
        };

        /// <summary>
        /// Runs the tape back to the mark (the start of the tape without one) and leaves the player there. Their
        /// walking and looking stay as the caller set them.
        /// </summary>
        public IEnumerator Rewind(int key)
        {
            if (frames.Count == 0 || player == null) yield break;
            int target = marks.TryGetValue(key, out int mark) ? Mathf.Clamp(mark, 0, frames.Count - 1) : 0;
            Rewinding = true;
            VhsEffect.Rewinding = true;

            float from = frames[frames.Count - 1].time, to = frames[target].time;
            float seconds = Mathf.Clamp((from - to) / speed, length.x, length.y);
            int index = frames.Count - 1;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float u = Mathf.Clamp01(t / seconds);
                float time = Mathf.Lerp(from, to, u * u * (3f - 2f * u)); // the reels speed up, then brake
                while (index > target + 1 && frames[index - 1].time > time) index--;
                var a = frames[Mathf.Max(index - 1, target)];
                var b = frames[index];
                Apply(a, b, b.time > a.time ? Mathf.InverseLerp(a.time, b.time, time) : 0f);
                yield return null;
            }
            Apply(frames[target], frames[target], 0f);

            frames.RemoveRange(target + 1, frames.Count - target - 1);
            var later = new List<int>();
            foreach (var pair in marks)
                if (pair.Value > target) later.Add(pair.Key);
            foreach (int k in later) marks.Remove(k);
            clock = frames[target].time;
            VhsEffect.Rewinding = false;
            Rewinding = false;
        }

        void Apply(Frame a, Frame b, float u)
        {
            var body = player.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
            player.transform.SetPositionAndRotation(Vector3.Lerp(a.position, b.position, u), Quaternion.Euler(0f, Mathf.LerpAngle(a.yaw, b.yaw, u), 0f));
            if (body != null) body.enabled = true;
            player.Pitch = Mathf.Lerp(a.pitch, b.pitch, u);
            player.headDrop = Mathf.Lerp(a.drop, b.drop, u);
        }

        void OnDisable()
        {
            if (Rewinding) VhsEffect.Rewinding = false;
            Rewinding = false;
        }
    }
}
