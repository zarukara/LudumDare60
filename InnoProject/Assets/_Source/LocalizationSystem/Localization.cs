using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocalizationSystem
{
    public enum Language
    {
        Russian,
        English,
    }

    /// <summary>
    /// The game's texts in Russian and English. The choice is made in the menu and kept in PlayerPrefs.
    /// Add a key to <see cref="Table"/> and use <see cref="Get"/> or a <see cref="LocalizedText"/> component.
    /// </summary>
    public static class Localization
    {
        const string PrefKey = "language";

        static readonly Dictionary<string, (string ru, string en)> Table = new Dictionary<string, (string, string)>
        {
            ["menu.play"] = ("Играть", "Play"),
            ["menu.quit"] = ("Выход", "Quit"),
            ["menu.language"] = ("Язык", "Language"),

            ["board.title"] = ("КАК ВЫБРАТЬСЯ", "HOW TO GET OUT"),
            ["board.line1"] = ("1. Внимательно осмотри офис.", "1. Look around the office."),
            ["board.line2"] = ("2. Всё как обычно — иди к дальней двери.", "2. All normal? Go on to the far door."),
            ["board.line3"] = ("3. Видишь старую технику — иди назад.", "3. Old tech that does not belong? Turn back."),
            ["board.line4"] = ("4. Ошибёшься — начнёшь сначала.", "4. One mistake and you start over."),
            ["board.line5"] = ("5. Восемь офисов подряд — и ты свободен.", "5. Eight offices in a row and you are free."),

            ["sign.round"] = ("РАУНД {0}", "ROUND {0}"),
            ["sign.exit"] = ("ВЫХОД", "EXIT"),

            ["hint.flashlight"] = ("F — фонарик", "F — flashlight"),
            ["hint.board"] = ("Подойди к доске и изучи её", "Go to the whiteboard and read it"),
            ["hint.run"] = ("БЕГИ! Shift", "RUN! Shift"),
            ["hud.finish"] = ("ФИНИШ", "THE END"),
            ["hud.time"] = ("Ты выбрался из офиса за {0}", "You got out of the office in {0}"),
            ["hud.again"] = ("R — пройти заново      M — в меню", "R — play again      M — menu"),
            ["hud.debug"] = ("Прогресс {0}/{1} · аномалия: {2}", "Progress {0}/{1} · anomaly: {2}"),
            ["hud.none"] = ("нет", "none"),
        };

        static Language? current;

        /// <summary>Fired after the language changes, so texts on screen can update.</summary>
        public static event Action Changed;

        public static Language Current
        {
            get
            {
                if (current == null) current = (Language)PlayerPrefs.GetInt(PrefKey, (int)Language.Russian);
                return current.Value;
            }
            set
            {
                if (current == value) return;
                current = value;
                PlayerPrefs.SetInt(PrefKey, (int)value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        public static string Get(string key)
        {
            if (!Table.TryGetValue(key, out var entry)) return key;
            return Current == Language.English ? entry.en : entry.ru;
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);
    }
}
