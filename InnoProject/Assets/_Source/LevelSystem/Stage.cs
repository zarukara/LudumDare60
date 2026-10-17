using UnityEngine;

namespace LevelSystem
{
    /// <summary>One built stage: corridors, the office (or the exit hall) and the sign. Lives on the stage root.</summary>
    public class Stage : MonoBehaviour
    {
        public StageLayout Layout { get; set; }
        public GameObject Room { get; set; }
        public TextMesh[] SignTexts { get; set; }
        public bool IsFinish { get; set; }

        /// <summary>Exit hall only: walking in here ends the run (local to the stage root).</summary>
        public Bounds FinishZone { get; set; }

        /// <summary>Centre of the entry line, local to the stage root.</summary>
        public Vector3 EntryLine => StageLayout.EntryLine;

        /// <summary>Centre of the exit line, local to the stage root.</summary>
        public Vector3 ExitLine => Layout.ExitLine;
    }
}
