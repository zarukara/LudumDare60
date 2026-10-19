using System;
using System.Collections.Generic;
using System.Linq;
using LocalizationSystem;
using UnityEngine;

namespace LevelSystem
{

    public static class OfficeRoomBuilder
    {
        public const float Width = 8f, Length = 12f, Height = 2.9f, Wall = 0.2f;
        public const float DoorWidth = 1f, DoorHeight = 2.1f;

        public const float EntranceX = 6f, ExitX = 2f;

        const float DeskPitch = 1.55f, PartitionDepth = 1.6f, WindowWidth = 1.5f, SillHeight = 1f, WindowTop = 2.3f, DeskTop = 0.75f;
        static readonly float[] WestDesks = { 1.575f, 3.125f, 4.675f, 6.225f, 7.775f, 9.325f };
        static readonly float[] EastDesks = { 4.775f, 6.325f, 7.875f, 9.425f, 10.975f };
        static readonly float[] WestWindows = { 2.35f, 5.45f, 8.55f };
        static readonly float[] EastWindows = { 6.325f, 9.425f };

        static readonly Color RoomLight = new Color(0.85f, 0.95f, 0.88f);
        static readonly Color Moonlight = new Color(0.5f, 0.6f, 1f);
        static readonly Color ScreenGlow = new Color(0.45f, 0.62f, 1f);
        static readonly Color LampGlow = new Color(1f, 0.72f, 0.42f);
        static readonly Color Marker = new Color(0.07f, 0.07f, 0.08f);

        public struct Opening
        {
            public float Start, End, Bottom, Top;

            public Opening(float start, float end, float bottom, float top)
            {
                Start = start;
                End = end;
                Bottom = bottom;
                Top = top;
            }
        }

        public static GameObject Build(OfficePalette p)
        {
            var root = new GameObject("OfficeRoom").transform;
            Shell(Prims.Group("Shell", root), p);
            Ceiling(Prims.Group("Ceiling", root), p);
            Workstations(Prims.Group("Workstations", root), p);
            Props(Prims.Group("Props", root), p);
            return root.gameObject;
        }

        static void Shell(Transform shell, OfficePalette p)
        {
            Prims.Box("Floor", shell, new Vector3(Width / 2f, -0.05f, Length / 2f), new Vector3(Width + 2f * Wall, 0.1f, Length + 2f * Wall), p.carpet);
            const float top = Height + 0.1f;
            WallPieces(shell, p.wall, "Wall_South", true, -Wall / 2f, -Wall, Width + Wall, top, Wall, new List<Opening> { DoorOpening(EntranceX) });
            WallPieces(shell, p.wall, "Wall_North", true, Length + Wall / 2f, -Wall, Width + Wall, top, Wall, new List<Opening> { DoorOpening(ExitX) });
            WallPieces(shell, p.wall, "Wall_West", false, -Wall / 2f, 0f, Length, top, Wall, WestWindows.Select(WindowOpening).ToList());
            WallPieces(shell, p.wall, "Wall_East", false, Width + Wall / 2f, 0f, Length, top, Wall, EastWindows.Select(WindowOpening).ToList());

            Door(shell, p, "Entrance_Door", EntranceX, -Wall / 2f);
            Door(shell, p, "Exit_Door", ExitX, Length + Wall / 2f);
            for (int i = 0; i < WestWindows.Length; i++) Window(shell, p, $"Window_W{i + 1}", true, WestWindows[i]);
            for (int i = 0; i < EastWindows.Length; i++) Window(shell, p, $"Window_E{i + 1}", false, EastWindows[i]);
            Baseboards(shell, p);
        }

        static Opening DoorOpening(float x) => new Opening(x - DoorWidth / 2f, x + DoorWidth / 2f, 0f, DoorHeight);
        static Opening WindowOpening(float z) => new Opening(z - WindowWidth / 2f, z + WindowWidth / 2f, SillHeight, WindowTop);

        public static void WallPieces(Transform parent, Material mat, string name, bool alongX, float across, float from, float to, float height,
            float thickness, List<Opening> openings)
        {
            var cuts = new SortedSet<float> { from, to };
            foreach (var o in openings)
            {
                cuts.Add(Mathf.Clamp(o.Start, from, to));
                cuts.Add(Mathf.Clamp(o.End, from, to));
            }
            var list = cuts.ToList();
            int piece = 0;
            for (int i = 0; i + 1 < list.Count; i++)
            {
                float a = list[i], b = list[i + 1];
                if (b - a < 0.001f) continue;
                float mid = (a + b) / 2f;
                float y = 0f;
                foreach (var gap in openings.Where(o => o.Start <= mid && o.End >= mid).OrderBy(o => o.Bottom))
                {
                    if (gap.Bottom > y + 0.001f) Piece(a, b, y, gap.Bottom);
                    y = Mathf.Max(y, gap.Top);
                }
                if (height > y + 0.001f) Piece(a, b, y, height);
            }

            void Piece(float a, float b, float y0, float y1)
            {
                float mid = (a + b) / 2f;
                var center = alongX ? new Vector3(mid, (y0 + y1) / 2f, across) : new Vector3(across, (y0 + y1) / 2f, mid);
                var size = alongX ? new Vector3(b - a, y1 - y0, thickness) : new Vector3(thickness, y1 - y0, b - a);
                Prims.Box($"{name}_{piece++}", parent, center, size, mat);
            }
        }

        public static Transform Door(Transform parent, OfficePalette p, string name, float x, float wallZ)
        {
            var g = Prims.Group(name, parent, new Vector3(x, 0f, wallZ));
            float hw = DoorWidth / 2f;
            Prims.Box("Jamb_L", g, new Vector3(-hw + 0.02f, DoorHeight / 2f, 0f), new Vector3(0.04f, DoorHeight, Wall + 0.01f), p.doorFrame, false);
            Prims.Box("Jamb_R", g, new Vector3(hw - 0.02f, DoorHeight / 2f, 0f), new Vector3(0.04f, DoorHeight, Wall + 0.01f), p.doorFrame, false);
            Prims.Box("Head", g, new Vector3(0f, DoorHeight - 0.02f, 0f), new Vector3(DoorWidth, 0.04f, Wall + 0.01f), p.doorFrame, false);
            foreach (float side in new[] { -1f, 1f })
            {
                float z = side * (Wall / 2f + 0.008f);
                Prims.Box("Architrave_L", g, new Vector3(-hw - 0.035f, (DoorHeight + 0.07f) / 2f, z), new Vector3(0.07f, DoorHeight + 0.07f, 0.016f), p.doorFrame, false);
                Prims.Box("Architrave_R", g, new Vector3(hw + 0.035f, (DoorHeight + 0.07f) / 2f, z), new Vector3(0.07f, DoorHeight + 0.07f, 0.016f), p.doorFrame, false);
                Prims.Box("Architrave_Top", g, new Vector3(0f, DoorHeight + 0.035f, z), new Vector3(DoorWidth + 0.14f, 0.07f, 0.016f), p.doorFrame, false);
            }
            Prims.Flat("Threshold", g, new Vector3(0f, 0.004f, 0f), new Vector3(DoorWidth, 0.008f, Wall + 0.06f), p.metal);

            const float leafWidth = DoorWidth - 0.09f;
            var hinge = Prims.Group("Hinge", g, new Vector3(hw - 0.045f, 0f, 0f), 180f);
            var leaf = Prims.Box("Leaf", hinge, new Vector3(leafWidth / 2f, 1.03f, 0f), new Vector3(leafWidth, 2.04f, 0.04f), p.doorLeaf);
            foreach (float side in new[] { -1f, 1f })
            {
                Prims.Box("Handle", hinge, new Vector3(leafWidth - 0.11f, 1f, side * 0.048f), new Vector3(0.13f, 0.02f, 0.02f), p.metal, false);
                Prims.Box("Rosette", hinge, new Vector3(leafWidth - 0.06f, 1f, side * 0.026f), new Vector3(0.05f, 0.05f, 0.012f), p.metal, false);
                Prims.Box("Kick_Plate", hinge, new Vector3(leafWidth / 2f, 0.12f, side * 0.0215f), new Vector3(leafWidth - 0.06f, 0.2f, 0.003f), p.metal, false);
            }
            var door = hinge.gameObject.AddComponent<AutoDoor>();
            door.leafWidth = leafWidth;
            door.leafCollider = leaf.GetComponent<Collider>();
            door.openSound = p.doorOpen;
            door.closeSound = p.doorClose;
            return hinge;
        }

        static void Window(Transform parent, OfficePalette p, string name, bool west, float z)
        {
            float wx = west ? -Wall / 2f : Width + Wall / 2f;
            float inward = west ? 1f : -1f;
            float hw = WindowWidth / 2f, yc = (SillHeight + WindowTop) / 2f, h = WindowTop - SillHeight;
            var g = Prims.Group(name, parent);

            Prims.Box("Frame_Bottom", g, new Vector3(wx, SillHeight + 0.025f, z), new Vector3(Wall + 0.01f, 0.05f, WindowWidth), p.whitePlastic, false);
            Prims.Box("Frame_Top", g, new Vector3(wx, WindowTop - 0.025f, z), new Vector3(Wall + 0.01f, 0.05f, WindowWidth), p.whitePlastic, false);
            Prims.Box("Frame_A", g, new Vector3(wx, yc, z - hw + 0.025f), new Vector3(Wall + 0.01f, h, 0.05f), p.whitePlastic, false);
            Prims.Box("Frame_B", g, new Vector3(wx, yc, z + hw - 0.025f), new Vector3(Wall + 0.01f, h, 0.05f), p.whitePlastic, false);
            Prims.Box("Mullion", g, new Vector3(wx - inward * 0.03f, yc, z), new Vector3(0.04f, h - 0.1f, 0.04f), p.whitePlastic, false);
            Prims.Flat("Glass", g, new Vector3(wx - inward * 0.03f, yc, z), new Vector3(0.012f, h - 0.1f, WindowWidth - 0.1f), p.glass);

            Prims.Quad("Night_View", g, new Vector3(wx - inward * (Wall / 2f + 0.2f), yc, z), new Vector2(WindowWidth + 0.5f, h + 0.5f), new Vector3(inward, 0f, 0f), p.windowNight);
            Prims.SpotLight("Moonlight", g, new Vector3(wx + inward * 0.2f, WindowTop - 0.1f, z), new Vector3(inward, -0.6f, 0f), 100f, 3.5f, 0.25f, Moonlight, false);

            var blinds = Prims.Group("Blinds", g);
            for (float y = SillHeight + 0.07f; y < WindowTop - 0.06f; y += 0.065f)
                Prims.Box("Slat", blinds, new Vector3(wx + inward * 0.05f, y, z), new Vector3(0.035f, 0.004f, WindowWidth - 0.12f), p.blinds, false, new Vector3(0f, 0f, inward * 35f));
            Prims.Box("Head_Rail", blinds, new Vector3(wx + inward * 0.05f, WindowTop - 0.035f, z), new Vector3(0.05f, 0.04f, WindowWidth - 0.1f), p.blinds, false);
        }

        static void Baseboards(Transform shell, OfficePalette p)
        {
            const float h = 0.08f, t = 0.015f;
            var g = Prims.Group("Baseboards", shell);

            void Run(bool alongX, float across, float from, float to)
            {
                if (to - from < 0.01f) return;
                var center = alongX ? new Vector3((from + to) / 2f, h / 2f, across) : new Vector3(across, h / 2f, (from + to) / 2f);
                var size = alongX ? new Vector3(to - from, h, t) : new Vector3(t, h, to - from);
                Prims.Box("Baseboard", g, center, size, p.baseboard, false);
            }

            const float clear = DoorWidth / 2f + 0.07f;
            Run(true, t / 2f, 0f, EntranceX - clear);
            Run(true, t / 2f, EntranceX + clear, Width);
            Run(true, Length - t / 2f, 0f, ExitX - clear);
            Run(true, Length - t / 2f, ExitX + clear, Width);
            Run(false, t / 2f, 0f, Length);
            Run(false, Width - t / 2f, 0f, Length);
        }

        static void Ceiling(Transform c, OfficePalette p)
        {
            Prims.Box("Ceiling_Slab", c, new Vector3(Width / 2f, Height + 0.05f, Length / 2f), new Vector3(Width + Wall, 0.1f, Length + Wall), p.ceiling, false);
            foreach (float z in new[] { 2f, 6f, 10f })
            foreach (float x in new[] { 2.2f, 5.8f })
            {

                bool on = (x > 4f && z < 4f) || (x < 4f && z > 8f);
                Prims.Flat("Panel_Frame", c, new Vector3(x, Height - 0.008f, z), new Vector3(0.66f, 0.016f, 1.26f), p.lightPlastic);
                Prims.Flat("Light_Panel", c, new Vector3(x, Height - 0.018f, z), new Vector3(0.6f, 0.02f, 1.2f), on ? p.lightPanel : p.lightPanelOff);
                if (on) Prims.SpotLight("Light", c, new Vector3(x, Height - 0.06f, z), Vector3.down, 150f, 6.5f, 3.2f, RoomLight, true);
            }
            foreach (float z in new[] { 4f, 8f })
            {
                Prims.Flat("Vent", c, new Vector3(Width / 2f, Height - 0.007f, z), new Vector3(0.6f, 0.014f, 0.6f), p.whitePlastic);
                for (int i = 0; i < 5; i++)
                    Prims.Flat("Vent_Slot", c, new Vector3(Width / 2f, Height - 0.0145f, z - 0.2f + i * 0.1f), new Vector3(0.5f, 0.002f, 0.04f), p.darkPlastic);
            }
            Prims.Cylinder("Smoke_Detector", c, new Vector3(Width / 2f, Height - 0.015f, 6f), 0.065f, 0.03f, p.whitePlastic);
        }

        static void Workstations(Transform parent, OfficePalette p)
        {
            for (int i = 0; i < WestDesks.Length; i++) Workstation(parent, p, $"Desk_W{i + 1}", true, WestDesks[i], i);
            for (int i = 0; i < EastDesks.Length; i++) Workstation(parent, p, $"Desk_E{i + 1}", false, EastDesks[i], i + 6);
            var partitions = Prims.Group("Partitions", parent);
            for (int i = 0; i <= WestDesks.Length; i++) Partition(partitions, p, true, WestDesks[0] - DeskPitch / 2f + i * DeskPitch);
            for (int i = 0; i <= EastDesks.Length; i++) Partition(partitions, p, false, EastDesks[0] - DeskPitch / 2f + i * DeskPitch);
        }

        static void Partition(Transform parent, OfficePalette p, bool west, float z)
        {
            float x = west ? PartitionDepth / 2f : Width - PartitionDepth / 2f;
            Prims.Box("Partition", parent, new Vector3(x, 0.63f, z), new Vector3(PartitionDepth, 1.2f, 0.05f), p.partition);
            Prims.Box("Partition_Cap", parent, new Vector3(x, 1.245f, z), new Vector3(PartitionDepth + 0.02f, 0.03f, 0.065f), p.metal, false);
            Prims.Box("Partition_Foot", parent, new Vector3(x, 0.015f, z), new Vector3(PartitionDepth, 0.03f, 0.08f), p.metal, false);
        }

        static void Workstation(Transform parent, OfficePalette p, string name, bool west, float z, int index)
        {
            var g = Prims.Group(name, parent, new Vector3(west ? 0f : Width, 0f, z), west ? 90f : -90f);
            Prims.Box("Desk_Top", g, new Vector3(0f, 0.735f, 0.4f), new Vector3(1.45f, 0.03f, 0.75f), p.deskTop);
            Prims.Box("Desk_Side_A", g, new Vector3(-0.69f, 0.36f, 0.4f), new Vector3(0.03f, 0.72f, 0.7f), p.deskFrame, false);
            Prims.Box("Desk_Side_B", g, new Vector3(0.69f, 0.36f, 0.4f), new Vector3(0.03f, 0.72f, 0.7f), p.deskFrame, false);
            Prims.Box("Desk_Modesty", g, new Vector3(0f, 0.47f, 0.09f), new Vector3(1.35f, 0.45f, 0.02f), p.deskFrame, false);

            var pedestal = Prims.Group("Pedestal", g, new Vector3(0.43f, 0f, 0.42f));
            Prims.Box("Body", pedestal, new Vector3(0f, 0.3f, 0f), new Vector3(0.4f, 0.58f, 0.55f), p.lightPlastic, false);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.11f + i * 0.18f;
                Prims.Box("Drawer", pedestal, new Vector3(0f, y, 0.278f), new Vector3(0.38f, 0.165f, 0.006f), p.whitePlastic, false);
                Prims.Box("Handle", pedestal, new Vector3(0f, y + 0.05f, 0.287f), new Vector3(0.12f, 0.015f, 0.012f), p.darkPlastic, false);
            }

            float[] yaws = { -12f, 6f, 15f, -5f, 9f, -15f, 4f, -9f, 12f, -3f, 7f };
            float[] shifts = { 0f, 0.1f, -0.08f, 0.05f, -0.1f, 0.07f, -0.04f, 0.09f, -0.06f, 0.02f, -0.09f };
            float[] distances = { 1.1f, 1.2f, 1.05f, 1.15f, 1.25f, 1.1f, 1.2f, 1.08f, 1.18f, 1.12f, 1.22f };
            Decor.Chair(g, p, "Chair", new Vector3(-0.1f + shifts[index], 0f, distances[index]), 180f + yaws[index]);

            bool twoScreens = index % 6 == 3;
            Monitor(g, p, "Monitor", new Vector3(twoScreens ? 0.17f : -0.05f, DeskTop, 0.2f), 0f);
            Prims.Box("Keyboard", g, new Vector3(-0.05f, 0.761f, 0.53f), new Vector3(0.44f, 0.022f, 0.14f), p.darkPlastic, false);

            Prims.Flat("Mouse_Pad", g, new Vector3(-0.43f, 0.7515f, 0.53f), new Vector3(0.22f, 0.003f, 0.18f), p.blue);
            Prims.Box("Mouse", g, new Vector3(-0.43f, 0.765f, 0.53f), new Vector3(0.06f, 0.025f, 0.1f), p.darkPlastic, false);
            Decor.Bin(g, p, new Vector3(-0.5f, 0f, 0.55f));

            switch (index % 6)
            {
                case 0:
                    Mug(g, new Vector3(0.49f, DeskTop, 0.16f), p.whitePlastic);
                    Papers(g, p, new Vector3(0.42f, DeskTop, 0.5f), 6f);
                    break;
                case 1:
                    Phone(g, p, new Vector3(0.52f, DeskTop, 0.25f));
                    Decor.SmallPlant(g, p, new Vector3(-0.58f, DeskTop, 0.18f));
                    break;
                case 2:
                    Lamp(g, p, new Vector3(0.56f, DeskTop, 0.16f));
                    Papers(g, p, new Vector3(0.44f, DeskTop, 0.5f), -10f);
                    break;
                case 3:
                    Monitor(g, p, "Monitor_2", new Vector3(-0.43f, DeskTop, 0.24f), 18f);
                    Mug(g, new Vector3(0.5f, DeskTop, 0.32f), p.red);
                    break;
                case 4:
                    PhotoFrame(g, p, new Vector3(0.46f, DeskTop, 0.12f));
                    PenCup(g, p, new Vector3(0.6f, DeskTop, 0.22f));
                    break;
                default:
                    Papers(g, p, new Vector3(0.45f, DeskTop, 0.36f), 4f);
                    Papers(g, p, new Vector3(0.47f, DeskTop + 0.024f, 0.35f), -7f);
                    Mug(g, new Vector3(-0.56f, DeskTop, 0.31f), p.blue);
                    break;
            }
        }

        static Transform Monitor(Transform parent, OfficePalette p, string name, Vector3 deskPoint, float yaw)
        {
            var m = Prims.Group(name, parent, deskPoint, yaw);
            Prims.Box("Base", m, new Vector3(0f, 0.008f, -0.03f), new Vector3(0.22f, 0.016f, 0.17f), p.darkPlastic, false);
            Prims.Box("Neck", m, new Vector3(0f, 0.17f, -0.055f), new Vector3(0.05f, 0.32f, 0.03f), p.darkPlastic, false);
            Prims.Box("Body", m, new Vector3(0f, 0.33f, -0.02f), new Vector3(0.56f, 0.34f, 0.035f), p.darkPlastic, false);
            Prims.Quad("Screen", m, new Vector3(0f, 0.335f, -0.0015f), new Vector2(0.53f, 0.31f), Vector3.forward, p.screenDesktop);
            Prims.PointLight("Screen_Glow", m, new Vector3(0f, 0.32f, 0.3f), 1.6f, 0.45f, ScreenGlow);
            return m;
        }

        static void Mug(Transform parent, Vector3 desk, Material mat)
        {
            Prims.Cylinder("Mug", parent, desk + new Vector3(0f, 0.05f, 0f), 0.04f, 0.1f, mat);
            Prims.Box("Mug_Handle", parent, desk + new Vector3(0.047f, 0.05f, 0f), new Vector3(0.012f, 0.055f, 0.014f), mat, false);
        }

        static void Papers(Transform parent, OfficePalette p, Vector3 desk, float yaw) =>
            Prims.Box("Papers", parent, desk + new Vector3(0f, 0.012f, 0f), new Vector3(0.22f, 0.024f, 0.3f), p.paper, false, new Vector3(0f, yaw, 0f));

        static Transform Phone(Transform parent, OfficePalette p, Vector3 desk)
        {
            var g = Prims.Group("Phone", parent, desk);
            Prims.Box("Body", g, new Vector3(0f, 0.03f, 0f), new Vector3(0.2f, 0.06f, 0.18f), p.darkPlastic, false);
            Prims.Box("Handset", g, new Vector3(-0.06f, 0.075f, 0f), new Vector3(0.05f, 0.035f, 0.19f), p.darkPlastic, false);
            Prims.Flat("Keypad", g, new Vector3(0.04f, 0.061f, 0.02f), new Vector3(0.08f, 0.002f, 0.1f), p.lightPlastic);
            return g;
        }

        static void Lamp(Transform parent, OfficePalette p, Vector3 desk)
        {
            Prims.Cylinder("Lamp_Base", parent, desk + new Vector3(0f, 0.01f, 0f), 0.07f, 0.02f, p.darkPlastic);
            Prims.Box("Lamp_Arm", parent, desk + new Vector3(0f, 0.2f, 0.03f), new Vector3(0.02f, 0.38f, 0.02f), p.metal, false, new Vector3(-10f, 0f, 0f));
            Prims.Box("Lamp_Head", parent, desk + new Vector3(0f, 0.39f, 0.1f), new Vector3(0.1f, 0.05f, 0.17f), p.darkPlastic, false, new Vector3(20f, 0f, 0f));
            Prims.Sphere("Lamp_Bulb", parent, desk + new Vector3(0f, 0.355f, 0.13f), new Vector3(0.05f, 0.03f, 0.05f), p.dialGlow);
            Prims.PointLight("Lamp_Light", parent, desk + new Vector3(0f, 0.3f, 0.15f), 1.7f, 0.9f, LampGlow);
        }

        static void PhotoFrame(Transform parent, OfficePalette p, Vector3 desk) =>
            Prims.Box("Photo_Frame", parent, desk + new Vector3(0f, 0.08f, 0f), new Vector3(0.13f, 0.16f, 0.015f), p.darkPlastic, false, new Vector3(-12f, 0f, 0f));

        static void PenCup(Transform parent, OfficePalette p, Vector3 desk)
        {
            Prims.Cylinder("Pen_Cup", parent, desk + new Vector3(0f, 0.05f, 0f), 0.035f, 0.1f, p.darkPlastic);
            Prims.Box("Pen", parent, desk + new Vector3(0.01f, 0.12f, 0f), new Vector3(0.008f, 0.14f, 0.008f), p.blue, false, new Vector3(0f, 0f, 8f));
            Prims.Box("Pen", parent, desk + new Vector3(-0.01f, 0.12f, 0.01f), new Vector3(0.008f, 0.14f, 0.008f), p.red, false, new Vector3(-6f, 0f, -5f));
        }

        static void Props(Transform parent, OfficePalette p)
        {
            Bookcase(parent, p);
            Copier(parent, p);
            Decor.BigPlant(parent, p, "Ficus", new Vector3(3.9f, 0f, 0.7f));
            Decor.WaterCooler(parent, p, new Vector3(0.85f, 0f, 11.55f), 90f);
            Decor.CoatRack(parent, p, new Vector3(7.55f, 0f, 0.5f));
            Decor.Extinguisher(parent, p, new Vector3(Width, 0f, 1.75f), -90f);
            Whiteboard(parent, p);
            Clock(parent, p);

            Decor.Poster(parent, p, "Poster_W1", new Vector3(0f, 1.65f, 3.9f), 90f, Poster(p, 0), new Vector2(0.5f, 0.7f));
            Decor.Poster(parent, p, "Poster_W2", new Vector3(0f, 1.65f, 7f), 90f, Poster(p, 1), new Vector2(0.5f, 0.7f));
            Decor.Poster(parent, p, "Poster_E1", new Vector3(Width, 1.65f, 4.775f), -90f, Poster(p, 2), new Vector2(0.5f, 0.7f));
            Decor.Poster(parent, p, "Poster_E2", new Vector3(Width, 1.65f, 7.875f), -90f, Poster(p, 0), new Vector2(0.5f, 0.7f));
            Decor.Poster(parent, p, "Poster_E3", new Vector3(Width, 1.65f, 10.975f), -90f, Poster(p, 1), new Vector2(0.5f, 0.7f));

            Corkboard(parent, p);
            var exitSign = Prims.Group("Exit_Sign", parent, new Vector3(ExitX, 2.36f, Length), 180f);
            Prims.Flat("Box", exitSign, new Vector3(0f, 0f, 0.03f), new Vector3(0.44f, 0.16f, 0.05f), p.exitSign);
            Localized(Prims.Text("Text", exitSign, new Vector3(0f, -0.002f, 0.057f), Vector3.forward, "", 0.11f, Color.white, p.text), "sign.exit");
            Prims.PointLight("Glow", exitSign, new Vector3(0f, -0.1f, 0.25f), 1.8f, 0.6f, new Color(0.3f, 1f, 0.45f));

            var switchPlate = Prims.Group("Light_Switch", parent, new Vector3(EntranceX - 0.8f, 1.15f, 0f));
            Prims.Box("Plate", switchPlate, new Vector3(0f, 0f, 0.006f), new Vector3(0.08f, 0.12f, 0.012f), p.whitePlastic, false);
            Prims.Box("Rocker", switchPlate, new Vector3(0f, 0f, 0.014f), new Vector3(0.03f, 0.05f, 0.006f), p.lightPlastic, false);
            var thermostat = Prims.Group("Thermostat", parent, new Vector3(7.3f, 1.5f, 0f));
            Prims.Box("Body", thermostat, new Vector3(0f, 0f, 0.012f), new Vector3(0.12f, 0.12f, 0.024f), p.whitePlastic, false);
            Prims.Flat("Display", thermostat, new Vector3(0f, 0.015f, 0.0245f), new Vector3(0.06f, 0.03f, 0.002f), p.copierScreen);
        }

        static Material Poster(OfficePalette p, int i) => p.posters.Length == 0 ? p.paper : p.posters[i % p.posters.Length];

        public static void Localized(TextMesh text, string key)
        {
            var localized = text.gameObject.AddComponent<LocalizedText>();
            localized.key = key;
            localized.Apply();
        }

        static Transform Bookcase(Transform parent, OfficePalette p)
        {
            const float w = 1.4f, d = 0.42f, h = 2f;
            var b = Prims.Group("Bookcase", parent, new Vector3(0f, 0f, 11.1f), 90f);
            var hit = b.gameObject.AddComponent<BoxCollider>();
            hit.center = new Vector3(0f, h / 2f, d / 2f);
            hit.size = new Vector3(w, h, d);
            Prims.Box("Side_A", b, new Vector3(-w / 2f + 0.01f, h / 2f, d / 2f), new Vector3(0.02f, h, d), p.deskTop, false);
            Prims.Box("Side_B", b, new Vector3(w / 2f - 0.01f, h / 2f, d / 2f), new Vector3(0.02f, h, d), p.deskTop, false);
            Prims.Box("Top", b, new Vector3(0f, h - 0.01f, d / 2f), new Vector3(w, 0.02f, d), p.deskTop, false);
            Prims.Box("Plinth", b, new Vector3(0f, 0.04f, d / 2f), new Vector3(w - 0.04f, 0.08f, d), p.deskTop, false);
            Prims.Box("Back", b, new Vector3(0f, h / 2f, 0.006f), new Vector3(w - 0.04f, h, 0.012f), p.deskTop, false);
            foreach (float y in new[] { 0.47f, 0.86f, 1.25f, 1.64f })
                Prims.Box("Shelf", b, new Vector3(0f, y, d / 2f), new Vector3(w - 0.04f, 0.02f, d - 0.02f), p.deskTop, false);

            var binders = Prims.Group("Binders", b);
            var random = new System.Random(21);
            foreach (float level in new[] { 0.08f, 0.48f, 0.87f })
            {
                float x = -w / 2f + 0.04f;
                while (x < w / 2f - 0.13f)
                {
                    float bw = 0.055f + (float)random.NextDouble() * 0.03f, bh = 0.29f + (float)random.NextDouble() * 0.04f;
                    var mat = p.binders.Length > 0 ? p.binders[random.Next(p.binders.Length)] : p.darkPlastic;
                    Prims.Box("Binder", binders, new Vector3(x + bw / 2f, level + bh / 2f, d / 2f + 0.03f), new Vector3(bw - 0.004f, bh, 0.29f), mat, false);
                    x += bw;
                    if (random.NextDouble() < 0.08) x += 0.08f;
                }
            }
            for (int i = 0; i < 3; i++)
                Prims.Box("File_Box", b, new Vector3(-0.44f + i * 0.44f, 1.39f, d / 2f), new Vector3(0.36f, 0.26f, 0.34f), p.cardboard, false);
            Decor.SmallPlant(b, p, new Vector3(0.4f, h, d / 2f), 1.6f);
            return b;
        }

        static void Copier(Transform parent, OfficePalette p)
        {
            var c = Prims.Group("Copier", parent, new Vector3(Width, 0f, 3f), -90f);
            var hit = c.gameObject.AddComponent<BoxCollider>();
            hit.center = new Vector3(0.05f, 0.55f, 0.37f);
            hit.size = new Vector3(1.25f, 1.1f, 0.7f);
            Prims.Box("Body", c, new Vector3(0f, 0.47f, 0.37f), new Vector3(1f, 0.94f, 0.66f), p.lightPlastic, false);
            foreach (float y in new[] { 0.22f, 0.44f })
            {
                Prims.Flat("Tray_Line", c, new Vector3(0f, y, 0.701f), new Vector3(0.92f, 0.008f, 0.004f), p.darkPlastic);
                Prims.Box("Tray_Handle", c, new Vector3(0f, y + 0.09f, 0.71f), new Vector3(0.25f, 0.02f, 0.02f), p.darkPlastic, false);
            }
            Prims.Box("Scanner", c, new Vector3(0f, 1f, 0.37f), new Vector3(0.98f, 0.12f, 0.64f), p.whitePlastic, false);
            Prims.Box("Feeder", c, new Vector3(0.05f, 1.1f, 0.35f), new Vector3(0.62f, 0.08f, 0.46f), p.darkPlastic, false);
            Prims.Box("Panel", c, new Vector3(-0.33f, 1.05f, 0.66f), new Vector3(0.34f, 0.035f, 0.16f), p.darkPlastic, false, new Vector3(18f, 0f, 0f));
            Prims.Flat("Panel_Screen", c, new Vector3(-0.36f, 1.072f, 0.665f), new Vector3(0.14f, 0.004f, 0.07f), p.copierScreen, new Vector3(18f, 0f, 0f));
            Prims.Box("Output_Tray", c, new Vector3(0.6f, 0.78f, 0.4f), new Vector3(0.22f, 0.02f, 0.38f), p.darkPlastic, false);
            Prims.Box("Output_Paper", c, new Vector3(0.6f, 0.795f, 0.4f), new Vector3(0.2f, 0.01f, 0.29f), p.paper, false);
        }

        static void Whiteboard(Transform parent, OfficePalette p)
        {
            var g = Prims.Group("Whiteboard", parent, new Vector3(4.6f, 0f, Length), 180f);
            Prims.Box("Board", g, new Vector3(0f, 1.4f, 0.012f), new Vector3(2.4f, 1.1f, 0.02f), p.whiteboard, false);
            Prims.Box("Frame_Top", g, new Vector3(0f, 1.955f, 0.014f), new Vector3(2.42f, 0.025f, 0.028f), p.metal, false);
            Prims.Box("Frame_Bottom", g, new Vector3(0f, 0.845f, 0.014f), new Vector3(2.42f, 0.025f, 0.028f), p.metal, false);
            Prims.Box("Frame_A", g, new Vector3(-1.2f, 1.4f, 0.014f), new Vector3(0.025f, 1.135f, 0.028f), p.metal, false);
            Prims.Box("Frame_B", g, new Vector3(1.2f, 1.4f, 0.014f), new Vector3(0.025f, 1.135f, 0.028f), p.metal, false);
            Prims.Box("Tray", g, new Vector3(0f, 0.83f, 0.05f), new Vector3(1.8f, 0.025f, 0.07f), p.metal, false);
            Prims.Cylinder("Marker_Blue", g, new Vector3(-0.3f, 0.853f, 0.05f), 0.011f, 0.13f, p.blue, false, new Vector3(0f, 0f, 90f));
            Prims.Cylinder("Marker_Red", g, new Vector3(-0.12f, 0.853f, 0.055f), 0.011f, 0.13f, p.red, false, new Vector3(0f, 0f, 90f));
            Prims.Box("Eraser", g, new Vector3(0.28f, 0.86f, 0.05f), new Vector3(0.13f, 0.035f, 0.05f), p.darkPlastic, false);

            var notes = Prims.Group("Notes", g);
            Localized(Prims.Text("Title", notes, new Vector3(0f, 1.8f, 0.0235f), Vector3.forward, "", 0.11f, Marker, p.text,
                font: p.whiteboardFont), "board.title");
            for (int i = 0; i < 5; i++)
                Localized(Prims.Text("Line", notes, new Vector3(1.08f, 1.64f - i * 0.15f, 0.0235f), Vector3.forward, "", 0.08f, Marker, p.text,
                    TextAnchor.MiddleLeft, p.whiteboardFont), $"board.line{i + 1}");
        }

        static void Clock(Transform parent, OfficePalette p)
        {
            var g = Prims.Group("Clock", parent, new Vector3(4.6f, 2.27f, Length), 180f);
            Prims.Cylinder("Rim", g, new Vector3(0f, 0f, 0.02f), 0.18f, 0.04f, p.darkPlastic, false, new Vector3(90f, 0f, 0f));
            Prims.Quad("Face", g, new Vector3(0f, 0f, 0.0405f), new Vector2(0.32f, 0.32f), Vector3.forward, p.clockFace);

            void Hand(string name, float z, float angle, float length, float width, Material mat)
            {
                var pivot = Prims.Group(name, g, new Vector3(0f, 0f, z));
                pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
                Prims.Box("Hand", pivot, new Vector3(0f, length / 2f - 0.015f, 0f), new Vector3(width, length, 0.004f), mat, false);
            }

            Hand("Hour", 0.045f, 305f, 0.1f, 0.022f, p.darkPlastic);
            Hand("Minute", 0.049f, 60f, 0.14f, 0.014f, p.darkPlastic);
            Hand("Second", 0.053f, 200f, 0.15f, 0.006f, p.red);
            Prims.Cylinder("Cap", g, new Vector3(0f, 0f, 0.056f), 0.012f, 0.008f, p.darkPlastic, false, new Vector3(90f, 0f, 0f));
        }

        static void Corkboard(Transform parent, OfficePalette p)
        {
            var g = Prims.Group("Corkboard", parent, new Vector3(2.1f, 0f, 0f));
            Prims.Box("Cork", g, new Vector3(0f, 1.55f, 0.012f), new Vector3(1.6f, 0.9f, 0.02f), p.cork, false);
            Prims.Box("Frame_Top", g, new Vector3(0f, 2.012f, 0.014f), new Vector3(1.64f, 0.025f, 0.028f), p.wood, false);
            Prims.Box("Frame_Bottom", g, new Vector3(0f, 1.088f, 0.014f), new Vector3(1.64f, 0.025f, 0.028f), p.wood, false);
            Prims.Box("Frame_A", g, new Vector3(-0.808f, 1.55f, 0.014f), new Vector3(0.025f, 0.95f, 0.028f), p.wood, false);
            Prims.Box("Frame_B", g, new Vector3(0.808f, 1.55f, 0.014f), new Vector3(0.025f, 0.95f, 0.028f), p.wood, false);
            var notes = new[]
            {
                (new Vector2(-0.55f, 1.75f), new Vector2(0.21f, 0.28f), -4f, p.paper), (new Vector2(-0.22f, 1.8f), new Vector2(0.15f, 0.15f), 6f, p.stickyNote),
                (new Vector2(0.1f, 1.68f), new Vector2(0.21f, 0.28f), 3f, p.paper), (new Vector2(0.45f, 1.78f), new Vector2(0.15f, 0.15f), -8f, p.stickyNote),
                (new Vector2(-0.4f, 1.3f), new Vector2(0.28f, 0.2f), 2f, p.paper), (new Vector2(0.3f, 1.32f), new Vector2(0.15f, 0.15f), 10f, p.stickyNote),
            };
            foreach (var (pos, size, roll, mat) in notes)
            {
                Prims.Quad("Note", g, new Vector3(pos.x, pos.y, 0.0235f), size, Vector3.forward, mat, roll);
                Prims.Sphere("Pin", g, new Vector3(pos.x, pos.y + size.y / 2f - 0.03f, 0.027f), Vector3.one * 0.016f, p.red);
            }
        }

        public sealed class AnomalySpec
        {
            public string File, Title;

            public string[] Replaces = new string[0];

            public Func<Transform, OfficePalette, Transform> Build;
        }

        public static readonly AnomalySpec[] AnomalySpecs =
        {
            new AnomalySpec { File = "CRT_TV", Title = "Ламповый телевизор", Build = (o, p) => OldTech.CrtTelevision(o, p, new Vector3(4f, 0f, 8.6f), 180f) },
            new AnomalySpec
            {
                File = "Rotary_Phone", Title = "Дисковый телефон", Replaces = new[] { "Workstations/Desk_E2/Phone" },
                Build = (o, p) => OnDesk(o, "Desk_E2", d => OldTech.RotaryPhone(d, p, new Vector3(0.52f, DeskTop, 0.25f), 0f)),
            },
            new AnomalySpec { File = "Radio", Title = "Радиоприёмник", Build = (o, p) => OnDesk(o, "Desk_W6", d => OldTech.Radio(d, p, new Vector3(-0.53f, DeskTop, 0.2f), 0f)) },
            new AnomalySpec
            {
                File = "Typewriter", Title = "Печатная машинка", Replaces = new[] { "Workstations/Desk_W5/Keyboard" },
                Build = (o, p) => OnDesk(o, "Desk_W5", d => OldTech.Typewriter(d, p, new Vector3(-0.05f, DeskTop, 0.5f), 0f)),
            },
            new AnomalySpec
            {
                File = "CRT_Monitor", Title = "Ламповый монитор", Replaces = new[] { "Workstations/Desk_W2/Monitor" },
                Build = (o, p) => OnDesk(o, "Desk_W2", d => OldTech.CrtMonitor(d, p, new Vector3(-0.05f, DeskTop, 0.36f), 0f)),
            },
            new AnomalySpec
            {
                File = "Boombox", Title = "Кассетный магнитофон",
                Build = (o, p) => Moved(o, OldTech.Boombox(o.Find("Props/Bookcase"), p, new Vector3(-0.3f, 2f, 0.21f), 0f)),
            },
            new AnomalySpec { File = "Gramophone", Title = "Граммофон", Build = (o, p) => OldTech.Gramophone(o, p, new Vector3(2.9f, 0f, 3.3f), 137f) },
            new AnomalySpec { File = "Film_Camera", Title = "Кинокамера", Build = (o, p) => OldTech.FilmCamera(o, p, new Vector3(2.4f, 0f, 1.6f), 114f) },
        };

        static Transform OnDesk(Transform office, string desk, Func<Transform, Transform> build) =>
            Moved(office, build(office.Find("Workstations/" + desk)));

        static Transform Moved(Transform office, Transform obj)
        {
            obj.SetParent(office, true);
            return obj;
        }
    }
}
