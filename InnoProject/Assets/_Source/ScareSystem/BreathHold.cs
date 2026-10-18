using UnityEngine;

namespace ScareSystem
{
    /// <summary>
    /// Holding the breath while she is close (<see cref="StairsHunt"/>), as a little timing game. A marker runs to and
    /// fro along a bar; one press while it is in the lit zone holds the breath for a while (<see cref="Phase.Hold"/>),
    /// then a new zone lights up and has to be caught in time again. A press outside the zone, or none in time, and
    /// the player gasps. <see cref="Difficulty"/> (0..1) narrows the zone and speeds the marker up.
    /// </summary>
    public class BreathHold
    {
        public enum Phase
        {
            Catch,
            Hold,
        }

        public Phase State { get; private set; } = Phase.Catch;
        /// <summary>Where the marker is along the bar, 0..1.</summary>
        public float Marker { get; private set; }
        public float ZoneCenter { get; private set; }
        public float ZoneWidth { get; private set; }
        /// <summary>Catch: seconds left to press. Hold: seconds the breath still lasts.</summary>
        public float TimeLeft { get; private set; }
        /// <summary>While holding: how much of the held breath is left, 1..0.</summary>
        public float HoldShare => State == Phase.Hold ? Mathf.Clamp01(TimeLeft / Mathf.Max(0.01f, holdTime)) : 0f;
        /// <summary>Time.time of the last good press.</summary>
        public float LastCatch { get; private set; } = -1f;
        public float Difficulty { get; set; }

        float direction = 1f, holdTime;

        float Speed => Mathf.Lerp(0.7f, 1.25f, Difficulty);   // bar lengths per second
        float Window => Mathf.Lerp(2.8f, 1.8f, Difficulty);
        float Width => Mathf.Lerp(0.24f, 0.12f, Difficulty);
        float HoldSeconds => Mathf.Lerp(1.9f, 1.3f, Difficulty);

        public BreathHold(float difficulty = 0f)
        {
            Difficulty = difficulty;
            Marker = Random.value * 0.2f;
            NewZone();
        }

        /// <summary>One frame: +1 the zone was caught, -1 the player gasped, 0 nothing happened.</summary>
        public int Tick(float dt, bool pressed)
        {
            TimeLeft -= dt;
            if (State == Phase.Hold)
            {
                if (TimeLeft <= 0f) NewZone();
                return 0; // presses while holding do not count
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

        /// <summary>A fresh zone away from the marker, and the time to catch it.</summary>
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
