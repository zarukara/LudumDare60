using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Floor plan of one stage on a grid of 2 m corridor cells: cell (i, j) covers x = 2i-1..2i+1, z = 2j..2j+2.
    /// Walking order: entry seam, a straight corridor north to the office, the office, the exit seam.
    ///
    /// A seam is a short westward piece with the stage line in the middle, reached by one turn and left by one
    /// turn: north, corner, west through the line, corner, north. From the line only the seam and about 1.6 m
    /// around each corner can be seen; the seam looks the same after a half turn, and the exit seam (two cells
    /// north of the office's exit door) is a copy of the entry seam. So a player crossing either line can be moved
    /// onto the entry line of a freshly built stage without noticing (turned around when they walked back).
    /// Two turns between offices in total, unless a side step is asked for.
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

        /// <summary>Front cell of the exit seam; the exit line runs along its eastern edge.</summary>
        public Vector2Int ExitSeam { get; private set; }

        public Vector2Int EntranceCell => Before[Before.Count - 1];
        public Vector2Int ExitCell => After[0];

        /// <summary>Cell after the entry seam's second turn where the stage sign hangs (not visible from the line).</summary>
        public static readonly Vector2Int SignCell = new Vector2Int(-1, 2);

        /// <summary>Walking direction through both stage lines.</summary>
        public static readonly Vector3 Forward = Vector3.left;

        /// <summary>Centre of the entry line (the east edge of cell (0, 0)).</summary>
        public static Vector3 EntryLine => LineOf(Vector2Int.zero);

        public Vector3 ExitLine => LineOf(ExitSeam);
        public Vector3 RoomOrigin => new Vector3(Room.x * Cell, 0f, Room.y * Cell);

        public static Vector3 CellCenter(Vector2Int c) => new Vector3(c.x * Cell, 0f, c.y * Cell + Cell / 2f);

        static Vector3 LineOf(Vector2Int front) => new Vector3(front.x * Cell + Cell / 2f, 0f, front.y * Cell + Cell / 2f);

        // Seam cells relative to the front cell, in walking order: two cells going north, the corner, west through
        // the line (between the 4th and 5th cell), the corner, two cells going north. Cell i and cell 7 - i face
        // each other across the line. On the entry side the first cell is a dead end; on the exit side it is the
        // cell above the office door, and the last one is a dead end.
        public static readonly Vector2Int[] SeamCells =
        {
            new Vector2Int(2, -2), new Vector2Int(2, -1), new Vector2Int(2, 0), new Vector2Int(1, 0),
            new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(-1, 2),
        };

        /// <param name="sideStepChance">Chance of one sideways jog on the way to the office (two more turns).</param>
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
            Up(layout.Before, ref cur, 1); // the entrance cell, right under the door
            layout.Room = new Vector2Int(cur.x - 3, cur.y + 1);

            // The exit seam starts at the cell above the office's north door.
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

        /// <summary>True if no cell is used twice (sanity check for tests).</summary>
        public bool IsValid() => Before.Concat(After).Distinct().Count() == Before.Count + After.Count;
    }
}
