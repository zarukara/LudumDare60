using UnityEngine;

namespace LevelSystem
{

    public class Stage : MonoBehaviour
    {
        public StageLayout Layout { get; set; }
        public GameObject Room { get; set; }
        public TextMesh[] SignTexts { get; set; }
        public bool IsFinish { get; set; }

        public Bounds FinishZone { get; set; }

        public Vector3 EntryLine => StageLayout.EntryLine;

        public Vector3 ExitLine => Layout.ExitLine;
    }
}
