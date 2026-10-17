using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AnomalySystem.EditorTools
{
    /// <summary>
    /// Keeps every <see cref="AnomalyLibrary"/> in sync with the prefabs in its own folder: drop a prefab into the
    /// anomalies folder (or delete one) and the library updates itself.
    /// </summary>
    public sealed class AnomalyLibrarySync : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var changed = imported.Concat(deleted).Concat(moved).Concat(movedFrom).Where(p => p.EndsWith(".prefab")).ToArray();
            if (changed.Length == 0) return;
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(AnomalyLibrary)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string folder = Path.GetDirectoryName(path)?.Replace('\\', '/') + "/";
                if (changed.Any(p => p.StartsWith(folder))) Sync(AssetDatabase.LoadAssetAtPath<AnomalyLibrary>(path));
            }
        }

        /// <summary>Fills the library with the prefabs of its folder (sorted by name); saves only on change.</summary>
        public static void Sync(AnomalyLibrary library)
        {
            if (library == null) return;
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(library))?.Replace('\\', '/');
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null)
                .OrderBy(p => p.name)
                .ToArray();
            if (library.prefabs != null && library.prefabs.SequenceEqual(prefabs)) return;
            library.prefabs = prefabs;
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssetIfDirty(library);
        }
    }
}
