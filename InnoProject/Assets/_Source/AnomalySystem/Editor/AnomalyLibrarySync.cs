using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AnomalySystem.EditorTools
{

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

        public static void Sync(AnomalyLibrary library)
        {
            if (library == null) return;
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(library))?.Replace('\\', '/');
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".prefab"))
                .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p))
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
