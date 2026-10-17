using UnityEngine;

namespace LevelSystem
{
    /// <summary>
    /// Feeds the dynamic font atlas to a Text Mesh drawn with the "Office/Text" material (which, unlike the default
    /// font material, is hidden behind walls). Works in edit mode too and follows atlas rebuilds.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TextMesh))]
    public class FontBinder : MonoBehaviour
    {
        static Font defaultFont;
        static readonly int MainTex = Shader.PropertyToID("_MainTex");

        /// <summary>Unity's built-in font (has Cyrillic).</summary>
        public static Font DefaultFont => defaultFont != null ? defaultFont : defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        TextMesh textMesh;
        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;
        Texture bound;
        string boundText;

        void OnEnable() => Bind();

        void LateUpdate()
        {
            if (textMesh == null || textMesh.font == null) return;
            if (textMesh.font.material.mainTexture != bound || textMesh.text != boundText) Bind();
        }

        void Bind()
        {
            textMesh = GetComponent<TextMesh>();
            meshRenderer = GetComponent<MeshRenderer>();
            if (textMesh.font == null) textMesh.font = DefaultFont;
            if (!string.IsNullOrEmpty(textMesh.text))
                textMesh.font.RequestCharactersInTexture(textMesh.text, textMesh.fontSize, textMesh.fontStyle);
            bound = textMesh.font.material.mainTexture;
            boundText = textMesh.text;
            if (bound == null) return;
            block ??= new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(block);
            block.SetTexture(MainTex, bound);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}
