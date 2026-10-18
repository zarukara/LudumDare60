using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// The way out after the last office: behind its usual entrance door, instead of an office, the emergency
    /// stairwell (<see cref="Stairwell"/>) from the 8th floor down to the basement, every floor alike. Each floor is
    /// one group, so the stairwell can switch far floors off. Treads are only drawn: an invisible ramp over each
    /// flight carries the player, and the concrete slab under the steps is the ceiling of the flight below (and
    /// something for claws to hold on to). The lamps start as a dim night light. Stepping onto the basement
    /// landing ends the run.
    /// </summary>
    public static class FinishHallBuilder
    {
        static readonly Color NumberColor = new Color(0.62f, 0.12f, 0.1f);

        public static GameObject Build(Transform parent, OfficePalette p, Vector3 origin, out Bounds finishZone)
        {
            var root = Prims.Group("Exit_Stairs", parent, origin);
            var well = root.gameObject.AddComponent<Stairwell>();
            well.glow = new Material(p.lightPanel) { name = "Stairwell_Lamp_Glow" };
            for (int floor = Stairwell.Attic; floor >= Stairwell.Basement; floor--)
                well.floors[floor] = Floor(root, well, p, floor).gameObject;

            float bottom = Stairwell.FloorY(Stairwell.Basement);
            finishZone = new Bounds(origin + new Vector3(Stairwell.Split, bottom + 1.1f, Stairwell.LandingDepth / 2f),
                new Vector3(Stairwell.East - Stairwell.West, 2.2f, Stairwell.LandingDepth - 0.6f));
            return root.gameObject;
        }

        /// <summary>One floor: its landing, the walls up to the next one, and the flights from it down to the floor below.</summary>
        static Transform Floor(Transform root, Stairwell well, OfficePalette p, int floor)
        {
            var g = Prims.Group("Floor_" + floor, root, new Vector3(0f, Stairwell.FloorY(floor), 0f));
            Walls(g, p, floor);
            Landing(g, p, "Landing", 0f, 0f, Stairwell.LandingDepth);
            if (floor > Stairwell.Basement) Flights(g, well, p);
            if (floor >= 1 && floor <= Stairwell.Top)
            {
                if (floor < Stairwell.Top) well.covers[floor] = Cart(g, p); // the way in is left clear
                well.numbers[floor] =Prims.Text("Floor_Number", g, new Vector3(Stairwell.CartX, 1.8f, 0.012f), Vector3.forward, floor.ToString(), 0.7f, NumberColor, p.text);
                Lamp(well, g, p, "Lamp_Landing", new Vector3(Stairwell.CartX, 2.95f, 0f), 0f);
            }
            if (floor == Stairwell.Top) Grate(g, p);
            if (floor == Stairwell.Attic) Roof(g, p);
            if (floor == Stairwell.Basement) Basement(g, p);
            return g;
        }

        /// <summary>The shaft's walls from this landing up to the next, with the floor's two doors; the 8th floor's south one is the way in.</summary>
        static void Walls(Transform g, OfficePalette p, int floor)
        {
            const float t = OfficeRoomBuilder.Wall, h = Stairwell.FloorHeight;
            const float bottom = Stairwell.Slab, top = Stairwell.Slab + OfficeRoomBuilder.DoorHeight, hw = OfficeRoomBuilder.DoorWidth / 2f;
            const float doorX = OfficeRoomBuilder.EntranceX, doorZ = Stairwell.SideDoorZ;
            var walls = Prims.Group("Walls", g, new Vector3(0f, -Stairwell.Slab, 0f));
            OfficeRoomBuilder.WallPieces(walls, p.wall, "Wall_South", true, -t / 2f, Stairwell.West - t, Stairwell.East + t, h, t,
                new List<OfficeRoomBuilder.Opening> { new OfficeRoomBuilder.Opening(doorX - hw, doorX + hw, bottom, top) });
            OfficeRoomBuilder.WallPieces(walls, p.wall, "Wall_West", false, Stairwell.West - t / 2f, 0f, Stairwell.North, h, t,
                new List<OfficeRoomBuilder.Opening> { new OfficeRoomBuilder.Opening(doorZ - hw, doorZ + hw, bottom, top) });
            Prims.Box("Wall_East", walls, new Vector3(Stairwell.East + t / 2f, h / 2f, Stairwell.North / 2f), new Vector3(t, h, Stairwell.North), p.wall);
            Prims.Box("Wall_North", walls, new Vector3(Stairwell.Split, h / 2f, Stairwell.North + t / 2f), new Vector3(Stairwell.East - Stairwell.West + 2f * t, h, t), p.wall);

            bool wayIn = floor == Stairwell.Top;
            var south = OfficeRoomBuilder.Door(g, p, wayIn ? "Entrance_Door" : "Floor_Door", doorX, -t / 2f);
            if (!wayIn) south.GetComponent<AutoDoor>().locked = true;
            var side = Prims.Group("Side_Door", g, new Vector3(Stairwell.West - t / 2f, 0f, doorZ), 90f);
            OfficeRoomBuilder.Door(side, p, "Side_Door", 0f, 0f).GetComponent<AutoDoor>().locked = true;
        }

        /// <summary>A landing's slab: plaster underneath (the ceiling of the floor below), the floor on top.</summary>
        static void Landing(Transform g, OfficePalette p, string name, float y, float z0, float z1)
        {
            float width = Stairwell.East - Stairwell.West, depth = z1 - z0, z = (z0 + z1) / 2f;
            Prims.Box(name, g, new Vector3(Stairwell.Split, y - Stairwell.Slab / 2f, z), new Vector3(width, Stairwell.Slab, depth), p.wall);
            Prims.Flat(name + "_Floor", g, new Vector3(Stairwell.Split, y + 0.004f, z), new Vector3(width, 0.008f, depth), p.corridorFloor);
        }

        /// <summary>From this landing down to the next one: a flight to the half landing, a flight back, the wall between them.</summary>
        static void Flights(Transform g, Stairwell well, OfficePalette p)
        {
            const float half = Stairwell.MiddleWall / 2f;
            Flight(g, p, "Flight_Down", Stairwell.Split + half, Stairwell.East, 0f, true);
            Landing(g, p, "Half_Landing", -Stairwell.FlightDrop, Stairwell.FlightEnd, Stairwell.North);
            Flight(g, p, "Flight_Back", Stairwell.West, Stairwell.Split - half, -Stairwell.FlightDrop, false);
            Prims.Box("Middle_Wall", g, new Vector3(Stairwell.Split, -Stairwell.FloorHeight / 2f, (Stairwell.LandingDepth + Stairwell.FlightEnd) / 2f),
                new Vector3(Stairwell.MiddleWall, Stairwell.FloorHeight, Stairwell.Run), p.wall);
            Lamp(well, g, p, "Lamp_Half", new Vector3(Stairwell.Split, -Stairwell.FlightDrop + 2.5f, Stairwell.North), 180f);
        }

        /// <summary>
        /// Twelve steps down from <paramref name="top"/>, northwards (away from the landing) or back south, with
        /// handrails on both walls. The player walks on an invisible ramp over the step edges.
        /// </summary>
        static void Flight(Transform g, OfficePalette p, string name, float x0, float x1, float top, bool north)
        {
            var f = Prims.Group(name, g);
            float width = x1 - x0, xc = (x0 + x1) / 2f, dir = north ? 1f : -1f;
            float start = north ? Stairwell.LandingDepth : Stairwell.FlightEnd, end = start + dir * Stairwell.Run;
            const float depth = Stairwell.Rise + 0.06f; // reaches down into the slab
            for (int i = 0; i < Stairwell.Steps; i++)
            {
                float tread = top - Stairwell.Rise * (i + 1);
                float near = start + dir * Stairwell.Tread * i;
                Prims.Box($"Step_{i}", f, new Vector3(xc, tread - depth / 2f, near + dir * Stairwell.Tread / 2f), new Vector3(width, depth, Stairwell.Tread), p.corridorFloor, false);
                // The edge above it, the one stepped down from.
                Prims.Box($"Nosing_{i}", f, new Vector3(xc, tread + Stairwell.Rise - 0.012f, near - dir * 0.02f), new Vector3(width, 0.024f, 0.04f), p.metal, false);
            }

            float length = Mathf.Sqrt(Stairwell.Run * Stairwell.Run + Stairwell.FlightDrop * Stairwell.FlightDrop);
            var tilt = Quaternion.Euler(dir * Mathf.Atan(Stairwell.Slope) * Mathf.Rad2Deg, 0f, 0f);
            Vector3 up = tilt * Vector3.up;
            var edges = new Vector3(xc, top - Stairwell.FlightDrop / 2f, (Stairwell.LandingDepth + Stairwell.FlightEnd) / 2f);
            var ramp = new GameObject("Ramp");
            ramp.transform.SetParent(f, false);
            ramp.transform.localRotation = tilt;
            ramp.transform.localPosition = edges - up * 0.05f;
            ramp.AddComponent<BoxCollider>().size = new Vector3(width, 0.1f, length + 0.05f);
            // Its top just under the inner corners of the steps, its underside at Stairwell.Underside below the edges.
            Prims.Box("Slab", f, edges - Vector3.up * (Stairwell.Underside - Stairwell.FlightSlab * Mathf.Sqrt(1f + Stairwell.Slope * Stairwell.Slope)) - up * (Stairwell.FlightSlab / 2f),
                new Vector3(width, Stairwell.FlightSlab, length), p.wall, true, tilt.eulerAngles);

            float outer = north ? x1 - 0.07f : x0 + 0.07f, inner = north ? x0 + 0.07f : x1 - 0.07f;
            foreach (float x in new[] { outer, inner })
                Prims.Segment("Handrail", f, new Vector3(x, top + 0.9f, start), new Vector3(x, top - Stairwell.FlightDrop + 0.9f, end), 0.045f, p.metal);
        }

        /// <summary>A caged bulkhead lamp on a wall; its face uses the stairwell's shared glow so all of them flash together.</summary>
        static void Lamp(Stairwell well, Transform g, OfficePalette p, string name, Vector3 position, float yaw)
        {
            var lamp = Prims.Group(name, g, position, yaw);
            Prims.Box("Body", lamp, new Vector3(0f, 0f, 0.05f), new Vector3(0.34f, 0.18f, 0.1f), p.whitePlastic, false);
            Prims.Flat("Glow", lamp, new Vector3(0f, 0f, 0.102f), new Vector3(0.28f, 0.12f, 0.006f), well.glow);
            for (int i = -1; i <= 1; i++)
                Prims.Box("Guard", lamp, new Vector3(i * 0.09f, 0f, 0.115f), new Vector3(0.012f, 0.17f, 0.012f), p.metal, false);
            well.lamps.Add(Prims.PointLight("Light", lamp, new Vector3(0f, -0.15f, 0.4f), 7f, well.nightIntensity, Color.white));
        }

        /// <summary>A cleaning cart knocked over onto its back, its things spilled round it. The player hides behind it.</summary>
        static HidingSpot Cart(Transform g, OfficePalette p)
        {
            var cart = Prims.Group("Cleaning_Cart", g, new Vector3(Stairwell.CartX, 0f, Stairwell.CartZ));
            var block = cart.gameObject.AddComponent<BoxCollider>();
            block.center = new Vector3(0f, 0.31f, 0.52f);
            block.size = new Vector3(1.3f, 0.62f, 1.04f);

            // Built standing (x along it, y up, z across), then laid over: its top points at the stairs.
            var body = Prims.Group("Body", cart, new Vector3(0f, 0.3f, 0f));
            body.localRotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (float y in new[] { 0.14f, 0.55f })
                Prims.Box("Shelf", body, new Vector3(0f, y, 0f), new Vector3(1.2f, 0.05f, 0.56f), p.darkPlastic, false);
            Prims.Box("Top_Tray", body, new Vector3(0.12f, 0.98f, 0f), new Vector3(0.9f, 0.05f, 0.5f), p.darkPlastic, false);
            Prims.Box("Tray_Lip", body, new Vector3(0.12f, 1.03f, 0.24f), new Vector3(0.9f, 0.08f, 0.02f), p.darkPlastic, false);
            foreach (float x in new[] { -0.58f, 0.58f })
                foreach (float z in new[] { -0.25f, 0.25f })
                    Prims.Box("Post", body, new Vector3(x, 0.55f, z), new Vector3(0.04f, 0.92f, 0.04f), p.lightPlastic, false);
            Prims.Segment("Handle", body, new Vector3(-0.66f, 0.95f, -0.24f), new Vector3(-0.66f, 0.95f, 0.24f), 0.035f, p.metal);
            Prims.Box("Bag", body, new Vector3(0.36f, 0.56f, 0f), new Vector3(0.42f, 0.74f, 0.5f), p.yellow, false);
            foreach (float x in new[] { -0.5f, 0.5f })
                foreach (float z in new[] { -0.22f, 0.22f })
                    Prims.Cylinder("Wheel", body, new Vector3(x, 0.06f, z), 0.06f, 0.04f, p.darkPlastic, false, new Vector3(90f, 0f, 0f));

            // What fell out of it.
            Prims.Cylinder("Bucket", cart, new Vector3(1f, 0.17f, 0.45f), 0.17f, 0.34f, p.yellow, false, new Vector3(0f, 25f, 90f));
            Prims.Box("Wringer", cart, new Vector3(1.22f, 0.2f, 0.35f), new Vector3(0.1f, 0.22f, 0.26f), p.darkPlastic, false, new Vector3(0f, 25f, 0f));
            Prims.Flat("Spill", cart, new Vector3(1.05f, 0.012f, 0.75f), new Vector3(1.1f, 0.004f, 0.9f), p.water, new Vector3(0f, 18f, 0f));
            Prims.Segment("Mop_Stick", cart, new Vector3(1.45f, 0.03f, 1.15f), new Vector3(-0.35f, 0.04f, 1.55f), 0.028f, p.wood);
            Prims.Box("Mop_Head", cart, new Vector3(1.55f, 0.04f, 1.12f), new Vector3(0.12f, 0.06f, 0.3f), p.paper, false, new Vector3(0f, -12f, 0f));
            Prims.Cylinder("Towel_Roll", cart, new Vector3(-0.9f, 0.065f, 1.3f), 0.065f, 0.26f, p.paper, false, new Vector3(0f, 40f, 90f));
            Prims.Sphere("Trash_Bag", cart, new Vector3(-0.45f, 0.17f, 1.4f), new Vector3(0.5f, 0.34f, 0.44f), p.darkPlastic);
            foreach (var (at, mat, yaw) in new[] { (new Vector3(0.7f, 0.04f, 1.35f), p.blue, 70f), (new Vector3(0.2f, 0.04f, 1.6f), p.red, 150f), (new Vector3(1.35f, 0.04f, -0.15f), p.whitePlastic, 20f) })
                Prims.Cylinder("Bottle", cart, at, 0.04f, 0.22f, mat, false, new Vector3(0f, yaw, 90f));

            var spot = cart.gameObject.AddComponent<HidingSpot>();
            spot.seat = Prims.Group("Seat", cart, new Vector3(0f, 0f, -0.55f));
            spot.exit = Prims.Group("Exit", cart, new Vector3(1.4f, 0f, -0.15f));
            return spot;
        }

        /// <summary>The flight up to the attic, barred at its foot on the 8th floor.</summary>
        static void Grate(Transform g, OfficePalette p)
        {
            const float x0 = Stairwell.West, x1 = Stairwell.Split - Stairwell.MiddleWall / 2f, h = 2.3f, z = Stairwell.LandingDepth + 0.05f;
            var grate = Prims.Group("Attic_Grate", g);
            for (float x = x0 + 0.08f; x < x1 - 0.03f; x += 0.12f)
                Prims.Box("Bar", grate, new Vector3(x, h / 2f, z), new Vector3(0.022f, h, 0.022f), p.metal, false);
            foreach (float y in new[] { 0.12f, 1.1f, h - 0.05f })
                Prims.Box("Brace", grate, new Vector3((x0 + x1) / 2f, y, z), new Vector3(x1 - x0, 0.04f, 0.03f), p.metal, false);
            Prims.Box("Padlock", grate, new Vector3(x1 - 0.35f, 1.02f, z - 0.035f), new Vector3(0.07f, 0.09f, 0.03f), p.brass, false);
            var block = grate.gameObject.AddComponent<BoxCollider>();
            block.center = new Vector3((x0 + x1) / 2f, h / 2f, z);
            block.size = new Vector3(x1 - x0, h, 0.08f);
        }

        /// <summary>The top of the shaft, over the attic landing.</summary>
        static void Roof(Transform g, OfficePalette p)
        {
            const float t = OfficeRoomBuilder.Wall, under = Stairwell.FloorHeight - Stairwell.Slab;
            Prims.Box("Roof", g, new Vector3(Stairwell.Split, under + Stairwell.Slab / 2f, Stairwell.North / 2f),
                new Vector3(Stairwell.East - Stairwell.West + 2f * t, Stairwell.Slab, Stairwell.North + 2f * t), p.wall);
            Prims.Box("Middle_Wall_Top", g, new Vector3(Stairwell.Split, under / 2f, (Stairwell.LandingDepth + Stairwell.FlightEnd) / 2f),
                new Vector3(Stairwell.MiddleWall, under, Stairwell.Run), p.wall);
        }

        /// <summary>The bottom: a floor under the last flights and a green exit sign over the door out. No lamps.</summary>
        static void Basement(Transform g, OfficePalette p)
        {
            Prims.Box("Pit", g, new Vector3(Stairwell.Split, -Stairwell.Slab / 2f, (Stairwell.LandingDepth + Stairwell.North) / 2f),
                new Vector3(Stairwell.East - Stairwell.West, Stairwell.Slab, Stairwell.North - Stairwell.LandingDepth), p.corridorFloor);
            var sign = Prims.Group("Exit_Sign", g, new Vector3(OfficeRoomBuilder.EntranceX, OfficeRoomBuilder.DoorHeight + 0.3f, 0.02f));
            Prims.Flat("Panel", sign, new Vector3(0f, 0f, 0.015f), new Vector3(0.62f, 0.22f, 0.03f), p.exitSign);
            var text = Prims.Text("Text", sign, new Vector3(0f, 0f, 0.032f), Vector3.forward, "", 0.14f, Color.white, p.text);
            OfficeRoomBuilder.Localized(text, "sign.exit");
        }
    }
}
