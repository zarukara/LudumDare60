using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Props used both in the office and in the corridors. Each one is a group standing on the floor at its
    /// position; wall props face their local +Z (away from the wall).
    /// </summary>
    public static class Decor
    {
        /// <summary>Office chair; its local +Z is where the seated person looks. Only the seat collides.</summary>
        public static Transform Chair(Transform parent, OfficePalette p, string name, Vector3 floor, float yaw)
        {
            var c = Prims.Group(name, parent, floor, yaw);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Prims.Box("Leg", c, dir * 0.15f + Vector3.up * 0.07f, new Vector3(0.04f, 0.03f, 0.3f), p.darkPlastic, false, new Vector3(0f, a, 0f));
                Prims.Sphere("Caster", c, dir * 0.29f + Vector3.up * 0.03f, Vector3.one * 0.055f, p.darkPlastic);
            }
            Prims.Cylinder("Column", c, new Vector3(0f, 0.26f, 0f), 0.025f, 0.34f, p.metal);
            Prims.Box("Seat", c, new Vector3(0f, 0.46f, 0.02f), new Vector3(0.48f, 0.07f, 0.47f), p.chairFabric);
            Prims.Box("Back_Bar", c, new Vector3(0f, 0.6f, -0.24f), new Vector3(0.05f, 0.28f, 0.03f), p.darkPlastic, false);
            Prims.Box("Back", c, new Vector3(0f, 0.87f, -0.25f), new Vector3(0.44f, 0.48f, 0.05f), p.chairFabric, false, new Vector3(-8f, 0f, 0f));
            foreach (float x in new[] { -0.255f, 0.255f })
            {
                Prims.Box("Arm_Post", c, new Vector3(x, 0.56f, 0f), new Vector3(0.03f, 0.18f, 0.03f), p.darkPlastic, false);
                Prims.Box("Arm_Pad", c, new Vector3(x, 0.66f, 0.02f), new Vector3(0.05f, 0.025f, 0.24f), p.darkPlastic, false);
            }
            return c;
        }

        /// <summary>Tall ficus in a round pot (the circle on the office plan).</summary>
        public static Transform BigPlant(Transform parent, OfficePalette p, string name, Vector3 floor)
        {
            var g = Prims.Group(name, parent, floor);
            Prims.Cylinder("Pot", g, new Vector3(0f, 0.27f, 0f), 0.28f, 0.54f, p.pot, true);
            Prims.Cylinder("Soil", g, new Vector3(0f, 0.545f, 0f), 0.26f, 0.012f, p.soil);
            Prims.Cylinder("Trunk", g, new Vector3(0f, 0.95f, 0f), 0.035f, 0.9f, p.wood);
            var leaves = new[]
            {
                (new Vector3(0f, 1.35f, 0f), new Vector3(0.75f, 0.6f, 0.75f)),
                (new Vector3(-0.15f, 1.62f, 0.1f), new Vector3(0.55f, 0.5f, 0.55f)),
                (new Vector3(0.16f, 1.55f, -0.1f), new Vector3(0.5f, 0.45f, 0.5f)),
                (new Vector3(0f, 1.86f, -0.02f), new Vector3(0.45f, 0.4f, 0.45f)),
                (new Vector3(-0.2f, 1.22f, -0.15f), new Vector3(0.42f, 0.36f, 0.42f)),
                (new Vector3(0.2f, 1.2f, 0.16f), new Vector3(0.44f, 0.38f, 0.44f)),
            };
            foreach (var (pos, size) in leaves)
                Prims.Sphere("Leaves", g, pos, size, p.leaves);
            return g;
        }

        public static Transform SmallPlant(Transform parent, OfficePalette p, Vector3 floor, float scale = 1f)
        {
            var g = Prims.Group("Small_Plant", parent, floor);
            Prims.Cylinder("Pot", g, new Vector3(0f, 0.06f * scale, 0f), 0.055f * scale, 0.12f * scale, p.pot);
            Prims.Sphere("Leaves", g, new Vector3(0f, 0.17f * scale, 0f), new Vector3(0.18f, 0.16f, 0.18f) * scale, p.leaves);
            return g;
        }

        /// <summary>Floor plant for corridor corners.</summary>
        public static Transform CornerPlant(Transform parent, OfficePalette p, Vector3 floor)
        {
            var g = Prims.Group("Plant", parent, floor);
            Prims.Cylinder("Pot", g, new Vector3(0f, 0.22f, 0f), 0.21f, 0.44f, p.pot, true);
            Prims.Cylinder("Soil", g, new Vector3(0f, 0.445f, 0f), 0.19f, 0.01f, p.soil);
            Prims.Sphere("Leaves", g, new Vector3(0f, 0.85f, 0f), new Vector3(0.55f, 0.7f, 0.55f), p.leaves);
            Prims.Sphere("Leaves", g, new Vector3(0.08f, 1.15f, -0.05f), new Vector3(0.38f, 0.42f, 0.38f), p.leaves);
            return g;
        }

        public static Transform WaterCooler(Transform parent, OfficePalette p, Vector3 floor, float yaw)
        {
            var g = Prims.Group("Water_Cooler", parent, floor, yaw);
            Prims.Box("Cabinet", g, new Vector3(0f, 0.5f, 0f), new Vector3(0.32f, 1f, 0.32f), p.whitePlastic);
            Prims.Box("Tap_Panel", g, new Vector3(0f, 0.86f, 0.165f), new Vector3(0.2f, 0.12f, 0.01f), p.lightPlastic, false);
            Prims.Box("Tap_Cold", g, new Vector3(-0.05f, 0.86f, 0.18f), new Vector3(0.03f, 0.04f, 0.03f), p.blue, false);
            Prims.Box("Tap_Hot", g, new Vector3(0.05f, 0.86f, 0.18f), new Vector3(0.03f, 0.04f, 0.03f), p.red, false);
            Prims.Box("Drip_Tray", g, new Vector3(0f, 0.72f, 0.18f), new Vector3(0.18f, 0.02f, 0.06f), p.darkPlastic, false);
            Prims.Cylinder("Bottle", g, new Vector3(0f, 1.25f, 0f), 0.14f, 0.46f, p.water);
            Prims.Cylinder("Bottle_Neck", g, new Vector3(0f, 1.0f, 0f), 0.05f, 0.06f, p.water);
            Prims.Cylinder("Cups", g, new Vector3(0.2f, 0.75f, 0f), 0.04f, 0.26f, p.lightPlastic);
            return g;
        }

        /// <summary>Framed print on a wall; <paramref name="wallPoint"/> is on the wall face at the picture's centre height.</summary>
        public static Transform Poster(Transform parent, OfficePalette p, string name, Vector3 wallPoint, float yaw, Material picture, Vector2 size)
        {
            var g = Prims.Group(name, parent, wallPoint, yaw);
            Prims.Box("Frame", g, new Vector3(0f, 0f, 0.012f), new Vector3(size.x + 0.06f, size.y + 0.06f, 0.024f), p.darkPlastic, false);
            Prims.Quad("Picture", g, new Vector3(0f, 0f, 0.0245f), size, Vector3.forward, picture);
            return g;
        }

        public static Transform Bin(Transform parent, OfficePalette p, Vector3 floor, float radius = 0.14f, float height = 0.32f)
        {
            var g = Prims.Group("Bin", parent, floor);
            Prims.Cylinder("Bin", g, new Vector3(0f, height / 2f, 0f), radius, height, p.darkPlastic);
            Prims.Cylinder("Bag", g, new Vector3(0f, height - 0.01f, 0f), radius * 0.92f, 0.012f, p.whitePlastic);
            return g;
        }

        /// <summary>Wall-mounted extinguisher; the group stands on the floor at the wall face, facing +Z.</summary>
        public static Transform Extinguisher(Transform parent, OfficePalette p, Vector3 floorAtWall, float yaw)
        {
            var g = Prims.Group("Extinguisher", parent, floorAtWall, yaw);
            Prims.Box("Bracket", g, new Vector3(0f, 0.86f, 0.01f), new Vector3(0.07f, 0.14f, 0.02f), p.darkPlastic, false);
            Prims.Cylinder("Body", g, new Vector3(0f, 0.6f, 0.1f), 0.08f, 0.52f, p.red);
            Prims.Box("Valve", g, new Vector3(0f, 0.9f, 0.1f), new Vector3(0.05f, 0.07f, 0.05f), p.darkPlastic, false);
            Prims.Box("Lever", g, new Vector3(0f, 0.95f, 0.13f), new Vector3(0.035f, 0.02f, 0.11f), p.darkPlastic, false);
            Prims.Box("Hose", g, new Vector3(0.075f, 0.74f, 0.13f), new Vector3(0.02f, 0.34f, 0.02f), p.darkPlastic, false);
            Prims.Box("Sign", g, new Vector3(0f, 1.3f, 0.004f), new Vector3(0.2f, 0.2f, 0.008f), p.red, false);
            Prims.Box("Sign_Mark", g, new Vector3(0f, 1.3f, 0.009f), new Vector3(0.05f, 0.13f, 0.002f), p.whitePlastic, false);
            return g;
        }

        public static Transform CoatRack(Transform parent, OfficePalette p, Vector3 floor)
        {
            var g = Prims.Group("Coat_Rack", parent, floor);
            Prims.Cylinder("Base", g, new Vector3(0f, 0.015f, 0f), 0.22f, 0.03f, p.darkPlastic);
            Prims.Cylinder("Pole", g, new Vector3(0f, 0.9f, 0f), 0.02f, 1.75f, p.darkPlastic, true);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 45f;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Prims.Box("Hook", g, dir * 0.08f + Vector3.up * 1.68f, new Vector3(0.015f, 0.015f, 0.16f), p.darkPlastic, false, new Vector3(-25f, a, 0f));
            }
            Prims.Box("Coat", g, new Vector3(0.1f, 1.286f, 0.1f), new Vector3(0.42f, 0.86f, 0.13f), p.coat, false, new Vector3(4f, 45f, 3f));
            return g;
        }
    }
}
