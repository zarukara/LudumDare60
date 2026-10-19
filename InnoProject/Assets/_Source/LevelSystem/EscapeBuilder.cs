using System.Collections.Generic;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// What the end of the stairs needs, built when it is needed: the office corridor behind a landing's door, the
    /// hall with the glass doors out behind another, and invisible walls. Positions and yaws are local to the
    /// parent; a corridor stands with its front at the origin, the rest of it towards -Z.
    /// </summary>
    public static class EscapeBuilder
    {
        /// <summary>The office corridor behind a landing's door: carpet, closed doors either side, a lamp, dark at the end.</summary>
        public static Transform Corridor(Transform parent, OfficePalette p, Vector3 position, float yaw)
        {
            const float hw = 1f, length = 8f, h = 2.9f, t = OfficeRoomBuilder.Wall;
            var root = Prims.Group("Corridor", parent, position, yaw);
            Prims.Box("Floor", root, new Vector3(0f, -0.05f, -length / 2f), new Vector3(2f * hw, 0.1f, length), p.carpet);
            Prims.Box("Ceiling", root, new Vector3(0f, h + 0.05f, -length / 2f), new Vector3(2f * hw, 0.1f, length), p.ceiling);
            Prims.Box("Wall_End", root, new Vector3(0f, h / 2f, -length - t / 2f), new Vector3(2f * hw + 2f * t, h, t), p.wall);
            var doorsAt = new[] { -2.5f, -5.8f };
            foreach (float side in new[] { -1f, 1f })
            {
                // Each side wall has two shut office doors in it.
                var openings = new List<OfficeRoomBuilder.Opening>();
                foreach (float z in doorsAt) openings.Add(new OfficeRoomBuilder.Opening(z - 0.5f, z + 0.5f, 0f, OfficeRoomBuilder.DoorHeight));
                float x = side * (hw + t / 2f);
                OfficeRoomBuilder.WallPieces(root, p.wall, side < 0f ? "Wall_L" : "Wall_R", false, x, -length, 0f, h, t, openings);
                foreach (float z in doorsAt)
                    OfficeRoomBuilder.Door(Prims.Group("Office_Door", root, new Vector3(x, 0f, z), 90f), p, "Door", 0f, 0f).GetComponent<AutoDoor>().locked = true;
            }
            Prims.Flat("Light_Panel", root, new Vector3(0f, h - 0.01f, -2.5f), new Vector3(0.6f, 0.02f, 1.2f), p.lightPanel);
            Prims.PointLight("Light", root, new Vector3(0f, h - 0.4f, -2.5f), 6f, 1.1f, new Color(0.85f, 0.9f, 1f));
            return root;
        }

        public const float ExitWidth = 4.6f, ExitLength = 7f;

        /// <summary>
        /// The way out behind a landing's door: a small entrance hall with a stone floor, a reception desk, a bench and
        /// plants, and at the far end glass doors out into the morning, a lit exit sign over them. It has no wall of
        /// its own at the open end: it stands against the stairwell's wall, the door in that. Local +Z runs from there
        /// to the glass doors, <see cref="ExitLength"/> away.
        /// </summary>
        public static Transform Exit(Transform parent, OfficePalette p, Vector3 position, float yaw)
        {
            const float hw = ExitWidth / 2f, length = ExitLength, h = 3f, t = OfficeRoomBuilder.Wall;
            var root = Prims.Group("Exit_Hall", parent, position, yaw);
            Prims.Box("Floor", root, new Vector3(0f, -0.05f, length / 2f), new Vector3(2f * hw, 0.1f, length), p.corridorFloor);
            Prims.Box("Ceiling", root, new Vector3(0f, h + 0.05f, length / 2f), new Vector3(2f * hw, 0.1f, length), p.ceiling);
            var walls = Prims.Group("Walls", root);
            OfficeRoomBuilder.WallPieces(walls, p.wall, "Wall_Out", true, length + t / 2f, -hw - t, hw + t, h, t,
                new List<OfficeRoomBuilder.Opening> { new OfficeRoomBuilder.Opening(-1f, 1f, 0f, 2.4f) });
            Prims.Box("Wall_L", walls, new Vector3(-hw - t / 2f, h / 2f, length / 2f), new Vector3(t, h, length), p.wall);
            Prims.Box("Wall_R", walls, new Vector3(hw + t / 2f, h / 2f, length / 2f), new Vector3(t, h, length), p.wall);

            Prims.Box("Reception", root, new Vector3(1.55f, 0.55f, 3.6f), new Vector3(0.7f, 1.1f, 1.8f), p.wood);
            Prims.Box("Reception_Top", root, new Vector3(1.55f, 1.12f, 3.6f), new Vector3(0.8f, 0.04f, 1.9f), p.deskTop, false);
            Prims.Box("Bench", root, new Vector3(-1.95f, 0.25f, 4f), new Vector3(0.6f, 0.5f, 1.8f), p.chairFabric);
            foreach (var at in new[] { new Vector3(-1.95f, 0f, 0.8f), new Vector3(1.95f, 0f, 6.3f), new Vector3(-1.95f, 0f, 6.3f) })
            {
                Prims.Cylinder("Pot", root, at + Vector3.up * 0.2f, 0.22f, 0.4f, p.pot, true);
                Prims.Sphere("Leaves", root, at + Vector3.up * 0.85f, new Vector3(0.7f, 0.9f, 0.7f), p.leaves);
            }
            for (int i = 0; i < 2; i++)
            {
                float z = 1.8f + i * 3.4f;
                Prims.Flat("Light_Panel", root, new Vector3(0f, h - 0.01f, z), new Vector3(1.2f, 0.02f, 0.6f), p.lightPanel);
                Prims.PointLight("Light", root, new Vector3(0f, h - 0.4f, z), 7f, 1.3f, new Color(1f, 0.92f, 0.8f));
            }

            // The way out: glass doors, the morning behind them, the sign over them.
            foreach (float side in new[] { -1f, 1f })
                Prims.Flat("Glass", root, new Vector3(side * 0.5f, 1.2f, length + t / 2f), new Vector3(0.96f, 2.36f, 0.02f), p.glass);
            Prims.Box("Mullion", root, new Vector3(0f, 1.2f, length + t / 2f), new Vector3(0.05f, 2.4f, 0.06f), p.metal, false);
            Prims.Box("Porch", root, new Vector3(0f, -0.05f, length + t + 0.6f), new Vector3(2.4f, 0.1f, 1.2f), p.corridorFloor, false);
            Prims.Quad("Morning", root, new Vector3(0f, 1.6f, length + 1.2f), new Vector2(5f, 4f), Vector3.back, p.windowLight);
            Prims.SpotLight("Daylight", root, new Vector3(0f, 2.6f, length + 0.8f), new Vector3(0f, -0.35f, -1f), 70f, 9f, 2.2f, new Color(1f, 0.95f, 0.85f), false);
            ExitSign(root, p, new Vector3(0f, 2.65f, length - 0.02f), 180f);
            return root;
        }

        /// <summary>A lit green exit sign ("ВЫХОД" / "EXIT"), facing along the group's +Z.</summary>
        public static Transform ExitSign(Transform parent, OfficePalette p, Vector3 position, float yaw)
        {
            var sign = Prims.Group("Exit_Sign", parent, position, yaw);
            Prims.Flat("Panel", sign, new Vector3(0f, 0f, 0.015f), new Vector3(0.7f, 0.24f, 0.03f), p.exitSign);
            OfficeRoomBuilder.Localized(Prims.Text("Text", sign, new Vector3(0f, 0f, 0.032f), Vector3.forward, "", 0.15f, Color.white, p.text), "sign.exit");
            return sign;
        }

        /// <summary>
        /// An invisible wall (a box collider): local centre and size. On the Ignore Raycast layer, so her claws find
        /// no hold on it and it hides nobody from her eyes.
        /// </summary>
        public static GameObject Barrier(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name) { layer = 2 };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }
    }
}
