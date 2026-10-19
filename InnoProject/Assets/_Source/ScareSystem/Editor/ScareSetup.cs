using System;
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
    /// Sets up the Babaika's scares: the scare sound bank with its named slots (a README.txt next to it lists them),
    /// the <see cref="BabaikaChase"/> with its aftermath, and the hunt on the exit stairs (<see cref="AddStairs"/>).
    /// </summary>
    public static class ScareSetup
    {
        public const string SoundFolder = "Assets/_Presentation/Sounds/Scares";
        public const string BankPath = SoundFolder + "/ScareSounds.asset";
        public const string BabaikaPrefabPath = "Assets/_Presentation/Babaika/Babaika.prefab";

        static readonly (string name, string description, SoundBank.Placeholder placeholder, float volume)[] Slots =
        {
            (BabaikaChase.SoundScream, "Её вопль в конце спуска с потолка, прямо перед погоней (3D).", SoundBank.Placeholder.Scream, 1f),
            (BabaikaChase.SoundChaseLoop, "Петля: хрип и скрежет бабайки, пока она гонится (3D, звучит от неё).", SoundBank.Placeholder.None, 0.8f),
            (BabaikaChase.SoundClaws, "Щелчок когтей по полу и стенам на каждом её шаге (3D). Пусто: бетонные шаги выше тоном.", SoundBank.Placeholder.None, 0.6f),
            (BabaikaChase.SoundMusic, "Петля: музыка или гул погони (2D).", SoundBank.Placeholder.Drone, 0.6f),
            (BabaikaChase.SoundHeartbeat, "Петля: сердцебиение, громче и быстрее, когда она близко (2D).", SoundBank.Placeholder.Heartbeat, 0.8f),
            (BabaikaChase.SoundSlam, "Дверь с силой захлопывается за игроком на 1 уровне (3D).", SoundBank.Placeholder.Slam, 1f),
            (BabaikaChase.SoundBang, "Бабайка бьётся в захлопнутую дверь, лучше 3-4 варианта (3D).", SoundBank.Placeholder.Thud, 1f),
            (BabaikaChase.SoundScreamer, "Скример, когда она хватает игрока (2D, громко). Пусто: её вопль babaika_scream.", SoundBank.Placeholder.Scream, 1f),
            (GoreOffice.SoundReveal, "После погони: открылась дверь офиса 6 уровня, а там кишки и кровь (2D, вместе с испугом).", SoundBank.Placeholder.None, 0.9f),
            (StairsHunt.SoundAlarm, "Петля: сирена тревоги, воет с 7 этажа до выхода, пока мигает красный свет (2D, тише, когда она рядом).", SoundBank.Placeholder.Drone, 0.8f),
            (StairsHunt.SoundBreathIn, "Игрок затаил дыхание за тележкой: короткий резкий вдох (2D).", SoundBank.Placeholder.None, 0.8f),
            (StairsHunt.SoundBreathOut, "Она проползла мимо: облегчённый выдох (2D).", SoundBank.Placeholder.None, 0.8f),
            (StairsHunt.SoundGasp, "Сорвался на шкале дыхания: судорожный вдох или кашель, сразу перед скримером (2D).", SoundBank.Placeholder.Hiss, 1f),
            (StairsHunt.SoundRewind, "Петля: кассета перематывается назад после скримера на лестнице (2D).", SoundBank.Placeholder.Rewind, 0.7f),
        };

        /// <summary>Slots that borrow another slot's files while they have none of their own.</summary>
        static readonly (string slot, string fallback)[] Fallbacks =
        {
            (BabaikaChase.SoundScreamer, BabaikaChase.SoundScream),
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
                slot.fallback = Array.Find(Fallbacks, f => f.slot == name).fallback;
            }
            // Slots the chase no longer plays go, unless sounds were already chosen for them.
            bank.slots.RemoveAll(s => !Array.Exists(Slots, x => x.name == s.name) && (s.clips == null || !Array.Exists(s.clips, c => c != null)));
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssetIfDirty(bank);
            WriteReadme();
            SoundBankSync.Sync(bank);
            return bank;
        }

        static void WriteReadme()
        {
            var text = new StringBuilder();
            text.AppendLine("Звуки событий с бабайкой (5 уровень: спуск с потолка, погоня до 1 уровня, дверь или скример; потом кишки на 6 уровне;");
            text.AppendLine("в конце лестница: красный свет с 7 этажа, она ползёт по потолку, игрок прячется и задерживает дыхание).");
            text.AppendLine("Положи сюда файлы с этими именами, и они сами попадут в свой слот ScareSounds.asset.");
            text.AppendLine("Несколько вариантов одного звука: door_bang.wav, door_bang_2.wav, door_bang_3.wav, будут играть вперемешку.");
            text.AppendLine("Подходят .wav, .ogg, .mp3. Пока файла нет, играет грубая заглушка или тишина.");
            text.AppendLine();
            foreach (var (name, description, _, _) in Slots) text.AppendLine($"{name,-20} {description}");
            File.WriteAllText(SoundFolder + "/README.txt", text.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(SoundFolder + "/README.txt");
        }

        /// <summary>
        /// Adds (or refreshes) the chase and its aftermath (<see cref="GoreSetup.AddGore"/>) on the Game object, and the
        /// hunt on the exit stairs.
        /// </summary>
        public static BabaikaChase AddChase(GameObject game, RoundManager round, PlayerController player, Camera view)
        {
            var chase = game.GetComponent<BabaikaChase>();
            if (chase == null) chase = game.AddComponent<BabaikaChase>();
            chase.round = round;
            chase.player = player;
            chase.view = view;
            chase.sounds = EnsureSoundBank();
            chase.ambience = game.GetComponent<Ambience>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BabaikaPrefabPath);
            chase.babaikaPrefab = prefab != null ? prefab.GetComponent<BabaikaSystem.Babaika>() : null;
            EditorUtility.SetDirty(chase);
            GoreSetup.AddGore(game, round, chase, player, view);
            AddStairs(game, round, player, view);
            return chase;
        }

        /// <summary>
        /// The hunt on the exit stairs: <see cref="StairsHunt"/>, its <see cref="TapeRewind"/> and HUD on the Game
        /// object, hiding (<see cref="PlayerHide"/>) on the player.
        /// </summary>
        public static StairsHunt AddStairs(GameObject game, RoundManager round, PlayerController player, Camera view)
        {
            var hide = player.GetComponent<PlayerHide>();
            if (hide == null) hide = player.gameObject.AddComponent<PlayerHide>();
            hide.view = view;
            var tape = game.GetComponent<TapeRewind>();
            if (tape == null) tape = game.AddComponent<TapeRewind>();
            tape.player = player;
            var hunt = game.GetComponent<StairsHunt>();
            if (hunt == null) hunt = game.AddComponent<StairsHunt>();
            hunt.round = round;
            hunt.player = player;
            hunt.hide = hide;
            hunt.flashlight = player.GetComponentInChildren<Flashlight>(true);
            hunt.view = view;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BabaikaPrefabPath);
            hunt.babaikaPrefab = prefab != null ? prefab.GetComponent<BabaikaSystem.Babaika>() : null;
            hunt.sounds = EnsureSoundBank();
            hunt.ambience = game.GetComponent<Ambience>();
            hunt.tape = tape;
            var hud = game.GetComponent<UISystem.StairsHud>();
            if (hud == null) hud = game.AddComponent<UISystem.StairsHud>();
            hud.hunt = hunt;
            hud.hide = hide;
            foreach (var changed in new UnityEngine.Object[] { hide, tape, hunt, hud }) EditorUtility.SetDirty(changed);
            return hunt;
        }
    }
}
