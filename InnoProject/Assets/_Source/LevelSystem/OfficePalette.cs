using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// All materials of the office level. Created and filled by the editor menu "Office/Build All"
    /// (assets in Assets/_Presentation/Office). Textured materials expect UVs in metres (see <see cref="Prims"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Office/Palette", fileName = "OfficePalette")]
    public class OfficePalette : ScriptableObject
    {
        [Header("Building")]
        public Material carpet;
        public Material corridorFloor;
        public Material wall;
        public Material baseboard;
        public Material ceiling;
        public Material lightPanel;
        public Material lightPanelOff;
        public Material doorLeaf;
        public Material doorFrame;
        public Material glass;
        public Material windowLight;
        public Material windowNight;
        public Material blinds;

        [Header("Furniture")]
        public Material deskTop;
        public Material deskFrame;
        public Material partition;
        public Material chairFabric;
        public Material metal;
        public Material darkPlastic;
        public Material whitePlastic;
        public Material lightPlastic;
        public Material wood;
        public Material cardboard;
        public Material coat;

        [Header("Screens and signs")]
        public Material screenDesktop;
        public Material screenEye;
        public Material screenOff;
        public Material copierScreen;
        public Material signPanel;
        public Material exitSign;
        public Material text;

        [Header("Decor")]
        public Material leaves;
        public Material pot;
        public Material soil;
        public Material paper;
        public Material stickyNote;
        public Material whiteboard;
        public Material cork;
        public Material clockFace;
        public Material red;
        public Material yellow;
        public Material blue;
        public Material water;
        public Material figure;
        public Material[] posters = new Material[0];
        public Material[] binders = new Material[0];

        [Header("Old tech (anomalies)")]
        public Material beige;
        public Material bakelite;
        public Material brass;
        public Material chrome;
        public Material tvScreen;
        public Material terminalScreen;
        public Material grille;
        public Material vintageGreen;
        public Material tvStatic;
        public Material dialGlow;
        public Material recLamp;

        [Header("Camera")]
        public Material vhs;
    }
}
