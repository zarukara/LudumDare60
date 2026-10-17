using RoundSystem;
using UnityEngine;

namespace SoundSystem
{
    /// <summary>
    /// Background ambience that grows with the player's level, like the pages in Slender: one seamless loop per
    /// stage, calm to intense. On a new stage the old loop fades out and the new one swells in from the top of its
    /// loop; a mistake (back to level 1) brings the calm back, and reaching the exit fades everything out.
    /// Scripts can push it down for a while with <see cref="Duck"/> (a chase with its own music). Plays in 2D;
    /// the pause stops it along with every other sound.
    /// </summary>
    public class Ambience : MonoBehaviour
    {
        public RoundManager round;
        [Tooltip("Loops from calm to intense, already balanced against each other.")]
        public AudioClip[] stages = new AudioClip[0];
        [Tooltip("The level each stage after the first starts on: 3, 5, 7 = levels 1-2, 3-4, 5-6 and 7-8.")]
        public int[] stageFromLevel = { 3, 5, 7 };
        [Range(0f, 1f)] public float volume = 0.6f;
        [Tooltip("Seconds for the old stage to fade out.")]
        public float fadeOut = 2.5f;
        [Tooltip("Seconds for the new stage to swell in; it starts once the old one is mostly gone.")]
        public float fadeIn = 4f;

        /// <summary>The stage that plays or swells in (0 = calm).</summary>
        public int Stage { get; private set; }

        AudioSource[] sources;
        float[] fades;
        bool[] playing;
        float master, masterTarget = 1f, duck = 1f, duckTarget = 1f;

        /// <summary>Smoothly lowers the ambience to a share of its volume; 1 brings it back.</summary>
        public void Duck(float share) => duckTarget = Mathf.Clamp01(share);

        void Awake()
        {
            sources = new AudioSource[stages.Length];
            fades = new float[stages.Length];
            playing = new bool[stages.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.clip = stages[i];
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.priority = 0; // never dropped for effects
                source.volume = 0f;
                sources[i] = source;
            }
        }

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

        void OnRunStarted() => masterTarget = 1f;

        void OnExitReached() => masterTarget = 0f;

        void Update()
        {
            if (sources.Length == 0) return;
            float dt = Time.deltaTime;
            Stage = StageFor(round != null ? round.Level : 1);
            master = Mathf.MoveTowards(master, masterTarget, dt / (masterTarget > master ? 3f : 2f));
            duck = Mathf.MoveTowards(duck, duckTarget, dt / 1.5f);

            bool othersQuiet = true;
            for (int i = 0; i < sources.Length; i++)
                if (i != Stage && fades[i] > 0.35f) othersQuiet = false;

            for (int i = 0; i < sources.Length; i++)
            {
                bool wanted = i == Stage && (othersQuiet || fades[i] > 0f);
                fades[i] = Mathf.MoveTowards(fades[i], wanted ? 1f : 0f, dt / (wanted ? fadeIn : fadeOut));
                if (fades[i] > 0f && !playing[i])
                {
                    sources[i].Play();
                    playing[i] = true;
                }
                else if (fades[i] <= 0f && playing[i])
                {
                    sources[i].Stop();
                    playing[i] = false;
                }
                float f = fades[i] * fades[i] * (3f - 2f * fades[i]);
                sources[i].volume = volume * master * duck * f;
            }
        }

        int StageFor(int level)
        {
            int stage = 0;
            for (int i = 0; i < stageFromLevel.Length && i + 1 < stages.Length; i++)
                if (level >= stageFromLevel[i]) stage = i + 1;
            return stage;
        }
    }
}
