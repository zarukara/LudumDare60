using System.Collections.Generic;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// The way out after the last stage: an empty hall in place of the office, the same entrance door, and a wide
    /// opening full of daylight in the north wall under a green "ВЫХОД" sign.
    /// </summary>
    public static class FinishHallBuilder
    {
        const float OpeningHalf = 1f, OpeningTop = 2.5f;

        public static GameObject Build(Transform parent, OfficePalette p, Vector3 origin, out Bounds finishZone)
        {
            const float w = OfficeRoomBuilder.Width, l = OfficeRoomBuilder.Length, h = OfficeRoomBuilder.Height, t = OfficeRoomBuilder.Wall;
            float cx = w / 2f;
            var root = Prims.Group("Exit_Hall", parent, origin);

            Prims.Box("Floor", root, new Vector3(cx, -0.05f, l / 2f), new Vector3(w + 2f * t, 0.1f, l + 2f * t), p.corridorFloor);
            Prims.Box("Ceiling", root, new Vector3(cx, h + 0.05f, l / 2f), new Vector3(w + t, 0.1f, l + t), p.ceiling, false);
            var door = new OfficeRoomBuilder.Opening(OfficeRoomBuilder.EntranceX - 0.5f, OfficeRoomBuilder.EntranceX + 0.5f, 0f, OfficeRoomBuilder.DoorHeight);
            var opening = new OfficeRoomBuilder.Opening(cx - OpeningHalf, cx + OpeningHalf, 0f, OpeningTop);
            OfficeRoomBuilder.WallPieces(root, p.wall, "Wall_South", true, -t / 2f, -t, w + t, h + 0.1f, t, new List<OfficeRoomBuilder.Opening> { door });
            OfficeRoomBuilder.WallPieces(root, p.wall, "Wall_North", true, l + t / 2f, -t, w + t, h + 0.1f, t, new List<OfficeRoomBuilder.Opening> { opening });
            OfficeRoomBuilder.WallPieces(root, p.wall, "Wall_West", false, -t / 2f, 0f, l, h + 0.1f, t, new List<OfficeRoomBuilder.Opening>());
            OfficeRoomBuilder.WallPieces(root, p.wall, "Wall_East", false, w + t / 2f, 0f, l, h + 0.1f, t, new List<OfficeRoomBuilder.Opening>());
            OfficeRoomBuilder.Door(root, p, "Entrance_Door", OfficeRoomBuilder.EntranceX, -t / 2f);

            // Short white passage ending in pure light; an invisible wall stops the player inside it.
            float passageEnd = l + t + 0.6f;
            Prims.Box("Passage_A", root, new Vector3(cx - OpeningHalf - 0.1f, OpeningTop / 2f, (l + passageEnd) / 2f), new Vector3(0.2f, OpeningTop, passageEnd - l), p.wall);
            Prims.Box("Passage_B", root, new Vector3(cx + OpeningHalf + 0.1f, OpeningTop / 2f, (l + passageEnd) / 2f), new Vector3(0.2f, OpeningTop, passageEnd - l), p.wall);
            Prims.Box("Passage_Top", root, new Vector3(cx, OpeningTop + 0.1f, (l + passageEnd) / 2f), new Vector3(2f * OpeningHalf + 0.4f, 0.2f, passageEnd - l), p.wall, false);
            Prims.Box("Passage_Floor", root, new Vector3(cx, -0.05f, (l + t + passageEnd) / 2f), new Vector3(2f * OpeningHalf, 0.1f, passageEnd - l - t), p.corridorFloor);
            Prims.Quad("Daylight", root, new Vector3(cx, OpeningTop / 2f, passageEnd), new Vector2(2f * OpeningHalf + 0.1f, OpeningTop + 0.1f), Vector3.back, p.windowLight);
            var stopper = new GameObject("Stopper");
            stopper.transform.SetParent(root, false);
            stopper.transform.localPosition = new Vector3(cx, 1.2f, passageEnd - 0.15f);
            stopper.AddComponent<BoxCollider>().size = new Vector3(2f * OpeningHalf, 2.4f, 0.1f);
            Prims.SpotLight("Daylight_Light", root, new Vector3(cx, OpeningTop - 0.2f, passageEnd - 0.1f), new Vector3(0f, -0.35f, -1f), 120f, 16f, 7f, new Color(1f, 0.98f, 0.94f), true);

            var sign = Prims.Group("Exit_Sign", root, new Vector3(cx, OpeningTop + 0.25f, l), 180f);
            Prims.Flat("Box", sign, new Vector3(0f, 0f, 0.035f), new Vector3(0.9f, 0.3f, 0.06f), p.exitSign);
            Prims.Text("Text", sign, new Vector3(0f, -0.004f, 0.067f), Vector3.forward, "ВЫХОД", 0.22f, Color.white, p.text);

            foreach (float z in new[] { 3f, 7.5f })
            {
                Prims.Flat("Light_Panel", root, new Vector3(cx, h - 0.018f, z), new Vector3(0.6f, 0.02f, 1.2f), p.lightPanel);
                Prims.PointLight("Light", root, new Vector3(cx, h - 0.3f, z), 6f, 1.6f, new Color(1f, 0.95f, 0.88f));
            }
            Decor.CornerPlant(root, p, new Vector3(cx - 1.7f, 0f, l - 0.5f));
            Decor.CornerPlant(root, p, new Vector3(cx + 1.7f, 0f, l - 0.5f));

            finishZone = new Bounds(origin + new Vector3(cx, 1f, l - 0.4f), new Vector3(2f * OpeningHalf + 0.4f, 2.5f, 1.4f));
            return root.gameObject;
        }
    }
}
