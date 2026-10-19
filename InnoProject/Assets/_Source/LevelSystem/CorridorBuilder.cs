using System.Collections.Generic;
using UnityEngine;

namespace LevelSystem
{

    public static class CorridorBuilder
    {
        public const float Height = 2.6f, WallThickness = 0.15f;
        const float Half = StageLayout.Cell / 2f;
        const int North = 0, South = 2;

        static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        static readonly Color LightColor = new Color(0.78f, 0.92f, 0.84f);
        static readonly Color SignColor = new Color(1f, 0.82f, 0.25f);

        sealed class CellInfo
        {
            public readonly bool[] Open = new bool[4];
            public int Door = -1;
            public bool Fixed;

            public int Index;
        }

        public static void Build(Transform parent, OfficePalette p, StageLayout layout, System.Random rng, bool withExit)
        {
            var root = Prims.Group("Corridors", parent);
            var cells = new Dictionary<Vector2Int, CellInfo>();
            Link(cells, layout.Before, layout.Fixed);
            cells[layout.EntranceCell].Door = North;
            if (withExit)
            {
                Link(cells, layout.After, layout.Fixed);
                cells[layout.ExitCell].Door = South;
            }

            var walls = new HashSet<(Vector2Int, int)>();
            foreach (var pair in cells)
                BuildCell(root, p, pair.Key, pair.Value, walls, rng);
        }

        public static TextMesh[] Sign(Transform parent, OfficePalette p, string text, Color? color = null)
        {
            var g = Prims.Group("Stage_Sign", parent, StageLayout.CellCenter(StageLayout.SignCell) + Vector3.up * 2.15f);
            Prims.Box("Panel", g, Vector3.zero, new Vector3(1.1f, 0.32f, 0.07f), p.signPanel, false);
            foreach (float x in new[] { -0.4f, 0.4f })
                Prims.Box("Rod", g, new Vector3(x, 0.305f, 0f), new Vector3(0.015f, 0.29f, 0.015f), p.metal, false);
            var c = color ?? SignColor;
            return new[]
            {
                Prims.Text("Text_Front", g, new Vector3(0f, 0f, -0.037f), Vector3.back, text, 0.21f, c, p.text),
                Prims.Text("Text_Back", g, new Vector3(0f, 0f, 0.037f), Vector3.forward, text, 0.21f, c, p.text),
            };
        }

        static void Link(Dictionary<Vector2Int, CellInfo> cells, List<Vector2Int> path, HashSet<Vector2Int> fixedCells)
        {
            for (int i = 0; i < path.Count; i++)
            {
                var info = Get(cells, path[i]);
                info.Fixed = fixedCells.Contains(path[i]);
                info.Index = i;
                if (i == 0) continue;
                int d = System.Array.IndexOf(Steps, path[i] - path[i - 1]);
                Get(cells, path[i - 1]).Open[d] = true;
                info.Open[(d + 2) % 4] = true;
            }
        }

        static readonly bool[] SeamLights = { true, false, true, false, false, true, false, true };

        static bool Lit(int index) => index < SeamLights.Length ? SeamLights[index] : (index - SeamLights.Length) % 2 == 1;

        static CellInfo Get(Dictionary<Vector2Int, CellInfo> cells, Vector2Int c)
        {
            if (!cells.TryGetValue(c, out var info)) cells[c] = info = new CellInfo();
            return info;
        }

        static Vector3 Dir(int side) => new Vector3(Steps[side].x, 0f, Steps[side].y);

        static void BuildCell(Transform root, OfficePalette p, Vector2Int c, CellInfo info, HashSet<(Vector2Int, int)> walls, System.Random rng)
        {
            var g = Prims.Group($"Cell_{c.x}_{c.y}", root, StageLayout.CellCenter(c));
            float cell = StageLayout.Cell;
            Prims.Box("Floor", g, new Vector3(0f, -0.05f, 0f), new Vector3(cell, 0.1f, cell), p.corridorFloor);
            Prims.Box("Ceiling", g, new Vector3(0f, Height + 0.05f, 0f), new Vector3(cell, 0.1f, cell), p.ceiling, false);

            bool alongX = (info.Open[1] || info.Open[3]) && !(info.Open[0] || info.Open[2]);
            var panelSize = alongX ? new Vector3(1.2f, 0.02f, 0.6f) : new Vector3(0.6f, 0.02f, 1.2f);

            bool lit = info.Door >= 0 || Lit(info.Index);
            Prims.Flat("Panel_Frame", g, new Vector3(0f, Height - 0.008f, 0f), panelSize + new Vector3(0.06f, -0.004f, 0.06f), p.lightPlastic);
            Prims.Flat("Light_Panel", g, new Vector3(0f, Height - 0.018f, 0f), panelSize, lit ? p.lightPanel : p.lightPanelOff);

            if (lit) Prims.PointLight("Light", g, new Vector3(0f, Height - 0.25f, 0f), 3.4f, 0.6f, LightColor);

            for (int side = 0; side < 4; side++)
            {
                if (info.Open[side] || info.Door == side) continue;
                var dir = Dir(side);
                bool runsAlongX = side == 0 || side == 2;
                var key = side < 2 ? (c, side) : (c + Steps[side], side - 2);
                if (walls.Add(key))
                {
                    var size = runsAlongX ? new Vector3(cell + WallThickness, Height, WallThickness) : new Vector3(WallThickness, Height, cell + WallThickness);
                    Prims.Box("Wall", g, dir * Half + Vector3.up * (Height / 2f), size, p.wall);
                }
                var face = dir * (Half - WallThickness / 2f);
                float length = cell - WallThickness;
                Prims.Box("Baseboard", g, face - dir * 0.0075f + Vector3.up * 0.04f,
                    runsAlongX ? new Vector3(length, 0.08f, 0.015f) : new Vector3(0.015f, 0.08f, length), p.baseboard, false);
                Prims.Box("Rail", g, face - dir * 0.006f + Vector3.up * 0.95f,
                    runsAlongX ? new Vector3(length, 0.06f, 0.012f) : new Vector3(0.012f, 0.06f, length), p.baseboard, false);
            }

            if (!info.Fixed && info.Door < 0 && c != StageLayout.SignCell) Decorate(g, p, info, rng);
        }

        static void Decorate(Transform g, OfficePalette p, CellInfo info, System.Random rng)
        {
            var sides = new List<int>();
            for (int side = 0; side < 4; side++)
                if (!info.Open[side]) sides.Add(side);
            double roll = rng.NextDouble();

            if (sides.Count == 2 && sides[1] - sides[0] == 2)
            {

                int side = sides[rng.Next(2)];
                float shift = (float)(rng.NextDouble() - 0.5) * 0.6f;
                if (roll < 0.2) WallPoster(g, p, side, shift, rng);
                else if (roll < 0.34) SideDoor(g, p, side, rng);
                else if (roll < 0.44) Decor.Extinguisher(g, p, WallFloor(side, shift), WallYaw(side));
                else if (roll < 0.53) NoticeBoard(g, p, side, shift, rng);
                else if (roll < 0.59) Bench(g, p, side);
            }
            else if (sides.Count == 2)
            {

                var corner = (Dir(sides[0]) + Dir(sides[1])) * (Half - WallThickness / 2f - 0.32f);
                if (roll < 0.25) Decor.CornerPlant(g, p, corner);
                else if (roll < 0.37) Decor.Bin(g, p, corner, 0.16f, 0.4f);
                else if (roll < 0.45) Decor.WaterCooler(g, p, corner, Mathf.Atan2(-corner.x, -corner.z) * Mathf.Rad2Deg);
            }

            if (rng.NextDouble() < 0.2)
            {

                var spot = new Vector3(rng.Next(2) == 0 ? -0.6f : 0.6f, Height - 0.007f, rng.Next(2) == 0 ? -0.55f : 0.55f);
                Prims.Flat("Vent", g, spot, new Vector3(0.4f, 0.014f, 0.4f), p.whitePlastic);
                for (int i = 0; i < 4; i++)
                    Prims.Flat("Vent_Slot", g, spot + new Vector3(0f, -0.0075f, -0.135f + i * 0.09f), new Vector3(0.32f, 0.002f, 0.035f), p.darkPlastic);
            }
        }

        static Vector3 WallFloor(int side, float shift)
        {
            var dir = Dir(side);
            var along = new Vector3(-dir.z, 0f, dir.x);
            return dir * (Half - WallThickness / 2f) + along * shift;
        }

        static float WallYaw(int side)
        {
            var inward = -Dir(side);
            return Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
        }

        static void WallPoster(Transform g, OfficePalette p, int side, float shift, System.Random rng)
        {
            var picture = p.posters.Length > 0 ? p.posters[rng.Next(p.posters.Length)] : p.paper;
            Decor.Poster(g, p, "Poster", WallFloor(side, shift) + Vector3.up * 1.6f, WallYaw(side), picture, new Vector2(0.5f, 0.7f));
        }

        static void SideDoor(Transform g, OfficePalette p, int side, System.Random rng)
        {
            var d = Prims.Group("Side_Door", g, WallFloor(side, 0f), WallYaw(side));
            Prims.Box("Jamb_A", d, new Vector3(-0.5f, 1.07f, 0.02f), new Vector3(0.07f, 2.14f, 0.04f), p.doorFrame, false);
            Prims.Box("Jamb_B", d, new Vector3(0.5f, 1.07f, 0.02f), new Vector3(0.07f, 2.14f, 0.04f), p.doorFrame, false);
            Prims.Box("Head", d, new Vector3(0f, 2.14f, 0.02f), new Vector3(1.07f, 0.07f, 0.04f), p.doorFrame, false);
            Prims.Box("Leaf", d, new Vector3(0f, 1.035f, 0.015f), new Vector3(0.93f, 2.07f, 0.03f), p.doorLeaf, false);
            Prims.Box("Handle", d, new Vector3(0.34f, 1f, 0.045f), new Vector3(0.13f, 0.02f, 0.02f), p.metal, false);
            Prims.Box("Rosette", d, new Vector3(0.39f, 1f, 0.034f), new Vector3(0.05f, 0.05f, 0.01f), p.metal, false);
            Prims.Box("Plate", d, new Vector3(0f, 1.62f, 0.033f), new Vector3(0.17f, 0.08f, 0.006f), p.metal, false);
            Prims.Text("Number", d, new Vector3(0f, 1.62f, 0.0365f), Vector3.forward, (200 + rng.Next(10, 90)).ToString(), 0.06f, new Color(0.15f, 0.15f, 0.17f), p.text);
        }

        static void NoticeBoard(Transform g, OfficePalette p, int side, float shift, System.Random rng)
        {
            var b = Prims.Group("Notice_Board", g, WallFloor(side, shift), WallYaw(side));
            Prims.Box("Cork", b, new Vector3(0f, 1.5f, 0.012f), new Vector3(0.9f, 0.6f, 0.02f), p.cork, false);
            Prims.Box("Frame_Top", b, new Vector3(0f, 1.81f, 0.014f), new Vector3(0.94f, 0.02f, 0.028f), p.wood, false);
            Prims.Box("Frame_Bottom", b, new Vector3(0f, 1.19f, 0.014f), new Vector3(0.94f, 0.02f, 0.028f), p.wood, false);
            int notes = rng.Next(3, 6);
            for (int i = 0; i < notes; i++)
            {
                var pos = new Vector3(-0.32f + i * 0.16f, 1.45f + (float)rng.NextDouble() * 0.15f, 0.0235f);
                bool sticky = rng.Next(3) == 0;
                Prims.Quad("Note", b, pos, sticky ? new Vector2(0.12f, 0.12f) : new Vector2(0.15f, 0.21f), Vector3.forward,
                    sticky ? p.stickyNote : p.paper, (float)(rng.NextDouble() - 0.5) * 16f);
            }
        }

        static void Bench(Transform g, OfficePalette p, int side)
        {
            var b = Prims.Group("Bench", g, WallFloor(side, 0f), WallYaw(side));
            Prims.Box("Seat", b, new Vector3(0f, 0.45f, 0.24f), new Vector3(1.2f, 0.05f, 0.4f), p.wood);
            foreach (float x in new[] { -0.5f, 0.5f })
                Prims.Box("Leg", b, new Vector3(x, 0.215f, 0.24f), new Vector3(0.05f, 0.43f, 0.35f), p.metal, false);
        }
    }
}
