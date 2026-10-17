using UnityEngine;

namespace LevelSystem
{
    /// <summary>Puts a stage together: corridors from the layout, the office prefab (or exit hall) and the sign.</summary>
    public static class StageBuilder
    {
        static readonly Color ExitGreen = new Color(0.3f, 1f, 0.45f);

        public static Stage Build(Transform parent, OfficePalette p, GameObject roomPrefab, StageLayout layout, System.Random rng, string signText, bool finish)
        {
            var root = new GameObject(finish ? "Stage_Exit" : "Stage").transform;
            root.SetParent(parent, false);
            var stage = root.gameObject.AddComponent<Stage>();
            stage.Layout = layout;
            stage.IsFinish = finish;

            CorridorBuilder.Build(root, p, layout, rng, !finish);
            stage.SignTexts = CorridorBuilder.Sign(root, p, signText, finish ? ExitGreen : (Color?)null);

            if (finish)
            {
                stage.Room = FinishHallBuilder.Build(root, p, layout.RoomOrigin, out var zone);
                stage.FinishZone = zone;
            }
            else
            {
                stage.Room = roomPrefab != null ? Object.Instantiate(roomPrefab, root) : OfficeRoomBuilder.Build(p);
                stage.Room.name = "OfficeRoom";
                stage.Room.transform.SetParent(root, false);
                stage.Room.transform.localPosition = layout.RoomOrigin;
                stage.Room.transform.localRotation = Quaternion.identity;
            }
            return stage;
        }
    }
}
