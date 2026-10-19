using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LevelSystem
{

    public sealed class StageLayout
    {
        public const float Cell = 2f;

        public readonly List<Vector2Int> Before = new List<Vector2Int>();

        public readonly List<Vector2Int> After = new List<Vector2Int>();

        public readonly HashSet<Vector2Int> Fixed = new HashSet<Vector2Int>();

        public Vector2Int Room { get; private set; }

        public Vector2Int ExitSeam { get; private set; }

        public Vector2Int EntranceCell => Before[Before.Count - 1];
        public Vector2Int ExitCell => After[0];

        public static readonly Vector2Int SignCell = new Vector2Int(-1, 2);

        public static readonly Vector3 Forward = Vector3.left;

        public static Vector3 EntryLine => LineOf(Vector2Int.zero);

        public Vector3 ExitLine => LineOf(ExitSeam);
        public Vector3 RoomOrigin => new Vector3(Room.x * Cell, 0f, Room.y * Cell);

        public static Vector3 CellCenter(Vector2Int c) => new Vector3(c.x * Cell, 0f, c.y * Cell + Cell / 2f);

        static Vector3 LineOf(Vector2Int front) => new Vector3(front.x * Cell + Cell / 2f, 0f, front.y * Cell + Cell / 2f);

        public static readonly Vector2Int[] SeamCells =
        {
            new Vector2Int(2, -2), new Vector2Int(2, -1), new Vector2Int(2, 0), new Vector2Int(1, 0),
            new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(-1, 2),
        };

        public static StageLayout Generate(System.Random rng, float sideStepChance = 0f)
        {
            var layout = new StageLayout();
            foreach (var c in SeamCells) layout.AddFixed(layout.Before, c);

            var cur = SeamCells[SeamCells.Length - 1];
            if (rng.NextDouble() < sideStepChance)
            {
                Up(layout.Before, ref cur, 1);
                int dir = rng.Next(2) == 0 ? -1 : 1;
                for (int k = rng.Next(1, 3); k > 0; k--)
                {
                    cur.x += dir;
                    layout.Before.Add(cur);
                }
                Up(layout.Before, ref cur, rng.Next(1, 3));
            }
            else
            {
                Up(layout.Before, ref cur, rng.Next(1, 4));
            }
            Up(layout.Before, ref cur, 1);
            layout.Room = new Vector2Int(cur.x - 3, cur.y + 1);

            var exitCell = new Vector2Int(layout.Room.x + 1, layout.Room.y + 6);
            layout.ExitSeam = exitCell - SeamCells[0];
            foreach (var c in SeamCells) layout.AddFixed(layout.After, layout.ExitSeam + c);
            return layout;
        }

        static void Up(List<Vector2Int> cells, ref Vector2Int cur, int count)
        {
            for (int k = 0; k < count; k++)
            {
                cur.y++;
                cells.Add(cur);
            }
        }

        void AddFixed(List<Vector2Int> cells, Vector2Int c)
        {
            cells.Add(c);
            Fixed.Add(c);
        }

        public bool IsValid() => Before.Concat(After).Distinct().Count() == Before.Count + After.Count;
    }
}
