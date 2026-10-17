using System.Collections.Generic;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Floor plan of one stage on a grid of 2 m corridor cells: cell (i, j) covers x = 2i-1..2i+1, z = 2j..2j+2.
    /// Walking order: entry seam, a short random corridor, the office, the exit seam.
    ///
    /// A seam is two cells with the stage line between them (the entry line is z = 0, x = -1..1) and an S-bend on
    /// each side: corner, one cell sideways, corner. From the line nothing beyond the bends can be seen, the seam
    /// looks the same after a half turn, and the exit seam (right behind the office) is a copy of the entry seam.
    /// So a player crossing either line can be moved onto the entry line of a freshly built stage without noticing
    /// (turned around when they walked back).
    ///
    /// The random corridor only goes forward (+Z) or sideways in one direction, so it never touches itself or
    /// anything else.
    /// </summary>
    public sealed class StageLayout
    {
        public const float Cell = 2f;

        /// <summary>Cells from the dead end behind the entry line up to the cell under the office entrance.</summary>
        public readonly List<Vector2Int> Before = new List<Vector2Int>();

        /// <summary>Cells from the cell above the office exit up to the dead end beyond the exit line.</summary>
        public readonly List<Vector2Int> After = new List<Vector2Int>();

        /// <summary>Seam cells: always built the same way (no random decoration).</summary>
        public readonly HashSet<Vector2Int> Fixed = new HashSet<Vector2Int>();

        /// <summary>Office position: its inner south-west corner is at (2 * Room.x, 0, 2 * Room.y).</summary>
        public Vector2Int Room { get; private set; }

        /// <summary>Front cell of the exit seam; the exit line runs along its southern edge.</summary>
        public Vector2Int ExitSeam { get; private set; }

        public Vector2Int EntranceCell => Before[Before.Count - 1];
        public Vector2Int ExitCell => After[0];

        /// <summary>First cell after the entry seam, where the stage sign hangs.</summary>
        public static readonly Vector2Int SignCell = new Vector2Int(-2, 2);

        public Vector3 RoomOrigin => new Vector3(Room.x * Cell, 0f, Room.y * Cell);
        public Vector3 ExitLine => new Vector3(ExitSeam.x * Cell, 0f, ExitSeam.y * Cell);

        public static Vector3 CellCenter(Vector2Int c) => new Vector3(c.x * Cell, 0f, c.y * Cell + Cell / 2f);

        // Seam cells relative to the front cell, in walking order: up from the dead end, a cell sideways, up through
        // the line, a cell sideways. The first and last entries are the dead ends.
        static readonly Vector2Int[] SeamBack =
        {
            new Vector2Int(2, -3), new Vector2Int(2, -2), new Vector2Int(1, -2), new Vector2Int(0, -2), new Vector2Int(0, -1),
        };
        static readonly Vector2Int[] SeamFront =
        {
            new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1), new Vector2Int(-2, 1),
        };

        public static StageLayout Generate(System.Random rng)
        {
            var layout = new StageLayout();

            // Entry seam (its dead end included), then a short corridor up to the office entrance.
            foreach (var c in SeamBack) layout.AddFixed(layout.Before, c);
            foreach (var c in SeamFront) layout.AddFixed(layout.Before, c);
            var cur = new Vector2Int(-2, 1);
            Up(layout.Before, ref cur, 2); // the cell right after the seam always exists and is straight
            int runs = new[] { 0, 1, 1, 2 }[rng.Next(4)];
            int dir = rng.Next(2) == 0 ? -1 : 1;
            for (int r = 0; r < runs; r++)
            {
                for (int k = rng.Next(1, 3); k > 0; k--)
                {
                    cur.x += dir;
                    layout.Before.Add(cur);
                }
                Up(layout.Before, ref cur, rng.Next(1, 3));
            }
            Up(layout.Before, ref cur, 1); // the entrance cell, right under the door
            layout.Room = new Vector2Int(cur.x - 3, cur.y + 1);

            // The exit seam starts right above the office's north door: exit cell, corner, sideways, corner, line.
            var exitCell = new Vector2Int(layout.Room.x + 1, layout.Room.y + 6);
            layout.ExitSeam = exitCell + new Vector2Int(-2, 3);
            layout.After.Add(exitCell);
            foreach (var c in SeamBack) if (c != SeamBack[0]) layout.AddFixed(layout.After, layout.ExitSeam + c);
            foreach (var c in SeamFront) layout.AddFixed(layout.After, layout.ExitSeam + c);
            layout.AddFixed(layout.After, layout.ExitSeam + new Vector2Int(-2, 2)); // dead end beyond the exit line
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
    }
}
