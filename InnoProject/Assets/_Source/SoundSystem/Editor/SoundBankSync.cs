using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SoundSystem.EditorTools
{

    public class SoundBankSync : AssetPostprocessor
    {
        static readonly Regex Numbered = new Regex(@"[\s_\-]*\(?\d+\)?$");

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool audio = imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(IsAudio);
            bool banks = imported.Any(p => p.EndsWith(".asset"));
            if (!audio && !banks) return;
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(SoundBank)))
                Sync(AssetDatabase.LoadAssetAtPath<SoundBank>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        static bool IsAudio(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".wav" || ext == ".ogg" || ext == ".mp3" || ext == ".aif" || ext == ".aiff" || ext == ".flac";
        }

        public static string SlotOf(string fileName) => Numbered.Replace(Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant(), "");

        [MenuItem("Assets/Sync Sound Banks")]
        public static void SyncAll()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(SoundBank)))
                Sync(AssetDatabase.LoadAssetAtPath<SoundBank>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        public static void Sync(SoundBank bank)
        {
            if (bank == null) return;
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(bank)).Replace('\\', '/');
            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(p => (slot: SlotOf(p), clip: AssetDatabase.LoadAssetAtPath<AudioClip>(p)))
                .Where(x => x.clip != null)
                .ToList();

            bool changed = false;
            foreach (var slot in bank.slots)
            {
                var found = clips.Where(x => x.slot == slot.name.ToLowerInvariant()).Select(x => x.clip).ToArray();
                if (found.SequenceEqual(slot.clips)) continue;
                slot.clips = found;
                changed = true;
            }
            if (!changed) return;
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssetIfDirty(bank);
        }
    }
}
