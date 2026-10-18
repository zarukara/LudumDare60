using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Old tech that should not be in a modern office: the anomalies of the game. Simple stand-ins built from
    /// primitives; each is a group standing on its surface (floor or desk top) with its front along local +Z.
    /// Artists can replace the children with real models later, the anomaly logic only toggles the group.
    /// </summary>
    public static class OldTech
    {
        public static Transform CrtTelevision(Transform parent, OfficePalette p, Vector3 floor, float yaw)
        {
            var g = Prims.Group("CRT_TV", parent, floor, yaw);
            Prims.Box("Stand_Top", g, new Vector3(0f, 0.6f, 0f), new Vector3(0.7f, 0.03f, 0.5f), p.wood);
            Prims.Box("Stand_Shelf", g, new Vector3(0f, 0.18f, 0f), new Vector3(0.64f, 0.02f, 0.44f), p.wood, false);
            foreach (float x in new[] { -0.32f, 0.32f })
            foreach (float z in new[] { -0.22f, 0.22f })
                Prims.Box("Leg", g, new Vector3(x, 0.3f, z), new Vector3(0.03f, 0.6f, 0.03f), p.wood, false);

            Prims.Box("Body", g, new Vector3(0f, 0.875f, -0.02f), new Vector3(0.62f, 0.52f, 0.46f), p.wood);
            Prims.Box("Tube_Back", g, new Vector3(0f, 0.86f, -0.33f), new Vector3(0.42f, 0.36f, 0.2f), p.bakelite, false);
            Prims.Box("Bezel", g, new Vector3(-0.06f, 0.875f, 0.215f), new Vector3(0.44f, 0.42f, 0.02f), p.bakelite, false);
            Prims.Box("Screen", g, new Vector3(-0.06f, 0.875f, 0.227f), new Vector3(0.38f, 0.32f, 0.01f), p.tvStatic, false);
            Prims.PointLight("Screen_Glow", g, new Vector3(-0.06f, 0.9f, 0.6f), 2.2f, 0.8f, new Color(0.7f, 0.78f, 0.9f));
            Prims.Box("Control_Panel", g, new Vector3(0.235f, 0.875f, 0.214f), new Vector3(0.12f, 0.42f, 0.02f), p.grille, false);
            foreach (float y in new[] { 1.0f, 0.9f })
                Prims.Cylinder("Knob", g, new Vector3(0.235f, y, 0.235f), 0.028f, 0.03f, p.chrome, false, new Vector3(90f, 0f, 0f));
            Prims.Cylinder("Antenna_Base", g, new Vector3(0f, 1.15f, -0.05f), 0.05f, 0.03f, p.bakelite);
            Prims.Box("Antenna_L", g, new Vector3(-0.12f, 1.33f, -0.05f), new Vector3(0.008f, 0.4f, 0.008f), p.chrome, false, new Vector3(0f, 0f, 35f));
            Prims.Box("Antenna_R", g, new Vector3(0.12f, 1.33f, -0.05f), new Vector3(0.008f, 0.4f, 0.008f), p.chrome, false, new Vector3(0f, 0f, -35f));
            return g;
        }

        public static Transform RotaryPhone(Transform parent, OfficePalette p, Vector3 desk, float yaw)
        {
            var g = Prims.Group("Rotary_Phone", parent, desk, yaw);
            Prims.Box("Base", g, new Vector3(0f, 0.03f, 0f), new Vector3(0.22f, 0.06f, 0.24f), p.bakelite, false);
            Prims.Box("Body", g, new Vector3(0f, 0.085f, -0.02f), new Vector3(0.19f, 0.07f, 0.17f), p.bakelite, false, new Vector3(-12f, 0f, 0f));
            Prims.Cylinder("Dial", g, new Vector3(0f, 0.095f, 0.075f), 0.065f, 0.012f, p.chrome, false, new Vector3(35f, 0f, 0f));
            Prims.Cylinder("Dial_Centre", g, new Vector3(0f, 0.1f, 0.079f), 0.024f, 0.014f, p.paper, false, new Vector3(35f, 0f, 0f));
            foreach (float x in new[] { -0.075f, 0.075f })
                Prims.Box("Cradle", g, new Vector3(x, 0.135f, -0.04f), new Vector3(0.03f, 0.03f, 0.05f), p.bakelite, false);
            Prims.Box("Handset", g, new Vector3(0f, 0.165f, -0.04f), new Vector3(0.24f, 0.03f, 0.045f), p.bakelite, false);
            Prims.Box("Earpiece", g, new Vector3(-0.11f, 0.155f, -0.04f), new Vector3(0.06f, 0.05f, 0.07f), p.bakelite, false);
            Prims.Box("Mouthpiece", g, new Vector3(0.11f, 0.155f, -0.04f), new Vector3(0.06f, 0.05f, 0.07f), p.bakelite, false);
            Prims.Box("Cord", g, new Vector3(0.15f, 0.012f, 0.03f), new Vector3(0.018f, 0.018f, 0.2f), p.bakelite, false, new Vector3(0f, 20f, 0f));
            return g;
        }

        public static Transform Radio(Transform parent, OfficePalette p, Vector3 surface, float yaw)
        {
            var g = Prims.Group("Radio", parent, surface, yaw);
            Prims.Box("Case", g, new Vector3(0f, 0.13f, 0f), new Vector3(0.38f, 0.26f, 0.2f), p.wood, false);
            Prims.Box("Grille", g, new Vector3(-0.08f, 0.14f, 0.101f), new Vector3(0.18f, 0.19f, 0.004f), p.grille, false);
            Prims.Box("Dial_Window", g, new Vector3(0.1f, 0.17f, 0.101f), new Vector3(0.13f, 0.08f, 0.004f), p.dialGlow, false);
            Prims.Box("Dial_Needle", g, new Vector3(0.09f, 0.17f, 0.104f), new Vector3(0.004f, 0.07f, 0.002f), p.red, false);
            foreach (float x in new[] { 0.065f, 0.135f })
                Prims.Cylinder("Knob", g, new Vector3(x, 0.075f, 0.11f), 0.02f, 0.025f, p.bakelite, false, new Vector3(90f, 0f, 0f));
            foreach (float x in new[] { -0.1f, 0.1f })
                Prims.Box("Handle_Post", g, new Vector3(x, 0.28f, 0f), new Vector3(0.015f, 0.04f, 0.015f), p.chrome, false);
            Prims.Box("Handle", g, new Vector3(0f, 0.3f, 0f), new Vector3(0.22f, 0.015f, 0.03f), p.chrome, false);
            Prims.Box("Antenna", g, new Vector3(0.16f, 0.42f, -0.06f), new Vector3(0.006f, 0.32f, 0.006f), p.chrome, false, new Vector3(0f, 0f, -12f));
            return g;
        }

        public static Transform Typewriter(Transform parent, OfficePalette p, Vector3 desk, float yaw)
        {
            var g = Prims.Group("Typewriter", parent, desk, yaw);
            Prims.Box("Base", g, new Vector3(0f, 0.04f, 0f), new Vector3(0.44f, 0.08f, 0.36f), p.vintageGreen, false);
            Prims.Box("Body", g, new Vector3(0f, 0.11f, -0.07f), new Vector3(0.42f, 0.1f, 0.2f), p.vintageGreen, false);
            Prims.Box("Key_Slope", g, new Vector3(0f, 0.08f, 0.1f), new Vector3(0.4f, 0.03f, 0.15f), p.vintageGreen, false, new Vector3(12f, 0f, 0f));
            for (int row = 0; row < 4; row++)
            for (int i = 0; i < 10; i++)
            {
                var pos = new Vector3(-0.18f + i * 0.04f + row * 0.008f, 0.1f + row * 0.012f, 0.16f - row * 0.033f);
                Prims.Cylinder("Key", g, pos, 0.012f, 0.012f, p.paper);
            }
            Prims.Box("Space_Bar", g, new Vector3(0f, 0.085f, 0.19f), new Vector3(0.2f, 0.012f, 0.02f), p.bakelite, false);
            Prims.Cylinder("Roller", g, new Vector3(0f, 0.19f, -0.1f), 0.03f, 0.5f, p.bakelite, false, new Vector3(0f, 0f, 90f));
            Prims.Box("Paper", g, new Vector3(0f, 0.27f, -0.115f), new Vector3(0.21f, 0.16f, 0.002f), p.paper, false, new Vector3(-10f, 0f, 0f));
            Prims.Box("Return_Lever", g, new Vector3(-0.28f, 0.22f, -0.08f), new Vector3(0.07f, 0.012f, 0.012f), p.chrome, false, new Vector3(0f, 0f, 20f));
            return g;
        }

        public static Transform CrtMonitor(Transform parent, OfficePalette p, Vector3 desk, float yaw)
        {
            var g = Prims.Group("CRT_Monitor", parent, desk, yaw);
            Prims.Box("Stand", g, new Vector3(0f, 0.015f, -0.04f), new Vector3(0.24f, 0.03f, 0.22f), p.beige, false);
            Prims.Box("Case", g, new Vector3(0f, 0.23f, -0.03f), new Vector3(0.4f, 0.36f, 0.26f), p.beige, false);
            Prims.Box("Tube_Back", g, new Vector3(0f, 0.22f, -0.24f), new Vector3(0.3f, 0.26f, 0.16f), p.beige, false);
            Prims.Box("Bezel", g, new Vector3(0f, 0.24f, 0.101f), new Vector3(0.33f, 0.26f, 0.004f), p.lightPlastic, false);
            Prims.Box("Screen", g, new Vector3(0f, 0.245f, 0.104f), new Vector3(0.29f, 0.22f, 0.003f), p.terminalScreen, false);
            Prims.Box("Power_LED", g, new Vector3(0.16f, 0.08f, 0.102f), new Vector3(0.012f, 0.008f, 0.004f), p.exitSign, false);
            return g;
        }

        public static Transform Boombox(Transform parent, OfficePalette p, Vector3 surface, float yaw)
        {
            var g = Prims.Group("Boombox", parent, surface, yaw);
            Prims.Box("Body", g, new Vector3(0f, 0.12f, 0f), new Vector3(0.52f, 0.22f, 0.13f), p.chrome, false);
            Prims.Box("Front", g, new Vector3(0f, 0.12f, 0.066f), new Vector3(0.5f, 0.2f, 0.004f), p.bakelite, false);
            foreach (float x in new[] { -0.17f, 0.17f })
            {
                Prims.Cylinder("Speaker", g, new Vector3(x, 0.12f, 0.07f), 0.075f, 0.008f, p.grille, false, new Vector3(90f, 0f, 0f));
                Prims.Cylinder("Speaker_Cap", g, new Vector3(x, 0.12f, 0.075f), 0.025f, 0.008f, p.chrome, false, new Vector3(90f, 0f, 0f));
            }
            Prims.Box("Cassette_Door", g, new Vector3(0f, 0.125f, 0.07f), new Vector3(0.13f, 0.085f, 0.004f), p.lightPlastic, false);
            Prims.Box("Cassette_Window", g, new Vector3(0f, 0.135f, 0.073f), new Vector3(0.08f, 0.03f, 0.002f), p.tvScreen, false);
            Prims.Box("VU_Meter", g, new Vector3(0f, 0.19f, 0.07f), new Vector3(0.1f, 0.018f, 0.003f), p.dialGlow, false);
            for (int i = 0; i < 6; i++)
                Prims.Box("Key", g, new Vector3(-0.05f + i * 0.02f, 0.235f, 0.04f), new Vector3(0.014f, 0.012f, 0.025f), p.bakelite, false);
            foreach (float x in new[] { -0.18f, 0.18f })
                Prims.Box("Handle_Post", g, new Vector3(x, 0.27f, 0f), new Vector3(0.02f, 0.08f, 0.02f), p.bakelite, false);
            Prims.Box("Handle", g, new Vector3(0f, 0.31f, 0f), new Vector3(0.38f, 0.02f, 0.025f), p.bakelite, false);
            Prims.Box("Antenna", g, new Vector3(0.2f, 0.37f, -0.04f), new Vector3(0.006f, 0.3f, 0.006f), p.chrome, false, new Vector3(0f, 0f, -20f));
            return g;
        }

        /// <summary>Gramophone on a small round table (stands on the floor).</summary>
        public static Transform Gramophone(Transform parent, OfficePalette p, Vector3 floor, float yaw)
        {
            var g = Prims.Group("Gramophone", parent, floor, yaw);
            Prims.Cylinder("Table_Top", g, new Vector3(0f, 0.7f, 0f), 0.3f, 0.03f, p.wood, true);
            Prims.Cylinder("Table_Leg", g, new Vector3(0f, 0.35f, 0f), 0.03f, 0.68f, p.wood);
            Prims.Cylinder("Table_Foot", g, new Vector3(0f, 0.02f, 0f), 0.2f, 0.04f, p.wood);
            Prims.Box("Box", g, new Vector3(0f, 0.78f, 0f), new Vector3(0.32f, 0.13f, 0.32f), p.wood, false);
            Prims.Cylinder("Platter", g, new Vector3(0f, 0.85f, 0.02f), 0.13f, 0.01f, p.bakelite);
            Prims.Cylinder("Record_Label", g, new Vector3(0f, 0.856f, 0.02f), 0.04f, 0.004f, p.red);
            Prims.Box("Crank", g, new Vector3(0.19f, 0.79f, 0f), new Vector3(0.07f, 0.012f, 0.012f), p.chrome, false);
            Prims.Cylinder("Arm_Base", g, new Vector3(0.11f, 0.87f, -0.11f), 0.02f, 0.04f, p.brass);
            Prims.Box("Tone_Arm", g, new Vector3(0.07f, 0.885f, -0.03f), new Vector3(0.012f, 0.012f, 0.18f), p.brass, false, new Vector3(0f, -25f, 0f));
            Prims.Frustum("Horn", g, new Vector3(0.11f, 0.89f, -0.11f), 0.02f, 0.24f, 0.46f, p.brass, new Vector3(50f, 0f, 0f));
            return g;
        }

        /// <summary>Old film camera with two reels on a wooden tripod (stands on the floor).</summary>
        public static Transform FilmCamera(Transform parent, OfficePalette p, Vector3 floor, float yaw)
        {
            var g = Prims.Group("Film_Camera", parent, floor, yaw);
            var hit = g.gameObject.AddComponent<BoxCollider>();
            hit.center = new Vector3(0f, 0.8f, 0f);
            hit.size = new Vector3(0.6f, 1.6f, 0.6f);
            var head = new Vector3(0f, 1.25f, 0f);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 30f) * Mathf.Deg2Rad;
                Prims.Segment("Leg", g, head, new Vector3(Mathf.Sin(a) * 0.4f, 0f, Mathf.Cos(a) * 0.4f), 0.03f, p.wood);
            }
            Prims.Box("Head", g, head + new Vector3(0f, 0.03f, 0f), new Vector3(0.12f, 0.05f, 0.12f), p.bakelite, false);
            Prims.Box("Body", g, new Vector3(0f, 1.4f, 0f), new Vector3(0.14f, 0.2f, 0.3f), p.bakelite, false);
            Prims.Cylinder("Lens", g, new Vector3(0f, 1.4f, 0.2f), 0.045f, 0.12f, p.chrome, false, new Vector3(90f, 0f, 0f));
            Prims.Cylinder("Lens_Glass", g, new Vector3(0f, 1.4f, 0.262f), 0.038f, 0.004f, p.tvScreen, false, new Vector3(90f, 0f, 0f));
            foreach (float z in new[] { 0.07f, -0.13f })
                Prims.Cylinder("Reel", g, new Vector3(0f, 1.61f, z), 0.11f, 0.025f, p.bakelite, false, new Vector3(0f, 0f, 90f));
            Prims.Box("Crank", g, new Vector3(0.09f, 1.38f, -0.05f), new Vector3(0.012f, 0.08f, 0.012f), p.chrome, false);
            Prims.Sphere("Rec_Lamp", g, new Vector3(-0.05f, 1.48f, 0.15f), Vector3.one * 0.025f, p.recLamp);
            return g;
        }
    }
}
