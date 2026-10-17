using System.IO;
using System.Text;
using PlayerSystem;
using RoundSystem;
using SoundSystem;
using SoundSystem.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ScareSystem.EditorTools
{
    /// <summary>
    /// Sets up the Babaika chase: the scare sound bank with its named slots (a README.txt next to it lists them)
    /// and the <see cref="BabaikaChase"/> on the scene's Game object.
    /// </summary>
    public static class ScareSetup
    {
        public const string SoundFolder = "Assets/_Presentation/Sounds/Scares";
        public const string BankPath = SoundFolder + "/ScareSounds.asset";
        public const string BabaikaPrefabPath = "Assets/_Presentation/Babaika/Babaika.prefab";

        static readonly (string name, string description, SoundBank.Placeholder placeholder, float volume)[] Slots =
        {
            (BabaikaChase.SoundDescend, "Шорох и скрежет, пока бабайка спускается с потолка в катсцене (3D).", SoundBank.Placeholder.Hiss, 0.9f),
            (BabaikaChase.SoundScream, "Её вопль в конце спуска, прямо перед погоней (3D).", SoundBank.Placeholder.Scream, 1f),
            (BabaikaChase.SoundChaseLoop, "Петля: хрип и скрежет бабайки, пока она гонится (3D, звучит от неё).", SoundBank.Placeholder.None, 0.8f),
            (BabaikaChase.SoundClaws, "Щелчок когтей по полу и стенам на каждом её шаге (3D). Пусто: бетонные шаги выше тоном.", SoundBank.Placeholder.None, 0.6f),
            (BabaikaChase.SoundMusic, "Петля: музыка или гул погони (2D).", SoundBank.Placeholder.Drone, 0.6f),
            (BabaikaChase.SoundHeartbeat, "Петля: сердцебиение, громче и быстрее, когда она близко (2D).", SoundBank.Placeholder.Heartbeat, 0.8f),
            (BabaikaChase.SoundSlam, "Дверь с силой захлопывается за игроком на 1 уровне (3D).", SoundBank.Placeholder.Slam, 1f),
            (BabaikaChase.SoundBang, "Бабайка бьётся в захлопнутую дверь, лучше 3-4 варианта (3D).", SoundBank.Placeholder.Thud, 1f),
            (BabaikaChase.SoundScreamer, "Скример, когда она хватает игрока (2D, громко).", SoundBank.Placeholder.Scream, 1f),
        };

        /// <summary>Creates or updates the bank: every slot exists with its description; chosen sounds are kept.</summary>
        public static SoundBank EnsureSoundBank()
        {
            if (!AssetDatabase.IsValidFolder(SoundFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Presentation/Sounds")) AssetDatabase.CreateFolder("Assets/_Presentation", "Sounds");
                AssetDatabase.CreateFolder("Assets/_Presentation/Sounds", "Scares");
            }
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }
            foreach (var (name, description, placeholder, volume) in Slots)
            {
                var slot = bank.Find(name);
                if (slot == null)
                {
                    slot = new SoundBank.Slot { name = name, volume = volume };
                    bank.slots.Add(slot);
                }
                slot.description = description;
                slot.placeholder = placeholder;
            }
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssetIfDirty(bank);
            WriteReadme();
            SoundBankSync.Sync(bank);
            return bank;
        }

        static void WriteReadme()
        {
            var text = new StringBuilder();
            text.AppendLine("Звуки события с бабайкой (5 уровень: спуск с потолка, погоня до 1 уровня, дверь или скример).");
            text.AppendLine("Положи сюда файлы с этими именами, и они сами попадут в свой слот ScareSounds.asset.");
            text.AppendLine("Несколько вариантов одного звука: door_bang.wav, door_bang_2.wav, door_bang_3.wav, будут играть вперемешку.");
            text.AppendLine("Подходят .wav, .ogg, .mp3. Пока файла нет, играет грубая заглушка или тишина.");
            text.AppendLine();
            foreach (var (name, description, _, _) in Slots) text.AppendLine($"{name,-20} {description}");
            File.WriteAllText(SoundFolder + "/README.txt", text.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(SoundFolder + "/README.txt");
        }

        /// <summary>Adds (or refreshes) the chase on the Game object.</summary>
        public static BabaikaChase AddChase(GameObject game, RoundManager round, PlayerController player, Camera view)
        {
            var chase = game.GetComponent<BabaikaChase>();
            if (chase == null) chase = game.AddComponent<BabaikaChase>();
            chase.round = round;
            chase.player = player;
            chase.view = view;
            chase.sounds = EnsureSoundBank();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BabaikaPrefabPath);
            chase.babaikaPrefab = prefab != null ? prefab.GetComponent<BabaikaSystem.Babaika>() : null;
            EditorUtility.SetDirty(chase);
            return chase;
        }
    }
}
