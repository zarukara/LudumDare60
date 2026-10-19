using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;

namespace LevelSystem
{

    public class Stairwell : MonoBehaviour
    {
        public const int Attic = 9, Top = 8, Basement = 0;
        public const float West = 3.2f, East = 7.8f, Split = 5.5f, MiddleWall = 0.3f;
        public const float LandingDepth = 3.6f, MidDepth = 2.2f, Slab = 0.3f, FlightSlab = 0.3f;
        public const int Steps = 12;
        public const float Rise = 0.175f, Tread = 0.3f;
        public const float Run = Steps * Tread, FlightDrop = Steps * Rise, FloorHeight = 2f * FlightDrop;
        public const float FlightEnd = LandingDepth + Run, North = FlightEnd + MidDepth;

        public const float Slope = FlightDrop / Run;
        public const float WestFlight = (West + Split - MiddleWall / 2f) / 2f, EastFlight = (Split + MiddleWall / 2f + East) / 2f;

        public const float CartX = 4.3f, CartZ = 1.05f;

        public const float WardrobeX = 3.85f, NumberX = 5f;

        public const float SideDoorZ = 2.8f;

        public static readonly float Underside = Rise + 0.015f + FlightSlab * Mathf.Sqrt(1f + Slope * Slope);

        static readonly Color NightColor = new Color(0.82f, 0.9f, 1f);
        static readonly Color AlarmColor = new Color(1f, 0.09f, 0.05f);
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Lamp brightness before the alarm.")]
        public float nightIntensity = 1.3f;
        [Tooltip("Peak brightness of the flashing red.")]
        public float alarmIntensity = 3.4f;
        [Tooltip("Flashes per second.")]
        public float alarmRate = 1.1f;
        [Tooltip("Floors kept switched on above and below the camera.")]
        public int floorsAround = 2;

        public bool Alarm { get; private set; }

        internal OfficePalette palette;
        internal Material glow;
        internal readonly List<Light> lamps = new List<Light>();
        internal readonly Dictionary<int, GameObject> floors = new Dictionary<int, GameObject>();
        internal readonly Dictionary<int, HidingSpot> covers = new Dictionary<int, HidingSpot>();
        internal readonly Dictionary<int, TextMesh> numbers = new Dictionary<int, TextMesh>();
        internal readonly Dictionary<int, GameObject> waysUp = new Dictionary<int, GameObject>();

        int shownFrom = int.MinValue, shownTo = int.MinValue;
        float alarmTime;

        public static float FloorY(int floor) => (floor - Top) * FloorHeight;

        public void SetAlarm(bool on)
        {
            Alarm = on;
            alarmTime = 0f;
        }

        public void SetNumber(int floor, string text)
        {
            if (numbers.TryGetValue(floor, out var number) && number != null) number.text = text;
        }

        public void ShiftNumbersUp(int top)
        {
            for (int floor = top; floor > Basement + 1; floor--)
                if (numbers.TryGetValue(floor, out var to) && numbers.TryGetValue(floor - 1, out var from) && to != null && from != null)
                    to.text = from.text;
        }

        public void ShutWaysUp(int lowest)
        {
            foreach (var pair in waysUp)
                if (pair.Value != null) pair.Value.SetActive(pair.Key >= lowest);
        }

        public void OpenWaysUp() => ShutWaysUp(int.MaxValue);

        public void ResetNumbers()
        {
            foreach (var pair in numbers)
                if (pair.Value != null) pair.Value.text = pair.Key.ToString();
        }

        public Transform FloorDoor(int floor) => floors.TryGetValue(floor, out var g) && g != null ? g.transform.Find("Floor_Door") : null;

        public HidingSpot Cover(int floor) => covers.TryGetValue(floor, out var spot) ? spot : null;

        public void RemoveCover(int floor)
        {
            if (!covers.TryGetValue(floor, out var spot)) return;
            covers.Remove(floor);
            if (spot != null) Destroy(spot.gameObject);
        }

        public float Level(Vector3 world) => Top + transform.InverseTransformPoint(world).y / FloorHeight;

        public bool OnLanding(int floor, Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            return local.x > West && local.x < East && local.z > 0f && local.z < LandingDepth && Mathf.Abs(local.y - FloorY(floor)) < 1.2f;
        }

        public bool NearFloor(int floor, Vector3 world) => Mathf.Abs(transform.InverseTransformPoint(world).y - FloorY(floor)) < 1.6f;

        public Pose Lookout(int floor) =>
            new Pose(transform.TransformPoint(new Vector3(EastFlight - 0.15f, FloorY(floor) + 0.05f, LandingDepth - 0.45f)), transform.rotation);

        public Pose Arrival(int floor) =>
            new Pose(transform.TransformPoint(new Vector3(WestFlight, FloorY(floor) + 0.05f, 3.1f)), transform.rotation * Quaternion.Euler(0f, 180f, 0f));

        public Vector3[] Route(int floor, out int stare, out bool upright)
        {
            var route = Route(floor, out stare);
            var cover = Cover(floor);
            upright = cover != null && cover.Enclosed;
            if (upright) route[stare] = cover.peek.position;
            return route;
        }

        public Vector3[] Route(int floor, out int stare)
        {
            const float hang = 0.8f;
            float landingCeiling = FloorHeight - Slab, halfBelow = landingCeiling - FlightDrop, halfAbove = halfBelow + FloorHeight;
            var local = new[]
            {
                new Vector3(WestFlight, CeilingOverFlightBelow(6f) - hang, 6f),
                new Vector3(WestFlight + 0.1f, halfBelow - hang, 8.3f),
                new Vector3(EastFlight - 0.1f, halfBelow - hang, 8.3f),
                new Vector3(EastFlight, CeilingOverFlightDown(6.9f) - hang, 6.9f),
                new Vector3(EastFlight, CeilingOverFlightDown(4f) - hang, 4f),
                new Vector3(EastFlight - 0.2f, landingCeiling - hang, 2.6f),
                new Vector3(CartX, landingCeiling - 1f, 1.75f),
                new Vector3(WestFlight, landingCeiling - hang, 3f),
                new Vector3(WestFlight, CeilingOverFlightUp(4.2f) - hang, 4.2f),
                new Vector3(WestFlight, CeilingOverFlightUp(6.9f) - hang, 6.9f),
                new Vector3(WestFlight + 0.1f, halfAbove - hang, 8.3f),
                new Vector3(EastFlight - 0.1f, halfAbove - hang, 8.3f),
            };
            stare = 6;
            float y = FloorY(floor);
            var route = new Vector3[local.Length];
            for (int i = 0; i < local.Length; i++) route[i] = transform.TransformPoint(local[i] + Vector3.up * y);
            return route;
        }

        static float CeilingOverFlightDown(float z) => FloorHeight - Underside - (z - LandingDepth) * Slope;
        static float CeilingOverFlightUp(float z) => FloorHeight - Underside + (z - LandingDepth) * Slope;
        static float CeilingOverFlightBelow(float z) => -Underside + (z - LandingDepth) * Slope;

        void Update()
        {
            var cam = Camera.main;
            if (cam != null) ShowFloorsAround(cam.transform.position);
            UpdateLamps();
        }

        void ShowFloorsAround(Vector3 viewer)
        {
            int level = Mathf.RoundToInt(Level(viewer));
            int from = level - floorsAround, to = level + floorsAround;
            if (from == shownFrom && to == shownTo) return;
            shownFrom = from;
            shownTo = to;
            foreach (var pair in floors)
                if (pair.Value != null) pair.Value.SetActive(pair.Key >= from && pair.Key <= to);
        }

        void UpdateLamps()
        {
            alarmTime += Time.deltaTime;
            float pulse = Alarm ? Flash(alarmTime * alarmRate) : 1f;
            Color color = Alarm ? AlarmColor : NightColor;
            float intensity = Alarm ? alarmIntensity * (0.05f + 0.95f * pulse) : nightIntensity;
            foreach (var lamp in lamps)
            {
                if (lamp == null) continue;
                lamp.color = color;
                lamp.intensity = intensity;
            }
            if (glow == null) return;
            glow.SetColor(EmissionColor, Alarm ? AlarmColor * (0.1f + 3.2f * pulse) : NightColor * 1.6f);
            glow.SetColor(BaseColor, Alarm ? Color.Lerp(new Color(0.3f, 0.04f, 0.03f), AlarmColor, pulse) : Color.white);
        }

        static float Flash(float t)
        {
            float phase = t - Mathf.Floor(t);
            return Mathf.SmoothStep(0f, 1f, phase / 0.08f) * (1f - Mathf.SmoothStep(0f, 1f, (phase - 0.5f) / 0.14f));
        }

        void OnDestroy()
        {
            if (glow != null) Destroy(glow);
        }
    }
}
