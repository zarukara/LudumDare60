using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoundSystem
{
    /// <summary>
    /// Named sound slots. Put audio files named after a slot into the bank's folder ("door_slam.wav",
    /// "door_bang_2.ogg", ...) and they land in that slot by themselves (SoundBankSync); several files in one slot
    /// play in random order. A slot without files plays the files of its fallback slot, its rough generated
    /// placeholder, or nothing.
    /// </summary>
    [CreateAssetMenu(menuName = "Sound/Sound Bank", fileName = "SoundBank")]
    public class SoundBank : ScriptableObject
    {
        public enum Placeholder
        {
            None,
            Thud,
            Slam,
            Scream,
            Heartbeat,
            Hiss,
            Drone,
        }

        [Serializable]
        public class Slot
        {
            public string name;
            [TextArea(1, 3)] public string description;
            public AudioClip[] clips = new AudioClip[0];
            [Range(0f, 1f)] public float volume = 1f;
            public Vector2 pitch = Vector2.one;
            [Tooltip("While the slot has no files, the files of this slot play instead (at this slot's volume).")]
            public string fallback;
            [Tooltip("Played while the slot has no files (and no fallback slot has any).")]
            public Placeholder placeholder;
        }

        public List<Slot> slots = new List<Slot>();

        static readonly Dictionary<Placeholder, AudioClip> Generated = new Dictionary<Placeholder, AudioClip>();
        readonly Dictionary<string, int> lastPick = new Dictionary<string, int>();

        public Slot Find(string slotName) => slots.Find(s => s.name == slotName);

        /// <summary>A random clip of the slot (never the same twice in a row), its fallback slot's, its placeholder, or null.</summary>
        public AudioClip Pick(string slotName, out Slot slot)
        {
            slot = Find(slotName);
            if (slot == null) return null;
            int count = 0;
            foreach (var c in slot.clips) if (c != null) count++;
            if (count == 0)
            {
                var other = string.IsNullOrEmpty(slot.fallback) || slot.fallback == slotName ? null : Find(slot.fallback);
                if (other != null && Array.Exists(other.clips, c => c != null)) return Pick(other.name, out _);
                return PlaceholderClip(slot.placeholder);
            }

            lastPick.TryGetValue(slotName, out int last);
            int pick = UnityEngine.Random.Range(0, count);
            if (count > 1 && pick == last) pick = (pick + 1) % count;
            lastPick[slotName] = pick;
            foreach (var c in slot.clips)
            {
                if (c == null) continue;
                if (pick-- == 0) return c;
            }
            return null;
        }

        /// <summary>One-shot from a point in the world. Returns the clip played, or null.</summary>
        public AudioClip PlayAt(string slotName, Vector3 position, float volume = 1f)
        {
            var clip = Pick(slotName, out var slot);
            if (clip == null) return null;
            var go = new GameObject("Sound_" + slotName);
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.minDistance = 2f;
            source.maxDistance = 30f;
            source.pitch = UnityEngine.Random.Range(slot.pitch.x, slot.pitch.y);
            source.PlayOneShot(clip, slot.volume * volume);
            Destroy(go, clip.length / Mathf.Max(0.1f, source.pitch) + 0.2f);
            return clip;
        }

        /// <summary>One-shot on a source (2D or 3D as the source is set up). Returns the clip played, or null.</summary>
        public AudioClip PlayOn(AudioSource source, string slotName, float volume = 1f)
        {
            if (source == null) return null;
            var clip = Pick(slotName, out var slot);
            if (clip == null) return null;
            source.pitch = UnityEngine.Random.Range(slot.pitch.x, slot.pitch.y);
            source.PlayOneShot(clip, slot.volume * volume);
            return clip;
        }

        /// <summary>A looping source on <paramref name="host"/> playing the slot, or null when the slot is silent.</summary>
        public AudioSource StartLoop(string slotName, GameObject host, bool spatial, float volume = 1f)
        {
            var clip = Pick(slotName, out var slot);
            if (clip == null || host == null) return null;
            var source = host.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = spatial ? 1f : 0f;
            source.minDistance = 2f;
            source.maxDistance = 25f;
            source.volume = slot.volume * volume;
            source.Play();
            return source;
        }

        // ------------------------------------------------------------------ placeholders

        static AudioClip PlaceholderClip(Placeholder kind)
        {
            if (kind == SoundBank.Placeholder.None) return null;
            if (Generated.TryGetValue(kind, out var clip) && clip != null) return clip;
            clip = Generate(kind);
            Generated[kind] = clip;
            return clip;
        }

        /// <summary>Rough synthesized stand-ins, so the scares work before the real sounds are chosen.</summary>
        static AudioClip Generate(Placeholder kind)
        {
            const int rate = 44100;
            var random = new System.Random((int)kind * 31 + 7);
            float Noise() => (float)(random.NextDouble() * 2.0 - 1.0);
            float seconds = kind switch
            {
                SoundBank.Placeholder.Thud => 0.45f,
                SoundBank.Placeholder.Slam => 0.9f,
                SoundBank.Placeholder.Scream => 1.6f,
                SoundBank.Placeholder.Heartbeat => 0.9f,
                SoundBank.Placeholder.Hiss => 2.4f,
                _ => 6f,
            };
            int length = (int)(rate * seconds);
            var data = new float[length];
            float low = 0f, mid = 0f, phase = 0f, phase2 = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / rate;
                float n = Noise();
                low += (n - low) * 0.04f;
                mid += (n - mid) * 0.3f;
                float v;
                switch (kind)
                {
                    case SoundBank.Placeholder.Thud:
                        v = (low * 3f + Mathf.Sin(t * Mathf.PI * 2f * 62f) * 0.8f) * Mathf.Exp(-t * 11f) * Mathf.Min(1f, t * 600f);
                        break;
                    case SoundBank.Placeholder.Slam:
                        v = (mid * Mathf.Exp(-t * 40f) * 1.2f + (low * 3f + Mathf.Sin(t * Mathf.PI * 2f * 48f)) * Mathf.Exp(-t * 6f) * 0.8f
                             + Mathf.Sin(t * Mathf.PI * 2f * 180f) * Mathf.Exp(-t * 9f) * 0.25f * Mathf.Sin(t * 70f)) * Mathf.Min(1f, t * 900f);
                        break;
                    case SoundBank.Placeholder.Scream:
                        float f = 520f + 380f * t + 90f * Mathf.Sin(t * 37f) + 40f * Noise();
                        phase += f / rate;
                        phase2 += f * 1.47f / rate;
                        float saw = (phase % 1f) * 2f - 1f, saw2 = (phase2 % 1f) * 2f - 1f;
                        v = Mathf.Clamp((saw * 0.6f + saw2 * 0.4f + mid * 0.7f) * 1.8f, -1f, 1f) * Mathf.Min(1f, t * 20f) * Mathf.Clamp01((seconds - t) * 3f) * 0.75f;
                        break;
                    case SoundBank.Placeholder.Heartbeat:
                        v = (Beat(t, 0f) + Beat(t, 0.26f) * 0.7f) * 0.9f + low * 0.3f * Mathf.Exp(-t * 6f);
                        break;
                    case SoundBank.Placeholder.Hiss:
                        v = (mid * 0.5f + low * 1.5f) * Mathf.Sin(Mathf.Clamp01(t / seconds) * Mathf.PI) * (0.6f + 0.4f * Mathf.Sin(t * 23f));
                        break;
                    default:
                        v = (Mathf.Sin(t * Mathf.PI * 2f * 41f) * 0.35f + Mathf.Sin(t * Mathf.PI * 2f * 43.5f) * 0.3f + Mathf.Sin(t * Mathf.PI * 2f * 61.7f) * 0.15f + low * 0.8f) * 0.6f;
                        break;
                }
                data[i] = v;
            }
            var clip = AudioClip.Create("Placeholder_" + kind, length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Beat(float t, float at) => t < at ? 0f : Mathf.Sin((t - at) * Mathf.PI * 2f * 55f) * Mathf.Exp(-(t - at) * 18f);
    }
}
