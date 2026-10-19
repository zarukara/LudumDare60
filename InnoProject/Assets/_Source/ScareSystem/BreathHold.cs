using UnityEngine;

namespace ScareSystem
{

    public class BreathHold
    {
        public enum Phase
        {
            Catch,
            Hold,
        }

        public Phase State { get; private set; } = Phase.Catch;

        public float Marker { get; private set; }
        public float ZoneCenter { get; private set; }
        public float ZoneWidth { get; private set; }

        public float TimeLeft { get; private set; }

        public float HoldShare => State == Phase.Hold ? Mathf.Clamp01(TimeLeft / Mathf.Max(0.01f, holdTime)) : 0f;

        public float LastCatch { get; private set; } = -1f;
        public float Difficulty { get; set; }

        float direction = 1f, holdTime;

        float Speed => Mathf.Lerp(0.7f, 1.25f, Difficulty);
        float Window => Mathf.Lerp(2.8f, 1.8f, Difficulty);
        float Width => Mathf.Lerp(0.24f, 0.12f, Difficulty);
        float HoldSeconds => Mathf.Lerp(1.9f, 1.3f, Difficulty);

        public BreathHold(float difficulty = 0f)
        {
            Difficulty = difficulty;
            Marker = Random.value * 0.2f;
            NewZone();
        }

        public int Tick(float dt, bool pressed)
        {
            TimeLeft -= dt;
            if (State == Phase.Hold)
            {
                if (TimeLeft <= 0f) NewZone();
                return 0;
            }

            Marker += direction * Speed * dt;
            if (Marker > 1f)
            {
                Marker = 2f - Marker;
                direction = -1f;
            }
            else if (Marker < 0f)
            {
                Marker = -Marker;
                direction = 1f;
            }

            if (pressed)
            {
                if (Mathf.Abs(Marker - ZoneCenter) > ZoneWidth / 2f) return -1;
                State = Phase.Hold;
                holdTime = TimeLeft = HoldSeconds;
                LastCatch = Time.time;
                return 1;
            }
            return TimeLeft <= 0f ? -1 : 0;
        }

        void NewZone()
        {
            State = Phase.Catch;
            ZoneWidth = Width;
            float half = ZoneWidth / 2f + 0.04f;
            float center = Random.Range(half, 1f - half);
            for (int i = 0; i < 6 && Mathf.Abs(center - Marker) < 0.3f; i++) center = Random.Range(half, 1f - half);
            ZoneCenter = center;
            TimeLeft = Window;
        }
    }
}
