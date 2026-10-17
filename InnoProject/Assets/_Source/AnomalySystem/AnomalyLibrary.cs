using UnityEngine;

namespace AnomalySystem
{
    /// <summary>
    /// The list of anomaly prefabs. Keep the asset in the anomalies folder: in the editor its list follows the
    /// prefabs in that folder (and its subfolders) by itself, see AnomalyLibrarySync.
    /// </summary>
    [CreateAssetMenu(menuName = "Anomalies/Anomaly Library", fileName = "AnomalyLibrary")]
    public class AnomalyLibrary : ScriptableObject
    {
        public GameObject[] prefabs = new GameObject[0];
    }
}
