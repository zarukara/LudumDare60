using UnityEngine;

namespace AnomalySystem
{

    [CreateAssetMenu(menuName = "Anomalies/Anomaly Library", fileName = "AnomalyLibrary")]
    public class AnomalyLibrary : ScriptableObject
    {
        public GameObject[] prefabs = new GameObject[0];
    }
}
