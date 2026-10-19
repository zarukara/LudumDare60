using UnityEngine;
using UnityEngine.UI;

namespace LocalizationSystem
{

    public class LocalizedText : MonoBehaviour
    {
        public string key;

        void OnEnable()
        {
            Localization.Changed += Apply;
            Apply();
        }

        void OnDisable() => Localization.Changed -= Apply;

        public void Apply()
        {
            if (string.IsNullOrEmpty(key)) return;
            string text = Localization.Get(key);
            if (TryGetComponent<TextMesh>(out var mesh)) mesh.text = text;
            if (TryGetComponent<Text>(out var label)) label.text = text;
        }
    }
}
