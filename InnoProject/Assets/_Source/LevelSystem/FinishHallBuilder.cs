using System.Collections.Generic;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// The way out after the last stage: behind the usual entrance door, instead of an office, an emergency
    /// staircase going down two floors (two flights per floor, folded round a dividing wall). Dim lamps on the
    /// landings, floor numbers on the walls, darkness at the bottom: stepping onto the last landing ends the run.
    /// Treads are only drawn; an invisible ramp over each flight makes walking down smooth.
    /// </summary>
    public static class FinishHallBuilder
    {
        // The shaft inside the office footprint (inner faces), entered from the south at EntranceX.
        const float West = 4.3f, East = 7.5f, North = 7.1f, Split = 5.9f;
        const float Rise = 0.175f, Tread = 0.3f, LandingDepth = 2.4f;
        const int Steps = 10, Floors = 2;
        const float FlightEnd = LandingDepth + Steps * Tread;
        const float FlightDrop = Steps * Rise;
        const float FloorDrop = 2f * FlightDrop;

        public static GameObject Build(Transform parent, OfficePalette p, Vector3 origin, out Bounds finishZone)
        {
            const float h = OfficeRoomBuilder.Height, t = OfficeRoomBuilder.Wall;
            float bottom = -Floors * FloorDrop;
            float low = bottom - 0.3f;
            var root = Prims.Group("Exit_Stairs", parent, origin);

            // Shaft walls from the bottom up to the ceiling; the south one has the entrance door at floor level.
            var door = new OfficeRoomBuilder.Opening(OfficeRoomBuilder.EntranceX - 0.5f, OfficeRoomBuilder.EntranceX + 0.5f, 0f, OfficeRoomBuilder.DoorHeight);
            OfficeRoomBuilder.WallPieces(root, p.wall, "Wall_South", true, -t / 2f, West - t, East + t, h + 0.1f, t, new List<OfficeRoomBuilder.Opening> { door });
            OfficeRoomBuilder.Door(root, p, "Entrance_Door", OfficeRoomBuilder.EntranceX, -t / 2f);
            float wallMid = (low + h) / 2f, wallHeight = h - low + 0.1f;
            Prims.Box("Wall_South_Below", root, new Vector3((West + East) / 2f, low / 2f, -t / 2f), new Vector3(East - West + 2f * t, -low, t), p.wall);
            Prims.Box("Wall_North", root, new Vector3((West + East) / 2f, wallMid, North + t / 2f), new Vector3(East - West + 2f * t, wallHeight, t), p.wall);
            Prims.Box("Wall_West", root, new Vector3(West - t / 2f, wallMid, North / 2f), new Vector3(t, wallHeight, North), p.wall);
            Prims.Box("Wall_East", root, new Vector3(East + t / 2f, wallMid, North / 2f), new Vector3(t, wallHeight, North), p.wall);
            Prims.Box("Ceiling", root, new Vector3((West + East) / 2f, h + 0.05f, North / 2f), new Vector3(East - West, 0.1f, North), p.ceiling, false);
            Prims.Box("Pit", root, new Vector3((West + East) / 2f, low - 0.05f, North / 2f), new Vector3(East - West, 0.1f, North), p.corridorFloor);

            Landing(root, p, "Landing_Top", 0f, 0f, LandingDepth);
            // The well above the flight coming back: a parapet so nobody steps into it from the top landing.
            Prims.Box("Parapet", root, new Vector3((West + Split) / 2f, 0.475f, LandingDepth + 0.05f), new Vector3(Split - West, 0.95f, 0.1f), p.wall);
            Prims.Segment("Parapet_Rail", root, new Vector3(West, 0.98f, LandingDepth + 0.05f), new Vector3(Split, 0.98f, LandingDepth + 0.05f), 0.05f, p.metal);
            for (int f = 0; f < Floors; f++)
            {
                float top = -f * FloorDrop, mid = top - FlightDrop, next = top - FloorDrop;
                Flight(root, p, $"Flight_{f}_Down", Split, East, top, true);
                Landing(root, p, $"Landing_{f}_Turn", mid, FlightEnd, North);
                Flight(root, p, $"Flight_{f}_Back", West, Split, mid, false);
                Landing(root, p, $"Landing_{f}_Floor", next, 0f, LandingDepth);
                // Dividing wall between the two flights, waist-high above the upper one.
                Prims.Box($"Divider_{f}", root, new Vector3(Split, (next - 0.2f + top + 0.95f) / 2f, (LandingDepth + FlightEnd) / 2f),
                    new Vector3(0.12f, top + 0.95f - next + 0.2f, FlightEnd - LandingDepth), p.wall);
                Prims.Segment($"Divider_{f}_Rail", root, new Vector3(Split, top + 0.98f, LandingDepth), new Vector3(Split, top + 0.98f, FlightEnd), 0.05f, p.metal);
                Prims.Segment($"Rail_{f}_Down", root, new Vector3(East - 0.06f, top + 0.9f, LandingDepth), new Vector3(East - 0.06f, mid + 0.9f, FlightEnd), 0.04f, p.metal);
                Prims.Segment($"Rail_{f}_Back", root, new Vector3(West + 0.06f, mid + 0.9f, FlightEnd), new Vector3(West + 0.06f, next + 0.9f, LandingDepth), 0.04f, p.metal);
            }

            // Lamps get fewer and dimmer on the way down; the last landing is dark.
            Lamp(root, p, "Lamp_Top", new Vector3(East - 0.02f, 2.2f, 1.2f), -90f, 1.5f);
            Lamp(root, p, "Lamp_0_Turn", new Vector3((West + East) / 2f, -FlightDrop + 2.2f, North - 0.02f), 180f, 1.1f);
            Lamp(root, p, "Lamp_1_Floor", new Vector3(West + 0.02f, -FloorDrop + 2.2f, 1.2f), 90f, 0.75f);
            Lamp(root, p, "Lamp_1_Turn", new Vector3((West + East) / 2f, -FloorDrop - FlightDrop + 2.2f, North - 0.02f), 180f, 0.35f);

            FloorNumber(root, p, "8", new Vector3(West - 0.005f + 0.01f, 1.55f, 1.2f), 90f);
            FloorNumber(root, p, "7", new Vector3((West + East) / 2f, -FloorDrop + 1.55f, 0.01f), 0f);
            FloorNumber(root, p, "6", new Vector3((West + East) / 2f, bottom + 1.55f, 0.01f), 0f);

            finishZone = new Bounds(origin + new Vector3((West + East) / 2f, bottom + 1.1f, LandingDepth / 2f - 0.1f), new Vector3(East - West, 2.2f, LandingDepth - 0.6f));
            return root.gameObject;
        }

        static void Landing(Transform root, OfficePalette p, string name, float y, float z0, float z1) =>
            Prims.Box(name, root, new Vector3((West + East) / 2f, y - 0.1f, (z0 + z1) / 2f), new Vector3(East - West, 0.2f, z1 - z0), p.corridorFloor);

        /// <summary>Ten steps down, northwards (forward) or back south; only drawn, the ramp carries the player.</summary>
        static void Flight(Transform root, OfficePalette p, string name, float x0, float x1, float top, bool north)
        {
            var g = Prims.Group(name, root);
            const float slab = Rise + 0.25f; // each step reaches a little under the next: a stepped underside
            for (int i = 0; i < Steps; i++)
            {
                float tread = top - Rise * (i + 1);
                float za = north ? LandingDepth + Tread * i : FlightEnd - Tread * i;
                float zb = north ? za + Tread : za - Tread;
                Prims.Box($"Step_{i}", g, new Vector3((x0 + x1) / 2f, tread - slab / 2f, (za + zb) / 2f), new Vector3(x1 - x0, slab, Tread), p.corridorFloor, false);
                Prims.Box($"Nosing_{i}", g, new Vector3((x0 + x1) / 2f, tread - 0.012f, north ? za + 0.02f : za - 0.02f), new Vector3(x1 - x0, 0.024f, 0.04f), p.metal, false);
            }

            float length = Mathf.Sqrt(FlightDrop * FlightDrop + (FlightEnd - LandingDepth) * (FlightEnd - LandingDepth));
            float angle = Mathf.Atan2(FlightDrop, FlightEnd - LandingDepth) * Mathf.Rad2Deg;
            var ramp = new GameObject("Ramp");
            ramp.transform.SetParent(g, false);
            ramp.transform.localRotation = Quaternion.Euler(north ? angle : -angle, 0f, 0f);
            ramp.transform.localPosition = new Vector3((x0 + x1) / 2f, top - FlightDrop / 2f, (LandingDepth + FlightEnd) / 2f) - ramp.transform.localRotation * Vector3.up * 0.05f;
            ramp.AddComponent<BoxCollider>().size = new Vector3(x1 - x0, 0.1f, length + 0.05f);
        }

        static void Lamp(Transform root, OfficePalette p, string name, Vector3 position, float yaw, float intensity)
        {
            var g = Prims.Group(name, root, position, yaw);
            Prims.Box("Body", g, new Vector3(0f, 0f, 0.04f), new Vector3(0.3f, 0.12f, 0.08f), p.whitePlastic, false);
            Prims.Flat("Glow", g, new Vector3(0f, -0.005f, 0.085f), new Vector3(0.24f, 0.07f, 0.01f), p.lightPanel);
            Prims.PointLight("Light", g, new Vector3(0f, -0.15f, 0.35f), 5.5f, intensity, new Color(0.82f, 0.9f, 1f));
        }

        static void FloorNumber(Transform root, OfficePalette p, string number, Vector3 position, float yaw)
        {
            var g = Prims.Group("Floor_" + number, root, position, yaw);
            Prims.Text("Number", g, new Vector3(0f, 0f, 0.012f), Vector3.forward, number, 0.6f, new Color(0.62f, 0.12f, 0.1f), p.text);
        }
    }
}
